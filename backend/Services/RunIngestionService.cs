using System.Data;
using backend.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace backend.Services;

public class RunIngestionService : IRunIngestionService
{
    private readonly string _connectionString;
    private readonly ILogger<RunIngestionService> _logger;

    public RunIngestionService(IConfiguration configuration, ILogger<RunIngestionService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        _logger = logger;
    }

    public async Task<RunIngestResponse> IngestAsync(IReadOnlyCollection<RunIngestRow> rows, string sourceName, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
        {
            throw new ArgumentException("No rows provided for ingestion.", nameof(rows));
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        long batchId = await CreateBatchAsync(conn, tx, sourceName, cancellationToken);

        try
        {
            await BulkInsertStagingAsync(conn, tx, batchId, rows, cancellationToken);
            await PromoteToWarehouseAsync(conn, tx, batchId, cancellationToken);
            await UpdateBatchStatusAsync(conn, tx, batchId, "succeeded", rows.Count, null, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new RunIngestResponse(batchId, rows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest batch {BatchId}", batchId);
            await UpdateBatchStatusAsync(conn, tx, batchId, "failed", rows.Count, ex.Message, cancellationToken);
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task BackfillDimensionsFromStagingAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            await UpsertDimsFromAllStagingAsync(conn, tx, cancellationToken);
            await UpdateAthletesFromStagingAsync(conn, tx, cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backfill from staging failed.");
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<long> CreateBatchAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string sourceName, CancellationToken ct)
    {
        const string sql = """
            insert into stg_ingest_log (source_name, status)
            values (@source, 'running')
            returning id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@source", sourceName);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    private static async Task BulkInsertStagingAsync(NpgsqlConnection conn, NpgsqlTransaction tx, long batchId, IReadOnlyCollection<RunIngestRow> rows, CancellationToken ct)
    {
        const string copySql = """
            COPY stg_runs_raw (
                run_date,
                athlete_id_source,
                distance_km,
                duration_min,
                gender_raw,
                age_group_raw,
                country_raw,
                majors_raw,
                ingest_batch_id
            ) FROM STDIN (FORMAT BINARY)
        """;

        await using var writer = await conn.BeginBinaryImportAsync(copySql, ct);
        writer.Timeout = TimeSpan.FromMinutes(2);

        foreach (var row in rows)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(row.RunDate, NpgsqlDbType.Date, ct);
            await writer.WriteAsync(row.AthleteIdSource, NpgsqlDbType.Integer, ct);
            await writer.WriteAsync(row.DistanceKm, NpgsqlDbType.Numeric, ct);
            await writer.WriteAsync(row.DurationMin, NpgsqlDbType.Numeric, ct);
            await writer.WriteAsync((object?)row.Gender ?? DBNull.Value, NpgsqlDbType.Varchar, ct);
            await writer.WriteAsync((object?)row.AgeGroup ?? DBNull.Value, NpgsqlDbType.Varchar, ct);
            await writer.WriteAsync((object?)row.Country ?? DBNull.Value, NpgsqlDbType.Varchar, ct);
            var majors = row.Majors is { Count: > 0 } ? string.Join(",", row.Majors) : null;
            await writer.WriteAsync((object?)majors ?? DBNull.Value, NpgsqlDbType.Varchar, ct);
            await writer.WriteAsync(batchId, NpgsqlDbType.Bigint, ct);
        }

        await writer.CompleteAsync(ct);
    }

    private static async Task PromoteToWarehouseAsync(NpgsqlConnection conn, NpgsqlTransaction tx, long batchId, CancellationToken ct)
    {
        // Upsert dimensions and fact from this batch only
        var commands = new[]
        {
            // Gender, age group, country
            """
            insert into dim_gender (gender_code)
            select distinct gender_raw from stg_runs_raw
            where ingest_batch_id = @batch and gender_raw is not null
            on conflict (gender_code) do nothing;

            insert into dim_age_group (age_group_label, min_age, max_age)
            select distinct 
                trim(age_group_raw) as age_group_label,
                nullif(regexp_replace(split_part(trim(age_group_raw), '-', 1), '\D', '', 'g'), '')::int as min_age,
                case 
                    when trim(age_group_raw) like '%+%' then null
                    else nullif(regexp_replace(split_part(trim(age_group_raw), '-', 2), '\D', '', 'g'), '')::int
                end as max_age
            from stg_runs_raw
            where ingest_batch_id = @batch
              and age_group_raw is not null
              and age_group_raw <> ''
            on conflict (age_group_label) do update set
                min_age = coalesce(dim_age_group.min_age, excluded.min_age),
                max_age = coalesce(dim_age_group.max_age, excluded.max_age);

            insert into dim_country (country_name)
            select distinct country_raw from stg_runs_raw
            where ingest_batch_id = @batch and country_raw is not null
            on conflict (country_name) do nothing;
            """,
            // Majors
            """
            with exploded as (
                select distinct trim(value) as major_name,
                       case
                           when length(regexp_replace(trim(value), '\D', '', 'g')) >= 4
                           then right(regexp_replace(trim(value), '\D', '', 'g'), 4)::int
                           else null
                       end as major_year
                from stg_runs_raw,
                     regexp_split_to_table(coalesce(majors_raw, ''), ',') as value
                where ingest_batch_id = @batch
                  and majors_raw is not null
                  and majors_raw <> ''
            )
            insert into dim_major (major_name, major_year)
            select major_name, major_year from exploded
            on conflict (major_name) do update set
                major_year = coalesce(dim_major.major_year, excluded.major_year);
            """,
            // Dates
            """
            insert into dim_date (date_key, iso_year, iso_week, month, quarter, week_start_date)
            select distinct week_start,
                   extract(isoyear from week_start)::int,
                   extract(week from week_start)::int,
                   extract(month from week_start)::int,
                   extract(quarter from week_start)::int,
                   week_start
            from (
                select date_trunc('week', run_date)::date as week_start
                from stg_runs_raw
                where ingest_batch_id = @batch
            ) d
            on conflict (date_key) do nothing;
            """,
            // Athletes
            """
            insert into dim_athlete (athlete_id_source, gender_key, age_group_key, country_key, first_seen_week, last_seen_week)
            select s.athlete_id_source,
                   g.gender_key,
                   ag.age_group_key,
                   c.country_key,
                   min(date_trunc('week', s.run_date)::date),
                   max(date_trunc('week', s.run_date)::date)
            from stg_runs_raw s
            left join dim_gender g on g.gender_code = s.gender_raw
            left join dim_age_group ag on ag.age_group_label = trim(s.age_group_raw)
            left join dim_country c on c.country_name = s.country_raw
            where s.ingest_batch_id = @batch
            group by s.athlete_id_source, g.gender_key, ag.age_group_key, c.country_key
            on conflict (athlete_id_source) do update set
                gender_key = excluded.gender_key,
                age_group_key = excluded.age_group_key,
                country_key = excluded.country_key,
                first_seen_week = least(dim_athlete.first_seen_week, excluded.first_seen_week),
                last_seen_week = greatest(dim_athlete.last_seen_week, excluded.last_seen_week);
            """,
            // Bridge
            """
            insert into bridge_athlete_major (athlete_key, major_key)
            select distinct da.athlete_key, dm.major_key
            from stg_runs_raw s
            join dim_athlete da on da.athlete_id_source = s.athlete_id_source
            join regexp_split_to_table(coalesce(s.majors_raw, ''), ',') as m(value) on true
            join dim_major dm on dm.major_name = trim(m.value)
            where s.ingest_batch_id = @batch
              and s.majors_raw is not null
              and s.majors_raw <> ''
            on conflict do nothing;
            """,
            // Fact
            """
            insert into fact_run_weekly (date_key, athlete_key, distance_km, duration_min, pace_min_per_km, zero_distance_flag)
            select date_trunc('week', s.run_date)::date as date_key,
                   da.athlete_key,
                   s.distance_km,
                   s.duration_min,
                   case when coalesce(s.distance_km, 0) = 0 then null else s.duration_min / nullif(s.distance_km, 0) end as pace_min_per_km,
                   coalesce(s.distance_km, 0) = 0
            from stg_runs_raw s
            join dim_athlete da on da.athlete_id_source = s.athlete_id_source
            where s.ingest_batch_id = @batch
            on conflict (date_key, athlete_key) do update set
                distance_km = excluded.distance_km,
                duration_min = excluded.duration_min,
                pace_min_per_km = excluded.pace_min_per_km,
                zero_distance_flag = excluded.zero_distance_flag;
            """
        };

        foreach (var sql in commands)
        {
            await using var cmd = new NpgsqlCommand(sql, conn, tx)
            {
                CommandTimeout = 120
            };
            cmd.Parameters.AddWithValue("@batch", batchId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task UpsertDimsFromAllStagingAsync(NpgsqlConnection conn, NpgsqlTransaction tx, CancellationToken ct)
    {
        var sql = """
            insert into dim_gender (gender_code)
            select distinct gender_raw from stg_runs_raw
            where gender_raw is not null
            on conflict (gender_code) do nothing;

            insert into dim_age_group (age_group_label, min_age, max_age)
            select distinct 
                trim(age_group_raw) as age_group_label,
                nullif(regexp_replace(split_part(trim(age_group_raw), '-', 1), '\D', '', 'g'), '')::int as min_age,
                case 
                    when trim(age_group_raw) like '%+%' then null
                    else nullif(regexp_replace(split_part(trim(age_group_raw), '-', 2), '\D', '', 'g'), '')::int
                end as max_age
            from stg_runs_raw
            where age_group_raw is not null
              and age_group_raw <> ''
            on conflict (age_group_label) do update set
                min_age = coalesce(dim_age_group.min_age, excluded.min_age),
                max_age = coalesce(dim_age_group.max_age, excluded.max_age);

            insert into dim_country (country_name)
            select distinct country_raw from stg_runs_raw
            where country_raw is not null
            on conflict (country_name) do nothing;

            with exploded as (
                select distinct trim(value) as major_name,
                       case
                           when length(regexp_replace(trim(value), '\D', '', 'g')) >= 4
                           then right(regexp_replace(trim(value), '\D', '', 'g'), 4)::int
                           else null
                       end as major_year
                from stg_runs_raw,
                     regexp_split_to_table(coalesce(majors_raw, ''), ',') as value
                where majors_raw is not null
                  and majors_raw <> ''
            )
            insert into dim_major (major_name, major_year)
            select major_name, major_year from exploded
            on conflict (major_name) do update set
                major_year = coalesce(dim_major.major_year, excluded.major_year);
        """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx) { CommandTimeout = 180 };
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpdateAthletesFromStagingAsync(NpgsqlConnection conn, NpgsqlTransaction tx, CancellationToken ct)
    {
        var sql = """
            with staged as (
                select s.athlete_id_source,
                       g.gender_key,
                       ag.age_group_key,
                       c.country_key,
                       min(date_trunc('week', s.run_date)::date) as first_seen,
                       max(date_trunc('week', s.run_date)::date) as last_seen
                from stg_runs_raw s
                left join dim_gender g on g.gender_code = s.gender_raw
                left join dim_age_group ag on ag.age_group_label = trim(s.age_group_raw)
                left join dim_country c on c.country_name = s.country_raw
                group by s.athlete_id_source, g.gender_key, ag.age_group_key, c.country_key
            )
            update dim_athlete a
            set gender_key = coalesce(a.gender_key, staged.gender_key),
                age_group_key = coalesce(a.age_group_key, staged.age_group_key),
                country_key = coalesce(a.country_key, staged.country_key),
                first_seen_week = least(coalesce(a.first_seen_week, staged.first_seen), staged.first_seen),
                last_seen_week = greatest(coalesce(a.last_seen_week, staged.last_seen), staged.last_seen)
            from staged
            where staged.athlete_id_source = a.athlete_id_source;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx) { CommandTimeout = 180 };
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpdateBatchStatusAsync(NpgsqlConnection conn, NpgsqlTransaction tx, long batchId, string status, int rowCount, string? notes, CancellationToken ct)
    {
        const string sql = """
            update stg_ingest_log
            set status = @status,
                row_count = @row_count,
                notes = @notes
            where id = @batch;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@row_count", rowCount);
        cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@batch", batchId);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}

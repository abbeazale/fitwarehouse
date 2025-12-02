using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IConfiguration configuration, ILogger<AnalyticsController> logger)
    {
        _connectionString = configuration.GetConnectionString("SupabaseConnection")
                             ?? configuration.GetConnectionString("DefaultConnection")
                             ?? throw new InvalidOperationException("Database connection string is not configured.");
        _logger = logger;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] int weeks = 12, CancellationToken cancellationToken = default)
    {
        weeks = weeks <= 0 ? 12 : weeks;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var response = new OverviewResponse
        {
            Weeks = weeks,
            Series = new List<WeeklySeriesPoint>(),
            Kpis = new OverviewKpis()
        };

        try
        {
            // Series query
            var seriesSql = @"
                with recent_weeks as (
                    select distinct d.date_key, d.week_start_date
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    order by d.date_key desc
                    limit @weeks
                ),
                windowed as (
                    select frw.distance_km,
                           frw.athlete_key,
                           d.week_start_date,
                           a.age_group_key
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    join dim_athlete a on a.athlete_key = frw.athlete_key
                    join recent_weeks rw on rw.date_key = d.date_key
                )
                select w.week_start_date as week_start,
                       coalesce(ag.age_group_label, 'Unknown') as age_group,
                       sum(coalesce(w.distance_km, 0)) as total_distance_km,
                       count(distinct w.athlete_key) as runner_count,
                       case when count(distinct w.athlete_key) = 0 then 0
                            else sum(coalesce(w.distance_km, 0)) / count(distinct w.athlete_key)
                       end as avg_distance_km_per_runner
                from windowed w
                left join dim_age_group ag on ag.age_group_key = w.age_group_key
                group by w.week_start_date, ag.age_group_label
                order by w.week_start_date, ag.age_group_label;
            ";

            await using (var cmd = new NpgsqlCommand(seriesSql, conn))
            {
                cmd.Parameters.AddWithValue("@weeks", weeks);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    response.Series.Add(new WeeklySeriesPoint
                    {
                        WeekStart = reader.GetDateTime(0).Date,
                        AgeGroup = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1),
                        TotalDistanceKm = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                        RunnerCount = reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
                        AvgDistanceKmPerRunner = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4)
                    });
                }
            }

            // KPI query
            var kpiSql = @"
                with recent_weeks as (
                    select distinct d.date_key, d.week_start_date
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    order by d.date_key desc
                    limit @weeks
                ),
                windowed as (
                    select coalesce(frw.distance_km,0) as distance_km,
                           frw.athlete_key,
                           d.week_start_date
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    join recent_weeks rw on rw.date_key = d.date_key
                )
                select
                    sum(w.distance_km) as total_distance_km,
                    case
                        when count(distinct w.week_start_date) = 0 or count(distinct w.athlete_key) = 0 then 0
                        else sum(w.distance_km) / (count(distinct w.week_start_date) * count(distinct w.athlete_key))
                    end as avg_weekly_distance_per_runner,
                    count(distinct w.athlete_key) as total_runners,
                    (
                        select sum(w2.distance_km)
                        from windowed w2
                        where w2.week_start_date = (select max(week_start_date) from windowed)
                    ) as latest_week_distance_km
                from windowed w;
            ";

            await using (var cmd = new NpgsqlCommand(kpiSql, conn))
            {
                cmd.Parameters.AddWithValue("@weeks", weeks);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    response.Kpis = new OverviewKpis
                    {
                        TotalDistanceKm = reader.IsDBNull(0) ? 0m : reader.GetDecimal(0),
                        AvgWeeklyDistanceKm = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1),
                        TotalRunners = reader.IsDBNull(2) ? 0 : reader.GetInt64(2),
                        LatestWeekDistanceKm = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3)
                    };
                }
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch overview analytics.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch analytics.");
        }
    }

    public class OverviewResponse
    {
        public int Weeks { get; set; }
        public List<WeeklySeriesPoint> Series { get; set; } = [];
        public OverviewKpis Kpis { get; set; } = new();
    }

    public class WeeklySeriesPoint
    {
        public DateTime WeekStart { get; set; }
        public string AgeGroup { get; set; } = "Unknown";
        public decimal TotalDistanceKm { get; set; }
        public long RunnerCount { get; set; }
        public decimal AvgDistanceKmPerRunner { get; set; }
    }

    [HttpGet("pace-by-demo")]
    public async Task<IActionResult> GetPaceByDemo([FromQuery] int weeks = 12, CancellationToken cancellationToken = default)
    {
        weeks = weeks <= 0 ? 12 : weeks;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var response = new PaceResponse
        {
            Weeks = weeks,
            Series = new List<PaceSeriesPoint>()
        };

        try
        {
            var sql = @"
                with recent_weeks as (
                    select distinct d.date_key, d.week_start_date
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    order by d.date_key desc
                    limit @weeks
                ),
                windowed as (
                    select frw.distance_km,
                           frw.duration_min,
                           frw.athlete_key,
                           d.week_start_date,
                           a.age_group_key,
                           a.gender_key
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    join dim_athlete a on a.athlete_key = frw.athlete_key
                    join recent_weeks rw on rw.date_key = d.date_key
                    where frw.distance_km is not null
                      and frw.distance_km > 0
                )
                select w.week_start_date as week_start,
                       coalesce(g.gender_code, 'U') || ' • ' || coalesce(ag.age_group_label, 'Unknown') as label,
                       case when sum(w.distance_km) = 0 then null
                            else sum(w.duration_min) / sum(w.distance_km)
                       end as avg_pace_min_per_km
                from windowed w
                left join dim_age_group ag on ag.age_group_key = w.age_group_key
                left join dim_gender g on g.gender_key = w.gender_key
                group by w.week_start_date, label
                order by w.week_start_date, label;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@weeks", weeks);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                response.Series.Add(new PaceSeriesPoint
                {
                    WeekStart = reader.GetDateTime(0).Date,
                    Label = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1),
                    AvgPaceMinPerKm = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2)
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch pace by demo.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch pace trends.");
        }
    }

    [HttpGet("major-distance-by-year")]
    public async Task<IActionResult> GetMajorDistanceByYear([FromQuery] int weeks = 12, CancellationToken cancellationToken = default)
    {
        weeks = weeks <= 0 ? 12 : weeks;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var response = new MajorDistanceResponse
        {
            Weeks = weeks,
            Series = new List<MajorDistancePoint>()
        };

        try
        {
            var sql = @"
                with recent_weeks as (
                    select distinct d.date_key, d.week_start_date
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    order by d.date_key desc
                    limit @weeks
                )
                select d.week_start_date as week_start,
                       coalesce(dm.major_year::text, 'Unknown') as major_year,
                       sum(coalesce(frw.distance_km,0)) as total_distance_km,
                       count(distinct frw.athlete_key) as runner_count,
                       case when count(distinct frw.athlete_key) = 0 then 0
                            else sum(coalesce(frw.distance_km,0)) / count(distinct frw.athlete_key)
                       end as avg_distance_km_per_runner
                from fact_run_weekly frw
                join dim_date d on d.date_key = frw.date_key
                join dim_athlete a on a.athlete_key = frw.athlete_key
                join bridge_athlete_major bam on bam.athlete_key = a.athlete_key
                join dim_major dm on dm.major_key = bam.major_key
                join recent_weeks rw on rw.date_key = d.date_key
                group by d.week_start_date, dm.major_year
                order by d.week_start_date, dm.major_year;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@weeks", weeks);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                response.Series.Add(new MajorDistancePoint
                {
                    WeekStart = reader.GetDateTime(0).Date,
                    MajorYear = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1),
                    TotalDistanceKm = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                    RunnerCount = reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
                    AvgDistanceKmPerRunner = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4)
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch major distance by year.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch major distance trends.");
        }
    }

    [HttpGet("top-countries")]
    public async Task<IActionResult> GetTopCountries([FromQuery] int weeks = 12, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        weeks = weeks <= 0 ? 12 : weeks;
        limit = limit <= 0 ? 50 : Math.Min(limit, 200);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var response = new TopCountryResponse
        {
            Weeks = weeks,
            Countries = new List<TopCountryEntry>()
        };

        try
        {
            var sql = @"
                with recent_weeks as (
                    select distinct d.date_key, d.week_start_date
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    order by d.date_key desc
                    limit @weeks
                )
                select coalesce(c.country_name, 'Unknown') as country,
                       sum(coalesce(frw.distance_km,0)) as total_distance_km,
                       case when sum(coalesce(frw.distance_km,0)) = 0 then null
                            else sum(coalesce(frw.duration_min,0)) / sum(coalesce(frw.distance_km,0))
                       end as avg_pace_min_per_km
                from fact_run_weekly frw
                join dim_date d on d.date_key = frw.date_key
                join dim_athlete a on a.athlete_key = frw.athlete_key
                left join dim_country c on c.country_key = a.country_key
                join recent_weeks rw on rw.date_key = d.date_key
                group by c.country_name
                order by total_distance_km desc nulls last
                limit @limit;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@weeks", weeks);
            cmd.Parameters.AddWithValue("@limit", limit);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            int rank = 1;
            while (await reader.ReadAsync(cancellationToken))
            {
                response.Countries.Add(new TopCountryEntry
                {
                    Rank = rank++,
                    Country = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                    TotalDistanceKm = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1),
                    AvgPaceMinPerKm = reader.IsDBNull(2) ? (decimal?)null : reader.GetDecimal(2)
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch top countries.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch top countries.");
        }
    }

    [HttpGet("athlete-pace")]
    public async Task<IActionResult> GetAthletePace([FromQuery] int athleteId, [FromQuery] int weeks = 52, CancellationToken cancellationToken = default)
    {
        if (athleteId <= 0)
        {
            return BadRequest("athleteId is required and must be > 0.");
        }

        weeks = weeks <= 0 ? 52 : weeks;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var response = new AthletePaceResponse
        {
            AthleteId = athleteId,
            Weeks = weeks,
            Series = new List<AthletePacePoint>()
            ,
            Majors = new List<AthleteMajor>()
        };

        try
        {
            var sql = @"
                with recent_weeks as (
                    select distinct d.date_key, d.week_start_date
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    join dim_athlete a on a.athlete_key = frw.athlete_key
                    where a.athlete_id_source = @athleteId
                    order by d.date_key desc
                    limit @weeks
                )
                select d.week_start_date,
                       coalesce(frw.pace_min_per_km, case when coalesce(frw.distance_km,0) = 0 then null else frw.duration_min / nullif(frw.distance_km,0) end) as pace_min_per_km
                from fact_run_weekly frw
                join dim_date d on d.date_key = frw.date_key
                join dim_athlete a on a.athlete_key = frw.athlete_key
                join recent_weeks rw on rw.date_key = d.date_key
                where a.athlete_id_source = @athleteId
                and (
                    frw.pace_min_per_km is not null
                    or (frw.distance_km is not null and frw.distance_km > 0 and frw.duration_min is not null)
                )
                order by d.date_key;
            ";

            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@athleteId", athleteId);
                cmd.Parameters.AddWithValue("@weeks", weeks);
                await using var paceReader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await paceReader.ReadAsync(cancellationToken))
                {
                    var pace = paceReader.IsDBNull(1) ? 0m : paceReader.GetDecimal(1);
                    if (pace > 0)
                    {
                        response.Series.Add(new AthletePacePoint
                        {
                            WeekStart = paceReader.GetDateTime(0).Date,
                            PaceMinPerKm = pace
                        });
                    }
                }
            }

            var majorsSql = @"
                select dm.major_name, dm.major_year
                from dim_athlete a
                join bridge_athlete_major bam on bam.athlete_key = a.athlete_key
                join dim_major dm on dm.major_key = bam.major_key
                where a.athlete_id_source = @athleteId
                order by dm.major_year nulls last, dm.major_name;
            ";

            await using (var majorsCmd = new NpgsqlCommand(majorsSql, conn))
            {
                majorsCmd.Parameters.AddWithValue("@athleteId", athleteId);
                await using var majorsReader = await majorsCmd.ExecuteReaderAsync(cancellationToken);
                while (await majorsReader.ReadAsync(cancellationToken))
                {
                    response.Majors.Add(new AthleteMajor
                    {
                        MajorName = majorsReader.IsDBNull(0) ? string.Empty : majorsReader.GetString(0),
                        MajorYear = majorsReader.IsDBNull(1) ? null : majorsReader.GetInt32(1)
                    });
                }
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch athlete pace for {AthleteId}", athleteId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch athlete pace.");
        }
    }

    [HttpGet("distance-by-gender")]
    public async Task<IActionResult> GetDistanceByGender([FromQuery] int weeks = 52, CancellationToken cancellationToken = default)
    {
        weeks = weeks <= 0 ? 52 : weeks;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var response = new GenderDistanceResponse
        {
            Weeks = weeks,
            Series = new List<GenderDistancePoint>()
        };

        try
        {
            var sql = @"
                with recent_weeks as (
                    select distinct d.date_key, d.week_start_date
                    from fact_run_weekly frw
                    join dim_date d on d.date_key = frw.date_key
                    order by d.date_key desc
                    limit @weeks
                )
                select d.week_start_date as week_start,
                       coalesce(g.gender_code, 'U') as gender,
                       sum(coalesce(frw.distance_km,0)) as total_distance_km,
                       count(distinct frw.athlete_key) as runner_count,
                       case when count(distinct frw.athlete_key) = 0 then 0
                            else sum(coalesce(frw.distance_km,0)) / count(distinct frw.athlete_key)
                       end as avg_distance_km_per_runner
                from fact_run_weekly frw
                join dim_date d on d.date_key = frw.date_key
                join dim_athlete a on a.athlete_key = frw.athlete_key
                left join dim_gender g on g.gender_key = a.gender_key
                join recent_weeks rw on rw.date_key = d.date_key
                group by d.week_start_date, g.gender_code
                order by d.week_start_date, g.gender_code;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@weeks", weeks);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                response.Series.Add(new GenderDistancePoint
                {
                    WeekStart = reader.GetDateTime(0).Date,
                    Gender = reader.IsDBNull(1) ? "U" : reader.GetString(1),
                    TotalDistanceKm = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                    RunnerCount = reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
                    AvgDistanceKmPerRunner = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4)
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch distance by gender.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch gender distance trends.");
        }
    }


    public class OverviewKpis
    {
        public decimal TotalDistanceKm { get; set; }
        public decimal AvgWeeklyDistanceKm { get; set; }
        public long TotalRunners { get; set; }
        public decimal LatestWeekDistanceKm { get; set; }
    }

    public class PaceResponse
    {
        public int Weeks { get; set; }
        public List<PaceSeriesPoint> Series { get; set; } = [];
    }

    public class PaceSeriesPoint
    {
        public DateTime WeekStart { get; set; }
        public string Label { get; set; } = "Unknown";
        public decimal AvgPaceMinPerKm { get; set; }
    }

    public class MajorDistanceResponse
    {
        public int Weeks { get; set; }
        public List<MajorDistancePoint> Series { get; set; } = [];
    }

    public class MajorDistancePoint
    {
        public DateTime WeekStart { get; set; }
        public string MajorYear { get; set; } = "Unknown";
        public decimal TotalDistanceKm { get; set; }
        public long RunnerCount { get; set; }
        public decimal AvgDistanceKmPerRunner { get; set; }
    }

    public class AthletePaceResponse
    {
        public int AthleteId { get; set; }
        public int Weeks { get; set; }
        public List<AthletePacePoint> Series { get; set; } = [];
        public List<AthleteMajor> Majors { get; set; } = [];
    }

    public class AthletePacePoint
    {
        public DateTime WeekStart { get; set; }
        public decimal PaceMinPerKm { get; set; }
    }

    public class AthleteMajor
    {
        public string MajorName { get; set; } = string.Empty;
        public int? MajorYear { get; set; }
    }

    public class TopCountryResponse
    {
        public int Weeks { get; set; }
        public List<TopCountryEntry> Countries { get; set; } = [];
    }

    public class TopCountryEntry
    {
        public int Rank { get; set; }
        public string Country { get; set; } = "Unknown";
        public decimal TotalDistanceKm { get; set; }
        public decimal? AvgPaceMinPerKm { get; set; }
    }

    public class MajorGenderEntry
    {
        public string MajorName { get; set; } = "Unknown";
        public string Gender { get; set; } = "U";
        public long RunnerCount { get; set; }
    }

    public class MajorGenderResponse
    {
        public List<MajorGenderEntry> Series { get; set; } = [];
    }

    [HttpGet("major-gender-distribution")]
    public async Task<IActionResult> GetMajorGenderDistribution([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        limit = limit <= 0 ? 10 : Math.Min(limit, 50);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var response = new MajorGenderResponse
        {
            Series = new List<MajorGenderEntry>()
        };

        try
        {
            var sql = @"
                with major_counts as (
                    select dm.major_name,
                           dm.major_year,
                           g.gender_code,
                           count(distinct bam.athlete_key) as runner_count
                    from bridge_athlete_major bam
                    join dim_major dm on dm.major_key = bam.major_key
                    join dim_athlete a on a.athlete_key = bam.athlete_key
                    left join dim_gender g on g.gender_key = a.gender_key
                    group by dm.major_name, dm.major_year, g.gender_code
                ),
                top_majors as (
                    select major_name, major_year
                    from (
                        select dm.major_name, dm.major_year, sum(runner_count) as total_runners
                        from major_counts dm
                        group by dm.major_name, dm.major_year
                        order by total_runners desc
                        limit @limit
                    ) t
                )
                select tm.major_name as major_name,
                       coalesce(mc.gender_code, 'U') as gender,
                       coalesce(mc.runner_count, 0) as runner_count
                from top_majors tm
                left join major_counts mc
                  on mc.major_name = tm.major_name
                 and coalesce(mc.major_year, -1) = coalesce(tm.major_year, -1)
                order by tm.major_name, mc.gender_code;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@limit", limit);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                response.Series.Add(new MajorGenderEntry
                {
                    MajorName = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                    Gender = reader.IsDBNull(1) ? "U" : reader.GetString(1),
                    RunnerCount = reader.IsDBNull(2) ? 0 : reader.GetInt64(2)
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch major gender distribution.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch major gender distribution.");
        }
    }

    public class GenderDistanceResponse
    {
        public int Weeks { get; set; }
        public List<GenderDistancePoint> Series { get; set; } = [];
    }

    public class GenderDistancePoint
    {
        public DateTime WeekStart { get; set; }
        public string Gender { get; set; } = "U";
        public decimal TotalDistanceKm { get; set; }
        public long RunnerCount { get; set; }
        public decimal AvgDistanceKmPerRunner { get; set; }
    }

    // Percentile DTOs removed
}

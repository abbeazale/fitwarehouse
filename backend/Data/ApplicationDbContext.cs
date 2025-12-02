using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<StagingIngestLog> StagingIngestLogs { get; set; } = null!;
    public DbSet<StagingRunRaw> StagingRunsRaw { get; set; } = null!;
    public DbSet<DimGender> DimGenders { get; set; } = null!;
    public DbSet<DimAgeGroup> DimAgeGroups { get; set; } = null!;
    public DbSet<DimCountry> DimCountries { get; set; } = null!;
    public DbSet<DimDate> DimDates { get; set; } = null!;
    public DbSet<DimMajor> DimMajors { get; set; } = null!;
    public DbSet<DimAthlete> DimAthletes { get; set; } = null!;
    public DbSet<BridgeAthleteMajor> BridgeAthleteMajors { get; set; } = null!;
    public DbSet<FactRunWeekly> FactRunWeeklies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StagingIngestLog>(entity =>
        {
            entity.ToTable("stg_ingest_log");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.SourceName)
                .HasColumnName("source_name")
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.LoadTimestampUtc)
                .HasColumnName("load_timestamp_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.RowCount)
                .HasColumnName("row_count");

            entity.Property(e => e.Checksum)
                .HasColumnName("checksum")
                .HasMaxLength(128);

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(32)
                .HasDefaultValue("pending")
                .IsRequired();

            entity.Property(e => e.Notes)
                .HasColumnName("notes");
        });

        modelBuilder.Entity<StagingRunRaw>(entity =>
        {
            entity.ToTable("stg_runs_raw");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.RunDate)
                .HasColumnName("run_date")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(e => e.AthleteIdSource)
                .HasColumnName("athlete_id_source")
                .IsRequired();

            entity.Property(e => e.DistanceKm)
                .HasColumnName("distance_km")
                .HasPrecision(10, 2);

            entity.Property(e => e.DurationMin)
                .HasColumnName("duration_min")
                .HasPrecision(10, 2);

            entity.Property(e => e.GenderRaw)
                .HasColumnName("gender_raw")
                .HasMaxLength(16);

            entity.Property(e => e.AgeGroupRaw)
                .HasColumnName("age_group_raw")
                .HasMaxLength(32);

            entity.Property(e => e.CountryRaw)
                .HasColumnName("country_raw")
                .HasMaxLength(64);

            entity.Property(e => e.MajorsRaw)
                .HasColumnName("majors_raw")
                .HasMaxLength(256);

            entity.Property(e => e.IngestBatchId)
                .HasColumnName("ingest_batch_id");

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<StagingIngestLog>()
                .WithMany()
                .HasForeignKey(e => e.IngestBatchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DimGender>(entity =>
        {
            entity.ToTable("dim_gender");

            entity.HasKey(e => e.GenderKey);

            entity.HasIndex(e => e.GenderCode)
                .IsUnique();

            entity.Property(e => e.GenderKey)
                .HasColumnName("gender_key")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.GenderCode)
                .HasColumnName("gender_code")
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(e => e.GenderLabel)
                .HasColumnName("gender_label")
                .HasMaxLength(64);
        });

        modelBuilder.Entity<DimAgeGroup>(entity =>
        {
            entity.ToTable("dim_age_group");

            entity.HasKey(e => e.AgeGroupKey);

            entity.HasIndex(e => e.AgeGroupLabel)
                .IsUnique();

            entity.Property(e => e.AgeGroupKey)
                .HasColumnName("age_group_key")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.AgeGroupLabel)
                .HasColumnName("age_group_label")
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(e => e.MinAge)
                .HasColumnName("min_age");

            entity.Property(e => e.MaxAge)
                .HasColumnName("max_age");
        });

        modelBuilder.Entity<DimCountry>(entity =>
        {
            entity.ToTable("dim_country");

            entity.HasKey(e => e.CountryKey);

            entity.HasIndex(e => e.CountryName)
                .IsUnique();

            entity.Property(e => e.CountryKey)
                .HasColumnName("country_key")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CountryName)
                .HasColumnName("country_name")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.IsoCode)
                .HasColumnName("iso_code")
                .HasMaxLength(16);

            entity.Property(e => e.Region)
                .HasColumnName("region")
                .HasMaxLength(64);
        });

        modelBuilder.Entity<DimDate>(entity =>
        {
            entity.ToTable("dim_date");

            entity.HasKey(e => e.DateKey);

            entity.Property(e => e.DateKey)
                .HasColumnName("date_key")
                .HasColumnType("date");

            entity.Property(e => e.IsoYear)
                .HasColumnName("iso_year")
                .IsRequired();

            entity.Property(e => e.IsoWeek)
                .HasColumnName("iso_week")
                .IsRequired();

            entity.Property(e => e.Month)
                .HasColumnName("month")
                .IsRequired();

            entity.Property(e => e.Quarter)
                .HasColumnName("quarter")
                .IsRequired();

            entity.Property(e => e.WeekStartDate)
                .HasColumnName("week_start_date")
                .HasColumnType("date")
                .IsRequired();
        });

        modelBuilder.Entity<DimMajor>(entity =>
        {
            entity.ToTable("dim_major");

            entity.HasKey(e => e.MajorKey);

            entity.HasIndex(e => e.MajorName)
                .IsUnique();

            entity.Property(e => e.MajorKey)
                .HasColumnName("major_key")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.MajorName)
                .HasColumnName("major_name")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.MajorYear)
                .HasColumnName("major_year");
        });

        modelBuilder.Entity<DimAthlete>(entity =>
        {
            entity.ToTable("dim_athlete");

            entity.HasKey(e => e.AthleteKey);

            entity.Property(e => e.AthleteKey)
                .HasColumnName("athlete_key")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.AthleteIdSource)
                .HasColumnName("athlete_id_source")
                .IsRequired();

            entity.Property(e => e.GenderKey)
                .HasColumnName("gender_key");

            entity.Property(e => e.AgeGroupKey)
                .HasColumnName("age_group_key");

            entity.Property(e => e.CountryKey)
                .HasColumnName("country_key");

            entity.Property(e => e.FirstSeenWeek)
                .HasColumnName("first_seen_week")
                .HasColumnType("date");

            entity.Property(e => e.LastSeenWeek)
                .HasColumnName("last_seen_week")
                .HasColumnType("date");

            entity.HasIndex(e => e.AthleteIdSource)
                .IsUnique();

            entity.HasOne<DimGender>()
                .WithMany()
                .HasForeignKey(e => e.GenderKey)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<DimAgeGroup>()
                .WithMany()
                .HasForeignKey(e => e.AgeGroupKey)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<DimCountry>()
                .WithMany()
                .HasForeignKey(e => e.CountryKey)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BridgeAthleteMajor>(entity =>
        {
            entity.ToTable("bridge_athlete_major");

            entity.HasKey(e => new { e.AthleteKey, e.MajorKey });

            entity.Property(e => e.AthleteKey)
                .HasColumnName("athlete_key");

            entity.Property(e => e.MajorKey)
                .HasColumnName("major_key");

            entity.HasOne<DimAthlete>()
                .WithMany()
                .HasForeignKey(e => e.AthleteKey)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<DimMajor>()
                .WithMany()
                .HasForeignKey(e => e.MajorKey)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FactRunWeekly>(entity =>
        {
            entity.ToTable("fact_run_weekly");

            entity.HasKey(e => new { e.DateKey, e.AthleteKey });

            entity.Property(e => e.DateKey)
                .HasColumnName("date_key")
                .HasColumnType("date");

            entity.Property(e => e.AthleteKey)
                .HasColumnName("athlete_key");

            entity.Property(e => e.DistanceKm)
                .HasColumnName("distance_km")
                .HasPrecision(10, 2);

            entity.Property(e => e.DurationMin)
                .HasColumnName("duration_min")
                .HasPrecision(10, 2);

            entity.Property(e => e.PaceMinPerKm)
                .HasColumnName("pace_min_per_km")
                .HasPrecision(10, 2);

            entity.Property(e => e.Load7dKm)
                .HasColumnName("load_7d_km")
                .HasPrecision(10, 2);

            entity.Property(e => e.Load28dKm)
                .HasColumnName("load_28d_km")
                .HasPrecision(10, 2);

            entity.Property(e => e.AcuteChronicRatio)
                .HasColumnName("acute_chronic_ratio")
                .HasPrecision(6, 3);

            entity.Property(e => e.ZeroDistanceFlag)
                .HasColumnName("zero_distance_flag")
                .HasDefaultValue(false);

            entity.HasIndex(e => e.AthleteKey)
                .HasDatabaseName("ix_fact_run_weekly_athlete");

            entity.HasOne<DimDate>()
                .WithMany()
                .HasForeignKey(e => e.DateKey)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<DimAthlete>()
                .WithMany()
                .HasForeignKey(e => e.AthleteKey)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

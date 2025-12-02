using System;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
partial class ApplicationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("backend.Models.BridgeAthleteMajor", b =>
            {
                b.Property<int>("AthleteKey")
                    .HasColumnType("integer")
                    .HasColumnName("athlete_key");

                b.Property<int>("MajorKey")
                    .HasColumnType("integer")
                    .HasColumnName("major_key");

                b.HasKey("AthleteKey", "MajorKey");

                b.HasIndex("MajorKey");

                b.ToTable("bridge_athlete_major", (string)null);
            });

        modelBuilder.Entity("backend.Models.DimAgeGroup", b =>
            {
                b.Property<int>("AgeGroupKey")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasColumnName("age_group_key");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("AgeGroupKey"));

                b.Property<string>("AgeGroupLabel")
                    .IsRequired()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)")
                    .HasColumnName("age_group_label");

                b.Property<int?>("MaxAge")
                    .HasColumnType("integer")
                    .HasColumnName("max_age");

                b.Property<int?>("MinAge")
                    .HasColumnType("integer")
                    .HasColumnName("min_age");

                b.HasKey("AgeGroupKey");

                b.HasIndex("AgeGroupLabel")
                    .IsUnique();

                b.ToTable("dim_age_group", (string)null);
            });

        modelBuilder.Entity("backend.Models.DimAthlete", b =>
            {
                b.Property<int>("AthleteKey")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasColumnName("athlete_key");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("AthleteKey"));

                b.Property<int?>("AgeGroupKey")
                    .HasColumnType("integer")
                    .HasColumnName("age_group_key");

                b.Property<int>("AthleteIdSource")
                    .HasColumnType("integer")
                    .HasColumnName("athlete_id_source");

                b.Property<int?>("CountryKey")
                    .HasColumnType("integer")
                    .HasColumnName("country_key");

                b.Property<DateOnly?>("FirstSeenWeek")
                    .HasColumnType("date")
                    .HasColumnName("first_seen_week");

                b.Property<int?>("GenderKey")
                    .HasColumnType("integer")
                    .HasColumnName("gender_key");

                b.Property<DateOnly?>("LastSeenWeek")
                    .HasColumnType("date")
                    .HasColumnName("last_seen_week");

                b.HasKey("AthleteKey");

                b.HasIndex("AgeGroupKey");

                b.HasIndex("AthleteIdSource")
                    .IsUnique();

                b.HasIndex("CountryKey");

                b.HasIndex("GenderKey");

                b.ToTable("dim_athlete", (string)null);
            });

        modelBuilder.Entity("backend.Models.DimCountry", b =>
            {
                b.Property<int>("CountryKey")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasColumnName("country_key");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("CountryKey"));

                b.Property<string>("CountryName")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)")
                    .HasColumnName("country_name");

                b.Property<string>("IsoCode")
                    .HasMaxLength(16)
                    .HasColumnType("character varying(16)")
                    .HasColumnName("iso_code");

                b.Property<string>("Region")
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)")
                    .HasColumnName("region");

                b.HasKey("CountryKey");

                b.HasIndex("CountryName")
                    .IsUnique();

                b.ToTable("dim_country", (string)null);
            });

        modelBuilder.Entity("backend.Models.DimDate", b =>
            {
                b.Property<DateOnly>("DateKey")
                    .HasColumnType("date")
                    .HasColumnName("date_key");

                b.Property<int>("IsoWeek")
                    .HasColumnType("integer")
                    .HasColumnName("iso_week");

                b.Property<int>("IsoYear")
                    .HasColumnType("integer")
                    .HasColumnName("iso_year");

                b.Property<int>("Month")
                    .HasColumnType("integer")
                    .HasColumnName("month");

                b.Property<int>("Quarter")
                    .HasColumnType("integer")
                    .HasColumnName("quarter");

                b.Property<DateOnly>("WeekStartDate")
                    .HasColumnType("date")
                    .HasColumnName("week_start_date");

                b.HasKey("DateKey");

                b.ToTable("dim_date", (string)null);
            });

        modelBuilder.Entity("backend.Models.DimGender", b =>
            {
                b.Property<int>("GenderKey")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasColumnName("gender_key");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("GenderKey"));

                b.Property<string>("GenderCode")
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnType("character varying(16)")
                    .HasColumnName("gender_code");

                b.Property<string>("GenderLabel")
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)")
                    .HasColumnName("gender_label");

                b.HasKey("GenderKey");

                b.HasIndex("GenderCode")
                    .IsUnique();

                b.ToTable("dim_gender", (string)null);
            });

        modelBuilder.Entity("backend.Models.DimMajor", b =>
            {
                b.Property<int>("MajorKey")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasColumnName("major_key");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("MajorKey"));

                b.Property<string>("MajorName")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)")
                    .HasColumnName("major_name");

                b.Property<int?>("MajorYear")
                    .HasColumnType("integer")
                    .HasColumnName("major_year");

                b.HasKey("MajorKey");

                b.HasIndex("MajorName")
                    .IsUnique();

                b.ToTable("dim_major", (string)null);
            });

        modelBuilder.Entity("backend.Models.FactRunWeekly", b =>
            {
                b.Property<DateOnly>("DateKey")
                    .HasColumnType("date")
                    .HasColumnName("date_key");

                b.Property<int>("AthleteKey")
                    .HasColumnType("integer")
                    .HasColumnName("athlete_key");

                b.Property<decimal?>("AcuteChronicRatio")
                    .HasPrecision(6, 3)
                    .HasColumnType("numeric(6,3)")
                    .HasColumnName("acute_chronic_ratio");

                b.Property<decimal?>("DistanceKm")
                    .HasPrecision(10, 2)
                    .HasColumnType("numeric(10,2)")
                    .HasColumnName("distance_km");

                b.Property<decimal?>("DurationMin")
                    .HasPrecision(10, 2)
                    .HasColumnType("numeric(10,2)")
                    .HasColumnName("duration_min");

                b.Property<decimal?>("Load28dKm")
                    .HasPrecision(10, 2)
                    .HasColumnType("numeric(10,2)")
                    .HasColumnName("load_28d_km");

                b.Property<decimal?>("Load7dKm")
                    .HasPrecision(10, 2)
                    .HasColumnType("numeric(10,2)")
                    .HasColumnName("load_7d_km");

                b.Property<decimal?>("PaceMinPerKm")
                    .HasPrecision(10, 2)
                    .HasColumnType("numeric(10,2)")
                    .HasColumnName("pace_min_per_km");

                b.Property<bool>("ZeroDistanceFlag")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasColumnName("zero_distance_flag")
                    .HasDefaultValue(false);

                b.HasKey("DateKey", "AthleteKey");

                b.HasIndex("AthleteKey")
                    .HasDatabaseName("ix_fact_run_weekly_athlete");

                b.ToTable("fact_run_weekly", (string)null);
            });

        modelBuilder.Entity("backend.Models.StagingIngestLog", b =>
            {
                b.Property<long>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("bigint")
                    .HasColumnName("id");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                b.Property<string>("Checksum")
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)")
                    .HasColumnName("checksum");

                b.Property<DateTime>("LoadTimestampUtc")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("load_timestamp_utc")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<string>("Notes")
                    .HasColumnType("text")
                    .HasColumnName("notes");

                b.Property<int?>("RowCount")
                    .HasColumnType("integer")
                    .HasColumnName("row_count");

                b.Property<string>("SourceName")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)")
                    .HasColumnName("source_name");

                b.Property<string>("Status")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)")
                    .HasColumnName("status")
                    .HasDefaultValue("pending");

                b.HasKey("Id");

                b.ToTable("stg_ingest_log", (string)null);
            });

        modelBuilder.Entity("backend.Models.StagingRunRaw", b =>
            {
                b.Property<long>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("bigint")
                    .HasColumnName("id");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                b.Property<string>("AgeGroupRaw")
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)")
                    .HasColumnName("age_group_raw");

                b.Property<int>("AthleteIdSource")
                    .HasColumnType("integer")
                    .HasColumnName("athlete_id_source");

                b.Property<string>("CountryRaw")
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)")
                    .HasColumnName("country_raw");

                b.Property<DateTime>("CreatedAtUtc")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at_utc")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.Property<decimal?>("DistanceKm")
                    .HasPrecision(10, 2)
                    .HasColumnType("numeric(10,2)")
                    .HasColumnName("distance_km");

                b.Property<decimal?>("DurationMin")
                    .HasPrecision(10, 2)
                    .HasColumnType("numeric(10,2)")
                    .HasColumnName("duration_min");

                b.Property<string>("GenderRaw")
                    .HasMaxLength(16)
                    .HasColumnType("character varying(16)")
                    .HasColumnName("gender_raw");

                b.Property<long?>("IngestBatchId")
                    .HasColumnType("bigint")
                    .HasColumnName("ingest_batch_id");

                b.Property<string>("MajorsRaw")
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)")
                    .HasColumnName("majors_raw");

                b.Property<DateOnly>("RunDate")
                    .HasColumnType("date")
                    .HasColumnName("run_date");

                b.HasKey("Id");

                b.HasIndex("IngestBatchId");

                b.ToTable("stg_runs_raw", (string)null);
            });

        modelBuilder.Entity("backend.Models.BridgeAthleteMajor", b =>
            {
                b.HasOne("backend.Models.DimAthlete", null)
                    .WithMany()
                    .HasForeignKey("AthleteKey")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("backend.Models.DimMajor", null)
                    .WithMany()
                    .HasForeignKey("MajorKey")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

        modelBuilder.Entity("backend.Models.DimAthlete", b =>
            {
                b.HasOne("backend.Models.DimAgeGroup", null)
                    .WithMany()
                    .HasForeignKey("AgeGroupKey")
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne("backend.Models.DimCountry", null)
                    .WithMany()
                    .HasForeignKey("CountryKey")
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne("backend.Models.DimGender", null)
                    .WithMany()
                    .HasForeignKey("GenderKey")
                    .OnDelete(DeleteBehavior.Restrict);
            });

        modelBuilder.Entity("backend.Models.FactRunWeekly", b =>
            {
                b.HasOne("backend.Models.DimAthlete", null)
                    .WithMany()
                    .HasForeignKey("AthleteKey")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("backend.Models.DimDate", null)
                    .WithMany()
                    .HasForeignKey("DateKey")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

        modelBuilder.Entity("backend.Models.StagingRunRaw", b =>
            {
                b.HasOne("backend.Models.StagingIngestLog", null)
                    .WithMany()
                    .HasForeignKey("IngestBatchId")
                    .OnDelete(DeleteBehavior.SetNull);
            });
#pragma warning restore 612, 618
    }
}

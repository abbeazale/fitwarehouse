using System;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20250303120000_StarSchemaInitial")]
public partial class StarSchemaInitial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "dim_age_group",
            columns: table => new
            {
                age_group_key = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                age_group_label = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                min_age = table.Column<int>(type: "integer", nullable: true),
                max_age = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_dim_age_group", x => x.age_group_key);
            });

        migrationBuilder.CreateTable(
            name: "dim_country",
            columns: table => new
            {
                country_key = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                country_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                iso_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                region = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_dim_country", x => x.country_key);
            });

        migrationBuilder.CreateTable(
            name: "dim_date",
            columns: table => new
            {
                date_key = table.Column<DateOnly>(type: "date", nullable: false),
                iso_year = table.Column<int>(type: "integer", nullable: false),
                iso_week = table.Column<int>(type: "integer", nullable: false),
                month = table.Column<int>(type: "integer", nullable: false),
                quarter = table.Column<int>(type: "integer", nullable: false),
                week_start_date = table.Column<DateOnly>(type: "date", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_dim_date", x => x.date_key);
            });

        migrationBuilder.CreateTable(
            name: "dim_gender",
            columns: table => new
            {
                gender_key = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                gender_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                gender_label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_dim_gender", x => x.gender_key);
            });

        migrationBuilder.CreateTable(
            name: "dim_major",
            columns: table => new
            {
                major_key = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                major_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                major_year = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_dim_major", x => x.major_key);
            });

        migrationBuilder.CreateTable(
            name: "stg_ingest_log",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                source_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                load_timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                row_count = table.Column<int>(type: "integer", nullable: true),
                checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "pending"),
                notes = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stg_ingest_log", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "stg_runs_raw",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                run_date = table.Column<DateOnly>(type: "date", nullable: false),
                athlete_id_source = table.Column<int>(type: "integer", nullable: false),
                distance_km = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                duration_min = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                gender_raw = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                age_group_raw = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                country_raw = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                majors_raw = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ingest_batch_id = table.Column<long>(type: "bigint", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stg_runs_raw", x => x.id);
                table.ForeignKey(
                    name: "FK_stg_runs_raw_stg_ingest_log_ingest_batch_id",
                    column: x => x.ingest_batch_id,
                    principalTable: "stg_ingest_log",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "dim_athlete",
            columns: table => new
            {
                athlete_key = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                athlete_id_source = table.Column<int>(type: "integer", nullable: false),
                gender_key = table.Column<int>(type: "integer", nullable: true),
                age_group_key = table.Column<int>(type: "integer", nullable: true),
                country_key = table.Column<int>(type: "integer", nullable: true),
                first_seen_week = table.Column<DateOnly>(type: "date", nullable: true),
                last_seen_week = table.Column<DateOnly>(type: "date", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_dim_athlete", x => x.athlete_key);
                table.ForeignKey(
                    name: "FK_dim_athlete_dim_age_group_age_group_key",
                    column: x => x.age_group_key,
                    principalTable: "dim_age_group",
                    principalColumn: "age_group_key",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_dim_athlete_dim_country_country_key",
                    column: x => x.country_key,
                    principalTable: "dim_country",
                    principalColumn: "country_key",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_dim_athlete_dim_gender_gender_key",
                    column: x => x.gender_key,
                    principalTable: "dim_gender",
                    principalColumn: "gender_key",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "bridge_athlete_major",
            columns: table => new
            {
                athlete_key = table.Column<int>(type: "integer", nullable: false),
                major_key = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bridge_athlete_major", x => new { x.athlete_key, x.major_key });
                table.ForeignKey(
                    name: "FK_bridge_athlete_major_dim_athlete_athlete_key",
                    column: x => x.athlete_key,
                    principalTable: "dim_athlete",
                    principalColumn: "athlete_key",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_bridge_athlete_major_dim_major_major_key",
                    column: x => x.major_key,
                    principalTable: "dim_major",
                    principalColumn: "major_key",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "fact_run_weekly",
            columns: table => new
            {
                date_key = table.Column<DateOnly>(type: "date", nullable: false),
                athlete_key = table.Column<int>(type: "integer", nullable: false),
                distance_km = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                duration_min = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                pace_min_per_km = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                load_7d_km = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                load_28d_km = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                acute_chronic_ratio = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                zero_distance_flag = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fact_run_weekly", x => new { x.date_key, x.athlete_key });
                table.ForeignKey(
                    name: "FK_fact_run_weekly_dim_athlete_athlete_key",
                    column: x => x.athlete_key,
                    principalTable: "dim_athlete",
                    principalColumn: "athlete_key",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_fact_run_weekly_dim_date_date_key",
                    column: x => x.date_key,
                    principalTable: "dim_date",
                    principalColumn: "date_key",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bridge_athlete_major_major_key",
            table: "bridge_athlete_major",
            column: "major_key");

        migrationBuilder.CreateIndex(
            name: "IX_dim_athlete_age_group_key",
            table: "dim_athlete",
            column: "age_group_key");

        migrationBuilder.CreateIndex(
            name: "IX_dim_athlete_athlete_id_source",
            table: "dim_athlete",
            column: "athlete_id_source",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_dim_athlete_country_key",
            table: "dim_athlete",
            column: "country_key");

        migrationBuilder.CreateIndex(
            name: "IX_dim_athlete_gender_key",
            table: "dim_athlete",
            column: "gender_key");

        migrationBuilder.CreateIndex(
            name: "ix_fact_run_weekly_athlete",
            table: "fact_run_weekly",
            column: "athlete_key");

        migrationBuilder.CreateIndex(
            name: "IX_stg_runs_raw_ingest_batch_id",
            table: "stg_runs_raw",
            column: "ingest_batch_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "bridge_athlete_major");

        migrationBuilder.DropTable(
            name: "fact_run_weekly");

        migrationBuilder.DropTable(
            name: "stg_runs_raw");

        migrationBuilder.DropTable(
            name: "dim_major");

        migrationBuilder.DropTable(
            name: "dim_athlete");

        migrationBuilder.DropTable(
            name: "dim_date");

        migrationBuilder.DropTable(
            name: "stg_ingest_log");

        migrationBuilder.DropTable(
            name: "dim_age_group");

        migrationBuilder.DropTable(
            name: "dim_country");

        migrationBuilder.DropTable(
            name: "dim_gender");
    }
}

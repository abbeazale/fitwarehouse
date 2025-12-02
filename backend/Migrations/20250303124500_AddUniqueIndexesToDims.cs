using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20250303124500_AddUniqueIndexesToDims")]
public partial class AddUniqueIndexesToDims : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "UX_dim_gender_gender_code",
            table: "dim_gender",
            column: "gender_code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_dim_age_group_age_group_label",
            table: "dim_age_group",
            column: "age_group_label",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_dim_country_country_name",
            table: "dim_country",
            column: "country_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_dim_major_major_name",
            table: "dim_major",
            column: "major_name",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_dim_gender_gender_code",
            table: "dim_gender");

        migrationBuilder.DropIndex(
            name: "UX_dim_age_group_age_group_label",
            table: "dim_age_group");

        migrationBuilder.DropIndex(
            name: "UX_dim_country_country_name",
            table: "dim_country");

        migrationBuilder.DropIndex(
            name: "UX_dim_major_major_name",
            table: "dim_major");
    }
}

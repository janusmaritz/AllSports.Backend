using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllSports.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDartBrandAndWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DartBrand",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DartWeight",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DartBrand",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "DartWeight",
                table: "Players");
        }
    }
}

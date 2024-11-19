using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceMiddleware.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductSourcedata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConditionPrice",
                columns: table => new
                {
                    ConditionGroup = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SourceData",
                columns: table => new
                {
                    Vendor = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConditionPrice");

            migrationBuilder.DropTable(
                name: "SourceData");
        }
    }
}

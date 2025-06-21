using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FisFaturaAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKdvMatrah0ToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Kdv_0",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Matrah_0",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kdv_0",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Matrah_0",
                table: "Invoices");
        }
    }
}

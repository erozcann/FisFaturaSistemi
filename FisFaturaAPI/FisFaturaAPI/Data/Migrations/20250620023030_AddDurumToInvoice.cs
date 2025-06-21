using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FisFaturaAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDurumToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Durum",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Durum",
                table: "Invoices");
        }
    }
}

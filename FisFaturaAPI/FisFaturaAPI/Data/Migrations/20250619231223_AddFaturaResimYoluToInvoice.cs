using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FisFaturaAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaturaResimYoluToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaturaResimYolu",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaturaResimYolu",
                table: "Invoices");
        }
    }
}

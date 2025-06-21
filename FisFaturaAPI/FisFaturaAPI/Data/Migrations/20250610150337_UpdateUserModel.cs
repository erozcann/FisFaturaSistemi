using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FisFaturaAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_KaydedenKullaniciId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Kdv_0",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Matrah_0",
                table: "Invoices");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_KaydedenKullaniciId",
                table: "Invoices",
                column: "KaydedenKullaniciId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_KaydedenKullaniciId",
                table: "Invoices");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_KaydedenKullaniciId",
                table: "Invoices",
                column: "KaydedenKullaniciId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FisFaturaAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateReceiptTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tip",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirmaAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VergiNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FisNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IcerikTuru = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OdemeSekli = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GelirGider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KdvOranlariJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatrahOranlariJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FisResimYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropColumn(
                name: "Tip",
                table: "Invoices");
        }
    }
}

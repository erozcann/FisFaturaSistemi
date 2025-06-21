using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FisFaturaAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TcKimlikNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Isim = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Soyisim = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SifreHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Firms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirmaAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VergiNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EkleyenKullaniciId = table.Column<int>(type: "int", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Firms_Users_EkleyenKullaniciId",
                        column: x => x.EkleyenKullaniciId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirmaGonderenId = table.Column<int>(type: "int", nullable: true),
                    FirmaAliciId = table.Column<int>(type: "int", nullable: true),
                    FaturaNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaturaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FaturaTuru = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Senaryo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GelirGider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OdemeTuru = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IcerikTuru = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    KdvToplam = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MatrahToplam = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Kdv_0 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Kdv_1 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Kdv_8 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Kdv_10 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Kdv_18 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Kdv_20 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Matrah_0 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Matrah_1 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Matrah_8 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Matrah_10 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Matrah_18 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Matrah_20 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    KaydedenKullaniciId = table.Column<int>(type: "int", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Firms_FirmaAliciId",
                        column: x => x.FirmaAliciId,
                        principalTable: "Firms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Firms_FirmaGonderenId",
                        column: x => x.FirmaGonderenId,
                        principalTable: "Firms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Users_KaydedenKullaniciId",
                        column: x => x.KaydedenKullaniciId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Firms_EkleyenKullaniciId",
                table: "Firms",
                column: "EkleyenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FirmaAliciId",
                table: "Invoices",
                column: "FirmaAliciId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FirmaGonderenId",
                table: "Invoices",
                column: "FirmaGonderenId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_KaydedenKullaniciId",
                table: "Invoices",
                column: "KaydedenKullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Firms");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

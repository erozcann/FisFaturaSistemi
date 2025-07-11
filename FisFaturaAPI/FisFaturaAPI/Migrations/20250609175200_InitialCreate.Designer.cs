﻿// <auto-generated />
using System;
using FisFaturaAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FisFaturaAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250609175200_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("FisFaturaAPI.Models.Firm", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("EkleyenKullaniciId")
                        .HasColumnType("int");

                    b.Property<string>("FirmaAdi")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("KayitTarihi")
                        .HasColumnType("datetime2");

                    b.Property<string>("VergiNo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("EkleyenKullaniciId");

                    b.ToTable("Firms");
                });

            modelBuilder.Entity("FisFaturaAPI.Models.Invoice", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("FaturaNo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("FaturaTarihi")
                        .HasColumnType("datetime2");

                    b.Property<string>("FaturaTuru")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("FirmaAliciId")
                        .HasColumnType("int");

                    b.Property<int>("FirmaGonderenId")
                        .HasColumnType("int");

                    b.Property<string>("GelirGider")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IcerikTuru")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("KaydedenKullaniciId")
                        .HasColumnType("int");

                    b.Property<DateTime>("KayitTarihi")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("KdvToplam")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Kdv_0")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Kdv_1")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Kdv_10")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Kdv_18")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Kdv_20")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Kdv_8")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("MatrahToplam")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Matrah_0")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Matrah_1")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Matrah_10")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Matrah_18")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Matrah_20")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Matrah_8")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("OdemeTuru")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Senaryo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("ToplamTutar")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("FirmaAliciId");

                    b.HasIndex("FirmaGonderenId");

                    b.HasIndex("KaydedenKullaniciId");

                    b.ToTable("Invoices");
                });

            modelBuilder.Entity("FisFaturaAPI.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Isim")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("KayitTarihi")
                        .HasColumnType("datetime2");

                    b.Property<string>("Rol")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SifreHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Soyisim")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TcKimlikNo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Telefon")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FisFaturaAPI.Models.Firm", b =>
                {
                    b.HasOne("FisFaturaAPI.Models.User", "EkleyenKullanici")
                        .WithMany()
                        .HasForeignKey("EkleyenKullaniciId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("EkleyenKullanici");
                });

            modelBuilder.Entity("FisFaturaAPI.Models.Invoice", b =>
                {
                    b.HasOne("FisFaturaAPI.Models.Firm", "FirmaAlici")
                        .WithMany()
                        .HasForeignKey("FirmaAliciId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("FisFaturaAPI.Models.Firm", "FirmaGonderen")
                        .WithMany()
                        .HasForeignKey("FirmaGonderenId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("FisFaturaAPI.Models.User", "KaydedenKullanici")
                        .WithMany()
                        .HasForeignKey("KaydedenKullaniciId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("FirmaAlici");

                    b.Navigation("FirmaGonderen");

                    b.Navigation("KaydedenKullanici");
                });
#pragma warning restore 612, 618
        }
    }
}

using FisFaturaAPI.Models;

namespace FisFaturaAPI.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        public string? FaturaNo { get; set; }
        public DateTime? FaturaTarihi { get; set; }
        public string? FaturaTuru { get; set; }
        public string? Senaryo { get; set; }
        public string? GelirGider { get; set; }
        public string? OdemeTuru { get; set; }
        public string? IcerikTuru { get; set; }
        public decimal? ToplamTutar { get; set; }

        public decimal? KdvToplam { get; set; }
        public decimal? MatrahToplam { get; set; }

        public decimal? Kdv_0 { get; set; }
        public decimal? Matrah_0 { get; set; }
        public decimal? Kdv_1 { get; set; }
        public decimal? Kdv_8 { get; set; }
        public decimal? Kdv_10 { get; set; }
        public decimal? Kdv_18 { get; set; }
        public decimal? Kdv_20 { get; set; }

        public decimal? Matrah_1 { get; set; }
        public decimal? Matrah_8 { get; set; }
        public decimal? Matrah_10 { get; set; }
        public decimal? Matrah_18 { get; set; }
        public decimal? Matrah_20 { get; set; }

        public DateTime? KayitTarihi { get; set; }

        // FK'lar
        public int? FirmaGonderenId { get; set; }
        public Firm? FirmaGonderen { get; set; }

        public int? FirmaAliciId { get; set; }
        public Firm? FirmaAlici { get; set; }

        public int? KaydedenKullaniciId { get; set; }
        public User? KaydedenKullanici { get; set; }

        public string? FaturaResimYolu { get; set; }

        public string Durum { get; set; } = "İşlendi";

        public string? Tip { get; set; } // 'Fatura' veya 'Fiş'
    }
}

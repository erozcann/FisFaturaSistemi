namespace FisFaturaAPI.Models
{
    public class CreateFirmDto
    {
        public string FirmaAdi { get; set; } = string.Empty;
        public string VergiNo { get; set; } = string.Empty;
        public int EkleyenKullaniciId { get; set; }
    }
}

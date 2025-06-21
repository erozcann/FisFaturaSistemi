using System.Collections.Generic;

namespace FisFaturaUI.Models
{
    public class CreateInvoicePageViewModel
    {
        public OcrInvoiceViewModel OcrInvoice { get; set; } = new OcrInvoiceViewModel();
        public Invoice? Invoice { get; set; }
        public List<FirmViewModel> Firms { get; set; } = new List<FirmViewModel>();
        public List<string> FaturaTurleri { get; set; } = new List<string> { "E-Fatura/E-Arşiv", "Diğer" }; // Örnek türler
        public List<string> OdemeYontemleri { get; set; } = new List<string> { "KART", "HAVALE", "NAKİT", "ÇEK", "SENET", "DİĞER" }; // Örnek türler
        public List<string> Senaryolar { get; set; } = new List<string> { "BORÇ", "ALACAK" }; // Örnek türler
        public List<string> GelirGiderSecenekleri { get; set; } = new List<string> { "Gelir", "Gider" }; // Örnek türler
    }
} 
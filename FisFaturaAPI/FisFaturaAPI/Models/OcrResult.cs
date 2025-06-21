using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FisFaturaAPI.Models
{
    public class OcrResult
    {
        [JsonPropertyName("firma_adi")]
        public string FirmaAdi { get; set; }

        [JsonPropertyName("vergi_no")]
        public string VergiNo { get; set; }

        [JsonPropertyName("fis_no")]
        public string FisNo { get; set; }

        [JsonPropertyName("tarih")]
        public string Tarih { get; set; }

        [JsonPropertyName("toplam_tutar")]
        public string ToplamTutar { get; set; }

        [JsonPropertyName("kdv_toplam")]
        public string KdvToplam { get; set; }

        [JsonPropertyName("matrah_toplam")]
        public string MatrahToplam { get; set; }

        [JsonPropertyName("kdv_oranlari")]
        public Dictionary<string, string> KdvOranlari { get; set; }

        [JsonPropertyName("matrah_oranlari")]
        public Dictionary<string, string> MatrahOranlari { get; set; }
        
        [JsonPropertyName("raw_ocr_lines")]
        public List<string> RawOcrLines { get; set; }
    }
} 
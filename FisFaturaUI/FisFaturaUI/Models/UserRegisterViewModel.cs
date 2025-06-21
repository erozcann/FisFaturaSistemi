using System.ComponentModel.DataAnnotations;

namespace FisFaturaUI.Models
{
    public class UserRegisterViewModel
    {
        [Required(ErrorMessage = "TC Kimlik Numarası alanı zorunludur.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "TC Kimlik Numarası 11 karakter olmalıdır.")]
        public string TcKimlikNo { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "İsim alanı zorunludur.")]
        public string Isim { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Soyisim alanı zorunludur.")]
        public string Soyisim { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Telefon alanı zorunludur.")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string Telefon { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrar alanı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare("Sifre", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string SifreTekrar { get; set; } = string.Empty;

        public string Rol { get; set; } = "Standart";
    }
}

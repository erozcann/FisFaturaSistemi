using FisFaturaAPI.Data;
using FisFaturaAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace FisFaturaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private string GenerateResetToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 22);
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] UserRegisterDto model)
        {
            try
            {
                _logger.LogInformation($"Kayıt isteği alındı: {model.Email}");

                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Sifre))
                {
                    _logger.LogWarning("Email veya şifre boş");
                    return BadRequest("Email ve şifre zorunludur.");
                }

                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    _logger.LogWarning($"Email zaten kullanımda: {model.Email}");
                    return BadRequest("Bu email adresi zaten kullanılıyor.");
                }

                var user = new User
                {
                    TcKimlikNo = model.TcKimlikNo,
                    Isim = model.Isim,
                    Soyisim = model.Soyisim,
                    Email = model.Email,
                    Telefon = model.Telefon,
                    SifreHash = HashPassword(model.Sifre),
                    Rol = "Standart",
                    KayitTarihi = DateTime.Now
                };

                _logger.LogInformation($"Yeni kullanıcı oluşturuluyor: {user.Email}");

                _context.Users.Add(user);
                var result = _context.SaveChanges();

                _logger.LogInformation($"Kayıt sonucu: {result} satır etkilendi");

                if (result > 0)
                {
                    return Ok(new { id = user.Id, isim = user.Isim });
                }
                else
                {
                    _logger.LogError("Veritabanına kayıt yapılamadı");
                    return BadRequest("Kayıt işlemi başarısız oldu.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kayıt hatası: {ex.Message}");
                return BadRequest($"Kayıt işlemi başarısız: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto login)
        {
            try
            {
                _logger.LogInformation($"Giriş isteği alındı: {login.Email}");

                if (string.IsNullOrEmpty(login.Email) || string.IsNullOrEmpty(login.Sifre))
                {
                    _logger.LogWarning("Email veya şifre boş");
                    return BadRequest("Email ve şifre zorunludur.");
                }

                var hashedPassword = HashPassword(login.Sifre);
                var user = _context.Users.FirstOrDefault(u => u.Email == login.Email && u.SifreHash == hashedPassword);
                
                if (user == null)
                {
                    _logger.LogWarning($"Geçersiz giriş denemesi: {login.Email}");
                    return Unauthorized("Geçersiz email veya şifre");
                }

                _logger.LogInformation($"Başarılı giriş: {user.Email}");
                return Ok(new { id = user.Id, isim = user.Isim, soyisim = user.Soyisim });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Giriş hatası: {ex.Message}");
                return BadRequest($"Giriş işlemi başarısız: {ex.Message}");
            }
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            try
            {
                _logger.LogInformation($"Şifremi unuttum isteği alındı: {model.Email}");

                if (string.IsNullOrEmpty(model.Email))
                {
                    return BadRequest("Email adresi zorunludur.");
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Kullanıcı bulunamadı: {model.Email}");
                    return BadRequest("Bu email adresi ile kayıtlı kullanıcı bulunamadı.");
                }

                // Reset token oluştur
                var resetToken = GenerateResetToken();
                user.ResetToken = resetToken;
                user.ResetTokenExpiry = DateTime.Now.AddHours(24); // 24 saat geçerli

                _context.SaveChanges();

                _logger.LogInformation($"Şifre sıfırlama token'ı oluşturuldu: {user.Email}");

                // Şifre sıfırlama linki
                var resetLink = $"https://localhost:7025/User/ResetPassword?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(resetToken)}";

                // SMTP ayarlarını oku
                var smtpSection = _configuration.GetSection("Smtp");
                var smtpHost = smtpSection["Host"];
                var smtpPort = int.Parse(smtpSection["Port"]);
                var smtpUser = smtpSection["User"];
                var smtpPass = smtpSection["Password"];
                var smtpFrom = smtpSection["From"];
                var smtpEnableSsl = bool.Parse(smtpSection["EnableSsl"]);

                // Mail gönder
                var mail = new MailMessage();
                mail.From = new MailAddress(smtpFrom);
                mail.To.Add(user.Email);
                mail.Subject = "FisFatura Şifre Sıfırlama";
                mail.Body = $"<p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:</p><p><a href='{resetLink}'>{resetLink}</a></p><p>Bu bağlantı 24 saat geçerlidir.</p>";
                mail.IsBodyHtml = true;

                using (var smtp = new SmtpClient(smtpHost, smtpPort))
                {
                    smtp.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                    smtp.EnableSsl = smtpEnableSsl;
                    smtp.Send(mail);
                }

                return Ok(new { 
                    message = "Şifre sıfırlama bağlantısı email adresinize gönderildi. Lütfen e-postanızı kontrol edin." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Şifremi unuttum hatası: {ex.Message}");
                return BadRequest($"İşlem başarısız: {ex.Message}");
            }
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordDto model)
        {
            try
            {
                _logger.LogInformation($"Şifre sıfırlama isteği alındı: {model.Email}");

                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.Token))
                {
                    return BadRequest("Email, yeni şifre ve token zorunludur.");
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Kullanıcı bulunamadı: {model.Email}");
                    return BadRequest("Kullanıcı bulunamadı.");
                }

                if (user.ResetToken != model.Token)
                {
                    _logger.LogWarning($"Geçersiz reset token: {model.Email}");
                    return BadRequest("Geçersiz şifre sıfırlama bağlantısı.");
                }

                if (user.ResetTokenExpiry < DateTime.Now)
                {
                    _logger.LogWarning($"Süresi dolmuş reset token: {model.Email}");
                    return BadRequest("Şifre sıfırlama bağlantısının süresi dolmuş.");
                }

                // Şifreyi güncelle
                user.SifreHash = HashPassword(model.NewPassword);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;

                _context.SaveChanges();

                _logger.LogInformation($"Şifre başarıyla sıfırlandı: {user.Email}");

                return Ok(new { message = "Şifreniz başarıyla değiştirildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Şifre sıfırlama hatası: {ex.Message}");
                return BadRequest($"İşlem başarısız: {ex.Message}");
            }
        }

        [HttpPut("update/{id}")]
        public IActionResult UpdateUser(int id, [FromBody] UserRegisterDto model)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            user.TcKimlikNo = model.TcKimlikNo;
            user.Isim = model.Isim;
            user.Soyisim = model.Soyisim;
            user.Email = model.Email;
            user.Telefon = model.Telefon;
            if (!string.IsNullOrEmpty(model.Sifre))
                user.SifreHash = HashPassword(model.Sifre);

            _context.SaveChanges();
            _logger.LogInformation($"Güncellenen kullanıcı: {JsonSerializer.Serialize(user)}");
            return Ok(new { message = "Kullanıcı güncellendi." });
        }

        [HttpGet("all")]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users
                .Select(u => new {
                    u.Id,
                    u.TcKimlikNo,
                    u.Isim,
                    u.Soyisim,
                    u.Email,
                    u.Telefon,
                    u.Rol,
                    u.KayitTarihi
                }).ToList();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _context.Users
                .Where(u => u.Id == id)
                .Select(u => new {
                    u.Id,
                    u.TcKimlikNo,
                    u.Isim,
                    u.Soyisim,
                    u.Email,
                    u.Telefon,
                    u.Rol,
                    u.KayitTarihi
                }).FirstOrDefault();
            if (user == null) return NotFound();
            return Ok(user);
        }
    }

    public class UserRegisterDto
    {
        public string TcKimlikNo { get; set; } = string.Empty;
        public string Isim { get; set; } = string.Empty;
        public string Soyisim { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string? Sifre { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}

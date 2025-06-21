using FisFaturaUI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace FisFaturaUI.Controllers
{
    public class UserController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            ILogger<UserController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(UserRegisterViewModel model)
        {
            try
            {
                _logger.LogInformation("Kayıt isteği alındı");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validasyon hatası");
                    return View(model);
                }

                var client = _httpClientFactory.CreateClient("FisFaturaAPI");
                _logger.LogInformation($"API isteği gönderiliyor: api/User/register");

                var response = await client.PostAsJsonAsync("api/User/register", model);
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API yanıtı: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Kayıt başarılı");
                    TempData["Success"] = "Kayıt başarılı. Giriş yapabilirsiniz.";
                    return RedirectToAction("Login");
                }

                _logger.LogError($"Kayıt hatası: {responseContent}");
                ViewBag.Error = responseContent;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kayıt işlemi sırasında hata: {ex.Message}");
                ViewBag.Error = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View(model);
            }
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(UserLoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var client = _httpClientFactory.CreateClient("FisFaturaAPI");
                var response = await client.PostAsJsonAsync("api/User/login", model);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Geçersiz email veya şifre.";
                    return View(model);
                }

                var user = JsonConvert.DeserializeObject<dynamic>(responseContent);
                HttpContext.Session.SetInt32("UserId", (int)user.id);
                HttpContext.Session.SetString("Isim", (string)user.isim);
                HttpContext.Session.SetString("UserName", (string)user.isim + " " + (string)user.soyisim);
                HttpContext.Session.SetString("LastLogin", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

                TempData["Success"] = "Başarıyla giriş yaptınız!";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View(model);
            }
        }

        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var client = _httpClientFactory.CreateClient("FisFaturaAPI");
                var response = await client.PostAsJsonAsync("api/User/forgot-password", model);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Şifre sıfırlama bağlantısı email adresinize gönderildi.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.Error = "Bu email adresi ile kayıtlı kullanıcı bulunamadı.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View(model);
            }
        }

        public IActionResult ResetPassword(string email, string token)
        {
            var model = new ResetPasswordViewModel
            {
                Email = email ?? "",
                Token = token ?? ""
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var client = _httpClientFactory.CreateClient("FisFaturaAPI");
                var response = await client.PostAsJsonAsync("api/User/reset-password", model);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Şifreniz başarıyla değiştirildi. Yeni şifrenizle giriş yapabilirsiniz.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.Error = "Şifre sıfırlama işlemi başarısız oldu. Lütfen tekrar deneyin.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View(model);
            }
        }

        public IActionResult Dashboard()
        {
            var id = HttpContext.Session.GetInt32("UserId");
            if (id == null) return RedirectToAction("Login");

            ViewBag.Isim = HttpContext.Session.GetString("Isim");
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Başarıyla çıkış yaptınız.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.GetAsync($"api/User/all");
            var users = JsonConvert.DeserializeObject<List<dynamic>>(await response.Content.ReadAsStringAsync());
            var user = users.FirstOrDefault(u => (int)u.id == userId);
            if (user == null) return RedirectToAction("Login");
            var model = new UserRegisterViewModel {
                TcKimlikNo = user.tcKimlikNo,
                Isim = user.isim,
                Soyisim = user.soyisim,
                Email = user.email,
                Telefon = user.telefon,
                Rol = user.rol
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(UserRegisterViewModel model)
        {
            _logger.LogInformation($"Profile POST action çalıştı. Session UserId: {HttpContext.Session.GetInt32("UserId")}");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        _logger.LogError($"ModelState Error - {key}: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.PutAsJsonAsync($"api/User/update/{userId}", model);
            if (response.IsSuccessStatusCode)
            {
                ViewBag.Success = "Bilgileriniz güncellendi.";
                // Güncel bilgileri tekrar çek
                var getResponse = await client.GetAsync($"api/User/{userId}");
                var userJson = await getResponse.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<dynamic>(userJson);
                model.TcKimlikNo = user.tcKimlikNo;
                model.Isim = user.isim;
                model.Soyisim = user.soyisim;
                model.Email = user.email;
                model.Telefon = user.telefon;
                model.Rol = user.rol;
            }
            else
            {
                ViewBag.Error = "Güncelleme başarısız.";
            }
            return View(model);
        }

        public async Task<IActionResult> Users()
        {
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.GetAsync("api/User/all");
            var users = JsonConvert.DeserializeObject<List<dynamic>>(await response.Content.ReadAsStringAsync());
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(UserRegisterViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction("Users");
            var client = _httpClientFactory.CreateClient("FisFaturaAPI");
            var response = await client.PostAsJsonAsync("api/User/register", model);
            return RedirectToAction("Users");
        }
    }
}

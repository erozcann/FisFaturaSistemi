using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FisFaturaUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public HomeController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            var galleryPath = Path.Combine(_env.WebRootPath, "images", "gallery");
            var imageFiles = new List<string>();

            if (Directory.Exists(galleryPath))
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                imageFiles = Directory.GetFiles(galleryPath)
                                      .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
                                      .Select(file => "/images/gallery/" + Path.GetFileName(file))
                                      .ToList();
            }

            ViewBag.GalleryImages = imageFiles;
            return View();
        }
    }
} 
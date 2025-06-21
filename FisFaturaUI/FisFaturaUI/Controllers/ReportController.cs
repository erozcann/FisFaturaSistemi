using Microsoft.AspNetCore.Mvc;

namespace FisFaturaUI.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult ExcelReports()
        {
            return View();
        }
    }
} 
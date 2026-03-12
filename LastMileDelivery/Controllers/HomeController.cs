using LastMileDelivery.Data;
using LastMileDelivery.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LastMileDelivery.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;


        // ================= HOME PAGE =================
        // This is the landing page for the application. It can be accessed by anyone, regardless of login status.
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult Feature()
        {
            return View();
        }

        
      

       
        
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

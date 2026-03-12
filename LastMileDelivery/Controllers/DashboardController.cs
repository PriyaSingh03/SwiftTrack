using Microsoft.AspNetCore.Mvc;

namespace LastMileDelivery.Controllers
{
    public class DashboardController : Controller
    {
        // ---------------- ADMIN DASHBOARD ----------------
        public IActionResult AdminDashboard()
        {
            if (!IsLoggedIn() || !IsRole("ADMIN"))
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // ---------------- AGENT DASHBOARD ----------------
        public IActionResult AgentDashboard()
        {
            if (!IsLoggedIn() || !IsRole("DELIVERY_AGENT"))
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // ---------------- CUSTOMER DASHBOARD ----------------
        public IActionResult CustomerDashboard()
        {
            if (!IsLoggedIn() || !IsRole("CUSTOMER"))
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // ---------------- HELPERS ----------------
        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId") != null;
        }

        private bool IsRole(string role)
        {
            return HttpContext.Session.GetString("Role") == role;
        }
    }
}
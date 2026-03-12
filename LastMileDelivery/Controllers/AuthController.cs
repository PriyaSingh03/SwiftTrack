using LastMileDelivery.Data;
using LastMileDelivery.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Json; // Required for PostAsJsonAsync and ReadFromJsonAsync

namespace LastMileDelivery.Controllers
{
    public class AuthController : Controller
    {
        //talk to the Database directly
        private readonly ApplicationDbContext _context;
        //It manages the 'connection life' efficiently so the server doesn't run out of sockets if many people try to log in at the same time.
        private readonly IHttpClientFactory _httpClientFactory;

        // Constructor injecting both DB Context (for Register) and Http Client (for Login API)
        public AuthController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // ===================== WEB LOGIN (GET) =====================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ===================== WEB LOGIN (POST via API) =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, string role)
        {
            // 1. Basic Validation
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(role))
            {
                TempData["Error"] = "Please fill in all fields.";
                return View();
            }

            // ===================== PASTE THE ADMIN BLOCK HERE =====================
            if (Email.ToLower() == "priya123@gmail.com" && Password == "priya@123")
            {
                // Storing admin data in variables
                int adminId = 1;
                string adminName = "Priya Singh";
                string adminEmail = "priya123@gmail.com";
                string adminRole = "ADMIN";

                // Saving variables into Session
                HttpContext.Session.SetInt32("UserId", adminId);
                HttpContext.Session.SetString("Username", adminName);
                HttpContext.Session.SetString("Email", adminEmail);
                HttpContext.Session.SetString("Role", adminRole);
                
                return RedirectToAction("AdminDashboard", "Admin");
                
            }
            // =================================================================

            try
            {
                var client = _httpClientFactory.CreateClient();
                var loginData = new { Email = Email, Password = Password, Role = role };

                // This targets your API project - make sure it is running!
                var response = await client.PostAsJsonAsync("https://localhost:7270/api/authapi/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<User>();
                    if (user != null)
                    {
                        // Once the login is successful, the controller uses Sessions
                        HttpContext.Session.SetInt32("UserId", user.UserId);
                        HttpContext.Session.SetString("Username", user.Username ?? "User");
                        HttpContext.Session.SetString("Role", user.Role ?? "");

                        return user.Role switch
                        {
                            "ADMIN" => RedirectToAction("AdminDashboard", "ADMIN"),
                            "CUSTOMER" => RedirectToAction("CustomerDashboard", "CUSTOMER"),
                            "DELIVERY_AGENT" => RedirectToAction("Dashboard", "Agent"),
                            _ => RedirectToAction("Index", "Home")
                        };
                    }
                }

                TempData["Error"] = "Invalid credentials or unauthorized role.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Could not connect to the Login Service. Hardcoded admin still works though!";
            }

            return View();
        }

        // ===================== WEB REGISTER (POST) =====================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string Username, string Email, string Password, string ConfirmPassword, string role, string PhoneNumber)
        {
            if (role == "ADMIN")
            {
                TempData["Error"] = "Admin registration is not allowed.";
                return View();
            }
            // It checks that the Password and ConfirmPassword are identical
            if (Password != ConfirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return View();
            }

            // It checks the database to make sure the email isn't already in use.
            if (_context.Users.Any(u => u.Email == Email))
            {
                TempData["Error"] = "Email already registered.";
                return View();
            }

            // Map inputs to Database standard values
            string finalRole = role switch
            {
                "Customer" => "CUSTOMER",
                "CUSTOMER" => "CUSTOMER",
                "Agent" => "DELIVERY_AGENT",
                "DELIVERY_AGENT" => "DELIVERY_AGENT",
                _ => "CUSTOMER"
            };

            User user = new User
            {
                Username = Username,
                Email = Email.Trim(),
                Password = HashPassword(Password),
                Role = finalRole,
                PhoneNumber = PhoneNumber,
                Status = "Active",
                Deliveries = 0
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "Registration successful! Please login.";
            //Once finished, it doesn't just stay on the blank page; it automatically sends the user to the Login page
            return RedirectToAction("Login");
        }

        // ===================== LOGOUT =====================

        public IActionResult Logout()
        {
            //It wipes out the UserID and Role, making it safe to leave the computer.
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ===================== HELPER =====================
        // SHA256 Encryption to turn the password into a long
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
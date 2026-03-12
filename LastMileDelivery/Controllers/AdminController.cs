using LastMileDelivery.Data;
using LastMileDelivery.Models;
using LastMileDelivery.ViewModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net.Http;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LastMileDelivery.Controllers
{
    public class AdminController : Controller
    {
        // This is our database connection.
        private readonly ApplicationDbContext _context;

        //It tells the app where the physical files are stored on the server.
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;

        
        //provide me with a database context and a hosting environment.
        public AdminController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment, IConfiguration configuration)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
        }


        //gather statistics from your database and pack them into a "ViewModel to be displayed on admin page
        public IActionResult AdminDashboard()
        {
            var total = _context.Deliveries.AsNoTracking().Count();

            //Calculate Percentages (avoid division by zero): divides that count by the total and multiplies by 100 to get a percentage 
            double GetPercentage(string status) =>
                total > 0 ? (double)_context.Deliveries.Count(d => d.Status == status) / total * 100 : 0;


            //The code creates a new object called model and fills it
            var model = new AdminDashboardVM
            {
                // total deliveries exist in the system
                TotalDeliveries = total,
                ActiveAgents = _context.Users.Count(u => u.Role == "DELIVERY_AGENT"),
                TotalCustomers = _context.Users.Count(u => u.Role == "Customer"),
                PendingDeliveries = _context.Deliveries.Count(d => d.Status == "DELIVERED"),

                // Performance Calculations
                SuccessRate = GetPercentage("DELIVERED"),
                AvgDeliveryTime = 45, // Note: This usually requires a 'CompletionDate' column to calculate actual duration

                // Distribution Calculations
                DeliveredPercentage = GetPercentage("DELIVERED"),
                InTransitPercentage = GetPercentage("IN_TRANSIT"),
                PendingPercentage = GetPercentage("ASSIGNED"),
                CancelledPercentage = GetPercentage("CANCELLED")
            };
            //This sends all that gathered data to the View
            return View(model);
        }

        // Admin can see the customer list with details of the customer
        public IActionResult AdminCustomer()
        {
            if (HttpContext.Session.GetString("Role") != "ADMIN")
                return RedirectToAction("Login", "Auth");
            var customer = _context.Users.Where(u => u.Role == "Customer").ToList();

            // 2. For each customer, count their orders from the Deliveries table
            foreach (var c in customer)
            {
                c.Deliveries = _context.Deliveries
                    .Count(d => d.CustomerId == c.UserId);
            }

            return View(customer);
        }

        // ===================== GET : ADD Customer =====================
        [HttpGet]
        public IActionResult AddCustomer()
        {
            if (HttpContext.Session.GetString("Role") != "ADMIN")
                return RedirectToAction("Login", "Auth");
            return View();
        }

        // ===================== POST : ADD Customer =====================
        [HttpPost]
        public IActionResult AddCustomer(string Username, string Email, string Phone)
        {
            if (HttpContext.Session.GetString("Role") != "ADMIN")
                return RedirectToAction("Login", "Auth");

            //// Check if Email OR Phone already exists
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == Email || u.PhoneNumber == Phone);
            //bool userExists = _context.Users.Any(u => u.Email == Email);
            if (existingUser != null)
            {
                // Store error message in TempData to be read by the JavaScript alert
                if (existingUser.Email == Email)
                {
                    TempData["UserError"] = "User already exists with this email!";
                }

                else if (existingUser.PhoneNumber == Phone)
                {
                    TempData["UserError"] = "This phone number is already registered to another account!";
                }
                return View();
            }
            string usernamePart = Username.Length >= 3 ? Username.Substring(0, 3) : Username;
            string phonePart = Phone.Length >= 4 ? Phone.Substring(Phone.Length - 4) : Phone;
            string rawPassword = usernamePart + phonePart;
            string hashedPassword = HashPassword(rawPassword);
            var user = new User
            {
                Username = Username,
                Email = Email,
                Role = "CUSTOMER",
                Password = hashedPassword,
                PhoneNumber = Phone,
                Status = "Active",
                Deliveries = 0
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            return RedirectToAction("AdminCustomer");
        }

        // ===================== POST : DeleteCustomer =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCustomer(string username)
        {
            if (HttpContext.Session.GetString("Role") != "ADMIN")
                return RedirectToAction("Login", "Auth");
            var customer = _context.Users
                .FirstOrDefault(u => u.Username == username && u.Role == "CUSTOMER");
            if (customer != null)
            {
                // 1. Find and remove linked Invoices first
                var relatedInvoices = _context.Invoices.Where(i => i.CustomerId == customer.UserId);
                _context.Invoices.RemoveRange(relatedInvoices);
                // 2. Now delete the customer
                _context.Users.Remove(customer);
                _context.SaveChanges();
            }
            return RedirectToAction("AdminCustomer");
        }

        // ===================== Password Hashing =====================
        private string HashPassword(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        //  ===================== Admin can see the deliveries and the details of the delivery =====================
        public IActionResult AdminDeliveries()
        {
            //If the person logged in is NOT an Admin, kick them out.
            if (HttpContext.Session.GetString("Role") != "ADMIN")
                return RedirectToAction("Login", "Auth");

            // Fetch deliveries with Customer, Agent AND ProofOfDelivery order them according to the newest deliveries appear at the very top
            var deliveries = _context.Deliveries
                .Include(d => d.Customer)
                .Include(d => d.Agent)
                .Include(d => d.ProofOfDelivery) 
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            return View(deliveries);
        }

        // ===================== GET : ADD DELIVERY =====================
        [HttpGet]
        public IActionResult AddDelivery()
        {
            if (HttpContext.Session.GetString("Role") != "ADMIN")
                return RedirectToAction("Login", "Auth");

            // list of customers in the drop down
            ViewBag.Customers = _context.Users
                .Where(u => u.Role == "CUSTOMER")
                .ToList();
            // list of agents with active status in drop down
            ViewBag.Agents = _context.Users
                .Where(u => u.Role == "DELIVERY_AGENT" && u.Status == "Active")
                .ToList();

            return View();
        }

        // ===================== POST : ADD DELIVERY =====================
        [HttpPost]
        public IActionResult AddDelivery(string CustomerName, int AssignedAgentId, string Destination, int DeliveryPincode, string Status, string status, string ETA)
        {
            if (HttpContext.Session.GetString("Role") != "ADMIN")
                return RedirectToAction("Login", "Auth");

            var customer = _context.Users.FirstOrDefault(u => u.Username == CustomerName && u.Role == "CUSTOMER");
            if (customer == null) return RedirectToAction("AddDelivery");

            string finalStatus = !string.IsNullOrEmpty(Status) ? Status : status;
            if (string.IsNullOrEmpty(finalStatus)) finalStatus = "ASSIGNED";

                // 1. Create Delivery
                Delivery delivery = new Delivery
            {
                CustomerId = customer.UserId,
                AgentId = AssignedAgentId,
                Address = Destination,
                Status = finalStatus.ToUpper(),
                CreatedAt = DateTime.Now,
               
            };

            _context.Deliveries.Add(delivery);
            _context.SaveChanges();

            // 2. Resolve Coordinates locally (Fixes the "Jumping" bug)
            var coords = GetCoordinatesFromPincode(DeliveryPincode);
            if (coords.lat == 0) return RedirectToAction("AddDelivery");

            decimal warehouseLat = 13.082700m;
            decimal warehouseLng = 80.270700m;

            // 3. Create Route with high precision
            LastMileDelivery.Models.Route routeEntity = new LastMileDelivery.Models.Route
            {
                DeliveryId = delivery.DeliveryId,
                StartLat = warehouseLat,
                StartLng = warehouseLng,
                CurrentLat = warehouseLat,
                CurrentLng = warehouseLng,
                EndLat = coords.lat,
                EndLng = coords.lon,
                DistanceKm = CalculateDistance(warehouseLat, warehouseLng, coords.lat, coords.lon),
                EstimatedTime = ETA
            };

            _context.Routes.Add(routeEntity);
            _context.SaveChanges();

            return RedirectToAction("AdminDeliveries");
            
        }

        // ===================== PINCODE → LAT/LNG MAPPING =====================
        private (decimal lat, decimal lon) GetCoordinatesFromPincode(int pincode)
        {
            // Fetch the specific pincode section from appsettings.json
            var section = _configuration.GetSection($"PincodeSettings:Locations:{pincode}");

            if (section.Exists())
            {
                // Parse strings back to high-precision decimals
                decimal lat = decimal.Parse(section["Lat"] ?? "0");
                decimal lon = decimal.Parse(section["Lon"] ?? "0");
                return (lat, lon);
            }

            return (0m, 0m); // Default fallback
        }

        // ===================== DISTANCE CALCULATION =====================
        private decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            // GeoCoordinate: calculate distances between coordinates using the Haversine formula, ensuring my delivery distance tracking is mathematically sound.
            var locA = new GeoCoordinate((double)lat1, (double)lon1);
            var locB = new GeoCoordinate((double)lat2, (double)lon2);
            //distance from location A to location B and then Divide by 1000 to get in km
            return (decimal)(locA.GetDistanceTo(locB) / 1000.0);
        }

        // ===================== Delete the delivery =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDelivery(int id)
        {
            // 1. Security Check
            if (HttpContext.Session.GetString("Role") != "ADMIN")
                return RedirectToAction("Login", "Auth");
            // 2. Find the Delivery record
            var delivery = _context.Deliveries.FirstOrDefault(d => d.DeliveryId == id);
            if (delivery != null)
            {
                // 3. Find ALL Routes linked to this DeliveryId
                // This is the step that clears the "FK_Route_Delivery" conflict
                var relatedRoutes = _context.Routes.Where(r => r.DeliveryId == id).ToList();
                if (relatedRoutes.Any())
                {
                    _context.Routes.RemoveRange(relatedRoutes);
                    _context.SaveChanges();
                }
                var searchPattern = $"Delivery Service #{id}";
                var relatedInvoiceItems = _context.InvoiceItems
                    .Where(ii => ii.Description.Contains(searchPattern))
                    .ToList();

                if (relatedInvoiceItems.Any())
                {
                    _context.InvoiceItems.RemoveRange(relatedInvoiceItems);

                    // 3. Save these deletions first
                    _context.SaveChanges();
                }
                // 4. Now that the children are gone, delete the parent
                _context.Deliveries.Remove(delivery);
                // 5. Save changes to the database
                _context.SaveChanges();
            }
            return RedirectToAction("AdminDeliveries");
        }


        // ====================  GET: AdminBilling list page ====================
        public async Task<IActionResult> AdminBilling()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Customer)
                .OrderByDescending(i => i.CreatedDate)
                .ToListAsync();

            return View(invoices);
        }

        // ====================  GET: Create Invoice Page ====================
        [HttpGet]
        public IActionResult CreateInvoice()
        {
            // 1. Get a list of all Delivery IDs that are already mentioned in any Invoice Item
            // We look for the pattern "Delivery Service #ID" in the description
            var existingInvoiceDescriptions = _context.InvoiceItems
                .Select(i => i.Description)
                .ToList();

            // 2. Fetch only deliveries that:
            //    a) Have a Route (your existing logic)
            //    b) DO NOT have an invoice already (the new logic)
            var deliveries = _context.Deliveries
                //It tells the database to join the Customer table so we can show the customer's name
                .Include(d => d.Customer)
                .Where(d => _context.Routes.Any(r => r.DeliveryId == d.DeliveryId))
                .AsEnumerable() // Switch to memory for the complex string check
                                //The "Anti-Duplicate" Check:
                .Where(d => !existingInvoiceDescriptions.Any(desc => desc.Contains($"Delivery Service #{d.DeliveryId}")))
                .Select(d => new SelectListItem
                {
                    Value = d.DeliveryId.ToString(),
                    Text = $"Delivery #{d.DeliveryId} - {d.Customer.Username} ({d.Status})"
                }).ToList();

            ViewBag.Deliveries = deliveries;

            var model = new Invoice
            {
                InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(100, 999),
                CreatedDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(7) // Setting a default 1-week due date
            };

            return View(model);
        }

        // ====================  POST: Create Invoice ====================
        [HttpPost]
        public async Task<IActionResult> CreateInvoice(Invoice invoice, int SelectedDeliveryId, decimal Weight, decimal CalculatedAmount)
        {
            ModelState.Remove("Customer");
            ModelState.Remove("Items");
            foreach (var key in ModelState.Keys.Where(k => k.Contains("Invoice")).ToList())
            {
                ModelState.Remove(key);
            }

            if (SelectedDeliveryId > 0 && CalculatedAmount > 0)
            {
                // === 1. CHECK IF INVOICE ALREADY EXISTS FOR THIS DELIVERY ===
                // This enforces your "One Delivery = One Invoice" rule
                bool alreadyExists = await _context.Invoices
                    .AnyAsync(i => i.Items.Any(item => item.Description.Contains($"Delivery Service #{SelectedDeliveryId}")));

                /* NOTE: If you have a specific 'DeliveryId' column in your Invoice table, 
                   use: _context.Invoices.AnyAsync(i => i.DeliveryId == SelectedDeliveryId) instead. */

                if (alreadyExists)
                {
                    TempData["Error"] = "An invoice has already been generated for this delivery ID.";
                    return RedirectToAction("AdminBilling");
                }
                var delivery = await _context.Deliveries
                    .Include(d => d.Customer)
                    .FirstOrDefaultAsync(d => d.DeliveryId == SelectedDeliveryId);

                var route = _context.Routes.FirstOrDefault(r => r.DeliveryId == SelectedDeliveryId);

                if (delivery != null)
                {
                    invoice.CustomerId = delivery.CustomerId;
                    invoice.CreatedDate = DateTime.Now;

                    if (invoice.DueDate.Year < 1753)
                    {
                        invoice.DueDate = DateTime.Now.AddDays(14);
                    }

                    invoice.TotalAmount = CalculatedAmount;
                    invoice.Status = "Pending";

                    var item = new InvoiceItem
                    {
                        Description = $"Delivery Service #{delivery.DeliveryId} - {route?.DistanceKm} km / {Weight} kg ({route?.EstimatedTime})",
                        Quantity = 1,
                        UnitPrice = CalculatedAmount
                    };

                    invoice.Items = new List<InvoiceItem> { item };

                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("AdminBilling");
                }
            }

            var deliveries = _context.Deliveries
                .Include(d => d.Customer)
                .Where(d => _context.Routes.Any(r => r.DeliveryId == d.DeliveryId))
                .Select(d => new SelectListItem
                {
                    Value = d.DeliveryId.ToString(),
                    Text = $"Delivery #{d.DeliveryId} - {d.Customer.Username} ({d.Status})"
                }).ToList();

            ViewBag.Deliveries = deliveries;
            return View(invoice);
        }

        // ====================  GET: InvoiceDetails ====================
        public async Task<IActionResult> InvoiceDetails(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // ==================== 2. AJAX API: Get Delivery Data ====================
        [HttpGet]
        public JsonResult GetDeliveryInfo(int deliveryId)
        {
            var route = _context.Routes.FirstOrDefault(r => r.DeliveryId == deliveryId);
            if (route == null)
            {
                return Json(new { success = false, message = "Route not found" });
            }
            return Json(new
            {
                success = true,
                distance = route.DistanceKm,
                eta = route.EstimatedTime
            });
        }

        // ==================== Admin can see the agents list with status and details of the agent ====================
        public IActionResult AdminAgents()
        {
            if (HttpContext.Session.GetString("Role") != "ADMIN")
                return RedirectToAction("Login", "Auth");
            var agents = _context.Users.Where(u => u.Role == "DELIVERY_AGENT").ToList();

            // 2. For each customer, count their orders from the Deliveries table
            foreach (var agent in agents)
            {
                // FIX: Use AgentId to count deliveries for agents
                agent.Deliveries = _context.Deliveries
                    .Count(d => d.AgentId == agent.UserId);
            }

            // Calculations for the cards
            ViewBag.TotalAgents = agents.Count();
            ViewBag.ActiveAgents = agents.Count(u => u.Status == "Active");
            ViewBag.InactiveAgents = agents.Count(u => u.Status == "Inactive");

            return View(agents);
        }       
    }
}
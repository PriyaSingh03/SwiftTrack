using LastMileDelivery.Data;
using LastMileDelivery.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // Required for file handling
using Microsoft.AspNetCore.Http; // Required for IFormFile
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LastMileDelivery.Controllers
{
    public class AgentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        // Inject IWebHostEnvironment here
        public AgentController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // ================= AGENT DASHBOARD =================
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "DELIVERY_AGENT")
                return RedirectToAction("Login", "Auth");

            int agentId = HttpContext.Session.GetInt32("UserId").Value;
            var agent = _context.Users.Find(agentId);
            ViewBag.AgentStatus = agent?.Status ?? "Inactive";

            var deliveries = _context.Deliveries
                .Include(d => d.Customer)
                .ThenInclude(c => c.Invoices)
                .ThenInclude(i => i.Items)
                .Where(d => d.AgentId == agentId)
                .OrderByDescending(d => d.CreatedAt)
                .ToList();
            // Get the count of deliveries assigned to this specific agent
            ViewBag.OrderCount = deliveries.Count;
            return View(deliveries);
        }

        // ================= VIEW DELIVERY / MAP =================
        public IActionResult ViewDelivery(int id)
        {
            if (HttpContext.Session.GetString("Role") != "DELIVERY_AGENT")
                return RedirectToAction("Login", "Auth");

            var delivery = _context.Deliveries
                .Include(d => d.Customer)
                .FirstOrDefault(d => d.DeliveryId == id);

            if (delivery == null) return NotFound();

            var route = _context.Routes.FirstOrDefault(r => r.DeliveryId == id);

            ViewBag.DeliveryId = delivery.DeliveryId;
            ViewBag.CustomerName = delivery.Customer.Username;
            ViewBag.Address = delivery.Address;
            ViewBag.Status = delivery.Status;

            if (route != null)
            {
                ViewBag.DatabaseDistance = route.DistanceKm;
                if (delivery.Status == "IN_TRANSIT" && route.CurrentLat != null)
                {
                    ViewBag.FromLat = route.CurrentLat;
                    ViewBag.FromLng = route.CurrentLng;
                }
                else
                {
                    ViewBag.FromLat = route.StartLat;
                    ViewBag.FromLng = route.StartLng;
                }

                ViewBag.ToLat = route.EndLat;
                ViewBag.ToLng = route.EndLng;
            }

            return View(delivery);
        }

        // ================= GET: UPDATE PAGE =================
        [HttpGet]
        public IActionResult Update(int id)
        {
            if (HttpContext.Session.GetString("Role") != "DELIVERY_AGENT")
                return RedirectToAction("Login", "Auth");

            var delivery = _context.Deliveries.Find(id);
            if (delivery == null) return NotFound();
            // 2. CHECK: If it is already delivered, do not update. 
            // Return a special view or a flag to the existing view.
            if (delivery.Status == "DELIVERED")
            {
                // You can return a specific "AlreadyDelivered" view
                return View("AlreadyDelivered", delivery);
            }
            if (delivery.Status == "CANCELLED")
            {
                return View("Cancelled", delivery);
            }

                return View(delivery);
        }



        // ================= POST: PROCESS UPDATE & PROOF UPLOAD =================
        [HttpPost]
        public async Task<IActionResult> Update(int id, string Status, int CurrentPincode, IFormFile ProofImage)
        {
            var delivery = _context.Deliveries.Find(id);
            if (delivery == null) return NotFound();

            // 1. Update Status
            delivery.Status = Status;

            // 2. Handle Proof of Delivery Upload
            if (Status == "DELIVERED" && ProofImage != null && ProofImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "proofs");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"proof_{id}_{DateTime.Now.Ticks}{Path.GetExtension(ProofImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProofImage.CopyToAsync(stream);
                }

                // 👉 INSERT YOUR CODE HERE
                var proof = new ProofOfDelivery
                {
                    DeliveryId = id,
                    PhotoURL = $"/uploads/proofs/{uniqueFileName}",
                    Timestamp = DateTime.Now,
                    Signature = "Signed by customer" // or path to signature file
                };
                _context.ProofOfDeliveries.Add(proof);
            }

            _context.SaveChanges();
            return RedirectToAction("Dashboard");
        }




        // ================= UPDATE STATUS (API) =================
        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            var delivery = _context.Deliveries.Find(id);
            if (delivery != null)
            {
                delivery.Status = status;
                _context.SaveChanges();
            }
            return RedirectToAction("ViewDelivery", new { id = id });
        }

        // ================= UPDATE LIVE LOCATION (API) =================
        [HttpPost]
        public IActionResult UpdateLocation(int deliveryId, double lat, double lng)
        {
            var delivery = _context.Deliveries.Find(deliveryId);
            var route = _context.Routes.FirstOrDefault(r => r.DeliveryId == deliveryId);

            if (delivery != null && route != null)
            {
                route.CurrentLat = (decimal)lat;
                route.CurrentLng = (decimal)lng;

                if (delivery.Status == "ASSIGNED")
                {
                    delivery.Status = "IN_TRANSIT";
                }

                _context.SaveChanges();
                return Ok(new { success = true });
            }

            return NotFound();
        }

        [HttpPost]
        public IActionResult ToggleSelfStatus()
        {
            int? agentId = HttpContext.Session.GetInt32("UserId");
            var agent = _context.Users.Find(agentId);

            if (agent != null)
            {
                // Flip status
                agent.Status = (agent.Status == "Active") ? "Inactive" : "Active";
                _context.SaveChanges();
            }

            return RedirectToAction("Dashboard");
        }
    }
}
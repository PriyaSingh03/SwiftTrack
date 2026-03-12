using LastMileDelivery.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LastMileDelivery.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= CUSTOMER DASHBOARD / TRACK ORDER =================
        [HttpGet]
        public IActionResult CustomerDashboard(int? orderId)
        {
            if (HttpContext.Session.GetString("Role") != "CUSTOMER")
                return RedirectToAction("Login", "Auth");

            
            //ViewBag.CustomerName = Username;
            //ViewBag.CustomerRole = "Customer";

            ViewBag.HasSearched = true; // Flag to show UI

            int customerId = HttpContext.Session.GetInt32("UserId").Value;

            var currentUser = _context.Users.FirstOrDefault(u => u.UserId == customerId);
            // Pass the Username and Role to the ViewBag for the sidebar
            ViewBag.CustomerName = currentUser?.Username ?? "Unknown User";
            ViewBag.CustomerRole = currentUser?.Role ?? "CUSTOMER";

            // Fetch orders for this customer to show in the table
            ViewBag.MyOrders = _context.Deliveries
                .Include(d => d.Agent)
                .Where(d => d.CustomerId == customerId)
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            // If orderId is null, the user just arrived at the page
            if (!orderId.HasValue)
            {
                ViewBag.HasSearched = false; // Flag to keep UI hidden
                return View();
            }

            var query = _context.Deliveries
                .Include(d => d.Agent)
                .Where(d => d.CustomerId == customerId);

            var delivery = orderId.HasValue
                ? query.FirstOrDefault(d => d.DeliveryId == orderId.Value)
                : query.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

            if (delivery == null)
            {
                ViewBag.NotFound = true;
                return View();
            }

            // 1. Get the orderId from the request (ensure this matches your route parameter)
            // 2. Fetch the invoice that specifically mentions this Delivery ID in its description
            var invoice = _context.Invoices
                .Include(i => i.Items)
                .Where(i => i.CustomerId == customerId)
                .AsEnumerable() // Switch to memory to allow string comparison on the description
                .FirstOrDefault(i => i.Items.Any(ii => ii.Description.Contains($"Delivery Service #{orderId}")));

            // 3. Set the ViewBags based on the SPECIFIC invoice found
            ViewBag.InvoiceStatus = invoice?.Status ?? "No Invoice";
            ViewBag.InvoiceAmount = invoice?.TotalAmount;

            var route = _context.Routes.FirstOrDefault(r => r.DeliveryId == delivery.DeliveryId);

            ViewBag.DeliveryId = delivery.DeliveryId;
            ViewBag.Status = delivery.Status;
            ViewBag.Address = delivery.Address;
            ViewBag.AgentName = delivery.Agent?.Username ?? "Not Assigned";

            // -------- [FIXED] MAP LOGIC --------
            if (route != null)
            {
                if (delivery.Status == "ASSIGNED")
                {
                    ViewBag.FromLat = route.StartLat;
                    ViewBag.FromLng = route.StartLng;
                }
                // Check Status AND ensure coordinates are not 0 (default for decimal)
                else if (delivery.Status == "IN_TRANSIT" && route.CurrentLat != 0)
                {
                    ViewBag.FromLat = route.CurrentLat;
                    ViewBag.FromLng = route.CurrentLng;
                }
                // Fallback if IN_TRANSIT but coordinates are 0
                else if (delivery.Status == "IN_TRANSIT")
                {
                    ViewBag.FromLat = route.StartLat;
                    ViewBag.FromLng = route.StartLng;
                }
                // Handle Cancelled explicitly
                else if (delivery.Status == "CANCELLED")
                {
                    ViewBag.FromLat = null; // No map needed
                    ViewBag.FromLng = null;
                }
                else // DELIVERED
                {
                    ViewBag.FromLat = null;
                    ViewBag.FromLng = null;
                }

                ViewBag.ToLat = route.EndLat;
                ViewBag.ToLng = route.EndLng;
                ViewBag.Distance = route.DistanceKm;
                ViewBag.ETA = route.EstimatedTime;
            }

            return View();
        }


        // GET: Customer/ProcessPayment/6
        // GET: Customer/ProcessPayment/6
        [HttpGet]
        public IActionResult ProcessPayment(int id)
        {
            int customerId = HttpContext.Session.GetInt32("UserId").Value;

            // Check if the delivery itself is cancelled before proceeding
            var delivery = _context.Deliveries.FirstOrDefault(d => d.DeliveryId == id);
            if (delivery != null && delivery.Status == "CANCELLED")
            {
                return BadRequest("Payment is not available for cancelled orders.");
            }

            // Link via CustomerId and get the latest pending invoice
            var invoice = _context.Invoices
                .Include(i => i.Items)
                .Where(i => i.CustomerId == customerId && i.Status == "Pending")
                .OrderByDescending(i => i.CreatedDate)
                .FirstOrDefault(i => i.Items.Any(ii => ii.Description.Contains($"Delivery Service #{id}")));

            if (invoice == null) return NotFound("No pending invoice found for this account.");

            var invoiceItem = _context.InvoiceItems.FirstOrDefault(ii => ii.InvoiceId == invoice.Id);

            ViewBag.DeliveryId = id;
            ViewBag.InvoiceId = invoice.Id;
            ViewBag.InvoiceNo = invoice.InvoiceNumber;
            ViewBag.Amount = invoice.TotalAmount;
            ViewBag.ParcelDetails = invoiceItem?.Description ?? "Standard Delivery";

            return View();
        }


        // POST: Customer/ConfirmPayment
        [HttpPost]
        public IActionResult ConfirmPayment(int invoiceId, int deliveryId, string upiId)
        {
            // Find the invoice by its actual ID
            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == invoiceId);

            if (invoice != null)
            {
                invoice.Status = "Paid";
                _context.SaveChanges();
            }

            return RedirectToAction("CustomerDashboard", new { orderId = deliveryId });
        }

        // ================= [NEW] GET LIVE STATUS API =================
        // Called by CustomerDashboard.cshtml every 5 seconds
        [HttpGet]
        public IActionResult GetLiveStatus(int orderId)
        {
            var delivery = _context.Deliveries.FirstOrDefault(d => d.DeliveryId == orderId);
            var route = _context.Routes.FirstOrDefault(r => r.DeliveryId == orderId);

            if (delivery == null || route == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            // FIX: Explicitly cast decimal to double for the logic variables
            double lat = (double)route.StartLat;
            double lng = (double)route.StartLng;

            // Use '!= 0' because decimal cannot be null
            if (delivery.Status == "IN_TRANSIT" && route.CurrentLat != 0)
            {
                lat = (double)route.CurrentLat;
                lng = (double)route.CurrentLng;
            }
            else if (delivery.Status == "DELIVERED")
            {
                lat = (double)route.EndLat;
                lng = (double)route.EndLng;
            }
            else if (delivery.Status == "CANCELLED")
            {
                // Keep start lat/lng or set to 0, status string is what matters
                lat = (double)route.StartLat;
                lng = (double)route.StartLng;
            }

            return Json(new
            {
                success = true,
                lat = lat,
                lng = lng,
                status = delivery.Status,
                eta = route.EstimatedTime
            });
        }
    }
}
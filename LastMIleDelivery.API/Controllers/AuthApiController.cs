using LastMileDelivery.API.Data;
//using LastMileDelivery.Data;
//using LastMileDelivery.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LastMileDelivery.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- DTO defined inside the controller for simplicity ---
        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email, Password, and Role are required." });
            }

            string hashedPassword = HashPassword(request.Password);

            // ASYNC: Non-blocking database lookup
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == request.Email.Trim() &&
                u.Password == hashedPassword &&
                u.Role == request.Role);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials or role." });
            }
            // POST SERIALIZE: The 'Ok' method automatically serializes the object to JSON
            return Ok(new
            {
                user.UserId,
                user.Username,
                user.Role,
                user.Email,
                user.Status
            });
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
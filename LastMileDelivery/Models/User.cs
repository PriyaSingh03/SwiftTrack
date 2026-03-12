using System.ComponentModel.DataAnnotations;

namespace LastMileDelivery.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string? Username { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; } 

        [Required]
        public string? Role { get; set; }

        [Required]
        [Phone] // Ensures valid phone formatting
        [StringLength(10)] // Common limit for phone numbers
        public string PhoneNumber { get; set; }

        public string? Status { get; set; }     // Active, Idle, Offline
        public int? Deliveries { get; set; }    // Ensure this exists in your DB!

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    }
}

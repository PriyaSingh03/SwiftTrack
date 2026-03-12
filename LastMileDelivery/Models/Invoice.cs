using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LastMileDelivery.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string InvoiceNumber { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public User? Customer { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }

    public class InvoiceItem
    {
        [Key]
        public int Id { get; set; }

        public int InvoiceId { get; set; }

        [ForeignKey("InvoiceId")]
        public Invoice? Invoice { get; set; } 

        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Helper property, not stored in DB
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
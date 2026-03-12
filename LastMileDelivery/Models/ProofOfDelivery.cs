using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LastMileDelivery.Models
{
    public class ProofOfDelivery
    {
        [Key]
        public int ProofId { get; set; }

        public int DeliveryId { get; set; }

        [ForeignKey("DeliveryId")]
        public virtual Delivery Delivery { get; set; }

        public string PhotoURL { get; set; } // Path to stored image
        public string Signature { get; set; } // Optional: Signature data
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LastMileDelivery.Models
{
    [Table("Delivery")]
    public class Delivery
    {
        [Key]
        public int DeliveryId { get; set; }

        public int CustomerId { get; set; }      // FK -> User(UserId)
        public int? AgentId { get; set; }       // FK -> User(UserId)

        public string Address { get; set; }

        public string Status { get; set; }       // ASSIGNED, OUT_FOR_DELIVERY, DELIVERED

        public DateTime CreatedAt { get; set; }

        [ForeignKey("CustomerId")]
        public User Customer { get; set; }

        [ForeignKey("AgentId")]
        public User Agent { get; set; }

        public virtual ProofOfDelivery ProofOfDelivery { get; set; }
    }
}
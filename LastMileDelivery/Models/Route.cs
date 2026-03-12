using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LastMileDelivery.Models
{
    [Table("Route")]
    public class Route
    {
        [Key]
        public int RouteId { get; set; }

        public int DeliveryId { get; set; }  // FK -> Delivery(DeliveryId)
        [Precision(18, 6)]
        public decimal StartLat { get; set; }
        [Precision(18, 6)]
        public decimal StartLng { get; set; }
        [Precision(18, 6)]
        public decimal EndLat { get; set; }
        [Precision(18, 6)]
        public decimal EndLng { get; set; }
        [Precision(18, 6)]
        public decimal CurrentLat { get; set; }
        [Precision(18, 6)]
        public decimal CurrentLng { get; set; }
      
        public decimal DistanceKm { get; set; }
        public string EstimatedTime { get; set; }
    }
}
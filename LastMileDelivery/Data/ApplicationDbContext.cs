using LastMileDelivery.Models;
using Microsoft.EntityFrameworkCore;
 
namespace LastMileDelivery.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Delivery> Deliveries { get; set; }

        public DbSet<LastMileDelivery.Models.Route> Routes { get; set; }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        public DbSet<ProofOfDelivery> ProofOfDeliveries { get; set; }

        
    }
}
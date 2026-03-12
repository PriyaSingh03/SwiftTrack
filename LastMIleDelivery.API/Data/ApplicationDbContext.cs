using LastMileDelivery.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LastMileDelivery.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

    }
}
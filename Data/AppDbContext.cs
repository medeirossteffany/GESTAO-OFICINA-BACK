using Microsoft.EntityFrameworkCore;
using GestaoOficina.Models;

namespace GestaoOficina.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<CustomerLegalType> CustomerLegalTypes { get; set; }
        public DbSet<CustomerCategory> CustomerCategories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ServiceOrderStatus> ServiceOrderStatuses { get; set; }
        public DbSet<ServiceOrder> ServiceOrders { get; set; }
        public DbSet<ServiceOrderPart> ServiceOrderParts { get; set; }
        public DbSet<ServiceOrderTimeline> ServiceOrderTimelines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aqui você pode configurar as relações, chaves únicas, etc.
        }
    }
}

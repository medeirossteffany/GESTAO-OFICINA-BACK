using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using GestaoOficina.Data.Configurations;
using GestaoOficina.Entities;

namespace GestaoOficina.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<UserUnit> UserUnits { get; set; }
        public DbSet<CustomerLegalType> CustomerLegalTypes { get; set; }
        public DbSet<CustomerCategory> CustomerCategories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerUnit> CustomerUnits { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ServiceOrderStatus> ServiceOrderStatuses { get; set; }
        public DbSet<ServiceOrder> ServiceOrders { get; set; }
        public DbSet<ServiceOrderPart> ServiceOrderParts { get; set; }
        public DbSet<ServiceOrderTimeline> ServiceOrderTimelines { get; set; }
        public DbSet<TenantUsage> TenantUsages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new TenantConfiguration());
            modelBuilder.ApplyConfiguration(new UnitConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new UserUnitConfiguration());
            modelBuilder.ApplyConfiguration(new CustomerLegalTypeConfiguration());
            modelBuilder.ApplyConfiguration(new CustomerCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new CustomerConfiguration());
            modelBuilder.ApplyConfiguration(new CustomerUnitConfiguration());
            modelBuilder.ApplyConfiguration(new VehicleConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceOrderStatusConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceOrderConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceOrderPartConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceOrderTimelineConfiguration());
            modelBuilder.ApplyConfiguration(new TenantUsageConfiguration());
        }
    }
}

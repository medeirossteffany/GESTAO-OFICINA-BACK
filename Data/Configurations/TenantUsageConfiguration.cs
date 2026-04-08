using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoOficina.Data.Configurations
{
    public class TenantUsageConfiguration : IEntityTypeConfiguration<TenantUsage>
    {
        public void Configure(EntityTypeBuilder<TenantUsage> builder)
        {
            builder.ToTable("TenantUsages");

            builder.HasKey(x => x.TenantId);

            builder.Property(x => x.CurrentUnits).IsRequired().HasDefaultValue(0);
            builder.Property(x => x.CurrentUsers).IsRequired().HasDefaultValue(0);
            builder.Property(x => x.CurrentCustomers).IsRequired().HasDefaultValue(0);
            builder.Property(x => x.CurrentVehicles).IsRequired().HasDefaultValue(0);
            builder.Property(x => x.CurrentServicesInMonth).IsRequired().HasDefaultValue(0);
            builder.Property(x => x.ServicesMonthReference).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();

            builder.HasOne(x => x.Tenant)
                .WithOne(t => t.Usage)
                .HasForeignKey<TenantUsage>(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

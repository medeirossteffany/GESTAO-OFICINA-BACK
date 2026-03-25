using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
    {
        public void Configure(EntityTypeBuilder<ServiceOrder> builder)
        {
            builder.HasKey(so => so.Id);
            builder.Property(so => so.EntryDate).IsRequired();
            builder.Property(so => so.BodyworkValue).HasDefaultValue(0);
            builder.Property(so => so.PaintValue).HasDefaultValue(0);
            builder.Property(so => so.PartsValue).HasDefaultValue(0);
            builder.Property(so => so.TotalDiscount).HasDefaultValue(0);
            builder.Property(so => so.TotalAmount).IsRequired();
            builder.Property(so => so.CreatedAt).IsRequired();
            builder.Property(so => so.UpdatedAt).IsRequired();
            builder.HasOne(so => so.Tenant)
                .WithMany(t => t.ServiceOrders)
                .HasForeignKey(so => so.TenantId);
            builder.HasOne(so => so.Unit)
                .WithMany(u => u.ServiceOrders)
                .HasForeignKey(so => so.UnitId);
            builder.HasOne(so => so.Vehicle)
                .WithMany()
                .HasForeignKey(so => so.VehicleId);
            builder.HasOne(so => so.OwnerCustomer)
                .WithMany()
                .HasForeignKey(so => so.OwnerCustomerId);
            builder.HasOne(so => so.Status)
                .WithMany()
                .HasForeignKey(so => so.StatusId);
        }
    }
}

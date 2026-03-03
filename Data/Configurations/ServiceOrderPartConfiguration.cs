using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class ServiceOrderPartConfiguration : IEntityTypeConfiguration<ServiceOrderPart>
    {
        public void Configure(EntityTypeBuilder<ServiceOrderPart> builder)
        {
            builder.HasKey(sop => sop.Id);
            builder.Property(sop => sop.Description).IsRequired();
            builder.Property(sop => sop.Quantity).IsRequired();
            builder.Property(sop => sop.UnitPrice).IsRequired();
            builder.Property(sop => sop.TotalPrice).IsRequired();
            builder.Property(sop => sop.CreatedAt).IsRequired();
            builder.HasOne(sop => sop.Tenant)
                .WithMany(t => t.ServiceOrderParts)
                .HasForeignKey(sop => sop.TenantId);
            builder.HasOne(sop => sop.ServiceOrder)
                .WithMany()
                .HasForeignKey(sop => sop.ServiceOrderId);
        }
    }
}
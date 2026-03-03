using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Plate).IsRequired();
            builder.Property(v => v.Brand).IsRequired();
            builder.Property(v => v.Model).IsRequired();
            builder.Property(v => v.CreatedAt).IsRequired();
            builder.HasIndex(v => new { v.TenantId, v.Plate }).IsUnique();
            builder.HasOne(v => v.Tenant)
                .WithMany(t => t.Vehicles)
                .HasForeignKey(v => v.TenantId);
            builder.HasOne(v => v.Customer)
                .WithMany()
                .HasForeignKey(v => v.CustomerId);
        }
    }
}
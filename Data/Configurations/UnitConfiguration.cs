using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class UnitConfiguration : IEntityTypeConfiguration<Unit>
    {
        public void Configure(EntityTypeBuilder<Unit> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Name).IsRequired();
            builder.Property(u => u.AddressZip).IsRequired();
            builder.Property(u => u.AddressStreet).IsRequired();
            builder.Property(u => u.AddressNumber).IsRequired();
            builder.Property(u => u.AddressDistrict).IsRequired();
            builder.Property(u => u.AddressCity).IsRequired();
            builder.Property(u => u.AddressState).IsRequired();
            builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
            builder.Property(u => u.CreatedAt).IsRequired();
            builder.HasIndex(u => new { u.TenantId, u.Cnpj }).IsUnique().HasFilter("[Cnpj] IS NOT NULL");
            builder.HasOne(u => u.Tenant)
                .WithMany(t => t.Units)
                .HasForeignKey(u => u.TenantId);
            builder.Property(u => u.Email).HasMaxLength(255);
            builder.Property(u => u.Phone).HasMaxLength(30);
        }
    }
}

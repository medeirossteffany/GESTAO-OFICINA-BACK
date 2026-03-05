using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.Name).IsRequired();
            builder.Property(u => u.Role)
                .HasConversion<string>()
                .IsRequired();
            builder.Property(u => u.IsActive).IsRequired();
            builder.Property(u => u.FullAccess).IsRequired().HasDefaultValue(false);
            builder.Property(u => u.CreatedAt).IsRequired();
            
            // Email único globalmente (em todos os tenants)
            builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            
            // PhoneNumber único globalmente (em todos os tenants)
            builder.HasIndex(u => u.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL");
            
            builder.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId);
        }
    }
}
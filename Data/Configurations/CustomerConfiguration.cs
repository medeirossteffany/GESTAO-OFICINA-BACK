using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired();
            builder.Property(c => c.CpfCnpj).HasColumnName("cpf/cnpj");
            builder.Property(c => c.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(c => c.CreatedAt).IsRequired();
            builder.HasOne(c => c.Tenant)
                .WithMany(t => t.Customers)
                .HasForeignKey(c => c.TenantId);
            builder.HasOne(c => c.LegalType)
                .WithMany()
                .HasForeignKey(c => c.LegalTypeId);
        }
    }
}

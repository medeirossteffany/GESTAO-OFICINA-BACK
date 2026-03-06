using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class CustomerLegalTypeConfiguration : IEntityTypeConfiguration<CustomerLegalType>
    {
        public void Configure(EntityTypeBuilder<CustomerLegalType> builder)
        {
            builder.HasKey(clt => clt.Id);
            builder.Property(clt => clt.Code).IsRequired();
            builder.Property(clt => clt.Name).IsRequired();
            builder.HasIndex(clt => clt.Code).IsUnique();

            builder.HasData(
                new CustomerLegalType { Id = 1, Code = "PF", Name = "Cliente Físico" },
                new CustomerLegalType { Id = 2, Code = "PJ", Name = "Cliente Empresa" }
            );
        }
    }
}
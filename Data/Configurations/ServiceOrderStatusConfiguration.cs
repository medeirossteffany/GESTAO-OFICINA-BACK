using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class ServiceOrderStatusConfiguration : IEntityTypeConfiguration<ServiceOrderStatus>
    {
        public void Configure(EntityTypeBuilder<ServiceOrderStatus> builder)
        {
            builder.HasKey(sos => sos.Id);
            builder.Property(sos => sos.Code).IsRequired();
            builder.Property(sos => sos.Name).IsRequired();
            builder.Property(sos => sos.SortOrder).IsRequired();
            builder.HasIndex(sos => sos.Code).IsUnique();

            builder.HasData(
                new ServiceOrderStatus { Id = 1, Code = "ENVIADO", Name = "Enviado", SortOrder = 1 },
                new ServiceOrderStatus { Id = 2, Code = "FEITO", Name = "Feito", SortOrder = 2 },
                new ServiceOrderStatus { Id = 3, Code = "FINALIZADO", Name = "Finalizado", SortOrder = 3 }
            );
        }
    }
}
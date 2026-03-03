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
        }
    }
}
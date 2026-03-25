using GestaoOficina.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoOficina.Data.Configurations
{
    public class CustomerUnitConfiguration : IEntityTypeConfiguration<CustomerUnit>
    {
        public void Configure(EntityTypeBuilder<CustomerUnit> builder)
        {
            builder.HasKey(cu => cu.Id);

            builder.HasOne(cu => cu.Customer)
                .WithMany(c => c.CustomerUnits)
                .HasForeignKey(cu => cu.CustomerId);

            builder.HasOne(cu => cu.Unit)
                .WithMany(u => u.CustomerUnits)
                .HasForeignKey(cu => cu.UnitId);

            builder.HasIndex(cu => new { cu.CustomerId, cu.UnitId }).IsUnique();
        }
    }
}

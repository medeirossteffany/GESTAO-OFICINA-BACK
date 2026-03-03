using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class CustomerCategoryConfiguration : IEntityTypeConfiguration<CustomerCategory>
    {
        public void Configure(EntityTypeBuilder<CustomerCategory> builder)
        {
            builder.HasKey(cc => cc.Id);
            builder.Property(cc => cc.Name).IsRequired();
            builder.Property(cc => cc.IsActive).IsRequired();
            builder.Property(cc => cc.CreatedAt).IsRequired();
            builder.HasOne(cc => cc.Tenant)
                .WithMany(t => t.CustomerCategories)
                .HasForeignKey(cc => cc.TenantId);
        }
    }
}

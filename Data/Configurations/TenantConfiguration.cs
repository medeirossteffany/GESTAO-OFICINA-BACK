using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants", t =>
                t.HasCheckConstraint("CK_Tenants_Plan", "`Plan` IN ('Basico','Profissional','Premium')"));

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired();
            builder.Property(t => t.CreatedAt).IsRequired();

            builder.Property(t => t.Plan)
                .HasConversion<string>()
                .IsRequired()
                .HasDefaultValue(TenantPlan.Basico);

            builder.HasOne(t => t.Unit)
                .WithMany()
                .HasForeignKey(t => t.UnitId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}

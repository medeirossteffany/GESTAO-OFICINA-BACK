using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class ServiceOrderTimelineConfiguration : IEntityTypeConfiguration<ServiceOrderTimeline>
    {
        public void Configure(EntityTypeBuilder<ServiceOrderTimeline> builder)
        {
            builder.HasKey(sot => sot.Id);
            builder.Property(sot => sot.EventType).IsRequired();
            builder.Property(sot => sot.Message).IsRequired();
            builder.Property(sot => sot.CreatedAt).IsRequired();
            builder.HasOne(sot => sot.Tenant)
                .WithMany(t => t.ServiceOrderTimelines)
                .HasForeignKey(sot => sot.TenantId);
            builder.HasOne(sot => sot.ServiceOrder)
                .WithMany()
                .HasForeignKey(sot => sot.ServiceOrderId);
            builder.HasOne(sot => sot.User)
                .WithMany()
                .HasForeignKey(sot => sot.UserId)
                .IsRequired(false);
            builder.HasOne(sot => sot.OldStatus)
                .WithMany()
                .HasForeignKey(sot => sot.OldStatusId)
                .IsRequired(false);
            builder.HasOne(sot => sot.NewStatus)
                .WithMany()
                .HasForeignKey(sot => sot.NewStatusId)
                .IsRequired(false);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GestaoOficina.Entities;

namespace GestaoOficina.Data.Configurations
{
    public class UserUnitConfiguration : IEntityTypeConfiguration<UserUnit>
    {
        public void Configure(EntityTypeBuilder<UserUnit> builder)
        {
            builder.HasKey(uu => uu.Id);
            builder.Property(uu => uu.IsActive).IsRequired().HasDefaultValue(true);
            builder.HasOne(uu => uu.User)
                .WithMany(u => u.UserUnits)
                .HasForeignKey(uu => uu.UserId);
            builder.HasOne(uu => uu.Unit)
                .WithMany(u => u.UserUnits)
                .HasForeignKey(uu => uu.UnitId);
            builder.HasIndex(uu => new { uu.UserId, uu.UnitId }).IsUnique();
        }
    }
}

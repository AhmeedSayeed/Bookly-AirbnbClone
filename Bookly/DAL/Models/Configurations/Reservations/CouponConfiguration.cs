using DAL.Constants;
using DAL.Models.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Reservations
{
    public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(DataSchemaConstants.CouponCodeLength);

            builder.HasIndex(c => c.Code)
       .IsUnique()
       .HasFilter("[IsDeleted] = 0");
            builder.Property(c => c.DiscountPercent)
                .HasColumnType(DataSchemaConstants.PercentageColumnType);
        }
    }
}
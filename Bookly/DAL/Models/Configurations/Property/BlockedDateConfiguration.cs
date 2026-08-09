using DAL.Constants;
using DAL.Models.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Property
{
    public class BlockedDateConfiguration : IEntityTypeConfiguration<BlockedDate>
    {
        public void Configure(EntityTypeBuilder<BlockedDate> builder)
        {
            builder.HasKey(bd => bd.Id);

            builder.Property(bd => bd.Reason)
                .HasMaxLength(DataSchemaConstants.ShortNameLength);

            builder.HasOne(bd => bd.Listing)
                .WithMany(l => l.BlockedDates)
                .HasForeignKey(bd => bd.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
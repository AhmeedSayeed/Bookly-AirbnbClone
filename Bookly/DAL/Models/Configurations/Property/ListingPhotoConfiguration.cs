using DAL.Constants;
using DAL.Models.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Property
{
    public class ListingPhotoConfiguration : IEntityTypeConfiguration<ListingPhoto>
    {
        public void Configure(EntityTypeBuilder<ListingPhoto> builder)
        {
            builder.HasKey(lp => lp.Id);

            builder.Property(lp => lp.Url)
                .IsRequired()
                .HasMaxLength(DataSchemaConstants.MaxUrlLength);

            builder.HasOne(lp => lp.Listing)
                .WithMany(l => l.Photos)
                .HasForeignKey(lp => lp.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
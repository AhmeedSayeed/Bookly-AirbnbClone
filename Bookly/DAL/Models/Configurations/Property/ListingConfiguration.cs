using DAL.Constants;
using DAL.Models.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Property
{
    public class ListingConfiguration : IEntityTypeConfiguration<Listing>
    {
        public void Configure(EntityTypeBuilder<Listing> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Title)
                .IsRequired()
                .HasMaxLength(DataSchemaConstants.DefaultNameLength);

            builder.Property(l => l.Description)
                .IsRequired()
                .HasMaxLength(DataSchemaConstants.MaxDescriptionLength);

            builder.Property(l => l.Latitude)
                .HasColumnType(DataSchemaConstants.CoordinateColumnType);

            builder.Property(l => l.Longitude)
                .HasColumnType(DataSchemaConstants.CoordinateColumnType);

            builder.Property(l => l.PricePerNight)
                .HasColumnType(DataSchemaConstants.MoneyColumnType);

            builder.HasOne(l => l.Host)
                .WithMany(u => u.Listings)
                .HasForeignKey(l => l.HostId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
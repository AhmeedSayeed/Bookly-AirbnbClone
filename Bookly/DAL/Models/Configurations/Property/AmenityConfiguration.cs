using DAL.Constants;
using DAL.Models.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Property
{
    public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
    {
        public void Configure(EntityTypeBuilder<Amenity> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(DataSchemaConstants.ShortNameLength);

            builder.Property(a => a.IconClass)
                .HasMaxLength(DataSchemaConstants.ShortNameLength);
        }
    }
}
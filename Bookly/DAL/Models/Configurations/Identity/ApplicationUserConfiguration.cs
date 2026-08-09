using DAL.Constants;
using DAL.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Identity
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(DataSchemaConstants.ShortNameLength);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(DataSchemaConstants.ShortNameLength);

            builder.Property(u => u.Bio)
                .HasMaxLength(DataSchemaConstants.MaxDescriptionLength);

            builder.Property(u => u.ProfilePhotoUrl)
                .HasMaxLength(DataSchemaConstants.MaxUrlLength);
        }
    }
}
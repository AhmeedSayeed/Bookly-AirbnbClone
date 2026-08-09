using DAL.Constants;
using DAL.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Identity
{
    public class HostVerificationConfiguration : IEntityTypeConfiguration<HostVerification>
    {
        public void Configure(EntityTypeBuilder<HostVerification> builder)
        {
            builder.HasKey(hv => hv.Id);

            builder.Property(hv => hv.DocumentUrl)
                .IsRequired()
                .HasMaxLength(DataSchemaConstants.MaxUrlLength);

            builder.HasOne(hv => hv.User)
                .WithOne(u => u.HostVerification)
                .HasForeignKey<HostVerification>(hv => hv.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
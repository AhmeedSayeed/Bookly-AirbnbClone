using DAL.Constants;
using DAL.Models.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Interactions
{
    public class HostResponseConfiguration : IEntityTypeConfiguration<HostResponse>
    {
        public void Configure(EntityTypeBuilder<HostResponse> builder)
        {
            builder.HasKey(hr => hr.Id);

            builder.Property(hr => hr.Content)
                .IsRequired()
                .HasMaxLength(DataSchemaConstants.MaxDescriptionLength);

            builder.HasOne(hr => hr.Review)
                .WithOne(r => r.HostResponse)
                .HasForeignKey<HostResponse>(hr => hr.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
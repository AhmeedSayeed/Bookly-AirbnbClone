using DAL.Constants;
using DAL.Models.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Interactions
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Message)
                .HasMaxLength(DataSchemaConstants.DefaultNameLength);

            builder.Property(n => n.MessageKey)
                .HasMaxLength(100);

            builder.Property(n => n.MessageArgsJson)
                .HasMaxLength(1000);

            builder.Property(n => n.Link)
                .HasMaxLength(DataSchemaConstants.MaxUrlLength);

            builder.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
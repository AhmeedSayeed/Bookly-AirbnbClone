using DAL.Models.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            // Set the primary key
            builder.HasKey(c => c.Id);

            // Prevent cascade delete issues for Conversation -> Guest
            builder.HasOne(c => c.Guest)
                .WithMany()
                .HasForeignKey(c => c.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent cascade delete issues for Conversation -> Host
            builder.HasOne(c => c.Host)
                .WithMany()
                .HasForeignKey(c => c.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            // A Conversation is deleted if the Listing is deleted
            builder.HasOne(c => c.Listing)
                .WithMany()
                .HasForeignKey(c => c.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional: If a booking is deleted, do not delete the conversation, just set to null (if nullable) or restrict
            builder.HasOne(c => c.Booking)
                .WithMany()
                .HasForeignKey(c => c.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
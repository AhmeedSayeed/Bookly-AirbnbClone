using DAL.Constants;
using DAL.Models.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Models.Configurations.Reservations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                .HasColumnType(DataSchemaConstants.MoneyColumnType);

            builder.Property(p => p.Currency)
                .HasMaxLength(DataSchemaConstants.CurrencyLength)
                .HasDefaultValue(DataSchemaConstants.DefaultCurrency);

            builder.Property(p => p.PaymobOrderId)
                .HasMaxLength(DataSchemaConstants.ShortNameLength);

            builder.Property(p => p.PaymobTransactionId)
                .HasMaxLength(DataSchemaConstants.ShortNameLength);

            builder.HasOne(p => p.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
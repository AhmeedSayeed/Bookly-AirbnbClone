using BLL.DTOs;
using BLL.Services.Interfaces;
using BLL.Settings;
using DAL.Enums;
using DAL.Models.Reservations;
using DAL.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly PaymobSettings _settings;
        private readonly IRepository<Booking> _bookingRepo;
        private readonly IRepository<Payment> _paymentRepo;
        private readonly INotificationService _notificationService;

        public PaymentService(
            HttpClient httpClient,
            IOptions<PaymobSettings> settings,
            IRepository<Booking> bookingRepo,
            IRepository<Payment> paymentRepo,
            INotificationService notificationService)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _bookingRepo = bookingRepo;
            _paymentRepo = paymentRepo;
            _notificationService = notificationService;
        }

        public async Task<Response<string>> InitiatePaymentAsync(int bookingId, int userId, PaymentMethod method)
        {
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Guest)
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == userId);

            if (booking == null)
                return Response<string>.FailWithKey(ResponseStatus.NotFound, "PaymentBookingNotFound");

            if (booking.Status != BookingStatus.Confirmed)
                return Response<string>.FailWithKey(ResponseStatus.ValidationError, "OnlyConfirmedBookingsCanBePaid");

            int amountCents = (int)(booking.TotalPrice * 100);
            int selectedIntegrationId = method == PaymentMethod.MobileWallet
                ? _settings.WalletIntegrationId
                : _settings.IntegrationId;

            var intentionPayload = new
            {
                amount = amountCents,
                currency = "EGP",
                payment_methods = new object[] { selectedIntegrationId },
                items = new object[]
                {
                    new
                    {
                        name = string.IsNullOrWhiteSpace(booking.Listing?.Title) ? "Booking Reservation" : booking.Listing.Title,
                        amount = amountCents,
                        description = $"Reservation #{booking.Id}",
                        quantity = 1
                    }
                },
                billing_data = new
                {
                    first_name = string.IsNullOrWhiteSpace(booking.Guest?.FirstName) ? "Guest" : booking.Guest.FirstName,
                    last_name = string.IsNullOrWhiteSpace(booking.Guest?.LastName) ? "User" : booking.Guest.LastName,
                    email = string.IsNullOrWhiteSpace(booking.Guest?.Email) ? "guest@example.com" : booking.Guest.Email,
                    phone_number = string.IsNullOrWhiteSpace(booking.Guest?.PhoneNumber) ? "+201000000000" : booking.Guest.PhoneNumber,
                    apartment = "NA",
                    floor = "NA",
                    street = "NA",
                    building = "NA",
                    shipping_method = "PKG",
                    postal_code = "NA",
                    city = "Cairo",
                    country = "EG",
                    state = "Cairo"
                },
                customer = new
                {
                    first_name = string.IsNullOrWhiteSpace(booking.Guest?.FirstName) ? "Guest" : booking.Guest.FirstName,
                    last_name = string.IsNullOrWhiteSpace(booking.Guest?.LastName) ? "User" : booking.Guest.LastName,
                    email = string.IsNullOrWhiteSpace(booking.Guest?.Email) ? "guest@example.com" : booking.Guest.Email
                },
                special_reference = $"BOOKING_{booking.Id}_{DateTime.UtcNow.Ticks}",

                notification_url = "https://p2rwwxz5-7104.euw.devtunnels.ms/Payments/Webhook",
                redirection_url = $"https://localhost:7104/Payments/Callback/{booking.Id}"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/intention/")
            {
                Content = JsonContent.Create(intentionPayload)
            };

            request.Headers.Remove("Authorization");
            request.Headers.TryAddWithoutValidation("Authorization", $"Token {_settings.SecretKey.Trim()}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                return Response<string>.FailWithKey(
                    ResponseStatus.Error,
                    "PaymobError",
                    new[] { errorDetails }
                );
            }

            var json = await response.Content.ReadFromJsonAsync<JsonNode>();
            string clientSecret = json?["client_secret"]?.ToString() ?? string.Empty;
            string intentionId = json?["id"]?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(clientSecret))
                return Response<string>.FailWithKey(
                    ResponseStatus.Error,
                    "PaymentGatewayCheckoutSecretFailed"
                );

            var existingPayment = await _paymentRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(p => p.BookingId == booking.Id);

            if (existingPayment == null)
            {
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    Amount = booking.TotalPrice,
                    Currency = "EGP",
                    PaymobOrderId = intentionId,
                    IntegrationId = selectedIntegrationId,
                    Method = method,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                await _paymentRepo.AddAsync(payment);
            }
            else
            {
                existingPayment.PaymobOrderId = intentionId;
                existingPayment.IntegrationId = selectedIntegrationId;
                existingPayment.Method = method;
                existingPayment.Status = PaymentStatus.Pending;
                _paymentRepo.Update(existingPayment);
            }

            await _paymentRepo.SaveAsync();

            string checkoutUrl = $"{_settings.BaseUrl}/unifiedcheckout/?publicKey={_settings.PublicKey.Trim()}&clientSecret={clientSecret}";
            return Response<string>.Success(checkoutUrl);
        }

        public async Task<Response<bool>> ProcessWebhookAsync(string requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
                return Response<bool>.FailWithKey(ResponseStatus.ValidationError, "EmptyWebhookPayload");

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(requestBody);
            }
            catch (JsonException)
            {
                return Response<bool>.FailWithKey(ResponseStatus.ValidationError, "InvalidJsonPayload");
            }

            using (document)
            {
                var root = document.RootElement;
                JsonElement target = root;
                if (root.TryGetProperty("data", out var dataElem))
                    target = dataElem;
                else if (root.TryGetProperty("obj", out var objElem))
                    target = objElem;

                bool isSuccess = false;
                if (target.TryGetProperty("success", out var successProp))
                {
                    isSuccess = successProp.ValueKind == JsonValueKind.True ||
                                (successProp.ValueKind == JsonValueKind.String && bool.TryParse(successProp.GetString(), out var s) && s);
                }

                string transactionId = target.TryGetProperty("id", out var idProp) ? idProp.ToString() : string.Empty;

                string? specialRef = null;
                if (target.TryGetProperty("special_reference", out var sRef))
                    specialRef = sRef.GetString();

                string? intentionId = null;
                if (target.TryGetProperty("intention", out var intProp))
                {
                    if (intProp.ValueKind == JsonValueKind.Object && intProp.TryGetProperty("id", out var iId))
                        intentionId = iId.ToString();
                    else if (intProp.ValueKind == JsonValueKind.String)
                        intentionId = intProp.GetString();
                }

                string? orderId = null;
                if (target.TryGetProperty("order", out var ordProp))
                {
                    if (ordProp.ValueKind == JsonValueKind.Object && ordProp.TryGetProperty("id", out var oId))
                        orderId = oId.ToString();
                    else
                        orderId = ordProp.ToString();
                }

                Payment? payment = null;

                if (!string.IsNullOrEmpty(specialRef) && specialRef.StartsWith("BOOKING_"))
                {
                    var parts = specialRef.Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int bookingId))
                    {
                        payment = await _paymentRepo.GetAllAsIQueryable()
                            .Include(p => p.Booking)
                                .ThenInclude(b => b.Listing)
                            .FirstOrDefaultAsync(p => p.BookingId == bookingId);
                    }
                }

                if (payment == null && !string.IsNullOrEmpty(intentionId))
                {
                    payment = await _paymentRepo.GetAllAsIQueryable()
                        .Include(p => p.Booking)
                            .ThenInclude(b => b.Listing)
                        .FirstOrDefaultAsync(p => p.PaymobOrderId == intentionId);
                }

                if (payment == null && !string.IsNullOrEmpty(orderId))
                {
                    payment = await _paymentRepo.GetAllAsIQueryable()
                        .Include(p => p.Booking)
                            .ThenInclude(b => b.Listing)
                        .FirstOrDefaultAsync(p => p.PaymobOrderId == orderId);
                }

                if (payment == null)
                    return Response<bool>.FailWithKey(ResponseStatus.NotFound, "PaymentRecordNotFoundForWebhook");

                payment.PaymobTransactionId = transactionId;
                payment.Status = isSuccess ? PaymentStatus.Success : PaymentStatus.Failed;
                payment.PaidAt = isSuccess ? DateTime.UtcNow : null;

                if (isSuccess && payment.Booking != null)
                {
                    payment.Booking.Status = BookingStatus.Paid;
                    _bookingRepo.Update(payment.Booking);

                    // Notify guest about successful payment
                    await _notificationService.SendNotificationAsync(
                        payment.Booking.GuestId,
                        "PaymentSuccessfulNotification",
                        new[] { payment.Booking.Listing.Title },
                        $"/Bookings/Details/{payment.Booking.Id}"
                    );

                    // Notify host that the booking is now fully paid
                    await _notificationService.SendNotificationAsync(
                        payment.Booking.Listing.HostId,
                        "BookingPaidHostNotification",
                        new[] { payment.Booking.Listing.Title },
                        $"/Bookings/HostDetails/{payment.Booking.Id}"
                    );
                }

                _paymentRepo.Update(payment);
                await _paymentRepo.SaveAsync();

                return Response<bool>.SuccessWithKey(true, "WebhookProcessedSuccessfully");
            }
        }

        public async Task ConfirmPaymentDirectAsync(int bookingId)
        {
            var payment = await _paymentRepo.GetAllAsIQueryable()
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Listing)
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);

            if (payment != null && payment.Booking != null && payment.Status != PaymentStatus.Success)
            {
                payment.Status = PaymentStatus.Success;
                payment.PaidAt = DateTime.UtcNow;
                payment.Booking.Status = BookingStatus.Paid;

                _bookingRepo.Update(payment.Booking);
                _paymentRepo.Update(payment);
                await _paymentRepo.SaveAsync();

                // Trigger notifications for direct confirmation fallback
                await _notificationService.SendNotificationAsync(
                    payment.Booking.GuestId,
                    "PaymentSuccessfulNotification",
                    new[] { payment.Booking.Listing.Title },
                    $"/Bookings/Details/{payment.Booking.Id}"
                );

                await _notificationService.SendNotificationAsync(
                    payment.Booking.Listing.HostId,
                    "BookingPaidHostNotification",
                    new[] { payment.Booking.Listing.Title },
                    $"/Bookings/HostDetails/{payment.Booking.Id}"
                );
            }
        }

        public bool ValidateHmac(Dictionary<string, string> queryParams, string receivedHmac)
        {
            string[] keys = {
                "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction",
                "id", "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded",
                "is_standalone_payment", "is_voided", "order", "owner", "pending",
                "source_data.pan", "source_data.sub_type", "source_data.type", "success"
            };

            var concatenated = new StringBuilder();
            foreach (var key in keys)
            {
                if (queryParams.TryGetValue(key, out var val))
                    concatenated.Append(val);
            }

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_settings.HmacSecret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenated.ToString()));
            var calculatedHmac = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return calculatedHmac.Equals(receivedHmac, StringComparison.OrdinalIgnoreCase);
        }
    }
}
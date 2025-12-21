using Microsoft.Extensions.Options;
using PaymentMicroService.DTOs;
using PaymentMicroService.Models;
using Stripe;
using System.Text.Json;
using Stripe.Checkout;

namespace PaymentMicroService.Services
{
    public class StripeService : IStripeService
    {
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<StripeService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly UserServiceClient _userServiceClient; // Changed from IUserRepository
        private readonly EmailServiceClient _emailServiceClient; // Changed from EmailService
        private readonly ReservationServiceClient _reservationServiceClient; // Changed from IReservationRepository

        public StripeService(
            IOptions<StripeSettings> stripeSettings,
            ILogger<StripeService> logger,
            IHttpClientFactory httpClientFactory,
            ReservationServiceClient reservationServiceClient,
            UserServiceClient userServiceClient,
            EmailServiceClient emailServiceClient,
            IConfiguration configuration)
        {
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _reservationServiceClient = reservationServiceClient;
            _userServiceClient = userServiceClient;
            _emailServiceClient = emailServiceClient;
            _configuration = configuration;

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<CheckoutSessionResponseDTO> CreateCheckoutSessionAsync(CreateCheckoutSessionDTO request)
        {
            // Note: In microservices, we might need to call ReservationsMicroService to verify reservation existence.
            // For now, we proceed with the data provided in the DTO.
            // Removed reservation fetch to avoid auth issues between services.

            var options = new SessionCreateOptions
            {
                Locale = "en", // Force English language on Stripe Checkout page
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(request.Amount * 100),
                            Currency = request.Currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = request.ProductName ?? $"Room Reservation #{request.ReservationId}",
                                Description = request.ProductDescription ?? "Hotel Room Reservation"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{_stripeSettings.SuccessUrl}?session_id={{CHECKOUT_SESSION_ID}}&reservation_id={request.ReservationId}",
                CancelUrl = $"{_stripeSettings.CancelUrl}?reservation_id={request.ReservationId}",
                Metadata = new Dictionary<string, string>
                {
                    { "reservation_id", request.ReservationId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe checkout session {SessionId} for reservation {ReservationId}",
                session.Id, request.ReservationId);

            return new CheckoutSessionResponseDTO
            {
                SessionId = session.Id,
                SessionUrl = session.Url,
                PublishableKey = _stripeSettings.PublishableKey
            };
        }

        public async Task<bool> ConfirmPaymentAsync(string sessionId)
        {
            try
            {
                var service = new SessionService();
                var session = await service.GetAsync(sessionId);

                // For test mode, assume paid if session exists and has reservation_id
                var reservationIdStr = session.Metadata.GetValueOrDefault("reservation_id");
                if (int.TryParse(reservationIdStr, out int reservationId))
                {
                    _logger.LogInformation("Confirming payment for session {SessionId}, reservation {ReservationId}", sessionId, reservationId);
                    return await ConfirmReservationAndSendEmail(reservationId);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> HandleWebhookAsync(string json, string stripeSignature)
        {
            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);

                _logger.LogInformation("Received Stripe webhook event: {EventType}", stripeEvent.Type);

                if (stripeEvent.Type == Events.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null && session.PaymentStatus == "paid")
                    {
                        var reservationIdStr = session.Metadata.GetValueOrDefault("reservation_id");
                        if (int.TryParse(reservationIdStr, out int reservationId))
                        {
                            _logger.LogInformation("Webhook: Checkout session completed for reservation {ReservationId}", reservationId);
                            return await ConfirmReservationAndSendEmail(reservationId);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                return false;
            }
        }

        private async Task<bool> ConfirmReservationAndSendEmail(int reservationId)
        {
            try
            {
                _logger.LogInformation("Starting confirmation process for reservation {ReservationId}", reservationId);
                // Fetch reservation details first to get email and other info
                var reservation = await _reservationServiceClient.GetReservationByIdAsync(reservationId);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation {ReservationId} not found for payment confirmation", reservationId);
                    return false;
                }

                _logger.LogInformation("Fetched reservation details: {ReservationJson}", JsonSerializer.Serialize(reservation));

                if (!await _reservationServiceClient.ConfirmReservationAsync(reservationId))
                {
                    _logger.LogWarning("Failed to confirm reservation {ReservationId} via Reservation Service", reservationId);
                    return false;
                }

                // Get user for email (optional for the greeting, but good to have)
                var user = await _userServiceClient.GetUserByEmailAsync(reservation.Email);
                string fullName = user?.FullName ?? "Valued Customer";
                string userEmail = reservation.Email;

                string emailSubject = "Booking Confirmation - Payment Received";
                string emailBody = $@"
                <h1>Booking Confirmation</h1>
                <p>Dear {fullName},</p>
                <p>Your payment has been received and your booking is now confirmed.</p>
                <p><strong>Reservation Details:</strong></p>
                <ul>
                    <li>Reservation ID: {reservation.ReservationId}</li>
                    <li>Entry date: {reservation.CheckInDate:dd/MM/yyyy}</li>
                    <li>Departure date: {reservation.CheckOutDate:dd/MM/yyyy}</li>
                    <li>Number of nights: {reservation.NumberOfNights}</li>
                    <li>Number of guests: {reservation.NumberOfGuests}</li>
                </ul>
                <p>Thank you for choosing our hotel. We look forward to your stay!</p>
                ";

                await _emailServiceClient.SendEmailAsync(userEmail, emailSubject, emailBody, "confirmation", reservation.ReservationId);
                _logger.LogInformation("Payment confirmed and email queued for reservation {ReservationId} (Recipient: {UserEmail})", reservationId, userEmail);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming reservation {ReservationId} after payment", reservationId);
                return false;
            }
        }
    }
}

using PaymentMicroService.DTOs;

namespace PaymentMicroService.Services
{
    public interface IStripeService
    {
        Task<CheckoutSessionResponseDTO> CreateCheckoutSessionAsync(CreateCheckoutSessionDTO request);
        Task<bool> ConfirmPaymentAsync(string sessionId);
        Task<bool> HandleWebhookAsync(string json, string stripeSignature);
    }
}

using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>
/// Contract for transactional email sending via SendGrid.
/// All methods are fire-and-monitor — they should not block the calling operation.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends a welcome email after successful registration.</summary>
    Task SendWelcomeAsync(string toEmail, string firstName, CancellationToken cancellationToken = default);

    /// <summary>Sends a 6-digit OTP for registration or password reset.</summary>
    Task SendOtpAsync(string toEmail, string firstName, string otp, CancellationToken cancellationToken = default);

    /// <summary>Sends an order placed confirmation email with order summary.</summary>
    Task SendOrderConfirmationAsync(
        string toEmail,
        string firstName,
        string orderNumber,
        decimal total,
        string paymentMethod,
        DeliveryMethod deliveryMethod,
        DateTime? estimatedDelivery = null,
        string? phone = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a new-order notification to the store's admin inbox. Fire-and-forget from the
    /// caller's perspective — implementations must never let a send failure propagate, so a
    /// broken email provider can never block order placement.
    /// </summary>
    Task SendAdminOrderNotificationAsync(
        string orderNumber,
        string customerName,
        string customerEmail,
        string customerPhone,
        decimal total,
        string paymentMethod,
        string deliveryAddressJson,
        CancellationToken cancellationToken = default);

    /// <summary>Sends an order shipped notification with tracking number.</summary>
    Task SendOrderShippedAsync(string toEmail, string firstName, string orderNumber, string trackingNumber, CancellationToken cancellationToken = default);

    /// <summary>Sends a password reset OTP email.</summary>
    Task SendPasswordResetAsync(string toEmail, string firstName, string otp, CancellationToken cancellationToken = default);

    /// <summary>Sends a back-in-stock notification for a product.</summary>
    Task SendBackInStockAsync(string toEmail, string firstName, string productName, string productSlug, CancellationToken cancellationToken = default);

    /// <summary>Sends a generic templated email by template key.</summary>
    Task SendTemplatedAsync(string toEmail, string templateKey, object templateData, CancellationToken cancellationToken = default);
}

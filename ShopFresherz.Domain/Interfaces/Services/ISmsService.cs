namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>
/// Contract for SMS delivery via Termii (primary) with Africa's Talking as fallback.
/// Used for OTP delivery and order status notifications.
/// </summary>
public interface ISmsService
{
    /// <summary>Sends a 6-digit OTP via SMS to the given phone number.</summary>
    Task SendOtpAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);

    /// <summary>Sends an order status update SMS.</summary>
    Task SendOrderUpdateAsync(
        string phoneNumber,
        string orderNumber,
        string status,
        CancellationToken cancellationToken = default);

    /// <summary>Sends an order delivery/shipping notification SMS.</summary>
    Task SendDeliveryNotificationAsync(
        string phoneNumber,
        string orderNumber,
        string trackingNumber,
        CancellationToken cancellationToken = default);
}

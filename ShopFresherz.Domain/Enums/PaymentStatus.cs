namespace ShopFresherz.Domain.Enums;

/// <summary>Represents the payment state of an order.</summary>
public enum PaymentStatus
{
    /// <summary>No payment received yet.</summary>
    Unpaid = 0,

    /// <summary>Payment successfully confirmed by gateway webhook.</summary>
    Paid = 1,

    /// <summary>Payment has been refunded to customer.</summary>
    Refunded = 2,

    /// <summary>
    /// Confirmation claimed by confirm-order or the webhook and is being verified.
    /// Acts as an atomic lock so a concurrent call cannot also process the same payment.
    /// </summary>
    Verifying = 3
}

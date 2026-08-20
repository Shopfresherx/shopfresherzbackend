namespace ShopFresherz.Domain.Enums;

/// <summary>Supported payment methods at checkout.</summary>
public enum PaymentMethod
{
    /// <summary>Debit/credit card via Flutterwave.</summary>
    Card = 0,

    /// <summary>Bank transfer via Flutterwave.</summary>
    BankTransfer = 1,

    /// <summary>USSD code payment via Flutterwave.</summary>
    USSD = 2,

    /// <summary>Cash payment on delivery (Lagos, Abuja, Port Harcourt only).</summary>
    PayOnDelivery = 3
}

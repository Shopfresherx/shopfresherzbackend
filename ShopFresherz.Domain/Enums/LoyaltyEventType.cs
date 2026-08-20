namespace ShopFresherz.Domain.Enums;

/// <summary>Classifies a loyalty points transaction.</summary>
public enum LoyaltyEventType
{
    /// <summary>Points awarded for a delivered order (1 pt per ₦100).</summary>
    Earned = 0,

    /// <summary>Points redeemed against an order (100 pts = ₦100).</summary>
    Redeemed = 1,

    /// <summary>Points expired after 12-month inactivity window.</summary>
    Expired = 2,

    /// <summary>Bonus points: first purchase, birthday, referral, etc.</summary>
    Bonus = 3
}

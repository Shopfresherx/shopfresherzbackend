using ShopFresherz.Domain.Common;
using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// Represents a registered platform user (customer, admin, or super admin).
/// </summary>
public class User : BaseEntity
{
    /// <summary>Gets or sets the user's email address (lowercase normalised, unique).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's phone number in international format (+234...).</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the BCrypt cost-12 hashed password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the CDN URL to the user's avatar image.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Gets or sets the user's access role on the platform.</summary>
    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>Gets or sets a value indicating whether email or phone has been verified.</summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>Gets or sets the user's current loyalty points balance.</summary>
    public int LoyaltyPoints { get; set; } = 0;

    /// <summary>Gets or sets the Google OAuth subject identifier.</summary>
    public string? GoogleId { get; set; }

    /// <summary>Gets or sets the hashed refresh token (SHA-256).</summary>
    public string? RefreshTokenHash { get; set; }

    /// <summary>Gets or sets the UTC expiry of the current refresh token.</summary>
    public DateTime? RefreshTokenExpires { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the user's last successful login.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Gets or sets the SHA-256 hash of the active password-reset OTP.</summary>
    public string? PasswordResetOtpHash { get; set; }

    /// <summary>Gets or sets the UTC expiry of the active password-reset OTP.</summary>
    public DateTime? PasswordResetOtpExpires { get; set; }

    /// <summary>Gets or sets the collection of orders placed by this user.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>Gets or sets the collection of saved delivery addresses.</summary>
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    /// <summary>Gets or sets the collection of product reviews submitted by this user.</summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Gets or sets the user's wishlist items.</summary>
    public ICollection<Wishlist> Wishlist { get; set; } = new List<Wishlist>();

    /// <summary>Gets or sets the loyalty points transaction history.</summary>
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();

    /// <summary>Gets or sets the collection of saved bank cards.</summary>
    public ICollection<SavedCard> SavedCards { get; set; } = new List<SavedCard>();

    /// <summary>Gets or sets whether order update notifications are enabled.</summary>
    public bool NotificationOrderUpdates { get; set; } = true;

    /// <summary>Gets or sets whether promotional notifications are enabled.</summary>
    public bool NotificationPromotions { get; set; } = true;

    /// <summary>Gets or sets whether back-in-stock notifications are enabled.</summary>
    public bool NotificationBackInStock { get; set; } = true;

    /// <summary>Gets or sets whether wishlist reminder notifications are enabled.</summary>
    public bool NotificationWishlistReminders { get; set; } = true;

    /// <summary>Gets or sets whether review reminder notifications are enabled.</summary>
    public bool NotificationReviewReminders { get; set; } = true;
}

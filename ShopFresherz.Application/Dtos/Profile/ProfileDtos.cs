namespace ShopFresherz.Application.Dtos.Profile;

using ShopFresherz.Application.Common;
using ShopFresherz.Domain.Enums;

/// <summary>User account profile DTO.</summary>
public sealed class UserProfileDto
{
    /// <summary>Gets or sets the user ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the avatar URL.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Gets or sets the user role.</summary>
    public UserRole Role { get; set; }

    /// <summary>Gets or sets whether the account is verified.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Gets or sets the current loyalty points balance.</summary>
    public int LoyaltyPoints { get; set; }

    /// <summary>Gets or sets the account creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>User loyalty points summary and transaction history.</summary>
public sealed class LoyaltyDto
{
    /// <summary>Gets or sets the current loyalty points balance.</summary>
    public int Balance { get; set; }

    /// <summary>Gets or sets the paged transaction history.</summary>
    public PagedResult<LoyaltyTransactionDto> Transactions { get; set; } = null!;
}

/// <summary>Single loyalty transaction DTO.</summary>
public sealed class LoyaltyTransactionDto
{
    /// <summary>Gets or sets the transaction ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the transaction event type.</summary>
    public LoyaltyEventType EventType { get; set; }

    /// <summary>Gets or sets the points delta.</summary>
    public int Points { get; set; }

    /// <summary>Gets or sets the transaction description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the transaction creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when the points expire.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Gets or sets the remaining points balance after this transaction.</summary>
    public int RemainingPoints { get; set; }
}

/// <summary>Saved address DTO.</summary>
public sealed class AddressDto
{
    /// <summary>Gets or sets the address ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the friendly label (e.g., "Home", "Office").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the first address line.</summary>
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional second address line.</summary>
    public string? Line2 { get; set; }

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the Nigerian state.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the default address.</summary>
    public bool IsDefault { get; set; }
}

/// <summary>Request payload for updating a user's profile.</summary>
public sealed class UpdateProfileRequest
{
    /// <summary>Gets or sets the updated first name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Gets or sets the updated last name.</summary>
    public string? LastName { get; set; }

    /// <summary>Gets or sets the updated phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the updated avatar URL.</summary>
    public string? AvatarUrl { get; set; }
}

/// <summary>Request payload for changing the account password.</summary>
public sealed class ChangePasswordRequest
{
    /// <summary>Gets or sets the current password for verification.</summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the new password.</summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the new password confirmation.</summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>Request payload for updating a saved address (all fields optional).</summary>
public sealed class UpdateAddressRequest
{
    /// <summary>Gets or sets the updated address label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the updated first address line.</summary>
    public string? Line1 { get; set; }

    /// <summary>Gets or sets the updated second address line.</summary>
    public string? Line2 { get; set; }

    /// <summary>Gets or sets the updated city.</summary>
    public string? City { get; set; }

    /// <summary>Gets or sets the updated Nigerian state.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the updated postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Gets or sets a value indicating whether this should be the default address.</summary>
    public bool? IsDefault { get; set; }
}

/// <summary>Request payload for adding a new saved address.</summary>
public sealed class CreateAddressRequest
{
    /// <summary>Gets or sets the address label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the first address line.</summary>
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the second address line.</summary>
    public string? Line2 { get; set; }

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the Nigerian state.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Gets or sets a value indicating whether this should be set as the default address.</summary>
    public bool IsDefault { get; set; } = false;
}

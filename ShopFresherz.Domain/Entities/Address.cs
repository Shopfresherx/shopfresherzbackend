using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>
/// A saved delivery address belonging to a registered user.
/// At checkout a JSON snapshot is written to Order.DeliveryAddressJson.
/// </summary>
public class Address : BaseEntity
{
    /// <summary>Gets or sets the owning user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the friendly label (e.g., "Home", "Office").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the first address line (street / house number).</summary>
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional second address line.</summary>
    public string? Line2 { get; set; }

    /// <summary>Gets or sets the city name.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the Nigerian state (e.g., Lagos, Abuja FCT).</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the user's default delivery address.</summary>
    public bool IsDefault { get; set; } = false;
}

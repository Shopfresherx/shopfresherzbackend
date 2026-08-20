namespace ShopFresherz.Domain.ValueObjects;

/// <summary>
/// An immutable snapshot of a delivery address captured at order placement time.
/// Serialised as JSON into Order.DeliveryAddressJson — decoupled from the mutable saved address.
/// </summary>
public sealed record DeliveryAddress(
    string Label,
    string Line1,
    string? Line2,
    string City,
    string State,
    string? PostalCode,
    string RecipientName,
    string RecipientPhone);

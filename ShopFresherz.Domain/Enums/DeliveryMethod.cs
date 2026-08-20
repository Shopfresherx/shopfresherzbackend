namespace ShopFresherz.Domain.Enums;

/// <summary>Delivery options available at checkout.</summary>
public enum DeliveryMethod
{
    /// <summary>Standard delivery 3–5 business days. ₦1,500 flat fee.</summary>
    Standard = 0,

    /// <summary>Express delivery 1–3 business days. ₦3,500 flat fee.</summary>
    Express = 1,

    /// <summary>Customer collects from store. Free.</summary>
    Pickup = 2
}

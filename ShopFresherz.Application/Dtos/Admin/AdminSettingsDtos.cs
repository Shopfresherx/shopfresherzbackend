using System.Text.Json;

namespace ShopFresherz.Application.Dtos.Admin;

public sealed class AdminSettingsDto
{
    public StoreSettingsDto Store { get; set; } = new();

    public PaymentSettingsDto Payment { get; set; } = new();

    public ShippingSettingsDto Shipping { get; set; } = new();

    public NotificationSettingsDto Notifications { get; set; } = new();

    public SeoSettingsDto Seo { get; set; } = new();

    public SecuritySettingsDto Security { get; set; } = new();

    public MaintenanceSettingsDto Maintenance { get; set; } = new();
}

public sealed class AdminSettingsUpdateRequest
{
    public StoreSettingsDto? Store { get; set; }

    public PaymentSettingsDto? Payment { get; set; }

    public ShippingSettingsDto? Shipping { get; set; }

    public NotificationSettingsDto? Notifications { get; set; }

    public SeoSettingsDto? Seo { get; set; }

    public SecuritySettingsDto? Security { get; set; }

    public MaintenanceSettingsDto? Maintenance { get; set; }
}

public sealed class StoreSettingsDto
{
    public string StoreName { get; set; } = "ShopFresherz";

    public string SupportEmail { get; set; } = "support@shopfresherz.com";

    public string SupportPhone { get; set; } = string.Empty;

    public string Currency { get; set; } = "NGN";

    public string TimeZone { get; set; } = "Africa/Lagos";

    public string? LogoUrl { get; set; }

    public string? ContactAddress { get; set; }
}

public sealed class PaymentSettingsDto
{
    public bool FlutterwaveEnabled { get; set; } = true;

    public bool BankTransferEnabled { get; set; } = false;

    public string DefaultProvider { get; set; } = "Flutterwave";
}

public sealed class ShippingSettingsDto
{
    public decimal DefaultDeliveryFee { get; set; } = 0;

    public decimal FreeShippingThreshold { get; set; } = 0;

    public int EstimatedDeliveryDaysMin { get; set; } = 1;

    public int EstimatedDeliveryDaysMax { get; set; } = 5;

    public List<string> SupportedStates { get; set; } = [];
}

public sealed class NotificationSettingsDto
{
    public bool EmailEnabled { get; set; } = true;

    public bool SmsEnabled { get; set; } = false;

    public bool OrderUpdatesEnabled { get; set; } = true;

    public bool StockAlertsEnabled { get; set; } = true;

    public bool MarketingEnabled { get; set; } = true;
}

public sealed class SeoSettingsDto
{
    public string DefaultTitle { get; set; } = "ShopFresherz";

    public string DefaultDescription { get; set; } = "Shop gadgets, phones, laptops, and accessories in Nigeria.";

    public string? DefaultImageUrl { get; set; }
}

public sealed class SecuritySettingsDto
{
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    public int RefreshTokenExpiryDays { get; set; } = 7;

    public bool RequireEmailVerification { get; set; } = false;

    public bool AdminMfaRequired { get; set; } = false;
}

public sealed class MaintenanceSettingsDto
{
    public bool Enabled { get; set; } = false;

    public string? Message { get; set; }
}

public sealed class AdminSettingsSectionUpdateRequest
{
    public JsonElement Value { get; set; }
}

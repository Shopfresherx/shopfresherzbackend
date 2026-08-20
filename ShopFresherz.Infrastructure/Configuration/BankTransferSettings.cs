namespace ShopFresherz.Infrastructure.Configuration;

/// <summary>Configuration for manual bank-transfer checkout.</summary>
public sealed class BankTransferSettings
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}

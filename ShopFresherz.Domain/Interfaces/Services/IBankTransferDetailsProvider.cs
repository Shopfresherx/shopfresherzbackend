namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>Provides the merchant bank account shown for manual transfers.</summary>
public interface IBankTransferDetailsProvider
{
    BankTransferDetails GetDetails();
}

public sealed record BankTransferDetails(
    string BankName,
    string AccountNumber,
    string AccountName);

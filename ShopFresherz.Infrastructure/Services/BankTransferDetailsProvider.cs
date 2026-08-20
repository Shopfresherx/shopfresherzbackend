using Microsoft.Extensions.Options;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;

namespace ShopFresherz.Infrastructure.Services;

public sealed class BankTransferDetailsProvider : IBankTransferDetailsProvider
{
    private readonly BankTransferSettings _settings;

    public BankTransferDetailsProvider(IOptions<BankTransferSettings> settings)
    {
        _settings = settings.Value;
    }

    public BankTransferDetails GetDetails() => new(
        _settings.BankName,
        _settings.AccountNumber,
        _settings.AccountName);
}

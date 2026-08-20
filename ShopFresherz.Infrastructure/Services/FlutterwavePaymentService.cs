using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>Fallback hosted checkout integration for Flutterwave.</summary>
public sealed class FlutterwavePaymentService : IFlutterwavePaymentService
{
    private readonly HttpClient _http;
    private readonly FlutterwaveOptions _options;
    private readonly ILogger<FlutterwavePaymentService> _logger;

    public FlutterwavePaymentService(
        HttpClient http,
        IOptions<FlutterwaveOptions> options,
        ILogger<FlutterwavePaymentService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string PublicKey => _options.PublicKey;

    /// <inheritdoc />
    public string CallbackUrl => _options.CallbackUrl;

    /// <summary>Initialises a Flutterwave hosted payment and returns the payment link.</summary>
    public async Task<PaymentInitResult?> InitializeAsync(
        string email,
        string name,
        string phone,
        Guid orderId,
        string orderNumber,
        decimal totalNgn,
        CancellationToken cancellationToken)
    {
        try
        {
            object payload = new
            {
                tx_ref = orderNumber,
                amount = totalNgn,
                currency = "NGN",
                redirect_url = _options.CallbackUrl,
                customer = new { email, name, phonenumber = phone },
                customizations = new
                {
                    title = "ShopFresherz",
                    logo = "https://shopfresherz.com/logo.png",
                },
                meta = new { orderId = orderId.ToString() },
            };

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.SecretKey);

            HttpResponseMessage response = await _http.PostAsJsonAsync(
                "https://api.flutterwave.com/v3/payments",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Flutterwave init failed for order {OrderNumber}: {Status} {Body}",
                    orderNumber,
                    response.StatusCode,
                    body);
                return null;
            }

            using JsonDocument doc = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));

            string? link = doc.RootElement
                .GetProperty("data")
                .GetProperty("link")
                .GetString();

            return string.IsNullOrWhiteSpace(link)
                ? null
                : new PaymentInitResult(link, orderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flutterwave exception for order {OrderNumber}", orderNumber);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<FlutterwaveVerificationResult?> VerifyAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpRequestMessage request = new(
                HttpMethod.Get,
                $"https://api.flutterwave.com/v3/transactions/{transactionId}/verify");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

            HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);

            string raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Flutterwave verify failed for transaction {TransactionId}: {Status} {Body}",
                    transactionId,
                    response.StatusCode,
                    raw);
                return null;
            }

            using JsonDocument doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("data", out JsonElement data))
            {
                return null;
            }

            string status = data.TryGetProperty("status", out JsonElement s) ? s.GetString() ?? string.Empty : string.Empty;
            string txRef = data.TryGetProperty("tx_ref", out JsonElement t) ? t.GetString() ?? string.Empty : string.Empty;
            decimal amount = data.TryGetProperty("amount", out JsonElement a) ? a.GetDecimal() : 0m;
            string currency = data.TryGetProperty("currency", out JsonElement c) ? c.GetString() ?? string.Empty : string.Empty;

            return new FlutterwaveVerificationResult(status, txRef, amount, currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flutterwave verify exception for transaction {TransactionId}", transactionId);
            return null;
        }
    }
}

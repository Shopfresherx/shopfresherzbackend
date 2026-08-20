using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>SMS delivery service backed by the Termii API.</summary>
public sealed class TermiiSmsService : ISmsService
{
    private readonly HttpClient _http;
    private readonly TermiiOptions _options;
    private readonly ILogger<TermiiSmsService> _logger;

    public TermiiSmsService(
        HttpClient http,
        IOptions<TermiiOptions> options,
        ILogger<TermiiSmsService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendOtpAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default)
    {
        await SendAsync(
            phoneNumber,
            $"Your ShopFresherz verification code is: {otp}. Valid for 10 minutes. Do not share this code.",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendOrderUpdateAsync(
        string phoneNumber,
        string orderNumber,
        string status,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            phoneNumber,
            $"ShopFresherz: Your order {orderNumber} is now {status}. Visit shopfresherz.com to track.",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendDeliveryNotificationAsync(
        string phoneNumber,
        string orderNumber,
        string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            phoneNumber,
            $"ShopFresherz: Order {orderNumber} has been shipped! Tracking: {trackingNumber}",
            cancellationToken);
    }

    private async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogInformation("Termii not configured — skipping SMS to {Phone}", phoneNumber);
            return;
        }

        try
        {
            object payload = new
            {
                to = phoneNumber,
                from = _options.SenderId,
                sms = message,
                type = "plain",
                api_key = _options.ApiKey,
                channel = "generic",
            };

            HttpResponseMessage response = await _http.PostAsJsonAsync(
                "https://api.ng.termii.com/api/sms/send",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Termii SMS failed to {Phone}: {Status} {Body}",
                    phoneNumber,
                    response.StatusCode,
                    body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Termii SMS exception sending to {Phone}", phoneNumber);
        }
    }
}

using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShopFresherz.Application.Features.Payments.Commands;
using ShopFresherz.Infrastructure.Configuration;

namespace ShopFresherz.API.Controllers;

/// <summary>Receives and verifies inbound payment gateway webhook events.</summary>
[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly FlutterwaveOptions _flutterwaveOptions;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IMediator mediator,
        IOptions<FlutterwaveOptions> flutterwaveOptions,
        ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _flutterwaveOptions = flutterwaveOptions.Value;
        _logger = logger;
    }

    /// <summary>Receives Flutterwave payment webhook events.</summary>
    [AllowAnonymous]
    [HttpPost("webhook/flutterwave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FlutterwaveWebhook(CancellationToken cancellationToken)
    {
        try
        {
            Request.EnableBuffering();
            using StreamReader reader = new(Request.Body, Encoding.UTF8, leaveOpen: true);
            string rawBody = await reader.ReadToEndAsync(cancellationToken);
            Request.Body.Position = 0;

            string? signature = Request.Headers["verif-hash"].FirstOrDefault();
            if (!SignatureMatches(signature, _flutterwaveOptions.WebhookSecret))
            {
                return Ok();
            }

            using JsonDocument doc = JsonDocument.Parse(rawBody);
            JsonElement root = doc.RootElement;

            string eventType = root.TryGetProperty("event", out JsonElement eventElement)
                ? eventElement.GetString() ?? string.Empty
                : string.Empty;
            if (!string.Equals(eventType, "charge.completed", StringComparison.OrdinalIgnoreCase) ||
                !root.TryGetProperty("data", out JsonElement data))
            {
                return Ok();
            }

            string status = data.TryGetProperty("status", out JsonElement statusElement)
                ? statusElement.GetString() ?? string.Empty
                : string.Empty;
            if (!string.Equals(status, "successful", StringComparison.OrdinalIgnoreCase))
            {
                return Ok();
            }

            string reference = data.TryGetProperty("tx_ref", out JsonElement referenceElement)
                ? referenceElement.GetString() ?? string.Empty
                : string.Empty;
            decimal amount = data.TryGetProperty("amount", out JsonElement amountElement) &&
                amountElement.TryGetDecimal(out decimal parsedAmount)
                    ? parsedAmount
                    : 0m;
            string currency = data.TryGetProperty("currency", out JsonElement currencyElement)
                ? currencyElement.GetString() ?? string.Empty
                : string.Empty;
            string email = data.TryGetProperty("customer", out JsonElement customer) &&
                customer.TryGetProperty("email", out JsonElement emailElement)
                    ? emailElement.GetString() ?? string.Empty
                    : string.Empty;

            await _mediator.Send(
                new HandlePaymentWebhookCommand(eventType, reference, amount, currency, email),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flutterwave webhook processing failed; acknowledging the event.");
        }

        return Ok();
    }

    private static bool SignatureMatches(string? signature, string expected)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        byte[] actualBytes = Encoding.UTF8.GetBytes(signature);
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        return actualBytes.Length == expectedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}

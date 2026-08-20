namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>Flutterwave checkout integration (hosted redirect + inline popup verification).</summary>
public interface IFlutterwavePaymentService
{
    /// <summary>Gets the configured Flutterwave public key, returned to the frontend for the inline popup.</summary>
    string PublicKey { get; }

    /// <summary>Gets the configured post-payment callback URL for the frontend to fall back to.</summary>
    string CallbackUrl { get; }

    Task<PaymentInitResult?> InitializeAsync(
        string email,
        string name,
        string phone,
        Guid orderId,
        string orderNumber,
        decimal totalNgn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a completed transaction server-side via Flutterwave's verify endpoint.
    /// Returns null if the gateway call fails or the transaction cannot be retrieved.
    /// </summary>
    Task<FlutterwaveVerificationResult?> VerifyAsync(
        string transactionId,
        CancellationToken cancellationToken = default);
}

/// <summary>Result from payment initialisation.</summary>
public sealed record PaymentInitResult(string AuthorisationUrl, string Reference);

/// <summary>Authoritative transaction details returned by Flutterwave's verify endpoint.</summary>
public sealed record FlutterwaveVerificationResult(
    string Status,
    string TxRef,
    decimal Amount,
    string Currency);

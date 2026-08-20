using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopFresherz.Domain.Enums;
using ShopFresherz.Domain.Interfaces.Services;
using ShopFresherz.Infrastructure.Configuration;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>Transactional email delivery through Brevo's REST API.</summary>
public sealed class BrevoEmailService : IEmailService
{
    private const string Endpoint = "https://api.brevo.com/v3/smtp/email";
    private const string AdminNotificationEmail = "info@shopfresherz.com";

    private readonly HttpClient _http;
    private readonly BrevoSettings _settings;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(
        HttpClient http,
        IOptions<BrevoSettings> settings,
        ILogger<BrevoEmailService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task SendWelcomeAsync(string toEmail, string firstName, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, firstName, "Welcome to ShopFresherz! 🎉",
            Wrap("Welcome to ShopFresherz! 🎉", WelcomeBody(firstName)),
            cancellationToken);

    public Task SendOtpAsync(string toEmail, string firstName, string otp, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, firstName, "Verify your ShopFresherz account",
            Wrap("Verify your ShopFresherz account", VerifyEmailBody(firstName, otp)),
            cancellationToken);

    public Task SendOrderConfirmationAsync(
        string toEmail,
        string firstName,
        string orderNumber,
        decimal total,
        string paymentMethod,
        DeliveryMethod deliveryMethod,
        DateTime? estimatedDelivery = null,
        string? phone = null,
        CancellationToken cancellationToken = default)
    {
        string subject = $"Order Confirmed — {orderNumber} 🎉";
        return SendAsync(toEmail, firstName, subject,
            Wrap(subject, OrderConfirmationBody(firstName, orderNumber, total, paymentMethod, deliveryMethod, estimatedDelivery, phone)),
            cancellationToken);
    }

    public async Task SendAdminOrderNotificationAsync(
        string orderNumber,
        string customerName,
        string customerEmail,
        string customerPhone,
        decimal total,
        string paymentMethod,
        string deliveryAddressJson,
        CancellationToken cancellationToken = default)
    {
        string subject = $"New Order — {orderNumber} ({paymentMethod})";

        // Build the body in its own try/catch so a failure here — malformed delivery-address
        // JSON, an unexpected field type, or anything else in this synchronous construction
        // path — can never throw into the caller before SendAsync (and its own catch-all)
        // even gets a chance to run. Falls back to a body without the address section rather
        // than skipping the admin notification entirely.
        string body;
        try
        {
            body = Wrap(subject, AdminOrderNotificationBody(
                orderNumber, customerName, customerEmail, customerPhone, total, paymentMethod, deliveryAddressJson));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to build admin order-notification body for order {OrderNumber}; sending a fallback notification instead.",
                orderNumber);
            body = Wrap(subject, AdminOrderNotificationFallbackBody(orderNumber, customerName, customerEmail, customerPhone, total, paymentMethod));
        }

        await SendAsync(AdminNotificationEmail, "ShopFresherz Admin", subject, body, cancellationToken);
    }

    public Task SendOrderShippedAsync(string toEmail, string firstName, string orderNumber, string trackingNumber, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, firstName, $"Your order {orderNumber} has shipped",
            Wrap($"Your order {orderNumber} has shipped", OrderShippedBody(firstName, orderNumber, trackingNumber)),
            cancellationToken);

    public Task SendPasswordResetAsync(string toEmail, string firstName, string otp, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, firstName, "Reset your ShopFresherz password",
            Wrap("Reset your ShopFresherz password", PasswordResetBody(firstName, otp)),
            cancellationToken);

    public Task SendBackInStockAsync(string toEmail, string firstName, string productName, string productSlug, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, firstName, $"{productName} is back in stock",
            Wrap($"{productName} is back in stock", BackInStockBody(firstName, productName, productSlug)),
            cancellationToken);

    public Task SendTemplatedAsync(string toEmail, string templateKey, object templateData, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, string.Empty, templateKey,
            Wrap(templateKey, TemplatedBody(templateKey, templateData)),
            cancellationToken);

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Brevo email skipped because recipient email is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            _logger.LogWarning("Brevo is not configured; email to {Email} was not sent.", toEmail);
            return;
        }

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, Endpoint);
            request.Headers.Add("api-key", _settings.ApiKey);
            request.Headers.Accept.ParseAdd("application/json");
            request.Content = JsonContent.Create(new
            {
                sender = new { name = _settings.FromName, email = _settings.FromEmail },
                to = new[] { new { email = toEmail, name = toName } },
                subject,
                htmlContent = htmlBody,
            });

            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Brevo email to {Email} failed with {StatusCode}: {ResponseBody}",
                    toEmail, response.StatusCode, responseBody);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Brevo email to {Email} timed out.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo email to {Email} failed.", toEmail);
        }
    }

    /// <summary>Wraps body content in the shared branded header/footer shell.</summary>
    private static string Wrap(string subject, string bodyContent) =>
        $"""
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>{Encode(subject)}</title>
        </head>
        <body style="margin:0;padding:0;background-color:#F5F2ED;font-family:Inter,-apple-system,BlinkMacSystemFont,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#F5F2ED;padding:40px 20px;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">

                  <!-- HEADER -->
                  <tr>
                    <td style="background:linear-gradient(135deg,#1A1A2E 0%,#242436 100%);border-radius:16px 16px 0 0;padding:32px 40px;text-align:center;">
                      <img src="https://res.cloudinary.com/dtcpnqmi9/image/upload/v1782151604/ShopFreshersV2LogoOrange_mvnyvo.png"
                           alt="ShopFresherz" width="48" height="48"
                           style="border-radius:10px;margin-bottom:12px;display:block;margin:0 auto 12px;">
                      <h1 style="margin:0;color:#FFFFFF;font-size:22px;font-weight:700;letter-spacing:-0.5px;">
                        ShopFresherz
                      </h1>
                      <p style="margin:4px 0 0;color:#888;font-size:11px;letter-spacing:2px;text-transform:uppercase;">
                        GADGET STORE
                      </p>
                    </td>
                  </tr>

                  <!-- ORANGE ACCENT BAR -->
                  <tr>
                    <td style="background:linear-gradient(90deg,#F97316,#EA580C);height:3px;"></td>
                  </tr>

                  <!-- BODY -->
                  <tr>
                    <td style="background:#FFFFFF;padding:40px;border-radius:0 0 16px 16px;">
                      {bodyContent}
                    </td>
                  </tr>

                  <!-- FOOTER -->
                  <tr>
                    <td style="padding:24px 0;text-align:center;">
                      <p style="margin:0 0 8px;color:#888;font-size:12px;">
                        © 2026 ShopFresherz — Fresherz Gadgets Hub Ltd
                      </p>
                      <p style="margin:0 0 8px;color:#888;font-size:12px;">
                        📞 +234 907 530 8722 ·
                        <a href="https://shopfresherz.com" style="color:#F97316;text-decoration:none;">
                          shopfresherz.com
                        </a>
                      </p>
                      <p style="margin:0;color:#AAA;font-size:11px;">
                        If you did not request this email, please ignore it.
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string WelcomeBody(string firstName) =>
        $"""
        <div style="text-align:center;margin-bottom:32px;">
          <div style="width:64px;height:64px;background:#FFF3E0;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;">
            <span style="font-size:28px;">🎉</span>
          </div>
          <h2 style="margin:0 0 8px;color:#111111;font-size:26px;font-weight:800;letter-spacing:-0.5px;">
            Welcome, {Encode(firstName)}!
          </h2>
          <p style="margin:0;color:#666;font-size:15px;line-height:1.6;">
            You're now part of the ShopFresherz family.
          </p>
        </div>

        <p style="color:#444;font-size:15px;line-height:1.7;margin:0 0 24px;">
          We're Nigeria's most trusted destination for premium gadgets and electronics.
          Get ready to discover the latest iPhones, laptops, gaming consoles,
          and accessories at the best prices.
        </p>

        <!-- FEATURE BOXES -->
        <table width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 32px;">
          <tr>
            <td width="33%" style="padding:0 6px 0 0;vertical-align:top;">
              <div style="background:#FFF8F3;border:1px solid #FED7AA;border-radius:12px;padding:16px;text-align:center;">
                <div style="font-size:24px;margin-bottom:8px;">🚀</div>
                <p style="margin:0;color:#111;font-size:13px;font-weight:600;">Fast Delivery</p>
                <p style="margin:4px 0 0;color:#888;font-size:11px;">Within Lagos 24hrs</p>
              </div>
            </td>
            <td width="33%" style="padding:0 3px;vertical-align:top;">
              <div style="background:#F0FDF4;border:1px solid #BBF7D0;border-radius:12px;padding:16px;text-align:center;">
                <div style="font-size:24px;margin-bottom:8px;">✅</div>
                <p style="margin:0;color:#111;font-size:13px;font-weight:600;">100% Authentic</p>
                <p style="margin:4px 0 0;color:#888;font-size:11px;">No fakes, ever</p>
              </div>
            </td>
            <td width="33%" style="padding:0 0 0 6px;vertical-align:top;">
              <div style="background:#EFF6FF;border:1px solid #BFDBFE;border-radius:12px;padding:16px;text-align:center;">
                <div style="font-size:24px;margin-bottom:8px;">🔒</div>
                <p style="margin:0;color:#111;font-size:13px;font-weight:600;">Secure Payment</p>
                <p style="margin:4px 0 0;color:#888;font-size:11px;">Via Flutterwave</p>
              </div>
            </td>
          </tr>
        </table>

        <!-- CTA BUTTON -->
        <div style="text-align:center;margin:0 0 24px;">
          <a href="https://shopfresherz.com/store"
             style="display:inline-block;background:linear-gradient(135deg,#F97316,#EA580C);color:#FFFFFF;text-decoration:none;padding:14px 40px;border-radius:10px;font-size:15px;font-weight:700;letter-spacing:0.3px;box-shadow:0 4px 15px rgba(249,115,22,0.3);">
            Start Shopping →
          </a>
        </div>

        <p style="color:#888;font-size:13px;text-align:center;margin:0;">
          Need help? Chat with us on
          <a href="https://wa.me/2349075308722" style="color:#F97316;text-decoration:none;font-weight:600;">
            WhatsApp
          </a>
        </p>
        """;

    private static string VerifyEmailBody(string firstName, string otp) =>
        $"""
        <div style="text-align:center;margin-bottom:32px;">
          <div style="width:64px;height:64px;background:#EFF6FF;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;">
            <span style="font-size:28px;">✉️</span>
          </div>
          <h2 style="margin:0 0 8px;color:#111111;font-size:24px;font-weight:800;">
            Verify Your Email
          </h2>
          <p style="margin:0;color:#666;font-size:15px;">
            Enter the code below to verify your account
          </p>
        </div>

        <p style="color:#444;font-size:15px;line-height:1.7;margin:0 0 24px;text-align:center;">
          Hi {Encode(firstName)}, use this verification code to complete
          your ShopFresherz account setup:
        </p>

        <!-- OTP BOX -->
        <div style="background:linear-gradient(135deg,#FFF8F3,#FFF3E0);border:2px solid #FED7AA;border-radius:16px;padding:32px;text-align:center;margin:0 0 24px;">
          <p style="margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px;">
            Your verification code
          </p>
          <div style="font-size:48px;font-weight:900;color:#F97316;letter-spacing:12px;line-height:1;margin:8px 0;">
            {Encode(otp)}
          </div>
          <p style="margin:12px 0 0;color:#888;font-size:13px;">
            ⏱ This code expires in <strong>15 minutes</strong>
          </p>
        </div>

        <div style="background:#FFF8F3;border-left:4px solid #F97316;border-radius:0 8px 8px 0;padding:12px 16px;margin:0 0 24px;">
          <p style="margin:0;color:#444;font-size:13px;line-height:1.6;">
            🔒 <strong>Security tip:</strong> Never share this code with anyone.
            ShopFresherz will never ask for your OTP via call or message.
          </p>
        </div>

        <p style="color:#888;font-size:13px;text-align:center;margin:0;">
          Didn't request this? You can safely ignore this email.
        </p>
        """;

    private static string PasswordResetBody(string firstName, string otp) =>
        $"""
        <div style="text-align:center;margin-bottom:32px;">
          <div style="width:64px;height:64px;background:#FEF2F2;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;">
            <span style="font-size:28px;">🔑</span>
          </div>
          <h2 style="margin:0 0 8px;color:#111111;font-size:24px;font-weight:800;">
            Reset Your Password
          </h2>
          <p style="margin:0;color:#666;font-size:15px;">
            Use the code below to reset your password
          </p>
        </div>

        <p style="color:#444;font-size:15px;line-height:1.7;margin:0 0 24px;text-align:center;">
          Hi {Encode(firstName)}, we received a request to reset your
          ShopFresherz password. Use this code:
        </p>

        <!-- OTP BOX -->
        <div style="background:linear-gradient(135deg,#FFF8F3,#FFF3E0);border:2px solid #FED7AA;border-radius:16px;padding:32px;text-align:center;margin:0 0 24px;">
          <p style="margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px;">
            Password reset code
          </p>
          <div style="font-size:48px;font-weight:900;color:#F97316;letter-spacing:12px;line-height:1;margin:8px 0;">
            {Encode(otp)}
          </div>
          <p style="margin:12px 0 0;color:#888;font-size:13px;">
            ⏱ Expires in <strong>15 minutes</strong>
          </p>
        </div>

        <div style="background:#FEF2F2;border-left:4px solid #EF4444;border-radius:0 8px 8px 0;padding:12px 16px;margin:0 0 24px;">
          <p style="margin:0;color:#444;font-size:13px;line-height:1.6;">
            ⚠️ <strong>Not you?</strong> If you did not request a password reset,
            please ignore this email. Your password will not change.
          </p>
        </div>

        <p style="color:#888;font-size:13px;text-align:center;margin:0;">
          Need help? Contact us on
          <a href="https://wa.me/2349075308722" style="color:#F97316;text-decoration:none;font-weight:600;">
            WhatsApp
          </a>
        </p>
        """;

    private static string OrderConfirmationBody(
        string firstName,
        string orderNumber,
        decimal total,
        string paymentMethod,
        DeliveryMethod deliveryMethod,
        DateTime? estimatedDelivery,
        string? phone)
    {
        string phoneRow = string.IsNullOrWhiteSpace(phone)
            ? string.Empty
            : $"""
              <tr>
                <td style="color:#666;font-size:14px;padding:6px 0;">Phone Number</td>
                <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                  {Encode(phone)}
                </td>
              </tr>
              """;

        (string deliveryEstimateLabel, string deliveryNextStepText) = deliveryMethod switch
        {
            DeliveryMethod.Pickup => (
                "Ready for Pickup",
                "🏬 We'll text you when your order is ready to collect in-store"),
            DeliveryMethod.Express => (
                estimatedDelivery.HasValue
                    ? $"1-3 Business Days (by {estimatedDelivery.Value:MMM d, yyyy})"
                    : "1-3 Business Days",
                "🚚 Express delivery within 1-3 business days to your address"),
            _ => (
                estimatedDelivery.HasValue
                    ? $"3-5 Business Days (by {estimatedDelivery.Value:MMM d, yyyy})"
                    : "3-5 Business Days",
                "🚚 Delivery within 3-5 business days to your address"),
        };

        return $"""
        <div style="text-align:center;margin-bottom:32px;">
          <div style="width:64px;height:64px;background:#F0FDF4;border:2px solid #86EFAC;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;">
            <span style="font-size:28px;">✅</span>
          </div>
          <h2 style="margin:0 0 8px;color:#111111;font-size:24px;font-weight:800;">
            Order Confirmed!
          </h2>
          <div style="display:inline-block;background:#FFF3E0;border:1px solid #FED7AA;border-radius:20px;padding:6px 20px;margin-top:8px;">
            <span style="color:#F97316;font-size:14px;font-weight:700;">{Encode(orderNumber)}</span>
          </div>
        </div>

        <p style="color:#444;font-size:15px;line-height:1.7;margin:0 0 24px;">
          Hi {Encode(firstName)}, thank you for shopping with ShopFresherz!
          Your order has been confirmed and is being processed.
        </p>

        <!-- ORDER SUMMARY BOX -->
        <div style="background:#F8F8F8;border-radius:12px;padding:20px;margin:0 0 24px;">
          <h3 style="margin:0 0 16px;color:#111;font-size:15px;font-weight:700;
            border-bottom:1px solid #E5E5E5;padding-bottom:12px;">
            Order Summary
          </h3>
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Total Amount</td>
              <td style="color:#F97316;font-size:16px;font-weight:800;text-align:right;">
                ₦{total:N2}
              </td>
            </tr>
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Payment Method</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(paymentMethod)}
              </td>
            </tr>
            {phoneRow}
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Estimated Delivery</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(deliveryEstimateLabel)}
              </td>
            </tr>
          </table>
        </div>

        <!-- NEXT STEPS -->
        <div style="background:#FFF8F3;border:1px solid #FED7AA;border-radius:12px;padding:20px;margin:0 0 24px;">
          <h3 style="margin:0 0 12px;color:#F97316;font-size:14px;font-weight:700;
            text-transform:uppercase;letter-spacing:0.5px;">
            What Happens Next?
          </h3>
          <table cellpadding="0" cellspacing="0" width="100%">
            <tr>
              <td style="padding:6px 0;color:#444;font-size:13px;line-height:1.6;">
                📞 We'll call you within 2 hours to confirm your order
              </td>
            </tr>
            <tr>
              <td style="padding:6px 0;color:#444;font-size:13px;line-height:1.6;">
                📦 Your order will be packed and dispatched within 24 hours
              </td>
            </tr>
            <tr>
              <td style="padding:6px 0;color:#444;font-size:13px;line-height:1.6;">
                {deliveryNextStepText}
              </td>
            </tr>
            <tr>
              <td style="padding:6px 0;color:#444;font-size:13px;line-height:1.6;">
                💬 SMS alert when your order is out for delivery
              </td>
            </tr>
          </table>
        </div>

        <!-- CTA -->
        <div style="text-align:center;">
          <a href="https://shopfresherz.com/store"
             style="display:inline-block;background:linear-gradient(135deg,#F97316,#EA580C);color:#FFFFFF;text-decoration:none;padding:14px 40px;border-radius:10px;font-size:15px;font-weight:700;box-shadow:0 4px 15px rgba(249,115,22,0.3);">
            Continue Shopping →
          </a>
        </div>
        """;
    }

    private static string AdminOrderNotificationBody(
        string orderNumber,
        string customerName,
        string customerEmail,
        string customerPhone,
        decimal total,
        string paymentMethod,
        string deliveryAddressJson)
    {
        string deliveryAddress = FormatDeliveryAddress(deliveryAddressJson);

        return $"""
        <div style="text-align:center;margin-bottom:32px;">
          <div style="width:64px;height:64px;background:#EFF6FF;border:2px solid #93C5FD;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;">
            <span style="font-size:28px;">🛎️</span>
          </div>
          <h2 style="margin:0 0 8px;color:#111111;font-size:24px;font-weight:800;">
            New Order Received
          </h2>
          <div style="display:inline-block;background:#FFF3E0;border:1px solid #FED7AA;border-radius:20px;padding:6px 20px;margin-top:8px;">
            <span style="color:#F97316;font-size:14px;font-weight:700;">{Encode(orderNumber)}</span>
          </div>
        </div>

        <div style="background:#F8F8F8;border-radius:12px;padding:20px;margin:0 0 24px;">
          <h3 style="margin:0 0 16px;color:#111;font-size:15px;font-weight:700;
            border-bottom:1px solid #E5E5E5;padding-bottom:12px;">
            Order Details
          </h3>
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Total Amount</td>
              <td style="color:#F97316;font-size:16px;font-weight:800;text-align:right;">
                ₦{total:N2}
              </td>
            </tr>
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Payment Method</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(paymentMethod)}
              </td>
            </tr>
          </table>
        </div>

        <div style="background:#F8F8F8;border-radius:12px;padding:20px;margin:0 0 24px;">
          <h3 style="margin:0 0 16px;color:#111;font-size:15px;font-weight:700;
            border-bottom:1px solid #E5E5E5;padding-bottom:12px;">
            Customer
          </h3>
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Name</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(string.IsNullOrWhiteSpace(customerName) ? "Guest" : customerName)}
              </td>
            </tr>
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Email</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(string.IsNullOrWhiteSpace(customerEmail) ? "—" : customerEmail)}
              </td>
            </tr>
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Phone</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(string.IsNullOrWhiteSpace(customerPhone) ? "—" : customerPhone)}
              </td>
            </tr>
          </table>
        </div>

        <div style="background:#F8F8F8;border-radius:12px;padding:20px;margin:0 0 24px;">
          <h3 style="margin:0 0 16px;color:#111;font-size:15px;font-weight:700;
            border-bottom:1px solid #E5E5E5;padding-bottom:12px;">
            Delivery Address
          </h3>
          <p style="margin:0;color:#111;font-size:14px;line-height:1.6;">
            {Encode(deliveryAddress)}
          </p>
        </div>
        """;
    }

    /// <summary>
    /// Degraded admin order-notification body used when <see cref="AdminOrderNotificationBody"/>
    /// itself fails to build (e.g. an unexpected error unrelated to the address JSON). Omits the
    /// delivery-address section entirely rather than risk repeating whatever failed.
    /// </summary>
    private static string AdminOrderNotificationFallbackBody(
        string orderNumber,
        string customerName,
        string customerEmail,
        string customerPhone,
        decimal total,
        string paymentMethod) =>
        $"""
        <div style="text-align:center;margin-bottom:32px;">
          <h2 style="margin:0 0 8px;color:#111111;font-size:24px;font-weight:800;">
            New Order Received
          </h2>
          <div style="display:inline-block;background:#FFF3E0;border:1px solid #FED7AA;border-radius:20px;padding:6px 20px;margin-top:8px;">
            <span style="color:#F97316;font-size:14px;font-weight:700;">{Encode(orderNumber)}</span>
          </div>
        </div>

        <div style="background:#F8F8F8;border-radius:12px;padding:20px;margin:0 0 24px;">
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Total Amount</td>
              <td style="color:#F97316;font-size:16px;font-weight:800;text-align:right;">
                ₦{total:N2}
              </td>
            </tr>
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Payment Method</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(paymentMethod)}
              </td>
            </tr>
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Name</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(string.IsNullOrWhiteSpace(customerName) ? "Guest" : customerName)}
              </td>
            </tr>
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Email</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(string.IsNullOrWhiteSpace(customerEmail) ? "—" : customerEmail)}
              </td>
            </tr>
            <tr>
              <td style="color:#666;font-size:14px;padding:6px 0;">Phone</td>
              <td style="color:#111;font-size:14px;font-weight:600;text-align:right;">
                {Encode(string.IsNullOrWhiteSpace(customerPhone) ? "—" : customerPhone)}
              </td>
            </tr>
          </table>
          <p style="margin:12px 0 0;color:#888;font-size:12px;">
            Delivery address unavailable — check the order in admin for details.
          </p>
        </div>
        """;

    /// <summary>
    /// Parses the checkout delivery-address JSON snapshot (Label/Line1/Line2/City/State/PostalCode)
    /// into a single readable line for admin-facing emails. Never throws — malformed or missing
    /// data falls back to a placeholder so a bad snapshot can't break the notification.
    /// </summary>
    private static string FormatDeliveryAddress(string deliveryAddressJson)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(deliveryAddressJson);
            JsonElement root = doc.RootElement;

            string Get(string name) =>
                root.TryGetProperty(name, out JsonElement value) ? value.GetString() ?? string.Empty : string.Empty;

            List<string> parts = new();
            void AddIfPresent(string value)
            {
                if (!string.IsNullOrWhiteSpace(value)) parts.Add(value);
            }

            AddIfPresent(Get("Label"));
            AddIfPresent(Get("Line1"));
            AddIfPresent(Get("Line2"));
            AddIfPresent(string.Join(", ", new[] { Get("City"), Get("State") }.Where(s => !string.IsNullOrWhiteSpace(s))));
            AddIfPresent(Get("PostalCode"));

            return parts.Count > 0 ? string.Join(", ", parts) : "Address unavailable";
        }
        catch (Exception)
        {
            // Catches JsonException (malformed JSON) as well as InvalidOperationException
            // (a field present but holding a non-string JSON value, e.g. a number) and any
            // other parsing surprise — this must never throw into its caller.
            return "Address unavailable";
        }
    }

    private static string OrderShippedBody(string firstName, string orderNumber, string trackingNumber) =>
        $"""
        <div style="text-align:center;margin-bottom:32px;">
          <div style="width:64px;height:64px;background:#EFF6FF;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;">
            <span style="font-size:28px;">🚚</span>
          </div>
          <h2 style="margin:0 0 8px;color:#111111;font-size:24px;font-weight:800;">
            Your Order Is On Its Way
          </h2>
          <div style="display:inline-block;background:#FFF3E0;border:1px solid #FED7AA;border-radius:20px;padding:6px 20px;margin-top:8px;">
            <span style="color:#F97316;font-size:14px;font-weight:700;">{Encode(orderNumber)}</span>
          </div>
        </div>

        <p style="color:#444;font-size:15px;line-height:1.7;margin:0 0 24px;text-align:center;">
          Hi {Encode(firstName)}, your order has shipped and is heading your way.
        </p>

        <div style="background:#F8F8F8;border-radius:12px;padding:20px;text-align:center;margin:0 0 24px;">
          <p style="margin:0 0 8px;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px;">
            Tracking Number
          </p>
          <p style="margin:0;color:#111;font-size:18px;font-weight:800;">
            {Encode(trackingNumber)}
          </p>
        </div>

        <p style="color:#888;font-size:13px;text-align:center;margin:0;">
          Questions? Contact us on
          <a href="https://wa.me/2349075308722" style="color:#F97316;text-decoration:none;font-weight:600;">
            WhatsApp
          </a>
        </p>
        """;

    private static string BackInStockBody(string firstName, string productName, string productSlug) =>
        $"""
        <div style="text-align:center;margin-bottom:32px;">
          <div style="width:64px;height:64px;background:#F0FDF4;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;">
            <span style="font-size:28px;">📦</span>
          </div>
          <h2 style="margin:0 0 8px;color:#111111;font-size:24px;font-weight:800;">
            Back In Stock!
          </h2>
        </div>

        <p style="color:#444;font-size:15px;line-height:1.7;margin:0 0 24px;text-align:center;">
          Hi {Encode(firstName)}, good news — <strong>{Encode(productName)}</strong> is available again.
        </p>

        <div style="text-align:center;">
          <a href="https://shopfresherz.com/products/{Uri.EscapeDataString(productSlug)}"
             style="display:inline-block;background:linear-gradient(135deg,#F97316,#EA580C);color:#FFFFFF;text-decoration:none;padding:14px 40px;border-radius:10px;font-size:15px;font-weight:700;box-shadow:0 4px 15px rgba(249,115,22,0.3);">
            View Product →
          </a>
        </div>
        """;

    private static string TemplatedBody(string templateKey, object templateData) =>
        $"""
        <div style="text-align:center;margin-bottom:24px;">
          <h2 style="margin:0 0 8px;color:#111111;font-size:22px;font-weight:800;">
            {Encode(templateKey)}
          </h2>
        </div>
        <pre style="background:#F8F8F8;border-radius:8px;padding:16px;color:#444;font-size:12px;overflow-x:auto;">{Encode(JsonSerializer.Serialize(templateData))}</pre>
        """;

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}

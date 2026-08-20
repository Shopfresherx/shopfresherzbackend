using ShopFresherz.Domain.Enums;

namespace ShopFresherz.Application.Dtos.Auth;

/// <summary>Authenticated user profile embedded in every auth response.</summary>
public sealed class UserDto
{
    /// <summary>Gets or sets the user's unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the CDN URL to the user's avatar.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Gets or sets the user's platform role.</summary>
    public UserRole Role { get; set; }

    /// <summary>Gets or sets a value indicating whether the account is verified.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Gets or sets the user's current loyalty points balance.</summary>
    public int LoyaltyPoints { get; set; }
}

/// <summary>Response payload returned for all successful authentication operations.</summary>
public sealed class AuthResponse
{
    /// <summary>Gets or sets the RS256 JWT access token (valid 15 minutes).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Gets or sets the opaque refresh token (valid 7 days).</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC expiry of the access token.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Gets or sets the authenticated user's profile.</summary>
    public UserDto User { get; set; } = null!;
}

/// <summary>Request payload for new user registration.</summary>
public sealed class RegisterRequest
{
    /// <summary>Gets or sets the user's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's phone number in international format.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the desired password (min 8 chars, 1 uppercase, 1 digit).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets the password confirmation (must match Password).</summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Request payload for user login.</summary>
public sealed class LoginRequest
{
    /// <summary>Gets or sets the user's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's password.</summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>Request payload for Google ID-token sign-in.</summary>
public sealed class GoogleSignInRequest
{
    public string IdToken { get; set; } = string.Empty;
}

/// <summary>Request payload for access token refresh.</summary>
public sealed class RefreshTokenRequest
{
    /// <summary>Gets or sets the expired access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Gets or sets the refresh token.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Request payload for initiating a password reset flow.</summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>Gets or sets the email address associated with the account.</summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>Request payload for completing a password reset with OTP.</summary>
public sealed class ResetPasswordRequest
{
    /// <summary>Gets or sets the email address for the account.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the 6-digit OTP sent via email/SMS.</summary>
    public string Otp { get; set; } = string.Empty;

    /// <summary>Gets or sets the new password.</summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the new password confirmation.</summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>Request payload for OTP-based email/phone verification.</summary>
public sealed class VerifyOtpRequest
{
    /// <summary>Gets or sets the email address to verify.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the 6-digit OTP received by email or SMS.</summary>
    public string Otp { get; set; } = string.Empty;
}

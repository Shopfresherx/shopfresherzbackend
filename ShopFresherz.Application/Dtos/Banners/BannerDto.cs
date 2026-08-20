namespace ShopFresherz.Application.Dtos.Banners;

/// <summary>Homepage banner response DTO.</summary>
public sealed record BannerDto(
    Guid Id,
    string Title,
    string ImageUrl,
    string? LinkUrl,
    string? SubTitle,
    string? CtaText,
    string? Tag,
    int SortOrder);

/// <summary>Create banner request payload.</summary>
public sealed record CreateBannerRequest(
    string Title,
    string ImageUrl,
    string? LinkUrl,
    string? SubTitle,
    string? CtaText,
    string? Tag,
    int SortOrder,
    bool IsActive,
    DateTime? StartsAt,
    DateTime? EndsAt);

/// <summary>Patch banner request payload.</summary>
public sealed record UpdateBannerRequest(
    string? Title,
    string? ImageUrl,
    string? LinkUrl,
    string? SubTitle,
    string? CtaText,
    string? Tag,
    int? SortOrder,
    bool? IsActive,
    DateTime? StartsAt,
    DateTime? EndsAt);

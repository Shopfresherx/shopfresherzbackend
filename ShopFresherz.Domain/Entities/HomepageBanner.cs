using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>Homepage hero/content banner managed by admins.</summary>
public sealed class HomepageBanner : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public string? SubTitle { get; set; }

    public string? CtaText { get; set; }

    public string? Tag { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }
}

using ShopFresherz.Domain.Common;

namespace ShopFresherz.Domain.Entities;

/// <summary>Persisted admin-configurable application setting section.</summary>
public sealed class AppSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    public string ValueJson { get; set; } = "{}";
}

namespace ShopFresherz.Application.Dtos.FlashDeals;

/// <summary>Public flash deal DTO.</summary>
public sealed class FlashDealDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal SalePrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int? MaxQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int? RemainingQuantity { get; set; }
    public bool IsLive { get; set; }
    public TimeSpan TimeRemaining { get; set; }
}

/// <summary>Request payload for creating a flash deal.</summary>
public sealed class CreateFlashDealRequest
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal SalePrice { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int? MaxQuantity { get; set; }
}

/// <summary>Request payload for updating a flash deal.</summary>
public sealed class UpdateFlashDealRequest
{
    public decimal? SalePrice { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int? MaxQuantity { get; set; }
    public bool? IsActive { get; set; }
}

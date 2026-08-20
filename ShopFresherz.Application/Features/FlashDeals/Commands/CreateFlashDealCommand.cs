using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.FlashDeals;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.FlashDeals.Commands;

/// <summary>Admin command for creating a flash deal.</summary>
public sealed record CreateFlashDealCommand(CreateFlashDealRequest Request)
    : IRequest<Result<Guid>>;

/// <summary>Handler for <see cref="CreateFlashDealCommand"/>.</summary>
public sealed class CreateFlashDealCommandHandler
    : IRequestHandler<CreateFlashDealCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;

    public CreateFlashDealCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreateFlashDealCommand command,
        CancellationToken cancellationToken)
    {
        CreateFlashDealRequest request = command.Request;
        Product? product = await _uow.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Error.NotFound("Product");
        }

        Error? validation = await ValidateAsync(
            product,
            request.VariantId,
            request.SalePrice,
            request.StartsAt,
            request.EndsAt,
            request.MaxQuantity,
            null,
            cancellationToken);

        if (validation is not null) return validation;

        decimal originalPrice = request.VariantId.HasValue
            ? product.Variants.First(v => v.Id == request.VariantId.Value).Price
            : product.Price;

        FlashDeal deal = new()
        {
            ProductId = product.Id,
            VariantId = request.VariantId,
            SalePrice = request.SalePrice,
            OriginalPrice = originalPrice,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            MaxQuantity = request.MaxQuantity,
            IsActive = true,
        };

        await _uow.FlashDeals.AddAsync(deal, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(deal.Id);
    }

    private async Task<Error?> ValidateAsync(
        Product product,
        Guid? variantId,
        decimal salePrice,
        DateTime startsAt,
        DateTime endsAt,
        int? maxQuantity,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (salePrice <= 0) return Error.Validation("Sale price must be greater than zero.");
        if (endsAt <= DateTime.UtcNow) return Error.Validation("End date must be in the future.");
        if (endsAt <= startsAt) return Error.Validation("End date must be after start date.");
        if (maxQuantity.HasValue && maxQuantity.Value <= 0) return Error.Validation("Max quantity must be greater than zero.");

        decimal originalPrice = product.Price;
        if (variantId.HasValue)
        {
            ProductVariant? variant = product.Variants.FirstOrDefault(v => v.Id == variantId.Value);
            if (variant is null) return Error.Validation("Product variant was not found.");
            originalPrice = variant.Price;
        }

        if (salePrice >= originalPrice)
        {
            return Error.Validation("Sale price must be less than the product price.");
        }

        bool hasOverlap = await _uow.FlashDeals.HasOverlapAsync(
            product.Id,
            variantId,
            startsAt,
            endsAt,
            excludeId,
            cancellationToken);

        return hasOverlap
            ? Error.Conflict("An overlapping active flash deal already exists for this product.")
            : null;
    }
}

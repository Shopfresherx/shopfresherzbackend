using AutoMapper;
using ShopFresherz.Application.Dtos.Auth;
using ShopFresherz.Application.Dtos.Cart;
using ShopFresherz.Application.Dtos.Order;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Application.Dtos.Profile;
using ShopFresherz.Application.Dtos.Review;
using ShopFresherz.Domain.Entities;

namespace ShopFresherz.Application.Common.Mappings;

/// <summary>AutoMapper profile registering all entity-to-DTO mappings.</summary>
public sealed class MappingProfile : Profile
{
    /// <summary>Initialises all mappings.</summary>
    public MappingProfile()
    {
        // ── Auth ─────────────────────────────────────────────────────────────
        CreateMap<User, UserDto>();

        // ── Product ──────────────────────────────────────────────────────────
        CreateMap<Brand, BrandDto>();
        CreateMap<Category, CategoryDto>();
        CreateMap<ProductImage, ProductImageDto>();
        CreateMap<ProductVariant, ProductVariantDto>()
            .ForMember(d => d.AvailableQty, opt => opt.MapFrom(s => s.AvailableQty));

        CreateMap<Product, ProductSummaryDto>()
            .ForMember(d => d.PrimaryImageUrl, opt => opt.MapFrom(
                s => s.Images.OrderBy(i => i.SortOrder).FirstOrDefault() != null
                    ? s.Images.OrderBy(i => i.SortOrder).First().DisplayUrl
                    : null))
            .ForMember(d => d.AvailableQty, opt => opt.MapFrom(s => s.AvailableQty))
            .ForMember(d => d.Brand, opt => opt.MapFrom(s => s.Brand))
            .ForMember(d => d.Category, opt => opt.MapFrom(s => s.Category))
            .ForMember(d => d.ImageUrls, opt => opt.MapFrom(s =>
                s.ImageUrls.Count > 0
                    ? s.ImageUrls
                    : s.Images.OrderBy(i => i.SortOrder).Select(i => i.DisplayUrl).ToList()));

        CreateMap<Product, ProductDetailDto>()
            .IncludeBase<Product, ProductSummaryDto>()
            .ForMember(d => d.Images, opt => opt.MapFrom(
                s => s.Images.OrderBy(i => i.SortOrder).ToList()))
            .ForMember(d => d.Variants, opt => opt.MapFrom(s => s.Variants));

        // ── Cart ─────────────────────────────────────────────────────────────
        CreateMap<CartItem, CartItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSlug, opt => opt.MapFrom(s => s.Product.Slug))
            .ForMember(d => d.ProductImageUrl, opt => opt.MapFrom(
                s => s.Product.Images.OrderBy(i => i.SortOrder).FirstOrDefault() != null
                    ? s.Product.Images.OrderBy(i => i.SortOrder).First().DisplayUrl
                    : null))
            .ForMember(d => d.VariantAttributesJson, opt => opt.MapFrom(
                s => s.Variant != null ? s.Variant.AttributesJson : null))
            .ForMember(d => d.UnitPrice, opt => opt.MapFrom(
                s => s.Variant != null ? s.Variant.Price : s.Product.Price));

        CreateMap<Cart, CartDto>()
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

        // ── Order ────────────────────────────────────────────────────────────
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductSnapshot, opt => opt.Ignore()); // Deserialized manually.

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.DeliveryAddress, opt => opt.Ignore()) // Deserialized manually.
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

        // ── Profile ──────────────────────────────────────────────────────────
        CreateMap<Address, AddressDto>();

        // ── Review ───────────────────────────────────────────────────────────
        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.ReviewerName, opt => opt.MapFrom(
                s => s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : "Anonymous"));
    }
}

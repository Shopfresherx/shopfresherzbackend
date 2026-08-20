using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using System.Text.Json;

namespace ShopFresherz.Application.Features.Admin.Queries;

public sealed record GetAdminSettingsQuery(string? Section = null) : IRequest<Result<object>>;

public sealed class GetAdminSettingsQueryHandler : IRequestHandler<GetAdminSettingsQuery, Result<object>>
{
    private readonly IUnitOfWork _uow;

    public GetAdminSettingsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<object>> Handle(GetAdminSettingsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppSetting> settings = await _uow.AppSettings.GetAllAsync(cancellationToken);
        AdminSettingsDto dto = AdminSettingsMapper.FromSettings(settings);

        if (string.IsNullOrWhiteSpace(request.Section))
        {
            return Result<object>.Success(dto);
        }

        object? section = AdminSettingsMapper.GetSection(dto, request.Section);
        return section is null
            ? Result<object>.Failure(new Error("VALIDATION", $"Unknown settings section '{request.Section}'."))
            : Result<object>.Success(section);
    }
}

internal static class AdminSettingsMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public static AdminSettingsDto FromSettings(IReadOnlyList<AppSetting> settings)
    {
        Dictionary<string, string> values = settings.ToDictionary(x => x.Key, x => x.ValueJson, StringComparer.OrdinalIgnoreCase);

        return new AdminSettingsDto
        {
            Store = Read(values, "store", new StoreSettingsDto()),
            Payment = Read(values, "payment", new PaymentSettingsDto()),
            Shipping = Read(values, "shipping", new ShippingSettingsDto()),
            Notifications = Read(values, "notifications", new NotificationSettingsDto()),
            Seo = Read(values, "seo", new SeoSettingsDto()),
            Security = Read(values, "security", new SecuritySettingsDto()),
            Maintenance = Read(values, "maintenance", new MaintenanceSettingsDto()),
        };
    }

    public static object? GetSection(AdminSettingsDto dto, string section) => Normalize(section) switch
    {
        "store" => dto.Store,
        "payment" => dto.Payment,
        "shipping" => dto.Shipping,
        "notifications" => dto.Notifications,
        "seo" => dto.Seo,
        "security" => dto.Security,
        "maintenance" => dto.Maintenance,
        _ => null,
    };

    public static IReadOnlyDictionary<string, string> ToSectionJson(AdminSettingsUpdateRequest request)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        if (request.Store is not null) values["store"] = Write(request.Store);
        if (request.Payment is not null) values["payment"] = Write(request.Payment);
        if (request.Shipping is not null) values["shipping"] = Write(request.Shipping);
        if (request.Notifications is not null) values["notifications"] = Write(request.Notifications);
        if (request.Seo is not null) values["seo"] = Write(request.Seo);
        if (request.Security is not null) values["security"] = Write(request.Security);
        if (request.Maintenance is not null) values["maintenance"] = Write(request.Maintenance);

        return values;
    }

    public static string? ToSectionJson(string section, JsonElement value)
    {
        return Normalize(section) switch
        {
            "store" => Write(value.Deserialize<StoreSettingsDto>(JsonOptions) ?? new StoreSettingsDto()),
            "payment" => Write(value.Deserialize<PaymentSettingsDto>(JsonOptions) ?? new PaymentSettingsDto()),
            "shipping" => Write(value.Deserialize<ShippingSettingsDto>(JsonOptions) ?? new ShippingSettingsDto()),
            "notifications" => Write(value.Deserialize<NotificationSettingsDto>(JsonOptions) ?? new NotificationSettingsDto()),
            "seo" => Write(value.Deserialize<SeoSettingsDto>(JsonOptions) ?? new SeoSettingsDto()),
            "security" => Write(value.Deserialize<SecuritySettingsDto>(JsonOptions) ?? new SecuritySettingsDto()),
            "maintenance" => Write(value.Deserialize<MaintenanceSettingsDto>(JsonOptions) ?? new MaintenanceSettingsDto()),
            _ => null,
        };
    }

    private static T Read<T>(Dictionary<string, string> values, string key, T fallback)
    {
        if (!values.TryGetValue(key, out string? json) || string.IsNullOrWhiteSpace(json))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static string Write<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string Normalize(string section) => section.Trim().ToLowerInvariant();
}

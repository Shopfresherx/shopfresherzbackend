using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;
using ShopFresherz.Application.Features.Admin.Queries;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;
using System.Text.Json;

namespace ShopFresherz.Application.Features.Admin.Commands;

public sealed record UpdateAdminSettingsCommand(AdminSettingsUpdateRequest Request) : IRequest<Result<AdminSettingsDto>>;

public sealed record UpdateAdminSettingsSectionCommand(string Section, JsonElement Value) : IRequest<Result<AdminSettingsDto>>;

public sealed class UpdateAdminSettingsCommandHandler :
    IRequestHandler<UpdateAdminSettingsCommand, Result<AdminSettingsDto>>,
    IRequestHandler<UpdateAdminSettingsSectionCommand, Result<AdminSettingsDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateAdminSettingsCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<AdminSettingsDto>> Handle(UpdateAdminSettingsCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> updates = AdminSettingsMapper.ToSectionJson(request.Request);
        if (updates.Count == 0)
        {
            return Result<AdminSettingsDto>.Failure(new Error("VALIDATION", "At least one settings section is required."));
        }

        foreach (KeyValuePair<string, string> update in updates)
        {
            await _uow.AppSettings.UpsertAsync(update.Key, update.Value, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return await LoadSettings(cancellationToken);
    }

    public async Task<Result<AdminSettingsDto>> Handle(UpdateAdminSettingsSectionCommand request, CancellationToken cancellationToken)
    {
        string? valueJson = AdminSettingsMapper.ToSectionJson(request.Section, request.Value);
        if (valueJson is null)
        {
            return Result<AdminSettingsDto>.Failure(new Error("VALIDATION", $"Unknown settings section '{request.Section}'."));
        }

        await _uow.AppSettings.UpsertAsync(request.Section.Trim().ToLowerInvariant(), valueJson, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return await LoadSettings(cancellationToken);
    }

    private async Task<Result<AdminSettingsDto>> LoadSettings(CancellationToken cancellationToken)
    {
        IReadOnlyList<AppSetting> settings = await _uow.AppSettings.GetAllAsync(cancellationToken);
        return Result<AdminSettingsDto>.Success(AdminSettingsMapper.FromSettings(settings));
    }
}

using Microsoft.AspNetCore.Mvc;
using Retailer.Application.Legacy.Settings;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class SettingsController : VersionNeutralApiController
{
    private readonly ISettingService _settingService;

    public SettingsController(ISettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.Settings)]
    [OpenApiOperation("Get all settings, optionally filtered by category.", "")]
    public async Task<HttpResponseDto<List<SettingResponse>>> GetAllAsync([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var settings = await _settingService.GetAllAsync(category, cancellationToken);
        return settings.ToInformationResponse();
    }

    [HttpGet("{key}")]
    [MustHavePermission(AppAction.View, AppResource.Settings)]
    [OpenApiOperation("Get a setting by key.", "")]
    public async Task<HttpResponseDto<SettingResponse>> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var setting = await _settingService.GetByKeyAsync(key, cancellationToken)
            ?? new SettingResponse { Key = key, Value = string.Empty };
        return setting.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Update, AppResource.Settings)]
    [OpenApiOperation("Upsert an individual setting.", "")]
    public async Task<HttpResponseDto<SettingResponse>> UpsertAsync(SettingUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _settingService.UpsertAsync(request, cancellationToken);
        return result.ToInformationResponse("Setting saved successfully.");
    }

    [HttpPost("batch")]
    [MustHavePermission(AppAction.Update, AppResource.Settings)]
    [OpenApiOperation("Upsert multiple settings at once.", "")]
    public async Task<HttpResponseDto<List<SettingResponse>>> BatchUpsertAsync(List<SettingUpdateRequest> requests, CancellationToken cancellationToken)
    {
        var result = await _settingService.BatchUpsertAsync(requests, cancellationToken);
        return result.ToInformationResponse("Settings updated successfully.");
    }
}

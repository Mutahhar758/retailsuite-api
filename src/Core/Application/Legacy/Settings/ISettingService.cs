namespace Retailer.Application.Legacy.Settings;

public interface ISettingService : ITransientService
{
    Task<List<SettingResponse>> GetAllAsync(string? category, CancellationToken cancellationToken);
    Task<SettingResponse?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task<SettingResponse> UpsertAsync(SettingUpdateRequest request, CancellationToken cancellationToken);
    Task<List<SettingResponse>> BatchUpsertAsync(List<SettingUpdateRequest> requests, CancellationToken cancellationToken);
}

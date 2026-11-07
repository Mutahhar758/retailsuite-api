using Retailer.Application.Legacy.DailyStockClosings;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

[Route("api/daily-stock-closings")]
public class DailyStockClosingsController : VersionNeutralApiController
{
    private readonly IDailyStockClosingService _dailyStockClosingService;

    public DailyStockClosingsController(IDailyStockClosingService dailyStockClosingService)
    {
        _dailyStockClosingService = dailyStockClosingService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.Sales)]
    [OpenApiOperation("Get daily stock closing history list.", "")]
    public async Task<HttpResponseDto<List<DailyStockClosingListItemResponse>>> GetListAsync(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _dailyStockClosingService.GetListAsync(fromDate, toDate, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("sheet")]
    [MustHavePermission(AppAction.View, AppResource.Sales)]
    [OpenApiOperation("Get daily stock closing sheet for a specific date.", "")]
    public async Task<HttpResponseDto<DailyStockClosingSheetResponse>> GetSheetAsync(
        [FromQuery] DateOnly date,
        [FromQuery] string? categoryCode,
        CancellationToken cancellationToken)
    {
        var result = await _dailyStockClosingService.GetSheetAsync(date, categoryCode, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.Sales)]
    [OpenApiOperation("Save or update a daily stock closing sheet.", "")]
    public async Task<HttpResponseDto<DailyStockClosingSaveResponse>> SaveAsync(
        DailyStockClosingSaveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dailyStockClosingService.SaveAsync(request, cancellationToken);
        return result.ToInformationResponse("Daily stock closing saved.");
    }

    [HttpDelete("{date}")]
    [MustHavePermission(AppAction.Delete, AppResource.Sales)]
    [OpenApiOperation("Delete a daily stock closing for a specific date.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        await _dailyStockClosingService.DeleteAsync(date, cancellationToken);
        return "Daily stock closing deleted.".ToInformationResponse("Daily stock closing deleted.");
    }
}

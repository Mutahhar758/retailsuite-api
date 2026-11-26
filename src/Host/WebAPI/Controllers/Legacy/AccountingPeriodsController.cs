using Retailer.Application.Legacy.AccountingPeriods;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

[Route("api/v1/accounting-periods")]
public class AccountingPeriodsController : VersionNeutralApiController
{
    private readonly IAccountingPeriodCloseService _periodCloseService;

    public AccountingPeriodsController(IAccountingPeriodCloseService periodCloseService)
    {
        _periodCloseService = periodCloseService;
    }

    [HttpGet("lock-status")]
    [OpenApiOperation("Get current accounting period lock date status.", "")]
    public async Task<HttpResponseDto<PeriodLockStatusResponse>> GetLockStatusAsync(CancellationToken cancellationToken)
    {
        var result = await _periodCloseService.GetLockStatusAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.AccountingPeriods)]
    [OpenApiOperation("Get accounting period close history.", "")]
    public async Task<HttpResponseDto<List<AccountingPeriodCloseResponse>>> GetHistoryAsync(CancellationToken cancellationToken)
    {
        var result = await _periodCloseService.GetHistoryAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{id:int}")]
    [MustHavePermission(AppAction.View, AppResource.AccountingPeriods)]
    [OpenApiOperation("Get accounting period close details and closing journal entries.", "")]
    public async Task<HttpResponseDto<AccountingPeriodCloseDetailResponse>> GetDetailAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _periodCloseService.GetDetailAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost("preview")]
    [MustHavePermission(AppAction.Create, AppResource.AccountingPeriods)]
    [OpenApiOperation("Preview period closing entries, validations, and post-closing summary.", "")]
    public async Task<HttpResponseDto<PeriodClosePreviewResponse>> PreviewCloseAsync(
        PeriodClosePreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _periodCloseService.PreviewCloseAsync(request, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost("close")]
    [MustHavePermission(AppAction.Create, AppResource.AccountingPeriods)]
    [OpenApiOperation("Execute accounting period close, post closing entries, and lock backdated transactions.", "")]
    public async Task<HttpResponseDto<AccountingPeriodCloseResponse>> ExecuteCloseAsync(
        PeriodCloseExecuteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _periodCloseService.ExecuteCloseAsync(request, cancellationToken);
        return result.ToInformationResponse("Accounting period closed successfully.");
    }

    [HttpPost("{id:int}/reopen")]
    [MustHavePermission(AppAction.Approve, AppResource.AccountingPeriods)]
    [OpenApiOperation("Reopen the most recently closed accounting period (LIFO).", "")]
    public async Task<HttpResponseDto<AccountingPeriodCloseResponse>> ReopenLatestAsync(
        int id,
        PeriodReopenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _periodCloseService.ReopenLatestAsync(id, request, cancellationToken);
        return result.ToInformationResponse("Accounting period reopened successfully.");
    }
}

using Retailer.Application.Legacy.BankReconciliations;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class BankReconciliationsController : VersionNeutralApiController
{
    private readonly IBankReconciliationService _bankReconciliationService;

    public BankReconciliationsController(IBankReconciliationService bankReconciliationService)
    {
        _bankReconciliationService = bankReconciliationService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.BankReconciliations)]
    [OpenApiOperation("Get bank reconciliation lines and balances.", "")]
    public async Task<HttpResponseDto<BankReconciliationSnapshotResponse>> GetSnapshotAsync(
        [FromQuery] BankReconciliationFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _bankReconciliationService.GetSnapshotAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPut]
    [MustHavePermission(AppAction.Update, AppResource.BankReconciliations)]
    [OpenApiOperation("Save bank reconciliation clear state.", "")]
    public async Task<HttpResponseDto<string>> SaveAsync(
        BankReconciliationSaveRequest request,
        CancellationToken cancellationToken)
    {
        await _bankReconciliationService.SaveAsync(request, cancellationToken);
        return "Bank reconciliation updated.".ToInformationResponse("Bank reconciliation updated.");
    }
}

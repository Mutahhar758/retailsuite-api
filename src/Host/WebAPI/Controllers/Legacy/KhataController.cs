using Retailer.Application.Legacy.Khata;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class KhataController : VersionNeutralApiController
{
    private readonly IKhataService _khataService;

    public KhataController(IKhataService khataService)
    {
        _khataService = khataService;
    }

    [HttpGet("parties")]
    [MustHavePermission(AppAction.View, AppResource.ReceiptVouchers, AppResource.PaymentVouchers, AppResource.AccountStatement)]
    [OpenApiOperation("Get Khata party list with balances and summary totals.", "")]
    public async Task<HttpResponseDto<KhataSummaryResponse>> GetPartiesAsync(
        [FromQuery] string? partyType,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await _khataService.GetPartiesAsync(partyType, search, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("parties/{accountId}/ledger")]
    [MustHavePermission(AppAction.View, AppResource.ReceiptVouchers, AppResource.PaymentVouchers, AppResource.AccountStatement)]
    [OpenApiOperation("Get chronological Khata ledger for a specific party.", "")]
    public async Task<HttpResponseDto<KhataLedgerResponse>> GetPartyLedgerAsync(
        string accountId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _khataService.GetPartyLedgerAsync(accountId, fromDate, toDate, cancellationToken);
        return result.ToInformationResponse();
    }
}

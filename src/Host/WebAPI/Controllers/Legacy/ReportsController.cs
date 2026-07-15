using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class ReportsController : VersionNeutralApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("account-statement")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get account statement report data.", "")]
    public async Task<HttpResponseDto<List<AccountStatementLineResponse>>> GetAccountStatementAsync(
        [FromQuery] AccountStatementFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAccountStatementAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("account-statement-with-due")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get account statement with due days report data.", "")]
    public async Task<HttpResponseDto<List<AccountStatementWithDueLineResponse>>> GetAccountStatementWithDueAsync(
        [FromQuery] AccountStatementFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAccountStatementWithDueAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("balance-detail")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get balance detail report data.", "")]
    public async Task<HttpResponseDto<List<BalanceDetailLineResponse>>> GetBalanceDetailAsync(
        [FromQuery] BalanceDetailFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetBalanceDetailAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("trial-balance")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get trial balance report data.", "")]
    public async Task<HttpResponseDto<List<TrialBalanceLineResponse>>> GetTrialBalanceAsync(
        [FromQuery] TrialBalanceFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetTrialBalanceAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("stock-ledger")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get stock ledger report data.", "")]
    public async Task<HttpResponseDto<List<StockLedgerLineResponse>>> GetStockLedgerAsync(
        [FromQuery] StockLedgerFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetStockLedgerAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("stock-balance")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get stock balance report data.", "")]
    public async Task<HttpResponseDto<List<StockBalanceLineResponse>>> GetStockBalanceAsync(
        [FromQuery] StockBalanceFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetStockBalanceAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("balance-sheet")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get balance sheet report data.", "")]
    public async Task<HttpResponseDto<List<BalanceSheetLineResponse>>> GetBalanceSheetAsync(
        [FromQuery] BalanceSheetFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetBalanceSheetAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("income-summary")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get income summary report data.", "")]
    public async Task<HttpResponseDto<List<IncomeSummaryLineResponse>>> GetIncomeSummaryAsync(
        [FromQuery] IncomeSummaryFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetIncomeSummaryAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("customer-bill")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get customer bill report data.", "")]
    public async Task<HttpResponseDto<CustomerBillResponse>> GetCustomerBillAsync(
        [FromQuery] CustomerBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetCustomerBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("envelope")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get envelope report data.", "")]
    public async Task<HttpResponseDto<List<EnvelopeLineResponse>>> GetEnvelopeAsync(
        [FromQuery] EnvelopeFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetEnvelopeAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("sale-bill")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get sale bill report data.", "")]
    public async Task<HttpResponseDto<SaleBillResponse>> GetSaleBillAsync(
        [FromQuery] SaleBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetSaleBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("purchase-bill")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get purchase bill report data.", "")]
    public async Task<HttpResponseDto<PurchaseBillResponse>> GetPurchaseBillAsync(
        [FromQuery] PurchaseBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPurchaseBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("purchase-ret-bill")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get purchase return bill report data.", "")]
    public async Task<HttpResponseDto<PurchaseBillResponse>> GetPurchaseRetBillAsync(
        [FromQuery] PurchaseBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPurchaseRetBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("sale-ret-bill")]
    [MustHavePermission(AppAction.View, AppResource.Reports)]
    [OpenApiOperation("Get sale return bill report data.", "")]
    public async Task<HttpResponseDto<SaleRetBillResponse>> GetSaleRetBillAsync(
        [FromQuery] SaleRetBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetSaleRetBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }
}

using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class ReportsController : VersionNeutralApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("account-statement")]
    [OpenApiOperation("Get account statement report data.", "")]
    public async Task<HttpResponseDto<List<AccountStatementLineResponse>>> GetAccountStatementAsync(
        [FromQuery] AccountStatementFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAccountStatementAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("account-statement-with-due")]
    [OpenApiOperation("Get account statement with due days report data.", "")]
    public async Task<HttpResponseDto<List<AccountStatementWithDueLineResponse>>> GetAccountStatementWithDueAsync(
        [FromQuery] AccountStatementFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAccountStatementWithDueAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("balance-detail")]
    [OpenApiOperation("Get balance detail report data.", "")]
    public async Task<HttpResponseDto<List<BalanceDetailLineResponse>>> GetBalanceDetailAsync(
        [FromQuery] BalanceDetailFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetBalanceDetailAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("trial-balance")]
    [OpenApiOperation("Get trial balance report data.", "")]
    public async Task<HttpResponseDto<List<TrialBalanceLineResponse>>> GetTrialBalanceAsync(
        [FromQuery] TrialBalanceFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetTrialBalanceAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("stock-ledger")]
    [OpenApiOperation("Get stock ledger report data.", "")]
    public async Task<HttpResponseDto<List<StockLedgerLineResponse>>> GetStockLedgerAsync(
        [FromQuery] StockLedgerFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetStockLedgerAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("stock-balance")]
    [OpenApiOperation("Get stock balance report data.", "")]
    public async Task<HttpResponseDto<List<StockBalanceLineResponse>>> GetStockBalanceAsync(
        [FromQuery] StockBalanceFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetStockBalanceAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("balance-sheet")]
    [OpenApiOperation("Get balance sheet report data.", "")]
    public async Task<HttpResponseDto<List<BalanceSheetLineResponse>>> GetBalanceSheetAsync(
        [FromQuery] BalanceSheetFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetBalanceSheetAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("income-summary")]
    [OpenApiOperation("Get income summary report data.", "")]
    public async Task<HttpResponseDto<List<IncomeSummaryLineResponse>>> GetIncomeSummaryAsync(
        [FromQuery] IncomeSummaryFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetIncomeSummaryAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("customer-bill")]
    [OpenApiOperation("Get customer bill report data.", "")]
    public async Task<HttpResponseDto<CustomerBillResponse>> GetCustomerBillAsync(
        [FromQuery] CustomerBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetCustomerBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("envelope")]
    [OpenApiOperation("Get envelope report data.", "")]
    public async Task<HttpResponseDto<List<EnvelopeLineResponse>>> GetEnvelopeAsync(
        [FromQuery] EnvelopeFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetEnvelopeAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("sale-bill")]
    [OpenApiOperation("Get sale bill report data.", "")]
    public async Task<HttpResponseDto<SaleBillResponse>> GetSaleBillAsync(
        [FromQuery] SaleBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetSaleBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("purchase-bill")]
    [OpenApiOperation("Get purchase bill report data.", "")]
    public async Task<HttpResponseDto<PurchaseBillResponse>> GetPurchaseBillAsync(
        [FromQuery] PurchaseBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPurchaseBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("purchase-ret-bill")]
    [OpenApiOperation("Get purchase return bill report data.", "")]
    public async Task<HttpResponseDto<PurchaseBillResponse>> GetPurchaseRetBillAsync(
        [FromQuery] PurchaseBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPurchaseRetBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("sale-ret-bill")]
    [OpenApiOperation("Get sale return bill report data.", "")]
    public async Task<HttpResponseDto<SaleRetBillResponse>> GetSaleRetBillAsync(
        [FromQuery] SaleRetBillFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetSaleRetBillAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }
}

using Retailer.Application.Legacy.SaleSupplies;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class SaleSuppliesController : VersionNeutralApiController
{
    private readonly ISaleSupplyService _saleSupplyService;

    public SaleSuppliesController(ISaleSupplyService saleSupplyService)
    {
        _saleSupplyService = saleSupplyService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.SaleSupplies)]
    [OpenApiOperation("Get sale supply voucher list.", "")]
    public async Task<HttpResponseDto<List<SaleSupplyResponse>>> GetListAsync(
        [FromQuery] SaleSupplyListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _saleSupplyService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [MustHavePermission(AppAction.View, AppResource.SaleSupplies)]
    [OpenApiOperation("Get sale supply voucher detail.", "")]
    public async Task<HttpResponseDto<List<SaleSupplyLineResponse>>> GetDetailAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        var result = await _saleSupplyService.GetDetailAsync(voucherNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.SaleSupplies)]
    [OpenApiOperation("Create a new sale supply voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(SaleSupplyCreateRequest request, CancellationToken cancellationToken)
    {
        var voucherNo = await _saleSupplyService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Sale supply created.");
    }

    [HttpPut("{voucherNo}")]
    [MustHavePermission(AppAction.Update, AppResource.SaleSupplies)]
    [OpenApiOperation("Update an existing sale supply voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        SaleSupplyUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _saleSupplyService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Sale supply updated.".ToInformationResponse("Sale supply updated.");
    }

    [HttpDelete("{voucherNo}")]
    [MustHavePermission(AppAction.Delete, AppResource.SaleSupplies)]
    [OpenApiOperation("Delete a sale supply voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        await _saleSupplyService.DeleteAsync(voucherNo, cancellationToken);
        return "Sale supply deleted.".ToInformationResponse("Sale supply deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [MustHavePermission(AppAction.Delete, AppResource.SaleSupplies)]
    [OpenApiOperation("Delete a single line from a sale supply voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        await _saleSupplyService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Sale supply line deleted.".ToInformationResponse("Sale supply line deleted.");
    }

    [HttpGet("customer-records")]
    [MustHavePermission(AppAction.View, AppResource.SaleSupplies)]
    [OpenApiOperation("Get customer sale supply lines across a date range.", "")]
    public async Task<HttpResponseDto<List<SaleSupplyLineResponse>>> GetCustomerLinesAsync(
        [FromQuery] string customerId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? itemId,
        CancellationToken cancellationToken)
    {
        var result = await _saleSupplyService.GetCustomerLinesAsync(customerId, fromDate, toDate, itemId, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPut("{voucherNo}/lines/{seq}")]
    [MustHavePermission(AppAction.Update, AppResource.SaleSupplies)]
    [OpenApiOperation("Update a single line in a sale supply voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateLineAsync(
        string voucherNo,
        int seq,
        SaleSupplyLineRequest request,
        CancellationToken cancellationToken)
    {
        await _saleSupplyService.UpdateLineAsync(voucherNo, seq, request, cancellationToken);
        return "Sale supply line updated.".ToInformationResponse("Sale supply line updated.");
    }

    [HttpPut("customer-lines")]
    [MustHavePermission(AppAction.Update, AppResource.SaleSupplies)]
    [OpenApiOperation("Batch update multiple sale supply lines.", "")]
    public async Task<HttpResponseDto<string>> UpdateCustomerLinesAsync(
        List<SaleSupplyCustomerLineUpdateRequest> requests,
        CancellationToken cancellationToken)
    {
        await _saleSupplyService.UpdateCustomerLinesAsync(requests, cancellationToken);
        return "Sale supply lines updated.".ToInformationResponse("Sale supply lines updated.");
    }
}

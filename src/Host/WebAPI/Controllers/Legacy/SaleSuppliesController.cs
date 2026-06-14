using Retailer.Application.Legacy.SaleSupplies;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class SaleSuppliesController : VersionNeutralApiController
{
    private readonly ISaleSupplyService _saleSupplyService;

    public SaleSuppliesController(ISaleSupplyService saleSupplyService)
    {
        _saleSupplyService = saleSupplyService;
    }

    [HttpGet]
    [OpenApiOperation("Get sale supply voucher list.", "")]
    public async Task<HttpResponseDto<List<SaleSupplyResponse>>> GetListAsync(
        [FromQuery] SaleSupplyListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _saleSupplyService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [OpenApiOperation("Get sale supply voucher detail.", "")]
    public async Task<HttpResponseDto<List<SaleSupplyLineResponse>>> GetDetailAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        var result = await _saleSupplyService.GetDetailAsync(voucherNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create a new sale supply voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(SaleSupplyCreateRequest request, CancellationToken cancellationToken)
    {
        var voucherNo = await _saleSupplyService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Sale supply created.");
    }

    [HttpPut("{voucherNo}")]
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
    [OpenApiOperation("Delete a sale supply voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        await _saleSupplyService.DeleteAsync(voucherNo, cancellationToken);
        return "Sale supply deleted.".ToInformationResponse("Sale supply deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [OpenApiOperation("Delete a single line from a sale supply voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        await _saleSupplyService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Sale supply line deleted.".ToInformationResponse("Sale supply line deleted.");
    }
}

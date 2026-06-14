using Retailer.Application.Legacy.Sales;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class SalesController : VersionNeutralApiController
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpGet]
    [OpenApiOperation("Get sale voucher list.", "")]
    public async Task<HttpResponseDto<List<SaleResponse>>> GetListAsync(
        [FromQuery] SaleListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _saleService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [OpenApiOperation("Get sale voucher detail.", "")]
    public async Task<HttpResponseDto<List<SaleLineResponse>>> GetDetailAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        var result = await _saleService.GetDetailAsync(voucherNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create a new sale voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(SaleCreateRequest request, CancellationToken cancellationToken)
    {
        var voucherNo = await _saleService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Sale created.");
    }

    [HttpPut("{voucherNo}")]
    [OpenApiOperation("Update an existing sale voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        SaleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _saleService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Sale updated.".ToInformationResponse("Sale updated.");
    }

    [HttpDelete("{voucherNo}")]
    [OpenApiOperation("Delete a sale voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        await _saleService.DeleteAsync(voucherNo, cancellationToken);
        return "Sale deleted.".ToInformationResponse("Sale deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [OpenApiOperation("Delete a single line from a sale voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        await _saleService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Sale line deleted.".ToInformationResponse("Sale line deleted.");
    }
}

using Retailer.Application.Legacy.StockAdjustments;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class StockAdjustmentsController : VersionNeutralApiController
{
    private readonly IStockAdjustmentService _stockAdjustmentService;

    public StockAdjustmentsController(IStockAdjustmentService stockAdjustmentService)
    {
        _stockAdjustmentService = stockAdjustmentService;
    }

    [HttpGet]
    [OpenApiOperation("Get stock adjustment voucher list.", "")]
    public async Task<HttpResponseDto<List<StockAdjustmentResponse>>> GetListAsync(
        [FromQuery] StockAdjustmentListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _stockAdjustmentService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [OpenApiOperation("Get stock adjustment voucher detail.", "")]
    public async Task<HttpResponseDto<List<StockAdjustmentLineResponse>>> GetDetailAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        var result = await _stockAdjustmentService.GetDetailAsync(voucherNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create a new stock adjustment voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(StockAdjustmentCreateRequest request, CancellationToken cancellationToken)
    {
        var voucherNo = await _stockAdjustmentService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Stock adjustment created.");
    }

    [HttpPut("{voucherNo}")]
    [OpenApiOperation("Update an existing stock adjustment voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        StockAdjustmentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _stockAdjustmentService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Stock adjustment updated.".ToInformationResponse("Stock adjustment updated.");
    }

    [HttpDelete("{voucherNo}")]
    [OpenApiOperation("Delete a stock adjustment voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        await _stockAdjustmentService.DeleteAsync(voucherNo, cancellationToken);
        return "Stock adjustment deleted.".ToInformationResponse("Stock adjustment deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [OpenApiOperation("Delete a single line from a stock adjustment voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        await _stockAdjustmentService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Stock adjustment line deleted.".ToInformationResponse("Stock adjustment line deleted.");
    }
}

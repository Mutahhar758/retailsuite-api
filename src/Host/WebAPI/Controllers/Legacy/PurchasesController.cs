using Retailer.Application.Legacy.Purchases;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class PurchasesController : VersionNeutralApiController
{
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    [OpenApiOperation("Get purchase voucher list.", "")]
    public async Task<HttpResponseDto<List<PurchaseResponse>>> GetListAsync(
        [FromQuery] PurchaseListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [OpenApiOperation("Get purchase voucher detail.", "")]
    public async Task<HttpResponseDto<List<PurchaseLineResponse>>> GetDetailAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetDetailAsync(voucherNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create a new purchase voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(PurchaseCreateRequest request, CancellationToken cancellationToken)
    {
        var voucherNo = await _purchaseService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Purchase created.");
    }

    [HttpPut("{voucherNo}")]
    [OpenApiOperation("Update an existing purchase voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        PurchaseUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _purchaseService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Purchase updated.".ToInformationResponse("Purchase updated.");
    }

    [HttpDelete("{voucherNo}")]
    [OpenApiOperation("Delete a purchase voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        await _purchaseService.DeleteAsync(voucherNo, cancellationToken);
        return "Purchase deleted.".ToInformationResponse("Purchase deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [OpenApiOperation("Delete a single line from a purchase voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        await _purchaseService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Purchase line deleted.".ToInformationResponse("Purchase line deleted.");
    }
}

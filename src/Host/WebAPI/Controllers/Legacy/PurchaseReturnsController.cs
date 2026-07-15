using Retailer.Application.Legacy.PurchaseReturns;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class PurchaseReturnsController : VersionNeutralApiController
{
    private readonly IPurchaseReturnService _purchaseReturnService;

    public PurchaseReturnsController(IPurchaseReturnService purchaseReturnService)
    {
        _purchaseReturnService = purchaseReturnService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.PurchaseReturns)]
    [OpenApiOperation("Get purchase return voucher list.", "")]
    public async Task<HttpResponseDto<List<PurchaseReturnResponse>>> GetListAsync(
        [FromQuery] PurchaseReturnListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseReturnService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [MustHavePermission(AppAction.View, AppResource.PurchaseReturns)]
    [OpenApiOperation("Get purchase return voucher detail.", "")]
    public async Task<HttpResponseDto<List<PurchaseReturnLineResponse>>> GetDetailAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        var result = await _purchaseReturnService.GetDetailAsync(voucherNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.PurchaseReturns)]
    [OpenApiOperation("Create a new purchase return voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(PurchaseReturnCreateRequest request, CancellationToken cancellationToken)
    {
        var voucherNo = await _purchaseReturnService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Purchase return created.");
    }

    [HttpPut("{voucherNo}")]
    [MustHavePermission(AppAction.Update, AppResource.PurchaseReturns)]
    [OpenApiOperation("Update an existing purchase return voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        PurchaseReturnUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _purchaseReturnService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Purchase return updated.".ToInformationResponse("Purchase return updated.");
    }

    [HttpDelete("{voucherNo}")]
    [MustHavePermission(AppAction.Delete, AppResource.PurchaseReturns)]
    [OpenApiOperation("Delete a purchase return voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        await _purchaseReturnService.DeleteAsync(voucherNo, cancellationToken);
        return "Purchase return deleted.".ToInformationResponse("Purchase return deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [MustHavePermission(AppAction.Delete, AppResource.PurchaseReturns)]
    [OpenApiOperation("Delete a single line from a purchase return voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        await _purchaseReturnService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Purchase return line deleted.".ToInformationResponse("Purchase return line deleted.");
    }
}

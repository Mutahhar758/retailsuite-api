using Retailer.Application.Legacy.SaleReturns;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class SaleReturnsController : VersionNeutralApiController
{
    private readonly ISaleReturnService _saleReturnService;

    public SaleReturnsController(ISaleReturnService saleReturnService)
    {
        _saleReturnService = saleReturnService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.SaleReturns)]
    [OpenApiOperation("Get sale return voucher list.", "")]
    public async Task<HttpResponseDto<List<SaleReturnResponse>>> GetListAsync(
        [FromQuery] SaleReturnListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _saleReturnService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [MustHavePermission(AppAction.View, AppResource.SaleReturns)]
    [OpenApiOperation("Get sale return voucher detail.", "")]
    public async Task<HttpResponseDto<List<SaleReturnLineResponse>>> GetDetailAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        var result = await _saleReturnService.GetDetailAsync(voucherNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.SaleReturns)]
    [OpenApiOperation("Create a new sale return voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(SaleReturnCreateRequest request, CancellationToken cancellationToken)
    {
        var voucherNo = await _saleReturnService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Sale return created.");
    }

    [HttpPut("{voucherNo}")]
    [MustHavePermission(AppAction.Update, AppResource.SaleReturns)]
    [OpenApiOperation("Update an existing sale return voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        SaleReturnUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _saleReturnService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Sale return updated.".ToInformationResponse("Sale return updated.");
    }

    [HttpDelete("{voucherNo}")]
    [MustHavePermission(AppAction.Delete, AppResource.SaleReturns)]
    [OpenApiOperation("Delete a sale return voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        await _saleReturnService.DeleteAsync(voucherNo, cancellationToken);
        return "Sale return deleted.".ToInformationResponse("Sale return deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [MustHavePermission(AppAction.Delete, AppResource.SaleReturns)]
    [OpenApiOperation("Delete a single line from a sale return voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        await _saleReturnService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Sale return line deleted.".ToInformationResponse("Sale return line deleted.");
    }
}

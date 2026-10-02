using Retailer.Application.Legacy.MobileShop;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

[Route("api/mobile/imei")]
public class ImeiController : VersionNeutralApiController
{
    private readonly IImeiService _imeiService;

    public ImeiController(IImeiService imeiService)
    {
        _imeiService = imeiService;
    }

    [HttpGet("stock")]
    [MustHavePermission(AppAction.View, AppResource.ImeiStock)]
    [OpenApiOperation("Get current IMEI handsets stock.", "")]
    public async Task<HttpResponseDto<List<ImeiStockResponse>>> GetImeiStockAsync(CancellationToken cancellationToken)
    {
        var result = await _imeiService.GetImeiStockAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{imei}")]
    [MustHavePermission(AppAction.View, AppResource.WarrantyLookup)]
    [OpenApiOperation("Get full 360-degree timeline and warranty history of an IMEI.", "")]
    public async Task<HttpResponseDto<ImeiHistoryResponse?>> GetImeiHistoryAsync(string imei, CancellationToken cancellationToken)
    {
        var result = await _imeiService.GetImeiHistoryAsync(imei, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost("cost")]
    [MustHavePermission(AppAction.Create, AppResource.ImeiStock)]
    [OpenApiOperation("Capitalize cost / add expense into an in-stock IMEI (PTA tax, screen replacement, refurbishment).", "")]
    public async Task<HttpResponseDto<string>> AddImeiCostAsync(ImeiCostAdditionRequest request, CancellationToken cancellationToken)
    {
        var id = await _imeiService.AddImeiCostAsync(request, cancellationToken);
        return id.ToString().ToInformationResponse("Expense capitalized to device successfully.");
    }
}

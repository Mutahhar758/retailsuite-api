using Retailer.Application.Legacy.Manufacturing;
using Retailer.Domain.Common.Enums;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class ManufacturingController : VersionNeutralApiController
{
    private readonly IManufacturingService _manufacturingService;

    public ManufacturingController(IManufacturingService manufacturingService)
    {
        _manufacturingService = manufacturingService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.Manufacturing)]
    [OpenApiOperation("Get manufacturing voucher list.", "")]
    public async Task<HttpResponseDto<List<ProductionResponse>>> GetListAsync(
        [FromQuery] ProductionListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _manufacturingService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [MustHavePermission(AppAction.View, AppResource.Manufacturing)]
    [OpenApiOperation("Get manufacturing voucher detail.", "")]
    public async Task<HttpResponseDto<ProductionResponse>> GetDetailAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        var result = await _manufacturingService.GetDetailAsync(voucherNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost("estimate")]
    [MustHavePermission(AppAction.View, AppResource.Manufacturing)]
    [OpenApiOperation("Calculate estimated consumption and costs for a production run.", "")]
    public async Task<HttpResponseDto<ProductionEstimateResponse>> CalculateEstimateAsync(
        ProductionEstimateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _manufacturingService.CalculateEstimateAsync(request, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.Manufacturing)]
    [OpenApiOperation("Create a new manufacturing voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(
        ProductionCreateRequest request,
        CancellationToken cancellationToken)
    {
        var voucherNo = await _manufacturingService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Manufacturing voucher created.");
    }

    [HttpPut("{voucherNo}")]
    [MustHavePermission(AppAction.Update, AppResource.Manufacturing)]
    [OpenApiOperation("Update an existing manufacturing voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        ProductionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _manufacturingService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Manufacturing voucher updated.".ToInformationResponse("Manufacturing voucher updated.");
    }

    [HttpPatch("{voucherNo}/status")]
    [MustHavePermission(AppAction.Update, AppResource.Manufacturing)]
    [OpenApiOperation("Update the status of a manufacturing voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateStatusAsync(
        string voucherNo,
        [FromQuery] ProductionStatus status,
        CancellationToken cancellationToken)
    {
        await _manufacturingService.UpdateStatusAsync(voucherNo, status, cancellationToken);
        return $"Manufacturing voucher status updated to {status}.".ToInformationResponse("Status updated.");
    }

    [HttpDelete("{voucherNo}")]
    [MustHavePermission(AppAction.Delete, AppResource.Manufacturing)]
    [OpenApiOperation("Delete a manufacturing voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        await _manufacturingService.DeleteAsync(voucherNo, cancellationToken);
        return "Manufacturing voucher deleted.".ToInformationResponse("Manufacturing voucher deleted.");
    }
}

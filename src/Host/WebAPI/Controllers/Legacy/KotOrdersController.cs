using Retailer.Application.Legacy.Kots;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Retailer.Host.Controllers.Legacy;

public class KotOrdersController : VersionNeutralApiController
{
    private readonly IKotService _kotService;

    public KotOrdersController(IKotService kotService)
    {
        _kotService = kotService;
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.KotOrders)]
    [OpenApiOperation("Create a new KOT Order.", "")]
    public async Task<HttpResponseDto<KotOrderResponse>> CreateAsync(KotOrderCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _kotService.CreateAsync(request, cancellationToken);
        return result.ToInformationResponse("KOT Order created.");
    }

    [HttpGet("active")]
    [MustHavePermission(AppAction.View, AppResource.KotOrders)]
    [OpenApiOperation("Get active KOT orders for KDS display.", "")]
    public async Task<HttpResponseDto<List<KotOrderResponse>>> GetActiveAsync([FromQuery] string? prepStationId, CancellationToken cancellationToken)
    {
        var result = await _kotService.GetActiveListAsync(prepStationId, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("token/{query}")]
    [MustHavePermission(AppAction.View, AppResource.KotOrders)]
    [OpenApiOperation("Get KOT order by Token # or ID.", "")]
    public async Task<HttpResponseDto<KotOrderResponse?>> GetByTokenAsync(string query, CancellationToken cancellationToken)
    {
        var result = await _kotService.GetByTokenOrIdAsync(query, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPut("{id}/items/{itemId}/status")]
    [MustHavePermission(AppAction.Update, AppResource.KotOrders)]
    [OpenApiOperation("Update status of an item in KOT order.", "")]
    public async Task<HttpResponseDto<string>> UpdateItemStatusAsync(int id, int itemId, [FromBody] string status, CancellationToken cancellationToken)
    {
        await _kotService.UpdateItemStatusAsync(id, itemId, status, cancellationToken);
        return "Item status updated.".ToInformationResponse("Item status updated.");
    }

    [HttpPut("{id}/status")]
    [MustHavePermission(AppAction.Update, AppResource.KotOrders)]
    [OpenApiOperation("Update status of a KOT order.", "")]
    public async Task<HttpResponseDto<string>> UpdateOrderStatusAsync(int id, [FromBody] string status, CancellationToken cancellationToken)
    {
        await _kotService.UpdateOrderStatusAsync(id, status, cancellationToken);
        return "Order status updated.".ToInformationResponse("Order status updated.");
    }

    [HttpPut("{id}/finalize")]
    [MustHavePermission(AppAction.Update, AppResource.KotOrders)]
    [OpenApiOperation("Finalize a KOT order by associating it with a completed sale.", "")]
    public async Task<HttpResponseDto<string>> FinalizePaymentAsync(int id, [FromBody] string saleVoucherNo, CancellationToken cancellationToken)
    {
        await _kotService.FinalizePaymentAsync(id, saleVoucherNo, cancellationToken);
        return "Order finalized and paid.".ToInformationResponse("Order finalized and paid.");
    }
}

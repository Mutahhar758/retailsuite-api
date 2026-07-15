using Retailer.Application.Legacy.SupplyOrders;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class SupplyOrdersController : VersionNeutralApiController
{
    private readonly ISupplyOrderService _supplyOrderService;

    public SupplyOrdersController(ISupplyOrderService supplyOrderService)
    {
        _supplyOrderService = supplyOrderService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.SupplyOrders)]
    [OpenApiOperation("Get supply orders.", "")]
    public async Task<HttpResponseDto<List<SupplyOrderResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var supplyOrders = await _supplyOrderService.GetAsync(cancellationToken);
        return supplyOrders.ToInformationResponse();
    }

    [HttpGet("{id:int}")]
    [MustHavePermission(AppAction.View, AppResource.SupplyOrders)]
    [OpenApiOperation("Get supply order by id.", "")]
    public async Task<HttpResponseDto<SupplyOrderResponse?>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var supplyOrder = await _supplyOrderService.GetByIdAsync(id, cancellationToken);
        return supplyOrder.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.SupplyOrders)]
    [OpenApiOperation("Create supply order.", "")]
    public async Task<HttpResponseDto<SupplyOrderResponse>> CreateAsync(SupplyOrderUpsertRequest request, CancellationToken cancellationToken)
    {
        var id = await _supplyOrderService.CreateAsync(request, cancellationToken);
        var supplyOrder = await _supplyOrderService.GetByIdAsync(id, cancellationToken)
            ?? new SupplyOrderResponse { Id = id, Title = request.Title };

        return supplyOrder.ToInformationResponse("Supply order created.");
    }

    [HttpPut("{id:int}")]
    [MustHavePermission(AppAction.Update, AppResource.SupplyOrders)]
    [OpenApiOperation("Update supply order.", "")]
    public async Task<HttpResponseDto<SupplyOrderResponse>> UpdateAsync(int id, SupplyOrderUpsertRequest request, CancellationToken cancellationToken)
    {
        var updatedId = await _supplyOrderService.UpdateAsync(id, request, cancellationToken);
        var supplyOrder = await _supplyOrderService.GetByIdAsync(updatedId, cancellationToken)
            ?? new SupplyOrderResponse { Id = updatedId, Title = request.Title };

        return supplyOrder.ToInformationResponse("Supply order updated.");
    }

    [HttpDelete("{id:int}")]
    [MustHavePermission(AppAction.Delete, AppResource.SupplyOrders)]
    [OpenApiOperation("Delete supply order.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _supplyOrderService.DeleteAsync(id, cancellationToken);
        return "Supply order deleted.".ToInformationResponse("Supply order deleted.");
    }
}

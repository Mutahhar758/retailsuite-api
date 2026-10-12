using Retailer.Application.Legacy.RecurringChains;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class RecurringChainsController : VersionNeutralApiController
{
    private readonly IRecurringChainService _recurringChainService;

    public RecurringChainsController(IRecurringChainService recurringChainService)
    {
        _recurringChainService = recurringChainService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.RecurringVouchers)]
    [OpenApiOperation("Get all recurring voucher chains.", "")]
    public async Task<HttpResponseDto<List<RecurringChainResponse>>> GetListAsync(CancellationToken cancellationToken)
    {
        var result = await _recurringChainService.GetListAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{id:int}")]
    [MustHavePermission(AppAction.View, AppResource.RecurringVouchers)]
    [OpenApiOperation("Get recurring voucher chain by ID.", "")]
    public async Task<HttpResponseDto<RecurringChainResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _recurringChainService.GetByIdAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.RecurringVouchers)]
    [OpenApiOperation("Create a recurring voucher chain from an existing voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateFromVoucherAsync(RecurringChainCreateRequest request, CancellationToken cancellationToken)
    {
        var id = await _recurringChainService.CreateFromVoucherAsync(request, cancellationToken);
        return id.ToString().ToInformationResponse("Recurring chain created successfully.");
    }

    [HttpPut("{id:int}")]
    [MustHavePermission(AppAction.Update, AppResource.RecurringVouchers)]
    [OpenApiOperation("Update a recurring voucher chain.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(int id, RecurringChainUpdateRequest request, CancellationToken cancellationToken)
    {
        await _recurringChainService.UpdateAsync(id, request, cancellationToken);
        return "Recurring chain updated.".ToInformationResponse("Recurring chain updated.");
    }

    [HttpPut("{id:int}/toggle")]
    [MustHavePermission(AppAction.Update, AppResource.RecurringVouchers)]
    [OpenApiOperation("Toggle active/inactive status of a recurring chain.", "")]
    public async Task<HttpResponseDto<string>> ToggleActiveAsync(int id, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        await _recurringChainService.ToggleActiveAsync(id, isActive, cancellationToken);
        var msg = isActive ? "Recurring chain enabled." : "Recurring chain disabled.";
        return msg.ToInformationResponse(msg);
    }

    [HttpDelete("{id:int}")]
    [MustHavePermission(AppAction.Delete, AppResource.RecurringVouchers)]
    [OpenApiOperation("Delete a recurring voucher chain.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _recurringChainService.DeleteAsync(id, cancellationToken);
        return "Recurring chain deleted.".ToInformationResponse("Recurring chain deleted.");
    }

    [HttpPost("{id:int}/run")]
    [MustHavePermission(AppAction.Create, AppResource.RecurringVouchers)]
    [OpenApiOperation("Manually run today's voucher for a recurring chain.", "")]
    public async Task<HttpResponseDto<string>> RunTodayAsync(int id, [FromQuery] DateOnly? targetDate, CancellationToken cancellationToken)
    {
        var voucherNo = await _recurringChainService.RunChainForTodayAsync(id, targetDate, cancellationToken);
        return (voucherNo ?? string.Empty).ToInformationResponse(voucherNo != null ? $"Voucher '{voucherNo}' generated." : "No voucher generated.");
    }
}

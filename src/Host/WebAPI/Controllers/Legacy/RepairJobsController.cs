using Retailer.Application.Legacy.MobileShop;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

[Route("api/repair-jobs")]
public class RepairJobsController : VersionNeutralApiController
{
    private readonly IRepairJobService _repairJobService;

    public RepairJobsController(IRepairJobService repairJobService)
    {
        _repairJobService = repairJobService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.RepairJobs)]
    [OpenApiOperation("Get repair job cards list.", "")]
    public async Task<HttpResponseDto<List<RepairJobResponse>>> GetListAsync(
        [FromQuery] RepairJobListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _repairJobService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{jobNo}")]
    [MustHavePermission(AppAction.View, AppResource.RepairJobs)]
    [OpenApiOperation("Get repair job card detail.", "")]
    public async Task<HttpResponseDto<RepairJobResponse?>> GetByIdAsync(
        string jobNo,
        CancellationToken cancellationToken)
    {
        var result = await _repairJobService.GetByIdAsync(jobNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.RepairJobs)]
    [OpenApiOperation("Create a new repair job card.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(
        RepairJobCreateRequest request,
        CancellationToken cancellationToken)
    {
        var jobNo = await _repairJobService.CreateAsync(request, cancellationToken);
        return jobNo.ToInformationResponse("Repair job card created.");
    }

    [HttpPut("{jobNo}")]
    [MustHavePermission(AppAction.Update, AppResource.RepairJobs)]
    [OpenApiOperation("Update a repair job card.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string jobNo,
        RepairJobUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _repairJobService.UpdateAsync(jobNo, request, cancellationToken);
        return "Repair job card updated.".ToInformationResponse("Repair job card updated.");
    }

    [HttpPut("{jobNo}/status")]
    [MustHavePermission(AppAction.Update, AppResource.RepairJobs)]
    [OpenApiOperation("Update repair job status.", "")]
    public async Task<HttpResponseDto<string>> UpdateStatusAsync(
        string jobNo,
        RepairJobStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _repairJobService.UpdateStatusAsync(jobNo, request, cancellationToken);
        return "Repair job status updated.".ToInformationResponse("Repair job status updated.");
    }

    [HttpPost("{jobNo}/bill")]
    [MustHavePermission(AppAction.Create, AppResource.Sales)]
    [OpenApiOperation("Convert completed repair job card into a Sale voucher.", "")]
    public async Task<HttpResponseDto<string>> BillJobAsync(
        string jobNo,
        RepairJobBillRequest request,
        CancellationToken cancellationToken)
    {
        var saleVNo = await _repairJobService.BillJobAsync(jobNo, request, cancellationToken);
        return saleVNo.ToInformationResponse("Repair job billed successfully.");
    }

    [HttpDelete("{jobNo}")]
    [MustHavePermission(AppAction.Delete, AppResource.RepairJobs)]
    [OpenApiOperation("Delete/cancel a repair job card.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(
        string jobNo,
        CancellationToken cancellationToken)
    {
        await _repairJobService.DeleteAsync(jobNo, cancellationToken);
        return "Repair job card deleted.".ToInformationResponse("Repair job card deleted.");
    }
}

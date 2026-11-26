using Retailer.Application.Payroll.Components;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Payroll;

[Route("api/payroll/components")]
public class PayrollComponentsController : VersionNeutralApiController
{
    private readonly IPayrollComponentService _service;

    public PayrollComponentsController(IPayrollComponentService service)
    {
        _service = service;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.PayrollComponents)]
    [OpenApiOperation("Get all payroll components.", "")]
    public async Task<HttpResponseDto<List<PayrollComponentDto>>> GetAllAsync(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(isActive, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{id:int}")]
    [MustHavePermission(AppAction.View, AppResource.PayrollComponents)]
    [OpenApiOperation("Get payroll component by ID.", "")]
    public async Task<HttpResponseDto<PayrollComponentDto?>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.PayrollComponents)]
    [OpenApiOperation("Create a payroll component.", "")]
    public async Task<HttpResponseDto<object>> CreateAsync(PayrollComponentUpsertRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request, cancellationToken);
        return ((object)id).ToInformationResponse("Payroll component created.");
    }

    [HttpPut("{id:int}")]
    [MustHavePermission(AppAction.Update, AppResource.PayrollComponents)]
    [OpenApiOperation("Update a payroll component.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(int id, PayrollComponentUpsertRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(id, request, cancellationToken);
        return "Payroll component updated.".ToInformationResponse("Payroll component updated.");
    }

    [HttpDelete("{id:int}")]
    [MustHavePermission(AppAction.Delete, AppResource.PayrollComponents)]
    [OpenApiOperation("Delete a payroll component.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return "Payroll component deleted.".ToInformationResponse("Payroll component deleted.");
    }
}

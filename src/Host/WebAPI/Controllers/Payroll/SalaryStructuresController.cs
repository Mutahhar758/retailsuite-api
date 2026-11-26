using Retailer.Application.Payroll.Structures;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Payroll;

[Route("api/payroll/structures")]
public class SalaryStructuresController : VersionNeutralApiController
{
    private readonly ISalaryStructureService _service;

    public SalaryStructuresController(ISalaryStructureService service)
    {
        _service = service;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.SalaryStructures)]
    [OpenApiOperation("Get all salary structures.", "")]
    public async Task<HttpResponseDto<List<SalaryStructureDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{id:int}")]
    [MustHavePermission(AppAction.View, AppResource.SalaryStructures)]
    [OpenApiOperation("Get salary structure by ID.", "")]
    public async Task<HttpResponseDto<SalaryStructureDto?>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.SalaryStructures)]
    [OpenApiOperation("Create a salary structure.", "")]
    public async Task<HttpResponseDto<object>> CreateAsync(SalaryStructureUpsertRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request, cancellationToken);
        return ((object)id).ToInformationResponse("Salary structure created.");
    }

    [HttpPut("{id:int}")]
    [MustHavePermission(AppAction.Update, AppResource.SalaryStructures)]
    [OpenApiOperation("Update a salary structure.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(int id, SalaryStructureUpsertRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(id, request, cancellationToken);
        return "Salary structure updated.".ToInformationResponse("Salary structure updated.");
    }

    [HttpDelete("{id:int}")]
    [MustHavePermission(AppAction.Delete, AppResource.SalaryStructures)]
    [OpenApiOperation("Delete a salary structure.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return "Salary structure deleted.".ToInformationResponse("Salary structure deleted.");
    }

    [HttpGet("employee/{hrInfoId}/components")]
    [MustHavePermission(AppAction.View, AppResource.HRInfo)]
    [OpenApiOperation("Get employee custom salary component overrides.", "")]
    public async Task<HttpResponseDto<List<EmployeeSalaryComponentDto>>> GetEmployeeOverridesAsync(string hrInfoId, CancellationToken cancellationToken)
    {
        var result = await _service.GetEmployeeOverridesAsync(hrInfoId, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPut("employee/{hrInfoId}/components")]
    [MustHavePermission(AppAction.Update, AppResource.HRInfo)]
    [OpenApiOperation("Save employee custom salary component overrides.", "")]
    public async Task<HttpResponseDto<string>> SaveEmployeeOverridesAsync(
        string hrInfoId,
        List<EmployeeSalaryComponentUpsertItem> items,
        CancellationToken cancellationToken)
    {
        await _service.SaveEmployeeOverridesAsync(hrInfoId, items, cancellationToken);
        return "Employee salary components saved.".ToInformationResponse("Employee salary components saved.");
    }
}

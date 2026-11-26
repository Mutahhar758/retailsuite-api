using Retailer.Application.Payroll.Loans;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Payroll;

[Route("api/payroll/loans")]
public class EmployeeLoansController : VersionNeutralApiController
{
    private readonly IEmployeeLoanService _service;

    public EmployeeLoansController(IEmployeeLoanService service)
    {
        _service = service;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.EmployeeLoans)]
    [OpenApiOperation("Get employee loans and salary advances.", "")]
    public async Task<HttpResponseDto<List<EmployeeLoanDto>>> GetListAsync(
        [FromQuery] EmployeeLoanFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{id:int}")]
    [MustHavePermission(AppAction.View, AppResource.EmployeeLoans)]
    [OpenApiOperation("Get employee loan details and schedule.", "")]
    public async Task<HttpResponseDto<EmployeeLoanDto?>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.EmployeeLoans)]
    [OpenApiOperation("Create a new employee loan or salary advance.", "")]
    public async Task<HttpResponseDto<object>> CreateAsync(EmployeeLoanCreateRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request, cancellationToken);
        return ((object)id).ToInformationResponse("Employee loan created.");
    }

    [HttpPost("{id:int}/approve")]
    [MustHavePermission(AppAction.Approve, AppResource.EmployeeLoans)]
    [OpenApiOperation("Approve a pending employee loan.", "")]
    public async Task<HttpResponseDto<string>> ApproveAsync(int id, CancellationToken cancellationToken)
    {
        await _service.ApproveAsync(id, cancellationToken);
        return "Employee loan approved.".ToInformationResponse("Employee loan approved.");
    }

    [HttpPost("{id:int}/disburse")]
    [MustHavePermission(AppAction.Update, AppResource.EmployeeLoans)]
    [OpenApiOperation("Disburse an approved employee loan and post GL entry.", "")]
    public async Task<HttpResponseDto<string>> DisburseAsync(int id, EmployeeLoanDisburseRequest request, CancellationToken cancellationToken)
    {
        await _service.DisburseAsync(id, request, cancellationToken);
        return "Employee loan disbursed.".ToInformationResponse("Employee loan disbursed.");
    }

    [HttpPost("{id:int}/cancel")]
    [MustHavePermission(AppAction.Delete, AppResource.EmployeeLoans)]
    [OpenApiOperation("Cancel a pending/approved employee loan.", "")]
    public async Task<HttpResponseDto<string>> CancelAsync(int id, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        await _service.CancelAsync(id, reason, cancellationToken);
        return "Employee loan cancelled.".ToInformationResponse("Employee loan cancelled.");
    }

    [HttpPost("{loanId:int}/schedules/{scheduleId:int}/skip")]
    [MustHavePermission(AppAction.Update, AppResource.EmployeeLoans)]
    [OpenApiOperation("Skip a scheduled loan installment and defer to the end of schedule.", "")]
    public async Task<HttpResponseDto<string>> SkipInstallmentAsync(
        int loanId,
        int scheduleId,
        LoanInstallmentActionRequest request,
        CancellationToken cancellationToken)
    {
        await _service.SkipInstallmentAsync(loanId, scheduleId, request, cancellationToken);
        return "Installment skipped and deferred.".ToInformationResponse("Installment skipped and deferred.");
    }

    [HttpPost("{loanId:int}/schedules/{scheduleId:int}/settle")]
    [MustHavePermission(AppAction.Update, AppResource.EmployeeLoans)]
    [OpenApiOperation("Manually settle a scheduled loan installment.", "")]
    public async Task<HttpResponseDto<string>> SettleInstallmentAsync(
        int loanId,
        int scheduleId,
        LoanInstallmentActionRequest request,
        CancellationToken cancellationToken)
    {
        await _service.SettleInstallmentManuallyAsync(loanId, scheduleId, request, cancellationToken);
        return "Installment settled manually.".ToInformationResponse("Installment settled manually.");
    }
}

using Retailer.Application.Payroll.Disbursements;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Payroll;

[Route("api/payroll/disbursements")]
public class PayrollDisbursementsController : VersionNeutralApiController
{
    private readonly IPayrollDisbursementService _service;

    public PayrollDisbursementsController(IPayrollDisbursementService service)
    {
        _service = service;
    }

    [HttpGet("by-run/{payrollRunId:int}")]
    [MustHavePermission(AppAction.View, AppResource.PayrollDisbursements)]
    [OpenApiOperation("Get disbursements for a payroll run.", "")]
    public async Task<HttpResponseDto<List<PayrollDisbursementDto>>> GetByRunAsync(int payrollRunId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByRunIdAsync(payrollRunId, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{id:int}")]
    [MustHavePermission(AppAction.View, AppResource.PayrollDisbursements)]
    [OpenApiOperation("Get disbursement details by ID.", "")]
    public async Task<HttpResponseDto<PayrollDisbursementDto?>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.PayrollDisbursements)]
    [OpenApiOperation("Create a salary disbursement batch and post Payment Voucher to GL.", "")]
    public async Task<HttpResponseDto<PayrollDisbursementDto>> CreateAsync(PayrollDisbursementCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.ToInformationResponse($"Disbursement {result.DisbursementNo} posted.");
    }

    [HttpDelete("{id:int}")]
    [MustHavePermission(AppAction.Delete, AppResource.PayrollDisbursements)]
    [OpenApiOperation("Void a salary disbursement batch and reverse its GL Payment Voucher.", "")]
    public async Task<HttpResponseDto<string>> VoidAsync(int id, CancellationToken cancellationToken)
    {
        await _service.VoidAsync(id, cancellationToken);
        return "Disbursement voided.".ToInformationResponse("Disbursement voided.");
    }

    [HttpGet("{id:int}/bank-advice-csv")]
    [MustHavePermission(AppAction.Export, AppResource.PayrollDisbursements)]
    [OpenApiOperation("Export bank transfer advice CSV for a disbursement batch.", "")]
    public async Task<IActionResult> ExportBankAdviceCsvAsync(int id, CancellationToken cancellationToken)
    {
        var (content, fileName) = await _service.ExportBankAdviceCsvAsync(id, cancellationToken);
        return File(content, "text/csv", fileName);
    }
}

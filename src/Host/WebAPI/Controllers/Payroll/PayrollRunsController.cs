using Retailer.Application.Payroll.Runs;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Payroll;

[Route("api/payroll/runs")]
public class PayrollRunsController : VersionNeutralApiController
{
    private readonly IPayrollRunService _service;

    public PayrollRunsController(IPayrollRunService service)
    {
        _service = service;
    }

    [HttpGet("kpis")]
    [MustHavePermission(AppAction.View, AppResource.PayrollRuns)]
    [OpenApiOperation("Get payroll dashboard KPIs.", "")]
    public async Task<HttpResponseDto<PayrollDashboardKpiDto>> GetKpisAsync(CancellationToken cancellationToken)
    {
        var result = await _service.GetDashboardKpisAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.PayrollRuns)]
    [OpenApiOperation("Get payroll runs list.", "")]
    public async Task<HttpResponseDto<List<PayrollRunSummaryDto>>> GetListAsync(
        [FromQuery] PayrollRunFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{id:int}")]
    [MustHavePermission(AppAction.View, AppResource.PayrollRuns)]
    [OpenApiOperation("Get payroll run detail with employee items and components.", "")]
    public async Task<HttpResponseDto<PayrollRunDetailDto?>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.PayrollRuns)]
    [OpenApiOperation("Create a new payroll run.", "")]
    public async Task<HttpResponseDto<object>> CreateAsync(PayrollRunCreateRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request, cancellationToken);
        return ((object)id).ToInformationResponse("Payroll run created.");
    }

    [HttpPost("{id:int}/calculate")]
    [MustHavePermission(AppAction.Update, AppResource.PayrollRuns)]
    [OpenApiOperation("Calculate or recalculate a Draft/Calculated payroll run.", "")]
    public async Task<HttpResponseDto<PayrollRunDetailDto>> CalculateAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _service.CalculateAsync(id, cancellationToken);
        return result.ToInformationResponse("Payroll run calculated.");
    }

    [HttpPut("{runId:int}/items/{itemId:int}")]
    [MustHavePermission(AppAction.Update, AppResource.PayrollRuns)]
    [OpenApiOperation("Update an individual employee line in a Draft/Calculated payroll run.", "")]
    public async Task<HttpResponseDto<PayrollRunItemDto>> UpdateItemAsync(
        int runId,
        int itemId,
        PayrollRunItemUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateItemAsync(runId, itemId, request, cancellationToken);
        return result.ToInformationResponse("Payroll item updated.");
    }

    [HttpPost("{id:int}/submit")]
    [MustHavePermission(AppAction.Update, AppResource.PayrollRuns)]
    [OpenApiOperation("Submit a calculated payroll run for approval.", "")]
    public async Task<HttpResponseDto<string>> SubmitAsync(int id, CancellationToken cancellationToken)
    {
        await _service.SubmitForApprovalAsync(id, cancellationToken);
        return "Payroll run submitted for approval.".ToInformationResponse("Payroll run submitted for approval.");
    }

    [HttpPost("{id:int}/approve")]
    [MustHavePermission(AppAction.Approve, AppResource.PayrollRuns)]
    [OpenApiOperation("Approve a pending payroll run.", "")]
    public async Task<HttpResponseDto<string>> ApproveAsync(int id, CancellationToken cancellationToken)
    {
        await _service.ApproveAsync(id, cancellationToken);
        return "Payroll run approved.".ToInformationResponse("Payroll run approved.");
    }

    [HttpPost("{id:int}/reject")]
    [MustHavePermission(AppAction.Approve, AppResource.PayrollRuns)]
    [OpenApiOperation("Reject a pending payroll run back to Calculated state.", "")]
    public async Task<HttpResponseDto<string>> RejectAsync(int id, [FromBody] PayrollReasonRequest request, CancellationToken cancellationToken)
    {
        await _service.RejectAsync(id, request.Reason ?? string.Empty, cancellationToken);
        return "Payroll run rejected.".ToInformationResponse("Payroll run rejected.");
    }

    [HttpGet("{id:int}/gl-preview")]
    [MustHavePermission(AppAction.View, AppResource.PayrollRuns)]
    [OpenApiOperation("Preview double-entry GL postings for a payroll run.", "")]
    public async Task<HttpResponseDto<PayrollGlPreviewDto>> GetGlPreviewAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetGlPreviewAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost("{id:int}/post")]
    [MustHavePermission(AppAction.Approve, AppResource.PayrollRuns)]
    [OpenApiOperation("Post an approved payroll run to the General Ledger.", "")]
    public async Task<HttpResponseDto<string>> PostAsync(int id, CancellationToken cancellationToken)
    {
        var voucherNo = await _service.PostAsync(id, cancellationToken);
        return voucherNo.ToInformationResponse($"Payroll posted to GL (Voucher PR-{voucherNo}).");
    }

    [HttpPost("{id:int}/reverse")]
    [MustHavePermission(AppAction.Approve, AppResource.PayrollRuns)]
    [OpenApiOperation("Reverse a posted payroll run and restore loan schedules.", "")]
    public async Task<HttpResponseDto<string>> ReverseAsync(int id, [FromBody] PayrollReasonRequest request, CancellationToken cancellationToken)
    {
        var revVoucher = await _service.ReverseAsync(id, request.Reason ?? string.Empty, cancellationToken);
        return revVoucher.ToInformationResponse($"Payroll reversed (Voucher PR-{revVoucher}).");
    }

    [HttpPost("{id:int}/cancel")]
    [MustHavePermission(AppAction.Delete, AppResource.PayrollRuns)]
    [OpenApiOperation("Cancel a Draft or Calculated payroll run.", "")]
    public async Task<HttpResponseDto<string>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        await _service.CancelAsync(id, cancellationToken);
        return "Payroll run cancelled.".ToInformationResponse("Payroll run cancelled.");
    }

    [HttpGet("{runId:int}/items/{itemId:int}/payslip-pdf")]
    [MustHavePermission(AppAction.Print, AppResource.PayrollRuns)]
    [OpenApiOperation("Download individual employee payslip PDF.", "")]
    public async Task<IActionResult> GetPayslipPdfAsync(int runId, int itemId, CancellationToken cancellationToken)
    {
        var bytes = await _service.GeneratePayslipPdfAsync(runId, itemId, cancellationToken);
        return File(bytes, "application/pdf", $"Payslip_Run{runId}_Item{itemId}.pdf");
    }

    [HttpGet("{runId:int}/payslips-pdf")]
    [MustHavePermission(AppAction.Print, AppResource.PayrollRuns)]
    [OpenApiOperation("Download bulk payslips PDF for all employees in a run.", "")]
    public async Task<IActionResult> GetBulkPayslipsPdfAsync(int runId, CancellationToken cancellationToken)
    {
        var bytes = await _service.GenerateBulkPayslipsPdfAsync(runId, cancellationToken);
        return File(bytes, "application/pdf", $"Payslips_Run{runId}.pdf");
    }

    [HttpGet("{runId:int}/register-pdf")]
    [MustHavePermission(AppAction.Export, AppResource.PayrollRuns)]
    [OpenApiOperation("Download payroll register PDF for a run.", "")]
    public async Task<IActionResult> GetRegisterPdfAsync(int runId, CancellationToken cancellationToken)
    {
        var bytes = await _service.GeneratePayrollRegisterPdfAsync(runId, cancellationToken);
        return File(bytes, "application/pdf", $"PayrollRegister_Run{runId}.pdf");
    }
}

public class PayrollReasonRequest
{
    public string? Reason { get; set; }
}

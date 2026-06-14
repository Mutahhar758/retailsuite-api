using Retailer.Application.Legacy.Payrolls;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class PayrollsController : VersionNeutralApiController
{
    private readonly IPayrollService _payrollService;

    public PayrollsController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpGet]
    [OpenApiOperation("Get payroll voucher list.", "")]
    public async Task<HttpResponseDto<List<PayrollResponse>>> GetListAsync(
        [FromQuery] PayrollListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _payrollService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("lookups")]
    [OpenApiOperation("Get payroll lookup data.", "")]
    public async Task<HttpResponseDto<PayrollLookupsResponse>> GetLookupsAsync(CancellationToken cancellationToken)
    {
        var result = await _payrollService.GetLookupsAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [OpenApiOperation("Get payroll voucher detail.", "")]
    public async Task<HttpResponseDto<List<PayrollLineResponse>>> GetDetailAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        var result = await _payrollService.GetDetailAsync(voucherNo, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create a new payroll voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(PayrollUpsertRequest request, CancellationToken cancellationToken)
    {
        var voucherNo = await _payrollService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Payroll created.");
    }

    [HttpPut("{voucherNo}")]
    [OpenApiOperation("Update an existing payroll voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        PayrollUpsertRequest request,
        CancellationToken cancellationToken)
    {
        await _payrollService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Payroll updated.".ToInformationResponse("Payroll updated.");
    }

    [HttpDelete("{voucherNo}")]
    [OpenApiOperation("Delete a payroll voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        await _payrollService.DeleteAsync(voucherNo, cancellationToken);
        return "Payroll deleted.".ToInformationResponse("Payroll deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [OpenApiOperation("Delete a single line from payroll voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(string voucherNo, long seq, CancellationToken cancellationToken)
    {
        await _payrollService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Payroll line deleted.".ToInformationResponse("Payroll line deleted.");
    }
}

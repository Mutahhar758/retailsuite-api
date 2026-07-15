using Retailer.Application.Legacy.JournalVouchers;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class JournalVouchersController : VersionNeutralApiController
{
    private readonly IJournalVoucherService _journalVoucherService;

    public JournalVouchersController(IJournalVoucherService journalVoucherService)
    {
        _journalVoucherService = journalVoucherService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.JournalVouchers)]
    [OpenApiOperation("Get journal voucher list.", "")]
    public async Task<HttpResponseDto<List<JournalVoucherResponse>>> GetListAsync(
        [FromQuery] JournalVoucherListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _journalVoucherService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("accounts/{accountId}/balance")]
    [OpenApiOperation("Get current account balance for journal voucher form.", "")]
    public async Task<HttpResponseDto<JournalVoucherBalanceResponse>> GetAccountBalanceAsync(string accountId, CancellationToken cancellationToken)
    {
        var result = await _journalVoucherService.GetAccountBalanceAsync(accountId, cancellationToken);
        return new JournalVoucherBalanceResponse { Balance = result }.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [MustHavePermission(AppAction.View, AppResource.JournalVouchers)]
    [OpenApiOperation("Get journal voucher detail.", "")]
    public async Task<HttpResponseDto<List<JournalVoucherLineResponse>>> GetDetailAsync(
        string voucherNo,
        [FromQuery] string? account,
        CancellationToken cancellationToken)
    {
        var result = await _journalVoucherService.GetDetailAsync(voucherNo, account, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.JournalVouchers)]
    [OpenApiOperation("Create a new journal voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(
        JournalVoucherCreateRequest request,
        CancellationToken cancellationToken)
    {
        var voucherNo = await _journalVoucherService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Journal voucher created.");
    }

    [HttpPut("{voucherNo}")]
    [MustHavePermission(AppAction.Update, AppResource.JournalVouchers)]
    [OpenApiOperation("Update an existing journal voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        JournalVoucherUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _journalVoucherService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Journal voucher updated.".ToInformationResponse("Journal voucher updated.");
    }

    [HttpDelete("{voucherNo}")]
    [MustHavePermission(AppAction.Delete, AppResource.JournalVouchers)]
    [OpenApiOperation("Delete a journal voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        await _journalVoucherService.DeleteAsync(voucherNo, cancellationToken);
        return "Journal voucher deleted.".ToInformationResponse("Journal voucher deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [MustHavePermission(AppAction.Delete, AppResource.JournalVouchers)]
    [OpenApiOperation("Delete a single line from a journal voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(
        string voucherNo,
        int seq,
        CancellationToken cancellationToken)
    {
        await _journalVoucherService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Journal voucher line deleted.".ToInformationResponse("Journal voucher line deleted.");
    }
}

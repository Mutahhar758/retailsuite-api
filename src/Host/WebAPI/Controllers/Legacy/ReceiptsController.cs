using Retailer.Application.Legacy.Receipts;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class ReceiptsController : VersionNeutralApiController
{
    private readonly IReceiptService _receiptService;

    public ReceiptsController(IReceiptService receiptService)
    {
        _receiptService = receiptService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.ReceiptVouchers)]
    [OpenApiOperation("Get receipt voucher list.", "")]
    public async Task<HttpResponseDto<List<ReceiptResponse>>> GetListAsync(
        [FromQuery] ReceiptListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _receiptService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("accounts/{accountId}/balance")]
    [OpenApiOperation("Get current account balance for receipt form.", "")]
    public async Task<HttpResponseDto<ReceiptBalanceResponse>> GetAccountBalanceAsync(string accountId, CancellationToken cancellationToken)
    {
        var result = await _receiptService.GetAccountBalanceAsync(accountId, cancellationToken);
        return new ReceiptBalanceResponse { Balance = result }.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [MustHavePermission(AppAction.View, AppResource.ReceiptVouchers)]
    [OpenApiOperation("Get receipt voucher detail.", "")]
    public async Task<HttpResponseDto<List<ReceiptLineResponse>>> GetDetailAsync(
        string voucherNo,
        [FromQuery] string? cashBankAccount,
        CancellationToken cancellationToken)
    {
        var result = await _receiptService.GetDetailAsync(voucherNo, cashBankAccount, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.ReceiptVouchers)]
    [OpenApiOperation("Create a new receipt voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(
        ReceiptCreateRequest request,
        CancellationToken cancellationToken)
    {
        var voucherNo = await _receiptService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Receipt created.");
    }

    [HttpPut("{voucherNo}")]
    [MustHavePermission(AppAction.Update, AppResource.ReceiptVouchers)]
    [OpenApiOperation("Update an existing receipt voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        ReceiptUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _receiptService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Receipt updated.".ToInformationResponse("Receipt updated.");
    }

    [HttpDelete("{voucherNo}")]
    [MustHavePermission(AppAction.Delete, AppResource.ReceiptVouchers)]
    [OpenApiOperation("Delete a receipt voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        await _receiptService.DeleteAsync(voucherNo, cancellationToken);
        return "Receipt deleted.".ToInformationResponse("Receipt deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [MustHavePermission(AppAction.Delete, AppResource.ReceiptVouchers)]
    [OpenApiOperation("Delete a single line from a receipt voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(
        string voucherNo,
        int seq,
        CancellationToken cancellationToken)
    {
        await _receiptService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Receipt line deleted.".ToInformationResponse("Receipt line deleted.");
    }
}

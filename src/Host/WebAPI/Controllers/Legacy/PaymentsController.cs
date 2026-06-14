using Retailer.Application.Legacy.Payments;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class PaymentsController : VersionNeutralApiController
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [OpenApiOperation("Get payment voucher list.", "")]
    public async Task<HttpResponseDto<List<PaymentResponse>>> GetListAsync(
        [FromQuery] PaymentListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("accounts/{accountId}/balance")]
    [OpenApiOperation("Get current account balance for payment form.", "")]
    public async Task<HttpResponseDto<PaymentBalanceResponse>> GetAccountBalanceAsync(string accountId, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetAccountBalanceAsync(accountId, cancellationToken);
        return new PaymentBalanceResponse { Balance = result }.ToInformationResponse();
    }

    [HttpGet("{voucherNo}")]
    [OpenApiOperation("Get payment voucher detail.", "")]
    public async Task<HttpResponseDto<List<PaymentLineResponse>>> GetDetailAsync(
        string voucherNo,
        [FromQuery] string? cashBankAccount,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetDetailAsync(voucherNo, cashBankAccount, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create a new payment voucher.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(
        PaymentCreateRequest request,
        CancellationToken cancellationToken)
    {
        var voucherNo = await _paymentService.CreateAsync(request, cancellationToken);
        return voucherNo.ToInformationResponse("Payment created.");
    }

    [HttpPut("{voucherNo}")]
    [OpenApiOperation("Update an existing payment voucher.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        string voucherNo,
        PaymentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _paymentService.UpdateAsync(voucherNo, request, cancellationToken);
        return "Payment updated.".ToInformationResponse("Payment updated.");
    }

    [HttpDelete("{voucherNo}")]
    [OpenApiOperation("Delete a payment voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(
        string voucherNo,
        CancellationToken cancellationToken)
    {
        await _paymentService.DeleteAsync(voucherNo, cancellationToken);
        return "Payment deleted.".ToInformationResponse("Payment deleted.");
    }

    [HttpDelete("{voucherNo}/lines/{seq}")]
    [OpenApiOperation("Delete a single line from a payment voucher.", "")]
    public async Task<HttpResponseDto<string>> DeleteLineAsync(
        string voucherNo,
        int seq,
        CancellationToken cancellationToken)
    {
        await _paymentService.DeleteLineAsync(voucherNo, seq, cancellationToken);
        return "Payment line deleted.".ToInformationResponse("Payment line deleted.");
    }
}

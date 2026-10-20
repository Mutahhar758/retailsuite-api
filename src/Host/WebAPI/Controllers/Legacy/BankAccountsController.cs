using Retailer.Application.Legacy.BankAccounts;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class BankAccountsController : VersionNeutralApiController
{
    private readonly IBankAccountService _bankAccountService;

    public BankAccountsController(IBankAccountService bankAccountService)
    {
        _bankAccountService = bankAccountService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.BankAccounts)]
    [OpenApiOperation("Get all bank accounts.", "")]
    public async Task<HttpResponseDto<List<BankAccountResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var accounts = await _bankAccountService.GetAllAsync(cancellationToken);
        return accounts.ToInformationResponse();
    }

    [HttpGet("{account}")]
    [MustHavePermission(AppAction.View, AppResource.BankAccounts)]
    [OpenApiOperation("Get bank account by code.", "")]
    public async Task<HttpResponseDto<BankAccountResponse?>> GetByIdAsync(string account, CancellationToken cancellationToken)
    {
        var result = await _bankAccountService.GetByIdAsync(account, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.BankAccounts)]
    [OpenApiOperation("Create a new bank account and COA detail account.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(BankAccountCreateRequest request, CancellationToken cancellationToken)
    {
        var accountCode = await _bankAccountService.CreateAsync(request, cancellationToken);
        return accountCode.ToInformationResponse("Bank account created.");
    }

    [HttpPut("{account}")]
    [MustHavePermission(AppAction.Update, AppResource.BankAccounts)]
    [OpenApiOperation("Update bank account details.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string account, BankAccountUpdateRequest request, CancellationToken cancellationToken)
    {
        await _bankAccountService.UpdateAsync(account, request, cancellationToken);
        return "Bank account updated.".ToInformationResponse("Bank account updated.");
    }

    [HttpDelete("{account}")]
    [MustHavePermission(AppAction.Delete, AppResource.BankAccounts)]
    [OpenApiOperation("Delete bank account if no ledger transactions exist.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string account, CancellationToken cancellationToken)
    {
        await _bankAccountService.DeleteAsync(account, cancellationToken);
        return "Bank account deleted.".ToInformationResponse("Bank account deleted.");
    }
}

using Retailer.Application.Legacy.ChartOfAccounts;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class ChartOfAccountsController : VersionNeutralApiController
{
    private readonly IChartOfAccountService _chartOfAccountService;

    public ChartOfAccountsController(IChartOfAccountService chartOfAccountService)
    {
        _chartOfAccountService = chartOfAccountService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.ChartOfAccounts)]
    [OpenApiOperation("Get active chart of accounts.", "")]
    public async Task<HttpResponseDto<List<ChartOfAccountResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var chartOfAccounts = await _chartOfAccountService.GetActiveAsync(cancellationToken);
        return chartOfAccounts.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.ChartOfAccounts)]
    [OpenApiOperation("Create a chart of account.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(ChartOfAccountCreateRequest request, CancellationToken cancellationToken)
    {
        var account = await _chartOfAccountService.CreateAsync(request, cancellationToken);
        return account.ToInformationResponse("Chart of account created.");
    }

    [HttpPut("{account}")]
    [MustHavePermission(AppAction.Update, AppResource.ChartOfAccounts)]
    [OpenApiOperation("Update a chart of account.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string account, ChartOfAccountUpdateRequest request, CancellationToken cancellationToken)
    {
        await _chartOfAccountService.UpdateAsync(account, request, cancellationToken);
        return "Chart of account updated.".ToInformationResponse("Chart of account updated.");
    }

    [HttpDelete("{account}")]
    [MustHavePermission(AppAction.Delete, AppResource.ChartOfAccounts)]
    [OpenApiOperation("Delete a chart of account.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string account, CancellationToken cancellationToken)
    {
        await _chartOfAccountService.DeleteAsync(account, cancellationToken);
        return "Chart of account deleted.".ToInformationResponse("Chart of account deleted.");
    }

    [HttpGet("heads")]
    [OpenApiOperation("Get chart of account heads by level.", "")]
    public async Task<HttpResponseDto<List<ChartOfAccountHeadResponse>>> GetHeadsAsync(
        [FromQuery] int level,
        CancellationToken cancellationToken)
    {
        var heads = await _chartOfAccountService.GetHeadsAsync(level, cancellationToken);
        return heads.ToInformationResponse();
    }

    [HttpGet("lookup")]
    [OpenApiOperation("Get chart of accounts filtered by account prefix.", "")]
    public async Task<HttpResponseDto<List<ChartOfAccountHeadResponse>>> GetLookupAsync(
        [FromQuery] string prefix,
        [FromQuery] int? level,
        CancellationToken cancellationToken)
    {
        var accounts = await _chartOfAccountService.GetByPrefixAsync(prefix, level, cancellationToken);
        return accounts.ToInformationResponse();
    }

    [HttpGet("detail")]
    [OpenApiOperation("Get all detail (leaf) accounts.", "")]
    public async Task<HttpResponseDto<List<ChartOfAccountHeadResponse>>> GetDetailAccountsAsync(CancellationToken cancellationToken)
    {
        var accounts = await _chartOfAccountService.GetDetailAccountsAsync(cancellationToken);
        return accounts.ToInformationResponse();
    }

    [HttpGet("cashbanks")]
    [OpenApiOperation("Get cash and bank accounts.", "")]
    public async Task<HttpResponseDto<List<ChartOfAccountHeadResponse>>> GetCashBankAccountsAsync(CancellationToken cancellationToken)
    {
        var accounts = await _chartOfAccountService.GetCashBankAccountsAsync(cancellationToken);
        return accounts.ToInformationResponse();
    }

    [HttpGet("suppliers")]
    [OpenApiOperation("Get supplier accounts.", "")]
    public async Task<HttpResponseDto<List<ChartOfAccountHeadResponse>>> GetSupplierAccountsAsync(CancellationToken cancellationToken)
    {
        var accounts = await _chartOfAccountService.GetSupplierAccountsAsync(cancellationToken);
        return accounts.ToInformationResponse();
    }

    [HttpGet("customers")]
    [OpenApiOperation("Get customer accounts.", "")]
    public async Task<HttpResponseDto<List<ChartOfAccountHeadResponse>>> GetCustomerAccountsAsync(CancellationToken cancellationToken)
    {
        var accounts = await _chartOfAccountService.GetCustomerAccountsAsync(cancellationToken);
        return accounts.ToInformationResponse();
    }
}

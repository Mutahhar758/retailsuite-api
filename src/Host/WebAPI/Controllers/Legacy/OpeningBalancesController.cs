using Retailer.Application.Legacy.OpeningBalances;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class OpeningBalancesController : VersionNeutralApiController
{
    private readonly IOpeningBalanceService _openingBalanceService;

    public OpeningBalancesController(IOpeningBalanceService openingBalanceService)
    {
        _openingBalanceService = openingBalanceService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.OpeningBalances)]
    [OpenApiOperation("Get opening balances.", "")]
    public async Task<HttpResponseDto<List<OpeningBalanceResponse>>> GetAsync(
        [FromQuery] string? parentAccountId,
        CancellationToken cancellationToken)
    {
        var result = await _openingBalanceService.GetAsync(parentAccountId, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPut]
    [MustHavePermission(AppAction.Update, AppResource.OpeningBalances)]
    [OpenApiOperation("Upsert an opening balance.", "")]
    public async Task<HttpResponseDto<string>> UpsertAsync(
        OpeningBalanceUpsertRequest request,
        CancellationToken cancellationToken)
    {
        await _openingBalanceService.UpsertAsync(request, cancellationToken);
        return "Opening balance saved.".ToInformationResponse("Opening balance saved.");
    }
}

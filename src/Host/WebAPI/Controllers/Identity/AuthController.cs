using Retailer.Application.Identity.Tokens;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Identity;

public sealed class AuthController : VersionNeutralApiController
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService) => _tokenService = tokenService;

    [HttpPost("login")]
    [AllowAnonymous]
    [OpenApiOperation("Request an access token using credentials.", "")]
    public async Task<HttpResponseDto<TokenResponseDto>> GetTokenAsync(TokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _tokenService.GetTokenAsync(request, cancellationToken);
        return response.ToInformationResponse();
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [OpenApiOperation("Request an access token using a refresh token.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Search))]
    public async Task<HttpResponseDto<TokenResponseDto>> RefreshAsync(RefreshTokenRequest request)
    {
        var response = await _tokenService.RefreshTokenAsync(request);
        return response.ToInformationResponse();
    }

    [HttpPost("verify-biometrics")]
    [AllowAnonymous]
    [OpenApiOperation("Biometric Login", "")]

    public async Task<TokenResponseDto> VerifyBiometricsAsync(BiometricTokenRequest request)
    {
        return await _tokenService.VerifyBiometricsSignatureAsync(request);
    }

    private string? GetIpAddress() =>
        Request.Headers.ContainsKey("X-Forwarded-For")
            ? Request.Headers["X-Forwarded-For"]
            : HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "N/A";
}
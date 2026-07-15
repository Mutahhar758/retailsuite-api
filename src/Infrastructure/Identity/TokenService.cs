using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Identity.Tokens;
using Retailer.Infrastructure.Auth;
using Retailer.Infrastructure.Auth.Jwt;
using Retailer.Shared.Authorization;
using Retailer.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Persistence;
using Retailer.Shared.Localization;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Tokens.Identity;
using Org.BouncyCastle.Crypto;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Runtime.InteropServices;
using Retailer.Domain.Common.Enums;
using Finbuckle.MultiTenant.Abstractions;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Identity;

internal class TokenService : ITokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer _localizer;
    private readonly SecuritySettings _securitySettings;
    private readonly JwtSettings _jwtSettings;
    private readonly ISessionService _sessionService;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;

    public TokenService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings,
        IStringLocalizer<TokenService> localizer,
        IOptions<SecuritySettings> securitySettings,
        ISessionService sessionService,
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    {
        _userManager = userManager;
        _localizer = localizer;
        _jwtSettings = jwtSettings.Value;
        _securitySettings = securitySettings.Value;
        _sessionService = sessionService;
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    public async Task<TokenResponseDto> GetTokenAsync(TokenRequest request, CancellationToken cancellationToken)
    {
        var login = request.Login.Trim().Normalize();
        var user = request.LoginType == LoginType.Username
            ? await _userManager.FindByNameAsync(login)
            : await _userManager.FindByEmailAsync(login);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException(_localizer[MessageConstants.AuthFailed]);
        }

        if (user.Status == UserStatus.Blocked)
        {
            throw new UnauthorizedException(_localizer["Your account has been deactivated. Please contact administration."]);
        }

        var response = await GenerateTokensAndUpdateUserAsync(user, request.DeviceId, request.FcmToken, request.AppVersion, request.DeviceName);

        return response;
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var userPrincipal = GetPrincipalFromExpiredToken(request.Token);
        string? userEmail = userPrincipal.GetEmail();
        var user = await _userManager.FindByEmailAsync(userEmail!);
        if (user is null || user.Status == UserStatus.Blocked)
        {
            throw new UnauthorizedException(_localizer[MessageConstants.AuthFailed]);
        }

        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException(_localizer[MessageConstants.InvalidRefreshToken]);
        }
        return await GenerateTokensAndUpdateUserAsync(user, request.DeviceId!, request.FcmToken!, request.AppVersion!, request.DeviceName);
    }

    public async Task<TokenResponseDto> GenerateTokensAndUpdateUserAsync(ApplicationUser user, string deviceId, string fcmToken, string appVersion, string deviceName)
    {
        string token = await GenerateJwtAsync(user);
        user.RefreshToken = GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
        await _sessionService.CreateSessionAsync(user.Id, DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpirationInMinutes), token, deviceId, fcmToken, appVersion, deviceName);
        await _userManager.UpdateAsync(user);

        return new TokenResponseDto
        {
            Token = token,
            RefreshToken = user.RefreshToken,
            RefreshTokenExpiryTime = user.RefreshTokenExpiryTime,
            Status = user.Status
        };
    }

    private async Task<string> GenerateJwtAsync(ApplicationUser user) =>
        GenerateEncryptedToken(GetSigningCredentials(), await GetClaimsAsync(user));

    private async Task<IEnumerable<Claim>> GetClaimsAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(AppClaims.Fullname, $"{user.FirstName} {user.LastName}"),
            new(AppClaims.Username, user.UserName ?? string.Empty),
            new(ClaimTypes.Name, user.FirstName ?? string.Empty),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty),
            new(AppClaims.ImageUrl, user.ImageUrl ?? string.Empty),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty)
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (user.IsOwner)
        {
            claims.Add(new(AppClaims.IsOwner, "true"));
        }

        var tenantIdentifier = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Identifier;
        if (!string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            claims.Add(new Claim("tenant_id", tenantIdentifier));
        }

        return claims;
    }

    private static string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
    {
        var token = new JwtSecurityToken(
           claims: claims,
           expires: DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpirationInMinutes),
           signingCredentials: signingCredentials);
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = false
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedException(_localizer[MessageConstants.InvalidToken]);
            }

            return principal;
        }
        catch
        {
            throw new UnauthorizedException(_localizer[MessageConstants.InvalidToken]);
        }
    }

    private SigningCredentials GetSigningCredentials()
    {
        byte[] secret = Encoding.UTF8.GetBytes(_jwtSettings.Key);
        return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
    }

    public async Task<TokenResponseDto> VerifyBiometricsSignatureAsync(BiometricTokenRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);

        if (string.IsNullOrEmpty(user.BiometricPublicKey))
            throw new UnauthorizedException(_localizer["Authentication Failed"]);

        var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(user.BiometricPublicKey!), out _);

        byte[] signatureBytes = Convert.FromBase64String(request.Signature);
        byte[] userIdBytes = Encoding.UTF8.GetBytes(request.UserId);

        bool isVerified = rsa.VerifyData(
                userIdBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

        var response = isVerified ?
            await GenerateTokensAndUpdateUserAsync(user, request.DeviceId, request.FcmToken, request.AppVersion, request.DeviceName) :
            throw new UnauthorizedException(_localizer["Authentication Failed"]);

        return response;
    }
}
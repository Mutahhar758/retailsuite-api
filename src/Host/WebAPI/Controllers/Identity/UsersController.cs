using Retailer.Application.Common.Exceptions;
using Retailer.Application.Identity.Tokens;
using Retailer.Application.Identity.Users;
using Retailer.Application.Identity.Users.Password;
using Retailer.Application.Tokens.Identity;
using Retailer.Infrastructure.Common.Extensions;
using DocumentFormat.OpenXml.InkML;
using Microsoft.Identity.Client;
using NSwag.CodeGeneration.TypeScript;
using NSwag;
using System.Text;
using NSwag.CodeGeneration;
using MimeKit;
using System.Text.RegularExpressions;
using NJsonSchema.CodeGeneration.TypeScript;
using System.Threading.Tasks;
using Retailer.Infrastructure.Auth.InternalServiceAuthorization;

namespace Retailer.Host.Controllers.Identity;

public class UsersController : VersionNeutralApiController
{
    private readonly IUserService _userService;
    private readonly ISessionService _sessionService;

    public UsersController(IUserService userService, ISessionService sessionService)
    {
        _userService = userService;
        _sessionService = sessionService;
    }

    [HttpPost("list")]
    [OpenApiOperation("Get list of all users.", "")]
    public async Task<HttpResponseDto<PaginationResponse<UserDetailsDto>>> GetListAsync(UserListFilter filter, CancellationToken cancellationToken)
    {
        return (await _userService.SearchAsync(filter, cancellationToken)).ToInformationResponse();
    }

    [HttpGet("{id}")]
    [OpenApiOperation("Get a user's details.", "")]
    public async Task<HttpResponseDto<UserDetailsDto>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return (await _userService.GetAsync(id, cancellationToken)).ToInformationResponse();
    }

    [HttpGet("{id}/roles")]
    [OpenApiOperation("Get a user's roles.", "")]
    public async Task<HttpResponseDto<List<UserRoleDto>>> GetRolesAsync(string id, CancellationToken cancellationToken)
    {
        return (await _userService.GetRolesAsync(id, cancellationToken)).ToInformationResponse();
    }

    [HttpPost("{id}/roles")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Register))]
    [OpenApiOperation("Update a user's assigned roles.", "")]
    public async Task<HttpResponseDto<string>> AssignRolesAsync(string id, UserRolesRequest request, CancellationToken cancellationToken)
    {
        string msg = await _userService.AssignRolesAsync(id, request, cancellationToken);
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpPost]
    [OpenApiOperation("Creates a new user.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(CreateUserRequest request)
    {
        string msg = await _userService.CreateAsync(request, GetOriginFromRequest());
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpPut("{id}")]
    [OpenApiOperation("Updates an existing user.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string id, UpdateUserRequest request)
    {
        request.Id = id;
        await _userService.UpdateAsync(request, id);
        return HttpResponseExtension.InformationResponse("User updated successfully.");
    }

    [HttpPost("self-register")]
    [AllowAnonymous]
    [OpenApiOperation("Anonymous user creates a user.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Register))]
    public async Task<HttpResponseDto<string>> SelfRegisterAsync(CreateUserRequest request)
    {
        var msg = await _userService.CreateAsync(request, GetOriginFromRequest());
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpPost("{id}/toggle-status")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Register))]
    [OpenApiOperation("Toggle a user's active status.", "")]
    public async Task ToggleStatusAsync(string id, ToggleUserStatusRequest request, CancellationToken cancellationToken)
    {
        if (id != request.UserId)
            throw new BadRequestException("Invalid request.");

        await _userService.ToggleStatusAsync(request, cancellationToken);
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [OpenApiOperation("Confirm email address for a user.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Search))]
    public async Task<HttpResponseDto<string>> ConfirmEmailAsync([FromQuery] string userId, [FromQuery] string code, CancellationToken cancellationToken)
    {
        string msg = await _userService.ConfirmEmailAsync(userId, code, cancellationToken);
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [OpenApiOperation("Verify OTP for a user.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Search))]

    public async Task<HttpResponseDto<TokenResponseDto>> VerifyOTPAsync([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        return (await _userService.VerifyOTPAsync(request, cancellationToken)).ToInformationResponse();
    }

    [HttpGet("request-otp")]
    [AllowAnonymous]
    [OpenApiOperation("Request OTP for a user")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Search))]

    public async Task<HttpResponseDto<string>> RequestOTPAsync([FromQuery] string email)
    {
        string msg = await _userService.RequestOTPAsync(email);
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpGet("confirm-phone-number")]
    [AllowAnonymous]
    [OpenApiOperation("Confirm phone number for a user.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Search))]
    public async Task<HttpResponseDto<string>> ConfirmPhoneNumberAsync([FromQuery] string userId, [FromQuery] string code)
    {
        string msg = await _userService.ConfirmPhoneNumberAsync(userId, code);
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [OpenApiOperation("Request a password reset email for a user.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Register))]
    public async Task<HttpResponseDto<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        string msg = await _userService.ForgotPasswordAsync(request, GetOriginFromRequest());
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [OpenApiOperation("Reset a user's password.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Register))]
    public async Task<HttpResponseDto<string>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        string msg = await _userService.ResetPasswordAsync(request);
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpPost("verify-password-otp")]
    [AllowAnonymous]
    [OpenApiOperation("Verify forgot password otp for a user.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Register))]
    public async Task VerifyPasswordOtpAsync(VerifyPasswordOtpRequest request)
    {
        await _userService.VerifyPasswordOtpAsync(request);
    }

    [HttpPost("change-password")]
    [OpenApiOperation("Change current user's password.", "")]
    public async Task<HttpResponseDto<string>> ChangePasswordAsync(ChangePasswordRequest request)
    {
        await _userService.ChangePasswordAsync(request);
        return HttpResponseExtension.InformationResponse("Password changed successfully.");
    }

    [HttpPost("Logout")]
    [OpenApiOperation("Log out a user.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Register))]
    public async Task<IActionResult> LogoutAsync()
    {
        var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (token is null)
        {
            return Unauthorized();
        }

        await _sessionService.LogOutSessionAsync(token);
        return Ok();
    }

    private string GetOriginFromRequest() => $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";

    private string? GetIpAddress() =>
        Request.Headers.ContainsKey("X-Forwarded-For")
            ? Request.Headers["X-Forwarded-For"]
            : HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "N/A";
}
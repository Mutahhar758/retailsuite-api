using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Retailer.Application.Auditing;
using Retailer.Application.Identity.Users;
using Retailer.Application.Identity.Users.Password;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Personal;

public class PersonalController : VersionNeutralApiController
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public PersonalController(IUserService userService, IAuditService auditService)
    {
        _userService = userService;
        _auditService = auditService;
    }

    [HttpGet("profile")]
    [OpenApiOperation("Get profile details of currently logged in user.", "")]
    public async Task<HttpResponseDto<UserDetailsDto>> GetProfileAsync(CancellationToken cancellationToken)
    {
        return (await _userService.GetAsync(cancellationToken: cancellationToken)).ToInformationResponse();
    }

    [HttpPut("profile")]
    [OpenApiOperation("Update profile details of currently logged in user.", "")]
    public async Task UpdateProfileAsync(UpdateUserRequest request)
    {
        await _userService.UpdateAsync(request);
    }

    [HttpPut("change-password")]
    [OpenApiOperation("Change password of currently logged in user.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Register))]
    public async Task ChangePasswordAsync(ChangePasswordRequest model)
    {
        await _userService.ChangePasswordAsync(model);
    }

    [HttpPatch("toggle-biometrics")]
    [OpenApiOperation("toggle biometric authentication for a logged in user", "")]
    public async Task ToggleBiometrics(SetBiometricsRequest request)
    {
        await _userService.ToggleBiometricsAsync(request);
    }

    [HttpGet("permissions")]
    [OpenApiOperation("Get permissions of currently logged in user.", "")]
    public async Task<HttpResponseDto<List<string>>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        return (await _userService.GetPermissionsAsync(cancellationToken: cancellationToken)).ToInformationResponse();
    }

    [HttpGet("logs")]
    [OpenApiOperation("Get audit logs of currently logged in user.", "")]
    public async Task<HttpResponseDto<List<AuditDto>>> GetLogsAsync()
    {
        return (await _auditService.GetUserTrailsAsync()).ToInformationResponse();
    }
}
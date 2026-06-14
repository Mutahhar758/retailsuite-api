using Retailer.Application.Common.Caching;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.FileStorage;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Mailing;
using Retailer.Application.Common.Models;
using Retailer.Application.Identity.Tokens;
using Retailer.Application.Identity.Users;
using Retailer.Domain.Identity;
using Retailer.Infrastructure.Auth;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Shared.Authorization;
using Retailer.Shared.Localization;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Retailer.Application.Tokens.Identity;
using Retailer.Domain.Common.Enums;

namespace Retailer.Infrastructure.Identity;

internal partial class UserService : IUserService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;
    private readonly IStringLocalizer _localizer;
    private readonly IMailService _mailService;
    private readonly SecuritySettings _securitySettings;
    private readonly IEmailTemplateService _templateService;
    private readonly IFileStorageService _fileStorage;
    private readonly ICacheService _cache;
    private readonly ICacheKeyService _cacheKeys;
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionService;
    private readonly ICurrentUser _currentUser;

    public UserService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db,
        IStringLocalizer<UserService> localizer,
        IMailService mailService,
        IEmailTemplateService templateService,
        IFileStorageService fileStorage,
        ICacheService cache,
        ICacheKeyService cacheKeys,
        IOptions<SecuritySettings> securitySettings,
        ITokenService tokenService,
        ISessionService sessionService,
        ICurrentUser currentUser)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _localizer = localizer;
        _mailService = mailService;
        _templateService = templateService;
        _fileStorage = fileStorage;
        _cache = cache;
        _cacheKeys = cacheKeys;
        _securitySettings = securitySettings.Value;
        _tokenService = tokenService;
        _sessionService = sessionService;
        _currentUser = currentUser;
    }

    public async Task<PaginationResponse<UserDetailsDto>> SearchAsync(UserListFilter filter, CancellationToken cancellationToken)
    {
        return await _userManager.Users
       .ProjectToType<UserDetailsDto>()
       .PaginatedListAsync(filter, cancellationToken);
    }

    public async Task<bool> ExistsWithNameAsync(string name)
    {
        return await _userManager.FindByNameAsync(name) is not null;
    }

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
    {
        return await _userManager.FindByEmailAsync(email.Normalize()) is ApplicationUser user && user.Id != exceptId;
    }

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
    {
        return await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber) is ApplicationUser user && user.Id != exceptId;
    }

    public async Task<List<UserDetailsDto>> GetListAsync(CancellationToken cancellationToken) =>
        (await _userManager.Users
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .Adapt<List<UserDetailsDto>>();

    public Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        _userManager.Users.AsNoTracking().CountAsync(cancellationToken);

    public async Task<UserDetailsDto> GetAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        userId ??= _currentUser.GetUserId();

        var user = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(s => new UserDetailsDto
            {
                 Id = new Guid(s.Id),
                 Email = s.Email,
                 FirstName = s.FirstName,
                 LastName = s.LastName,
                 Status = s.Status,
                 EmailConfirmed = s.EmailConfirmed,
                 IsBiometricEnabled = s.BiometricPublicKey != null ? true : false,
                 ImageUrl = s.ImageUrl,
                 UserName = s.UserName
            })
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);

        return user;
    }

    public async Task ToggleStatusAsync(ToggleUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.Where(u => u.Id == request.UserId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);

        bool isAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        if (isAdmin)
        {
            throw new ConflictException(_localizer[MessageConstants.AdminStatusNotUpdatable]);
        }

        user.Status = request.ActivateUser ? UserStatus.Active : UserStatus.Blocked;

        await _userManager.UpdateAsync(user);
    }
}
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Shared.Authorization;
using Retailer.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Retailer.Domain.Common.Enums;

namespace Retailer.Infrastructure.Persistence.Initialization;

internal class ApplicationDbSeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CustomSeederRunner _seederRunner;
    private readonly ILogger<ApplicationDbSeeder> _logger;

    public ApplicationDbSeeder(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, CustomSeederRunner seederRunner, ILogger<ApplicationDbSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _seederRunner = seederRunner;
        _logger = logger;
    }

    public async Task SeedDatabaseAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken, string? adminEmail = null)
    {
        dbContext.EnforceMultiTenantOnTracking();

        await SeedRolesAsync(dbContext);
        await SeedAdminUserAsync(dbContext, adminEmail);
        await _seederRunner.RunSeedersAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(ApplicationDbContext dbContext)
    {
        foreach (string roleName in AppRoles.DefaultRoles)
        {
            if (await _roleManager.Roles.SingleOrDefaultAsync(r => r.Name == roleName)
                is not ApplicationRole role)
            {
                // Create the role
                _logger.LogInformation("Seeding {role} Role.", roleName);
                role = new ApplicationRole(roleName, $"{roleName} Role");
                await _roleManager.CreateAsync(role);
            }

            // Assign permissions
            if (roleName == AppRoles.Basic)
            {
                await AssignPermissionsToRoleAsync(dbContext, AppPermissions.Basic, role);
            }
            else if (roleName == AppRoles.Admin)
            {
                await AssignPermissionsToRoleAsync(dbContext, AppPermissions.Admin, role);
            }
            else if (roleName == AppRoles.Cashier)
            {
                await AssignPermissionsToRoleAsync(dbContext, AppPermissions.Cashier, role);
            }
            else if (roleName == AppRoles.InventoryManager)
            {
                await AssignPermissionsToRoleAsync(dbContext, AppPermissions.InventoryManager, role);
            }
            else if (roleName == AppRoles.Accountant)
            {
                await AssignPermissionsToRoleAsync(dbContext, AppPermissions.Accountant, role);
            }
            else if (roleName == AppRoles.PayrollManager)
            {
                await AssignPermissionsToRoleAsync(dbContext, AppPermissions.PayrollManager, role);
            }
        }
    }

    private async Task AssignPermissionsToRoleAsync(ApplicationDbContext dbContext, IReadOnlyList<AppPermission> permissions, ApplicationRole role)
    {
        var currentClaims = await _roleManager.GetClaimsAsync(role);

        int maxId = await dbContext.RoleClaims.AnyAsync()
            ? await dbContext.RoleClaims.MaxAsync(c => c.Id)
            : 0;

        foreach (var permission in permissions)
        {
            if (!currentClaims.Any(c => c.Type == AppClaims.Permission && c.Value == permission.Name))
            {
                maxId++;
                _logger.LogInformation("Seeding {role} Permission '{permission}'.", role.Name, permission.Name);
                dbContext.RoleClaims.Add(new ApplicationRoleClaim
                {
                    Id = maxId,
                    RoleId = role.Id,
                    ClaimType = AppClaims.Permission,
                    ClaimValue = permission.Name,
                    CreatedBy = "ApplicationDbSeeder"
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }

    public static class InitialDataSeedConstants
    {
        public const string AdminEmail = "admin@mailinator.com";
        public const string AdminUserName = "Admin";
        public const string AdminFirstName = "Admin";
        public const string AdminLastName = "Admin";
        public const string DefaultPassword = "Hallo123$";
    }

    private async Task SeedAdminUserAsync(ApplicationDbContext dbContext, string? adminEmail = null)
    {
        string email = !string.IsNullOrWhiteSpace(adminEmail) ? adminEmail : InitialDataSeedConstants.AdminEmail;
        if (await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email)
            is not ApplicationUser adminUser)
        {
            adminUser = new ApplicationUser
            {
                FirstName = InitialDataSeedConstants.AdminFirstName,
                LastName = InitialDataSeedConstants.AdminLastName,
                Email = email,
                UserName = InitialDataSeedConstants.AdminUserName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = InitialDataSeedConstants.AdminUserName.ToUpperInvariant(),
                Status = UserStatus.Active,
                IsOwner = true
            };

            _logger.LogInformation("Seeding Default Admin User.");
            var password = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = password.HashPassword(adminUser, InitialDataSeedConstants.DefaultPassword);
            var result = await _userManager.CreateAsync(adminUser);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed admin user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        var adminRole = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Admin);
        if (adminRole is null)
        {
            return;
        }

        if (!await dbContext.Set<IdentityUserRole<string>>().AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id))
        {
            _logger.LogInformation("Assigning Admin Role to Admin User.");
            dbContext.Set<IdentityUserRole<string>>().Add(new IdentityUserRole<string>
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });
            await dbContext.SaveChangesAsync();
        }
    }
}
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Retailer.Infrastructure.Identity.Settings;
using Retailer.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Retailer.Infrastructure.Identity;

internal static class Startup
{
    internal static IServiceCollection AddIdentity(this IServiceCollection services) =>
        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequiredLength = 6;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.User.RequireUniqueEmail = true;
                    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultPhoneProvider;
                    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultPhoneProvider;

                    // If you want otp to be generated of 4 digits, uncomment the below lines and comment the above two lines
                    //options.Tokens.PasswordResetTokenProvider = FourDigitTokenProvider.FourDigitPhone;
                    //options.Tokens.EmailConfirmationTokenProvider = FourDigitTokenProvider.FourDigitEmail;
                    //options.Tokens.ChangePhoneNumberTokenProvider = FourDigitTokenProvider.FourDigitPhone;
                })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<FourDigitTokenProvider>(FourDigitTokenProvider.FourDigitEmail)
            .AddTokenProvider<FourDigitTokenProvider>(FourDigitTokenProvider.FourDigitPhone)
            .Services;

    internal static IServiceCollection AddUserSessionMiddleware(this IServiceCollection services) =>
    services.AddScoped<UserSessionMiddleware>();

    internal static IApplicationBuilder UseUserSessionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UserSessionMiddleware>();
    }
}
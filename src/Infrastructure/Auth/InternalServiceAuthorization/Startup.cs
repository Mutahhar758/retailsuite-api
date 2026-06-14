using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Retailer.Shared.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Retailer.Infrastructure.Auth.InternalServiceAuthorization;

internal static class Startup
{
    internal static IServiceCollection AddInternalServicesKey(this IServiceCollection services, IConfiguration config) =>
        services.Configure<InternalServicesKeySettings>(config.GetSection(nameof(InternalServicesKeySettings)));
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Retailer.Infrastructure.Common.External;
using Retailer.Shared.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Retailer.Infrastructure.Link;

internal static class Startup
{
    internal static IServiceCollection AddExternalLinks(this IServiceCollection services, IConfiguration config) =>
        services.Configure<LinkSettings>(config.GetSection(nameof(LinkSettings)));
}

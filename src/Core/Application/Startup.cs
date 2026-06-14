using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Retailer.Application;

public static class Startup
{
    public static void AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
    }
}
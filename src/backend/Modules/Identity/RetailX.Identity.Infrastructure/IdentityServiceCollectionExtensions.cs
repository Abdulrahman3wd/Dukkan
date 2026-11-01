using Microsoft.Extensions.DependencyInjection;
using RetailX.Identity.Domain;

namespace RetailX.Identity.Infrastructure;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordService, PasswordService>();
        return services;
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailX.Identity.Domain;

namespace RetailX.Identity.Infrastructure;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, TokenService>();
        var permissions = new PermissionRegistry();
        permissions.Register(CorePermissions.Definitions);
        services.AddSingleton<IPermissionRegistry>(permissions);
        services.AddMemoryCache();
        services.AddScoped<IPermissionService, PermissionService>();
        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;
using RetailX.Settings.Domain;

namespace RetailX.Settings.Infrastructure;

public static class SettingsServiceCollectionExtensions
{
    public static IServiceCollection AddSettingsModule(this IServiceCollection services)
    {
        var registry = new SettingsRegistry();
        registry.Register(CoreSettings.Definitions);

        services.AddSingleton<ISettingsRegistry>(registry);
        return services;
    }
}
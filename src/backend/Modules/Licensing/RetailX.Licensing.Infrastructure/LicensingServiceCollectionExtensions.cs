using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailX.Licensing.Domain;

namespace RetailX.Licensing.Infrastructure;

public static class LicensingServiceCollectionExtensions
{
    public static IServiceCollection AddLicensingModule(this IServiceCollection services, IConfiguration config)
    {
        var publicKey = config["Licensing:PublicKeyPem"]
            ?? throw new InvalidOperationException("Licensing:PublicKeyPem is not configured.");

        services.AddSingleton<ILicenseVerifier>(new RsaLicenseVerifier(publicKey));
        services.AddScoped<ILicenseService, LicenseService>();

        // vendor side: only registered when a private key exists (our own server, never at customers)
        var privateKey = config["Licensing:PrivateKeyPem"];
        if (!string.IsNullOrWhiteSpace(privateKey))
            services.AddSingleton<ILicenseSigner>(new RsaLicenseSigner(privateKey));

        return services;
    }
}
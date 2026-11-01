using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RetailX.Licensing.Domain;

namespace RetailX.Licensing.Infrastructure;

public sealed class RsaLicenseVerifier : ILicenseVerifier
{
    private readonly string _publicKeyPem;
    public RsaLicenseVerifier(string publicKeyPem) => _publicKeyPem = publicKeyPem;

    public LicensePayload? Verify(SignedLicense license)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_publicKeyPem);

            var data = Encoding.UTF8.GetBytes(license.Payload);
            var sig = Convert.FromBase64String(license.Signature);

            return rsa.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                ? JsonSerializer.Deserialize<LicensePayload>(license.Payload)
                : null;
        }
        catch
        {
            return null;
        }
    }
}

public sealed class RsaLicenseSigner : ILicenseSigner
{
    private readonly string _privateKeyPem;
    public RsaLicenseSigner(string privateKeyPem) => _privateKeyPem = privateKeyPem;

    public SignedLicense Sign(LicensePayload payload)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKeyPem);

        var json = JsonSerializer.Serialize(payload);
        var sig = rsa.SignData(Encoding.UTF8.GetBytes(json), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return new SignedLicense(json, Convert.ToBase64String(sig));
    }
}
namespace RetailX.Licensing.Domain;

public enum LicenseState
{
    Missing = 0,
    Valid = 1,
    InGracePeriod = 2,
    Expired = 3,
    Invalid = 4
}

public sealed record LicenseStatus(
    LicenseState State,
    LicensePayload? License,
    int UsedBranches,
    int UsedPos,
    string? Message);

public interface ILicenseSigner
{
    /// <summary>Vendor side only: signs a payload with the private key.</summary>
    SignedLicense Sign(LicensePayload payload);
}

public interface ILicenseVerifier
{
    /// <summary>Returns the payload if the signature is valid, otherwise null.</summary>
    LicensePayload? Verify(SignedLicense license);
}

public interface ILicenseService
{
    Task<LicenseStatus> GetStatusAsync(CancellationToken ct = default);
    Task<LicenseStatus> InstallAsync(SignedLicense license, CancellationToken ct = default);

    /// <summary>Throws LicenseException if another branch can't be created.</summary>
    Task EnsureCanAddBranchAsync(CancellationToken ct = default);
    Task EnsureCanAddTerminalAsync(CancellationToken ct = default);
    Task EnsureWritableAsync(CancellationToken ct = default);
}

public sealed class LicenseException : Exception
{
    public LicenseException(string message) : base(message) { }
}
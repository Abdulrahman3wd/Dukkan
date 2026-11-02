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
    string? Message);

public interface ILicenseSigner
{
    SignedLicense Sign(LicensePayload payload);
}

public interface ILicenseVerifier
{
    LicensePayload? Verify(SignedLicense license);
}

public interface ILicenseService
{
    /// <summary>Status for the caller's tenant (from the JWT).</summary>
    Task<LicenseStatus> GetStatusAsync(CancellationToken ct = default);

    /// <summary>Status for an explicit tenant (used at POS login, before a token exists).</summary>
    Task<LicenseStatus> GetStatusAsync(Guid tenantId, CancellationToken ct = default);

    Task<LicenseStatus> InstallAsync(SignedLicense license, CancellationToken ct = default);

    Task EnsureCanAddBranchAsync(CancellationToken ct = default);
    Task EnsureWritableAsync(CancellationToken ct = default);
}

public class LicenseException : Exception
{
    public LicenseException(string message) : base(message) { }
}

/// <summary>All seats of the branch are taken.</summary>
public sealed class SeatUnavailableException : LicenseException
{
    public SeatUnavailableException(string message) : base(message) { }
}
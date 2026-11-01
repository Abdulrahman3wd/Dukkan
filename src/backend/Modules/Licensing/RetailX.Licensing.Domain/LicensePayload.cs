namespace RetailX.Licensing.Domain;

/// <summary>The data inside the signed license file.</summary>
public sealed record LicensePayload(
    Guid LicenseId,
    Guid TenantId,
    string CustomerName,
    int Branches,
    int PosPerBranch,
    int ExtraPos,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    int GraceDays)
{
    public int MaxPos => Branches * PosPerBranch + ExtraPos;
}

/// <summary>What gets saved to disk / sent to the customer: payload + signature.</summary>
public sealed record SignedLicense(string Payload, string Signature);
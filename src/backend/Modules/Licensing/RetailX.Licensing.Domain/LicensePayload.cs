namespace RetailX.Licensing.Domain;

/// <summary>Branch that gets a different number of seats than the default.</summary>
public sealed record SeatOverride(Guid BranchId, int Seats);

/// <summary>The data inside the signed license file.</summary>
public sealed record LicensePayload(
    Guid LicenseId,
    Guid TenantId,
    string CustomerName,
    int Branches,
    int DefaultSeatsPerBranch,
    IReadOnlyList<SeatOverride> SeatOverrides,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    int GraceDays)
{
    public int SeatsFor(Guid branchId)
        => SeatOverrides?.FirstOrDefault(o => o.BranchId == branchId)?.Seats ?? DefaultSeatsPerBranch;
}

/// <summary>What gets saved to disk / sent to the customer: payload + signature.</summary>
public sealed record SignedLicense(string Payload, string Signature);
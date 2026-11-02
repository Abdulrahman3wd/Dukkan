using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.Licensing.Domain;
using RetailX.Organization.Domain;

namespace RetailX.Licensing.Infrastructure;

public sealed class LicenseService : ILicenseService
{
    private readonly DbContext _db;
    private readonly ILicenseVerifier _verifier;
    private readonly ICurrentContext _ctx;

    public LicenseService(DbContext db, ILicenseVerifier verifier, ICurrentContext ctx)
    {
        _db = db; _verifier = verifier; _ctx = ctx;
    }

    private Guid RequireTenant()
        => _ctx.TenantId ?? throw new InvalidOperationException("Tenant context is required.");

    public Task<LicenseStatus> GetStatusAsync(CancellationToken ct = default)
        => GetStatusAsync(RequireTenant(), ct);

    public async Task<LicenseStatus> GetStatusAsync(Guid tenantId, CancellationToken ct = default)
    {
        // explicit tenant filter so this also works before a JWT exists (POS login)
        var branches = await _db.Set<Branch>().IgnoreQueryFilters()
            .CountAsync(b => b.TenantId == tenantId && b.IsActive && !b.IsDeleted, ct);

        var current = await _db.Set<InstalledLicense>().IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.IsCurrent && !l.IsDeleted, ct);

        if (current is null)
            return new LicenseStatus(LicenseState.Missing, null, branches, "No license installed.");

        var payload = _verifier.Verify(new SignedLicense(current.Payload, current.Signature));
        if (payload is null || payload.TenantId != tenantId)
            return new LicenseStatus(LicenseState.Invalid, null, branches, "License signature is invalid.");

        var now = DateTime.UtcNow;
        if (now <= payload.ExpiresAtUtc)
            return new LicenseStatus(LicenseState.Valid, payload, branches, null);

        if (now <= payload.ExpiresAtUtc.AddDays(payload.GraceDays))
            return new LicenseStatus(LicenseState.InGracePeriod, payload, branches,
                "License expired. Renew before the grace period ends.");

        return new LicenseStatus(LicenseState.Expired, payload, branches,
            "License expired. The system is read-only.");
    }

    public async Task<LicenseStatus> InstallAsync(SignedLicense license, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        var payload = _verifier.Verify(license)
            ?? throw new LicenseException("License signature is invalid.");

        if (payload.TenantId != tenantId)
            throw new LicenseException("This license belongs to a different customer.");

        var old = await _db.Set<InstalledLicense>().Where(l => l.IsCurrent).ToListAsync(ct);
        foreach (var o in old) o.IsCurrent = false;

        _db.Set<InstalledLicense>().Add(new InstalledLicense
        {
            LicenseId = payload.LicenseId,
            Payload = license.Payload,
            Signature = license.Signature
        });
        await _db.SaveChangesAsync(ct);

        return await GetStatusAsync(tenantId, ct);
    }

    public async Task EnsureCanAddBranchAsync(CancellationToken ct = default)
    {
        var s = await GetStatusAsync(ct);
        EnsureUsable(s);
        if (s.UsedBranches >= s.License!.Branches)
            throw new LicenseException(
                $"Branch limit reached ({s.License.Branches}). Purchase an additional branch license.");
    }

    public async Task EnsureWritableAsync(CancellationToken ct = default)
        => EnsureUsable(await GetStatusAsync(ct));

    private static void EnsureUsable(LicenseStatus s)
    {
        if (s.State is LicenseState.Valid or LicenseState.InGracePeriod) return;
        throw new LicenseException(s.Message ?? "License is not valid.");
    }
}
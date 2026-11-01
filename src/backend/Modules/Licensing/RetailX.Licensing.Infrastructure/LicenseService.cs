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

    public async Task<LicenseStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var tenantId = _ctx.TenantId ?? throw new InvalidOperationException("Tenant context is required.");

        var branches = await _db.Set<Branch>().CountAsync(b => b.IsActive, ct);
        var pos = await _db.Set<Terminal>().CountAsync(t => t.IsActive, ct);

        var current = await _db.Set<InstalledLicense>().AsNoTracking()
            .FirstOrDefaultAsync(l => l.IsCurrent, ct);

        if (current is null)
            return new LicenseStatus(LicenseState.Missing, null, branches, pos, "No license installed.");

        var payload = _verifier.Verify(new SignedLicense(current.Payload, current.Signature));
        if (payload is null || payload.TenantId != tenantId)
            return new LicenseStatus(LicenseState.Invalid, null, branches, pos, "License signature is invalid.");

        var now = DateTime.UtcNow;
        if (now <= payload.ExpiresAtUtc)
            return new LicenseStatus(LicenseState.Valid, payload, branches, pos, null);

        if (now <= payload.ExpiresAtUtc.AddDays(payload.GraceDays))
            return new LicenseStatus(LicenseState.InGracePeriod, payload, branches, pos,
                "License expired. Renew before the grace period ends.");

        return new LicenseStatus(LicenseState.Expired, payload, branches, pos,
            "License expired. The system is read-only.");
    }

    public async Task<LicenseStatus> InstallAsync(SignedLicense license, CancellationToken ct = default)
    {
        var tenantId = _ctx.TenantId ?? throw new InvalidOperationException("Tenant context is required.");

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

        return await GetStatusAsync(ct);
    }

    public async Task EnsureCanAddBranchAsync(CancellationToken ct = default)
    {
        var s = await GetStatusAsync(ct);
        EnsureUsable(s);
        if (s.UsedBranches >= s.License!.Branches)
            throw new LicenseException($"Branch limit reached ({s.License.Branches}). Purchase an additional branch license.");
    }

    public async Task EnsureCanAddTerminalAsync(CancellationToken ct = default)
    {
        var s = await GetStatusAsync(ct);
        EnsureUsable(s);
        if (s.UsedPos >= s.License!.MaxPos)
            throw new LicenseException($"POS limit reached ({s.License.MaxPos}). Purchase an additional POS license.");
    }

    public async Task EnsureWritableAsync(CancellationToken ct = default)
    {
        var s = await GetStatusAsync(ct);
        if (s.State is LicenseState.Valid or LicenseState.InGracePeriod) return;
        throw new LicenseException(s.Message ?? "License is not valid.");
    }

    private static void EnsureUsable(LicenseStatus s)
    {
        if (s.State is LicenseState.Valid or LicenseState.InGracePeriod) return;
        throw new LicenseException(s.Message ?? "License is not valid.");
    }
}
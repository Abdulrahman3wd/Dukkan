using System.Data;
using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.Licensing.Domain;
using RetailX.Organization.Domain;

namespace RetailX.Licensing.Infrastructure;

public sealed class SeatService : ISeatService
{
    public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);
    private const int HeartbeatSeconds = 60;

    private readonly DbContext _db;
    private readonly ILicenseService _license;
    private readonly ICurrentContext _ctx;

    public SeatService(DbContext db, ILicenseService license, ICurrentContext ctx)
    {
        _db = db; _license = license; _ctx = ctx;
    }

    private IQueryable<SeatSession> Sessions(Guid tenantId)
        => _db.Set<SeatSession>().IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && !s.IsDeleted);

    public async Task<SeatLease> AcquireAsync(Guid tenantId, Guid companyId, Guid branchId,
        Guid terminalId, Guid userId, CancellationToken ct = default)
    {
        var status = await _license.GetStatusAsync(tenantId, ct);
        if (status.State is not (LicenseState.Valid or LicenseState.InGracePeriod))
            throw new LicenseException(status.Message ?? "License is not valid.");

        // serializable: two simultaneous logins can't both grab the last seat
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var now = DateTime.UtcNow;

        // a terminal holds one session at a time (re-login after a crash replaces the old one)
        var previous = await Sessions(tenantId)
            .Where(s => s.TerminalId == terminalId && s.EndedAtUtc == null).ToListAsync(ct);
        foreach (var p in previous) End(p, now, "Replaced by a new login");
        if (previous.Count > 0) await _db.SaveChangesAsync(ct);

        var total = status.License!.SeatsFor(branchId);
        var reserved = await ReservedForManagerAsync(tenantId, branchId, ct);
        var available = total - reserved;

        var cutoff = now - Timeout;
        var used = await Sessions(tenantId).CountAsync(
            s => s.BranchId == branchId && s.EndedAtUtc == null && s.LastHeartbeatAtUtc > cutoff, ct);

        if (used >= available)
            throw new SeatUnavailableException(
                $"All seats in this branch are in use ({used}/{Math.Max(available, 0)}). " +
                "Ask a colleague to log out, or purchase additional seats.");

        var session = new SeatSession
        {
            TenantId = tenantId,
            CompanyId = companyId,
            BranchId = branchId,
            TerminalId = terminalId,
            UserId = userId,
            StartedAtUtc = now,
            LastHeartbeatAtUtc = now
        };
        _db.Set<SeatSession>().Add(session);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new SeatLease(session.Id, HeartbeatSeconds, (int)Timeout.TotalSeconds);
    }

    public async Task<bool> HeartbeatAsync(Guid sessionId, CancellationToken ct = default)
    {
        var s = await _db.Set<SeatSession>().FirstOrDefaultAsync(x => x.Id == sessionId, ct);
        if (s is null || s.EndedAtUtc is not null) return false;

        var now = DateTime.UtcNow;
        if (s.LastHeartbeatAtUtc <= now - Timeout)
        {
            End(s, now, "Timed out");
            await _db.SaveChangesAsync(ct);
            return false;
        }

        s.LastHeartbeatAtUtc = now;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task ReleaseAsync(Guid sessionId, CancellationToken ct = default)
    {
        var s = await _db.Set<SeatSession>().FirstOrDefaultAsync(x => x.Id == sessionId, ct);
        if (s is null || s.EndedAtUtc is not null) return;
        End(s, DateTime.UtcNow, "Logout");
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ForceReleaseAsync(Guid sessionId, CancellationToken ct = default)
    {
        var s = await _db.Set<SeatSession>().FirstOrDefaultAsync(x => x.Id == sessionId, ct);
        if (s is null || s.EndedAtUtc is not null) return false;
        End(s, DateTime.UtcNow, "Forced by manager");
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SeatUsage> GetUsageAsync(Guid branchId, CancellationToken ct = default)
    {
        var tenantId = _ctx.TenantId ?? throw new InvalidOperationException("Tenant context is required.");

        var status = await _license.GetStatusAsync(tenantId, ct);
        var total = status.License?.SeatsFor(branchId) ?? 0;
        var reserved = await ReservedForManagerAsync(tenantId, branchId, ct);

        var cutoff = DateTime.UtcNow - Timeout;
        var active = await Sessions(tenantId)
            .Where(s => s.BranchId == branchId && s.EndedAtUtc == null && s.LastHeartbeatAtUtc > cutoff)
            .Select(s => new SeatSessionInfo(s.Id, s.TerminalId, s.UserId, s.StartedAtUtc, s.LastHeartbeatAtUtc))
            .ToListAsync(ct);

        return new SeatUsage(branchId, total, reserved, Math.Max(total - reserved, 0), active.Count, active);
    }

    /// <summary>
    /// A branch with more than one terminal and a Manager among them keeps one seat for the Manager.
    /// A single-POS branch has no reservation: that POS is both manager and seller.
    /// </summary>
    private async Task<int> ReservedForManagerAsync(Guid tenantId, Guid branchId, CancellationToken ct)
    {
        var roles = await _db.Set<Terminal>().IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId && t.BranchId == branchId && t.IsActive && !t.IsDeleted)
            .Select(t => t.Role).ToListAsync(ct);

        return roles.Count > 1 && roles.Contains(TerminalRole.Manager) ? 1 : 0;
    }

    private static void End(SeatSession s, DateTime now, string reason)
    {
        s.EndedAtUtc = now;
        s.EndReason = reason;
    }
}
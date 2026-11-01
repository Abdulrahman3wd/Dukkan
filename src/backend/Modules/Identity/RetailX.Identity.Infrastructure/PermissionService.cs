using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.Identity.Domain;

namespace RetailX.Identity.Infrastructure;

public sealed class PermissionService : IPermissionService
{
    private readonly DbContext _db;
    private readonly ICurrentContext _ctx;
    private readonly IPermissionRegistry _registry;
    private readonly IMemoryCache _cache;

    public PermissionService(DbContext db, ICurrentContext ctx,
        IPermissionRegistry registry, IMemoryCache cache)
    {
        _db = db; _ctx = ctx; _registry = registry; _cache = cache;
    }

    public async Task<IReadOnlySet<string>> GetCurrentAsync(CancellationToken ct = default)
    {
        if (_ctx.UserId is not { } userId) return new HashSet<string>();

        var key = $"perm:{userId}:{_ctx.BranchId}";
        if (_cache.TryGetValue(key, out IReadOnlySet<string>? cached) && cached is not null)
            return cached;

        var user = await _db.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !user.IsActive) return new HashSet<string>();

        HashSet<string> result;
        if (user.IsTenantAdmin)
        {
            result = _registry.All.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else if (_ctx.BranchId is not { } branchId)
        {
            result = new HashSet<string>();
        }
        else
        {
            var roleId = await _db.Set<UserBranchAssignment>().AsNoTracking()
                .Where(a => a.UserId == userId && a.BranchId == branchId && a.IsActive)
                .Select(a => a.RoleId)
                .FirstOrDefaultAsync(ct);

            result = roleId is null
                ? new HashSet<string>()
                : (await _db.Set<RolePermission>().AsNoTracking()
                    .Where(p => p.RoleId == roleId)
                    .Select(p => p.Permission)
                    .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        _cache.Set(key, (IReadOnlySet<string>)result, TimeSpan.FromMinutes(2));
        return result;
    }

    public async Task<bool> HasAsync(string permission, CancellationToken ct = default)
        => (await GetCurrentAsync(ct)).Contains(permission);
}
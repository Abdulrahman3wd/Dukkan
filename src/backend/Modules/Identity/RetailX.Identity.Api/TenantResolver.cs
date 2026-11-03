using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RetailX.BuildingBlocks.Persistence;
using RetailX.Organization.Domain;

namespace RetailX.Identity.Api;

public sealed class TenantResolver
{
    private readonly RetailXDbContext _db;
    private readonly IConfiguration _cfg;

    public TenantResolver(RetailXDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    public bool IsSingleTenant
        => !string.Equals(_cfg["Deployment:Mode"], "MultiTenant", StringComparison.OrdinalIgnoreCase);

    /// <summary>Explicit id wins; otherwise a single-tenant server uses its only tenant.</summary>
    public async Task<Guid?> ResolveAsync(Guid? requested, CancellationToken ct)
    {
        if (requested is not null) return requested;
        if (!IsSingleTenant) return null;

        var ids = await _db.Set<Tenant>().IgnoreQueryFilters()
            .Where(t => !t.IsDeleted && t.IsActive)
            .Select(t => t.Id).Take(2).ToListAsync(ct);

        return ids.Count == 1 ? ids[0] : null;
    }
}
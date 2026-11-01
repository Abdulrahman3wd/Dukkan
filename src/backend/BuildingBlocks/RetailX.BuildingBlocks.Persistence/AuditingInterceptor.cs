using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RetailX.BuildingBlocks.Domain;
using RetailX.BuildingBlocks.MultiTenancy;

namespace RetailX.BuildingBlocks.Persistence;

/// <summary>
/// Runs on every SaveChanges: fills tenant ids, UpdatedAtUtc, Version, and converts deletes to soft deletes.
/// </summary>
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentContext _ctx;

    public AuditingInterceptor(ICurrentContext ctx) => _ctx = ctx;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? db)
    {
        if (db is null) return;

        foreach (var entry in db.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    entry.Entity.Version = 1;
                    FillScope(entry);
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                    entry.Entity.Version++;
                    break;

                case EntityState.Deleted:
                    // soft delete: needed so deletions can sync to POS terminals
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                    entry.Entity.Version++;
                    break;
            }
        }
    }

    private void FillScope(EntityEntry<Entity> entry)
    {
        if (entry.Entity is ITenantScoped t && t.TenantId == Guid.Empty && _ctx.TenantId.HasValue)
            t.TenantId = _ctx.TenantId.Value;

        if (entry.Entity is ICompanyScoped c && c.CompanyId == Guid.Empty && _ctx.CompanyId.HasValue)
            c.CompanyId = _ctx.CompanyId.Value;

        if (entry.Entity is IBranchScoped b && b.BranchId == Guid.Empty && _ctx.BranchId.HasValue)
            b.BranchId = _ctx.BranchId.Value;
    }
}
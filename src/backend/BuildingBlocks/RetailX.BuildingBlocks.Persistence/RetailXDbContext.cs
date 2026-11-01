using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.Domain;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.Organization.Infrastructure;

namespace RetailX.BuildingBlocks.Persistence;

public class RetailXDbContext : DbContext
{
    private readonly ICurrentContext _ctx;

    public RetailXDbContext(DbContextOptions<RetailXDbContext> options, ICurrentContext ctx)
        : base(options) => _ctx = ctx;

    // read by the query filters; EF re-evaluates these per DbContext instance
    public Guid? CurrentTenantId => _ctx.TenantId;
    public Guid? CurrentCompanyId => _ctx.CompanyId;
    public Guid? CurrentBranchId => _ctx.BranchId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOrganization();
        // next modules will be registered here

        ApplyGlobalFilters(modelBuilder);
    }

    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (!typeof(Entity).IsAssignableFrom(clr)) continue;

            var p = Expression.Parameter(clr, "e");
            Expression filter = Expression.Equal(
                Expression.Property(p, nameof(Entity.IsDeleted)),
                Expression.Constant(false));

            // Tenant filter (skipped for Tenant entity itself, which has no TenantId)
            if (typeof(ITenantScoped).IsAssignableFrom(clr))
            {
                var tenantProp = Expression.Property(p, nameof(ITenantScoped.TenantId));
                var ctxTenant = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
                var hasTenant = Expression.Property(ctxTenant, "HasValue");
                var tenantMatch = Expression.Equal(
                    tenantProp, Expression.Property(ctxTenant, "Value"));
                // if no tenant in context => no rows (safe by default)
                filter = Expression.AndAlso(filter, Expression.AndAlso(hasTenant, tenantMatch));
            }

            modelBuilder.Entity(clr).HasQueryFilter(Expression.Lambda(filter, p));
        }
    }
}
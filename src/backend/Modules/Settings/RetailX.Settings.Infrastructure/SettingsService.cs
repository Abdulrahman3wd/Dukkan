using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.Settings.Domain;

namespace RetailX.Settings.Infrastructure;

/// <summary>Works against any DbContext that exposes SettingValue (the shared RetailXDbContext).</summary>
public sealed class SettingsService : ISettingsService
{
    private readonly DbContext _db;
    private readonly ISettingsRegistry _registry;
    private readonly ICurrentContext _ctx;

    public SettingsService(DbContext db, ISettingsRegistry registry, ICurrentContext ctx)
    {
        _db = db;
        _registry = registry;
        _ctx = ctx;
    }

    public async Task<string> GetAsync(string key, Guid? companyId, Guid? branchId, CancellationToken ct = default)
    {
        var def = _registry.Find(key)
            ?? throw new KeyNotFoundException($"Setting '{key}' is not registered.");

        var tenantId = _ctx.TenantId ?? throw new InvalidOperationException("Tenant context is required.");

        var overrides = await _db.Set<SettingValue>().AsNoTracking()
            .Where(v => v.Key == key && v.TenantId == tenantId)
            .ToListAsync(ct);

        string? Find(SettingScope scope, Guid? id)
            => id is null ? null : overrides.FirstOrDefault(v => v.Scope == scope && v.ScopeId == id)?.Value;

        return Find(SettingScope.Branch, branchId)
            ?? Find(SettingScope.Company, companyId)
            ?? Find(SettingScope.Tenant, tenantId)
            ?? def.DefaultValue;
    }

    public async Task<T> GetAsync<T>(string key, Guid? companyId, Guid? branchId, CancellationToken ct = default)
    {
        var raw = await GetAsync(key, companyId, branchId, ct);
        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
    }

    public async Task SetAsync(string key, SettingScope scope, Guid scopeId, string value, CancellationToken ct = default)
    {
        var def = _registry.Find(key)
            ?? throw new KeyNotFoundException($"Setting '{key}' is not registered.");

        if (scope > def.MostSpecificScope)
            throw new InvalidOperationException(
                $"Setting '{key}' cannot be overridden at {scope} level (max: {def.MostSpecificScope}).");

        Validate(def, value);

        var tenantId = _ctx.TenantId ?? throw new InvalidOperationException("Tenant context is required.");

        var existing = await _db.Set<SettingValue>()
            .FirstOrDefaultAsync(v => v.Key == key && v.Scope == scope && v.ScopeId == scopeId, ct);

        if (existing is null)
        {
            _db.Set<SettingValue>().Add(new SettingValue
            {
                TenantId = tenantId,
                Key = key,
                Scope = scope,
                ScopeId = scopeId,
                Value = value
            });
        }
        else
        {
            existing.Value = value;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetAsync(string key, SettingScope scope, Guid scopeId, CancellationToken ct = default)
    {
        var existing = await _db.Set<SettingValue>()
            .FirstOrDefaultAsync(v => v.Key == key && v.Scope == scope && v.ScopeId == scopeId, ct);

        if (existing is not null)
        {
            _db.Set<SettingValue>().Remove(existing);   // interceptor turns this into a soft delete
            await _db.SaveChangesAsync(ct);
        }
    }

    private static void Validate(SettingDefinition def, string value)
    {
        var ok = def.Type switch
        {
            SettingValueType.Int => int.TryParse(value, CultureInfo.InvariantCulture, out _),
            SettingValueType.Bool => bool.TryParse(value, out _),
            SettingValueType.Decimal => decimal.TryParse(value, CultureInfo.InvariantCulture, out _),
            _ => true
        };
        if (!ok) throw new ArgumentException($"Value '{value}' is not a valid {def.Type}.");
    }
}
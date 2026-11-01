namespace RetailX.Settings.Domain;

public interface ISettingsRegistry
{
    void Register(params SettingDefinition[] definitions);
    SettingDefinition? Find(string key);
    IReadOnlyCollection<SettingDefinition> All { get; }
}

public interface ISettingsService
{
    /// <summary>Resolves the effective value: Branch → Company → Tenant → Default.</summary>
    Task<string> GetAsync(string key, Guid? companyId, Guid? branchId, CancellationToken ct = default);

    Task<T> GetAsync<T>(string key, Guid? companyId, Guid? branchId, CancellationToken ct = default);

    Task SetAsync(string key, SettingScope scope, Guid scopeId, string value, CancellationToken ct = default);

    /// <summary>Removes the override so the value is inherited again.</summary>
    Task ResetAsync(string key, SettingScope scope, Guid scopeId, CancellationToken ct = default);
}
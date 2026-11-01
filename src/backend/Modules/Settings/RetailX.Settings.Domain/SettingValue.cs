using RetailX.BuildingBlocks.Domain;

namespace RetailX.Settings.Domain;

/// <summary>An overridden value of a setting at a specific scope.</summary>
public class SettingValue : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Key { get; set; } = default!;
    public SettingScope Scope { get; set; }

    /// <summary>Id of the company/branch (or the tenant itself) this value applies to.</summary>
    public Guid ScopeId { get; set; }

    public string Value { get; set; } = default!;
}
namespace RetailX.Identity.Domain;

public sealed record PermissionDefinition(
    string Key, string Group, string DisplayNameEn, string DisplayNameAr);

public interface IPermissionRegistry
{
    void Register(params PermissionDefinition[] definitions);
    bool Exists(string key);
    IReadOnlyCollection<PermissionDefinition> All { get; }
}

public interface IPermissionService
{
    /// <summary>Permissions of the current user in the current branch.</summary>
    Task<IReadOnlySet<string>> GetCurrentAsync(CancellationToken ct = default);
    Task<bool> HasAsync(string permission, CancellationToken ct = default);
}
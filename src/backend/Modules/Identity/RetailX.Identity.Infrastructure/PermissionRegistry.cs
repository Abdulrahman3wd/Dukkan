using RetailX.Identity.Domain;

namespace RetailX.Identity.Infrastructure;

public sealed class PermissionRegistry : IPermissionRegistry
{
    private readonly Dictionary<string, PermissionDefinition> _defs = new(StringComparer.OrdinalIgnoreCase);

    public void Register(params PermissionDefinition[] definitions)
    {
        foreach (var d in definitions)
            if (!_defs.TryAdd(d.Key, d))
                throw new InvalidOperationException($"Permission '{d.Key}' is already registered.");
    }

    public bool Exists(string key) => _defs.ContainsKey(key);
    public IReadOnlyCollection<PermissionDefinition> All => _defs.Values;
}
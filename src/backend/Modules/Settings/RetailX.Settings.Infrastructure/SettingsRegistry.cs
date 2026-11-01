using RetailX.Settings.Domain;

namespace RetailX.Settings.Infrastructure;

public sealed class SettingsRegistry : ISettingsRegistry
{
    private readonly Dictionary<string, SettingDefinition> _defs = new(StringComparer.OrdinalIgnoreCase);

    public void Register(params SettingDefinition[] definitions)
    {
        foreach (var d in definitions)
        {
            if (!_defs.TryAdd(d.Key, d))
                throw new InvalidOperationException($"Setting '{d.Key}' is already registered.");
        }
    }

    public SettingDefinition? Find(string key) => _defs.GetValueOrDefault(key);

    public IReadOnlyCollection<SettingDefinition> All => _defs.Values;
}
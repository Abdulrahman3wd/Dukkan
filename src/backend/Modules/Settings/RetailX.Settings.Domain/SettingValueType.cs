namespace RetailX.Settings.Domain;

public enum SettingValueType
{
    String = 1,
    Int = 2,
    Bool = 3,
    Decimal = 4
}

/// <summary>
/// Declares a setting in code. Modules register their own definitions.
/// </summary>
/// <param name="Key">Unique key, e.g. "General.Language".</param>
/// <param name="MostSpecificScope">Lowest level this setting may be overridden at.</param>
public sealed record SettingDefinition(
    string Key,
    SettingValueType Type,
    string DefaultValue,
    SettingScope MostSpecificScope,
    string Group);
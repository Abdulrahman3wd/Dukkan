using RetailX.Settings.Domain;

namespace RetailX.Settings.Infrastructure;

public static class CoreSettings
{
    public const string Language = "General.Language";
    public const string SyncIntervalSeconds = "Sync.IntervalSeconds";
    public const string AllowNegativeStock = "Inventory.AllowNegativeStock";

    public static SettingDefinition[] Definitions =>
    [
        new(Language, SettingValueType.String, "ar", SettingScope.Branch,
            "General", "Language", "اللغة"),

        new(SyncIntervalSeconds, SettingValueType.Int, "300", SettingScope.Branch,
            "Sync", "Sync interval (seconds)", "فترة المزامنة (بالثواني)"),

        new(AllowNegativeStock, SettingValueType.Bool, "false", SettingScope.Branch,
            "Inventory", "Allow negative stock", "السماح بالمخزون السالب"),
    ];
}
namespace RetailX.Settings.Domain;

/// <summary>Levels a setting can be overridden at. Higher number = more specific.</summary>
public enum SettingScope
{
    Tenant = 1,
    Company = 2,
    Branch = 3
}
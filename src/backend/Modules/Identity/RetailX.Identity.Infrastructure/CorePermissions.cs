using RetailX.Identity.Domain;

namespace RetailX.Identity.Infrastructure;

public static class CorePermissions
{
    public const string UsersView = "Identity.User.View";
    public const string UsersManage = "Identity.User.Manage";
    public const string RolesManage = "Identity.Role.Manage";
    public const string SettingsView = "Settings.View";
    public const string SettingsManage = "Settings.Manage";
    public const string OrgManage = "Organization.Manage";
    public const string SeatsView = "Licensing.Seat.View";
    public const string SeatsManage = "Licensing.Seat.Manage";
    public static PermissionDefinition[] Definitions =>
    [
        new(UsersView, "Identity"),
        new(UsersManage, "Identity"),
        new(RolesManage, "Identity"),
        new(SettingsView, "Settings"),
        new(SettingsManage, "Settings"),
        new(OrgManage, "Organization"),
        new(SeatsView, "Licensing"),
        new(SeatsManage, "Licensing"),
];
}
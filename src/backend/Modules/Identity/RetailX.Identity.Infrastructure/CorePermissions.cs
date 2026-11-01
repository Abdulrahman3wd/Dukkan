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

    public static PermissionDefinition[] Definitions =>
    [
        new(UsersView, "Identity", "View users", "عرض المستخدمين"),
        new(UsersManage, "Identity", "Manage users", "إدارة المستخدمين"),
        new(RolesManage, "Identity", "Manage roles", "إدارة الأدوار"),
        new(SettingsView, "Settings", "View settings", "عرض الإعدادات"),
        new(SettingsManage, "Settings", "Manage settings", "تعديل الإعدادات"),
        new(OrgManage, "Organization", "Manage companies/branches", "إدارة الشركات والفروع"),
    ];
}
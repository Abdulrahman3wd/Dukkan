using Microsoft.EntityFrameworkCore;
using RetailX.Settings.Domain;

namespace RetailX.Settings.Infrastructure;

public static class SettingsModelConfiguration
{
    public static void ApplySettings(this ModelBuilder b)
    {
        b.Entity<SettingValue>(e =>
        {
            e.ToTable("SettingValues", "settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(200).IsRequired();
            e.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.Key, x.Scope, x.ScopeId }).IsUnique();
        });
    }
}
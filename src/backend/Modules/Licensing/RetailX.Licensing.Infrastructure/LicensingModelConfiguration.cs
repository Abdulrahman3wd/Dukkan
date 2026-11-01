using Microsoft.EntityFrameworkCore;
using RetailX.Licensing.Domain;

namespace RetailX.Licensing.Infrastructure;

public static class LicensingModelConfiguration
{
    public static void ApplyLicensing(this ModelBuilder b)
    {
        b.Entity<InstalledLicense>(e =>
        {
            e.ToTable("InstalledLicenses", "licensing");
            e.HasKey(x => x.Id);
            e.Property(x => x.Payload).IsRequired();
            e.Property(x => x.Signature).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.IsCurrent });
        });
    }
}
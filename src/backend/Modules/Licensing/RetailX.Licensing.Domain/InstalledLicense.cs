using RetailX.BuildingBlocks.Domain;

namespace RetailX.Licensing.Domain;

public class InstalledLicense : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid LicenseId { get; set; }
    public string Payload { get; set; } = default!;
    public string Signature { get; set; } = default!;
    public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsCurrent { get; set; } = true;
}
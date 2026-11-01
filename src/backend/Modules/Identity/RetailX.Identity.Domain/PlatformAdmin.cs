using RetailX.BuildingBlocks.Domain;

namespace RetailX.Identity.Domain;

/// <summary>Software vendor admin (us). Not tied to any tenant.</summary>
public class PlatformAdmin : Entity
{
    public string UserName { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
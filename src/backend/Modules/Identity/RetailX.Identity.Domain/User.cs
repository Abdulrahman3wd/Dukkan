using RetailX.BuildingBlocks.Domain;

namespace RetailX.Identity.Domain;

public class User : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }

    public string UserName { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = default!;

    /// <summary>Optional PIN for fast login on the POS screen (stored hashed).</summary>
    public string? PinHash { get; set; }

    /// <summary>True = sees every company and branch in the tenant (Tenant Admin).</summary>
    public bool IsTenantAdmin { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<UserBranchAssignment> Assignments { get; set; } = new List<UserBranchAssignment>();
}
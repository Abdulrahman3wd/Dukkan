using RetailX.BuildingBlocks.Domain;

namespace RetailX.Identity.Domain;

/// <summary>Employee works in this branch with this role. Transfer = deactivate + add a new one.</summary>
public class UserBranchAssignment : Entity, IBranchScoped
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Linked to the Roles module in the next step.</summary>
    public Guid? RoleId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }

    public User? User { get; set; }
}
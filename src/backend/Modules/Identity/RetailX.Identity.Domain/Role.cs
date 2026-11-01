using RetailX.BuildingBlocks.Domain;

namespace RetailX.Identity.Domain;

public class Role : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>System roles (seeded) cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
}

public class RolePermission : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public string Permission { get; set; } = default!;

    public Role? Role { get; set; }
}
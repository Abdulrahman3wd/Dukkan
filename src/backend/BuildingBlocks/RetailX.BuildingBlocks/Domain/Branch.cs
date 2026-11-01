using RetailX.BuildingBlocks.Domain;

namespace RetailX.Organization.Domain;

public class Branch : Entity, IBranchScoped
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId
    {
        get => Id;
        set { /* BranchId of a branch is its own Id */ }
    }

    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;

    public Company? Company { get; set; }
    public ICollection<Terminal> Terminals { get; set; } = new List<Terminal>();
}
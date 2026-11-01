using RetailX.BuildingBlocks.Domain;

namespace RetailX.Organization.Domain;

/// <summary>The customer who bought the system (a person or a company group).</summary>
public class Tenant : Entity
{
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Company> Companies { get; set; } = new List<Company>();
}
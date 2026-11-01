using RetailX.BuildingBlocks.Domain;

namespace RetailX.Organization.Domain;

/// <summary>A sub-company inside a tenant (e.g. X Tech / X Wear).</summary>
public class Company : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public string? TaxNumber { get; set; }
    public string DefaultCurrency { get; set; } = "EGP";
    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
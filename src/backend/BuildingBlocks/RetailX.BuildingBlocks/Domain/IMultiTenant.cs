namespace RetailX.BuildingBlocks.Domain;

/// <summary>Entity belongs to a Tenant (the customer who bought the system).</summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}

/// <summary>Entity belongs to a Company within the Tenant (e.g. X Tech / X Wear).</summary>
public interface ICompanyScoped : ITenantScoped
{
    Guid CompanyId { get; set; }
}

/// <summary>Entity belongs to a Branch within the Company.</summary>
public interface IBranchScoped : ICompanyScoped
{
    Guid BranchId { get; set; }
}
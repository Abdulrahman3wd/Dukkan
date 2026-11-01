namespace RetailX.BuildingBlocks.MultiTenancy;

/// <summary>
/// Describes who is calling and at which level. Filled from the JWT token per request.
/// </summary>
public interface ICurrentContext
{
    Guid? TenantId { get; }
    Guid? CompanyId { get; }
    Guid? BranchId { get; }
    Guid? UserId { get; }
}
using System.Security.Claims;
using RetailX.BuildingBlocks.MultiTenancy;

namespace RetailX.Api.Infrastructure;

/// <summary>Reads the caller's tenant/company/branch/user from the JWT claims.</summary>
public sealed class CurrentContext : ICurrentContext
{
    private readonly IHttpContextAccessor _http;
    public CurrentContext(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public Guid? TenantId => Read("tenant_id");
    public Guid? CompanyId => Read("company_id");
    public Guid? BranchId => Read("branch_id");
    public Guid? UserId => Read("sub");

    private Guid? Read(string claim)
        => User?.FindFirst(claim)?.Value is { } v && Guid.TryParse(v, out var g) ? g : null;
}
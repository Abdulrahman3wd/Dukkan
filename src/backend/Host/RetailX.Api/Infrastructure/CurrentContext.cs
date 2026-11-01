using RetailX.BuildingBlocks.MultiTenancy;

namespace RetailX.Api.Infrastructure;

/// <summary>Temporary: reads ids from headers. Will be replaced by JWT claims in the Identity module.</summary>
public sealed class CurrentContext : ICurrentContext
{
    private readonly IHttpContextAccessor _http;
    public CurrentContext(IHttpContextAccessor http) => _http = http;

    public Guid? TenantId => Read("X-Tenant-Id");
    public Guid? CompanyId => Read("X-Company-Id");
    public Guid? BranchId => Read("X-Branch-Id");
    public Guid? UserId => null;

    private Guid? Read(string header)
        => _http.HttpContext?.Request.Headers[header].FirstOrDefault() is { } v && Guid.TryParse(v, out var g)
            ? g : null;
}
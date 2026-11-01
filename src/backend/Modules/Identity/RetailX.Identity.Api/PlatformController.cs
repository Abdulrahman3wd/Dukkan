using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.Persistence;
using RetailX.Identity.Domain;
using RetailX.Organization.Domain;

namespace RetailX.Identity.Api;

public record ProvisionTenantRequest(
    string TenantName, string? TenantCode,
    string AdminUserName, string AdminFullName, string AdminPassword);

[ApiController]
[Authorize(Policy = "PlatformAdmin")]
[Route("api/platform")]
public class PlatformController : ControllerBase
{
    private readonly RetailXDbContext _db;
    private readonly IPasswordService _passwords;

    public PlatformController(RetailXDbContext db, IPasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    /// <summary>Creates a tenant together with its first Tenant Admin user.</summary>
    [HttpPost("tenants")]
    public async Task<IActionResult> ProvisionTenant(ProvisionTenantRequest r, CancellationToken ct)
    {
        var tenant = new Tenant { Name = r.TenantName, Code = r.TenantCode };
        _db.Set<Tenant>().Add(tenant);

        _db.Set<User>().Add(new User
        {
            TenantId = tenant.Id,
            UserName = r.AdminUserName,
            FullName = r.AdminFullName,
            PasswordHash = _passwords.Hash(r.AdminPassword),
            IsTenantAdmin = true
        });

        await _db.SaveChangesAsync(ct);
        return Ok(new { tenantId = tenant.Id });
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await _db.Set<Tenant>().AsNoTracking()
            .Select(t => new { t.Id, t.Name, t.Code, t.IsActive }).ToListAsync(ct));
}
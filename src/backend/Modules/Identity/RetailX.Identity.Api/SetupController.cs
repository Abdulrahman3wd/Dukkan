using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RetailX.BuildingBlocks.Persistence;
using RetailX.Identity.Domain;
using RetailX.Organization.Domain;

namespace RetailX.Identity.Api;

public record InitializeRequest(
    string CompanyName, string AdminUserName, string AdminFullName, string AdminPassword);

[ApiController]
[AllowAnonymous]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly RetailXDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly IConfiguration _cfg;
    private readonly IHostEnvironment _env;
    private readonly TenantResolver _resolver;

    public SetupController(RetailXDbContext db, IPasswordService passwords,
        IConfiguration cfg, IHostEnvironment env, TenantResolver resolver)
    {
        _db = db; _passwords = passwords; _cfg = cfg; _env = env; _resolver = resolver;
    }

    /// <summary>Public: tells the web app whether to show Setup or Login.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var tenants = await _db.Set<Tenant>().IgnoreQueryFilters()
            .Where(t => !t.IsDeleted).Select(t => t.Name).Take(2).ToListAsync(ct);

        return Ok(new
        {
            initialized = tenants.Count > 0,
            mode = _resolver.IsSingleTenant ? "SingleTenant" : "MultiTenant",
            tenantName = _resolver.IsSingleTenant && tenants.Count == 1 ? tenants[0] : null
        });
    }

    /// <summary>One-time installation: creates the tenant and its first admin.</summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize(
        InitializeRequest r, [FromHeader(Name = "X-Setup-Key")] string? key, CancellationToken ct)
    {
        if (!_resolver.IsSingleTenant) return NotFound();

        var expected = _cfg["Setup:Key"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            // no key configured: only allowed on a developer machine
            if (!_env.IsDevelopment()) return StatusCode(503, "Setup key is not configured on this server.");
        }
        else if (!CryptographicOperations.FixedTimeEquals(
                     Encoding.UTF8.GetBytes(key ?? ""), Encoding.UTF8.GetBytes(expected)))
        {
            return Unauthorized("Invalid setup key.");
        }

        if (await _db.Set<Tenant>().IgnoreQueryFilters().AnyAsync(ct))
            return Conflict("This server is already set up.");

        if (r.AdminPassword.Length < 8) return BadRequest("Password must be at least 8 characters.");

        var tenant = new Tenant { Name = r.CompanyName };
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
}
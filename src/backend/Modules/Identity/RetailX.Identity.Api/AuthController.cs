using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.BuildingBlocks.Persistence;
using RetailX.Identity.Domain;
using RetailX.Organization.Domain;

namespace RetailX.Identity.Api;

public record LoginRequest(Guid? TenantId, string UserName, string Password);
public record SelectBranchRequest(Guid BranchId);
public record BranchOption(Guid CompanyId, Guid BranchId);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    bool BranchSelected,
    IReadOnlyList<BranchOption> Branches);

public record RefreshRequest(string RefreshToken);
public record PlatformLoginRequest(string UserName, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const int RefreshTokenDays = 14;

    private readonly RetailXDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;
    private readonly ICurrentContext _ctx;
    private readonly TenantResolver _resolver;

    public AuthController(RetailXDbContext db, IPasswordService passwords,
        ITokenService tokens, ICurrentContext ctx, TenantResolver resolver)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
        _ctx = ctx;
        _resolver = resolver;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest r, CancellationToken ct)
    {
        var tenantId = await _resolver.ResolveAsync(r.TenantId, ct);
        if (tenantId is null) return BadRequest("Tenant could not be determined.");

        var user = await _db.Set<User>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId
                                      && u.UserName == r.UserName
                                      && !u.IsDeleted, ct);

        if (user is null || !user.IsActive || !_passwords.Verify(user.PasswordHash, r.Password))
            return Unauthorized("Invalid username or password.");

        var branches = await _db.Set<UserBranchAssignment>().IgnoreQueryFilters()
            .Where(a => a.UserId == user.Id && a.IsActive && !a.IsDeleted)
            .Select(a => new BranchOption(a.CompanyId, a.BranchId))
            .ToListAsync(ct);

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // exactly one branch -> select it automatically
        Guid? companyId = null, branchId = null;
        if (branches.Count == 1)
        {
            companyId = branches[0].CompanyId;
            branchId = branches[0].BranchId;
        }

        var token = _tokens.CreateAccessToken(new AccessTokenRequest(
            user.Id, user.TenantId, user.UserName, user.IsTenantAdmin, companyId, branchId));

        var refresh = await IssueRefreshTokenAsync(user.TenantId, user.Id, companyId, branchId, ct);
        return new AuthResponse(token, refresh, branchId is not null, branches);
    }

    [Authorize]
    [HttpPost("select-branch")]
    public async Task<ActionResult<AuthResponse>> SelectBranch(SelectBranchRequest r, CancellationToken ct)
    {
        if (_ctx.UserId is not { } userId || _ctx.TenantId is not { } tenantId)
            return Unauthorized();

        var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !user.IsActive) return Unauthorized();

        var assignments = await _db.Set<UserBranchAssignment>()
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync(ct);

        Guid? companyId;
        var match = assignments.FirstOrDefault(a => a.BranchId == r.BranchId);

        if (match is not null)
        {
            companyId = match.CompanyId;
        }
        else if (user.IsTenantAdmin)
        {
            // tenant admin may enter any branch of the tenant without an assignment
            var branch = await _db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == r.BranchId, ct);
            if (branch is null) return NotFound("Branch not found.");
            companyId = branch.CompanyId;
        }
        else
        {
            return Forbid();
        }

        var token = _tokens.CreateAccessToken(new AccessTokenRequest(
            user.Id, tenantId, user.UserName, user.IsTenantAdmin, companyId, r.BranchId));

        var refresh = await IssueRefreshTokenAsync(tenantId, user.Id, companyId, r.BranchId, ct);
        var branches = assignments.Select(a => new BranchOption(a.CompanyId, a.BranchId)).ToList();
        return new AuthResponse(token, refresh, true, branches);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest r, CancellationToken ct)
    {
        var hash = _tokens.HashRefreshToken(r.RefreshToken);

        // the hash is unique, so the tenant doesn't need to be supplied
        var stored = await _db.Set<RefreshToken>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null) return Unauthorized();

        // reuse of a revoked token => assume theft, revoke every token of this user
        if (stored.RevokedAtUtc is not null)
        {
            var all = await _db.Set<RefreshToken>().IgnoreQueryFilters()
                .Where(t => t.UserId == stored.UserId && t.RevokedAtUtc == null)
                .ToListAsync(ct);
            foreach (var t in all) t.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Unauthorized();
        }

        if (stored.ExpiresAtUtc <= DateTime.UtcNow) return Unauthorized();

        var user = await _db.Set<User>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == stored.UserId && !u.IsDeleted, ct);
        if (user is null || !user.IsActive) return Unauthorized();

        var newRaw = _tokens.GenerateRefreshToken();
        var newHash = _tokens.HashRefreshToken(newRaw);

        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.ReplacedByTokenHash = newHash;

        _db.Set<RefreshToken>().Add(new RefreshToken
        {
            TenantId = stored.TenantId,
            UserId = stored.UserId,
            TokenHash = newHash,
            CompanyId = stored.CompanyId,
            BranchId = stored.BranchId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
        });
        await _db.SaveChangesAsync(ct);

        var access = _tokens.CreateAccessToken(new AccessTokenRequest(
            user.Id, user.TenantId, user.UserName, user.IsTenantAdmin, stored.CompanyId, stored.BranchId));

        return new AuthResponse(access, newRaw, stored.BranchId is not null, Array.Empty<BranchOption>());
    }

    [AllowAnonymous]
    [HttpPost("platform-login")]
    public async Task<IActionResult> PlatformLogin(PlatformLoginRequest r, CancellationToken ct)
    {
        var admin = await _db.Set<PlatformAdmin>()
            .FirstOrDefaultAsync(a => a.UserName == r.UserName && !a.IsDeleted, ct);

        if (admin is null || !admin.IsActive || !_passwords.Verify(admin.PasswordHash, r.Password))
            return Unauthorized("Invalid username or password.");

        return Ok(new { accessToken = _tokens.CreatePlatformAdminToken(admin.Id, admin.UserName) });
    }

    private async Task<string> IssueRefreshTokenAsync(
        Guid tenantId, Guid userId, Guid? companyId, Guid? branchId, CancellationToken ct)
    {
        var raw = _tokens.GenerateRefreshToken();
        _db.Set<RefreshToken>().Add(new RefreshToken
        {
            TenantId = tenantId,
            UserId = userId,
            TokenHash = _tokens.HashRefreshToken(raw),
            CompanyId = companyId,
            BranchId = branchId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
        });
        await _db.SaveChangesAsync(ct);
        return raw;
    }
}
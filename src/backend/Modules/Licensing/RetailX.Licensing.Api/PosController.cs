using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.Persistence;
using RetailX.Identity.Api;
using RetailX.Identity.Domain;
using RetailX.Identity.Infrastructure;
using RetailX.Licensing.Domain;
using RetailX.Organization.Domain;

namespace RetailX.Licensing.Api;

public record PosLoginRequest(Guid TenantId, Guid TerminalId, string UserName, string Password);

public record PosLoginResponse(
    string AccessToken, Guid SessionId, Guid CompanyId, Guid BranchId,
    int HeartbeatSeconds, int TimeoutSeconds);

[ApiController]
[Route("api/pos")]
public class PosController : ControllerBase
{
    // POS token lasts a full shift; the seat itself is protected by the heartbeat.
    private const int PosTokenMinutes = 600;

    private readonly RetailXDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;
    private readonly ISeatService _seats;

    public PosController(RetailXDbContext db, IPasswordService passwords,
        ITokenService tokens, ISeatService seats)
    {
        _db = db; _passwords = passwords; _tokens = tokens; _seats = seats;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<PosLoginResponse>> Login(PosLoginRequest r, CancellationToken ct)
    {
        var terminal = await _db.Set<Terminal>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == r.TerminalId && t.TenantId == r.TenantId
                                      && t.IsActive && !t.IsDeleted, ct);

        var user = await _db.Set<User>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == r.TenantId && u.UserName == r.UserName
                                      && !u.IsDeleted, ct);

        if (terminal is null || user is null || !user.IsActive
            || !_passwords.Verify(user.PasswordHash, r.Password))
            return Unauthorized("Invalid terminal, username or password.");

        if (!user.IsTenantAdmin)
        {
            var assigned = await _db.Set<UserBranchAssignment>().IgnoreQueryFilters()
                .AnyAsync(a => a.UserId == user.Id && a.BranchId == terminal.BranchId
                               && a.IsActive && !a.IsDeleted, ct);
            if (!assigned) return StatusCode(403, "You are not assigned to this branch.");
        }

        SeatLease lease;
        try
        {
            lease = await _seats.AcquireAsync(terminal.TenantId, terminal.CompanyId,
                terminal.BranchId, terminal.Id, user.Id, ct);
        }
        catch (SeatUnavailableException ex) { return Conflict(ex.Message); }
        catch (LicenseException ex) { return StatusCode(402, ex.Message); }

        var token = _tokens.CreateAccessToken(new AccessTokenRequest(
            user.Id, user.TenantId, user.UserName, user.IsTenantAdmin,
            terminal.CompanyId, terminal.BranchId,
            TerminalId: terminal.Id, SeatSessionId: lease.SessionId, LifetimeMinutes: PosTokenMinutes));

        return new PosLoginResponse(token, lease.SessionId, terminal.CompanyId, terminal.BranchId,
            lease.HeartbeatSeconds, lease.TimeoutSeconds);
    }

    [Authorize]
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(CancellationToken ct)
    {
        if (!TryGetSession(out var id)) return BadRequest("This token is not a POS session.");
        return await _seats.HeartbeatAsync(id, ct)
            ? NoContent()
            : StatusCode(410, "Seat expired. Please log in again.");
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!TryGetSession(out var id)) return BadRequest("This token is not a POS session.");
        await _seats.ReleaseAsync(id, ct);
        return NoContent();
    }

    [Authorize]
    [HasPermission(CorePermissions.SeatsView)]
    [HttpGet("seats/{branchId:guid}")]
    public async Task<SeatUsage> Usage(Guid branchId, CancellationToken ct)
        => await _seats.GetUsageAsync(branchId, ct);

    [Authorize]
    [HasPermission(CorePermissions.SeatsManage)]
    [HttpPost("seats/{sessionId:guid}/release")]
    public async Task<IActionResult> ForceRelease(Guid sessionId, CancellationToken ct)
        => await _seats.ForceReleaseAsync(sessionId, ct) ? NoContent() : NotFound();

    private bool TryGetSession(out Guid id)
        => Guid.TryParse(User.FindFirst("seat_session_id")?.Value, out id);
}
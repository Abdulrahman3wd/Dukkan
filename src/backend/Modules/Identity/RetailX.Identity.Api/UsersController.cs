using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.Persistence;
using RetailX.Identity.Domain;
using RetailX.Identity.Infrastructure;

namespace RetailX.Identity.Api;

public record CreateUserRequest(
    string UserName, string FullName, string Password,
    string? Email, string? Phone, bool IsTenantAdmin);

public record UpdateUserRequest(string FullName, string? Email, string? Phone, bool IsActive);
public record ChangePasswordRequest(string NewPassword);
public record SetPinRequest(string Pin);

public record AssignmentDto(Guid Id, Guid CompanyId, Guid BranchId, Guid? RoleId, bool IsActive,
    DateTime StartedAtUtc, DateTime? EndedAtUtc);

public record UserDto(Guid Id, string UserName, string FullName, string? Email, string? Phone,
    bool IsTenantAdmin, bool IsActive, IReadOnlyList<AssignmentDto> Assignments);

public record TransferRequest(Guid FromBranchId, Guid ToCompanyId, Guid ToBranchId, Guid? RoleId);

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly RetailXDbContext _db;
    private readonly IPasswordService _passwords;

    public UsersController(RetailXDbContext db, IPasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    [HttpGet]
    [HasPermission(CorePermissions.UsersView)]
    public async Task<IEnumerable<UserDto>> List(CancellationToken ct)
        => await _db.Set<User>().AsNoTracking()
            .Select(u => new UserDto(u.Id, u.UserName, u.FullName, u.Email, u.Phone,
                u.IsTenantAdmin, u.IsActive,
                u.Assignments.Where(a => !a.IsDeleted)
                    .Select(a => new AssignmentDto(a.Id, a.CompanyId, a.BranchId, a.RoleId,
                        a.IsActive, a.StartedAtUtc, a.EndedAtUtc)).ToList()))
            .ToListAsync(ct);

    [HttpPost]
    [HasPermission(CorePermissions.UsersManage)]
    public async Task<IActionResult> Create(CreateUserRequest r, CancellationToken ct)
    {
        if (r.Password.Length < 8) return BadRequest("Password must be at least 8 characters.");

        var exists = await _db.Set<User>().AnyAsync(u => u.UserName == r.UserName, ct);
        if (exists) return Conflict("UserName already exists.");

        var user = new User
        {
            UserName = r.UserName,
            FullName = r.FullName,
            Email = r.Email,
            Phone = r.Phone,
            PasswordHash = _passwords.Hash(r.Password),
            IsTenantAdmin = r.IsTenantAdmin
        };
        _db.Set<User>().Add(user);
        await _db.SaveChangesAsync(ct);
        return Ok(new { user.Id });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(CorePermissions.UsersManage)]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest r, CancellationToken ct)
    {
        var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        user.FullName = r.FullName;
        user.Email = r.Email;
        user.Phone = r.Phone;
        user.IsActive = r.IsActive;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/password")]
    [HasPermission(CorePermissions.UsersManage)]
    public async Task<IActionResult> ChangePassword(Guid id, ChangePasswordRequest r, CancellationToken ct)
    {
        if (r.NewPassword.Length < 8) return BadRequest("Password must be at least 8 characters.");

        var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        user.PasswordHash = _passwords.Hash(r.NewPassword);

        // force re-login everywhere
        var tokens = await _db.Set<RefreshToken>()
            .Where(t => t.UserId == id && t.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var t in tokens) t.RevokedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Transfer: ends the assignment in the old branch and opens a new one.</summary>
    [HttpPost("{id:guid}/transfer")]
    [HasPermission(CorePermissions.UsersManage)]
    public async Task<IActionResult> Transfer(Guid id, TransferRequest r, CancellationToken ct)
    {
        var old = await _db.Set<UserBranchAssignment>()
            .FirstOrDefaultAsync(a => a.UserId == id && a.BranchId == r.FromBranchId && a.IsActive, ct);
        if (old is null) return NotFound("No active assignment in the source branch.");

        var roleId = r.RoleId ?? old.RoleId;

        old.IsActive = false;
        old.EndedAtUtc = DateTime.UtcNow;

        _db.Set<UserBranchAssignment>().Add(new UserBranchAssignment
        {
            UserId = id,
            CompanyId = r.ToCompanyId,
            BranchId = r.ToBranchId,
            RoleId = roleId
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Ends an assignment without opening a new one.</summary>
    [HttpDelete("{id:guid}/assignments/{branchId:guid}")]
    [HasPermission(CorePermissions.UsersManage)]
    public async Task<IActionResult> Unassign(Guid id, Guid branchId, CancellationToken ct)
    {
        var a = await _db.Set<UserBranchAssignment>()
            .FirstOrDefaultAsync(x => x.UserId == id && x.BranchId == branchId && x.IsActive, ct);
        if (a is null) return NotFound();

        a.IsActive = false;
        a.EndedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
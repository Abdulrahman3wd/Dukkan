using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.Persistence;
using RetailX.Identity.Domain;
using RetailX.Identity.Infrastructure;

namespace RetailX.Identity.Api;

public record RoleDto(Guid Id, string Name, string? Description, IReadOnlyList<string> Permissions);
public record SaveRoleRequest(string Name, string? Description, IReadOnlyList<string> Permissions);
public record AssignRoleRequest(Guid UserId, Guid CompanyId, Guid BranchId, Guid RoleId);

[ApiController]
[Authorize]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly RetailXDbContext _db;
    private readonly IPermissionRegistry _registry;

    public RolesController(RetailXDbContext db, IPermissionRegistry registry)
    {
        _db = db; _registry = registry;
    }

    [HttpGet("permissions")]
    [HasPermission(CorePermissions.RolesManage)]
    public IEnumerable<PermissionDefinition> AllPermissions()
        => _registry.All.OrderBy(p => p.Group).ThenBy(p => p.Key);

    [HttpGet]
    [HasPermission(CorePermissions.RolesManage)]
    public async Task<IEnumerable<RoleDto>> List(CancellationToken ct)
        => await _db.Set<Role>().AsNoTracking()
            .Select(r => new RoleDto(r.Id, r.Name, r.Description,
                r.Permissions.Where(p => !p.IsDeleted).Select(p => p.Permission).ToList()))
            .ToListAsync(ct);

    [HttpPost]
    [HasPermission(CorePermissions.RolesManage)]
    public async Task<IActionResult> Create(SaveRoleRequest r, CancellationToken ct)
    {
        var unknown = r.Permissions.Where(p => !_registry.Exists(p)).ToList();
        if (unknown.Count > 0) return BadRequest($"Unknown permissions: {string.Join(", ", unknown)}");

        var role = new Role { Name = r.Name, Description = r.Description };
        foreach (var p in r.Permissions.Distinct())
            role.Permissions.Add(new RolePermission { Permission = p });

        _db.Set<Role>().Add(role);
        await _db.SaveChangesAsync(ct);
        return Ok(new { role.Id });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(CorePermissions.RolesManage)]
    public async Task<IActionResult> Update(Guid id, SaveRoleRequest r, CancellationToken ct)
    {
        var unknown = r.Permissions.Where(p => !_registry.Exists(p)).ToList();
        if (unknown.Count > 0) return BadRequest($"Unknown permissions: {string.Join(", ", unknown)}");

        var role = await _db.Set<Role>().Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (role is null) return NotFound();

        role.Name = r.Name;
        role.Description = r.Description;

        var wanted = r.Permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var p in role.Permissions.Where(p => !p.IsDeleted && !wanted.Contains(p.Permission)).ToList())
            _db.Remove(p);   // soft delete via interceptor

        var existing = role.Permissions.Where(p => !p.IsDeleted)
            .Select(p => p.Permission).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var p in wanted.Where(p => !existing.Contains(p)))
            role.Permissions.Add(new RolePermission { Permission = p });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("assign")]
    [HasPermission(CorePermissions.UsersManage)]
    public async Task<IActionResult> Assign(AssignRoleRequest r, CancellationToken ct)
    {
        var role = await _db.Set<Role>().AnyAsync(x => x.Id == r.RoleId, ct);
        var user = await _db.Set<User>().AnyAsync(x => x.Id == r.UserId, ct);
        if (!role || !user) return NotFound();

        var a = await _db.Set<UserBranchAssignment>()
            .FirstOrDefaultAsync(x => x.UserId == r.UserId && x.BranchId == r.BranchId && x.IsActive, ct);

        if (a is null)
        {
            _db.Set<UserBranchAssignment>().Add(new UserBranchAssignment
            {
                UserId = r.UserId,
                CompanyId = r.CompanyId,
                BranchId = r.BranchId,
                RoleId = r.RoleId
            });
        }
        else
        {
            a.RoleId = r.RoleId;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
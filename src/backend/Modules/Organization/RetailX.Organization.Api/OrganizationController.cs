using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.BuildingBlocks.Persistence;
using RetailX.Organization.Domain;

namespace RetailX.Organization.Api;

[ApiController]
[Route("api/organization")]
public class OrganizationController : ControllerBase
{
    private readonly RetailXDbContext _db;
    private readonly ICurrentContext _ctx;

    public OrganizationController(RetailXDbContext db, ICurrentContext ctx)
    {
        _db = db;
        _ctx = ctx;
    }

    // ---------- Tenants (temporary open endpoint, will be Super Admin only) ----------

    [HttpPost("tenants")]
    public async Task<ActionResult<TenantResponse>> CreateTenant(CreateTenantRequest r, CancellationToken ct)
    {
        var t = new Tenant { Name = r.Name, Code = r.Code };
        _db.Set<Tenant>().Add(t);
        await _db.SaveChangesAsync(ct);
        return Ok(new TenantResponse(t.Id, t.Name, t.Code, t.IsActive));
    }

    [HttpGet("tenants")]
    public async Task<IEnumerable<TenantResponse>> GetTenants(CancellationToken ct)
        => await _db.Set<Tenant>().AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Select(t => new TenantResponse(t.Id, t.Name, t.Code, t.IsActive))
            .ToListAsync(ct);

    // ---------- Companies ----------

    [HttpPost("companies")]
    public async Task<ActionResult<CompanyResponse>> CreateCompany(CreateCompanyRequest r, CancellationToken ct)
    {
        if (_ctx.TenantId is null) return BadRequest("X-Tenant-Id header is required.");

        var c = new Company
        {
            Name = r.Name,
            Code = r.Code,
            TaxNumber = r.TaxNumber,
            DefaultCurrency = r.DefaultCurrency ?? "EGP"
        };
        _db.Set<Company>().Add(c);          // TenantId is filled by the interceptor
        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(c));
    }

    [HttpGet("companies")]
    public async Task<IEnumerable<CompanyResponse>> GetCompanies(CancellationToken ct)
        => await _db.Set<Company>().AsNoTracking()
            .Select(c => new CompanyResponse(c.Id, c.TenantId, c.Name, c.Code, c.TaxNumber, c.DefaultCurrency, c.IsActive))
            .ToListAsync(ct);

    // ---------- Branches ----------

    [HttpPost("companies/{companyId:guid}/branches")]
    public async Task<ActionResult<BranchResponse>> CreateBranch(Guid companyId, CreateBranchRequest r, CancellationToken ct)
    {
        var company = await _db.Set<Company>().FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company is null) return NotFound("Company not found.");

        var b = new Branch
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            Name = r.Name,
            Code = r.Code,
            Address = r.Address,
            Phone = r.Phone
        };
        _db.Set<Branch>().Add(b);
        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(b));
    }

    [HttpGet("companies/{companyId:guid}/branches")]
    public async Task<IEnumerable<BranchResponse>> GetBranches(Guid companyId, CancellationToken ct)
        => await _db.Set<Branch>().AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .Select(b => new BranchResponse(b.Id, b.CompanyId, b.Name, b.Code, b.Address, b.Phone, b.IsActive))
            .ToListAsync(ct);

    // ---------- Terminals ----------

    [HttpPost("branches/{branchId:guid}/terminals")]
    public async Task<ActionResult<TerminalResponse>> CreateTerminal(Guid branchId, CreateTerminalRequest r, CancellationToken ct)
    {
        var branch = await _db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == branchId, ct);
        if (branch is null) return NotFound("Branch not found.");

        var t = new Terminal
        {
            TenantId = branch.TenantId,
            CompanyId = branch.CompanyId,
            BranchId = branch.Id,
            Name = r.Name,
            Role = r.Role
        };
        _db.Set<Terminal>().Add(t);
        await _db.SaveChangesAsync(ct);
        return Ok(new TerminalResponse(t.Id, t.BranchId, t.Name, t.Role, t.IsActive));
    }

    [HttpGet("branches/{branchId:guid}/terminals")]
    public async Task<IEnumerable<TerminalResponse>> GetTerminals(Guid branchId, CancellationToken ct)
        => await _db.Set<Terminal>().AsNoTracking()
            .Where(t => t.BranchId == branchId)
            .Select(t => new TerminalResponse(t.Id, t.BranchId, t.Name, t.Role, t.IsActive))
            .ToListAsync(ct);

    private static CompanyResponse ToResponse(Company c)
        => new(c.Id, c.TenantId, c.Name, c.Code, c.TaxNumber, c.DefaultCurrency, c.IsActive);

    private static BranchResponse ToResponse(Branch b)
        => new(b.Id, b.CompanyId, b.Name, b.Code, b.Address, b.Phone, b.IsActive);
}
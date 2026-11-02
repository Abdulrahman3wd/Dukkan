using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailX.Identity.Api;
using RetailX.Licensing.Domain;

namespace RetailX.Licensing.Api;

public record SeatOverrideRequest(Guid BranchId, int Seats);

public record IssueLicenseRequest(
    Guid TenantId,
    string CustomerName,
    int Branches,
    int DefaultSeatsPerBranch,
    int ValidDays,
    int GraceDays = 14,
    List<SeatOverrideRequest>? SeatOverrides = null);

[ApiController]
[Route("api/license")]
public class LicensingController : ControllerBase
{
    private readonly ILicenseService _license;
    private readonly ILicenseSigner? _signer;

    public LicensingController(ILicenseService license, IServiceProvider sp)
    {
        _license = license;
        _signer = sp.GetService(typeof(ILicenseSigner)) as ILicenseSigner;
    }

    /// <summary>Current license status for the caller's tenant.</summary>
    [Authorize]
    [HttpGet("status")]
    public async Task<LicenseStatus> Status(CancellationToken ct)
        => await _license.GetStatusAsync(ct);

    /// <summary>Customer uploads the license file they received from us.</summary>
    [Authorize]
    [HasPermission("Settings.Manage")]
    [HttpPost("install")]
    public async Task<IActionResult> Install(SignedLicense license, CancellationToken ct)
    {
        try
        {
            return Ok(await _license.InstallAsync(license, ct));
        }
        catch (LicenseException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Vendor only: issues a signed license for a tenant.</summary>
    [Authorize(Policy = "PlatformAdmin")]
    [HttpPost("issue")]
    public IActionResult Issue(IssueLicenseRequest r)
    {
        if (_signer is null)
            return StatusCode(501, "This server has no signing key.");

        if (r.Branches < 1 || r.DefaultSeatsPerBranch < 1 || r.ValidDays < 1)
            return BadRequest("Branches, DefaultSeatsPerBranch and ValidDays must be at least 1.");

        if (r.SeatOverrides?.Any(o => o.Seats < 1) == true)
            return BadRequest("Seat overrides must be at least 1.");

        var now = DateTime.UtcNow;

        var overrides = (r.SeatOverrides ?? new List<SeatOverrideRequest>())
            .Select(o => new SeatOverride(o.BranchId, o.Seats))
            .ToList();

        var payload = new LicensePayload(
            Guid.NewGuid(),
            r.TenantId,
            r.CustomerName,
            r.Branches,
            r.DefaultSeatsPerBranch,
            overrides,
            now,
            now.AddDays(r.ValidDays),
            r.GraceDays);

        return Ok(_signer.Sign(payload));
    }
}
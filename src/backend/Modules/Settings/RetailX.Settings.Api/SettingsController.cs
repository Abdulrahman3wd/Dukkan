using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailX.Settings.Domain;

namespace RetailX.Settings.Api;

public record SettingDto(
    string Key, string Group, string DisplayNameEn, string DisplayNameAr,
    string Type, string MostSpecificScope, string EffectiveValue);

public record SetSettingRequest(SettingScope Scope, Guid ScopeId, string Value);
[Authorize]
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settings;
    private readonly ISettingsRegistry _registry;

    public SettingsController(ISettingsService settings, ISettingsRegistry registry)
    {
        _settings = settings;
        _registry = registry;
    }

    /// <summary>All settings with their effective value for the given company/branch.</summary>
    [HttpGet]
    public async Task<IEnumerable<SettingDto>> GetAll(
        [FromQuery] Guid? companyId, [FromQuery] Guid? branchId, CancellationToken ct)
    {
        var list = new List<SettingDto>();
        foreach (var d in _registry.All.OrderBy(x => x.Group).ThenBy(x => x.Key))
        {
            var value = await _settings.GetAsync(d.Key, companyId, branchId, ct);
            list.Add(new SettingDto(d.Key, d.Group, d.DisplayNameEn, d.DisplayNameAr,
                d.Type.ToString(), d.MostSpecificScope.ToString(), value));
        }
        return list;
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Set(string key, SetSettingRequest r, CancellationToken ct)
    {
        try
        {
            await _settings.SetAsync(key, r.Scope, r.ScopeId, r.Value, ct);
            return NoContent();
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Reset(string key, [FromQuery] SettingScope scope, [FromQuery] Guid scopeId, CancellationToken ct)
    {
        await _settings.ResetAsync(key, scope, scopeId, ct);
        return NoContent();
    }
}
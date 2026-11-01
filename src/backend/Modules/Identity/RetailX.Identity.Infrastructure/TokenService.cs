using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RetailX.Identity.Domain;

namespace RetailX.Identity.Infrastructure;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _opt;
    public TokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

    public string CreateAccessToken(AccessTokenRequest c)
    {
        var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Sub, c.UserId.ToString()),
    new Claim(JwtRegisteredClaimNames.UniqueName, c.UserName),
    new Claim("tenant_id", c.TenantId.ToString()),
    new Claim("is_tenant_admin", c.IsTenantAdmin ? "true" : "false")
};
        if (c.CompanyId is { } co) claims.Add(new Claim("company_id", co.ToString()));
        if (c.BranchId is { } br) claims.Add(new Claim("branch_id", br.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreatePlatformAdminToken(Guid adminId, string userName)
    {
        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, adminId.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, userName),
        new Claim("platform_admin", "true")
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string rawToken)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
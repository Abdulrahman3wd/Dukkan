namespace RetailX.Identity.Domain;

public record AccessTokenRequest(
    Guid UserId,
    Guid TenantId,
    string UserName,
    bool IsTenantAdmin,
    Guid? CompanyId,
    Guid? BranchId);

public interface ITokenService
{
    string CreateAccessToken(AccessTokenRequest request);
    string CreatePlatformAdminToken(Guid adminId, string userName);

    /// <summary>Returns a new random token (raw, to send to the client).</summary>
    string GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
}
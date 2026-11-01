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
}
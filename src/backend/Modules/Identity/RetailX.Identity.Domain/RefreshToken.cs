using RetailX.BuildingBlocks.Domain;

namespace RetailX.Identity.Domain;

public class RefreshToken : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>SHA-256 of the token. The raw token is never stored.</summary>
    public string TokenHash { get; set; } = default!;

    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}
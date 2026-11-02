using RetailX.BuildingBlocks.Domain;

namespace RetailX.Licensing.Domain;

/// <summary>One POS login holding a seat. Released on logout, force release, or heartbeat timeout.</summary>
public class SeatSession : Entity, IBranchScoped
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public Guid TerminalId { get; set; }
    public Guid UserId { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeatAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public string? EndReason { get; set; }
}
namespace RetailX.Licensing.Domain;

public sealed record SeatLease(Guid SessionId, int HeartbeatSeconds, int TimeoutSeconds);

public sealed record SeatSessionInfo(
    Guid SessionId, Guid TerminalId, Guid UserId, DateTime StartedAtUtc, DateTime LastHeartbeatAtUtc);

public sealed record SeatUsage(
    Guid BranchId,
    int TotalSeats,
    int ReservedForManager,
    int AvailableForUsers,
    int UsedSeats,
    IReadOnlyList<SeatSessionInfo> Sessions);

public interface ISeatService
{
    /// <summary>Takes a seat at login. Throws SeatUnavailableException / LicenseException.</summary>
    Task<SeatLease> AcquireAsync(Guid tenantId, Guid companyId, Guid branchId,
        Guid terminalId, Guid userId, CancellationToken ct = default);

    /// <summary>False if the session is gone or timed out (client must log in again).</summary>
    Task<bool> HeartbeatAsync(Guid sessionId, CancellationToken ct = default);

    Task ReleaseAsync(Guid sessionId, CancellationToken ct = default);
    Task<bool> ForceReleaseAsync(Guid sessionId, CancellationToken ct = default);
    Task<SeatUsage> GetUsageAsync(Guid branchId, CancellationToken ct = default);
}
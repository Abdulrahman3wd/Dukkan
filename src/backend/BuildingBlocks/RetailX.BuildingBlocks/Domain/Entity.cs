namespace RetailX.BuildingBlocks.Domain;

/// <summary>
/// Base for all entities. Guid Id + Version are required from day one for offline sync.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Incremented on every change; used by sync to resolve conflicts.</summary>
    public long Version { get; set; }

    /// <summary>Soft delete so deletions can be synced to POS terminals.</summary>
    public bool IsDeleted { get; set; }
}
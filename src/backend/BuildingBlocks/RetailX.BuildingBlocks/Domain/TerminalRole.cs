using RetailX.BuildingBlocks.Domain;

namespace RetailX.Organization.Domain;

public enum TerminalRole
{
    /// <summary>Standalone POS, or the one that collects from other POS and syncs to the server.</summary>
    Manager = 1,
    /// <summary>Sends its invoices to the branch Manager.</summary>
    Pos = 2
}

public class Terminal : Entity, IBranchScoped
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public string Name { get; set; } = default!;
    public string? MachineId { get; set; }   // filled when the terminal is activated (licensing)
    public TerminalRole Role { get; set; } = TerminalRole.Pos;
    public bool IsActive { get; set; } = true;

    public Branch? Branch { get; set; }
}
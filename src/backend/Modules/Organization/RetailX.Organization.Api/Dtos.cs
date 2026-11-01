using RetailX.Organization.Domain;

namespace RetailX.Organization.Api;

public record CreateTenantRequest(string Name, string? Code);
public record TenantResponse(Guid Id, string Name, string? Code, bool IsActive);

public record CreateCompanyRequest(string Name, string? Code, string? TaxNumber, string? DefaultCurrency);
public record CompanyResponse(Guid Id, Guid TenantId, string Name, string? Code, string? TaxNumber, string DefaultCurrency, bool IsActive);

public record CreateBranchRequest(string Name, string? Code, string? Address, string? Phone);
public record BranchResponse(Guid Id, Guid CompanyId, string Name, string? Code, string? Address, string? Phone, bool IsActive);

public record CreateTerminalRequest(string Name, TerminalRole Role);
public record TerminalResponse(Guid Id, Guid BranchId, string Name, TerminalRole Role, bool IsActive);
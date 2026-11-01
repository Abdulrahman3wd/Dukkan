using Microsoft.EntityFrameworkCore;
using RetailX.Organization.Domain;
using System.Reflection.Emit;

namespace RetailX.Organization.Infrastructure;

/// <summary>Registers the Organization module entities into the shared DbContext.</summary>
public static class OrganizationModelConfiguration
{
    public static void ApplyOrganization(this ModelBuilder b)
    {
        b.Entity<Tenant>(e =>
        {
            e.ToTable("Tenants", "org");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
        });

        b.Entity<Company>(e =>
        {
            e.ToTable("Companies", "org");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.TaxNumber).HasMaxLength(50);
            e.Property(x => x.DefaultCurrency).HasMaxLength(3).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[Code] IS NOT NULL");
            e.HasOne(x => x.Tenant).WithMany(t => t.Companies)
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Branch>(e =>
        {
            e.ToTable("Branches", "org");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.BranchId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasFilter("[Code] IS NOT NULL");
            e.HasOne(x => x.Company).WithMany(c => c.Branches)
                .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Terminal>(e =>
        {
            e.ToTable("Terminals", "org");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.MachineId).HasMaxLength(200);
            e.HasIndex(x => x.MachineId).IsUnique().HasFilter("[MachineId] IS NOT NULL");
            e.HasOne(x => x.Branch).WithMany(br => br.Terminals)
                .HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
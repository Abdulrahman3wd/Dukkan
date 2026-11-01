using Microsoft.EntityFrameworkCore;
using RetailX.Identity.Domain;

namespace RetailX.Identity.Infrastructure;

public static class IdentityModelConfiguration
{
    public static void ApplyIdentity(this ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("Users", "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.PinHash).HasMaxLength(500);
            e.HasIndex(x => new { x.TenantId, x.UserName }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        b.Entity<UserBranchAssignment>(e =>
        {
            e.ToTable("UserBranchAssignments", "identity");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany(u => u.Assignments)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UserId, x.BranchId, x.IsActive });
        });
    }
}
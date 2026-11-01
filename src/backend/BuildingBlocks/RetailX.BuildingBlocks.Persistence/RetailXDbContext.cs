using Microsoft.EntityFrameworkCore;
using RetailX.Organization.Infrastructure;

namespace RetailX.BuildingBlocks.Persistence;

public class RetailXDbContext : DbContext
{
    public RetailXDbContext(DbContextOptions<RetailXDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOrganization();
        // next modules will be registered here
    }
}
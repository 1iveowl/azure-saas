using Saas.Identity.Authorization.Model.Data;
using Saas.Permissions.Api.Data.Configuration;

namespace Saas.Permissions.Api.Data.Context;

public class SaasPermissionsContext : DbContext
{
    public SaasPermissionsContext(DbContextOptions<SaasPermissionsContext> options) : base(options)
    {

    }

    public DbSet<SaasPermission> SaasPermissions { get; set; }
    public DbSet<TenantPermission> TenantPermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new UserPermissionEntityTypeConfiguration().Configure(modelBuilder.Entity<UserPermission>());
        new TenantPermissionEntityTypeConfiguration().Configure(modelBuilder.Entity<TenantPermission>());
        new SaasPermissionEntityTypeConfiguration().Configure(modelBuilder.Entity<SaasPermission>());
    }
}

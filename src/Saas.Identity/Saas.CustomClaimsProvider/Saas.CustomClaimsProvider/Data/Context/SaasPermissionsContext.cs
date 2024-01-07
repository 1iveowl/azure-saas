﻿using Microsoft.EntityFrameworkCore;
using Saas.Identity.Authorization.Model.Data;
using Saas.CustomClaimsProvider.Data.Configuration;

namespace Saas.CustomClaimsProvider.Data.Context;

public class SaasPermissionsContext(DbContextOptions<SaasPermissionsContext> options) : DbContext(options)
{
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

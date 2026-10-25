using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TaskHive.Application.Common;
using TaskHive.Domain.Common;
using TaskHive.Domain.Entities;

namespace TaskHive.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        private readonly ITenantContext _tenantContext;

        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
            : base(options)
        {
            _tenantContext = tenantContext;
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Pick up every IEntityTypeConfiguration<T> in this assembly.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Apply the tenant filter to every ITenantOwned entity, discovered from the model
            // itself. A new tenant-owned entity cannot forget to be filtered.
            var applyFilter = typeof(AppDbContext).GetMethod(
                nameof(ApplyTenantFilter),
                BindingFlags.Instance | BindingFlags.NonPublic)!;

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
                    continue;

                applyFilter.MakeGenericMethod(entityType.ClrType)
                           .Invoke(this, new object[] { modelBuilder });
            }
        }

        private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class, ITenantOwned
        {
            modelBuilder.Entity<TEntity>()
                        .HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        }

        // Override the (bool) overloads: the parameterless SaveChanges/SaveChangesAsync
        // delegate into these, so this covers all four entry points.
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyTenantAndAuditRules();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            ApplyTenantAndAuditRules();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ApplyTenantAndAuditRules()
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = DateTime.UtcNow;
            }

            foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
            {
                if (entry.State == EntityState.Added)
                {
                    // Only fill in a MISSING tenant. An explicitly set TenantId passes through,
                    // which is what lets signup create the first user before any JWT exists.
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = _tenantContext.TenantId
                            ?? throw new InvalidOperationException(
                                $"Cannot insert {entry.Entity.GetType().Name}: " +
                                "no tenant in context and no TenantId supplied.");
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    // A row must never move between tenants.
                    entry.Property(nameof(ITenantOwned.TenantId)).IsModified = false;
                }
            }
        }
    }
}

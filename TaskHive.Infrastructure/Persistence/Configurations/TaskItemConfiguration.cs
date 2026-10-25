using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskHive.Domain.Entities;

namespace TaskHive.Infrastructure.Persistence.Configurations
{
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("Tasks");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
            builder.Property(t => t.Description).HasMaxLength(4000);
            builder.Property(t => t.Status).IsRequired().HasMaxLength(20);
            builder.Property(t => t.Priority).IsRequired().HasMaxLength(20);

            builder.HasIndex(t => new { t.TenantId, t.ProjectId, t.Status });

            builder.HasOne<Project>()
                   .WithMany()
                   .HasForeignKey(t => t.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            // No foreign key from TaskItem.TenantId to Tenants - deliberate.
            // Tenant is reachable via Project, so this column is denormalized purely
            // so the tenant filter never needs a join. Adding the FK would give SQL
            // Server two cascade paths into Tasks (Tenant -> Tasks, and
            // Tenant -> Projects -> Tasks) and it rejects that with error 1785.
            // Integrity is enforced in SaveChanges instead.
        }
    }
}
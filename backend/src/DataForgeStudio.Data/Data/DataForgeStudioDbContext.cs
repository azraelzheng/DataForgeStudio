using Microsoft.EntityFrameworkCore;
using DataForgeStudio.Domain.Entities;

namespace DataForgeStudio.Data.Data;

/// <summary>
/// 数据库上下文
/// </summary>
public class DataForgeStudioDbContext : DbContext
{
    public DataForgeStudioDbContext(DbContextOptions<DataForgeStudioDbContext> options)
        : base(options)
    {
    }

    #region DbSets

    // 用户与权限
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    // 日志
    public DbSet<OperationLog> OperationLogs { get; set; }
    public DbSet<LoginLog> LoginLogs { get; set; }

    // 报表
    public DbSet<DataSource> DataSources { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<ReportField> ReportFields { get; set; }
    public DbSet<ReportParameter> ReportParameters { get; set; }

    // 系统
    public DbSet<SystemConfig> SystemConfigs { get; set; }
    public DbSet<BackupRecord> BackupRecords { get; set; }
    public DbSet<BackupSchedule> BackupSchedules { get; set; }
    public DbSet<License> Licenses { get; set; }

    // 大屏
    public DbSet<Dashboard> Dashboards { get; set; }
    public DbSet<DashboardWidget> DashboardWidgets { get; set; }
    public DbSet<WidgetRule> WidgetRules { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置 User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsSystem);

            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();

            // 约束: 只有 root 用户可以设置为 IsSystem = 1
            entity.ToTable(t => t.HasCheckConstraint("CK_Users_IsSystem", "[IsSystem] = 0 OR [Username] = 'root'"));
        });

        // 配置 Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleCode).IsUnique();
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RoleCode).HasMaxLength(50).IsRequired();
        });

        // 配置 UserRole
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => e.PermissionCode).IsUnique();
            entity.HasIndex(e => e.Module);
            entity.HasIndex(e => e.ParentId);

            entity.Property(e => e.PermissionCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PermissionName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Module).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();

            entity.HasOne(e => e.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置 RolePermission
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 DataSource
        modelBuilder.Entity<DataSource>(entity =>
        {
            entity.HasIndex(e => e.DataSourceCode).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsDefault);

            entity.Property(e => e.DataSourceName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DataSourceCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DbType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ServerAddress).HasMaxLength(200).IsRequired();

            // 约束: 只能有一个默认数据源
            entity.ToTable(t => t.HasCheckConstraint("CK_DataSources_DefaultCount",
                "[IsDefault] = 0 OR NOT EXISTS (SELECT 1 FROM DataSources d2 WHERE d2.IsDefault = 1 AND d2.DataSourceId <> [DataSourceId])"));
        });

        // 配置 Report
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasIndex(e => e.ReportCode).IsUnique();
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.ReportCategory);
            entity.HasIndex(e => e.DataSourceId);

            entity.Property(e => e.ReportName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ReportCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SqlStatement).IsRequired();

            // 约束: SQL 语句必须以 SELECT 开头
            entity.ToTable(t => t.HasCheckConstraint("CK_Reports_SqlStatement",
                "[SqlStatement] LIKE 'SELECT%' OR [SqlStatement] LIKE 'select%'"));

            entity.HasOne(e => e.DataSource)
                .WithMany(d => d.Reports)
                .HasForeignKey(e => e.DataSourceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置 ReportField
        modelBuilder.Entity<ReportField>(entity =>
        {
            entity.HasIndex(e => e.ReportId);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.Fields)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 ReportParameter
        modelBuilder.Entity<ReportParameter>(entity =>
        {
            entity.HasIndex(e => e.ReportId);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.Parameters)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 OperationLog
        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Module);
            entity.HasIndex(e => e.CreatedTime);

            entity.HasOne(e => e.User)
                .WithMany(u => u.OperationLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置 LoginLog
        modelBuilder.Entity<LoginLog>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LoginTime);

            entity.HasOne(e => e.User)
                .WithMany(u => u.LoginLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置 SystemConfig
        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasIndex(e => e.ConfigKey).IsUnique();

            entity.Property(e => e.ConfigKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ConfigType).HasMaxLength(20).IsRequired();
        });

        // 配置 BackupRecord
        modelBuilder.Entity<BackupRecord>(entity =>
        {
            entity.HasIndex(e => e.BackupTime);
        });

        // 配置 License
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasIndex(e => e.MachineCode).IsUnique();

            entity.Property(e => e.LicenseKey).HasMaxLength(4096).IsRequired();
            entity.Property(e => e.Signature).HasMaxLength(512).IsRequired();
            entity.Property(e => e.MachineCode).HasMaxLength(64).IsRequired();
        });

        // 配置 BackupSchedule
        modelBuilder.Entity<BackupSchedule>(entity =>
        {
            entity.HasIndex(e => e.NextRunTime);
            entity.HasIndex(e => e.IsEnabled);
        });

        // 配置 Dashboard
        modelBuilder.Entity<Dashboard>(entity =>
        {
            entity.HasIndex(e => e.IsPublic);
            entity.HasIndex(e => e.CreatedTime);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PublicUrl).HasFilter("[PublicUrl] IS NOT NULL");

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Theme).HasMaxLength(20).HasDefaultValue("dark");
            entity.Property(e => e.Status).HasDefaultValue("draft");
            entity.Property(e => e.BackgroundColor).HasDefaultValue("#0a1628");

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置 DashboardWidget
        modelBuilder.Entity<DashboardWidget>(entity =>
        {
            entity.HasIndex(e => new { e.DashboardId, e.ReportId });

            entity.Property(e => e.WidgetType).HasMaxLength(50).IsRequired();

            entity.HasOne(e => e.Dashboard)
                .WithMany(d => d.Widgets)
                .HasForeignKey(e => e.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Report)
                .WithMany()
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置 WidgetRule
        modelBuilder.Entity<WidgetRule>(entity =>
        {
            entity.HasIndex(e => e.WidgetId);

            entity.Property(e => e.Field).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Operator).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ActionType).HasMaxLength(50).IsRequired();

            entity.HasOne(e => e.Widget)
                .WithMany(w => w.Rules)
                .HasForeignKey(e => e.WidgetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 全局配置: 禁止级联删除
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    /// <summary>
    /// 保存更改时自动更新 UpdatedTime
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// 异步保存更改时自动更新 UpdatedTime
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Property("CreatedTime").CurrentValue == null)
            {
                entry.Property("CreatedTime").CurrentValue = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property("UpdatedTime").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}

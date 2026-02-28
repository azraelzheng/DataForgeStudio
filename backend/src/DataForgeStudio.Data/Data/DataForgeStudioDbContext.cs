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

    // 看板
    public DbSet<KanbanBoard> KanbanBoards { get; set; }
    public DbSet<KanbanCard> KanbanCards { get; set; }
    public DbSet<KanbanActivity> KanbanActivities { get; set; }
    public DbSet<KanbanAttachment> KanbanAttachments { get; set; }
    public DbSet<KanbanComment> KanbanComments { get; set; }

    // Dashboard
    public DbSet<Dashboard> Dashboards { get; set; }
    public DbSet<DashboardWidget> DashboardWidgets { get; set; }

    // Display
    public DbSet<DisplayConfig> DisplayConfigs { get; set; }

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

            // 注意: "只能有一个默认数据源" 的约束已移至业务逻辑层 (DataSourceService)
            // SQL Server CHECK 约束不支持子查询
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

        // 配置 KanbanBoard
        modelBuilder.Entity<KanbanBoard>(entity =>
        {
            entity.Property(e => e.BoardName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ColumnsConfig).IsRequired();
            entity.Property(e => e.SwimLaneBy).HasMaxLength(50);
            entity.Property(e => e.CustomSwimLaneField).HasMaxLength(100);
        });

        // 配置 KanbanCard
        modelBuilder.Entity<KanbanCard>(entity =>
        {
            entity.HasIndex(e => e.BoardId);
            entity.HasIndex(e => new { e.BoardId, e.Status });
            entity.HasIndex(e => new { e.BoardId, e.Status, e.SortOrder });

            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Priority).HasMaxLength(20).IsRequired();
            entity.Property(e => e.AssigneeId).HasMaxLength(100);
            entity.Property(e => e.AssigneeName).HasMaxLength(100);
            entity.Property(e => e.AssigneeAvatar).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);

            entity.HasOne(e => e.Board)
                .WithMany(b => b.Cards)
                .HasForeignKey(e => e.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 KanbanActivity
        modelBuilder.Entity<KanbanActivity>(entity =>
        {
            entity.HasIndex(e => e.CardId);
            entity.HasIndex(e => e.CreatedTime);

            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(100);
            entity.Property(e => e.ActivityType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasOne(e => e.Card)
                .WithMany(c => c.Activities)
                .HasForeignKey(e => e.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 KanbanAttachment
        modelBuilder.Entity<KanbanAttachment>(entity =>
        {
            entity.HasIndex(e => e.CardId);

            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileType).HasMaxLength(100);
            entity.Property(e => e.UploadedBy).HasMaxLength(100);

            entity.HasOne(e => e.Card)
                .WithMany()
                .HasForeignKey(e => e.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 KanbanComment
        modelBuilder.Entity<KanbanComment>(entity =>
        {
            entity.HasIndex(e => e.CardId);
            entity.HasIndex(e => e.CreatedTime);

            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasOne(e => e.Card)
                .WithMany()
                .HasForeignKey(e => e.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 Dashboard
        modelBuilder.Entity<Dashboard>(entity =>
        {
            entity.HasIndex(e => e.DashboardGuid).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsPublished);

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Category).HasMaxLength(50);
        });

        // 配置 DashboardWidget
        modelBuilder.Entity<DashboardWidget>(entity =>
        {
            entity.HasIndex(e => e.DashboardId);
            entity.HasIndex(e => e.WidgetGuid).IsUnique();
            entity.HasIndex(e => new { e.DashboardId, e.DisplayOrder });

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Position).IsRequired();

            entity.HasOne(e => e.Dashboard)
                .WithMany(d => d.Widgets)
                .HasForeignKey(e => e.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 DisplayConfig
        modelBuilder.Entity<DisplayConfig>(entity =>
        {
            entity.HasIndex(e => e.ConfigGuid).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Transition).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DashboardIds).IsRequired();
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

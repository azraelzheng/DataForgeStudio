# DataForgeStudio V4 数据库设计

## 设计原则
- 兼容 SQL Server 2005+
- 使用nvarchar支持中文
- 主键使用自增IDENTITY
- 统一字段命名规范
- 内置root用户（IsSystem=1）

## 完整表结构

### 1. 用户表 (Users)
```sql
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    RealName NVARCHAR(50),
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    Department NVARCHAR(100),
    Position NVARCHAR(50),
    IsActive BIT NOT NULL DEFAULT 1,
    IsSystem BIT NOT NULL DEFAULT 0,     -- 系统内置用户(root)
    IsLocked BIT NOT NULL DEFAULT 0,
    LastLoginTime DATETIME,
    LastLoginIP NVARCHAR(50),
    PasswordFailCount INT DEFAULT 0,
    MustChangePassword BIT DEFAULT 0,
    Remark NVARCHAR(500),
    CreatedBy INT,
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedBy INT,
    UpdatedTime DATETIME,
    CONSTRAINT CK_Users_IsSystem CHECK (IsSystem = 0 OR Username = 'root')
)

-- 内置root用户
INSERT INTO Users (Username, PasswordHash, RealName, IsActive, IsSystem)
VALUES ('root', '<HASH>', '系统管理员', 1, 1)
```

**IsSystem 字段说明**:
- `IsSystem = 1`: 系统内置用户，不可删除、不可禁用、前端不显示
- `IsSystem = 0`: 普通用户

---

### 2. 角色表 (Roles)
```sql
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    RoleCode NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200),
    IsSystem BIT NOT NULL DEFAULT 0,     -- 系统内置角色
    SortOrder INT DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedBy INT,
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedBy INT,
    UpdatedTime DATETIME
)

-- 内置角色
INSERT INTO Roles (RoleName, RoleCode, Description, IsSystem, SortOrder) VALUES
('超级管理员', 'SUPER_ADMIN', '拥有所有权限', 1, 1),
('管理员', 'ADMIN', '系统管理权限', 1, 2),
('普通用户', 'USER', '普通用户权限', 1, 3),
('访客', 'GUEST', '只读权限', 1, 4)
```

---

### 3. 用户角色关联表 (UserRoles)
```sql
CREATE TABLE UserRoles (
    UserRoleId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    CreatedBy INT,
    CreatedTime DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId),
    CONSTRAINT UQ_UserRoles_UserRole UNIQUE (UserId, RoleId)
)

-- root用户默认为超级管理员
INSERT INTO UserRoles (UserId, RoleId)
SELECT u.UserId, r.RoleId
FROM Users u, Roles r
WHERE u.Username = 'root' AND r.RoleCode = 'SUPER_ADMIN'
```

---

### 4. 权限表 (Permissions)
```sql
CREATE TABLE Permissions (
    PermissionId INT IDENTITY(1,1) PRIMARY KEY,
    PermissionCode NVARCHAR(100) NOT NULL UNIQUE,
    PermissionName NVARCHAR(100) NOT NULL,
    Module NVARCHAR(50) NOT NULL,           -- 模块名称
    Action NVARCHAR(50) NOT NULL,           -- 操作: View, Create, Edit, Delete, Export
    Description NVARCHAR(200),
    ParentId INT,                            -- 父权限ID
    SortOrder INT DEFAULT 0,
    IsSystem BIT NOT NULL DEFAULT 0,
    CreatedTime DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Permissions_Parent FOREIGN KEY (ParentId) REFERENCES Permissions(PermissionId)
)

-- 权限数据示例
INSERT INTO Permissions (PermissionCode, PermissionName, Module, Action, ParentId, SortOrder, IsSystem) VALUES
-- 首页
('HOME_VIEW', '首页查看', 'Home', 'View', NULL, 1, 1),

-- 报表设计
('REPORT_DESIGN_VIEW', '报表设计查看', 'ReportDesign', 'View', NULL, 10, 1),
('REPORT_DESIGN_CREATE', '报表创建', 'ReportDesign', 'Create', NULL, 11, 1),
('REPORT_DESIGN_EDIT', '报表编辑', 'ReportDesign', 'Edit', NULL, 12, 1),
('REPORT_DESIGN_DELETE', '报表删除', 'ReportDesign', 'Delete', NULL, 13, 1),
('REPORT_DESIGN_PREVIEW', '报表预览', 'ReportDesign', 'Preview', NULL, 14, 1),

-- 报表查询
('REPORT_QUERY_VIEW', '报表查询查看', 'ReportQuery', 'View', NULL, 20, 1),
('REPORT_QUERY_EXPORT', '报表导出', 'ReportQuery', 'Export', NULL, 21, 1),

-- 许可管理
('LICENSE_VIEW', '许可管理查看', 'License', 'View', NULL, 30, 1),
('LICENSE_ACTIVATE', '许可激活', 'License', 'Activate', NULL, 31, 1),

-- 系统管理 - 数据源
('DATASOURCE_VIEW', '数据源查看', 'DataSource', 'View', NULL, 40, 1),
('DATASOURCE_CREATE', '数据源创建', 'DataSource', 'Create', NULL, 41, 1),
('DATASOURCE_EDIT', '数据源编辑', 'DataSource', 'Edit', NULL, 42, 1),
('DATASOURCE_DELETE', '数据源删除', 'DataSource', 'Delete', NULL, 43, 1),
('DATASOURCE_TEST', '数据源测试', 'DataSource', 'Test', NULL, 44, 1),

-- 系统管理 - 权限组
('ROLE_VIEW', '角色查看', 'Role', 'View', NULL, 50, 1),
('ROLE_CREATE', '角色创建', 'Role', 'Create', NULL, 51, 1),
('ROLE_EDIT', '角色编辑', 'Role', 'Edit', NULL, 52, 1),
('ROLE_DELETE', '角色删除', 'Role', 'Delete', NULL, 53, 1),
('ROLE_ASSIGN_PERMISSION', '分配权限', 'Role', 'Assign', NULL, 54, 1),

-- 系统管理 - 用户
('USER_VIEW', '用户查看', 'User', 'View', NULL, 60, 1),
('USER_CREATE', '用户创建', 'User', 'Create', NULL, 61, 1),
('USER_EDIT', '用户编辑', 'User', 'Edit', NULL, 62, 1),
('USER_DELETE', '用户删除', 'User', 'Delete', NULL, 63, 1),
('USER_ASSIGN_ROLE', '分配角色', 'User', 'Assign', NULL, 64, 1),
('USER_RESET_PASSWORD', '重置密码', 'User', 'Reset', NULL, 65, 1),

-- 系统管理 - 日志
('LOG_VIEW', '日志查看', 'Log', 'View', NULL, 70, 1),
('LOG_EXPORT', '日志导出', 'Log', 'Export', NULL, 71, 1),
('LOG_DELETE', '日志删除', 'Log', 'Delete', NULL, 72, 1),

-- 系统管理 - 备份
('BACKUP_VIEW', '备份查看', 'Backup', 'View', NULL, 80, 1),
('BACKUP_CREATE', '创建备份', 'Backup', 'Create', NULL, 81, 1),
('BACKUP_RESTORE', '恢复备份', 'Backup', 'Restore', NULL, 82, 1),
('BACKUP_DELETE', '删除备份', 'Backup', 'Delete', NULL, 83, 1),
('BACKUP_CONFIG', '备份配置', 'Backup', 'Config', NULL, 84, 1)
```

---

### 5. 角色权限关联表 (RolePermissions)
```sql
CREATE TABLE RolePermissions (
    RolePermissionId INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,
    CreatedBy INT,
    CreatedTime DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId),
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES Permissions(PermissionId),
    CONSTRAINT UQ_RolePermissions_RolePermission UNIQUE (RoleId, PermissionId)
)

-- 超级管理员拥有所有权限
INSERT INTO RolePermissions (RoleId, PermissionId, CreatedBy)
SELECT
    (SELECT RoleId FROM Roles WHERE RoleCode = 'SUPER_ADMIN'),
    PermissionId,
    (SELECT UserId FROM Users WHERE Username = 'root')
FROM Permissions
```

---

### 6. 数据源表 (DataSources)
```sql
CREATE TABLE DataSources (
    DataSourceId INT IDENTITY(1,1) PRIMARY KEY,
    DataSourceName NVARCHAR(100) NOT NULL,
    DataSourceCode NVARCHAR(50) NOT NULL UNIQUE,
    DbType NVARCHAR(20) NOT NULL,              -- SQL Server, MySQL, Oracle, PostgreSQL等
    ServerAddress NVARCHAR(200) NOT NULL,
    Port INT,
    DatabaseName NVARCHAR(100),
    Username NVARCHAR(100),
    -- 密码加密存储
    Password NVARCHAR(500),                    -- AES加密
    IsIntegratedSecurity BIT DEFAULT 0,        -- Windows集成认证
    ConnectionTimeout INT DEFAULT 30,
    CommandTimeout INT DEFAULT 60,
    IsDefault BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    TestSql NVARCHAR(500),                     -- 测试连接SQL
    Remark NVARCHAR(500),
    CreatedBy INT,
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedBy INT,
    UpdatedTime DATETIME,
    LastTestTime DATETIME,
    LastTestResult BIT,
    LastTestMessage NVARCHAR(500)
)
```

---

### 7. 报表定义表 (Reports)
```sql
CREATE TABLE Reports (
    ReportId INT IDENTITY(1,1) PRIMARY KEY,
    ReportName NVARCHAR(100) NOT NULL,
    ReportCode NVARCHAR(50) NOT NULL UNIQUE,
    ReportCategory NVARCHAR(50),               -- 报表分类
    DataSourceId INT NOT NULL,
    SqlStatement NVARCHAR(MAX) NOT NULL,       -- SQL查询语句
    Description NVARCHAR(500),
    IsPaged BIT DEFAULT 1,                     -- 是否分页
    PageSize INT DEFAULT 50,
    CacheDuration INT DEFAULT 0,               -- 缓存时长(秒)，0表示不缓存
    IsEnabled BIT DEFAULT 1,
    IsSystem BIT DEFAULT 0,                    -- 系统报表
    ViewCount INT DEFAULT 0,                   -- 查看次数
    LastViewTime DATETIME,
    Remark NVARCHAR(500),
    CreatedBy INT,
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedBy INT,
    UpdatedTime DATETIME,
    CONSTRAINT FK_Reports_DataSources FOREIGN KEY (DataSourceId) REFERENCES DataSources(DataSourceId),
    CONSTRAINT CK_Reports_Sql CHECK (SqlStatement LIKE 'SELECT%')
)
```

---

### 8. 报表字段配置表 (ReportFields)
```sql
CREATE TABLE ReportFields (
    FieldId INT IDENTITY(1,1) PRIMARY KEY,
    ReportId INT NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,          -- 字段名称(数据库字段)
    DisplayName NVARCHAR(100) NOT NULL,        -- 显示名称
    DataType NVARCHAR(20) NOT NULL,            -- String, Int, Decimal, DateTime, Bool
    Width INT DEFAULT 100,
    IsVisible BIT DEFAULT 1,                   -- 是否显示
    IsSortable BIT DEFAULT 1,                  -- 是否可排序
    IsFilterable BIT DEFAULT 0,                -- 是否可筛选
    IsGroupable BIT DEFAULT 0,                 -- 是否可分组
    SortOrder INT DEFAULT 0,
    Align NVARCHAR(10) DEFAULT 'left',         -- left, center, right
    FormatString NVARCHAR(50),                 -- 格式化字符串，如 {0:N2}
    AggregateFunction NVARCHAR(20),            -- SUM, AVG, COUNT, MAX, MIN
    CssClass NVARCHAR(100),                    -- CSS类名
    Remark NVARCHAR(200),
    CreatedTime DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_ReportFields_Reports FOREIGN KEY (ReportId) REFERENCES Reports(ReportId)
)
```

---

### 9. 报表参数配置表 (ReportParameters)
```sql
CREATE TABLE ReportParameters (
    ParameterId INT IDENTITY(1,1) PRIMARY KEY,
    ReportId INT NOT NULL,
    ParameterName NVARCHAR(50) NOT NULL,       -- 参数名
    DisplayName NVARCHAR(100) NOT NULL,        -- 显示名称
    DataType NVARCHAR(20) NOT NULL,            -- String, Int, DateTime, Date, Bool
    InputType NVARCHAR(20) NOT NULL,           -- Text, Dropdown, DatePicker, DateRange, MultiSelect
    DefaultValue NVARCHAR(500),
    IsRequired BIT DEFAULT 1,
    SortOrder INT DEFAULT 0,
    -- Dropdown选项配置 (JSON格式)
    -- [{"value":"1","text":"选项1"},{"value":"2","text":"选项2"}]
    Options NVARCHAR(MAX),
    -- SQL查询选项配置 (用于动态下拉框)
    -- {"dataSourceId":1,"sql":"SELECT Id AS value, Name AS text FROM Table","hasAllOption":true}
    QueryOptions NVARCHAR(MAX),
    Remark NVARCHAR(200),
    CreatedTime DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_ReportParameters_Reports FOREIGN KEY (ReportId) REFERENCES Reports(ReportId)
)
```

---

### 10. 操作日志表 (OperationLogs)
```sql
CREATE TABLE OperationLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    Username NVARCHAR(50),
    Module NVARCHAR(50) NOT NULL,              -- 模块名称
    Action NVARCHAR(50) NOT NULL,              -- 操作类型
    ActionType NVARCHAR(20),                   -- Create, Read, Update, Delete, Login, Logout
    Description NVARCHAR(500),
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    RequestUrl NVARCHAR(500),
    RequestMethod NVARCHAR(10),
    RequestData NVARCHAR(MAX),                 -- 请求数据(JSON)
    ResponseData NVARCHAR(MAX),                -- 响应数据(JSON)
    Duration INT,                              -- 执行时长(毫秒)
    IsSuccess BIT DEFAULT 1,
    ErrorMessage NVARCHAR(MAX),
    CreatedTime DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_OperationLogs_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
)

-- 索引
CREATE INDEX IX_OperationLogs_UserId ON OperationLogs(UserId)
CREATE INDEX IX_OperationLogs_Module ON OperationLogs(Module)
CREATE INDEX IX_OperationLogs_CreatedTime ON OperationLogs(CreatedTime)
```

---

### 11. 系统配置表 (SystemConfigs)
```sql
CREATE TABLE SystemConfigs (
    ConfigId INT IDENTITY(1,1) PRIMARY KEY,
    ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
    ConfigValue NVARCHAR(MAX),
    ConfigType NVARCHAR(20) NOT NULL,          -- String, Int, Bool, Json
    Description NVARCHAR(200),
    IsSystem BIT DEFAULT 0,                    -- 系统配置，不可删除
    SortOrder INT DEFAULT 0,
    CreatedBy INT,
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedBy INT,
    UpdatedTime DATETIME
)

-- 系统配置示例
INSERT INTO SystemConfigs (ConfigKey, ConfigValue, ConfigType, Description, IsSystem) VALUES
('System.Title', 'DataForgeStudio V4', 'String', '系统标题', 1),
('System.Logo', '', 'String', '系统Logo', 0),
('System.SessionTimeout', '30', 'Int', '会话超时时间(分钟)', 1),
('System.MaxUploadSize', '10', 'Int', '最大上传大小(MB)', 1),
('System.EnableAuditLog', 'true', 'Bool', '启用审计日志', 1),
('System.Password.MinLength', '6', 'Int', '密码最小长度', 1),
('System.Password.RequireUppercase', 'true', 'Bool', '密码需要大写字母', 1),
('System.Password.RequireLowercase', 'true', 'Bool', '密码需要小写字母', 1),
('System.Password.RequireDigit', 'true', 'Bool', '密码需要数字', 1),
('System.Password.RequireSpecialChar', 'false', 'Bool', '密码需要特殊字符', 1),
('Backup.AutoBackupEnabled', 'false', 'Bool', '启用自动备份', 0),
('Backup.AutoBackupTime', '02:00', 'String', '自动备份时间', 0),
('Backup.AutoBackupRetentionDays', '30', 'Int', '备份保留天数', 0),
('Backup.BackupPath', 'C:\Backup\DataForgeStudio', 'String', '备份路径', 0)
```

---

### 12. 备份记录表 (BackupRecords)
```sql
CREATE TABLE BackupRecords (
    BackupId INT IDENTITY(1,1) PRIMARY KEY,
    BackupName NVARCHAR(200) NOT NULL,
    BackupType NVARCHAR(20) NOT NULL,          -- Manual, Auto
    BackupPath NVARCHAR(500) NOT NULL,
    DatabaseName NVARCHAR(100),
    FileSize BIGINT,                           -- 文件大小(字节)
    BackupTime DATETIME DEFAULT GETDATE(),
    IsSuccess BIT DEFAULT 1,
    ErrorMessage NVARCHAR(MAX),
    CreatedBy INT,
    CreatedTime DATETIME DEFAULT GETDATE()
)
```

---

### 13. 许可证表 (Licenses)
```sql
CREATE TABLE Licenses (
    LicenseId INT IDENTITY(1,1) PRIMARY KEY,
    LicenseKey NVARCHAR(500) NOT NULL UNIQUE,  -- RSA加密的许可证密钥
    CompanyName NVARCHAR(200),
    ContactPerson NVARCHAR(50),
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    MaxUsers INT,                              -- 最大用户数
    MaxReports INT,                            -- 最大报表数
    MaxDataSources INT,                        -- 最大数据源数
    ExpiryDate DATETIME,
    Features NVARCHAR(MAX),                    -- 功能列表(JSON)
    IsActive BIT DEFAULT 1,
    ActivatedTime DATETIME,
    ActivatedIP NVARCHAR(50),
    MachineCode NVARCHAR(200),                 -- 机器码
    Remark NVARCHAR(500),
    CreatedBy INT,
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedBy INT,
    UpdatedTime DATETIME
)
```

---

### 14. 登录日志表 (LoginLogs)
```sql
CREATE TABLE LoginLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    Username NVARCHAR(50),
    LoginTime DATETIME DEFAULT GETDATE(),
    LogoutTime DATETIME,
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    LoginStatus NVARCHAR(20),                  -- Success, Failed, Locked
    FailureReason NVARCHAR(200),
    SessionId NVARCHAR(100),
    CONSTRAINT FK_LoginLogs_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
)

CREATE INDEX IX_LoginLogs_UserId ON LoginLogs(UserId)
CREATE INDEX IX_LoginLogs_LoginTime ON LoginLogs(LoginTime)
```

---

## 视图定义

### 1. 用户权限视图 (v_UserPermissions)
```sql
CREATE VIEW v_UserPermissions AS
SELECT DISTINCT
    u.UserId,
    u.Username,
    p.PermissionCode,
    p.PermissionName,
    p.Module,
    p.Action
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN RolePermissions rp ON ur.RoleId = rp.RoleId
INNER JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE u.IsActive = 1
```

### 2. 报表完整信息视图 (v_ReportDetails)
```sql
CREATE VIEW v_ReportDetails AS
SELECT
    r.ReportId,
    r.ReportName,
    r.ReportCode,
    r.ReportCategory,
    r.SqlStatement,
    d.DataSourceId,
    d.DataSourceName,
    d.DbType,
    r.IsEnabled,
    (SELECT COUNT(*) FROM ReportFields WHERE ReportId = r.ReportId) AS FieldCount,
    (SELECT COUNT(*) FROM ReportParameters WHERE ReportId = r.ReportId) AS ParameterCount,
    r.CreatedTime,
    r.UpdatedTime
FROM Reports r
INNER JOIN DataSources d ON r.DataSourceId = d.DataSourceId
```

---

## 存储过程

### 1. 用户登录验证
```sql
CREATE PROCEDURE sp_UserLogin
    @Username NVARCHAR(50),
    @Password NVARCHAR(100),
    @IpAddress NVARCHAR(50),
    @UserAgent NVARCHAR(500),
    @UserId INT OUTPUT,
    @Result INT OUTPUT,        -- 0:成功, 1:用户不存在, 2:密码错误, 3:用户被禁用, 4:账户被锁定
    @Message NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PasswordHash NVARCHAR(256);
    DECLARE @IsActive BIT;
    DECLARE @IsLocked BIT;
    DECLARE @PasswordFailCount INT;

    -- 检查用户是否存在
    SELECT @UserId = UserId, @PasswordHash = PasswordHash,
           @IsActive = IsActive, @IsLocked = IsLocked,
           @PasswordFailCount = PasswordFailCount
    FROM Users
    WHERE Username = @Username;

    IF @UserId IS NULL
    BEGIN
        SET @Result = 1;
        SET @Message = '用户不存在';
        RETURN;
    END

    IF @IsActive = 0
    BEGIN
        SET @Result = 3;
        SET @Message = '用户已被禁用';
        RETURN;
    END

    IF @IsLocked = 1
    BEGIN
        SET @Result = 4;
        SET @Message = '账户已被锁定，请联系管理员';
        RETURN;
    END

    -- 验证密码
    IF @PasswordHash = dbo.fn_GenerateHash(@Password)  -- 假设有哈希函数
    BEGIN
        -- 登录成功
        SET @Result = 0;
        SET @Message = '登录成功';

        -- 更新登录信息
        UPDATE Users
        SET LastLoginTime = GETDATE(),
            LastLoginIP = @IpAddress,
            PasswordFailCount = 0
        WHERE UserId = @UserId;

        -- 记录登录日志
        INSERT INTO LoginLogs (UserId, Username, LoginTime, IpAddress, UserAgent, LoginStatus)
        VALUES (@UserId, @Username, GETDATE(), @IpAddress, @UserAgent, 'Success');
    END
    ELSE
    BEGIN
        -- 密码错误
        SET @Result = 2;
        SET @Message = '密码错误';

        -- 增加失败次数
        UPDATE Users
        SET PasswordFailCount = PasswordFailCount + 1,
            IsLocked = CASE WHEN PasswordFailCount + 1 >= 5 THEN 1 ELSE 0 END
        WHERE UserId = @UserId;

        -- 记录失败日志
        INSERT INTO LoginLogs (UserId, Username, LoginTime, IpAddress, UserAgent, LoginStatus, FailureReason)
        VALUES (@UserId, @Username, GETDATE(), @IpAddress, @UserAgent, 'Failed', '密码错误');
    END
END
```

### 2. 获取用户权限
```sql
CREATE PROCEDURE sp_GetUserPermissions
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        p.PermissionCode,
        p.PermissionName,
        p.Module,
        p.Action
    FROM Permissions p
    INNER JOIN RolePermissions rp ON p.PermissionId = rp.PermissionId
    INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
    WHERE ur.UserId = @UserId
    ORDER BY p.Module, p.SortOrder;
END
```

### 3. 创建备份
```sql
CREATE PROCEDURE sp_CreateBackup
    @BackupName NVARCHAR(200),
    @BackupPath NVARCHAR(500),
    @BackupType NVARCHAR(20),
    @CreatedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @DatabaseName NVARCHAR(100);
    DECLARE @Sql NVARCHAR(MAX);

    -- 获取当前数据库名
    SET @DatabaseName = DB_NAME();

    -- 构建备份命令
    SET @Sql = 'BACKUP DATABASE [' + @DatabaseName + '] TO DISK = ''' + @BackupPath + ''' WITH FORMAT, NAME = ''' + @BackupName + '''';

    BEGIN TRY
        EXEC sp_executesql @Sql;

        -- 记录成功
        INSERT INTO BackupRecords (BackupName, BackupType, BackupPath, DatabaseName, IsSuccess, CreatedBy)
        VALUES (@BackupName, @BackupType, @BackupPath, @DatabaseName, 1, @CreatedBy);
    END TRY
    BEGIN CATCH
        -- 记录失败
        INSERT INTO BackupRecords (BackupName, BackupType, BackupPath, DatabaseName, IsSuccess, ErrorMessage, CreatedBy)
        VALUES (@BackupName, @BackupType, @BackupPath, @DatabaseName, 0, ERROR_MESSAGE(), @CreatedBy);
    END CATCH
END
```

---

## 函数

### 密码哈希函数（需要根据实际加密方案实现）
```sql
CREATE FUNCTION fn_GenerateHash(@Input NVARCHAR(MAX))
RETURNS NVARCHAR(256)
AS
BEGIN
    -- 这里应该实现实际的哈希算法
    -- 可以使用 SQL Server 的 HASHBYTES 或调用外部程序
    RETURN HASHBYTES('SHA2_256', @Input + 'DataForgeStudio2025');
END
```

---

## 重要说明

### Root 用户处理

1. **数据库层面**:
   - `IsSystem = 1` 标记系统内置用户
   - 约束确保只有 root 用户可以设置为 IsSystem=1

2. **后端 API 层面**:
   - 查询用户列表时自动过滤 `WHERE IsSystem = 0`
   - 删除/更新用户时检查 `IsSystem` 标记
   - 创建用户时强制 `IsSystem = 0`

3. **前端层面**:
   - 用户列表不显示 IsSystem=1 的用户
   - 分配角色时隐藏 root 用户

### 推荐查询语句

```sql
-- 获取普通用户列表（前端显示）
SELECT * FROM Users WHERE IsSystem = 0

-- 获取所有用户（包含系统用户，仅管理员）
SELECT * FROM Users WHERE IsSystem = 0 OR (IsSystem = 1 AND EXISTS(SELECT 1 FROM UserRoles ur INNER JOIN Roles r ON ur.RoleId = r.RoleId WHERE ur.UserId = Users.UserId AND r.RoleCode = 'SUPER_ADMIN'))
```

### SQL Server 2005 兼容性说明

1. 不使用以下特性:
   - `SEQUENCE` (SQL Server 2012+)
   - `OFFSET/FETCH` (SQL Server 2012+)
   - `IIF` (SQL Server 2012+)
   - `CONCAT` (SQL Server 2012+)
   - `STRING_SPLIT` (SQL Server 2016+)

2. 使用 `IDENTITY` 代替 `SEQUENCE`
3. 使用 `ROW_NUMBER()` 实现分页

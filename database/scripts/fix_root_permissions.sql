-- 修复 root 用户权限问题
-- 确保 root 用户拥有所有权限

USE DataForgeStudio_V4;

-- 检查 root 用户是否存在
DECLARE @RootUserId INT;
SELECT @RootUserId = UserId FROM Users WHERE Username = 'root';

IF @RootUserId IS NULL
    PRINT '警告: root 用户不存在'
ELSE
    PRINT '找到 root 用户, UserId: ' + CAST(@RootUserId AS VARCHAR)

-- 检查超级管理员角色是否存在
DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = RoleId FROM Roles WHERE RoleName = '超级管理员';

IF @AdminRoleId IS NULL
    PRINT '警告: 超级管理员角色不存在'
ELSE
    PRINT '找到超级管理员角色, RoleId: ' + CAST(@AdminRoleId AS VARCHAR)

-- 如果 root 用户和超级管理员角色都存在，确保关联关系
IF @RootUserId IS NOT NULL AND @AdminRoleId IS NOT NULL
BEGIN
    -- 检查是否已存在关联
    IF NOT EXISTS (
        SELECT 1 FROM UserRoles
        WHERE UserId = @RootUserId AND RoleId = @AdminRoleId
    )
    BEGIN
        -- 创建关联
        INSERT INTO UserRoles (UserId, RoleId)
        VALUES (@RootUserId, @AdminRoleId)

        PRINT '✅ 已为 root 用户分配超级管理员角色'
    END
    ELSE
    BEGIN
        PRINT '✅ root 用户已拥有超级管理员角色'
    END
END

-- 验证 root 用户的权限
PRINT ''
PRINT '=== root 用户权限验证 ==='

-- 显示 root 用户的角色
SELECT u.Username, r.RoleName
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.Username = 'root'

-- 显示 root 用户通过角色获得的权限数量
DECLARE @PermissionCount INT;
SELECT @PermissionCount = COUNT(DISTINCT rp.PermissionId)
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN RolePermissions rp ON ur.RoleId = rp.RoleId
WHERE u.Username = 'root'

PRINT 'root 用户拥有的权限数量: ' + CAST(@PermissionCount AS VARCHAR)

-- 列出所有权限类别
SELECT
    p.Module,
    COUNT(*) as PermissionCount
FROM Permissions p
INNER JOIN RolePermissions rp ON p.PermissionId = rp.PermissionId
INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
WHERE ur.UserId = @RootUserId
GROUP BY p.Module
ORDER BY p.Module

-- 检查 role:view 权限
IF EXISTS (
    SELECT 1 FROM Permissions p
    INNER JOIN RolePermissions rp ON p.PermissionId = rp.PermissionId
    INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
    WHERE ur.UserId = @RootUserId AND p.PermissionCode = 'role:view'
)
    PRINT '✅ root 用户拥有 role:view 权限'
ELSE
    PRINT '❌ root 用户缺少 role:view 权限'

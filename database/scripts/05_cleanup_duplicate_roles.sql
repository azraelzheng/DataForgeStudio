-- 清理重复的角色数据
USE DataForgeStudio_V4;
GO

PRINT '开始清理重复的角色数据...';

-- 显示当前所有角色
SELECT '=== 当前角色列表 ===' as '';
SELECT RoleId, RoleName, RoleCode, IsSystem, CreatedTime
FROM Roles
ORDER BY CreatedTime;
GO

-- 删除用户角色关联（保留 root 用户）
DELETE FROM [dbo].[UserRoles]
WHERE [RoleId] IN (
    SELECT [RoleId] FROM [dbo].[Roles]
    WHERE [RoleCode] IN ('SUPER_ADMIN', 'ADMIN', 'USER', 'GUEST')
    AND [RoleId] NOT IN (
        -- 保留 root 用户的关联
        SELECT [ur].[RoleId] FROM [dbo].[UserRoles] ur
        INNER JOIN [dbo].[Users] u ON ur.[UserId] = u.[UserId]
        WHERE u.[Username] = 'root'
    )
);
GO

-- 删除旧的角色（2026-02-03 创建的）
DELETE FROM [dbo].[Roles]
WHERE [RoleCode] IN ('SUPER_ADMIN', 'ADMIN', 'USER', 'GUEST');
GO

PRINT '旧角色已删除';
PRINT '';

-- 显示清理后的角色列表
SELECT '=== 清理后的角色列表 ===' as '';
SELECT RoleId, RoleName, RoleCode, IsSystem, CreatedTime
FROM Roles
ORDER BY CreatedTime;
GO

PRINT '';
PRINT '清理完成！';
GO

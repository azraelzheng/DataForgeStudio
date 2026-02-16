-- 查询所有用户（不包括系统用户）
SELECT UserId, Username, RealName, IsActive, IsLocked, IsSystem, MustChangePassword
FROM Users
ORDER BY UserId;

-- 查询用户角色
SELECT u.Username, r.RoleName
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId
ORDER BY u.Username, r.RoleName;

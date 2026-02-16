using Microsoft.Data.SqlClient;
using System;
using static BCrypt.Net.BCrypt;

namespace ResetPassword;

class Program
{
    static void Main(string[] args)
    {
        // 数据库连接字符串 - 使用 Windows 身份验证
        var connectionString = "Data Source=localhost;Initial Catalog=DataForgeStudio_V4;Integrated Security=True;TrustServerCertificate=True;";

        // 新密码（符合强密码策略）
        var newPassword = "Admin@123";

        // 使用 BCrypt 哈希密码
        var passwordHash = HashPassword(newPassword);

        Console.WriteLine("=== DataForgeStudio V4 密码重置工具 ===");
        Console.WriteLine();

        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Console.WriteLine("✅ 数据库连接成功");

                // 更新 root 用户密码
                var sql = "UPDATE Users SET PasswordHash = @PasswordHash, MustChangePassword = 0 WHERE Username = 'root'";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    var rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"✅ root 用户密码重置成功！");
                        Console.WriteLine($"   用户名: root");
                        Console.WriteLine($"   新密码: {newPassword}");
                    }
                    else
                    {
                        Console.WriteLine("❌ 未找到 root 用户，尝试创建...");

                        // 如果 root 用户不存在，创建它
                        var createSql = @"
                            INSERT INTO Users (Username, PasswordHash, RealName, Email, IsActive, IsLocked, IsSystem, MustChangePassword, CreatedTime)
                            VALUES ('root', @PasswordHash, '系统管理员', 'root@dataforge.com', 1, 0, 1, 0, GETDATE())";

                        using (var createCommand = new SqlCommand(createSql, connection))
                        {
                            createCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                            var created = createCommand.ExecuteNonQuery();

                            if (created > 0)
                            {
                                Console.WriteLine($"✅ root 用户创建成功！");
                                Console.WriteLine($"   用户名: root");
                                Console.WriteLine($"   密码: {newPassword}");
                            }
                        }
                    }
                }

                // 显示所有用户
                Console.WriteLine();
                Console.WriteLine("=== 当前用户列表 ===");
                var listSql = "SELECT Username, RealName, IsActive, IsSystem FROM Users ORDER BY Username";

                using (var listCommand = new SqlCommand(listSql, connection))
                using (var reader = listCommand.ExecuteReader())
                {
                    Console.WriteLine("用户名\t\t真实姓名\t\t状态\t\t系统用户");
                    Console.WriteLine("----------------------------------------------------------------");

                    while (reader.Read())
                    {
                        var username = reader.GetString(0);
                        var realName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        var isActive = reader.GetBoolean(2) ? "启用" : "禁用";
                        var isSystem = reader.GetBoolean(3) ? "是" : "否";

                        Console.WriteLine($"{username}\t\t{realName}\t\t{isActive}\t\t{isSystem}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("请检查：");
                Console.WriteLine("1. SQL Server 是否正在运行");
                Console.WriteLine("2. 连接字符串是否正确");
                Console.WriteLine("3. 数据库 DataForgeStudio_V4 是否存在");
            }
        }

        Console.WriteLine();
        // Console.WriteLine("按任意键退出...");
        // Console.ReadKey();
    }
}

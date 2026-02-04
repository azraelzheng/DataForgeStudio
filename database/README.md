# Database Setup Guide

## Prerequisites

- SQL Server 2005 or later
- SQL Server Management Studio (SSMS) or sqlcmd command-line tool

## Setup Steps

### 1. Create Database and Tables

Run the database initialization script:

**Using SSMS:**
1. Open SSMS and connect to your SQL Server instance
2. Open the file `database/scripts/01_init_database.sql`
3. Execute the script (F5 or press the Execute button)

**Using sqlcmd:**
```bash
sqlcmd -S localhost -E -i database/scripts/01_init_database.sql
```

### 2. Insert Seed Data

Run the seed data script:

**Using SSMS:**
1. Open the file `database/scripts/02_seed_data.sql`
2. Execute the script (F5 or press the Execute button)

**Using sqlcmd:**
```bash
sqlcmd -S localhost -E -i database/scripts/02_seed_data.sql
```

### 3. Generate BCrypt Password Hashes (Optional)

If you want to create additional users with custom passwords:

1. Navigate to the tools directory:
```bash
cd tools/PasswordHashGenerator
```

2. Build the utility:
```bash
dotnet build
```

3. Run the utility:
```bash
dotnet run
```

4. Enter a password when prompted, and the BCrypt hash will be generated

5. Copy the generated hash into a SQL INSERT statement:
```sql
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [IsActive], [CreatedTime])
VALUES ('yourusername', 'PASTE_HASH_HERE', 'Your Real Name', 1, GETUTCDATE());
```

### 4. Configure Connection String

The backend connection string is in `backend/src/DataForgeStudio.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=DataForgeStudio_V4;Integrated Security=True;TrustServerCertificate=True;",
    "MasterConnection": "Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

**For SQL Server Authentication (username/password):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=DataForgeStudio_V4;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

**For Remote SQL Server:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=your-server-name;Initial Catalog=DataForgeStudio_V4;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

### 5. Run EF Migrations (Optional)

If you want to use Entity Framework Core migrations:

```bash
cd backend/src/DataForgeStudio.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Default Users

After running the seed script, the following users will be created:

| Username | Password | Role | Description |
|----------|----------|------|-------------|
| root | admin123 | Super Admin | Hidden from UI, system maintenance |
| admin | Test123! | Admin | Administrator |
| user01 | Test123! | User | Regular user |
| guest | Test123! | Guest | Guest user (read-only) |

**IMPORTANT:** The `root` user is hidden from the frontend user management (IsSystem = 1). It can only be modified directly in the database.

## Sample Reports

The following sample reports are created:

1. **User List** (RPT_USER_LIST)
   - Category: System
   - Shows all non-system users
   - Columns: User ID, Username, Real Name, Email, Phone, Status, Created Time

2. **Role List** (RPT_ROLE_LIST)
   - Category: System
   - Shows all roles with user count
   - Columns: Role ID, Role Name, Role Code, Description, User Count, Status, Created Time

3. **Login Statistics** (RPT_LOGIN_STATS)
   - Category: Statistics
   - Shows daily login statistics
   - Parameters: Start Date, End Date
   - Columns: Login Date, Login Count, Unique Users

## Verify Installation

To verify the database is set up correctly:

```sql
USE DataForgeStudio_V4;
GO

-- Check users
SELECT Username, RealName, IsActive, IsSystem FROM Users;

-- Check roles
SELECT RoleName, RoleCode, IsSystem FROM Roles;

-- Check user roles
SELECT u.Username, r.RoleName
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId
ORDER BY u.Username, r.RoleName;

-- Check reports
SELECT ReportName, ReportCategory, IsEnabled FROM Reports;

-- Check data sources
SELECT DataSourceName, DbType, IsActive FROM DataSources;
```

## Troubleshooting

### Connection Issues

If you get connection errors:

1. Verify SQL Server is running:
   ```bash
   sc query MSSQLSERVER
   ```

2. Check SQL Server allows remote connections:
   - Open SQL Server Configuration Manager
   - Go to SQL Server Network Configuration
   - Enable TCP/IP protocol
   - Restart SQL Server

3. For SQL Server Express, use `.\SQLEXPRESS` as the server name:
   ```json
   "DefaultConnection": "Data Source=.\\SQLEXPRESS;Initial Catalog=DataForgeStudio_V4;Integrated Security=True;TrustServerCertificate=True;"
   ```

### Permission Issues

If you get permission errors:

1. Ensure your Windows user has access to the database
2. Or use SQL Server authentication with a valid username/password
3. Grant necessary permissions:
   ```sql
   USE DataForgeStudio_V4;
   GO
   -- Add user to database
   CREATE USER [your-username] FOR LOGIN [your-username];

   -- Grant necessary permissions
   ALTER ROLE db_owner ADD MEMBER [your-username];
   ```

### Port Issues

Default SQL Server port is 1433. If using a different port:

```json
"DefaultConnection": "Data Source=localhost,1434;Initial Catalog=DataForgeStudio_V4;Integrated Security=True;TrustServerCertificate=True;"
```

## Backup and Restore

### Backup Database

```sql
BACKUP DATABASE DataForgeStudio_V4
TO DISK = 'C:\Backup\DataForgeStudio_V4_backup.bak'
WITH FORMAT,
MEDIANAME = 'DataForgeStudio_V4_Backup',
NAME = 'Full Backup of DataForgeStudio_V4';
GO
```

### Restore Database

```sql
RESTORE DATABASE DataForgeStudio_V4
FROM DISK = 'C:\Backup\DataForgeStudio_V4_backup.bak'
WITH REPLACE;
GO
```

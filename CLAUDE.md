# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**DataForgeStudio V4** - Web-based report management system with dynamic SQL query designer, multi-datasource support, and license management.

**Tech Stack**: ASP.NET Core 8.0 backend, Vue 3 frontend, SQL Server 2005+

**Language Preference**: Use Chinese for responses and code comments.

---

## Common Commands

### Backend Development
```bash
# Run API (from project root)
dotnet run --project backend/src/DataForgeStudio.Api

# Build backend
dotnet build backend/DataForgeStudio.sln

# Clean and restore
dotnet clean backend/DataForgeStudio.sln && dotnet restore backend/DataForgeStudio.sln

# Run license generator tool
dotnet run --project backend/tools/LicenseGenerator/LicenseGenerator.csproj
```

### Frontend Development
```bash
# Run dev server
cd frontend && npm run dev

# Install dependencies
cd frontend && npm install

# Build for production
cd frontend && npm run build
```

### Database Operations
```sql
-- View users (excludes root/system users)
SELECT * FROM Users WHERE IsSystem = 0;

-- Check user permissions
SELECT u.Username, r.RoleName, p.PermissionName
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId
INNER JOIN RolePermissions rp ON r.RoleId = rp.RoleId
INNER JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE u.Username = 'admin';
```

---

## Architecture

### Backend Layer Structure

```
DataForgeStudio.Api/      # Web API layer (Controllers, Middleware)
DataForgeStudio.Core/     # Business logic (Services, Interfaces, Configuration)
DataForgeStudio.Domain/    # Domain models (Entities, DTOs)
DataForgeStudio.Data/      # Data access (EF Core DbContext, Repositories)
DataForgeStudio.Shared/    # Shared utilities (Encryption, Exceptions, Constants)
```

### Key Architecture Patterns

**Security Options Pattern**: All security configuration (JWT, encryption, license) is centralized in `SecurityOptions.cs` and reads from environment variables or appsettings.json. The system uses development defaults when environment variables are not set (controlled by `useDefaultsForTesting` flag in Program.cs).

**Service Registration**: Services are registered in `Program.cs` using scoped lifetime. All services follow the interface-implementation pattern (e.g., `IUserService` → `UserService`).

**Database Initialization**: On startup, `DbInitializer.InitializeAsync()` creates the root user with a random temporary password and default permissions. `KeyManagementService` ensures RSA key pairs and AES keys exist for license operations.

**Middleware Pipeline Order** (Program.cs line 233-263):
1. HTTPS Redirection
2. Rate Limiting (custom `RateLimitMiddleware`)
3. HSTS (production only)
4. CORS
5. Authentication (JWT)
6. Authorization
7. Operation Logging
8. Controllers

### Root User Protection

The root user is protected at multiple levels:
- **Database**: `CHECK([IsSystem] = 0 OR [Username] = 'root')` constraint
- **API**: All user queries filter with `WHERE IsSystem = 0`
- **Frontend**: Root user is hidden from all user lists
- **Creation**: `IsSystem` is forced to `false` when creating users

### SQL Server 2005 Compatibility

**Avoid using**: `SEQUENCE`, `OFFSET/FETCH`, `IIF`, `CONCAT`, `STRING_SPLIT`

**Use instead**: `IDENTITY`, `ROW_NUMBER()`, `CASE WHEN`, `+` concatenation, custom split function

Example pagination for SQL Server 2005:
```sql
WITH PaginatedData AS (
    SELECT *, ROW_NUMBER() OVER (ORDER BY CreatedTime DESC) AS RowNum
    FROM Users WHERE IsSystem = 0
)
SELECT * FROM PaginatedData WHERE RowNum BETWEEN 1 AND 20;
```

### Security Features

- **Rate Limiting**: Custom middleware in `RateLimitMiddleware.cs` - 5 login attempts/15min, 10 SQL tests/5min
- **SQL Injection Prevention**: `SqlValidationService` blocks dangerous keywords and only allows SELECT statements
- **Password Hashing**: BCrypt with work factor 12
- **Data Source Passwords**: AES encrypted before storage
- **License System**: RSA 2048-bit signing with machine code binding

---

## Important File Locations

### Configuration
- `backend/src/DataForgeStudio.Api/Program.cs` - Service registration, middleware pipeline, security initialization
- `backend/src/DataForgeStudio.Api/appsettings.json` - CORS, JWT, encryption configuration
- `backend/src/DataForgeStudio.Core/Configuration/SecurityOptions.cs` - Environment-based security config

### Key Services
- `backend/src/DataForgeStudio.Core/Services/AuthenticationService.cs` - Login, JWT generation, password changes
- `backend/src/DataForgeStudio.Core/Services/ReportService.cs` - Report CRUD, SQL query execution, Excel export
- `backend/src/DataForgeStudio.Core/Services/DatabaseService.cs` - Multi-database connection support
- `backend/src/DataForgeStudio.Core/Services/SqlValidationService.cs` - SQL injection prevention
- `backend/src/DataForgeStudio.Core/Services/KeyManagementService.cs` - RSA/AES key management for licensing

### Middleware
- `backend/src/DataForgeStudio.Api/Middleware/RateLimitMiddleware.cs` - IP-based rate limiting
- `backend/src/DataForgeStudio.Api/Middleware/OperationLogMiddleware.cs` - Request/response logging

### Utilities
- `backend/src/DataForgeStudio.Shared/Utils/EncryptionHelper.cs` - AES, RSA, BCrypt hashing, machine code generation

### Database
- `backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs` - EF Core context with all DbSets and entity configurations
- `backend/src/DataForgeStudio.Data/Data/DbInitializer.cs` - Database seeding (root user, roles, permissions)

### Frontend
- `frontend/src/api/request.js` - Axios API client with interceptors
- `frontend/src/stores/user.js` - Pinia user store (token, userInfo)
- `frontend/src/stores/license.js` - Pinia license store
- `frontend/src/router/index.js` - Vue Router with permission guards

---

## Environment Variables (Production)

Required for production deployment:
```bash
DFS_JWT_SECRET="64-character-random-secret-key"
DFS_ENCRYPTION_AES_KEY="32-character-aes-key"
DFS_ENCRYPTION_AES_IV="16-character-aes-iv"
DFS_LICENSE_AES_KEY="32-character-license-aes-key"
DFS_LICENSE_AES_IV="16-character-license-aes-iv"
```

---

## API Response Format

All API responses follow this structure:
```json
{
  "success": true,
  "message": "操作成功",
  "data": {...},
  "errorCode": null,
  "timestamp": 1738464000
}
```

---

## Development Notes

### Adding New Features

1. **Domain Layer**: Create entity in `DataForgeStudio.Domain/Entities/`
2. **Data Layer**: Add DbSet to `DataForgeStudioDbContext`, configure relationships
3. **Repository**: Create interface in `Domain/Interfaces/`, implement in `Data/Repositories/`
4. **Service**: Create interface in `Core/Interfaces/`, implement in `Core/Services/`, register in `Program.cs`
5. **Controller**: Create in `Api/Controllers/` with appropriate authorization

### Testing API

- Swagger UI: `https://localhost:5000/swagger` (development only)
- Health check: `GET /health` (anonymous)
- API info: `GET /api` (anonymous)

### Temporary Password on First Run

When the API starts, DbInitializer generates a random 16-character password for the root user and prints it to the console. Look for:
```
⚠️  IMPORTANT: Root User Temporary Password
============================================
Username: root
Password: <random-password>
⚠️  You MUST change this password on first login!
```

---

## Deployment

### IIS Deployment
1. Install ASP.NET Core 8.0 Hosting Bundle
2. Publish: `dotnet publish -c Release -o publish/api`
3. Create IIS website pointing to `publish/api`
4. Configure app pool: "No Managed Code"
5. Set environment variables on server

### Connection String
```
Server=localhost;Database=DataForgeStudio_V4;User Id=sa;Password=your_password;TrustServerCertificate=True;
```

---

## Documentation

- `docs/PROJECT_STATUS.md` - Current project status
- `docs/database-design.md` - Complete database schema with SQL Server 2005 compatibility notes
- `docs/archive/` - Historical documents (completed plans, test reports)

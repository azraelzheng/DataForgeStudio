# DataForgeStudio Web Application Test Report

**Test Date:** 2026-02-26 21:30
**Base URL:** http://127.0.0.1:8089/
**API URL:** http://127.0.0.1:5000/
**Test User:** root

## Build Information

**Build Type:** Obfuscated (Clean Rebuild)
**Build Time:** 2026-02-26 21:26
**Obfuscation:** Only Shared.dll (Core.dll NOT obfuscated to preserve EF Core expression trees)
**Obfuscation Config:** Skip DTOs (properties, fields, methods), Exceptions, Constants, Utils (methods)

## Test Results Summary

### API Tests (Direct)

| Test | Status | Details |
|------|--------|---------|
| API Service Running | PASS | Service DFAppService is RUNNING |
| Login API | PASS | Returns valid JWT token |
| Token Structure | PASS | Contains UserId, Username, Roles, Permissions |
| User Info | PASS | UserId=1, Username=root, Role=超级管理员 |

### Frontend Tests

| Test | Status | Details |
|------|--------|---------|
| Login Page Load | PASS | Page renders correctly |
| Username Input | PASS | Found input[placeholder*="用户"] |
| Password Input | PASS | Found input[type="password"] |
| Login Button | PASS | Found button.login-button |
| Login Submit | PASS | Redirects to /home after login |
| Home Page | PASS | Shows statistics cards and quick actions |
| Statistics Display | PASS | 报表数量:0, 用户数量:1, 数据源数量:0, 运行:1天 |
| Navigation Menu | PASS | All menu items visible |

### Obfuscation Tests

| Assembly | Status | Notes |
|----------|--------|-------|
| DataForgeStudio.Api.dll | PASS | Not obfuscated (entry point) |
| DataForgeStudio.Core.dll | PASS | Not obfuscated (EF Core queries) |
| DataForgeStudio.Shared.dll | PASS | Obfuscated with DTO skipping |
| DataForgeStudio.Data.dll | PASS | Not obfuscated (EF Core entities) |
| DataForgeStudio.Domain.dll | PASS | Not obfuscated (EF Core entities) |

### API Endpoint Tests

| Endpoint | Status | Details |
|----------|--------|---------|
| POST /api/auth/login | PASS | Returns token and user info |
| GET /api/license/stats | PASS | Returns license statistics |
| GET /api/reports | PASS | Returns report list (after login) |

## Issues Resolved

### Issue 1: MissingMethodException for IAuthenticationService.LoginAsync
- **Cause:** Interface methods were being obfuscated
- **Fix:** Added `skipMethods="true"` to `DataForgeStudio.Core.Interfaces.*`

### Issue 2: MissingMethodException for ApiResponse.Ok/Fail
- **Cause:** Static factory methods on DTO classes were being obfuscated
- **Fix:** Added `skipMethods="true"` to `DataForgeStudio.Shared.DTO.*`

### Issue 3: SQL Syntax Error (nvarchar/@__)
- **Cause:** Obfuscation was affecting EF Core expression trees
- **Fix:** Removed Core.dll from obfuscation entirely

## Current Configuration

```xml
<!-- Only Shared.dll is obfuscated -->
<Module file="$(InPath)/DataForgeStudio.Shared/bin/Release/net8.0/DataForgeStudio.Shared.dll">
  <SkipType name="DataForgeStudio.Shared.DTO.*" skipProperties="true" skipFields="true" skipMethods="true" />
  <SkipType name="DataForgeStudio.Shared.Exceptions.*" skipProperties="true" skipFields="true" />
  <SkipType name="DataForgeStudio.Shared.Constants.*" skipProperties="true" skipFields="true" />
  <SkipType name="DataForgeStudio.Shared.Utils.*" skipMethods="true" />
</Module>
```

## Recommendations

1. **Do NOT obfuscate Core.dll** - It contains EF Core LINQ queries that break when obfuscated
2. **Do NOT obfuscate Data.dll or Domain.dll** - EF Core entity mappings require original property names
3. **Only obfuscate Shared.dll** - Utility classes and non-EF code can be safely obfuscated with proper skip rules

## Files Modified

1. `backend/obfuscar.xml` - Updated obfuscation configuration
2. `scripts/build-installer.ps1` - Updated to only copy Shared.dll from obfuscated output

## Test Environment

- **OS:** Windows 11
- **Database:** SQL Server 2016+
- **.NET:** 8.0
- **Node.js:** 10.9.2

## Conclusion

The web application is working correctly with the updated obfuscation configuration. The key finding is that **Core.dll must NOT be obfuscated** due to Entity Framework Core expression trees that rely on original method/property names for SQL generation. Only the Shared project can be safely obfuscated with proper skip rules for DTOs, Exceptions, Constants, and Utility classes.

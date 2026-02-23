# Pre-Release Detection and Evaluation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Verify DataForgeStudio V1.0 is ready for production release with zero critical issues.

**Architecture:** Systematic checklist covering code quality, security, build verification, functional testing, and documentation review.

**Tech Stack:** ASP.NET Core 8.0, Vue 3, SQL Server, Inno Setup

---

## Prerequisites

- Current branch: `fix/cs8618-nullable-warnings`
- Target branch: `master`
- All changes committed and pushed

---

## Task 1: Branch and Code State Verification

**Files:**
- Check: Git status, branch diff

**Step 1: Check current branch status**

Run: `git status --short`
Expected: No uncommitted changes in backend/frontend source files

**Step 2: Compare branch with master**

Run: `git diff master...HEAD --stat`
Expected: Review all changed files, confirm intentional changes

**Step 3: List commits to be merged**

Run: `git log master..HEAD --oneline`
Expected: Clear commit history with descriptive messages

---

## Task 2: Backend Compilation Verification

**Files:**
- Build: `backend/DataForgeStudio.sln`

**Step 1: Clean and restore packages**

Run:
```bash
cd H:/NEW/DataForgeStudio
dotnet clean backend/DataForgeStudio.sln
dotnet restore backend/DataForgeStudio.sln
```
Expected: Successful restore with no errors

**Step 2: Build Release configuration**

Run: `dotnet build backend/DataForgeStudio.sln --configuration Release`
Expected: **0 errors, 0 warnings**

**Step 3: Verify warning count**

Run: `dotnet build backend/DataForgeStudio.sln --configuration Release 2>&1 | grep -c "warning"`
Expected: Output should be `0`

---

## Task 3: Unit Tests Execution

**Files:**
- Test: `backend/tests/DataForgeStudio.Tests/`

**Step 1: Run all unit tests**

Run: `dotnet test backend/DataForgeStudio.sln --configuration Release`
Expected: **All tests pass** (47/47 or more)

**Step 2: Check for skipped tests**

Run: `dotnet test backend/DataForgeStudio.sln --configuration Release --verbosity normal 2>&1 | grep -i "skipped"`
Expected: No output (no skipped tests)

---

## Task 4: Frontend Build Verification

**Files:**
- Build: `frontend/`

**Step 1: Install dependencies**

Run:
```bash
cd H:/NEW/DataForgeStudio/frontend
npm install
```
Expected: No vulnerabilities, successful install

**Step 2: Run production build**

Run: `npm run build`
Expected: **Build successful**, dist folder created

**Step 3: Check for build warnings**

Review output for:
- Chunk size warnings (acceptable if < 2MB for main chunks)
- Dependency deprecation warnings (document if critical)

---

## Task 5: Security Audit

**Files:**
- Check: `appsettings.json`, environment configs, sensitive files

**Step 1: Check for hardcoded secrets in codebase**

Run:
```bash
cd H:/NEW/DataForgeStudio
grep -r "password\s*=\s*\"" backend/src --include="*.cs" | grep -v "test\|Test\|example\|sample\|//"
grep -r "secret\s*=\s*\"" backend/src --include="*.cs" | grep -v "test\|Test\|example\|sample\|//"
```
Expected: No hardcoded passwords or secrets in production code

**Step 2: Verify appsettings.json security**

Run: `cat backend/src/DataForgeStudio.Api/appsettings.json | grep -i "password\|secret\|key"`
Expected: Only placeholder/default values for development, no production credentials

**Step 3: Check npm audit**

Run:
```bash
cd H:/NEW/DataForgeStudio/frontend
npm audit
```
Expected: **0 vulnerabilities** or only low/info severity

**Step 4: Check .NET package vulnerabilities**

Run: `dotnet list backend/DataForgeStudio.sln package --vulnerable`
Expected: No vulnerable packages

---

## Task 6: Database Script Verification

**Files:**
- Check: `database/scripts/`

**Step 1: Verify initialization script exists**

Run: `ls -la database/scripts/01_init_database.sql`
Expected: File exists and is readable

**Step 2: Verify migration script exists**

Run: `ls -la database/scripts/migration_upgrade_structure.sql`
Expected: File exists for upgrading existing databases

**Step 3: Check for SQL Server 2005 compatibility**

Run: `grep -E "SEQUENCE|OFFSET.*FETCH|IIF\(|STRING_SPLIT" database/scripts/*.sql`
Expected: No matches (these features are not SQL Server 2005 compatible)

---

## Task 7: Build Production Artifacts

**Files:**
- Output: `publish/Server/`, `publish/Web/`, `publish/DeployManager/`

**Step 1: Publish API**

Run:
```bash
cd H:/NEW/DataForgeStudio
dotnet publish backend/src/DataForgeStudio.Api/DataForgeStudio.Api.csproj --configuration Release --runtime win-x64 --self-contained true -o publish/Server
```
Expected: Successful publish

**Step 2: Publish DeployManager**

Run:
```bash
dotnet publish backend/tools/DeployManager/DeployManager.csproj --configuration Release --runtime win-x64 --self-contained true -o publish/DeployManager
```
Expected: Successful publish

**Step 3: Copy frontend build**

Run:
```bash
rm -rf publish/Web
mkdir -p publish/Web
cp -r frontend/dist/* publish/Web/
```
Expected: Frontend files copied

**Step 4: Verify all artifacts exist**

Run: `ls -la publish/Server/DataForgeStudio.Api.dll publish/DeployManager/DeployManager.exe publish/Web/index.html`
Expected: All files exist

---

## Task 8: Installer Build Verification

**Files:**
- Output: `dist/DataForgeStudio-Setup.exe`

**Step 1: Build installer**

Run:
```bash
cd H:/NEW/DataForgeStudio/installer
"C:/Users/azrae/AppData/Local/Programs/Inno Setup 6/ISCC.exe" setup.iss 2>&1 | tail -5
```
Expected: "Successful compile" message

**Step 2: Verify installer file exists**

Run: `ls -la dist/DataForgeStudio-Setup.exe`
Expected: File exists, size > 50MB

---

## Task 9: Final Integration Check

**Step 1: Run all tests one more time**

Run: `dotnet test backend/DataForgeStudio.sln --configuration Release`
Expected: All tests pass

**Step 2: Verify build directory structure**

Run:
```bash
cd H:/NEW/DataForgeStudio/build/installer
ls -la Server/ WebSite/ manager/ configurator/ WebServer/
```
Expected: All directories exist with files

---

## Task 10: Merge to Master

**Files:**
- Merge: `fix/cs8618-nullable-warnings` -> `master`

**Step 1: Checkout master branch**

Run:
```bash
cd H:/NEW/DataForgeStudio
git checkout master
git pull origin master
```
Expected: Master is up to date

**Step 2: Merge feature branch**

Run:
```bash
git merge fix/cs8618-nullable-warnings --no-ff -m "Merge branch 'fix/cs8618-nullable-warnings' - Pre-release fixes"
```
Expected: Merge successful, no conflicts

**Step 3: Verify merge result**

Run: `dotnet build backend/DataForgeStudio.sln --configuration Release && dotnet test backend/DataForgeStudio.sln --configuration Release`
Expected: Build and tests pass

**Step 4: Push to master**

Run: `git push origin master`
Expected: Push successful

---

## Task 11: Tag Release

**Step 1: Create release tag**

Run:
```bash
git tag -a v1.0.0 -m "DataForgeStudio V1.0 Release"
git push origin v1.0.0
```
Expected: Tag created and pushed

---

## Task 12: Final Report

**Step 1: Generate release summary**

Document:
- Total commits merged
- Files changed
- Warnings resolved
- Test results
- Build artifact locations

**Step 2: Update PROJECT_STATUS.md**

Update the document with release date and final status.

---

## Verification Checklist Summary

| Check | Command | Expected |
|-------|---------|----------|
| Build Errors | `dotnet build` | 0 |
| Build Warnings | `dotnet build 2>&1 \| grep -c warning` | 0 |
| Unit Tests | `dotnet test` | All pass |
| NPM Audit | `npm audit` | 0 critical/high |
| .NET Vulnerabilities | `dotnet list package --vulnerable` | None |
| Hardcoded Secrets | `grep -r "password=" backend/src` | None in prod code |
| Installer Build | ISCC.exe | Successful compile |

---

## Rollback Plan

If any check fails:
1. Do NOT merge to master
2. Fix the issue on the feature branch
3. Re-run the failing check
4. Only proceed to merge after all checks pass

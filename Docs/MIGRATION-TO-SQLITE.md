# Migration from SQL Server LocalDB to SQLite

## Overview
This document describes the migration from SQL Server LocalDB to SQLite for cross-platform compatibility, particularly for running on Linux systems.

## Why SQLite?
- ✅ **Cross-platform**: Works seamlessly on Windows, Linux, and macOS
- ✅ **Zero configuration**: No database server installation required
- ✅ **File-based**: Single file database (DocumentApi.db)
- ✅ **Lightweight**: Perfect for development and small to medium deployments
- ✅ **Excellent EF Core support**: Full compatibility with Entity Framework Core

## Changes Made

### 1. NuGet Packages
**Removed:**
- `Microsoft.EntityFrameworkCore.SqlServer` (9.0.10)

**Added:**
- `Microsoft.EntityFrameworkCore.Sqlite` (9.0.10)

### 2. Connection String (appsettings.json)
**Before:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocumentApiDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

**After:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=DocumentApi.db"
  }
}
```

### 3. Program.cs
**Before:**
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**After:**
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### 4. ApplicationDbContext.cs - SQL Syntax Changes
**Before (SQL Server specific):**
```csharp
entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
entity.Property(e => e.UploadedAt).HasDefaultValueSql("GETUTCDATE()");
```

**After (SQLite compatible):**
```csharp
entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
entity.Property(e => e.UploadedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
```

### 5. Database Migrations
- Deleted old SQL Server migrations
- Created new SQLite migration: `20251105193729_InitialCreate`

## Database File Location
The SQLite database file `DocumentApi.db` will be created in the project root directory when you first run the application.

## Running the Application

### First Time Setup
1. The database will be automatically created when you run the application
2. Roles will be seeded automatically (user, admin)

### Commands
```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Create a new migration (if needed)
dotnet ef migrations add MigrationName

# Apply migrations manually (if needed)
dotnet ef database update
```

## Viewing the Database

You can use any SQLite database browser to view and query the database:

### Recommended Tools:
- **DB Browser for SQLite** (https://sqlitebrowser.org/) - Free, cross-platform
- **SQLiteStudio** (https://sqlitestudio.pl/) - Free, cross-platform
- **VS Code Extensions**: SQLite Viewer, SQLite Explorer
- **Command Line**: `sqlite3 DocumentApi.db`

### Example Queries:
```sql
-- View all users
SELECT * FROM Users;

-- View all roles
SELECT * FROM Roles;

-- View user roles
SELECT u.Email, r.Name as Role
FROM Users u
JOIN UserRoles ur ON u.Id = ur.UserId
JOIN Roles r ON ur.RoleId = r.Id;

-- View all documents
SELECT * FROM Documents;

-- View email logs
SELECT * FROM EmailLogs;
```

## Compatibility Notes

### What Works the Same:
- All Entity Framework Core operations
- LINQ queries
- Migrations
- Seeding
- Relationships and foreign keys
- Transactions

### SQLite Limitations (Not affecting this project):
- No `ALTER COLUMN` support (use migrations carefully)
- Limited concurrent write operations (not an issue for this API)
- Case-insensitive string comparisons by default

## Linux Deployment

### Prerequisites on Linux:
```bash
# Install .NET 8 SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# No database server installation needed!
```

### Running on Linux:
```bash
# Navigate to project directory
cd /path/to/DocumentApi

# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

The application will create the SQLite database file automatically on first run.

## Backup and Restore

### Backup:
Simply copy the `DocumentApi.db` file to a safe location.

```bash
cp DocumentApi.db DocumentApi.db.backup
```

### Restore:
Replace the current database file with the backup.

```bash
cp DocumentApi.db.backup DocumentApi.db
```

## Performance Considerations

For this document management API:
- ✅ SQLite handles the expected load efficiently
- ✅ File operations are the bottleneck, not database
- ✅ Suitable for single-server deployments
- ⚠️ For high-concurrency production use, consider PostgreSQL or MySQL

## Rollback (If Needed)

If you need to switch back to SQL Server:

1. Install SQL Server package:
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   dotnet remove package Microsoft.EntityFrameworkCore.Sqlite
   ```

2. Update `appsettings.json`:
   ```json
   "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocumentApiDb;Trusted_Connection=true;MultipleActiveResultSets=true"
   ```

3. Update `Program.cs`:
   ```csharp
   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
   ```

4. Update `ApplicationDbContext.cs`:
   ```csharp
   .HasDefaultValueSql("GETUTCDATE()")
   ```

5. Delete migrations and recreate:
   ```bash
   dotnet ef migrations add InitialCreate
   ```

## Summary

The migration to SQLite has been completed successfully:
- ✅ All packages updated
- ✅ Connection strings updated
- ✅ SQL syntax made cross-platform compatible
- ✅ New migrations created
- ✅ Documentation updated
- ✅ Build successful

The application is now fully compatible with Linux and other platforms!


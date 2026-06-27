# Database Migrations Setup

## Summary

Created database migrations for newly added columns and configured automatic migration execution on application startup.

## What Was Done

### 1. Created New Migrations

Two new Entity Framework Core migrations were created to align the database schema with the current domain models:

#### Migration: `20260627150000_AddQuotePnLTable`
- **Purpose**: Creates the `QuotePnLs` table for tracking realized and unrealized Profit/Loss per quote asset
- **Changes**:
  - Creates `QuotePnLs` table with columns: `Id`, `WalletId`, `QuoteSymbol`, `RealizedPnL`, `UnrealizedPnL`, `UpdatedAt`
  - Creates unique index on `(WalletId, QuoteSymbol)` to ensure one PnL record per wallet per quote symbol
  - Establishes foreign key relationship with `Wallets` table

#### Migration: `20260627150001_AddQuoteSymbolToTokenPositions`
- **Purpose**: Adds the missing `QuoteSymbol` column to `TokenPositions` table
- **Changes**:
  - Drops existing unique index on `(WalletId, TokenAddress)`
  - Adds `QuoteSymbol` column with max length of 32 characters
  - Creates new unique index on `(WalletId, TokenAddress, QuoteSymbol)` to properly track positions per quote asset

### 2. Updated Model Snapshot

Updated `WalletTrackerDbContextModelSnapshot.cs` to reflect the current database schema state. This ensures Entity Framework can correctly generate future migrations.

**Key Changes in Snapshot**:
- Added `QuotePnL` entity definition with all properties and relationships
- Updated `TokenPosition` entity to include `QuoteSymbol` property
- Updated `Trade` entity to include `QuoteSymbol` property
- Removed `RealizedPnLInQuote` and `UnrealizedPnLInQuote` from `WalletStats` (moved to `QuotePnL`)

## Automatic Migration Execution

Migrations are configured to run **automatically on application startup**. This is already implemented in `Program.cs`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WalletTrackerDbContext>();
    db.Database.Migrate();  // Automatically applies pending migrations
}
```

**How it works**:
1. When the WalletTracker.Api application starts, it creates a service scope
2. It retrieves the `WalletTrackerDbContext` from the dependency injection container
3. It calls `db.Database.Migrate()` which:
   - Connects to the configured database
   - Checks which migrations have been applied
   - Executes any pending migrations in order
   - Updates the `__EFMigrationsHistory` table to track applied migrations

## Migration Order

Migrations will be applied in this order:
1. `20260627080255_InitialCreate` - Creates initial schema (if not already applied)
2. `20260627085843_AddBackfillStatus` - Adds backfill status tracking
3. `20260627150000_AddQuotePnLTable` - Creates QuotePnL table
4. `20260627150001_AddQuoteSymbolToTokenPositions` - Adds QuoteSymbol to TokenPositions

## No Manual Action Required

✅ **Development**: Simply run the application (`dotnet run`). Migrations apply automatically.

✅ **Production**: Deploy the application. Migrations apply on startup.

✅ **Testing**: Run the API. The database schema is initialized/updated automatically.

## Files Modified/Created

**New Migration Files**:
- `Migrations/20260627150000_AddQuotePnLTable.cs`
- `Migrations/20260627150000_AddQuotePnLTable.Designer.cs`
- `Migrations/20260627150001_AddQuoteSymbolToTokenPositions.cs`
- `Migrations/20260627150001_AddQuoteSymbolToTokenPositions.Designer.cs`

**Updated File**:
- `Migrations/WalletTrackerDbContextModelSnapshot.cs`

## Verification

Build verification:
```
Build succeeded.
```

All migrations are syntactically correct and have been compiled successfully.

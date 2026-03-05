# STBWeb Handoff Documentation

## 1. Tech Stack Overview
- **CMS:** Umbraco 15
- **Framework:** .NET 9.0.311
- **Deployment:** Azure App Service via GitHub Actions CI/CD Pipeline
- **Database:** SQLite (local/dev), Azure SQL (production)

## 2. Completed Work
- **Document Types & Templates:** All Phase 1 Data Types, Document Types, Element Types, Compositions, and Views have been built and deployed.
- **UI & CSS:** Core CSS, Javascript, and Images are migrated and active.
- **Phase 3 Components:** All custom functional components are built and integrated:
  - Dynamic Calculators (Deposits/Loans)
  - Exchange Rate API Handlers
  - Google Maps Branch/ATM Locator
  - Legacy `AppFormSurfaceController` application forms
  - Full-Site Examine Search integrations
- **CI/CD:** The GitHub Actions pipeline is fully green and actively deploying code to the Azure App Service on every commit to `main`.

## 3. The Current Critical Blocker (Azure Startup Crash)
The deployment succeeds, but the Azure App Service fails to start due to a fatal crash in the Umbraco boot process:

```
ArgumentOutOfRangeException: Specified argument was out of the range of valid values. (Parameter 'parentId')
```

### Root Cause
1. In `Migrations/ContentSeeder.cs`, the `Home` node is failing to save to the database. Due to the failure, its `Id` returns `0`.
2. The seeder does not halt. It continues to the next lines, executing `var indId = S("Individuals", "Naselenie", "Individuals", homeId, "sectionRoot");`, thereby attempting to pass a `parentId` of `0` into the `_contentService.Create` method.
3. Umbraco rejects a `parentId` of `0` in this context, triggering the `ArgumentOutOfRangeException` and bringing down the entire application runtime.

### Required Fixes (Technical Debt in ContentSeeder.cs)
You **must** modify `Migrations/ContentSeeder.cs` to resolve the legacy method signatures and enforce strict execution halting:

1. **Obsolete Domain APIs:** The existing code uses an old Umbraco 13/14 convention: `private async Task EnsureDomainsAsync(int homeId)`. This must be updated to the Umbraco 15 standard, which utilizes the Node Key (`Guid`), not the Int ID.
   - Refactor to: `private async Task EnsureDomainsAsync(Guid nodeKey)`
   - You must generate a proper `Umbraco.Cms.Core.Models.ContentEditing.DomainsUpdateModel` and pass it to `await _domainService.UpdateDomainsAsync(nodeKey, updateModel);`.

2. **Strict Guard Clauses:** The `Home` node save failure must FATALLY halt the seeding pipeline.
   ```csharp
   var saveResult = _contentService.Save(home);
   if (!saveResult.Success || home.Id <= 0) 
   {
       _logger.LogError("CRITICAL: Home node failed to save. Execution halted.");
       return; // FATAL HALT - DO NOT PROCEED TO CHILD NODES
   }
   ```

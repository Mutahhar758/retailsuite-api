# RetailSuite On-Premises Setup Project (`RetailSuite.Setup.vdproj`)

This directory contains the **Microsoft Visual Studio Setup Project** (`RetailSuite.Setup.vdproj`) and provisioning scripts for building the On-Premises Windows Installer (`setup.exe` + `RetailSuite.msi`).

---

## 1. Prerequisites in Visual Studio
1. Install the official **Microsoft Visual Studio Installer Projects 2022** extension (`Extensions` → `Manage Extensions` → search `"Microsoft Visual Studio Installer Projects"`).
2. Open `Retailer.sln` and add `setup\RetailSuite.Setup\RetailSuite.Setup.vdproj` if not already loaded.

---

## 2. Bundling Portable Embedded PostgreSQL (`pgsql\`)
To include the portable PostgreSQL 16+ engine with `pgvector` inside the `.msi`:
1. Download the official PostgreSQL 16+ Windows x64 binary zip (`postgresql-16.x-windows-x64-binaries.zip`) and `pgvector` (`vector.dll`, `vector.control`, `vector--*.sql`).
2. Extract only the required runtime directories into `setup\RetailSuite.Setup\pgsql\`:
   - `pgsql\bin\` (`postgres.exe`, `initdb.exe`, `pg_ctl.exe`, `psql.exe`, and required `.dll`s)
   - `pgsql\lib\` (including `vector.dll`)
   - `pgsql\share\` (including `extension\vector.control` and `extension\vector--*.sql`)
3. In Visual Studio's **File System Editor** for `RetailSuite.Setup`, include the `pgsql` folder under **Application Folder**.

---

## 3. How the Installer Works on Customer Machines

1. **Publish Profile (`OnPremises` Configuration):**
   ```powershell
   dotnet publish src/Host/WebAPI/WebAPI.csproj -p:PublishProfile=OnPremises
   ```
   - Compiles with `ON_PREMISES` defined.
   - Physically strips `TenantDbContext`, `TenantService`, `TenantDatabaseInitializer`, `TenantDatabaseProvisioner`, `TenantsController`, and `Migrations\Tenant\**` from the binary.
   - Outputs a compressed, self-contained ReadyToRun (`win-x64`) executable (`Retailer.Host.exe`).

2. **UI Dialogs (`View` → `User Interface`):**
   - Collects `ACTIVATION_KEY`, `STORE_NAME`, `ADMIN_EMAIL`, and `CLOUD_URL` via the built-in **Textboxes (A)** MSI dialog.

3. **Custom Actions (`View` → `Custom Actions`):**
   - **Install:** Invokes:
     ```text
     Retailer.Host.exe --onprem-install --activation-key="[ACTIVATION_KEY]" --store-name="[STORE_NAME]" --admin-email="[ADMIN_EMAIL]" --cloud-relay-url="[CLOUD_URL]"
     ```
     Which automatically:
     1. Resolves the store's `TenantId` from the Cloud `/api/license/verify/{key}` endpoint (or offline fallback).
     2. Generates a **unique 64-byte (512-bit) cryptographic JWT signing key** and a **64-character random PostgreSQL password**.
     3. Encrypts all local settings (`StoreSettings`, `DatabaseSettings`, `SecuritySettings:JwtSettings:Key`) using **Windows DPAPI (`DataProtectionScope.LocalMachine`)** into `C:\ProgramData\RetailSuite\store.dat` with `SYSTEM`/Administrators-only ACLs.
     4. Initializes the portable PostgreSQL cluster (`initdb.exe`) bound strictly to `127.0.0.1:5439` and registers/starts the `RetailSuitePostgres` Windows Service.
     5. Registers and starts the `RetailSuiteLocalServer` Windows Service (`Retailer.Host.exe`), which automatically runs `ApplicationDbContext.Database.MigrateAsync()`, seeds default roles/admin user, and opens the outbound SignalR tunnel (`LocalTunnelAgent`).
   - **Uninstall:** Invokes `Retailer.Host.exe --onprem-uninstall` to stop and remove both `RetailSuiteLocalServer` and `RetailSuitePostgres` Windows Services cleanly.

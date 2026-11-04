# MVC Music Store (MVC 3): migration notes

Status: migrated (webForCore 0.7.0, webForCore-thirdparty 0.6.0, drawingForCore 0.2.0: 20/20 probes on Linux and Windows); log-on
and registration blocked by [F75](../../FINDINGS.md#f70).
jongalloway/MvcMusicStore at `ea2552d6` (Oct 2025): Jon Galloway's copy of the CodePlex sample, with its MVC 3, 4 and
5 versions; this is the MVC 3 one (`src/MVC3/MvcMusicStore-Completed`, "MVC 3 Tools Update"). Ms-PL
(`src/MVC3/readme.txt`).

webForCore 0.8.0 wasn't released when this was migrated, so it uses 0.7.0 (with webForCore-thirdparty 0.6.0 and
drawingForCore 0.2.0, `WEBFORCORE_VERSION=0.7.0 THIRDPARTY_VERSION=0.6.0 DRAWING_VERSION=0.2.0`).

## Assessment

- **Type:** ASP.NET MVC 3 with Razor views, .NET Framework 4.0. Six controllers (store, cart, checkout, account,
  store manager with scaffolded CRUD views), child actions (`GenreMenu`, `CartSummary`), an AJAX JSON action
  (`RemoveFromCart`), `[Authorize]` and `[Authorize(Roles = "Administrator")]`, the shopping cart id in `Session`.
- **Data:** Entity Framework 4.1 Code First (`DbContext`), with `DropCreateDatabaseIfModelChanges` seeding the
  catalogue, on SQL Server Compact 4.0 (`|DataDirectory|MvcMusicStore.sdf`). Accounts and roles: ASP.NET's default
  `SqlMembershipProvider`/`SqlRoleProvider` from machine.config (`LocalSqlServer`: SQL Express user instance,
  `aspnetdb.mdf`), with `<roleManager enabled="true">`.
- **Blockers:** none from `wfc.py assess`. SQL CE is Windows-native (no .NET build), and SQL Express user instances
  don't exist on Linux: both need SQL Server ([F77](../../FINDINGS.md#f72)).
- **Dependencies:** (a) replaced by webForCore: MVC 3, Web Pages 1, Razor 1 (now MVC 5.3, Web Pages/Razor 3.3).
  (b) kept: EntityFramework (4.1 has no .NET build; 6.5.1, as the guide says), the jQuery/Modernizr content packages
  (their files are checked in). (c) none.
- `targetFramework="4.0"` in `<compilation>`, no `<httpRuntime targetFramework>`: webForCore reproduces 4.5+
  behaviour. No `<machineKey>`.
- **Go**, with the database on SQL Server.

## Migration

1. `scripts/convert.py`: SDK project on net10.0, `webForCore` and `webForCore.Web.Mvc` 0.7.0, EntityFramework
   4.1.10331 → 6.5.1, the content packages as `PackageReference`s, `packages.config` removed, `Program.cs` with session
   state (the cart). Added `<LangVersion>7.3</LangVersion>` (guide, section 2).
2. Build: `CS0104: 'Compare' is an ambiguous reference between 'System.ComponentModel.DataAnnotations.CompareAttribute'
   and 'System.Web.Mvc.CompareAttribute'` in `Models/AccountModels.cs` (.NET 4.5 added the DataAnnotations one). A
   `using CompareAttribute = System.Web.Mvc.CompareAttribute;` there keeps what it compiled to on 4.0
   ([F74](../../FINDINGS.md#f69)). Then it builds; EF 6 needed no code changes (`DbContext`, `DbSet`,
   `DropCreateDatabaseIfModelChanges` are the same).
3. First request: 500, `Conflicting versions of ASP.NET Web Pages detected: specified version is "1.0.0.0", but the
   version in bin is "3.0.0.0"`. `web.config`'s `webpages:Version` is now `3.0.0.0`, as Microsoft's MVC 3 → 5 upgrade
   has it ([F73](../../FINDINGS.md#f68)). The other MVC 3 version numbers (`<compilation><assemblies>`,
   `Views/Web.config`'s Razor section and host factory) needed no change.
4. `MusicStoreEntities` connection string: SQL CE → the test bed's SQL Server (`127.0.0.1,14330`, database
   `MvcMusicStore3`, `TrustServerCertificate=True`), `providerName="System.Data.SqlClient"`. EF creates and seeds the
   database on the first request, so `app.toml` has `needs = ["mssql"]` and no `[setup]` (an empty database made by
   `setup` would have no EF model metadata, and the initializer wouldn't seed it).
5. `LocalSqlServer` (membership and roles) is left as machine.config has it: pointing it at SQL Server would need
   the `aspnetdb` schema, which `aspnet_regsql` creates on .NET Framework, and webForCore has neither the tool nor the
   scripts `SqlServices.Install` reads ([F75](../../FINDINGS.md#f70)).

## Smoke

**Round on webForCore 0.7.0: 20/20 probes on Linux, and 20/20 on Windows (CI run 41900122122)** (13 of the app's and the runner's 7 forbidden paths), also under
`LANG=C LC_ALL=C`.

Probes: the home page (top-selling albums from the database), the genre list, a genre's albums, an album's details,
the cart, the log-on and register forms (unobtrusive validation; `data-val-equalto` from MVC's `[Compare]`),
`/StoreManager` and `/Checkout/AddressAndPayment` for an anonymous user (302 to the log-on page), a child action
requested directly (500 "accessible only by a child request", as on IIS), a stylesheet, jQuery, and an unknown
controller (404).

Also checked by hand: `/ShoppingCart/AddToCart/1` (302 to the cart, which then lists the album, the cart id kept in
session) and the AJAX `POST /ShoppingCart/RemoveFromCart/1` (JSON).

## Open problems

- **Log-on and registration:** posting either form is a 500, "SQLExpress database file auto-creation error": the
  default membership provider wants a SQL Express user instance. With a real SQL Server it would need `aspnet_regsql`'s
  schema, which can't be made on webForCore ([F75](../../FINDINGS.md#f70), webForCore#123). So checkout and the store
  manager (which need a user, and the Administrator role) are untested.
- **Prices print with three decimals** (`8.990` for 8.99, `@String.Format("{0:F}", ...)` on the album page): .NET's
  ICU culture data has `NumberDecimalDigits` 3 for `en-GB`/`en-US` where Windows NLS had 2
  ([F76](../../FINDINGS.md#f71), webForCore#124). The probes don't check prices.
- On this machine, with other agents' apps running, the first run failed every view with "Failed to start monitoring
  changes to '.../Views/ShoppingCart'": the user's 128 inotify instances (`fs.inotify.max_user_instances`) had run out,
  each worker taking about 15. webForCore documents it (`docs/IIS-COMPATIBILITY.md`, process model); it passed once
  fewer were running.

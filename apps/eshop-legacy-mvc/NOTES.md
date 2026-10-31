# eShopLegacyMVC (eShopModernizing): migration notes

Status: migrated (webForCore 0.2.0, all probes pass).
(the last commit; the repo is archived). webForCore 0.1.0, from its GitHub release packages (first migrated
against the source at `4ec57209`, which the release differs from only in packaging; every result below was
repeated on the packages).

## Assessment

- **Type:** MVC 5.2.7 + Web API 2 (5.2.7) in one project, `eShopLegacyMVCSolution/src/eShopLegacyMVC`, .NET Framework
  4.7.2 (`<httpRuntime targetFramework="4.6.1">`). Autofac for DI (`Autofac.Mvc5`, `Autofac.WebApi2`), EF 6.2 code first
  against SQL Server (LocalDB in web.config), log4net, Application Insights 2.9, bundling. A second project,
  `eShopLegacy.Utilities` (net461 class library), holds a `BinaryFormatter` helper.
- The project file is half-converted already: `<PackageReference>`s next to `<Reference HintPath=..\packages>` and a
  `packages.config`.
- `UseMockData` in web.config switches the catalog to an in-memory mock; it is `false` in the repo.
- **Blockers:** none from the runner's scan. By hand: `BinaryFormatter` in `eShopLegacy.Utilities/Serializing.cs`
  (used by `GET /api/files`): documented, replaced by webForCore's port. `using System.Runtime.Remoting.Messaging` in
  `BrandsController.cs` (unused): webForCore's System.Web carries `CallContext` there, so it compiles.
- **Dependencies:** (a) replaced by webForCore: Microsoft.AspNet.Mvc, .Razor, .WebPages, .WebApi.*, .Web.Optimization,
  WebGrease, Antlr, Microsoft.Web.Infrastructure, CodeDom providers, Microsoft.Net.Compilers. (b) managed, kept:
  Autofac 6.1, log4net 2.0.10, Newtonsoft.Json, EntityFramework (upgraded). (c) .NET Framework-only, tested:
  Autofac.Mvc5 4.0.2 and Autofac.WebApi2 6.0.1 (net45/net472 builds against System.Web.Mvc/Http), Application Insights
  Web 2.9.1 + TelemetryCorrelation 1.0.5 (`net45` HTTP modules), Microsoft.AspNet.SessionState.SessionStateModule 1.1.0
  (`net462`, replaces the session module). The `System.*` compatibility packages (System.Memory, System.Buffers, ...)
  are part of .NET and were dropped. Content packages (bootstrap, jQuery, ...) have their files checked in.
- No IIS reference responses; verification is the smoke probes.
- **Go**, with SQL Server from `services/compose.yml`.

## Migration

1. `eShopLegacyMVC.csproj` → `eShopLegacyMVC.csproj.netfx`; new SDK-style project per `docs/MIGRATING.md` section 2,
   with the `webForCore` package (the "Always" set), `webForCore.Web.Mvc`, `.Web.Helpers`, `.Web.Http.WebHost` and
   `.Web.Optimization`, 0.1.0 (`docs/MIGRATING.md` section 1; the runner restores them from `.packages/`). The old `<Compile>` list equals the `*.cs` glob, so nothing is removed. `GenerateAssemblyInfo`
   off (the app has `Properties/AssemblyInfo.cs`). `packages.config` removed.
2. `eShopLegacy.Utilities.csproj` → `.csproj.netfx`; new SDK-style `net11.0` library referencing webForCore's
   `webForCore.Serialization.Formatters` package, and `Serializing.cs` switched to
   `WebFormsForCore.Serialization.Formatters.Binary` (`docs/NAMESPACES.md`: .NET 11's BinaryFormatter throws).
3. Packages: EntityFramework 6.2.0 → 6.5.1 (6.2 has no .NET build), Newtonsoft.Json 12.0.1 → 13.0.4 (NU1605 against
   webForCore's 13.0.4). [F7](../../FINDINGS.md#f7)
4. First build: 13 × CS0433, `System.Web.Mvc` 5.1.0.0 vs 5.3.1.0 and `System.Web.Http` 5.2.0.0 vs 5.3.1.0, because
   Autofac.Mvc5 and Autofac.WebApi2 pull Microsoft.AspNet.Mvc/WebApi back in. Each transitive Microsoft ASP.NET package
   referenced with `ExcludeAssets="all"`. [F5](../../FINDINGS.md#f5)
5. `Program.cs` added (`docs/MIGRATING.md` section 4, with session: `Session_Start` in Global.asax uses it).
6. web.config: `<system.codedom>` removed (guide step). Connection string pointed at the compose SQL Server
   (`127.0.0.1,14330`, `sa`, `TrustServerCertificate=True`) instead of LocalDB, which doesn't exist on Linux.
7. First smoke: every request `Could not load file or assembly 'Microsoft.AI.Web'` (web.config `<httpModules>`).
   `Microsoft.ApplicationInsights.Web` 2.9.1 restores with no assembly; referenced
   `lib/net45/Microsoft.AI.Web.dll` from the package with `GeneratePathProperty`. [F6](../../FINDINGS.md#f6)
8. `CatalogDBInitializer.cs`: the three SQL script paths were `@"Models\Infrastructure\....sql"`; `\` isn't a separator
   on Linux, so they became `/` (works on Windows too). Guide section 6.
9. `BundleConfig.cs`: `~/Content/site.css` → `~/Content/Site.css`, the file's real name (it was silently missing from
   the CSS bundle on Linux). Guide section 6: fix the reference, don't rename the file.

Not changed, though they fail: the `SessionStateModuleAsync` registration in `<system.webServer><modules>` (F1) and
`AppDomain.CurrentDomain.BaseDirectory` in `CatalogDBInitializer.ExecuteScript` (F3). Both are webForCore bugs.

## Smoke

Probes: the catalog list (`/`), an MVC details page, the Create form (anti-forgery token), the picture action
(attribute route `items/{id}/pic`), Web API `GET /api/brands` and `/api/brands/1` (JSON), a static CSS file, the CSS
bundle and a static PNG.

**As committed: 0 of 9 app probes pass; the 6 forbidden paths return 404; the W3C log probe returns 500.** Every request
fails in `SessionStateModuleAsync.Init` with `Type 'System.Web.Configuration.SessionStateSection' does not inherit from
'System.Configuration.IConfigurationSectionHandler'` ([F1](../../FINDINGS.md#f1)).

**Exploration past F1** (not committed: the session module lines removed from web.config, by hand):

- With SQL Server: the database and tables are created, then `Seed` throws `DirectoryNotFoundException` for
  `<site>/bin_dotnet/Models/Infrastructure/dbo.catalog_hilo.Sequence.sql` ([F3](../../FINDINGS.md#f3)). The empty
  database stays, so the catalog is empty from then on: `/api/brands` returns `[]`, `/Catalog/Details/1` and
  `/items/1/pic` 404.
- With `UseMockData=true` as well, everything works: the list, details and Create pages render through the layout
  and bundles; `POST /Catalog/Create` with the anti-forgery token and cookie returns 302 to `/` and the new item is
  listed; `/items/1/pic` returns the PNG; `/api/brands` returns
  `[{"Id":1,"Brand":"Azure"},...]` as `application/json`; `/api/files` returns the BinaryFormatter stream through
  webForCore's port; static files and fonts are served with the right types. The Application Insights and
  TelemetryCorrelation modules load and run.
- Once, the home page failed with `ArgumentException: Item has already been added` in the file change monitor, when two
  first requests rendered the bundles at the same time ([F2](../../FINDINGS.md#f2)).
- `/Content/css` starts with WebGrease's `/* Minification failed. Returning unminified contents.` for Bootstrap 4's
  custom properties. That is WebGrease 1.6's behaviour on IIS too.
- `/api` (`CatalogController2` with `[Route("api")]`) is 404, as on IIS: MVC only finds controllers whose names end
  in `Controller`. Not probed.
- `GET /logs/W3SVC1/u_ex<date>.0000.txt` returned the W3C log ([F4](../../FINDINGS.md#f4)).

### Round 2: webForCore 0.2.0

All 15 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

Overlay change for 0.2.0: `CatalogDBInitializer.ExecuteScript` builds the script path from `HttpRuntime.AppDomainAppPath` instead of `AppDomain.CurrentDomain.BaseDirectory`, which on webForCore is the host's output folder (`bin_dotnet/`), not the site root (#3, now documented in `docs/MIGRATING.md`). The SQL Server seed now runs and the catalog comes from the database.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F1](../../FINDINGS.md#f1) (webForCore bug): `ConfigurationManager` from .NET Framework libraries doesn't see
  web.config. Blocks every request.
- [F3](../../FINDINGS.md#f3) (webForCore bug): `AppDomain.CurrentDomain.BaseDirectory` is `bin_dotnet/`. Breaks the
  database seed.
- [F2](../../FINDINGS.md#f2) (webForCore bug): file change monitor duplicate key; intermittent 500 on pages with bundles.
- [F4](../../FINDINGS.md#f4) (webForCore bug, security): W3C logs served from the site.
- Scripts/CSS in the bundles render, but minification of Bootstrap 4's CSS fails (WebGrease, same as IIS).

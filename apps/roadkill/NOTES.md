# Roadkill: migration notes

Status: blocked (F19: `System.Web.Abstractions` 4.0 in web.config; then F20). Source:
https://github.com/roadkillwiki/roadkill at `1061add4`. Found by searching GitHub on 2026-10-31; the repo, licence
(MS-PL), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API. webForCore 0.2.0 packages.

## Assessment

Wiki engine: MVC 5 and Web API 2 (with Swashbuckle), OWIN, StructureMap through WebActivatorEx, its own data layer
(LightSpeed ORM from `lib/`, Dapper, MongoDB), Lucene.Net search, .NET Framework 4.5. Archived upstream. Web project
plus `Roadkill.Core` and `Roadkill.Plugins`.

- `scripts/wfc.py assess`: blocker `windows-auth` at `src/Roadkill.Core/Security/Windows/ActiveDirectoryUserService.cs:227`
  (the optional Active Directory user service; the default is forms authentication, so not a blocker for this test);
  notice `smtpclient-config` at `src/Roadkill.Core/Email/EmailClient.cs:17`.
- Dependencies: (a) replaced by webForCore: MVC 5, Web API 2, Web Pages, Optimization, WebGrease, Katana. (b)
  .NET-compatible: Newtonsoft.Json (upgraded), Dapper, Lucene.Net 3.0.3, AngleSharp, HtmlSanitizer, MongoDB driver,
  NLog. (c) .NET Framework-only, tested: LightSpeed (checked-in DLLs), StructureMap.web, WebActivatorEx 2.0.5,
  Swashbuckle.Core 5.2, recaptcha 1.0.5, Microsoft.AspNet.Mvc.FixedDisplayModes.
- **Go**, with SQL Server from `services/compose.yml` (its `SqlServer2008` provider).

## Migration

1. `scripts/convert.py` on the web project, `Roadkill.Core` and `Roadkill.Plugins`: SDK-style projects with the
   webForCore 0.2.0 packages, Newtonsoft.Json 7.0.1 → 13.0.4 (F7), `Program.cs`, `<system.codedom>` removed,
   `packages.config` removed.
2. First build: NU1202, `recaptcha 1.0.5 is not compatible with net11.0`. The package's lib folders are named
   `.NetFramework 2.0` and `.NetFramework 4.0`, which NuGet can't map to a framework. It's restored for its path only
   (`GeneratePathProperty="true" ExcludeAssets="all"`) and `Recaptcha.dll` referenced from
   `$(Pkgrecaptcha)/lib/.NetFramework 4.0/`. [F23](../../FINDINGS.md#f23)
3. Next build: `Mindscape.LightSpeed` and `WebGrease` types missing, and CS8357 (`AssemblyVersion("2.0.*")` with
   wildcards in a deterministic build). Three converter bugs, fixed in `scripts/convert.py` and the projects
   re-converted. (a) It compared the `<Compile>` list with the files on disk case-sensitively, so the 11 files under
   `Database/Repositories/Lightspeed/` (listed as `LightSpeed\`) were removed. (b) It kept HintPaths in the project's
   case (`lib/Lightspeed/` for `lib/LightSpeed/`). (c) It dropped `WebGrease` instead of mapping it to
   `webForCore.WebGrease`. It also now sets `<Deterministic>false</Deterministic>` when an `AssemblyInfo.cs` has a
   wildcard version.
4. MVC, Razor, Web Pages, Web API 4.0 and Microsoft.Web.Infrastructure come back through
   Microsoft.AspNet.Mvc.FixedDisplayModes, Swashbuckle.Core and WebActivatorEx. They're referenced with
   `ExcludeAssets="all"` in all three projects, keeping `FixedDisplayModes` itself (a real feature assembly).
   [F5](../../FINDINGS.md#f5)
5. Database: `connectionStrings.config` pointed at the compose SQL Server. The `Roadkill` database is created and
   loaded from the app's `lib/Test-databases/roadkill-sqlserver.sql` with `sqlcmd` in the container:
   ```
   docker exec services-mssql-1 /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P Smoke_test_1234 -Q "CREATE DATABASE Roadkill"
   docker exec -i services-mssql-1 /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P Smoke_test_1234 -d Roadkill < src/lib/Test-databases/roadkill-sqlserver.sql
   ```

## Smoke

Probes: the home page, the log-in page and a static CSS file (no text checks yet: no page has rendered).

**As committed: 0 of 3 app probes pass; the 6 forbidden paths return 404.** Every request is a Configuration Error:
`Could not load file or assembly 'System.Web.Abstractions, Version=4.0.0.0'` at `web.config` line 34,
`<compilation><assemblies><add assembly="System.Web.Abstractions, Version=4.0.0.0, ..." />` (and
`System.Web.Routing, Version=4.0.0.0` after it). On .NET Framework 4 these are type-forwarding facades into
`System.Web`, in the GAC; MVC 3/4-era web.configs list them. [F19](../../FINDINGS.md#f19)

**Exploration past F19** (not committed: the two lines removed by hand): every request fails in
`Roadkill.Core.Startup.Configuration` (the OWIN startup) with a `NullReferenceException`: `LocatorStartup.Locator` is
null because `[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(LocatorStartup), "StartMVC")]` in
`Roadkill.Core` never ran. [F20](../../FINDINGS.md#f20)

### With webForCore's fixes ([jabba2324/webForCore#24](https://github.com/jabba2324/webForCore/pull/24))

On the PR's packages, the facades load (#20) and WebActivatorEx runs `LocatorStartup.StartMVC` (#21). Every request then
fails in StructureMap 4.0's `AssemblyFinder.FindBinPath`: `Method not found: 'System.String
System.AppDomainSetup.get_PrivateBinPath()'` (the .NET Framework build of StructureMap; .NET has no private bin path).
App/environment: [F25](../../FINDINGS.md#f25). A StructureMap with a .NET build, or `scanner.AssembliesFromPath` in
place of `AssembliesFromApplicationBaseDirectory`, is the next step.

## Open problems

- [F19](../../FINDINGS.md#f19) (webForCore bug): `System.Web.Abstractions`/`System.Web.Routing` 4.0.0.0 can't be loaded.
- [F20](../../FINDINGS.md#f20) (webForCore bug): WebActivatorEx start methods never run.
- [F23](../../FINDINGS.md#f23) (app/environment): recaptcha's lib folder names. Worked around with a HintPath.

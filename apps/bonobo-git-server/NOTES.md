# Bonobo Git Server: migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
(master, 2021). MIT. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

- **Type:** MVC 5.2.7 with OWIN (Katana 3.0.1: cookie authentication, optional WS-Federation), Unity 5 through
  Unity.Mvc and WebActivatorEx, EF 6.1.3 code first on SQLite (`System.Data.SQLite` 1.0.104), LibGit2Sharp 0.23.1
  for browsing repositories, and git's smart HTTP protocol by running `git` (`Git/GitService/GitServiceExecutor.cs`).
  .NET Framework 4.6. One web project, `Bonobo.Git.Server`; a test project not migrated.
- **Blockers:** the runner flags `ObjectContext` in `Data/Update/AutomaticUpdater.cs:48,65`: that is EF 6's
  `IObjectContextAdapter.ObjectContext` (`System.Data.Entity.Core.Objects`), not EF 1-4, so not a blocker. Windows
  authentication (`Owin/WindowsAuthenticationHandshake.cs`, SSPI through P/Invoke) is one of three
  `AuthenticationProvider`s; web.config selects `Cookies`. The Active Directory membership service is likewise an
  option, off by default.
- **Environment:** `GitPath` is `~\App_Data\Git\git.exe` (git for Windows, downloaded by the build); on Linux the
  system's `/usr/bin/git`.
- **Dependencies:** (a) replaced by webForCore: Microsoft.AspNet.Mvc/Razor/WebPages/Web.Optimization, Microsoft.Owin,
  .Host.SystemWeb, .Security, .Security.Cookies, Owin, WebGrease, Antlr, Microsoft.Web.Infrastructure. (b) kept:
  Unity 5.11 (+ Unity.Mvc 5.11.1, WebActivatorEx 2.2.0, CommonServiceLocator), Serilog 2.4, LibGit2Sharp 0.23.1 (+
  NativeBinaries 1.0.164), System.Data.SQLite 1.0.104 (+ .EF6, .Linq), DotNetZip, CommonMark.NET, MediaTypeMap,
  MvcCheckBoxList, UDE.CSharp, AnonymousComparer, Newtonsoft.Json. (c) Microsoft.Owin.Security.WsFederation 3.0.1
  (not in webForCore).
- **Go.**

## Migration

1. `Bonobo.Git.Server.csproj` → `.csproj.netfx`; SDK-style project with the converter: `webForCore`,
   `webForCore.Web.Mvc`, `.Web.Helpers`, `.Web.WebPages.Deployment`, `.Web.Optimization`,
   `webForCore.Microsoft.Owin.Host.SystemWeb` and `.Microsoft.Owin.Security.Cookies` 0.1.0. Two `.cs` files on disk
   that the old project didn't compile are removed from the build (`App_GlobalResources/Resources.it-IT.designer.cs`,
   `Data/RepositoryRepositoryBase.cs`). EntityFramework 6.1.3 → 6.5.1 and Newtonsoft.Json 9.0.1 → 13.0.4
   ([F7](../../FINDINGS.md#f7)). `packages.config` removed; `<system.codedom>` removed; `Program.cs` added (session).
2. `Microsoft.Owin.Security.WsFederation` 3.0.1 kept for `Security/FederationAuthenticationProvider.cs`, with its
   Katana 3.0.1 dependencies (`Microsoft.Owin`, `Microsoft.Owin.Security`) excluded ([F5](../../FINDINGS.md#f5)).
3. Restore: `NU1605: Detected package downgrade: Microsoft.IdentityModel.Tokens from 8.19.1 to 5.1.3`
   (`webForCore -> webForCore.Web.Extensions -> CoreWCF.Http -> Microsoft.IdentityModel.Tokens (>= 8.19.1)`).
   `packages.config` listed IdentityModel.Tokens and .Logging only as WS-Federation's dependencies, so the two
   pinned references were removed and NuGet resolves them ([F7](../../FINDINGS.md#f7)).
4. web.config: `GitPath` → `/usr/bin/git`. The other `~\App_Data\...` settings go through `Server.MapPath`, which
   accepts `\` in virtual paths as on IIS, so they are unchanged.

**Build: fails** with `CS0433: The type 'IHtmlString' exists in both 'System.Web.HttpUtility' and 'System.Web'`
([F8](../../FINDINGS.md#f8)). Not worked around in the overlay.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 9/9 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28); webForCore-thirdparty's WebActivatorEx at 0.4.1.

Probes: the logon page, `/` (redirects to the logon page), a static CSS file.

**Exploration past F8** (not committed: webForCore's `WebForCoreDropFrameworkHttpUtility` target copied into the
project): the build succeeds. Every request then redirects to `/Home/Error` (`<customErrors mode="On">`); with
customErrors off, the error is [F1](../../FINDINGS.md#f1): WebActivatorEx's `PreApplicationStartMethod` reads
`ConfigurationManager.AppSettings` through `System.Configuration` 4.0.0.0 and gets `Type
'System.Configuration.AppSettingsSection' does not inherit from 'System.Configuration.IConfigurationSectionHandler'`.
Nothing runs past that point, so SQLite (1.0.104 has no .NET build; 1.0.119 would be the upgrade), LibGit2Sharp
0.23.1 (`net40`, native binaries for Linux included) and git over HTTP are untested.

### Round 2: webForCore 0.2.0

All 9 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

Overlay changes for 0.2.0: System.Data.SQLite 1.0.104 → 1.0.119 (1.0.104 ships only the Windows `SQLite.Interop.dll`; 1.0.119 has `runtimes/linux-x64/native`), `System.Data.SQLite.Linq` dropped (merged into the main package); `Microsoft.AspNet.Mvc`, `.WebPages` and `.Razor` 3.2.7 referenced with `ExcludeAssets="all"` because Unity.Mvc pulls them in and their `System.Web.Razor` 3.0 replaced webForCore's (MissingMethodException in `MvcCSharpRazorCodeGenerator.SetBaseType`; `docs/MIGRATING.md`, "Packages that need more than a reference"). The SQLite database is created by `AutomaticUpdater` on the first request and the log-on page renders. `/packages.config` answers 200 with the app's own "404 Page Not Found" page: web.config clears requestFiltering's `fileExtensions`/`hiddenSegments`, and `Application_Error` calls `Response.Clear()` (which resets the status to 200, as on .NET Framework) before rendering `Home/PageNotFound`. The file is not served; the probe is overridden in `app.toml` to check that. Repository hosting over HTTP and LibGit2Sharp are still untested.

### Round 3: webForCore 0.4.0

All 9 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0, and its 5 `ExcludeAssets` references for transitive Microsoft packages were removed: webForCore's packages override them since 0.3.0 (webForCore#23).

`webForCore.WebActivatorEx` (webForCore-thirdparty 0.4.0) added to the web project for its `UnityMvcActivator` (Bonobo also sets
the resolver from `Global.asax`, so it passed without it).

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F8](../../FINDINGS.md#f8) (webForCore bug): `IHtmlString` ambiguous in compiled code. Blocks the build.
- [F1](../../FINDINGS.md#f1) (webForCore bug): WebActivatorEx can't read appSettings. Blocks every request.
- Next, once those are fixed: System.Data.SQLite upgrade, LibGit2Sharp on Linux, git smart HTTP.

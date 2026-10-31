# ApplicantTracking: migration notes

Status: blocked (F20: WebActivatorEx's start methods never run, so Unity isn't registered). Source:
https://github.com/feliperomero3/ApplicantTracking at `ff9a2eb4`. Found by searching GitHub on 2026-10-31; the repo,
licence (Apache-2.0), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API. webForCore
0.2.0 packages.

## Assessment

MVC 5 applicant tracking system (CRUD over applicants) with ASP.NET Identity 2, EF 6 code first (migrations with a
seed), Unity 5 through `Unity.Mvc` and WebActivatorEx, .NET Framework 4.6.2. Web project plus two libraries
(`ApplicantTracking.Data`, `.Domain`). `scripts/wfc.py assess`: no blockers, no notices.

- Dependencies: (a) replaced by webForCore: MVC 5, Web Pages, Optimization, Katana, Identity 2. (b) .NET-compatible:
  EntityFramework (upgraded), Newtonsoft.Json (upgraded), Unity.Container 5.7, Unity.Abstractions 3.3. (c) .NET
  Framework-only, tested: Unity.Mvc 5.0.13, WebActivatorEx 2.2.0.
- A global `RequireHttpsAttribute` (`App_Start/FilterConfig.cs`).
- **Go**, with SQL Server from `services/compose.yml`.

## Migration

1. `scripts/convert.py` on the web project and both libraries: SDK-style projects with the webForCore 0.2.0 packages,
   EntityFramework 6.1.3/6.2.0 → 6.5.1 and Newtonsoft.Json → 13.0.4 (F7), `Program.cs`, `<system.codedom>` removed,
   `packages.config` removed. Builds first time.
2. web.config: `DefaultConnection` pointed at the compose SQL Server instead of a LocalDB `.mdf`.
3. First smoke: `/` is a 302 to `https://127.0.0.1/`, which is `RequireHttpsAttribute` doing its job (the same on IIS).
   `app.toml` sets `https = true`: the runner serves the app over HTTPS with the .NET development certificate.
4. Next: `MissingMethodException: 'System.CodeDom.CodeTypeDeclaration
   System.Web.Razor.Generator.CodeGeneratorContext.get_GeneratedClass()'` on every view. Unity.Mvc 5.0.13 depends on
   Microsoft.AspNet.Mvc 5.2.3, and WebActivatorEx on Microsoft.Web.Infrastructure, whose assemblies replaced
   webForCore's in `bin_dotnet/`. The four transitive packages referenced with `ExcludeAssets="all"`
   (`docs/MIGRATING.md`, "Packages that need more than a reference"). [F5](../../FINDINGS.md#f5)

## Smoke

Probes: the home page, the Create form (anti-forgery token, from a controller Unity constructs), the log-in page and a
static CSS file. (The applicant list and details pages were probed at first; see below for why they no longer are.)

**As committed: 2 of 4 app probes pass (`/` and the CSS); the 6 forbidden paths return 404.** Every controller with a
constructor dependency fails: `Cannot dynamically create an instance of type
'ApplicantTracking.Web.Controllers.ApplicantController'. Reason: No parameterless constructor defined.`
`App_Start/UnityMvcActivator.cs` registers Unity's resolver from
`[assembly: WebActivatorEx.PreApplicationStartMethod(...)]`, and it never runs. WebActivatorEx finds those attributes by
`Assembly.LoadFrom` on each DLL in `HttpRuntime.BinDirectory`, and in webForCore's PseudoAppDomain `LoadFrom` returns
the copy in the default load context, not the application's. Checked by hand from `Application_Start` (not committed):
`LoadFrom(bin_dotnet/ApplicantTracking.Web.dll)` is in `"Default"`, the running assembly is in `PseudoAppDomain ...`,
and `GetCustomAttributes(typeof(WebActivatorEx.PreApplicationStartMethodAttribute))` on the loaded copy finds 0.
[F20](../../FINDINGS.md#f20)

### With webForCore's fixes ([jabba2324/webForCore#24](https://github.com/jabba2324/webForCore/pull/24))

On the PR's packages, WebActivatorEx's start method registers Unity (#21) and the DI controllers work. The
`ExcludeAssets` references for Unity.Mvc's transitive packages aren't needed (#23).

`/Applicant` and `/Applicant/Details/1` then fail with the app's own error: `One or more validation errors were detected
during model generation: ApplicantTracking.Data.Identity.Domicilio: EntityType 'Domicilio' has no key defined`.
`Domain/Models/Domicilio.cs` has no key property and the context has a `DbSet<Domicilio>`, so EF 6 rejects the model on
.NET Framework too. Those two probes are removed (app/environment). The others pass on the PR's packages.

## Open problems

- [F20](../../FINDINGS.md#f20) (webForCore bug): WebActivatorEx start methods never run. Blocks every page with DI.

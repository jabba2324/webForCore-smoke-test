# Orchard CMS 1.x: migration notes

Status: blocked on Linux (F97: it runs out of inotify instances); on Windows see "Smoke". Source:
https://github.com/OrchardCMS/Orchard at `78ee4efb` (tag 1.10.4, the last 1.10.x release). The repo, licence (BSD
3-Clause, `LICENSE.txt`), web project (`src/Orchard.Web`) and its `<TargetFrameworkVersion>` (v4.8, with System.Web)
were checked when the app was verified.

Round 7, on webForCore 0.8.0 (.NET 10, under `webforcore-host`), webForCore-thirdparty 0.6.0 (not used: Orchard has
neither WebActivatorEx nor AjaxControlToolkit) and drawingForCore 0.3.0.

## Assessment

- **Frameworks:** MVC 5 (the whole site: Orchard's own routing, controllers and Razor views in Core, every module and
  theme), Web API 2 (`Orchard.WebApi`, routes from modules), OWIN (`Orchard.Owin.Startup` through
  `Microsoft.Owin.Host.SystemWeb`); `assess` also sees Web Pages (Razor). No Web Forms pages.
- **Target:** .NET 4.8 (`<httpRuntime targetFramework="4.8">`).
- **Projects:** the web project, `Orchard.Framework` (`src/Orchard`), `Orchard.Core` (`src/Orchard.Web/Core`),
  `Orchard.WarmupStarter`, the themes' project (only an `AssemblyInfo.cs`), the command-line tool (`src/Tools/Orchard`),
  69 modules under `Modules/` (each its own project, built into `Modules/<id>/bin`, which the web project doesn't
  reference), and the test and spec projects.
- **How Orchard loads modules:** it reads each module's `Module.txt`, then loads its assembly with one of its
  extension loaders: precompiled (`Modules/<id>/bin/<id>.dll`, copied to `App_Data/Dependencies` and loaded by name,
  through web.config's `<probing privatePath="App_Data/Dependencies">`), dynamic (the module's `.csproj`, parsed by
  Orchard and compiled by `BuildManager` through its own `.csproj` build provider), or referenced (the site's `bin`).
  Views compile at run time, with Orchard's `RazorBuildProvider.CodeGenerationStarted` handler adding the modules'
  assemblies.
- **Blockers flagged by `assess`:**
  - WCF hosting: `src/Orchard/Wcf/OrchardServiceHostFactory.cs` (a `ServiceHostFactory` for modules' `.svc` files;
    nothing in Orchard uses it);
  - LINQ to SQL: `System.Data.Linq` in every web.config's `<compilation><assemblies>` (72 files; no code uses it);
  - COM+ and Windows only: a `<remove>` of `System.EnterpriseServices` in `<compilation><assemblies>`, and the GAC list
    in `IAssemblyNameResolver.cs` (strings only);
  - Windows authentication: `HttpRequestBaseWrapper.LogonUserIdentity` (a pass-through).
  - None is on a path the site needs.
- **Notices:** `BinaryFormatter` (`SessionConfigurationCache`, the output cache's file and Redis providers),
  `new SmtpClient` (Orchard.Email; not exercised).
- **Dependencies:**
  - (a) replaced by webForCore: MVC 5.2.7, Web API 5.2.7, Razor and Web Pages 3.2.7, Katana 4.2.2 (`Microsoft.Owin`,
    `.Host.SystemWeb`), Microsoft.Web.Infrastructure, the CodeDom provider;
  - (b) managed, kept: Autofac 3.5.2, log4net 2.0.12, Newtonsoft.Json (13.0.4), Orchard.NuGet.Core, MySql.Data,
    Npgsql; per module Lucene.Net, Markdig, YamlDotNet, MailKit, ImageResizer, IronRuby, Mono.CSharp,
    StackExchange.Redis, the Azure SDKs, ...;
  - (c) .NET Framework only, replaced: Autofac.Configuration 3.3.0 (worked around), Castle.Core 3.3.1 and NHibernate
    4.1.2 with FluentNHibernate 2.0.3 (upgraded) (F100), `System.Data.Services.Client` (F101); SQL Server Compact
    (native, Windows only: the setup's "built-in" database, not used: the site uses SQL Server).
- **Database:** SQL Server (the compose one, database `orchard110`), created by Orchard's setup (NHibernate's schema
  export, then the recipe's migrations).

Go, with SQL Server and the setup's Core recipe.

## Migration

1. `scripts/convert.py` on the web project and 73 libraries (Orchard.Framework, Orchard.WarmupStarter, Orchard.Core,
   the themes' project, the 69 modules), then by hand:
   - the web project: `Core/**`, `Modules/**` and `Themes/**` out of its `Compile` and `EmbeddedResource` items (the
     SDK glob would compile them all into it; convert.py had listed the 2,437 files one by one); the reference to the
     command-line tool removed (an `.exe` that hosts the site in an AppDomain; not part of the site); the SQL Server
     Compact reference removed (native, Windows only);
   - `Program.cs`: convert.py's, without `AddSession()`/`UseSession()` (not needed from 0.8.0, MIGRATING.md section 4);
   - every library: `<LangVersion>7.3</LangVersion>`, as the old projects had;
   - the modules: `OutputPath` `bin` with no target framework folder (where Orchard's precompiled loader looks, as
     Visual Studio built them), and `Private="false"` on their references to Orchard.Framework and Orchard.Core (as
     before: those are the site's);
   - Orchard.Framework: `webForCore.Microsoft.Owin` (convert.py maps only `Microsoft.Owin.Host.SystemWeb` and the
     security packages); `Wcf/**` out of the build (WCF hosting, above);
   - the themes' project left as it was: it builds only an `AssemblyInfo.cs` and references Orchard.Layouts; nothing
     loads it.
2. `src/Orchard.WebForCore.slnx` (new): the migrated projects, so that `also_build` builds the modules, which no
   project references. `Orchard.sln` also has the tests, specs and tools, which aren't migrated.
3. Every web.config (199, most in views folders): the `<system.codedom>` entries for
   `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` removed, and `System.Data.Linq`, `System.Data.Entity`,
   `System.Data.Entity.Design`, `System.Data.Services.Client` and `System.Web.Entity` removed from
   `<compilation><assemblies>` (F37).
4. log4net: `log4net.Config` = `../Config/log4net.config` and the log files `../App_Data/Logs/...`: log4net resolves
   them against `AppDomain.BaseDirectory`, which is `bin_dotnet/` (MIGRATING.md section 6).
5. `~/bin` → `~/bin_dotnet` in `DefaultBuildManager.HasReferencedAssembly`, `ReferencedExtensionLoader` and
   `DefaultHostEnvironment`'s restart marker (`~/bin_dotnet/HostRestart`: a change there restarts the app, as `bin` did)
   (MIGRATING.md section 6, #91).
6. `BinaryFormatter` → `WebFormsForCore.Serialization.Formatters.Binary.BinaryFormatter` in `SessionConfigurationCache`
   (NHibernate's mappings cache) and in the output cache's file and Redis providers.
7. Libraries that don't run on .NET (F100):
   - Autofac.Configuration 3.3.0: `new ConfigurationSettingsReader(section, file)` throws `MissingMethodException`
     (`AppDomainSetup.ConfigurationFile`). `XmlFileConfigurationReader` (new; internal, because Orchard registers every
     public Autofac module in its assemblies with each shell) reads the same file through
     `SectionHandler.Deserialize(XmlReader)`; used for `Config/Host.config` and `Config/Sites.config`.
   - Castle.Core 3.3.1 → 4.1.1 (`AppDomain.DefineDynamicAssembly`); 4.1.1 is the last before `ILogger.Trace`, which
     Orchard's logger doesn't implement. `RoutePublisher`: `.ToList().ForEach(...)` (Castle's `ForEach` is internal in 4).
   - NHibernate 4.1.2 → 5.1.7, FluentNHibernate 2.0.3 → 2.1.2, Iesi.Collections 4.0.1 → 4.0.4, in every project that
     has them (NHibernate.Caches.SysCache2 → 5.3.0). 4.1's proxies need `AppDomain.DefineDynamicAssembly` (the setup
     failed with "illegal access to loading collection"), and its SQL Server batcher reflects into .NET Framework's
     `System.Data`. NHibernate 5's API: `IInterceptor.Instantiate` without `EntityMode` (`AbstractSessionInterceptor`,
     `SessionLocator`), `InitializeParameter(DbParameter, ...)` (the SQL CE drivers), `new EntityKey(id, persister)`
     (`AbstractDataServicesProvider`), `Get/SetPropertyValues` without `EntityMode` (`Repository`), and `ICache`'s
     async members (Orchard.Azure's `AzureCacheClient`, as wrappers of the synchronous ones). Orchard's old LINQ provider
     (`lib/nhibernate.linq`, built for NHibernate 3.3) stays.
8. Orchard.Packaging: `System.Data.Services.Client` → the `Microsoft.Data.Services.Client` 5.8.4 package (F101).
9. Orchard.Packaging and Orchard.Localization: their own packages copied into their `bin` folders (and only those),
   where Orchard finds them (F102). Other modules with packages of their own (Markdown, Layouts, MediaProcessing,
   Email, Lucene, ...) would need the same when enabled; the Core recipe doesn't enable them.
10. `Config/HostComponents.config`: the dynamic extension loader disabled (F103): it parses each module's `.csproj` in
    the old MSBuild format, and fails on the SDK ones at every start.
11. `Global.asax.cs`: an `AppDomain.AssemblyResolve` handler that loads `App_Data/Dependencies/<name>.dll`, as web.config's
    `<probing privatePath>` did on IIS. **A workaround for a webForCore bug (F99, webForCore#147).**

Tried and reverted: `<httpRuntime fcnMode="Single">` and `"Disabled"` (F98: ignored); Orchard's own monitoring
switches in `HostComponents.config` (they don't reduce the monitors ASP.NET's virtual path provider creates); keeping
only the 17 modules the Core recipe needs (F97: still not enough).

## Smoke

Setup (`[setup]`): the setup page; the setup form, with the compose SQL Server and the Core recipe (it answers with a
redirect to `/`, which is 503 while the recipe runs in the background, so that step may fail); the home page, waited for
until the recipe has finished; the admin's log-in; the dashboard.

Probes: the home page and the same page at its alias (Orchard.Pages through Orchard.Autoroute, in the SafeMode theme),
a module's MVC page (Orchard.Users' log-on form), the dashboard's redirect for an anonymous visitor, a static file from
a theme, and three paths that must not be served (a module's assembly, its `Module.txt`, and the tenant's
`Settings.txt`, which has the connection string), plus the runner's own.

**Linux (0.8.0): blocked, 9/15** (the paths that must not be served; the setup fails at its first step). Every
request is a 500: `IOException: The configured user limit (128) on the number of inotify instances has been reached`
(F97), from `Failed to start monitoring changes to '.../Modules/<id>/bin'` while Orchard probes its modules; static
files fail too ("Configuration Error": their folder's web.config can't be monitored). Measured with the worker run as a
user with no other inotify instances: it reached 127 (+1 for the host) during the first request. With only the 17
modules the Core recipe needs, Orchard got further, which shows what works on webForCore: it starts (about 65
instances), the setup page renders, the setup creates the schema with NHibernate 5 on SQL Server (38 tables), and the
recipe enables its features, imports its content types and runs the migrations, then fails at 128 on its first command
(`menu create`, while Orchard looks for `.po` files in every module).

**Windows:** see `results.windows.json` (CI).

## Open problems

- [F97](../../FINDINGS.md#f97) (webForCore bug, webForCore#145): one inotify instance per monitored folder (two for
  many); Orchard can't start on Linux.
- [F98](../../FINDINGS.md#f98) (webForCore bug, webForCore#146): `<httpRuntime fcnMode>` is ignored.
- [F99](../../FINDINGS.md#f99) (webForCore bug, webForCore#147): `<probing privatePath>` isn't applied; worked around
  in `Global.asax.cs`.
- [F100](../../FINDINGS.md#f100) (guide gap, webForCore#148): Autofac.Configuration 3.x, Castle.Core 3.x and
  NHibernate 4.x don't run on .NET.
- [F101](../../FINDINGS.md#f101) (guide gap, webForCore#149): `System.Data.Services.Client`.
- [F102](../../FINDINGS.md#f102) (guide gap, webForCore#150): plug-in libraries built into their own folder lose their
  packages.
- [F103](../../FINDINGS.md#f103) (app/environment): Orchard's run-time module compilation can't parse SDK projects, so
  it isn't exercised.
- Not migrated: the tests, specs and command-line tool. The modules whose own libraries aren't copied (step 9), and the
  Default and Blog recipes, are untried.

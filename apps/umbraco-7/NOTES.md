# Umbraco 7: migration notes

Status: blocked (it builds, but doesn't boot: AutoMapper 3.3.1, F22). Source: https://github.com/umbraco/Umbraco-CMS at
`bd60f4e2` (tag release-7.15.11, the last 7.x release). The repo, licence (MIT), web project and its
`<TargetFrameworkVersion>` (v4.5.2) were checked when the app was added.

Round 6, on webForCore 0.7.0 (.NET 10, under `webforcore-host`). No webForCore-thirdparty packages: Umbraco 7 uses
neither WebActivatorEx nor AjaxControlToolkit. Much of the code is Umbraco 6's (`apps/umbraco-6/`), and so are many of
the fixes.

## Assessment

- **Frameworks:**
  - MVC 5: the front end's templates (`RenderMvcController`), the back office's and installer's pages
    (`BackOfficeController`, `InstallController`), surface controllers;
  - Web API 2: the back office's API (`/umbraco/backoffice/UmbracoApi/...`) and the installer's (`/install/api/...`);
  - Web Forms: the legacy back-office pages and dialogs under `/umbraco/` (`.aspx`, `.ascx`), ASMX script services,
    generic handlers;
  - Web Pages: Razor macros (`umbraco.MacroEngines`);
  - OWIN (`UmbracoDefaultOwinStartup`, `owin:appStartup`), ASP.NET Identity 2 (back-office users), SignalR 2 (preview).
  - The back office itself is an AngularJS application, built from `src/Umbraco.Web.UI.Client` with npm, bower and
    gulp 3 into `Umbraco.Web.UI/umbraco/{lib,js,assets,views}` (not in the repository).
- **Target:** .NET 4.5.2 (`<httpRuntime targetFramework="4.5.2">`).
- **Projects:** the web project `src/Umbraco.Web.UI` and 12 libraries: `Umbraco.Core`, `Umbraco.Web` (assembly
  `umbraco`), `umbraco.businesslogic`, `.cms`, `.controls`, `.datalayer`, `.editorControls`, `.interfaces`,
  `.providers`, `umbraco.MacroEngines`, `UmbracoExamine`, `SQLCE4Umbraco`.
- **Blockers flagged by `assess`:**
  - `AppDomain.CreateDomain` in `Umbraco.Core/Packaging/PackageBinaryInspector.cs:289`: package installation's binary
    scan only;
  - `System.EnterpriseServices` in `TreeClientService.asmx.cs:24` (a `using`; webForCore has the type);
  - `WindowsIdentity` in `Umbraco.Core/Logging/AsynchronousRollingFileAppender.cs` (error paths of an appender the
    configuration doesn't use), `UmbracoRequest.LogonUserIdentity` (a pass-through), `FixWindowsAuthMiddlware.cs` (only
    with Windows authentication) and `umbraco.businesslogic/Utils/fileHelper.cs` (an ACL check);
  - none is on a path the app needs; they are compile fixes at most.
- **Notices:** `new SmtpClient()` in `EmailSender` and `NotificationService` (mail; not exercised).
- **Dependencies:**
  - (a) replaced by webForCore: MVC 5.2.7, Web API 5.2.7, Razor and Web Pages 3.2.7, Katana 4.0.1 (Owin, Host.SystemWeb,
    Security, Cookies, OAuth), Identity 2.2.2, SignalR 2.4.1, Microsoft.Web.Infrastructure;
  - (b) managed, kept: **AutoMapper 3.3.1** (F22: doesn't run on .NET), ClientDependency 1.9.9 (+ -Mvc5),
    dotless 1.5.2, Examine 0.1.90, Lucene.Net 2.9.4, HtmlAgilityPack 1.8.8, ImageProcessor 2.7 / .Web 4.10,
    log4net 2.0.12, **Log4Net.Async 2.0.4** (doesn't run on .NET either, F66), Markdown, MiniProfiler 2.1, MySql.Data 6.9,
    Newtonsoft.Json (raised to 13.0.4), semver, SharpZipLib, Tidy.Net, Microsoft.ApplicationBlocks.Data,
    Microsoft.IO.RecyclableMemoryStream, Umbraco.ModelsBuilder 3.0.10;
  - (c) .NET Framework-only: SqlServerCE 4 (native SQL CE: compiled against, not used; the site uses SQL Server).
  - Dropped as the guide says: Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Microsoft.Net.Compilers,
    Microsoft.CodeAnalysis 1.0 (there for that compiler), MSBuild.Microsoft.VisualStudio.Web.targets.
- **Database:** SQL Server (the compose one, database `umbraco7`), MySQL or SQL CE. The schema is made by the
  installer, a JSON API.

Go, with SQL Server, expecting F22 to stop it at boot (it configures AutoMapper there).

## Migration

1. `scripts/convert.py` on the web project and the 12 libraries.
   - `<LangVersion>7.3</LangVersion>` in every project: ten set none, and `Umbraco.Core` and `Umbraco.Web` set
     `latest`, which was 7.3 in the Visual Studio 2017/2019 they were built with (`docs/MIGRATING.md` section 2).
   - `<Deterministic>false</Deterministic>` in every project that compiles the shared `SolutionInfo.cs`:
     `AssemblyVersion("1.0.*")` (CS8357); the converter only looks at each project's own `AssemblyInfo.cs`.
   - The web project: Microsoft.CodeAnalysis 1.0 and MSBuild.Microsoft.VisualStudio.Web.targets removed (above).
   - The repository's own `src/NuGet.Config` already names Umbraco's MyGet feed (F64 for Umbraco 6), so nothing was
     added.
2. `Umbraco.Web.UI.csproj`: a target that makes `Web.config` (gitignored) from `web.Template.config` with
   `web.Template.Release.config`'s transform, and `config/ClientDependency.config` from its `.Release.config`, before
   the build, as the .NET Framework project's `BeforeBuild` did. Each is made once (the installer writes to them).
   The project's `BelleBuild` target (the AngularJS client, through PowerShell) isn't carried over (see "Smoke").
3. `web.Template.config`:
   - `<system.codedom>` removed (the DotNetCompilerPlatform entries; the guide's step);
   - `System.Runtime.InteropServices.WindowsRuntime`, `System.ServiceModel.Http` and `System.ServiceModel.NetTcp`
     removed from `<compilation><assemblies>`: .NET doesn't have them, and the first was a Configuration Error on every
     page (F37; the guide's step, `docs/MIGRATING.md` section 5);
   - the `umbracoDbDSN` connection string points at the compose SQL Server (`TrustServerCertificate=True`);
   - `umbracoBinDirectory` = `~/bin_dotnet`: the legacy create dialogs load `~/bin/umbraco.dll`
     (`LegacyDialogHandler.cs`; F61, as in Umbraco 6);
   - `log4net.Config` = `../config/log4net.config`: log4net resolves it against `AppDomain.BaseDirectory`, which is
     `bin_dotnet/` (`docs/MIGRATING.md` section 6), and the `\` didn't work on Linux;
   - an explicit `<machineKey>` (generated, test-only; `docs/MIGRATING.md` section 5): the installer's `web.config`
     writes restart the app.
4. `config/log4net.config`:
   - the log file is `../App_Data/Logs/...` (it was `App_Data\Logs\...`, relative to `bin_dotnet/`);
   - the root logger writes through the file appender directly, not Log4Net.Async's `ParallelForwardingAppender`
     (F66): that net40 assembly uses mscorlib's `System.Runtime.Remoting.Messaging.CallContext`, which .NET doesn't
     have, so every event was a `TypeLoadException` and nothing was logged (Umbraco's own `ParallelForwardingAppender`
     derives from it). Logging is synchronous instead.
5. Compile fixes:
   - unused `using`s of namespaces .NET doesn't have: `System.Runtime.Remoting.Contexts` (four files),
     `System.Deployment.Internal`, `System.Windows.Forms` and `.VisualStyles`, `System.Management.Instrumentation`
     (`UrlHelperExtensions.cs`);
   - `ApplicationTreeExtensions.cs` does use `System.Management.Instrumentation`'s `InstanceNotFoundException` (as an
     `Attempt`'s failure, never caught by type): on .NET it's an alias for `KeyNotFoundException`;
   - EF 1–4's exception types (`System.Data.Entity`, documented as unavailable): `PackageInstallation.cs` throws
     `PropertyConstraintException`, the two legacy property editor converters `ObjectNotFoundException`; nothing catches
     them by type, so on .NET they are aliases for `InvalidOperationException` and `KeyNotFoundException`.
     `SqlAzureTransientErrorDetectionStrategy.cs`'s `EntityException` check is under `#if NETFRAMEWORK` (as in Umbraco 6);
   - `ClassFactory.cs`: `AssemblyBuilder.DefineDynamicAssembly` for `AppDomain.CurrentDomain.DefineDynamicAssembly`;
   - `PackageBinaryInspector.cs`: the temporary AppDomain's `AppDomainSetup` and evidence under `#if NETFRAMEWORK`; on
     .NET it calls `AppDomain.CreateDomain(name)`, which throws `PlatformNotSupportedException` (package installation's
     binary scan; documented);
   - `FilePermissionHelper.cs`: `new DirectoryInfo(path).GetAccessControl()` for `Directory.GetAccessControl(path)`
     (.NET's API for it; Windows only);
   - `WebAuthExtensions.cs`: `request.SetUserPrincipal(principal)` under `#if NETFRAMEWORK`. It's System.ServiceModel's
     `HttpRequestMessage` extension (WCF's HTTP integration), which .NET doesn't have; Web API 2 gets the principal from
     `HttpContext.User`, set two lines before;
   - `DistinctBy`: Umbraco's own, called explicitly in `umbraco.cms/businesslogic/ContentType.cs` and twice in
     `Umbraco.Web/Editors/EntityController.cs` (CS0121 with .NET's `Enumerable.DistinctBy`; F63);
   - `templateControls/Item.cs`: the `ItemDesigner` class under `#if NETFRAMEWORK` (designers: documented gap; as in
     Umbraco 6).
6. Linux file names (F64):
   - the `.resx` files' linked resources in the case they have on disk (`BlockingWeb.config`, `Web.config`,
     `Packages.config`, `InlineXslt.xsltTemplate`, `JsInitialize.js`, `ServerVariables.js`, and
     `..\..\Umbraco.Web.UI\Views\Web.config`): MSB3103;
   - `Web.config` for `web.config` where Umbraco opens the site's (`GlobalSettings.cs` three times, `DatabaseContext.cs`,
     `ConfigureMachineKey.cs`) and the `Views` folder's (`RenderViewEngine.cs`), as in Umbraco 6.
7. Linux and .NET runtime fixes:
   - `IOHelper.MapPath`: `Path.VolumeSeparatorChar` is `/` on Linux, so `~/...` came back unmapped (F62; Umbraco 6's fix);
   - `MainDom.cs`: the named `Semaphore` (through `AsyncLock`) and named `EventWaitHandle` with which an app domain
     asks the previous one to release the "main domain" are Windows-only on .NET. On Linux the boot failed with
     `PlatformNotSupportedException: The named version of this synchronization primitive is not supported on this
     platform`. Elsewhere than Windows they are now unnamed (process-local); the old worker still releases MainDom when
     the host stops it (`IRegisteredObject.Stop`), but a new worker no longer signals it (F65).
8. `app.toml`: `[setup]` creates the `umbraco7` database; `generated` keeps `config/ClientDependency.config` out of the
   overlay.

Not done, because nothing past the boot can be checked (each is needed once F22 is out of the way):
- **The `umbraco/` and `Umbraco/` folders.** The repository has files in both (188 and 46: `Umbraco/js/installer.app.js`,
  `Umbraco/Views/AuthorizeUpgrade.cshtml` and `Preview/`, `Umbraco/config/lang/{cs,nb,zh_tw}.xml`,
  `Umbraco/PartialViewMacros/Templates/`, `Umbraco/umbraco.aspx`, ...). On Windows they're one folder; on Linux URLs
  under `/umbraco/` map to `umbraco/`, so the others are unreachable. The installer's view is asked for as
  `~/umbraco/install/views/index.cshtml` but is `umbraco/Install/Views/Index.cshtml`. A migration would merge `Umbraco/`
  into `umbraco/` (renames, a small patch) and fix those paths (F64).
- **The installer's `[setup]` steps:** `GET /install/api/GetSetup`, then `POST /install/api/PostPerformInstall` as
  `json` steps (the runner has them), repeated until the response says it's done.
- **The AngularJS client** (`/umbraco/` and `/install/` need its `lib/`, `assets/` and `js/`): `npm install` (which runs
  `bower-installer`) and `gulp` in `src/Umbraco.Web.UI.Client`. Its gulp 3 doesn't run on the Node 24 here
  (`primordials is not defined`); it needs Node 10–12 (or a gulp 4 port, which is beyond a migration). CI would need that
  Node and the build as a step before `build`.

## Smoke

- **Round 6 (webForCore 0.7.0 on .NET 10; under webforcore-host; Linux):** builds; 7/14 checks (0/7 probes and the
  7 forbidden paths), from a new database (`fetch --reset`, `build`, `setup`, `smoke`, as CI runs them). `setup` only
  creates the database.
- Windows (CI, PR #14): 7/14, the same as Linux: F22 stops the boot there too.

Why every probe fails: Umbraco's boot (`Application_Start`) throws, configuring AutoMapper:

```
ERROR Umbraco.Core.UmbracoApplicationBase - An unhandled exception occurred
System.ArgumentException: The type or method has 3 generic parameter(s), but 1 generic argument(s) were provided.
   at System.Reflection.RuntimeMethodInfo.MakeGenericMethod(Type[] methodInstantiation)
   at AutoMapper.TypeInfo.BuildPublicNoArgExtensionMethods(IEnumerable`1 sourceExtensionMethodSearch)
   ...
   at AutoMapper.TypeMapFactory.CreateTypeMap(Type sourceType, Type destinationType, IMappingOptions options, MemberList memberList)
   at AutoMapper.ConfigurationStore.CreateMap[TSource,TDestination]()
   at Umbraco.Web.Models.Mapping.CodeFileDisplayMapper.ConfigureMappings(IConfiguration config, ApplicationContext applicationContext) in .../CodeFileDisplayMapper.cs:line 41
   at AutoMapper.Mapper.Initialize(Action`1 action)
   at Umbraco.Core.CoreBootManager.InitializeModelMappers() in .../CoreBootManager.cs:line 222
   at Umbraco.Core.CoreBootManager.Initialize() in .../CoreBootManager.cs:line 134
   at Umbraco.Web.WebBootManager.Initialize() in .../WebBootManager.cs:line 108
   at Umbraco.Core.UmbracoApplicationBase.Application_Start(Object sender, EventArgs e)
```

That's the first of about 40 mapper classes. `ApplicationContext.Current` stays null, so the OWIN startup that runs as
each `HttpApplication` initialises (`UmbracoDefaultOwinStartup` → `Membership.Providers` →
`MembersMembershipProvider..ctor`) throws a `NullReferenceException`: every request, static files included, is a 500
(a Configuration Error page naming the membership provider; Umbraco's log adds "Resolution is not frozen" for
each). IIS would do the same with a boot that throws. The same
AutoMapper 3.3.1 fails the same way in a plain .NET 10 console app (any map whose destination member name extends a
source member of a type with generic interfaces, such as `string`: `Name` → `NameLength`). Moving to AutoMapper 12
means rewriting Umbraco's mapping layer (static `Mapper`, `IConfiguration`, about 40 mapper classes and their
resolvers), which is beyond a migration, so it stops here.

The probes are what a working, not yet installed Umbraco 7 answers, taken from its source (none has been seen to pass):
- `/`: 302 to the installer;
- `/install/api/GetSetup`: the installer's Web API (JSON);
- `/umbraco/LocalizedText`: an MVC action (JSON from the language files);
- `/umbraco/ping.aspx` (Web Forms) and an ASMX help page;
- ClientDependency's composite handler, and the same stylesheet as a static file.

`/install/` and `/umbraco/` aren't probed: they need the AngularJS client build and the folder merge (above).
The forbidden paths (`/web.config`, `/bin_dotnet/...`, `App_Data`, ...) are 404s.

## Open problems

- F22, app/environment: AutoMapper 3.3.1 doesn't run on .NET; it blocks the boot. Upgrading is a rewrite.
- F65, guide gap ([#94](https://github.com/xforcore/webForCore/issues/94)): named semaphores and events are
  Windows-only on .NET. Worked around in `MainDom.cs` (process-local on Linux).
- F66, guide gap ([#95](https://github.com/xforcore/webForCore/issues/95)): Log4Net.Async 2.0.4 can't use webForCore's
  `CallContext`. Logging goes through the file appender directly.
- F37, F61, F62, F63 (as in Umbraco 6): applied; F61 and F62 not verified past the boot.
- F64, app/environment: Windows file names (the `.resx` links, `web.config`, and the `umbraco`/`Umbraco` folders).
- Not exercised: `WindowsIdentity` in `AsynchronousRollingFileAppender` (not configured), `fileHelper.HasAccces` and
  `FilePermissionHelper.HasWritePermissionOnDir` (ACLs: `PlatformNotSupportedException` on Linux), package
  installation's AppDomain, `new SmtpClient()` in `EmailSender` and notifications (ignores `<mailSettings>`).

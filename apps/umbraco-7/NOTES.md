# Umbraco 7: migration notes

Status: blocked (round 0.9.0: 33/33 on Linux in every run, and on Windows in 2 of 3 CI runs; the third, 42027500654, had
32/33, the second content save failing to map, [F168](../../FINDINGS.md#f168)). webForCore 0.9.0, webForCore-thirdparty 0.7.0. Source: https://github.com/umbraco/Umbraco-CMS at `bd60f4e2` (tag
release-7.15.11, the last 7.x release). The repo, licence (MIT), web project and its `<TargetFrameworkVersion>` (v4.5.2)
were checked when the app was added.

Round 6, on webForCore 0.7.0 (.NET 10, under `webforcore-host`): it built, but AutoMapper 3.3.1 stopped the boot (F22).
Round 7, on webForCore 0.8.0, webForCore-thirdparty 0.6.0 and drawingForCore 0.3.0: AutoMapper 3.3.1 is replaced by
webForCore-thirdparty's `webForCore.AutoMapperLegacy` 0.6.0 (AutoMapper 3.x's static API on AutoMapper 12), so Umbraco
boots, installs and serves its back office. Umbraco 7 uses neither WebActivatorEx nor AjaxControlToolkit. Much of the
code is Umbraco 6's (`apps/umbraco-6/`), and so are many of the fixes.

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
     Round 7 removed it again from the three projects that use AutoMapper (step 9).
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
   - the `umbracoDbDSN` connection string is Umbraco's own, empty (round 6 pointed it at the compose SQL Server): with a
     connection string and an empty database, the installer's `GetSetup` fails as it would on IIS (`NewInstallStep`
     queries `umbracoUser`, which doesn't exist yet), so the installer is given the database instead, as a user would
     (`[setup]`, step 8);
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
8. `app.toml`: `[setup]` creates the `umbraco7` database and runs the installer's JSON API and a log-in ("Smoke");
   `generated` keeps `config/ClientDependency.config` out of the overlay.
9. **AutoMapper (round 7, F22):** AutoMapper 3.3.1 replaced by webForCore-thirdparty's `webForCore.AutoMapperLegacy`
   0.6.0 (which brings AutoMapper 12.0.1), as its README says, in `Umbraco.Core`, `Umbraco.Web` and
   `umbraco.businesslogic` (and `Umbraco.Web.UI`, which only referenced it):
   - `<PackageReference Include="webForCore.AutoMapperLegacy" Version="0.6.0" />` for `AutoMapper` 3.3.1;
   - its global usings as `<Using>` items in the three projects: `AutoMapperLegacy`, `Mapper` =
     `AutoMapperLegacy.Mapper`, and (Core and Web) `MapperConfiguration` = Umbraco's own
     `Umbraco.Core.Models.Mapping.MapperConfiguration` (AutoMapper 12 has a type of that name: CS0104 in
     `IdentityModelMappings.cs` and the Web mappers). Umbraco has no 3.x `Profile`s, so no `Profile` alias;
   - `<LangVersion>7.3</LangVersion>` removed (C# 14, `net10.0`'s default): the package gives 3.x's
     `ResolutionContext` members as extension properties;
   - the two classes that implement 3.x's `ITypeConverter<,>` directly (`ContentPropertyBasicConverter<T>`,
     `MemberModelMapper.MembershipUserTypeConverter`) name `AutoMapperLegacy.ITypeConverter<,>`, as the README says;
   - the other 87 files that name AutoMapper (of 90) compile unchanged, and everything else in the three projects compiles
     with C# 14 too;
   - **one mapping changed until round 0.9.0 (F104, against webForCore-thirdparty; fixed in webForCore-thirdparty
     0.7.0, and the change is gone):** `ContentTypeModelMapperExtensions.cs`,
     `MapPropertyGroupBasicToPropertyGroupDisplay`, maps `Properties` with `Ignore()` and an `AfterMap` that assigns the
     same `Select(Mapper.Map<TPropertyTypeDisplay>)` (as a list) instead of `MapFrom`. With `MapFrom`, AutoMapper 12
     maps the `IEnumerable<MemberPropertyTypeDisplay>` again, element by element, finds the `PropertyTypeBasic ->
     PropertyTypeDisplay` map for the elements and fails building the configuration, so the first `Mapper.Map`
     anywhere (the back-office user store, in the installer's last step) threw and the install couldn't finish.
   - **webForCore-thirdparty 0.7.0 (round 0.9.0):** the three projects reference `webForCore.AutoMapperLegacy` 0.7.0,
     and Umbraco installs and maps without the F104 change.
10. **C# 14 in expression trees (round 0.9.0, F165):** `ExpressionVisitorBase.VisitMethodCall` (Umbraco's LINQ to SQL
    for PetaPoco) reads `MemoryExtensions.Contains(op_Implicit(array), x)` as `Enumerable.Contains(array, x)`, so it
    builds its `IN (...)` as before. C# 14 (which step 9 needs) binds `array.Contains(x)` to the span overload inside
    expression trees too, and the visitor threw `No logic supported for op_Implicit` for every query with one: saving
    content in the back office (its permission check) was a 500. The guide's fix, a pinned `<LangVersion>` (F83),
    isn't open here.

Not done:
- **The `umbraco/` and `Umbraco/` folders.** The repository has files in both (188 and 46: `Umbraco/js/installer.app.js`,
  `Umbraco/Views/AuthorizeUpgrade.cshtml` and `Preview/`, `Umbraco/config/lang/{cs,nb,zh_tw}.xml`,
  `Umbraco/PartialViewMacros/Templates/`, `Umbraco/umbraco.aspx`, ...). On Windows they're one folder; on Linux URLs
  under `/umbraco/` map to `umbraco/`, so the others are unreachable (`/umbraco/umbraco.aspx` is a 404 on Linux). The
  installer's view is asked for as `~/umbraco/install/views/index.cshtml` but is `umbraco/Install/Views/Index.cshtml`.
  A migration would merge `Umbraco/` into `umbraco/` (renames, a small patch) and fix those paths (F64). Nothing probed
  needs them: the back office's page, its API and the installer's API work without.
- **The AngularJS client** (`/umbraco/` and `/install/` need its `lib/`, `assets/` and `js/`): `npm install` (which runs
  `bower-installer`) and `gulp` in `src/Umbraco.Web.UI.Client`. Its gulp 3 doesn't run on the Node 24 here
  (`primordials is not defined`); it needs Node 10–12 (or a gulp 4 port, which is beyond a migration). CI would need that
  Node and the build as a step before `build`.

## Smoke

- **Round 6 (webForCore 0.7.0; Linux and Windows, CI PR #14):** builds; 7/14 checks (0/7 probes and the 7 forbidden
  paths): AutoMapper 3.3.1 threw in the boot (below), so every request was a 500. **Round 7 (0.8.0), unchanged
  overlay:** 7/14 on both.
- **Round 7, with webForCore.AutoMapperLegacy 0.6.0 (Linux):** from a new database (`fetch --reset`, `build`,
  `setup`, `smoke`, as CI runs them), `setup` passes 5/5 steps and `smoke` 21/21 (14 probes and the 7 forbidden paths),
  also under `LANG=C` (F67). Windows (CI run 41919373974, PR #26): setup 5/5, 21/21, the same as Linux.
- Without the F104 change (step 9), the install got to its last step (`UmbracoVersion`) and failed there on every
  retry: setup 1/2, and the uninstalled site's probes.
- **Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0; Linux):** without F104's change, setup 5/5 and 21/21
  as before. Then verbs: setup makes a template and a document type through the back office's API (7 steps), and 12
  probes (33 in all; 7 of them POST or DELETE) make, publish, edit, unpublish, delete and purge a page; they needed
  step 10 (F165). 33/33 twice in a row on the same database, from a new one (`fetch --reset`, `build`, `setup`,
  `smoke`).
- **Round 0.9.0, CI:** Linux 33/33 in runs 42020729339, 42022957921, 42027500654 and 42032474244; Windows 33/33 in
  42020729339 and 42032474244, and 32/33 in 42027500654: the second `Content/PostSave` (the page saved again under a
  new name) answered 500, "Error mapping types. IContent -> ContentItemDisplay" ([F168](../../FINDINGS.md#f168), cause
  not found; Umbraco's own log, which would have the inner exception, isn't among CI's logs). Blocked until it's
  understood.

What round 6 saw (F22): Umbraco's boot threw in `CoreBootManager.InitializeModelMappers` → `Mapper.Initialize` →
`CodeFileDisplayMapper.ConfigureMappings` line 41, `ArgumentException: The type or method has 3 generic parameter(s),
but 1 generic argument(s) were provided` (AutoMapper 3.3.1's `TypeInfo.BuildPublicNoArgExtensionMethods`, scanning
`System.Linq.Enumerable`). `ApplicationContext.Current` stayed null, so the OWIN startup's membership provider threw
for every request. With webForCore.AutoMapperLegacy the about 40 mapper classes register unchanged; the maps are built
at the first `Mapper.Map`.

The setup, as Umbraco's AngularJS installer does it (`app.toml`):
- `GET /install/api/GetSetup` (the install steps), then `POST /install/api/PostPerformInstall` with a fixed install id
  and the instructions for the steps that take any: `DatabaseConfigure` (SQL Server, `127.0.0.1,14330`, `umbraco7`,
  sa), `User` (the admin: `admin@example.com` / `Smoke_test_1234`) and `StarterKitDownload` (the empty id: no starter
  kit, which would come from our.umbraco.org). Each post runs the next step: Permissions, DatabaseConfigure (writes the
  connection string to `web.config`, which restarts the application), DatabaseInstall (the schema and default data),
  User, UmbracoVersion (writes `umbracoConfigurationStatus`, another restart), then `"complete":true`. The step runs
  again, 5 s apart, until that answer (`retries = 30`); the post after a restart reads the steps' status from
  `App_Data/TEMP/Install/install_<id>.txt`, which is why the id is fixed. It takes about half a minute.
- The log-in: `POST /umbraco/backoffice/UmbracoApi/Authentication/PostLogin` (JSON, as the back office's client sends
  it) answers the user and sets the back-office cookie; `GetCurrentUser` with that cookie is the admin.
- A stylesheet, `Smoke`, made with the legacy create dialog (`/umbraco/create.aspx`, a Web Forms postback checked by
  the back-office cookie): it writes `css/Smoke.css` and answers with the script that opens its editor.
- Then, with the X-UMB-XSRF-TOKEN header (the UMB-XSRF-TOKEN cookie's value, captured from the log-in, as the back
  office's client sends it; the runner's steps and probes can send headers since round 0.9.0): a template (`SmokePage`,
  `Template/PostSave`, a Razor view that prints "Rendered by Umbraco 7: " and the page's name) and a document type
  that uses it (`smokePage`, `ContentType/PostSave`). Umbraco 6 made them with Web Forms pages that 7 no longer has.

The anonymous probes:
- `/`: Umbraco's "no published content" page (`config/splashes/noNodes.aspx`, Web Forms, through Umbraco's module);
- `/umbraco/`: the back office's page (an MVC view, `BackOfficeController`) with its server variables. Its AngularJS
  client (`lib/`, `js/`, `assets/`) isn't built (below), so in a browser it's a blank page;
- `/install/`: a 302 to `/umbraco/` once installed;
- Web API: `Authentication/IsAuthenticated` (`false`) and a back-office controller (`Section/GetSections`, 401 JSON);
- MVC: `/umbraco/LocalizedText` (the language file as JSON) and `/umbraco/Application` (the back office's script
  loader);
- SignalR: the back office's hubs proxy (`/umbraco/BackOffice/signalr/hubs`);
- Web Forms `ping.aspx`; ASMX: a service's help page and a script service's ASP.NET AJAX proxy;
- ClientDependency's composite handler (minifying a stylesheet), that stylesheet as a static file, and the stylesheet
  the setup made (`/css/Smoke.css`).

The verbs (round 0.9.0), the back office's API as its AngularJS client calls it, in one cookie jar: the log-in (a JSON
POST; the `UMB-XSRF-TOKEN` cookie captured), a page saved and published without the `X-UMB-XSRF-TOKEN` header
(refused, 417) and with it (the content editor's multipart `Content/PostSave`, `contentItem` as JSON; its id
captured), the page on the front end at `/` (Umbraco's MVC route with setup's template), saved again under a new name
(the front end shows it), unpublished (`PostUnPublish`; `/` is the "no nodes" page again), deleted (`DELETE
Content/DeleteById`; `GetById` shows it trashed) and the recycle bin emptied (`DELETE Content/EmptyRecycleBin`;
`GetById` is then a 404). A run leaves the database as it found it.

The forbidden paths (`/web.config`, `/bin_dotnet/...`, `App_Data`, ...) are 404s.

Other things seen:
- The installer's last step calls `InstallHelper.InstallStatus`, which reports the install to our.umbraco.org with a
  "Web Reference" SOAP client (`org.umbraco.update.CheckForUpgrade`). Its constructor reads its URL from a Visual
  Studio settings class, and webForCore's `ApplicationSettingsBase` throws `TypeLoadException` (F105, webForCore bug).
  Umbraco catches and logs it ("An error occurred in InstallStatus trying to check upgrades"), so the install goes on;
  the back office's upgrade check (`CheckForUpgrade.asmx` → the same client) would fail the same way.
  Fixed in 0.9.0 (round 0.9.0): the client is made and calls our.umbraco.org, which answers a 301 (Cloudflare) that
  Umbraco logs under the same message and goes on.
- Umbraco's log after the install has nothing else but "Umbraco has no content".
- Locally the machine's inotify limit (128 instances per user, shared with other apps' servers) stopped some runs
  before the application started ("Failed to start monitoring changes"); runs with
  fewer servers alongside pass (webForCore#145, #152; not this app's problem).

## Open problems

- F168: Windows, intermittent: the content save's mapping fails (1 of 3 runs).
- F165, against webForCore-thirdparty (an issue to raise there): the package needs C# 14, whose span conversions
  change `array.Contains` in expression trees, which Umbraco's SQL visitor rejected. Handled in the visitor (step 10).
- F104 (fixed in webForCore-thirdparty 0.7.0, [webForCore-thirdparty#11](https://github.com/xforcore/webForCore-thirdparty/issues/11)):
  the mapping change is gone (step 9).
- F105 (fixed in 0.9.0, [#154](https://github.com/xforcore/webForCore/issues/154)): `ApplicationSettingsBase`.
- F22: resolved for Umbraco 7 by webForCore-thirdparty's `webForCore.AutoMapperLegacy`.
- F65, guide gap ([#94](https://github.com/xforcore/webForCore/issues/94)): named semaphores and events are
  Windows-only on .NET. Worked around in `MainDom.cs` (process-local on Linux).
- F66, guide gap ([#95](https://github.com/xforcore/webForCore/issues/95)): Log4Net.Async 2.0.4 can't use webForCore's
  `CallContext`. Logging goes through the file appender directly.
- F37, F61, F62, F63 (as in Umbraco 6): applied. F62's `IOHelper.MapPath` fix is exercised by every request; F61's
  `umbracoBinDirectory` by the create dialog.
- F64, app/environment: Windows file names (the `.resx` links, `web.config`, and the `umbraco`/`Umbraco` folders).
- The AngularJS client isn't built: `npm install` (which runs `bower-installer`) and `gulp` in
  `src/Umbraco.Web.UI.Client`. Its gulp 3 doesn't run on the Node 24 here (`primordials is not defined`); it needs
  Node 10–12 (or a gulp 4 port, which is beyond a migration), and CI would need that Node and the build before `build`.
  Without it the back office and installer pages load no script in a browser; their server side is what's probed.
- Not exercised: `WindowsIdentity` in `AsynchronousRollingFileAppender` (not configured), `fileHelper.HasAccces` and
  `FilePermissionHelper.HasWritePermissionOnDir` (ACLs: `PlatformNotSupportedException` on Linux; the Permissions
  install step passed on Linux), package installation's AppDomain, `new SmtpClient()` in `EmailSender` and
  notifications (ignores `<mailSettings>`).

# Umbraco 8: migration notes

Status: migrated (CI run 41920542509: 21/21 on Linux and Windows). Source: https://github.com/umbraco/Umbraco-CMS at `4fbf8020` (tag release-8.18.15, the last
8.x release). The repo, licence (MIT), web project and its `<TargetFrameworkVersion>` (v4.7.2) were checked when the
app was pinned.

Round 8, on webForCore 0.8.0 (.NET 10, under `webforcore-host`), with drawingForCore 0.3.0. No webForCore-thirdparty
packages: Umbraco 8 uses neither WebActivatorEx nor AjaxControlToolkit, and it has its own object mapper (no AutoMapper,
which blocks Umbraco 7, F22). Much of the code is Umbraco 7's (`apps/umbraco-7/`), and so are several fixes.

## Assessment

- **Frameworks:**
  - MVC 5: the front end's templates (`RenderMvcController`), the back office's shell and script actions
    (`BackOfficeController`), the installer's page, surface controllers;
  - Web API 2: the back office's API (`/umbraco/backoffice/UmbracoApi/...`) and the installer's;
  - Web Forms: the "no content" splash page (`config/splashes/noNodes.aspx`);
  - OWIN (`UmbracoDefaultOwinStartup`), ASP.NET Identity 2 (back-office users, OWIN cookies), SignalR 2 (the preview
    hub, `/umbraco/BackOffice/signalr`).
  - The back office and installer UIs are an AngularJS application built from `src/Umbraco.Web.UI.Client` with npm and
    gulp into `Umbraco/{lib,js,assets,views}` (not in the repository; as for Umbraco 7, not built here).
- **Target:** .NET 4.7.2 (`<httpRuntime targetFramework="4.7.2">`, `fcnMode="Single"`).
- **Projects:** the web project `src/Umbraco.Web.UI` and four libraries: `Umbraco.Core`, `Umbraco.Web`,
  `Umbraco.Examine`, `Umbraco.ModelsBuilder.Embedded`.
- **Blockers flagged by `assess`:** `WindowsIdentity` in `Umbraco.Web/Security/FixWindowsAuthMiddlware.cs` (only acts
  when a request has a Windows identity: not with forms/OWIN authentication). Not a blocker.
- **Notices:** `new SmtpClient()` in `EmailSender` and `NotificationService` (mail; not exercised).
- **Dependencies:**
  - (a) replaced by webForCore: MVC 5.2.7, Web API 5.2.7, Katana 4.0.1 (Owin, Host.SystemWeb, Security, Cookies,
    OAuth), Identity 2.2.2, SignalR 2.4, Microsoft.Web.Infrastructure, Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
  - (b) managed, kept: ClientDependency 1.9.10 (+ -Mvc5), Examine 1.2.2, Lucene.Net 3.0.3, LightInject 5.4 (+ .Mvc,
    .WebApi, .Web), NPoco 3.9.4, Serilog 2.10 and its sinks, ImageProcessor 2.9 / .Web 4.12, HtmlAgilityPack 1.8,
    MessagePack 2.2, K4os.Compression.LZ4, Markdown, MiniProfiler 4.0, Semver, Newtonsoft.Json (12.0.1 → 13.0.4),
    Microsoft.CodeAnalysis.CSharp (2.10.0 → 5.9.0, F94);
  - (c) .NET Framework-only: Umbraco.SqlServerCE 4.0 (native SQL CE: compiled against, not used; the site uses SQL
    Server), CSharpTest.Net.Collections 14.906 (NuCache's file store: doesn't run on .NET, F93).
- **Database:** SQL Server (the compose one, database `umbraco8`) or SQL CE. Umbraco 8.12+ installs itself unattended
  when `Umbraco.Core.RuntimeState.InstallUnattended` is set, which avoids the AngularJS installer.

Go, with SQL Server and the unattended install.

## Migration

1. `scripts/convert.py` on the web project and the four libraries. Then:
   - `webForCore.Web.Http.WebHost` in `Umbraco.Web` and the web project (convert.py dropped `Microsoft.AspNet.WebApi`
     without adding it), and `webForCore.Net.Http.Formatting` and `webForCore.Microsoft.Owin` in `Umbraco.Core` (for
     `Microsoft.AspNet.WebApi.Client` and `Microsoft.Owin`);
   - `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in the libraries (`docs/MIGRATING.md` section 2);
   - `<LangVersion>13</LangVersion>` where the projects set `latest`, 7.3 in ModelsBuilder (which set 7.3);
   - `Microsoft.CodeAnalysis.CSharp` 2.10.0 → 5.9.0 in ModelsBuilder and the web project: NU1107 against
     webForCore.Compilers' Roslyn (F94);
   - `Microsoft.SourceLink.GitHub` 1.0.0-beta2 removed (a build-time tool): it failed the build reading the checkout's git
     repository ("Unsupported repository version 1");
   - `Umbraco.SqlServerCE`: `lib/net472` with a `build/` folder restores without its assembly (`docs/MIGRATING.md`
     section 3): its DLL by `HintPath` (`GeneratePathProperty`);
   - NPoco 3.9.4: its .NET Standard build has no `SqlBulkCopyHelper` (CS0103 in `NPocoDatabaseExtensions-Bulk.cs`):
     its `net45` build, in every project that gets it, the web project included (`docs/MIGRATING.md` section 3);
   - `CSharpTest.Net.Collections` → `Umbraco.CSharpTest.Net.Collections` 14.906.1403.1085, Umbraco's .NET build of the
     same assembly (F93, below).
2. `Umbraco.Web.UI.csproj`: a target that makes `Web.config` (gitignored) from `web.Template.config` with
   `web.Template.Release.config`'s transform, and `config/{ClientDependency,umbracoSettings,serilog,serilog.user}.config`
   from their `.Release.config`, before the build, as the .NET Framework project's `BeforeBuild` did (it had
   `Config\...`; the folder is `config`). Each is made once. The `BelleBuild` target (the AngularJS client, through
   PowerShell) isn't carried over.
3. `web.Template.config`:
   - `<system.codedom>` removed (the DotNetCompilerPlatform entries; the guide's step);
   - `umbracoDbDSN` points at the compose SQL Server (`TrustServerCertificate=True`);
   - `Umbraco.Core.RuntimeState.InstallUnattended` = `true` and `Umbraco.Core.ConfigurationStatus` = `8.18.15` (the
     unattended install runs only when they match); the user comes from the `UnattendedUser*` variables (`app.toml`'s
     `env`);
   - `Umbraco.Core.MainDom.Lock` = `SqlMainDomLock`: the default lock (`MainDomSemaphoreLock`) is a named `Semaphore`
     and `EventWaitHandle`, Windows-only on .NET (F65, which Umbraco 7 needed a code change for); Umbraco 8's own SQL
     Server lock works across processes everywhere;
   - `Umbraco.Core.Path` = `~/Umbraco`: the folder is `Umbraco`, so `~/umbraco/config/lang` etc. weren't found on Linux
     (F64). Routes are case-insensitive, so `/umbraco/` URLs still work;
   - an explicit `<machineKey>` (generated, test-only; `docs/MIGRATING.md` section 5).
4. `config/serilog.Release.config`: the log path with `/` (it was `%BASEDIR%\App_Data\Logs\...`, which on Linux is a file
   named `\App_Data\Logs\UmbracoTraceLog...json` in the site folder).
5. Compile fixes:
   - `ReflectionUtilities-Unused.cs` under `#if NETFRAMEWORK` (`Expression.CompileToMethod`,
     `AssemblyBuilderAccess.Save`; nothing uses it, as its name says);
   - `SqlAzureTransientErrorDetectionStrategy.cs`: the `EntityException` check (EF 1–4) under `#if NETFRAMEWORK`, as in
     Umbraco 7;
   - `DistinctBy`: Umbraco's own, called explicitly in `EntityController.cs` (twice) and `UrlProviderExtensions.cs`
     (CS0121 with .NET's `Enumerable.DistinctBy`; F63);
   - `FilePermissionHelper.cs`: `new DirectoryInfo(path).GetAccessControl()` for `Directory.GetAccessControl(path)`;
   - `WebAuthExtensions.cs`: `request.SetUserPrincipal(principal)` (System.ServiceModel's) under `#if NETFRAMEWORK`, as
     in Umbraco 7.
6. Linux file names (F64):
   - the `.resx` files' linked resources in their case on disk (`BlockingWeb.config`, `JsInitialize.js`,
     `PreviewInitialize.js`, `ServerVariables.js`, `..\..\Umbraco.Web.UI\Views\Web.config`): MSB3103;
   - `Web.config` for `web.config` where Umbraco opens the site's (`GlobalSettings.cs` twice, `DatabaseBuilder.cs`,
     `ConfigureMachineKey.cs`) and the `Views` folder's (`RenderViewEngine.cs`);
   - `InstallController`: `Install/Views/Index.cshtml` (it asked for `install/views/index.cshtml`).
7. Linux and .NET fixes:
   - `IOHelper.MapPath`: `Path.VolumeSeparatorChar` is `/` on Linux, so `~/...` came back unmapped and Umbraco wrote its
     type cache under a folder named `~` (F62; Umbraco 6 and 7's fix);
   - `SystemDirectories.Bin` = `~/bin_dotnet` (the type loader hashes it to know when to rescan; F61);
   - NuCache's file store: the first request after the install failed with a `NullReferenceException` in
     `CSharpTest.Net.IO.TransactedCompoundFile.FlushStream`: CSharpTest.Net.Collections 14.906 reads `FileStream`'s
     private `_handle` and calls kernel32's `FlushFileBuffers`. Umbraco's .NET build of the library (for Umbraco 9+)
     flushes with `FileStream.Flush(true)` (F93).
8. `app.toml`: `[setup]` creates the `umbraco8` database; its first request installs Umbraco; then the back office's
   log-in, a JSON post to `Authentication/PostLogin` (Web API, Identity, OWIN cookies), as the AngularJS client sends it.

Not done:
- **Content.** Creating a document type and a page goes through the back office's API, which checks the
  `X-UMB-XSRF-TOKEN` header against its cookie; setup steps can't send headers, so the site has no content and the
  front end's MVC rendering isn't probed (only its "no content" page).
- **The AngularJS client** (`/umbraco/` and `/install/` UIs): as for Umbraco 7, it needs `npm install` and `gulp` in
  `src/Umbraco.Web.UI.Client`, which isn't part of a migration.

## Smoke

Linux, here (`fetch --reset`, `build`, `setup`, `smoke`, also under `LANG=C LC_ALL=C`): **21/21** (14 probes and the runner's 7
forbidden paths). The setup installs Umbraco unattended and logs in. The worker holds 20 inotify instances.

The probes: `/` and `noNodes.aspx` (Web Forms: Umbraco's "no published content" page), `/install/` (302: installed),
the back office's shell page and `Application` script action (MVC), `LocalizedText` (MVC, JSON from the language
files), `Authentication/IsAuthenticated` and `GetCurrentUser` (Web API: anonymous, and a 401 JSON), the SignalR preview
hub's proxies and negotiation, ClientDependency's `DependencyHandler.axd`, two static files and `/Views/Web.config`
(404).

**CI run 41920542509 (Linux and Windows): 21/21 on both.**

### Content through the back office's API (C# runner, November 2026)

With the C# runner's requests (README.md, "Requests"), setup now makes content as the AngularJS back office would: it
logs in (capturing the `UMB-XSRF-TOKEN` cookie), then sends that value as `X-UMB-XSRF-TOKEN` with `Template/PostSave`
(a Razor template, written to `Views/SmokePage.cshtml`, which Umbraco's `.gitignore` keeps out of the overlay), `ContentType/PostSave` (a document type using
it) and `Content/PostSave` (a multipart form whose `contentItem` field is the item; `publishNew`), giving a published
home page. Between the unattended install and the saves, setup restarts the application (`restart = true`): Umbraco
composes the back office's services at the runtime level it boots at, so in the process that ran the unattended
install `ContentType/PostSave` fails with "Unable to resolve type: Umbraco.Web.Editors.EditorValidatorCollection".
That is Umbraco's own boot order (`CoreRuntime.Boot` composes, then installs), not webForCore's.

The probes: `/` now renders the home page through the template (MVC); `noNodes.aspx` sends a visitor to it (302); and
seven more probes use the API with verbs: the log-in (JSON `POST`, its XSRF cookie captured), `GetCurrentUser` with the
header (a call without it gets new tokens, as Umbraco's `SetAngularAntiForgeryTokens` sets them), `Content/PostSave`
without the header (417) and with it (published; the page's id and URL captured), the page on the front end, a
`DELETE` of it (`DeleteById`, to the recycle bin) and a 404 for its URL. Each smoke run publishes and deletes a page
of its own. 28 checks in all, all passing on Linux and Windows (CI run 41940185337).

### Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

28/28 on Linux as committed. Then 12 more probes (40 in all; 7 of the new ones POST or DELETE, 11 in all), and the
page's `DELETE` and 404 moved after them: the page is saved again under a new name with `publish` (its URL follows the
name, and the front end shows the new one), unpublished (`PostUnpublish`, JSON; the URL is then a 404), deleted, shown
trashed by `GetById`, and the recycle bin emptied (`DELETE Content/EmptyRecycleBin`; `GetById` is then a 404). Then
the media section: a folder made (`Media/PostAddFolder`, JSON) and listed, a `DELETE` of it refused with Web API's
405 (the media API's `DeleteById` is POST-only), deleted with a `POST`, and the media recycle bin emptied (the folder
is then a 404). A run now leaves no content or media behind. 40/40 on Linux, twice in a row on the same database (a
new one, as CI's). `generated` (the template's view) is gone from `app.toml`: Umbraco's `.gitignore` already keeps it
out, and the C# runner's `save-overlay` fails on an excluded path that git ignores (`git add -N` "paths are ignored").
No overlay change.

## Open problems

- F93, app/environment: CSharpTest.Net.Collections 14.906 doesn't run on .NET (replaced with Umbraco's .NET build).
- F94, guide gap ([#153](https://github.com/xforcore/webForCore/issues/153)): Roslyn's version (NU1107).
- F61, F62, F63, F64, F65 (as in Umbraco 6 and 7): applied; F65 through Umbraco's own SQL Server MainDom lock.
- Not exercised: `new SmtpClient()` (mail), Windows authentication
  (`FixWindowsAuthMiddlware`), the AngularJS UIs.

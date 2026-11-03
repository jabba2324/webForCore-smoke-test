# Umbraco 6: migration notes

Status: migrated (16/16 on Linux; four webForCore bugs and three guide gaps found on the way). Source:
https://github.com/umbraco/Umbraco-CMS at `026805e4` (tag release-6.2.6, the last 6.x release). The repo, licence (MIT),
web project and its `<TargetFrameworkVersion>` (v4.0) were checked when the app was added.

Round 6, on webForCore 0.7.0 (.NET 10, under `webforcore-host`). No webForCore-thirdparty packages: Umbraco 6 uses
neither WebActivatorEx nor AjaxControlToolkit.

## Assessment

- **Frameworks:**
  - Web Forms: the back office (`/umbraco/*.aspx`, about 150 pages and dialogs), the install wizard, master page
    templates for the front end;
  - MVC 4: the front end's Razor templates (`RenderMvcController`), the back office's `RestServices` controllers,
    surface controllers;
  - Web API (4.0): back-office API controllers (`/umbraco/BackOffice/Api/...`) and plugin API controllers;
  - ASMX script services (ASP.NET AJAX) and generic handlers;
  - Web Pages: Razor macros (`umbraco.MacroEngines`).
- **Target:** .NET 4.0 (`<httpRuntime>` has no `targetFramework`, and `requestValidationMode="2.0"`; webForCore
  reproduces 4.5+ behaviour).
- **Projects:** the web project `src/Umbraco.Web.UI` and 13 libraries: `Umbraco.Core`, `Umbraco.Web` (assembly
  `umbraco`), `umbraco.businesslogic`, `.cms`, `.controls`, `.datalayer`, `.editorControls`, `.interfaces`,
  `.macroRenderings`, `.providers`, `umbraco.MacroEngines`, `UmbracoExamine`, `SQLCE4Umbraco`.
- **Blockers flagged by `assess`:**
  - `AppDomain.CreateDomain` sandbox in `umbraco.businesslogic/Utils/TypeResolver.cs:65` (an `[Obsolete]`, unused class);
  - `using System.EnterpriseServices` in `TreeClientService.asmx.cs:10` (unused; webForCore has the type it'd need);
  - `WindowsIdentity` in `Umbraco.Core/Logging/AsynchronousRollingFileAppender.cs` (error paths only),
    `UmbracoRequest.LogonUserIdentity` (a pass-through) and `umbraco.businesslogic/Utils/fileHelper.cs` (an ACL check).
  - None is on a path the app needs; they are compile fixes at most.
- **Notices:** `BinaryFormatter` (Live Editing's context; `Umbraco.Core`'s uses are commented out), `new SmtpClient()`
  (notifications, `library.SendMail`, translation mails: not exercised).
- **Dependencies:**
  - (a) replaced by webForCore: MVC 4, Web API 4, Razor 2, Web Pages 2, Microsoft.Web.Infrastructure,
    System.Net.Http (Microsoft.Net.Http 2.0's types come with .NET);
  - (b) managed, kept: ClientDependency 1.8 (+ -Mvc), Examine 0.1.57, Lucene.Net 2.9.4, HtmlAgilityPack 1.4.6,
    log4net-mediumtrust 2.0, MiniProfiler 2.1, MySql.Data 6.6, Newtonsoft.Json (raised to 13.0.4), SharpZipLib 0.86,
    UrlRewritingNet 2.0, xmlrpcnet 2.5, uGoLive 1.4, Tidy.Net, Microsoft.ApplicationBlocks.Data,
    Microsoft.AspNet.Mvc.FixedDisplayModes, Microsoft.Web.Helpers 1.0;
  - (c) .NET Framework-only: SqlServerCE 4.0 (native SQL CE; compiled against, not used: the site uses SQL Server).
- **Database:** SQL Server (the compose one, database `umbraco6`), MySQL or SQL CE. The schema and data are created by
  the install wizard.

Go, with SQL Server.

## Migration

1. `scripts/convert.py` on the web project and the 13 libraries.
   - It added `webForCore.Microsoft.Web.Mvc` (MVC Futures) for `Microsoft.AspNet.Mvc.FixedDisplayModes`, whose
     assembly is `Microsoft.Web.Mvc.FixedDisplayModes`; the app doesn't use Futures, so it is removed.
   - `<LangVersion>7.3</LangVersion>` in every project: the projects set none (`docs/MIGRATING.md` section 2).
   - `<Deterministic>false</Deterministic>` in every project: the shared `SolutionInfo.cs` has
     `AssemblyVersion("1.0.*")` (CS8357); the converter only looks at each project's own `AssemblyInfo.cs`.
2. `src/NuGet.Config` (new): six packages aren't on nuget.org (NU1101), only on Umbraco's MyGet feed, which its
   `.nuget/NuGet.targets` names (F64).
3. `Umbraco.Web.UI.csproj`: a target that makes `Web.config` (gitignored) from `web.Template.config` before the build,
   as the .NET Framework project's `BeforeBuild` did, with `web.Template.Release.config`'s transform (the configuration
   Umbraco's release ships: no `<trust>`, `debug` off, legacy password encoding). It's made once, as the installer
   writes to it. The development template has `<trust level="Medium">`, which is a Configuration Error on webForCore
   (F57, webForCore bug).
4. `web.Template.config`:
   - `<system.codedom>` removed (what `convert.py` does to a `web.config`): its `Microsoft.CSharp.CSharpCodeProvider`
     entry was an `InvalidCastException` on every page (F58, webForCore bug);
   - `System.Design` and `System.Web.Extensions.Design` removed from `<compilation><assemblies>` (F37);
   - the `umbracoDbDSN` connection string points at the compose SQL Server (`TrustServerCertificate=True`), so the
     install wizard skips its connection form;
   - `umbracoBinDirectory` = `~/bin_dotnet`: the back office's create dialogs load `~/bin/umbraco.dll` (F61, guide gap);
   - `log4net.Config` = `../config/log4net.config`: log4net resolves it against `AppDomain.BaseDirectory`, which is
     `bin_dotnet/` (documented, `docs/MIGRATING.md` section 6), and the `\` didn't work on Linux;
   - an explicit `<machineKey>` (generated, test-only; `docs/MIGRATING.md` section 5): the installer's `web.config`
     write restarts the app in a new worker, and the setup's next postback failed with "Validation of viewstate MAC
     failed";
   - `<globalization culture="en-US" uiCulture="en-US">`: on a Linux machine with no locale (GitHub's Ubuntu runners,
     containers) the culture is the invariant one, Umbraco finds no language file for it, and the back office shows its
     keys (`[login]`) instead of text, so the log-in failed in CI (F67, guide gap).
5. `config/log4net.config`: the log file is `../App_Data/Logs/UmbracoTraceLog.txt`, for the same reason (it was
   `App_Data\Logs\...`, relative to `bin_dotnet/`); without it no log was written.
6. Compile fixes:
   - `Umbraco.Core/Dynamics/ClassFactory.cs`: `AssemblyBuilder.DefineDynamicAssembly` for
     `AppDomain.CurrentDomain.DefineDynamicAssembly` (.NET's API);
   - `Umbraco.Core/.../SqlAzureTransientErrorDetectionStrategy.cs`: the `EntityException` check (System.Data.Entity,
     EF 1–4) under `#if NETFRAMEWORK`;
   - `umbraco.businesslogic/Utils/TypeResolver.cs`: the `AppDomain.CreateDomain` sandbox and its `Unload` under
     `#if NETFRAMEWORK` (the class is obsolete; it then works in the current domain, its partial-trust path);
   - `SQLCE4Umbraco/SqlCEHelper.cs`: ten `ExecuteReader("...")`/`ExecuteNonQuery("...")` calls with no parameters pass
     `new IParameter[0]`: CS0121 between the `params IParameter[]` and `params P[]` overloads with Roslyn (F63);
   - `umbraco.cms/businesslogic/ContentType.cs`: `Umbraco.Core.EnumerableExtensions.DistinctBy(...)` called explicitly:
     CS0121 with .NET 6's `Enumerable.DistinctBy` (F63);
   - `Umbraco.Web`: `using System.Windows.Forms.VisualStyles` (`protectPage.aspx.cs`) and
     `System.Web.UI.Design.WebControls` (`EditMember.aspx.cs`), both unused, and the `ItemDesigner` class
     (`templateControls/Item.cs`) under `#if NETFRAMEWORK` (designers: documented gap);
   - `LiveEditing/DefaultLiveEditingContext.cs`: webForCore's `BinaryFormatter` (SYSLIB0011), as the guide says.
7. Linux file names (F64):
   - the `.resx` files' linked SQL scripts and resources in the case they have on disk (`Sql\Total.sql` for
     `sql\total.sql`, `Packages.config`, `InlineXslt.xsltTemplate`): MSB3103;
   - `Web.config` for `web.config` where Umbraco opens the site's and the `Views` folder's (`DatabaseContext.cs`,
     `GlobalSettings.cs`, `RenderViewEngine.cs`, `MediaUploader.ashx.cs`): the installer failed with
     `FileNotFoundException: .../web.config`, and the view engine would write a second `views/web.config`.
8. Linux path and URI APIs (F62, guide gap):
   - `Umbraco.Core/IO/IOHelper.cs`: `MapPath` took `path[1] == Path.VolumeSeparatorChar` (`/` on Linux) for "already a
     physical path", so `~/...` came back unmapped: the back office showed `[login]` for "Login" and Umbraco made its
     folders under a folder named `~`. It now checks for `:` and, on Linux, for the site's folder;
   - `umbraco/login.aspx.cs`: `IsUrlLocalToHost` skips file URIs: `/umbraco/Default.aspx` is an absolute `file:` URI on
     Linux, so every redirected log-in (anything under `/umbraco/`) was a 500 "attempt to redirect to another domain".
9. `app.toml`: `[setup]` (database `umbraco6`; the install wizard; then, in the back office, a document type, its MVC
   template's Razor, and a published home page); `generated` keeps `config/ClientDependency.config` out of the
   overlay (the wizard's last step rewrites its version).

## Smoke

- **Round 6 (webForCore 0.7.0 on .NET 10; under webforcore-host; Linux):** 15/16 checks on Linux (9 probes and the 7
  forbidden paths). Setup stops at the install wizard's JSON step (1/2 steps), so the site is never installed and the
  home page probe gets a 302 to `/install/` (F64). Every other probe passes on the uninstalled site.
- **With the runner's `json` setup steps** (added for this app): from a new database, `build`, `setup` (9/10 steps,
  the `may_fail` one catching F59) and `smoke` pass 16/16 on Linux.
- **Second CI run (PR #14), with the fixes:** 16/16 on Linux and on Windows.
- **First CI run (PR #14):** 16/16 on Windows; 14/16 on Linux, where the runner's `C` locale left the back office
  without its text (F67) and the log-in step was caught by the F59 restart. With the culture set and the log-in's
  `retries`, 16/16 locally under `LANG=C`, from a fresh checkout and database.

The probes cover:
- the published home page, rendered by Umbraco's `RenderMvcController` with the template's Razor view (MVC);
- the back office's log-in page (Web Forms; its text comes from the language files) and its redirect to it;
- `ping.aspx`;
- a back-office Web API controller (401 JSON);
- an ASMX script service's JavaScript proxy and a service's help page;
- ClientDependency's composite handler (`DependencyHandler.axd`, minifying a stylesheet);
- a static file.

Also checked by hand on the installed site (not probes, as they need a log-in or content):
- logging in, the back office's main page, dashboard, content tree (`TreeDataService.ashx` JSON), create dialogs;
- Web API and MVC `RestServices` actions when logged in (`ExamineManagementApi`, `CoreStrings/ToSafeAlias`);
- creating a document type, a template and content, and publishing it; an MVC (Razor) template and a Web Forms master
  page template both render;
- the install wizard's steps, as `[setup]` runs them.

Setup notes:
- The database step's install button is an ASP.NET AJAX page method (`InstallerRestService.aspx/Install`): only a JSON
  POST reaches it, so it's a `json` step (as is the template save, which Umbraco's editor sends as JSON to an MVC
  action).
- The admin user step is a `LinkButton`: with a user agent the browser files don't know, ASP.NET renders no
  `__doPostBack`/`__EVENTTARGET` (as on IIS), so the step posts `__EVENTTARGET` itself.
- The starter kits come from our.umbraco.org, so that step is skipped; the home page is made in the back office
  instead.
- After the wizard's last step, the next request fails once: the configured start creates `App_Code`, the host
  recycles, and the old worker's compilation loses its `App_global.asax` assembly (F59, webForCore bug). When the
  restart comes varies: a `may_fail` request waits for it, and the log-in after it has `retries` (in the first CI run
  the restart missed the first and caught the log-in's GET of the form).

Other differences seen:
- `install/steps/theend.ascx` is Windows-1252 without a BOM: `You’re done` renders `You�re done` (F60, webForCore bug).
- A URL with no content (`/no-such-page.aspx`) is a `NullReferenceException` in `library.GetCurrentNotFoundPageId`:
  the repository's development `config/umbracoSettings.config` has no `<error404>` (its `.Release.config` does). The
  same code fails the same way on IIS, so it isn't probed.
- `bin_dotnet/App_Data` appears (empty) after a run, as in other apps' outputs (bonobo-git-server, mvcforum, roadkill):
  not Umbraco's doing.

## Open problems

- F57, webForCore bug ([#87](https://github.com/xforcore/webForCore/issues/87)): partial trust levels are a
  Configuration Error. Avoided by the release transform, which the migration uses anyway.
- F58, webForCore bug ([#88](https://github.com/xforcore/webForCore/issues/88)): `<system.codedom>` with
  `Microsoft.CSharp.CSharpCodeProvider`. Removed, as the converter does.
- F59, webForCore bug ([#89](https://github.com/xforcore/webForCore/issues/89)): a request caught by an `App_Code`
  recycle fails with CS0006.
- F60, webForCore bug ([#90](https://github.com/xforcore/webForCore/issues/90)): ANSI files read as UTF-8.
- F67, guide gap ([#97](https://github.com/xforcore/webForCore/issues/97)): with no locale the culture is invariant;
  fixed by setting it in `<globalization>`.
- F61, F62, F63, guide gaps ([#91](https://github.com/xforcore/webForCore/issues/91),
  [#92](https://github.com/xforcore/webForCore/issues/92), [#93](https://github.com/xforcore/webForCore/issues/93)).
- F64, app/environment: the MyGet feed, SQL CE, Windows file names, and the install wizard's JSON page method (a
  `json` setup step since the runner has them).
- Not exercised: `WindowsIdentity.GetCurrent()` in the log4net appender's error paths and `fileHelper.HasAccces`
  would throw `PlatformNotSupportedException` on Linux; `new SmtpClient()` in notifications ignores `<mailSettings>`.

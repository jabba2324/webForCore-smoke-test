# nopCommerce 3.90: migration notes

Status: migrated: 22/23 probes on Linux (F86), 23/23 on Windows (intermittent, F89), webForCore 0.8.0.
Source: https://github.com/nopSolutions/nopCommerce at `12d1f013`
(tag `release-3.90`, the last MVC 5 release before nopCommerce moved to ASP.NET Core). Licence: the nopCommerce Public
License 3.0, GPL-3.0 with an attribution term ("powered by nopCommerce" on every page, which the store keeps). The
web project (`src/Presentation/Nop.Web`) targets .NET Framework 4.5.1 with System.Web.

Migrated on webForCore 0.8.0 (.NET 10, under `webforcore-host`) and drawingForCore 0.3.0. No webForCore-thirdparty
packages: WebActivator 1.5.2 is referenced by Nop.Web.Framework but nothing in the app has a WebActivator start
method, so `webForCore.WebActivator` isn't needed.

## Assessment

- **Frameworks:** MVC 5 (the store, and the administration in its own project, `Nop.Admin`, an MVC area built into the
  site's `bin`), Web API and OWIN referenced but unused (`owin:AutomaticAppStartup` false), EF 6 code first.
- **Projects:** `Nop.Web`, `Nop.Admin` (`Nop.Web/Administration`), `Nop.Web.Framework`, `Nop.Core`, `Nop.Data`,
  `Nop.Services`, and 20 plugins (`src/Plugins`), each built into `Nop.Web/Plugins/<name>/`. The plugin manager
  (`Nop.Core/Plugins/PluginManager.cs`, a `PreApplicationStartMethod`) copies each plugin's DLLs into the code
  generation folder (`AppDomain.DynamicDirectory`), loads them and adds them to `BuildManager`; the plugins' views are
  compiled at run time.
- **Target:** .NET 4.5.1 (`<httpRuntime targetFramework="4.5.1">`).
- **Blockers flagged by `assess`:** `windows-auth` in `InstallController.cs:282-294`, `Administration/CommonController.cs:425,444`
  and `Nop.Web.Framework/Security/FilePermissionHelper.cs:33`. Not authentication: the installer and the warnings page
  check the site's folders' ACLs for `WindowsIdentity.GetCurrent()`, and print its name when a check fails. A
  platform fix (below).
- **Notices:** `new SmtpClient()` in `EmailSender.cs:128`: it sets host, port and credentials from the store's e-mail
  account, so `<mailSettings>` isn't needed.
- **Dependencies:**
  - (a) replaced by webForCore: MVC 5.2.3, Web API 5.2.3, Razor and Web Pages 3.2.3, Web.Optimization 1.1.3,
    WebGrease, Katana 3.0.1, Microsoft.Web.Infrastructure;
  - (b) managed, kept: Autofac 4.4 (+ Mvc5), **AutoMapper 5.2** (F22: doesn't run on .NET), EntityFramework 6.1.3
    (6.5.1), FluentValidation 6.4 (+ MVC5), Newtonsoft.Json 9.0.1 (13.0.4), EPPlus 4.1, iTextSharp 5.5, MaxMind GeoIP2,
    StackExchange.Redis and RedLock (unused by default), WindowsAzure.Storage 8.1 (unused by default), MiniProfiler 3,
    **System.Linq.Dynamic 1.0.7** (F85: net40, needs .NET Framework's System.Data.Entity);
  - (c) .NET Framework-only, tested: ImageResizer 4.0.5 (thumbnails; works with drawingForCore), MiniProfiler.MVC4,
    Mvc2Futures (referenced, unused), the plugins' DotNetOpenAuth 4.3 (Facebook) and PayPal 1.8 (PayPal Direct), SQL
    Server Compact (only if chosen in the installer; not used here).
- **Go**, with SQL Server from `services/compose.yml`.

## Migration

1. `scripts/convert.py` on `Nop.Web` (web) and `Nop.Core`, `Nop.Data`, `Nop.Services`, `Nop.Web.Framework`,
   `Nop.Admin` (libraries): SDK-style net10.0 projects with the webForCore 0.8.0 packages, EntityFramework 6.1.3 →
   6.5.1, Newtonsoft.Json 9.0.1 → 13.0.4, `System.ServiceModel.Syndication` and `System.ComponentModel.Composition`
   as packages, `packages.config` removed, `Program.cs` (without ASP.NET Core's session: `AddSession`/`UseSession`
   aren't needed from 0.8.0, `docs/MIGRATING.md` section 4).
2. `Nop.Web.csproj`: `Administration/**` and `Plugins/**` out of its compilation (they are their own projects; the
   converter listed the 277 Administration files one by one), and a project reference to `Nop.Admin`, which built
   into the site's `bin` on .NET Framework (`<OutputPath>..\bin\</OutputPath>`), so `Nop.Admin.dll` is in
   `bin_dotnet` as it was in `bin`.
3. `<LangVersion>7.3</LangVersion>` in every project (none set one). Without it, C# 14 binds `productIds.Contains(p.Id)`
   (an `int[]`) in LINQ to Entities queries to `MemoryExtensions.Contains(ReadOnlySpan<int>, int)`, and EF 6 throws
   at run time: `LINQ to Entities does not recognize the method 'Boolean Contains[Int32](System.ReadOnlySpan`1[System.Int32], Int32)'`
   (every product page). `docs/MIGRATING.md` has the step, with only the compile-time symptom; `scripts/convert.py`
   pins it only for SDK-style projects. [F83](../../FINDINGS.md#f83)
4. `WindowsAzure.Storage` 8.1.1: its .NET Standard build has no synchronous blob methods (`CS1061: 'CloudBlockBlob'
   does not contain a definition for 'Exists'` in `AzurePictureService.cs`); the `net45` build, in `Nop.Services` and
   `Nop.Web` (guide, "A package's .NET Standard build can have less").
5. `FilePermissionHelper.CheckPermissions`: `Directory.GetAccessControl(path)` is `new DirectoryInfo(path).GetAccessControl()`
   on .NET (CS1929), and `WindowsIdentity.GetCurrent()` throws off Windows. The ACL is read first (it already returned
   "allowed" when it couldn't be read, which is what happens off Windows), then the identity. Same result on Windows.
6. AutoMapper 5.2.0 → 12.0.1 in `Nop.Core` and `Nop.Admin` (F22: configuring a map throws `Method
   System.Linq.Enumerable.MaxInteger: type argument 'System.String' violates the constraint of type parameter 'T'`, a
   500 on every page). nopCommerce 3.90 already uses AutoMapper's instance API (`MapperConfiguration`, `IMapper`), so
   it compiles unchanged on 12. [F22](../../FINDINGS.md#f22)
7. The plugins (all 20): each `.csproj` rewritten as an SDK-style net10.0 class library (`.csproj.netfx` is the old
   one), with its old `OutputPath` (`Nop.Web/Plugins/<name>/`), its `Content` files copied to the output as before
   (views, `Description.txt`, logos, scripts, `web.config`), and its project references `Private=false` as before (an
   `ItemDefinitionGroup`, so the references' own references, which an SDK project would copy, aren't copied either).
   `scripts/convert.py` doesn't handle these (it doesn't keep `OutputPath` or the copied content); the same rewrite was
   done for each one, the same way. Built by the runner after the web project (`also_build`).
   - `ExternalAuth.Facebook` and `Payments.PayPalDirect` have libraries of their own (DotNetOpenAuth, PayPal), which an
     SDK class library doesn't copy: they're referenced from the packages' `lib` folders so that they're copied into
     the plugin's folder, where the plugin manager loads them, as before.
   - Fedex and UPS (SOAP web references, `SoapHttpClientProtocol`) build unchanged.
8. `PluginManager.PerformFileDeploy`: `AppDomain.CurrentDomain.DynamicDirectory` is `null` on .NET, so the first plugin
   failed the pre-start method (`ArgumentNullException: Value cannot be null. (Parameter 'path')` at
   `new DirectoryInfo(directory)`), and every request with it. On IIS it is the code generation folder, which is
   `HttpRuntime.CodegenDir`; webForCore resolves assemblies from that folder, so `Assembly.Load` of the copied plugin
   then works. [F84](../../FINDINGS.md#f84)
9. System.Linq.Dynamic 1.0.7 → System.Linq.Dynamic.Core 1.2.1 (MIT, its successor) in `Nop.Services` and
   `Nop.Web.Framework`. 1.0.7 is built for .NET Framework 4.0 and references `System.Data.Entity` 4.0, so every
   administration edit page failed (`Could not load file or assembly 'System.Data.Entity, Version=4.0.0.0'` from
   `BaseNopValidator`), as would the Kendo grids' filters and message templates' conditions. The namespace changes
   (`System.Linq.Dynamic.Core`), `DynamicExpression.ParseLambda<T, TResult>(name, null)` is
   `DynamicExpressionParser.ParseLambda<T, TResult>(null, false, name)`, and the message tokenizer's
   `new[] { statement }.Where(condition)` needs `.AsQueryable()` (Dynamic.Core's `Where(string)` is on `IQueryable`).
   [F85](../../FINDINGS.md#f85)

Not changed:
- `web.config`: kept as it is. nopCommerce keeps its connection string in `App_Data/Settings.txt`, written by the
  installer. The thumbnails' URLs are `/content/images/thumbs/...` while the folder is `Content/Images/Thumbs` (F86):
  not changed, as it is a webForCore bug.
- `App_Data/InstalledPlugins.txt` and `Settings.txt` are written by the install wizard and ignored by nopCommerce's
  `.gitignore`, so they aren't in the overlay.

## Setup

`[setup]` in `app.toml`: an empty database `nop390`, then nopCommerce's install wizard (`POST /install`) with SQL
Server, a raw connection string to it, and the sample data. The wizard creates the schema, the sample store (about
30 s), installs every plugin it finds and restarts the application; the last step waits for the store's home page on
the new worker.

## Smoke

Probes (23): the home page (with the NivoSlider plugin's widget), a category, a product, search, the blog, the log-in,
register and contact forms, the news RSS feed and the XML sitemap, nopCommerce's own 404 page, `/admin` (302 to the
log-in), the theme's CSS, a plugin's CSS, a product thumbnail at the URL the store writes, a plugin DLL (403, the
site's `DenyAccessToPluginDLLs` handler), and the runner's checks that `web.config`, `App_Data`, `bin_dotnet`, `obj`,
source files and `webforcore-host.dll` aren't served.

- **webForCore 0.8.0, CI (Linux: ubuntu-24.04; Windows: windows-2025): 22/23 on Linux, 23/23 on Windows** (the
  committed results: run 41924554094). Setup 3/3 on both, every run. The Linux failure is the thumbnail,
  `/content/images/thumbs/0000020_build-your-own-computer_415.jpeg` (404; the file is `Content/Images/Thumbs/...`, F86),
  the same in all four Linux runs.
- **Windows is intermittent** (F89): 23/23 in 2 of 4 runs (41918820640, 41924554094), 7/23 in the others (41916845600,
  41921438048), where every request after setup is a 500 at start, "The process cannot access the file because it is
  being used by another process": the plugin manager copying the plugins into the compilation folder while something
  still holds setup's copies. The runner now ends a server's whole process tree on Windows (the first failure was before
  that; the second after), and records the stack of a failed probe, so the next failure will show the call.
  [F89](../../FINDINGS.md#f89)
- On the development machine (Linux) runs are unreliable for this app: its worker needs more than 50 inotify
  instances (F88) and the user's 128 are shared with a desktop session and other servers, so some pages fail with
  "The configured user limit (128) on the number of inotify instances has been reached" (the last local run, in
  `LOG.md`: setup's last step and 4 probes). The committed `results*.json` and logs are CI's.

## Notes for testing by hand

- nopCommerce treats a request whose user agent its `browscap` crawler list doesn't know as a search engine's, with its
  own built-in customer: logging in with `curl`'s default user agent "works" (the cookie is set) but every next
  request is the search engine customer, so `/admin` keeps sending you to the log-in page. With a browser's user agent
  the administration works (checked by hand: dashboard, product, order, category and plugin lists, system information,
  warnings, product and customer edit pages). App behaviour, not a finding.
- The application takes about one inotify instance per folder it watches (each folder with a `web.config`, each folder
  of views it looks in), more than 50 by the time the administration has been visited, so on a machine where the user's
  128 instances are shared with a desktop session and other servers, pages fail with "The configured user limit (128)
  on the number of inotify instances has been reached". [F88](../../FINDINGS.md#f88)

## Open problems

- [F86](../../FINDINGS.md#f86) (webForCore bug): a static file requested in another case than its own is a 404 on
  Linux until it has been requested in its own case once; every product picture (`/content/images/thumbs/...`) on
  Linux.
- [F88](../../FINDINGS.md#f88) (documented gap): inotify instances, one per watched folder.
- [F89](../../FINDINGS.md#f89) (app/environment, cause not confirmed): Windows smoke after setup fails at start in some
  runs.

# Kooboo CMS: migration notes

Status: migrated (CI run 41920542509: 23/23 on Linux and Windows). Source: https://github.com/Kooboo/CMS at `e5879728` (Kooboo CMS 4.4.0.813). Found by
searching GitHub on 2026-10-31; the repo, licence (BSD-3-Clause), web project and its `<TargetFrameworkVersion>` were
checked through the GitHub API.

Round 8, on webForCore 0.8.0 (.NET 10, under `webforcore-host`), with drawingForCore 0.3.0. No webForCore-thirdparty
packages: Kooboo uses neither WebActivatorEx nor AjaxControlToolkit (its Ninject engine is found by type scanning,
not WebActivator).

## Assessment

- **Type:** ASP.NET MVC 4 (Razor 2, Web Pages 2), .NET Framework 4.0. The back office is MVC areas (`Sites`,
  `Contents`, `Account`, `Membership`, ...); the front sites are rendered by `Kooboo.CMS.Sites` from Razor layouts and
  views stored as files under `Cms_Data/Sites/<site>/Templates`, compiled at run time, with content from a content
  "repository".
- **Data:** files. Sites, content repositories, memberships and users are XML/JSON files under `Cms_Data`; search is
  Lucene.Net 2.9. The SQL Server, MySQL, MongoDB, SQL CE, Couchbase and Azure providers are separate projects that
  aren't part of the web project, so no database is needed.
- **Projects:** the web project `Kooboo.CMS/Kooboo.CMS.Web` and its nine libraries (`Kooboo`, `Kooboo.CMS.Account`,
  `.Caching`, `.Common`, `.Content`, `.Form`, `.Membership`, `.Search`, `.Sites`), plus two that the old build copied
  into `bin` with post-build events: `Kooboo.CMS.Common.Runtime.Dependency.Ninject` (the DI engine, found by scanning
  `bin`) and `Kooboo.CMS.Sites.TemplateEngines.Razor` (the Razor template engine for the front sites).
- **Blockers flagged by `assess`:**
  - `Thread.Abort`/`AppDomain`: `Kooboo/Drawing/WebPageCapture.cs` (a WinForms `WebBrowser` page thumbnailer) and
    `Kooboo.CMS.Form/Html/T4/CustomTextTemplatingEngineHost.cs` (not in the project's `<Compile>` list);
  - `NetDataContractSerializer`: `Kooboo/ServiceProcess/IntegratedInstaller.cs` (a Windows service installer);
  - WCF hosting: `Web.config`'s `<system.serviceModel>` declares the CMIS module's `api/cmis.svc`, whose project
    (`Kooboo.CMS.Modules.CMIS`) isn't part of the web project;
  - `WindowsIdentity`: `HttpRequestBaseWrapper.LogonUserIdentity` (a pass-through) and
    `WebRequestErrorEventWrapper` (Kooboo's health-monitoring event wrapper calls `WindowsIdentity.GetCurrent()`: not
    reached by the probes).
  - None is on a path the web app needs.
- **Notices:** `new SmtpClient()` in `UserManager` (password reset mail) and `Site.cs`: not exercised.
- **Dependencies** (all checked-in DLLs under `Lib/`, no NuGet):
  - (a) replaced by webForCore: MVC 4, Web Pages 2, Razor 2, Web API's `System.Net.Http*`,
    Microsoft.Web.Infrastructure;
  - (b) managed, kept from `Lib/`: Ninject 3, Lucene.Net 2.9 (+ Contrib.Highlighter), HtmlAgilityPack, dotless,
    DiffPlex, CookComputing.XmlRpcV2, DotNetOpenAuth 4.1; Newtonsoft.Json 4.5 → the 13.0.4 package;
  - (c) needed replacing: DotNetZip 1.9.1.8 (`Ionic.Zip.Reduced`): it can't extract on Linux (F91).
- `<compilation targetFramework="4.0">`, no `<httpRuntime targetFramework>`: webForCore reproduces 4.5+ behaviour. No
  `<machineKey>`, no `<globalization>` (the site's own culture is in its settings).

Go.

## Migration

1. `scripts/convert.py` on the web project and the eleven libraries above (SDK projects on net10.0, `webForCore` and
   `webForCore.Web.Mvc` 0.8.0, `Program.cs`). Then:
   - the `Lib/MVC/4.0` hint paths it kept (`System.Web.WebPages`, `System.Web.Razor`, `System.Net.Http.WebRequest`)
     removed: webForCore provides them;
   - Newtonsoft.Json from `Lib/JSON.NET` (4.5) → the 13.0.4 package (the version webForCore requires);
   - `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in every library (`docs/MIGRATING.md` section 2);
   - the web project references the Ninject engine and the Razor template engine, which the .NET Framework build
     copied into `bin` with post-build events;
   - `Program.cs` without `AddSession()`/`UseSession()`: webForCore 0.8.0's session provider doesn't need ASP.NET Core's
     session (`docs/MIGRATING.md` section 4).
2. `Kooboo.csproj`: `ServiceProcess/**` (a Windows service installer: `System.Configuration.Install`,
   `ServiceBase`), `ServiceModel/**` (a WCF JSONP binding element: WCF hosting), `Drawing/WebPageCapture*.cs` (a
   WinForms `WebBrowser` thumbnailer) and `Drawing/ColorExtensions.cs` (WinForms' `ControlPaint`) left out of the
   build. Nothing in the web app or the other libraries uses them.
3. Compile fixes:
   - `Kooboo/Linq/Dynamic.cs`: `AssemblyBuilder.DefineDynamicAssembly` for `AppDomain.CurrentDomain.DefineDynamicAssembly`;
   - `Kooboo/Settings.cs`: `AppDomainSetup.PrivateBinPath` and `.ConfigurationFile` aren't on .NET. `BinDirectory` is
     `HttpRuntime.BinDirectory` (it was `PrivateBinPath`, the site's `bin` on IIS); `IsWebApplication` is
     `HostingEnvironment.IsHosted` (it checked that the configuration file was `Web.config`);
   - `[Compare]` ambiguous between MVC's and DataAnnotations' `CompareAttribute` (CS0104) in seven model classes: a
     `using CompareAttribute = System.Web.Mvc.CompareAttribute;` in each (F74);
   - `Kooboo.CMS.Sites`: `MembershipUser` ambiguous between Kooboo's and System.Web.ApplicationServices' (CS0104):
     `webForCore.Web.ApplicationServices` with `ExcludeAssets="compile"`, as the guide says (the project didn't
     reference that assembly on .NET Framework);
   - `Global.asax.cs`: an unused `using System.Web.Http;` commented out. The project never referenced Web API: MVC 4's
     installer put `System.Web.Http` in the GAC, where the .NET Framework build found it.
4. The site's folder (`docs/MIGRATING.md` section 6): Kooboo takes the site root from
   `AppDomain.CurrentDomain.BaseDirectory`/`SetupInformation.ApplicationBase`, which is `bin_dotnet/` on webForCore.
   `Settings.BaseDirectory` is `HttpRuntime.AppDomainAppPath` when hosted, and `BaseDir` (`Cms_Data`'s location) and
   `ModulePath` (`Areas`) use it. Without this, Kooboo looked for `bin_dotnet/Cms_Data`.
5. First request: `Conflicting versions of ASP.NET Web Pages detected: specified version is "2.0.0.0", but the version
   in bin is "3.0.0.0"`. `webpages:Version` is now `3.0.0.0` (F73).
6. Logging in failed with `PlatformNotSupportedException: This platform does not allow the automatic selection of an
   algorithm` from `KeyedHashAlgorithm.Create()` in `PasswordProvider.EncryptPassword`. On .NET Framework the
   parameterless crypto factories (`HashAlgorithm.Create()`, `KeyedHashAlgorithm.Create()`, ...) returned the
   machine.config default (HMACSHA1 here); on .NET they throw. `new HMACSHA1()` is what it was, and the shipped
   `admin` user's password hash matches it (F90).
7. Creating a site from a template failed: `ArgumentException: The value cannot be an empty string. (Parameter
   'path')` from `Directory.CreateDirectory` in DotNetZip 1.9.1.8's `ZipEntry.InternalExtract`, for every entry: that
   build makes the output path with Windows separators (`\`), so on Linux the entry's folder comes out empty and nothing
   extracts.
   The DotNetZip 1.16.0 package (same `Ionic.Zip` API, .NET Standard) replaces `Lib/DotNetZip/Ionic.Zip.Reduced.dll` in
   `Kooboo.CMS.Content`, `.Membership` and `.Sites` (F91, app/environment).
8. Kooboo 4.4 bug, not a migration one (it does the same on IIS): `SiteProvider.CreateSiteUsingPackage` saves its own
   copy of the site's settings after `CreateMembership` has saved the new membership name, so a site created with a
   membership name other than the template's points at the template's (`SampleSite`), which isn't there: the member
   pages throw a `NullReferenceException`. The setup creates the site with the template's membership name.
9. `app.toml`: `[setup]` logs in as the default admin (`admin`/`admin`) and creates the `sample` site from the
   `SampleSite` template (an AJAX form post answered with JSON), then rebuilds the search index of its `Article` folder
   (Contents > Search index > Rebuild: a site created from a template has no index, here as on IIS, so the search finds
   nothing until then; the first CI run, without this step, had 22/23) and waits for the search to find the articles;
   `generated` keeps the admin user's file (the log-in
   records its date there) out of the overlay.

Checked and left alone: `Web.config`'s `<system.serviceModel>` (the CMIS module isn't deployed), the MVC 4 version
numbers in `<compilation><assemblies>`, `<pages>` and the Razor host factory, and `<healthMonitoring>`.

## Smoke

The probes (16, and the runner's 7 forbidden paths): `/` (302 to Kooboo's 404 page: no site is bound to `127.0.0.1`) and that
page; the `sample` site's pages under `/dev~sample/` (the article list, an article, a category, "About us"), rendered
from its Razor templates in `Cms_Data` with content from the file repository; the Lucene search; the member log-in and
registration forms (anti-forgery tokens); the back office's sign-in form, and a page behind it (302 to it); Kooboo's
two resource handlers (combined CSS and scripts); the theme's CSS, an uploaded image and a script.

Linux, here: every probe passed by hand, and 18/23 in the quietest runner run. This machine's user had about 70 of
its 128 inotify instances in use by other processes, and Kooboo's worker needs 48 or more (F92), so most runner runs
hit "Failed to start monitoring changes" on some probes (12/23 to 18/23), and the setup sometimes too. CI's runners
are the reference.

**CI run 41920542509: 23/23 on Linux and on Windows** (16 probes, the 7 forbidden paths), with the setup's 4 steps passing.
The first CI run (41918977774) had 22/23 on both: the search found nothing until the setup rebuilt the index.

**Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0; Linux):** 23/23 as committed, and Kooboo's worker
now holds 7 inotify instances (webforcore-host 1; F92 is fixed). Then verbs: 17 probes (40 in all; 7 of them POSTs).
The back office, as its AJAX forms post (`X-Requested-With`): the log-in, an article created in the sample site's
Article folder (published), shown on the front end, edited (its id from the content list), shown edited, deleted,
and gone from the front end and the list. The sample's detail template throws a `NullReferenceException` for a
missing article (its own code, `ContentExtensions.GetRepository` on a null item), so that probe checks only that the
article's text is gone. Then the front end's member forms (`Kooboo-Submit/RegisterMember` and `ValidateMember`, each
with its anti-forgery token): a member registers, logs in and sees their profile, and the back office's membership
list shows them and deletes them (`MembershipUser/Delete`). 40/40 twice in a row on the same site; a run leaves the
site as it found it.

## Open problems

- F90, guide gap: the parameterless crypto factories throw on .NET (worked around as .NET Framework behaved).
- F91, app/environment: DotNetZip 1.9 can't extract on Linux (moved to 1.16).
- F92 (fixed in 0.9.0): the inotify instances Kooboo's worker takes on Linux (7 now).
- F73, F74 (MVC 4): applied.
- Not exercised: `WindowsIdentity.GetCurrent()` in Kooboo's health-monitoring event wrapper, `new SmtpClient()`
  (password reset, site mail), the back office beyond the log-in, site creation, articles and members.

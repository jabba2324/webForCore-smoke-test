# MVCForum: migration notes

Status: migrated (round 0.9.0, webForCore 0.9.0: 39/39 on Linux and Windows in CI runs 42020729339, 42027500654 and 42032474244, with the plugins built and session state in process). Source:
https://github.com/YodasMyDad/mvcforum at `8ce33799` (archived; the last commit). MIT. webForCore 0.4.0 packages,
webForCore-thirdparty 0.4.0 (WebActivatorEx).

## Assessment

MVC 5 forum (topics, categories, members, badges, polls, private messages, RSS) with Web API 2, OWIN, EF 6 code first with
migrations and a seed, Unity, Hangfire, ImageProcessor, SquishIt, .NET Framework 4.7.1. A web project and `MVCForum.Core`;
`MvcForum.Plugins` builds into `App_Data/Plugins` and isn't migrated.

- `scripts/wfc.py assess` reported two blockers that are false positives:
  - `ef-objectcontext` in `MVCForum.Core/Data/UnitOfWork/UnitOfWork.cs:19`: it is EF 6's own `ObjectContext`, through
    `IObjectContextAdapter`, which runs on .NET.
  - `wcf-hosting` in `Config/imageprocessor/security.config:12`: ImageProcessor's `<services>`, not WCF.

  The runner's patterns now look for EF 1–4's `System.Data.Objects` and for `<services>` inside
  `<system.serviceModel>`; no blockers after that.
- Dependencies:
  - (a) replaced by webForCore: MVC 5, Web API 2, Web Pages, Optimization, WebGrease, Katana.
  - (b) .NET-compatible: EntityFramework (upgraded), Newtonsoft.Json (upgraded), Unity 5, AntiXSS, HtmlAgilityPack,
    LinqKit, MarkdownSharp, Skybrud.Social.
  - (c) .NET Framework-only, tested: Hangfire 1.6 (Core, SqlServer), ImageProcessor.Web 4.9, SquishIt 1.0 (which starts
    through WebActivatorEx), Akismet.NET, AjaxMin, Microsoft.AspNet.Providers.
- **Go**, with SQL Server from `services/compose.yml`.

## Migration

1. `scripts/convert.py` on the web project and `MVCForum.Core`:
   - SDK-style projects with the webForCore 0.4.0 packages;
   - EntityFramework 6.2.0 → 6.5.1 and Newtonsoft.Json 10.0.3 → 13.0.4 (F7);
   - `Program.cs` with session state, `<system.codedom>` removed, `packages.config` removed.
2. First restore: NU1605. The app references preview `System.Configuration.ConfigurationManager` and
   `System.Runtime.Caching` 4.5.0, and webForCore's packages need 11.0. `scripts/convert.py` now drops both (webForCore
   brings them). [F32](../../FINDINGS.md#f32)
3. First build: 35 × `CS0104: 'MembershipUser' is an ambiguous reference between
   'MvcForum.Core.Models.Entities.MembershipUser' and 'System.Web.Security.MembershipUser'` (and `MembershipCreateStatus`)
   in `MVCForum.Core`.
   - On .NET Framework, Core compiled against `System.Web` only (the `SystemWebPackage` package). `System.Web`
     forwards the membership types to `System.Web.ApplicationServices`, which Core didn't reference, so they weren't
     visible.
   - The `webForCore` package gives every project `System.Web.ApplicationServices`.
   - `MvcForum.Core.csproj`: `webForCore.Web.ApplicationServices` with `ExcludeAssets="compile"`, as on .NET Framework.

   [F31](../../FINDINGS.md#f31)
4. `BuildWebCompiler` removed: a build-time Sass/LESS compiler that starts `cmd.exe`. Its output
   (`Themes/Metro/Content/Styles/Theme.css`) is checked in. [F34](../../FINDINGS.md#f34)
5. `CS1929: 'IAppBuilder' does not contain a definition for 'UseHangfireServer'`. NuGet picks Hangfire.Core 1.6.17's
   .NET Standard 1.3 build, which has no OWIN extensions; its `net45` build does.
   - Hangfire.Core is restored for its path only and its `lib/net45` DLL referenced, in both projects.
   - Hangfire.SqlServer the same, found at run time: its .NET Standard build can't look a connection string name up
     in web.config, so `UseSqlServerStorage("MVCForumContext")` was parsed as a connection string ("Format of the
     initialization string does not conform to specification").

   [F33](../../FINDINGS.md#f33)
6. web.config:
   - `MVCForumContext` pointed at the compose SQL Server.
   - The three ImageProcessor `configSource`s say `config\imageprocessor\...`, but the folder is `Config`. Changed to
     `Config\...` (guide section 6: fix the reference's case).
7. `System.Web.Helpers, Version=3.0.0.0` (in web.config) couldn't be loaded.
   - On .NET Framework it came with Microsoft.AspNet.Mvc, through Microsoft.AspNet.WebPages. `webForCore.Web.Mvc`
     doesn't bring `webForCore.Web.Helpers`, so it's added (and `scripts/convert.py` now adds it for MVC apps).

   [F30](../../FINDINGS.md#f30)
8. `App_Data/Plugins` created (with a README): `AssemblyProvider` enumerates it. Until round 0.9.0 it stayed empty: the plugins project that
   fills it on .NET Framework wasn't migrated (step 10).
9. `webForCore.WebActivatorEx` (webForCore-thirdparty 0.4.0) added: SquishIt.AspNet registers its platform from a
   WebActivatorEx start method. Without it, `SquishIt.Framework.Bundle.Css()` throws a `NullReferenceException` in
   every layout. WebActivatorEx arrives transitively, so the converter didn't add it.

10. **Round 0.9.0:** `MvcForum.Plugins` migrated (`MvcForum.Plugins.csproj`, the original kept as `.netfx`), and built
    after the site (`also_build`). It holds the forum's pipelines (creating, editing and deleting topics, posts and
    members) and its badges, which `ImplementationManager` finds in `App_Data/Plugins` (forum.config's
    `PluginSearchLocations`). Without it, creating a topic redirected to the new topic's URL but saved nothing (the
    pipeline had no pipes), and the page then redirected home. The project copies `MvcForum.Plugins.dll` into
    `App_Data/Plugins` after the build (an MSBuild `Copy`, for the .NET Framework project's `xcopy`; the copy is in the
    app's `.gitignore`), references MvcForum.Core with `Private="false"` (it's in the site's `bin_dotnet` already),
    and keeps `webForCore.Web.ApplicationServices` out of the compilation as Core does (F31: `MembershipUser` was
    ambiguous in `UserLoginPipe.cs`). Loaded with `Assembly.LoadFile`, it works as it is.
11. **Round 0.9.0:** session state in process. `Program.cs` had `UseAspNetCoreSessionProvider()` (the converter's
    template for an app that uses `Session`), which keeps session state out of process, serialized, as
    `mode="StateServer"` did on IIS. web.config has no `<sessionState>`, so on IIS it was `InProc`, where nothing is
    serialized. The forum keeps a `GenericMessageViewModel` (not `[Serializable]`) in `TempData` after creating a
    topic, deleting a member and more, so the next request was a 500: `Unable to serialize the session state ...
    Type 'MvcForum.Web.ViewModels.GenericMessageViewModel' ... is not marked as serializable`. `AddDistributedMemoryCache`,
    `AddSession`, `UseSession` and `UseAspNetCoreSessionProvider()` removed, so it's in process, as on IIS.
    [F164](../../FINDINGS.md#f164) (guide gap: the guide's template and step 6 say to add the provider whenever the app
    uses `Session`).

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 14/14 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28); it passes with no workarounds: its `configSource`s written with `\`, `relaxedUrlToFileSystemMapping="true"` (#26, #27).
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 14/14 probes on Linux and on Windows.
- **Round 6 (webForCore 0.7.0 on .NET 10, webForCore-thirdparty 0.5.0; under webforcore-host):** first, the restore
  failed with NU1605: `System.ComponentModel.Annotations` 5.0.0 (EntityFramework 6.5.1's minimum) downgraded to the
  project's 4.4.1. .NET 11 pruned both as part of the framework; .NET 10 prunes only up to 4.3.0 (F54, guide gap,
  [#62](https://github.com/xforcore/webForCore/issues/62)). MvcForum.Core and MvcForum.Web now reference 5.0.0
  (MvcForum.Plugins isn't built). Then 15/15 (the new probe is that `webforcore-host.dll` isn't served).
- **Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0):** 15/15 on Linux as committed. Then verbs: 24 probes
  added (39 in all), 7 of them POSTs, with steps 10 and 11 above, which the new probes needed (nothing the old ones
  reached). The seed's "Read Me" topic gives the admin's generated password; a member registers (the form with its
  anti-forgery token, which logs them in) and logs off; the admin logs on, creates a topic (the multipart form), replies
  (the AJAX post form, `X-Requested-With`), edits the topic's post, deletes the reply, finds the topic with the
  search, deletes the topic (deleting its first post) and deletes the member through the admin area (a GET, as the
  app has it). Each change is checked with a GET (the reply appears, the edit shows, the deleted reply and topic are
  gone, the member is no longer listed), and a run leaves the database as it found it: 39/39 on Linux, twice in a
  row, from a fresh database and on the same one. Not probed: private messages, polls, file uploads (they need a
  second member or files), the Web API's TinyMCE upload (a file).

Probes:
- the home page, a topic and a category (from the seed);
- the log-on and register forms;
- the RSS feed;
- a Web API 2 attribute route (POST only, so a GET is Web API's JSON 405);
- a static CSS file.

**As committed: 0 of 8 app probes pass; the 6 forbidden paths return 404.** Every request is a Configuration Error:
`Unable to open configSource file 'Config\imageprocessor\processing.config'`.
- `InternalConfigHost.StaticGetStreamNameForConfigSource` combines the path with `Path.Combine`, and on Linux `\` isn't a
  separator, so it names a file called `Config\imageprocessor\processing.config`.
- With `/` instead: `The configSource attribute must be a relative physical path, so the '/' character is not allowed`,
  as on .NET Framework.

So a `configSource` in a subfolder can't work on Linux at all. [F28](../../FINDINGS.md#f28)

**Exploration past F28** (not committed): the three files copied next to web.config and `configSource="ip-*.config"`.
- Every request then fails in EF migrations' seed: `Could not find file '<site>/NOT_A_VALID_FILESYSTEM_PATH'`, from
  `HostingEnvironment.MapPath("~/App_Data/forum.config")`. web.config has
  `<httpRuntime relaxedUrlToFileSystemMapping="true">`, which makes System.Web return that placeholder for any mapped
  path it considers invalid, and webForCore's `s_InvalidPhysicalPathChars` (from .NET Framework) includes `/`, so every
  Linux path is invalid. [F29](../../FINDINGS.md#f29)
- With `relaxedUrlToFileSystemMapping="false"` as well, and steps 5 (Hangfire.SqlServer), 8 and 9 above, all 8 app
  probes return their pages:
  - EF migrations create and seed the database;
  - `/`, `/thread/read-me/`, `/cat/example-category/`, `/members/logon/`, `/members/register/` and `/home/latestrss/`
    render through the Metro theme and SquishIt's bundles;
  - `/api/TinyMce/UploadImage` answers Web API's 405 JSON;
  - the theme CSS is served.

### With webForCore's fixes ([xforcore/webForCore#32](https://github.com/xforcore/webForCore/pull/32))

On the PR's packages, all 14 probes pass with the overlay as committed, and with its explicit `webForCore.Web.Helpers`
reference removed. The `Config\imageprocessor\*.config` sections load (#26), `MapPath` works with
`relaxedUrlToFileSystemMapping="true"` (#27), and `webForCore.Web.Mvc` brings System.Web.Helpers (#28).

## Open problems

- [F28](../../FINDINGS.md#f28) (webForCore bug): `configSource` in a subfolder can't be loaded on Linux. Blocks every request.
- [F29](../../FINDINGS.md#f29) (webForCore bug): `relaxedUrlToFileSystemMapping="true"` makes every `MapPath` invalid.
- [F30](../../FINDINGS.md#f30) (webForCore bug): `webForCore.Web.Mvc` doesn't bring System.Web.Helpers.
- [F31](../../FINDINGS.md#f31) (guide gap): System.Web.ApplicationServices is visible to every project.
- [F33](../../FINDINGS.md#f33) (guide gap): the .NET Standard builds of Hangfire 1.6 lack what the app uses.
- [F164](../../FINDINGS.md#f164) (guide gap): the guide adds the out-of-process session provider to every app that uses `Session`; an `InProc` app with values that aren't serializable (here in `TempData`) fails. Worked around as the guide should say: in-process session state.

# MVCForum: migration notes

Status: blocked (F28: `configSource` files in a subfolder can't be loaded; then F29). Source:
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
8. `App_Data/Plugins` created, empty (with a README): `AssemblyProvider` enumerates it, and the plugins project that
   fills it on .NET Framework isn't migrated.
9. `webForCore.WebActivatorEx` (webForCore-thirdparty 0.4.0) added: SquishIt.AspNet registers its platform from a
   WebActivatorEx start method. Without it, `SquishIt.Framework.Bundle.Css()` throws a `NullReferenceException` in
   every layout. WebActivatorEx arrives transitively, so the converter didn't add it.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 14/14 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28); it passes with no workarounds: its `configSource`s written with `\`, `relaxedUrlToFileSystemMapping="true"` (#26, #27).

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

### With webForCore's fixes ([jabba2324/webForCore#32](https://github.com/jabba2324/webForCore/pull/32))

On the PR's packages, all 14 probes pass with the overlay as committed, and with its explicit `webForCore.Web.Helpers`
reference removed. The `Config\imageprocessor\*.config` sections load (#26), `MapPath` works with
`relaxedUrlToFileSystemMapping="true"` (#27), and `webForCore.Web.Mvc` brings System.Web.Helpers (#28).

## Open problems

- [F28](../../FINDINGS.md#f28) (webForCore bug): `configSource` in a subfolder can't be loaded on Linux. Blocks every request.
- [F29](../../FINDINGS.md#f29) (webForCore bug): `relaxedUrlToFileSystemMapping="true"` makes every `MapPath` invalid.
- [F30](../../FINDINGS.md#f30) (webForCore bug): `webForCore.Web.Mvc` doesn't bring System.Web.Helpers.
- [F31](../../FINDINGS.md#f31) (guide gap): System.Web.ApplicationServices is visible to every project.
- [F33](../../FINDINGS.md#f33) (guide gap): the .NET Standard builds of Hangfire 1.6 lack what the app uses.

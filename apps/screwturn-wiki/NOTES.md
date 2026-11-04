# ScrewTurn Wiki 3.0: migration notes

Status: migrated (webForCore 0.7.0, webForCore-thirdparty 0.6.0, drawingForCore 0.2.0: 24/24 probes on Linux and Windows).
MarkZither/ScrewTurnWiki at `7b1ccb46` (24 Feb 2019): ScrewTurn Wiki v3.0.4.560 retargeted to .NET Framework 4.7.2.
GPLv2. Later commits of that fork replace Anthem.NET with Syncfusion's commercial Essential JS controls, so the pin is
before them. (chsword/screwturn, also checked, is the 4.0 line, whose default storage is SQL Server Compact: a Windows
native library with no .NET build, so not used.)

webForCore 0.8.0 wasn't released when this was migrated, so it uses 0.7.0 (with webForCore-thirdparty 0.6.0 and
drawingForCore 0.2.0, `WEBFORCORE_VERSION=0.7.0 THIRDPARTY_VERSION=0.6.0 DRAWING_VERSION=0.2.0`).

## Assessment

- **Type:** Web Forms web application project (`WebApplication/WebApplication.csproj`, `CodeBehind=`), 100+ pages
  and user controls, `.ashx`-style friendly URLs for wiki pages (`MainPage.ashx`, `Dev.MainPage.ashx`) handled by its
  own `Global.asax` URL rewriting, master pages, `App_GlobalResources`/`App_LocalResources` in 20 languages, a CAPTCHA
  drawn with System.Drawing, session-based log-in. Storage is the built-in file-system provider: text files with a
  `.cs` extension under `public/` (pages, users, settings, the search index), shipped with a main page and a `Dev`
  namespace.
- **Libraries:** AclEngine (.NET 3.5), Core, PluginFramework, SearchEngine (4.7.1). The plug-ins, SQL Server providers
  and test projects aren't referenced by the web project and aren't built.
- **Blockers:** none found (`wfc.py assess`).
- **Dependencies:** (a) replaced by webForCore: System.Web, System.Web.Extensions, the framework references.
  (b) kept: Anthem.NET 1.5.2 (an AJAX callback library from 2006, public domain, checked in as
  `References/Lib/Anthem.NET/Anthem.dll`, a .NET 2.0 build; used by 23 pages and controls). (c) none.
- `<httpRuntime>` has no `targetFramework` (`<compilation targetFramework="4.7.2">` only), and
  `requestValidationMode="0.0"`: webForCore reproduces 4.5+ behaviour. No `<machineKey>`.
- **Go.**

## Migration

1. `scripts/convert.py` on the web project and the four libraries: SDK projects on net10.0 with the `webForCore` 0.7.0
   package, `Program.cs` with session state (log-in is kept in `Session`), Anthem by `HintPath` as before. It removed 44
   `.cs` files under `public/` from compilation: they are the wiki's data, not code.
2. The four class libraries get `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (guide, section 2,
   "Class libraries that reference webForCore"), and every project `<LangVersion>7.3</LangVersion>` (the C# its
   .NET Framework projects compiled with; guide, section 2).
3. Builds first time, no code changes.
4. **Data written on Windows:** `public/Pages.cs`, the page index, lists the `Dev` namespace's main page as
   `Dev\MainPage.cs`. The provider joins that to the pages folder with `Path.Combine`, so on Linux the file isn't
   found and `/Dev.MainPage.ashx` rendered with an empty title and body. The data file now says `Dev/MainPage.cs`
   (Windows accepts it too). The code itself builds new entries with `Path.DirectorySeparatorChar`, so only data from a
   Windows server has this; an existing wiki moved to Linux needs the same change to its `public/Pages.cs`. An
   app/environment problem (guide, section 6, "File paths"), not webForCore's.
5. The wiki rewrites `public/Config.cs`, `public/Log.cs` and the search index files when it runs; `app.toml`'s
   `generated` keeps them out of the patch.

web.config is unchanged.

## Smoke

**Round on webForCore 0.7.0: 24/24 probes on Linux, and 24/24 on Windows (CI run 41900122122)** (17 of the app's and the runner's 7 forbidden paths).

Probes: the home page and `MainPage.ashx` (the wiki's rewritten URLs), a page in the `Dev` namespace, the page index,
categories, page history, a search, the OpenSearch description, the RSS feed, the log-in and registration forms,
`Edit.aspx` for an anonymous user (302 to the log-in), the CAPTCHA (a JPEG drawn with System.Drawing), a stylesheet
and a script, and `public/Users.cs` and `public/Config.cs` (the data files, 404 as on IIS: `.cs` is a forbidden
extension).

Also checked by hand: logging in as `admin` (a Web Forms postback with view state and event validation) redirects to
the home page as "Administrator"; `Admin.aspx` then redirects to `AdminHome.aspx`, which renders (its Anthem.NET
controls included), and `Edit.aspx?Page=MainPage` renders the editor.

## Open problems

None found.

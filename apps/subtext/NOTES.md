# Subtext: migration notes

Status: blocked by DotNetOpenAuth on the log-in page (F44): 27/28 probes on Linux and Windows, webForCore 0.9.0. Source: https://github.com/haacked/Subtext at `87a2d90d`. Found by searching GitHub on 2026-10-31; the
repo, licence (MIT), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API.

Round 0.10.0: the forbidden paths `/App_Data/`, `/obj/` and the two `bin_dotnet` assemblies answer 301 "Object moved" from
Subtext's own `<httpErrors>` ExecuteURL page (a path that doesn't exist, which its BlogRequestModule redirects), as
IIS 10 does; webForCore 0.9.0 answered 404. app.toml's `[forbidden]` overrides expect that; nothing is served.

Round 3b, on webForCore 0.4.0 and webForCore-thirdparty 0.4.0. It also uses `webForCore.WebActivator` 0.4.1, a build of
WebActivator 1.5 added to webForCore-thirdparty for this app
([xforcore/webForCore-thirdparty#1](https://github.com/xforcore/webForCore-thirdparty/pull/1), not released yet). Until
that is released, `scripts/wfc.py packages` doesn't download it: copy its `.nupkg` into `.packages/v0.4.0` by hand.

## Assessment

- **Frameworks:** Web Forms (57 `.aspx`), MVC 3-era controllers routed to `.ashx` URLs, Web Pages (`.cshtml` skins).
  Targets .NET 3.5 and 4.0.
- **Projects:**
  - `src/Subtext.Web`;
  - `src/Subtext.Framework`;
  - `src/Subtext.Extensibility`;
  - the two blog editor providers (CKEditor, FCKeditor).
- **Blockers:** none found.
- **Notices:** `BinaryFormatter` in `Util/SerializationHelper.cs:50,65`.
- **Dependencies:**
  - **Checked in (`src/Dependencies`):**
    - Microsoft.ApplicationBlocks.Data (.NET 1.1);
    - blowery.Web.HttpCompress;
    - SgmlReader, SimilarityNet, Snowball.Net, Velocit.RegularExpressions;
    - a `System.Web.Mvc.dll` from a developer's `D:` drive.
  - **Packages:** Ninject (with WebActivator 1.x), DotNetOpenAuth 3.4, log4net, Lucene.Net.
- **Database:** SQL Server only; Subtext's install wizard creates the schema from embedded scripts.

Go, with the SQL Server from `services/compose.yml` and an empty `SubtextData` database.

## Migration

1. `scripts/convert.py` on the web project and the four libraries.
   - `SubText.Framework.csproj` is referenced as `Subtext.Framework.csproj`. Windows doesn't mind; the converter and the
     runner now resolve references case-insensitively.
   - The hint path to `D:\dev\svn\subtext\trunk\src\Dependencies\System.Web.Mvc.dll` is dropped; webForCore.Web.Mvc
     replaces it.
2. `Web.config`:
   - the `subtextData` connection string points at the compose SQL Server (`TrustServerCertificate=True`);
   - the `<add assembly="System.Design, ...">` entry is removed. .NET has no System.Design; every page was a
     Configuration Error (F37).
3. `SubText.Web.csproj`:
   - `GenerateResourceUsePreserializedResources` and `System.Resources.Extensions`, for a `.resx` with images (F36,
     MSB3822);
   - `webForCore.WebActivator` 0.4.1, for `App_Start/NinjectMVC3.cs`'s
     `[assembly: WebActivator.PreApplicationStartMethod]` (F41).
4. `SubText.Framework.csproj`: `webForCore.Serialization.Formatters` 0.4.0. `Util/SerializationHelper.cs` uses its
   `BinaryFormatter` (`using WebFormsForCore.Serialization.Formatters.Binary`), as `docs/MIGRATING.md` says.
5. `UI/WebControls/MasterPage.cs`: `System.Web.UI.Design` and the `[Designer]`/`[ToolboxItem]` attributes are under
   `#if NETFRAMEWORK`. Designers are .NET Framework only (`docs/NAMESPACES.md`).
6. The 21 non-`.resx` embedded resources (installation scripts, welcome content, time zones, control scripts) are kept
   in the two projects. `scripts/convert.py` had dropped them (the SDK globs only `.resx`), so the install wizard
   failed with `ArgumentNullException (Parameter 'stream')`. The converter now keeps them.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 15/16 probes; it builds (F35 fixed) and the SqlClient calls work (F38 fixed); the install wizard runs; the log-in page's DotNetOpenAuth still fails (F44). webForCore-thirdparty's `webForCore.WebActivator` 0.4.1 is released now.
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 15/16 probes on Linux and on Windows; the install wizard runs; unchanged: the log-in page (DotNetOpenAuth).
- **Round 6 (webForCore 0.7.0 on .NET 10, webForCore-thirdparty 0.5.0; under webforcore-host; Linux and Windows):** 16/17 probes on Linux and Windows. Unchanged: the log-in page (DotNetOpenAuth, F44). The round's database was new, so the install wizard ran again (3 November), and the post probe's dated URL moved with it.

The probes need `SubtextData` set up by Subtext's install wizard. These are three postbacks:
1. "Install Now!" on `/install/default.aspx`;
2. the host admin on `Step02_ConfigureHost.aspx`;
3. "Quick Create" on `Step03_CreateBlog.aspx`.

The probes cover the home page, a post (Subtext's URL routing to Web Forms), the archives, the contact form, the log-in
page, an admin page (forms authentication redirect), RSS, two MVC controllers routed to `.ashx` URLs, and a static file.

- **On 0.4.0:** the build fails with `CS0433: The type 'SoapBindingUse' exists in both 'System.Web.Services.Description'
  and 'System.Web.Services'`, from the Web Reference proxy (F35, webForCore bug,
  [#34](https://github.com/xforcore/webForCore/issues/34)).
- **Past F35 (exploration only: a temporary build target dropped the package's compile reference):** every request failed with
  `MissingMethodException: Method not found: 'SqlDataReader SqlHelper.ExecuteReader(String, CommandType, String)'`
  (F38, webForCore bug, [#35](https://github.com/xforcore/webForCore/issues/35)).
- **On the packages of xforcore/webForCore#32** (F35 and F38 fixed): the install wizard runs, and 15 of 16 checks pass (10 probes and the 6 forbidden paths).
  The log-in page fails in DotNetOpenAuth's OpenID control (F44, app/environment).

### Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

**CI (Linux and Windows): 27/28 on both, in CI runs 42026922513 and 42030748535.**

27/28 on Linux, twice; still blocked by DotNetOpenAuth on the log-in page (F44, the same `TypeInitializationException`
in `UriIdentifier`). System.Data.Linq, now provided, isn't in Subtext's web.config (its F37 entry was System.Design,
still removed). 11 probes more, for what can be posted without the log-in: the search box (a postback whose 302 goes
to `/search.aspx?q=...`) and the MetaWeblog API (`POST /services/metablogapi.aspx`, XML-RPC; it checks the blog
owner's name and password itself, so it works without the log-in page): a wrong password is "Username and password
denied."; a post made with `newPost`, its page (its URL taken from `getPost`'s `link`), `editPost`, `getPost` with the
change, the search page finding it (Subtext's Lucene.Net index), `blogger.deletePost`, and `getPost`'s "The entry
could not be found" after. Subtext caches a post's page, so it isn't asked for after the edit and the delete. Not
probed: the comment form (an image CAPTCHA), the contact form (it sends e-mail) and everything behind the log-in.

## Open problems

- F35, webForCore bug: fixed in #32.
- F38, webForCore bug: fixed in #32.
- F44, app/environment: DotNetOpenAuth 3.x reflects on .NET Framework's private `UriParser` fields. The log-in page's
  OpenID control fails; replacing it is an app change.
- F36, F37, F41: guide gaps, documented in #32.

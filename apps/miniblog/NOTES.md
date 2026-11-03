# MiniBlog: migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
(master, 2022; still the Web Pages version). Apache-2.0. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

- **Type:** ASP.NET Web Pages 3 site, a Visual Studio *web site* in `Website/` (no project file): `.cshtml` pages and
  themes, `app_code/` (lower case) with the blog's storage and five handlers plus a whitespace-minifying module, all
  registered in `<system.webServer>`. Posts are XML files in `posts/`. `targetFramework="4.5"`.
- web.config uses much of what IIS adds: ten URL Rewrite rules (inbound and outbound), `<httpErrors>` ExecuteURL,
  `<customErrors>`, static content cache headers, request filtering, custom headers, a default document, forms
  authentication with `<credentials passwordFormat="SHA1">` and an explicit `<machineKey>`.
- **Blockers:** none found.
- **Dependencies:** `bin/` has the DLLs checked in. (a) replaced by webForCore: System.Web.WebPages(.Razor,
  .Deployment), System.Web.Helpers, System.Web.Razor, Microsoft.Web.Infrastructure; note `bin/` holds two copies of
  most of them, differing only in case (`System.Web.WebPages.dll.refresh` next to `system.web.webpages.dll`).
  (b) packages kept: AjaxMin 5.14, WebMarkupMin.Core 2.0 (netstandard1.3), xmlrpcnet and xmlrpcnet-server 3.0.0.266
  (.NET 2.0 builds; the server implements `IHttpHandler`). (c) `ImageCruncher.dll`, no package, kept in `bin/`.
- **Go.**

## Migration

1. New `Website/MiniBlog.csproj` (there was none): `docs/MIGRATING.md` section 2, the `webForCore`,
   `webForCore.Web.WebPages.Razor`, `.Web.WebPages.Deployment` and `.Web.Helpers` 0.1.0 packages, and the site's
   libraries as `PackageReference`s; `ImageCruncher` by `HintPath` to `bin/`. `app.toml` gets `project`.
2. `bin/`: Microsoft's DLLs and the packaged libraries deleted, with their `.refresh` and `.xml` files (guide: remove
   the old `bin`). Listed in `app.toml`'s `remove` so the DLLs aren't in the patch. `packages.config` removed.
3. `Program.cs` (no session state; `HandleAllRequestsWithWebForms()` for the extensionless URLs and rewrite rules).
4. The guide's `<Compile Remove="App_Code/**">` doesn't match `app_code` on Linux, but nothing from `app_code` ended
   up in `MiniBlog.dll` (6 KB, `Program` only) and the folder is compiled at run time as on IIS, so it was left as the
   guide has it.
5. First smoke: every page `CS1069: The type name 'SyndicationItem' could not be found` from
   `app_code/handlers/FeedHandler.cs`. Added the `System.ServiceModel.Syndication` 10.0.0 package
   ([F10](../../FINDINGS.md#f10)).

web.config is unchanged.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 18/18 probes.
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 18/18 probes on Linux and on Windows. On Linux a new `.cs` in `app_code` doesn't restart it, `Web.config` does (F53).

Probes: the home page, a post (`/post/<slug>`, a rewrite), a category, the RSS feed (a handler), robots.txt and
sitemap.xml (rewrites to `.cshtml`), the login page, the RSD view, `/metaweblog` (XML-RPC handler), a theme CSS file
(through `MinifyHandler`), `posts/*.xml` (must be 403), and a missing page (404).

**11 of 19 pass.** Passing: home page, login page, RSD, minified CSS, the 6 forbidden paths. Failing:

- `/post/test-1`, `/category/Test`, `/robots.txt`, `/sitemap.xml`: 404, and **`/posts/<id>.xml` returns 200 with the
  post's XML** (commenters' e-mail addresses, the draft flag) instead of 403. webForCore can't parse the first
  rewrite rule (`patternSyntax="Wildcard"`, `{CACHE_URL}`) and skips all ten ([F11](../../FINDINGS.md#f11)).
- `/feed/rss/` and `/metaweblog`: 404; their handlers are mapped as `path="/feed/*"` and `path="/metaweblog"`
  ([F13](../../FINDINGS.md#f13)). `/comment.ashx` and `/post.ashx` likewise, so comments and the editor can't post.
- `/logs/W3SVC1/...`: the W3C log is served ([F4](../../FINDINGS.md#f4)).

**Exploration past F11** (not committed: the "Remove WWW" rule rewritten as a regex on `{HTTP_HOST}`): robots.txt,
sitemap.xml, the `post/*.aspx` → `/post/*` 301, the fingerprint rewrite and the `posts/*.xml` 403 all work. The
slug, paging and category rules, which rewrite to `?slug={R:1}` and the like, give a bare 500
(`IndexOutOfRangeException` in ASP.NET Core's `RewriteAction`, [F12](../../FINDINGS.md#f12)); `/?slug=test-1` itself
renders the post.

Also checked by hand:
- Logging in as `demo`/`demo`: `FormsAuthentication.Authenticate` against web.config's SHA1 `<credentials>`, the
  anti-forgery token (validated by the page; the helper minifies HTML to unquoted attributes, which works) and
  `RedirectFromLoginPage` give a 302 to `/` with the `miniblog` cookie.
- The missing page returns ASP.NET's own 404 page rather than `/404.cshtml`. ASP.NET's error pages set
  `TrySkipIisCustomErrors`, so IIS also passes them through with `existingResponse="Auto"` for a URL that reaches
  ASP.NET; not compared with IIS, so not counted as a finding.

### Round 2: webForCore 0.2.0

All 18 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 18 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F11](../../FINDINGS.md#f11) (webForCore bug): every rewrite rule dropped; fails open on `posts/*.xml`.
- [F12](../../FINDINGS.md#f12) (webForCore bug): query-only rewrites crash.
- [F13](../../FINDINGS.md#f13) (webForCore bug): handlers mapped with a leading `/` are 404: feed, XML-RPC, comments,
  the editor.
- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C logs served.

# Rabbit (WebMatrix Starter Site): migration notes

Status: blocked (webForCore 0.7.0, webForCore-thirdparty 0.6.0, drawingForCore 0.2.0: 20/21 probes on Linux and Windows): one
probe fails on a webForCore bug ([F79](../../FINDINGS.md#f79)).
yysun/Rabbit at `a819ee23` (23 Jul 2011), MIT (`License.txt`). The repository has the Rabbit framework's class
library and two sites; this is `WebSite`: WebMatrix's ASP.NET Web Pages 1 **Starter Site** (accounts with
`WebSecurity`, `_SiteLayout`, `Account/*.cshtml`) with Yiyi Sun's Rabbit framework in `App_Code` (a module/hook
engine with MVC-style controllers for pages and tags, JSON files in `App_Data` as storage, a code generator and a unit
test runner page).

webForCore 0.8.0 wasn't released when this was migrated, so it uses 0.7.0 (with webForCore-thirdparty 0.6.0 and
drawingForCore 0.2.0, `WEBFORCORE_VERSION=0.7.0 THIRDPARTY_VERSION=0.6.0 DRAWING_VERSION=0.2.0`).

## Assessment

- **Type:** ASP.NET Web Pages 1 web site (no project file), `targetFramework="4.0"`; `_AppStart.cshtml`, `_PageStart`,
  layouts chosen at run time from `Templates/`, extensionless URLs (`/Pages/About` is `Default.cshtml` with
  `UrlData`), `App_Code` C# and a Razor helper file, `Request.AcceptTypes` switching to JSON.
- **Accounts:** `WebSecurity.InitializeDatabaseConnection("StarterSite", "UserProfile", "UserId", "Email", true)`
  (WebMatrix.WebData's SimpleMembership), `Database.Open("StarterSite")` in `Register.cshtml`, `Roles.IsUserInRole`
  in the layouts. No connection string: WebMatrix's `Database.Open("StarterSite")` falls back to
  `App_Data/StarterSite.sdf`, SQL Server Compact ([F77](../../FINDINGS.md#f77)).
- **Blockers:** none from `wfc.py assess`.
- **Dependencies:** (a) replaced by webForCore: System.Web.WebPages(.Razor, .Deployment), System.Web.Razor,
  System.Web.Helpers. (b) Microsoft's `WebMatrix.Data` and `WebMatrix.WebData`, which the Web Pages 1 installer put in
  the GAC; webForCore doesn't provide them ([F78](../../FINDINGS.md#f78)). (c) none. The site has no `bin`.
- **Go**, with the database on SQL Server.

## Migration

1. New `WebSite/WebSite.csproj` (there was none): `docs/MIGRATING.md` section 2, with the `webForCore`,
   `webForCore.Web.WebPages.Razor`, `.Web.WebPages.Deployment` and `.Web.Helpers` 0.7.0 packages, as MiniBlog's. And
   Microsoft's `Microsoft.AspNet.WebPages.Data` and `Microsoft.AspNet.WebPages.WebData` 3.2.9 for WebMatrix.Data and
   WebMatrix.WebData: their Web Pages and Razor dependencies lose to webForCore's (`bin_dotnet`'s
   `System.Web.WebPages.dll` and the rest are webForCore's), and they run as they are (F78). `app.toml` gets
   `project`.
2. `Program.cs` without session state, `HandleAllRequestsWithWebForms()` (extensionless URLs).
3. `Web.config`: a `StarterSite` connection string to the test bed's SQL Server (database `RabbitStarterSite`,
   `System.Data.SqlClient`, `TrustServerCertificate=True`) instead of WebMatrix's SQL CE file. `[setup]` creates the
   empty database; `WebSecurity` creates its tables (`autoCreateTables: true`).

Builds first time; no code changes.

## Smoke

**Round on webForCore 0.7.0: 20/21 probes on Linux, and 20/21 on Windows (CI run 41900122122; the same probe fails)** (14 of the app's and the runner's 7 forbidden paths); setup 3/3
steps.

Setup: registers an account (`Database.Open` + `WebSecurity.CreateAccount`, then logged in: the layout shows the
e-mail address), opens the authenticated Change Password page, logs out. So SimpleMembership on SQL Server, forms
authentication, and WebData's pre-start method all work on webForCore.

Probes: the home page (302 to the Pages module), two content pages and a tag page (the Rabbit controllers, JSON storage
in `App_Data`), the log-in, register and forgot-password forms, Change Password for an anonymous user, the Rabbit
admin and code generator pages, the unit test runner (finds its tests in `App_Code` by reflection), a stylesheet,
robots.txt and a data file under `App_Data` (404).

Also checked by hand: logging in with the account, a wrong password (the form's error), logging out, and the JSON
view (`Accept: application/json` on `/Pages`).

**Failing:** `/Account/ChangePassword` for an anonymous user is an empty **401**; on IIS forms authentication turns it
into a 302 to `/Account/Login`. `WebSecurity.RequireAuthenticatedUser()` sets 401 and calls `Response.End()`, and on
webForCore `Response.End()` sends the response before `FormsAuthenticationModule` gets to EndRequest. A page with
`Response.StatusCode = 401;` alone gets the 302; with `Response.End()` after it, the 401 (`.cshtml` and `.aspx`
alike) ([F79](../../FINDINGS.md#f79), webForCore#129).

### Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

**21/21 on Linux, then 37/37 with the verb probes, twice in a row.** 0.9.0 fixed F79: `/Account/ChangePassword`
sends an anonymous user to the log-in page.

Overlay change: `Views/Tags/_Page_Edit.cshtml`, the Pages module's editor, had `@string.Join(", ", Page.Tags)` inside
an `@if` block, which Web Pages 1's Razor accepted and Razor 3 refuses ("Unexpected "string" keyword after "@"
character", F155): every `/Pages/Edit/<page>` was a parser error. It's `@(string.Join(", ", Page.Tags))` now.

16 probes more, for verbs: a page made with the Pages module's form (`POST /Pages/Create`, 302 to the new page),
shown; its editor; the editor's AJAX save (`POST /Pages/Edit` as its jQuery call sends it, with `X-Requested-With`
and `Accept: application/json`, which Rabbit answers with JSON) with new content and tags; the page read back as JSON
(`Accept: application/json`, the site's own switch) with the tags; the tag's page listing it; the page deleted (`POST
/Pages/Delete/<id>`, the list's Delete link) and gone from the list. Then the account setup made: a failed log-in, a
log-in, its password changed and changed back (so the next run logs in the same), and log-out, after which Change
Password redirects to the log-in again. The pages live in `App_Data/Rabbit/Pages` (JSON files), deleted again by the
probes.

## Open problems

- F79 (above): every `WebSecurity.RequireAuthenticatedUser()`/`RequireRoles()` page shows anonymous users a blank 401.
- F78: WebMatrix.Data/WebData come from Microsoft's packages; webForCore neither ships nor documents them.
- Not the app's: one run on this machine, with other agents' apps running, failed 11 probes with "Failed to start
  monitoring changes" (the user's 128 inotify instances had run out; documented in webForCore's
  `docs/IIS-COMPATIBILITY.md`). The next run, with fewer running, was 20/21 again.

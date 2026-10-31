# AspNetDocs: MvcMovie (Getting Started with MVC 5): migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
(`LICENSE-CODE`; the docs are CC-BY-4.0). Split out of the `aspnetdocs-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

The MVC 5 tutorial's finished app: `MoviesController` over EF 6.1.3 code first with three migrations (`.resx` model snapshots), the Visual Studio 2013+ template's Identity 2 account and manage pages on OWIN cookies, Application Insights 2.2, bundling. .NET 4.6.1. LocalDB connection strings with `AttachDBFilename`. No blockers.

## Migration

1. Converter: `MvcMovie.csproj` → `.csproj.netfx`; SDK project with `webForCore`, `.Web.Mvc`, `.Web.Helpers`,
   `.Web.WebPages.Deployment`, `.Web.Optimization`, `.Microsoft.Owin.Host.SystemWeb`, `.Microsoft.Owin.Security.Cookies`,
   `.Microsoft.Owin.Security.OAuth`, `.Microsoft.AspNet.Identity.Owin`, `.Identity.EntityFramework` and `.Identity.Core`
   0.1.0. EntityFramework 6.1.3 → 6.5.1, Newtonsoft.Json 13.0.1 → 13.0.4 ([F7](../../FINDINGS.md#f7)). The repository
   has no `packages.config` for this sample (the old project's `<HintPath>`s name the versions).
2. Build: `CS0234: The type or namespace name 'Google' does not exist in the namespace 'Microsoft.Owin.Security'`
   (`App_Start/Startup.Auth.cs:6`, a `using`; the template's social logins are commented out). webForCore has no
   Facebook/Google/MicrosoftAccount/Twitter providers, so the original 3.0.1 packages are kept, with their Katana 3.0.1
   dependencies excluded ([F15](../../FINDINGS.md#f15), [F5](../../FINDINGS.md#f5)).
3. web.config: both LocalDB connection strings pointed at the compose SQL Server (no `AttachDBFilename`).

## Smoke

**11 of 12 pass**: the home page, `/Movies` (EF creates the database from the migrations, including the `.resx`
snapshots), `/HelloWorld/Welcome?name=Scott&numTimes=4`, the Register form and `Site.css`. Only the served W3C log
fails ([F4](../../FINDINGS.md#f4)).

By hand: registering `smoke@example.com` (anti-forgery token, Identity's password validation, `UserManager` on EF,
`SignInManager` issuing the OWIN cookie) returns 302 to `/` and the user is in `AspNetUsers`; `POST /Movies/Create`
returns 302 to `/Movies` and the movie is listed.

### Round 2: webForCore 0.2.0

All 11 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 11 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0, and its 2 `ExcludeAssets` references for transitive Microsoft packages were removed: webForCore's packages override them since 0.3.0 (webForCore#23).

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.
- [F15](../../FINDINGS.md#f15) (guide gap): social login packages.

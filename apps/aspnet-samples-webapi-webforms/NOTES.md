# ASP.NET samples: Web API WebFormSample: migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

Web API 2 controllers inside the Visual Studio 2012 Web Forms template (9 `.aspx`, ScriptManager packages, Universal Providers with LocalDB). .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `WebForm.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore` and `webForCore.Web.Http.WebHost` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). EntityFramework 6.0.0 → 6.5.1 ([F7](../../FINDINGS.md#f7)). Microsoft.AspNet.Providers.Core/LocalDb 2.0 and the ScriptManager packages are kept.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 7/7 probes.
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 7/7 probes on Linux and on Windows.
- **Round 6 (webForCore 0.7.0 on .NET 10, webForCore-thirdparty 0.5.0; under webforcore-host; Linux and Windows):** 8/8 probes on Linux and Windows.

Not run: the build fails with `CS0433` on `HttpUtility` in the template's `ViewSwitcher.ascx.cs` ([F8](../../FINDINGS.md#f8)), as in eshop-legacy-webforms. No probes yet.

### Round 2: webForCore 0.2.0

All 7 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 7 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

### Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

The probes covered only `/` before (the runner's default). The app has a Web API controller and the Visual Studio
2012 Web Forms template's account pages, on the Universal Providers (`System.Web.Providers` 2.0: membership, roles,
profile), so:

- **Web.config:** `DefaultConnection` points at the compose SQL Server (database `WebFormSample`) instead of LocalDB;
  `needs = ["mssql"]`. The Universal Providers create the database and their tables on first use, as on .NET
  Framework (an existing empty database fails with `Invalid object name 'dbo.Applications'`, as it would there), so
  `[setup]` lists no database and only registers the test user (`smokeuser`) through the Register page's
  CreateUserWizard postback. On a database that has the user already, the step gets the page back with "Please enter
  a different user name." (200), so it runs every time.
- **Probes (29; 12 with verbs):**
  - pages: `/`, `/About.aspx`, and `/Account/ChangePassword.aspx` redirecting an anonymous user to the log-in page;
  - Web API inside Web Forms (`ValuesController`, routed in `Global.asax`): GET, then POST, PUT and DELETE, each 204.
    The template's actions do nothing with what they're sent, so there's no change to read back;
  - Web Forms postbacks with view state and event validation captured from the GET before: registering `smokeuser`
    again is refused; a log-in with the wrong password is refused ("Your login attempt was not successful") and with
    the right one sets `.ASPXAUTH` (302); the master page's LoginView then greets the user;
  - `ChangePassword` (logged in) to a new password, its success page; the AJAX log-in handler (`LoginAjax.aspx`, a
    form POST with `X-Requested-With` answered with JSON) refuses the old password and accepts the new; the password
    is changed back, and the AJAX log-in accepts it. The run ends where setup left the database.

Run twice locally on a fresh database and once more on the same one: 29/29 each time. Results: RESULTS_PLACEHOLDER

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F8](../../FINDINGS.md#f8) (webForCore bug): blocks the build.

# OpenOrderFramework: migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
https://github.com/lbacaj/OpenOrderFramework at `a6e25736`. Found by searching GitHub on 2026-10-31; the repo, licence
(MIT), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API. webForCore 0.2.0 packages.

## Assessment

MVC 5 shopping cart (store, cart, checkout, orders, admin) on EF 6 code first and ASP.NET Identity 2.0.0 (the Identity
samples package), .NET Framework 4.5.1, one web project. `scripts/wfc.py assess`: no blockers, no notices.

- Dependencies: (a) replaced by webForCore: MVC 5, Web Pages, Optimization, Katana 2.1 (`Microsoft.Owin`,
  `.Host.SystemWeb`, `.Security`, `.Cookies`, `.OAuth`), Identity 2.0.0. (b) .NET-compatible: EntityFramework (upgraded),
  Newtonsoft.Json (upgraded), NLog 2.1, PagedList, RestSharp 104. (c) .NET Framework-only, tested: Katana 2.1's
  Facebook, Google, MicrosoftAccount and Twitter providers. Content packages (bootstrap, jQuery, d3, ...) have their
  files checked in.
- `Startup.Auth.cs` calls `app.UseGoogleAuthentication()` (no client id: the 2.1 OpenID provider).
- **Go**, with SQL Server from `services/compose.yml`.

## Migration

1. `scripts/convert.py` on the web project: `OpenOrderFramework.csproj` → `.csproj.netfx`, new SDK-style project with
   the webForCore 0.2.0 packages, EntityFramework 6.1.0 → 6.5.1, Newtonsoft.Json 5.0.6 → 13.0.4 (F7), `Program.cs`
   with session state, `<system.codedom>` removed, `packages.config` removed.
2. First build: `CS0234: 'Google' does not exist in the namespace 'Microsoft.Owin.Security'`. The converter dropped all
   `Microsoft.Owin.*` packages. Per `docs/MIGRATING.md` ("Katana packages webForCore doesn't lift", F15): the four
   social providers kept at the app's version, 2.1.0, with `Microsoft.Owin` and `Microsoft.Owin.Security` 2.1.0
   `ExcludeAssets="all"`. (`scripts/convert.py` now does this.)
3. Next build: `CS0104: 'SignInStatus' is an ambiguous reference between 'OpenOrderFramework.Models.SignInStatus' and
   'Microsoft.AspNet.Identity.Owin.SignInStatus'`. The app was written against Identity 2.0.0, which had no
   `SignInStatus`, and defines its own in `App_Start/IdentityConfig.cs`; webForCore's Identity is 2.2, which added one.
   `Controllers/AccountController.cs`: a `using SignInStatus = OpenOrderFramework.Models.SignInStatus;` alias.
   [F16](../../FINDINGS.md#f16)
4. web.config: `DefaultConnection` pointed at the compose SQL Server instead of LocalDB.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 12/12 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28); passes once its database exists; on the fresh database the first run hit its own seed deadlock (F26).
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 12/12 probes on Linux and on Windows. On Linux the first run against the new database timed out (F26); the second passed.

Probes: the home page, the store (genres), the cart, log-in and register (anti-forgery token) and a static CSS file.

**As committed: 0 of 6 app probes pass; the 6 forbidden paths return 404.** Every request, static files included, fails
in `OwinHttpModule.Init`: `Could not load file or assembly 'System.Net.Http.WebRequest, Version=4.0.0.0'` from
`GoogleAuthenticationMiddleware.ResolveHttpMessageHandler`. [F17](../../FINDINGS.md#f17)

**Exploration past F17** (not committed: `app.UseGoogleAuthentication()` commented out by hand): EF creates the
database; `/`, `/Store`, `/ShoppingCart`, `/Account/Login`, `/Account/Register` and `/Content/Site.css` all return
200 with their pages. `/Store/Browse` without a genre is a 500 (`Sequence contains no elements`), the app's own
behaviour on an empty catalogue.

### With webForCore's fixes ([jabba2324/webForCore#24](https://github.com/jabba2324/webForCore/pull/24))

On the PR's packages, all 12 probes pass: the `System.Net.Http.WebRequest` the Google provider needs is in the
"Always" set (#18).

### Round 3: webForCore 0.4.0

All 12 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0, and its 2 `ExcludeAssets` references for transitive Microsoft packages were removed: webForCore's packages override them since 0.3.0 (webForCore#23).

On a fresh database the first requests hang. The app's seed deadlocks; it's the app's own (F26):
- `ApplicationDbInitializer.Seed` runs inside EF 6's initialization lock, taken by the layout's
  `ShoppingCart.GetCount()`.
- `InitializeIdentityForEF` then uses the OWIN context's `RoleManager`/`UserManager`, whose `ApplicationDbContext` is
  another instance.
- Identity's `AsyncHelper.RunSync` runs `FindByNameAsync` on a thread-pool thread, which waits on the same lock.

Seen with `dotnet-stack`; SQL Server showed nothing blocked. Once the database exists, all 12 probes pass. The app's own
workflow creates the database with `Update-Database`, which doesn't run that seed.

## Open problems

- [F17](../../FINDINGS.md#f17) (guide gap): the Katana social providers webForCore doesn't lift fail at start-up
  when they're used. Blocks every request.
- [F16](../../FINDINGS.md#f16) (guide gap): Identity 2.0 → 2.2 adds `SignInStatus`; worked around with an alias.

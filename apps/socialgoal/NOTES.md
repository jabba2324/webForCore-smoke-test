# SocialGoal: migration notes

Status: migrated (webForCore 0.9.0 and webForCore-thirdparty 0.7.0's AutoMapperLegacy, 24/24 probes on Linux and Windows). Source:
https://github.com/MarlabsInc/SocialGoal at `42cfdb48`. Found by searching GitHub on 2026-10-31; the repo, licence
(MIT), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API. webForCore 0.2.0 packages.

## Assessment

MVC 5 social goal-setting site (goals, groups, updates, notifications) on EF 6 code first and ASP.NET Identity 2,
Autofac, AutoMapper 3.1, ELMAH with Elmah.MVC, MvcMailer, .NET Framework 4.5. Web project plus five libraries (Core,
Data, Model, Service, Web.Core). `scripts/wfc.py assess`: no blockers, no notices.

- Dependencies: (a) replaced by webForCore: MVC 5, Web Pages, Optimization, Katana 2.0, Identity 2. (b)
  .NET-compatible: EntityFramework 6.0.2-beta1 (upgraded), Newtonsoft.Json (upgraded), Autofac 3.1, AutoMapper
  3.1.1-ci1000, PagedList. (c) .NET Framework-only, tested: elmah.corelibrary 1.2.2, Elmah.MVC 2.1, Autofac.Mvc5 3.0,
  MvcMailer 4.5, Katana 2.0's social providers.
- `Startup.Auth.cs` calls `app.UseGoogleAuthentication()`.
- **Go**, with SQL Server from `services/compose.yml`.

## Migration

1. `scripts/convert.py` on the web project and the five libraries: SDK-style projects with the webForCore 0.2.0
   packages, EntityFramework → 6.5.1 and Newtonsoft.Json 5.0.6 → 13.0.4 (F7), `Program.cs` with session state,
   `<system.codedom>` removed, `packages.config` removed.
2. First build: `CS1061: 'IAppBuilder' does not contain a definition for 'UseGoogleAuthentication'`. The four Katana
   2.0.0 social providers kept, with `Microsoft.Owin` and `.Security` 2.0.0 `ExcludeAssets="all"` (`docs/MIGRATING.md`,
   F15). Builds.
3. web.config: `SocialGoalEntities` pointed at the compose SQL Server.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 8/9 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28); unchanged: AutoMapper 3 (F22).
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 8/9 probes on Linux and on Windows; unchanged (AutoMapper).
- **Round 6 (webForCore 0.7.0 on .NET 10, webForCore-thirdparty 0.5.0; under webforcore-host; Linux and Windows):** 9/10 probes on Linux and Windows. Unchanged: AutoMapper 3.1 (F22).

Probes: the home page, the log-in page and a static CSS file (no text checks yet: no page has rendered).

**As committed: 0 of 3 app probes pass; the 6 forbidden paths return 404.** Every request is a Configuration Error:
`An error occurred creating the configuration section handler for elmah/errorMail: Type 'Elmah.ErrorMailSectionHandler'
does not inherit from 'System.Configuration.IConfigurationSectionHandler'`. ELMAH 1.2 is built against .NET 2.0 and
references `System.Configuration.SingleTagSectionHandler` through `System, Version=2.0.0.0` (checked in its metadata).
On .NET, `System.dll` is a facade that forwards it to `System.Configuration.ConfigurationManager`, and the facade is
shared from the default load context, so the forward reaches the host's copy, not the application's. It's the case
webForCore 0.2.0's F1 fix (`System.Configuration` per domain) doesn't cover. [F18](../../FINDINGS.md#f18)

**Exploration past F18** (not committed, by hand):

- With ELMAH's three modules removed from web.config: every request fails in `OwinHttpModule.Init` on
  `System.Net.Http.WebRequest` from the Google provider. [F17](../../FINDINGS.md#f17)
- With `app.UseGoogleAuthentication()` also commented out: `ArgumentException: The type or method has 3 generic
  parameter(s), but 1 generic argument(s) were provided` in AutoMapper 3.1's
  `TypeInfo.BuildPublicNoArgExtensionMethods`. AutoMapper before 12 doesn't run on .NET 11 (reproduced without
  webForCore; see ReaLocate). [F22](../../FINDINGS.md#f22) Moving this app from AutoMapper 3's static `Mapper` API to 12 is
  a rewrite of its mapping code, so it stops here.

### With webForCore's fixes ([xforcore/webForCore#24](https://github.com/xforcore/webForCore/pull/24))

On the PR's packages, ELMAH's sections load (#19) and the Google provider starts (#18): `/Account/Login` and the CSS
pass. `/` fails on AutoMapper 3.1 ([F22](../../FINDINGS.md#f22), app/environment), as found by hand above.

### Round 3: webForCore 0.4.0

8 of 9 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0, and its 2 `ExcludeAssets` references for transitive Microsoft packages were removed: webForCore's packages override them since 0.3.0 (webForCore#23).

Unchanged: ELMAH and the Google provider work (0.3.0), and `/` fails on AutoMapper 3.1 (F22). Its Katana
`ExcludeAssets` references were removed.

### Round 0.9.0: AutoMapper, verbs (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

- **F22 fixed by webForCore-thirdparty's `webForCore.AutoMapperLegacy` 0.7.0** (AutoMapper 3.x's static API on
  AutoMapper 12), following its README, in `SocialGoal.Web` and `SocialGoal.Web.Core`:
  - `<PackageReference Include="webForCore.AutoMapperLegacy" Version="0.7.0" />` for `AutoMapper` 3.1.1-ci1000;
  - its global usings as `<Using>` items: `AutoMapperLegacy`, `Mapper` and `Profile` (the app has 3.x profiles);
  - `PagedListConverter` implements `AutoMapperLegacy.ITypeConverter<,>`;
  - nothing else: the projects set no `<LangVersion>` (C# 14 already), and the EF queries' `Contains` are on lists
    and queries, not arrays (F83 can't happen).
- **`[setup]` (new):** creates `SocialGoal` and registers `smokeuser` through the app's Register form.
- **Probes (14 added, 6 of them POSTs, with anti-forgery tokens where the forms have them):** `/` (now a 302 to the
  log-in, the home page is `[Authorize]`); the log-in, whose form calls the user name `Email`, with a wrong password
  ("Invalid username or password.") and the right one; the home page signed in; a goal created (302 to it), shown,
  edited, found by the search form, deleted (then 404); log-off. The log-in page and bootstrap.css probes now check
  their text.
- **Results:** 24/24 on Linux (9/10 before), twice in a row, from a new database.

**CI (round 0.9.0, run 42029051731):** 24/24 on Linux, 24/24 on Windows, the same in run 42024931148.

## Open problems

- **Round 0.9.0:** F18 and F17 were fixed in 0.3.0; F22 is resolved by webForCore.AutoMapperLegacy 0.7.0 (above). Nothing open.
- [F18](../../FINDINGS.md#f18) (webForCore bug): configuration types referenced through `System.dll` reach the host's
  ConfigurationManager. Blocks every request.
- [F17](../../FINDINGS.md#f17) (guide gap): Katana's Google provider needs System.Net.Http.WebRequest.
- [F22](../../FINDINGS.md#f22) (app/environment): AutoMapper 3.1 doesn't run on .NET 11 or .NET 10; the upgrade to 12 is the app's.

# SocialGoal: migration notes

Status: blocked (F18: ELMAH's config sections fail; then F17, then AutoMapper 3). Source:
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

## Open problems

- [F18](../../FINDINGS.md#f18) (webForCore bug): configuration types referenced through `System.dll` reach the host's
  ConfigurationManager. Blocks every request.
- [F17](../../FINDINGS.md#f17) (guide gap): Katana's Google provider needs System.Net.Http.WebRequest.
- [F22](../../FINDINGS.md#f22) (app/environment): AutoMapper 3.1 doesn't run on .NET 11; the upgrade to 12 is the app's.

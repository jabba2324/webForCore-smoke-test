# FunnelWeb: migration notes

Status: verified. Source: https://github.com/funnelweblog/FunnelWeb at `32f561e9`. Found by searching GitHub on 2026-10-31; the
repo, licence (BSD-3-Clause), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API.

Round 3b, on webForCore 0.4.0 (no webForCore-thirdparty packages).

## Assessment

- **Frameworks:** MVC 4 (with an Admin area), Razor views, .NET 4.0.
- **Projects:**
  - `src/FunnelWeb.Web`;
  - `src/FunnelWeb`;
  - `src/FunnelWeb.Extensions.CommentNotification`.
- **Blockers:** none found.
- **Notices:** `SmtpClient` configuration in `CommentPostedListener.cs:46`.
- **Dependencies:**
  - **Data:** NHibernate 3.3.1 with FluentNHibernate 1.3, and DbUp for the schema;
  - **Other:** Autofac 2.6 (MVC 3 integration), MarkdownSharp, Bundle Transformer.
- **Database:** SQL Server or SQL CE. SQL Server from `services/compose.yml`, with an empty `FunnelWeb` database.

Go. NHibernate 3.3 predates .NET and is the risk.

## Migration

1. `scripts/convert.py` on the web project and the two libraries.
2. `FunnelWeb.Web.csproj`: `Iesi.Collections` 3.2.0.4000 → 3.3.2.4000. Its packages.config had 3.2.0 while the
   `FunnelWeb` library has 3.3.2. With PackageReference the library's version flows to the web project, and restore
   failed with NU1605 (F42).
3. `DatabaseDeployer/FunnelWebScriptProvider.cs`: `_Assembly` → `System.Reflection.Assembly`. .NET has no
   `System.Runtime.InteropServices._Assembly` (an API .NET doesn't have, in app code).
4. `My.config`: the connection string points at the compose SQL Server.
5. The 45 non-`.resx` embedded resources (the DbUp scripts, `wlwmanifest.xml`) are kept. `scripts/convert.py` had
   dropped them; the converter now keeps them. Without them the upgrader found no scripts, and the home page went
   straight to NHibernate.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 9/10 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28); unchanged: NHibernate 3.3 (F39).
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 9/10 probes on Linux and on Windows; unchanged (NHibernate's proxies).

The probes need the `FunnelWeb` database set up by FunnelWeb's upgrader: log in at `/admin/login` as `test`/`test`
(My.config), then "Upgrade" on `/admin/install`. It runs 32 scripts. The probes cover the home page, the Admin area's
log-in form (MVC), the installer (forms authentication redirect) and a static file.

- **On 0.4.0:** 9 of 10 checks pass (4 probes and the 6 forbidden paths). The log-in, the installer and its upgrade (MVC postbacks) work.
- **The blog fails:** `The following types may not be used as proxies: FunnelWeb.Model.Authentication.Role: method
  MemberwiseClone should be 'public/protected virtual' or 'protected internal virtual'`, for every entity. With that
  check bypassed (exploration only, reverted), `NHibernate.AdoNet.SqlClientSqlCommandSet` fails: it reflects on
  System.Data's internal `SqlCommandSet`. NHibernate 3.3 doesn't run on .NET (F39, app/environment).

## Open problems

- F39, app/environment: NHibernate 3.3. Moving to NHibernate 5.1+ and FluentNHibernate 3 is an upgrade of the app's
  data layer, beyond a migration.
- F42, guide gap: documented in jabba2324/webForCore#32.

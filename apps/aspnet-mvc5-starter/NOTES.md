# Asp.Net MVC 5 Starter Kit: migration notes

Status: migrated (webForCore 0.9.0, 32/32 probes on Linux and Windows). Source: https://github.com/akasarto/aspnet-mvc5-starter-template at `885be06e`. Found by searching GitHub on 2026-10-31; the
repo, licence (MIT), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API.

## Assessment

Round 2 (webForCore 0.2.0), assessment only. MVC 5 starter template with OWIN, SignalR 2 and ASP.NET Identity,
globalized, .NET Framework 4.7.1. The web project references 13 libraries under `sources/` (identity, data, a SQL
Server store, a migrator, and infrastructure for Azure storage, Cloudinary, file system, MailGun, SMTP, dynamic images).

- `scripts/wfc.py assess`: no blockers; notice `smtpclient-config` at
  `sources/Shared.Infrastructure.Smtp/SystemNetSmtpEmailDispatcherService.cs:42` (documented replacement).
- **Go**, with SQL Server from `services/compose.yml`. Migrated in round 3 (webForCore 0.4.0).
- Dependencies:
  - (a) replaced by webForCore: MVC 5, Optimization, Katana, Identity 2, SignalR 2.
  - (b) .NET-compatible: Newtonsoft.Json (upgraded), Dapper, FluentMigrator 3, FluentValidation 8, Humanizer, Serilog,
    SimpleInjector 4.6, RestSharp, CloudinaryDotNet, WindowsAzure.Storage 9.3.

## Migration

1. `scripts/convert.py` on the web project and its 13 libraries:
   - SDK-style projects with the webForCore 0.4.0 packages;
   - Newtonsoft.Json 13.0.1 → 13.0.4 (F7);
   - `Program.cs`, `<system.codedom>` removed, `packages.config` removed.
2. First restore: `NU1605: Detected package downgrade: Humanizer.Core from 2.14.1 to 2.6.2`. webForCore's
   `webForCore.Compilers` (Roslyn 5.9) needs Humanizer.Core 2.14.1. The app's reference is raised to it
   (`scripts/convert.py` now does that). [F32](../../FINDINGS.md#f32)
3. First build: `MSB4018` in Microsoft.Web.LibraryManager.Build 2.0.48's restore task: it fails to load its providers
   by reflection on the .NET 11 SDK. LibMan.Build → 3.0.114, whose task works: the client libraries in `libman.json`
   (jQuery, Bootstrap 4.3.1, ...) are restored from cdnjs into `Assets/vendor` at build time. [F34](../../FINDINGS.md#f34)
4. Next build: `CloudBlobContainer`/`CloudBlockBlob` have no `Exists`, `OpenRead`, `UploadFromStream`, ... (CS1061).
   NuGet picks WindowsAzure.Storage 9.3.3's .NET Standard build, which has only the asynchronous methods; the app uses
   the synchronous ones of its `net45` build. The package is restored for its path only and its `lib/net45` DLL
   referenced, in `Shared.Infrastructure.Azure` and the web project. [F33](../../FINDINGS.md#f33)
5. web.config: `SqlServerConnection` pointed at the compose SQL Server instead of LocalDB.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 15/15 probes.
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 15/15 probes on Linux and on Windows.
- **Round 6 (webForCore 0.7.0 on .NET 10, webForCore-thirdparty 0.5.0; under webforcore-host; Linux and Windows):** 16/16 probes on Linux and Windows.

Probes:
- the home page;
- an area index (302 to its overview) and the overview;
- the Identity log-in and sign-up forms;
- an `[Authorize]` area (302 to log in);
- Bootstrap restored by LibMan, and the app's CSS;
- the SignalR hub proxy.

**All 9 app probes pass; the 6 forbidden paths return 404.** The Azure storage, Cloudinary and MailGun services, which
need accounts, weren't exercised.

### Round 0.9.0: verbs (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

- **The database was never created.** The app's schema and its super user come from `Data.Tools.Migrator`, a console
  program (`app.cmd install`), which the smoke test never ran; until now no probe reached the database (every page
  probed renders without it). The first log-in post was a 500 ("Cannot open database starterTemplateMVC5").
- **`[setup]` (new):** creates `starterTemplateMVC5` and runs the migrator's two scripts. They are UTF-16, and on Linux
  the runner pipes a setup SQL file into the container's `sqlcmd` on standard input, which reads them as bytes
  ("Incorrect syntax near '�'"), so the overlay adds `Smoke_Setup.sql`, the two scripts in UTF-8, one after the other
  (`sources/Data.Tools.Migrator/SqlServerScripts/`). They make the schema and `admin` (password `password`,
  SuperUser).
- **Probes (16 added, 7 of them POSTs):** the features area's sample form (FluentValidation, anti-forgery token)
  posted incomplete ("Some fields are not valid.") and complete ("All fields validated correctly."), and its AJAX
  promo-code check (`X-Requested-With`, JSON `"isValid":true,"promoCode":"TEST"`); the log-in with a wrong password
  ("Invalid credentials.") and the right one (302); the super user's `/management/users/`; the profile form: the full
  name changed (302), shown, and changed back (the time zone is taken from the form); SignalR's negotiate for the
  `databus` hub; log-off and `/management/users/` redirecting again.
- **Not probed:** SignalR's connect and send. Both need the negotiated connection token in the query string, URL
  encoded, and a probe's capture can't encode it (it has `+`, `/` and `=`). User administration (add, edit) needs a
  new user per run, and the app can't delete one.
- **Results:** 32/32 on Linux (16 before), twice in a row, from a new database.

## Open problems

- [F33](../../FINDINGS.md#f33) (guide gap): WindowsAzure.Storage's .NET Standard build lacks the synchronous API; the net45 build is referenced.

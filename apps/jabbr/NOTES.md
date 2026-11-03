# JabbR: migration notes

Status: blocked. Source: https://github.com/JabbR/JabbR at `eb5b4e2f` (the `dev` branch's last commit, 2016). Licence MIT.
Round 4, on webForCore 0.5.0 and webForCore-thirdparty 0.4.1.

## Assessment

- **Frameworks:**
  - SignalR 2.2 (prerelease);
  - pages as Nancy 0.20 modules on OWIN (`Microsoft.Owin.Host.SystemWeb`), with Nancy's Razor view engine;
  - Web API for its uploads.
- **Other packages:** EF6 with migrations; Ninject 3.0 (prerelease), with Nancy's Ninject bootstrapper. Targets .NET 4.5.
- **Flagged blocker:** `WindowsPrincipalHandler` (Windows auth), but it only checks
  `Request.User as WindowsPrincipal`, which is null off Windows. Not a blocker.
- **Database:** SQL Server; the compose one, and EF's migrations create the `JabbR` database.

## Migration

1. `scripts/convert.py` on the project.
   - It now keeps the Katana and SignalR packages webForCore doesn't ship, at the versions webForCore lifted:
     Microsoft.Owin.Diagnostics, FileSystems and StaticFiles 2.1.0-rc1 → 4.2.3; SignalR.SqlServer and ServiceBus
     2.2.0-pre → 2.4.3. It used to drop them: `UseErrorPage`, `UseStaticFiles` and `UseSqlServer` were missing.
   - EntityFramework 6.0.1 → 6.5.1 and Newtonsoft.Json 5.0.7 → 13.0.4 (F7).
2. `Hubs/Chat.cs`: `OnDisconnected()` → `OnDisconnected(bool stopCalled)`, SignalR 2.4's signature. webForCore's SignalR is
   one version (#17).
3. `Infrastructure/JabbRFormsAuthenticationProvider.cs`: no-op `ResponseSignedIn`, `ResponseSignOut` and `Exception`.
   Katana 3+ added these to `ICookieAuthenticationProvider` (webForCore ships Katana 4.2.3), and `CookieAuthenticationProvider`'s
   defaults are no-ops too.
4. `Web.config`: the `Jabbr` connection string points at the compose SQL Server.
5. Ninject 3.0.2-unstable → 3.3.6. Its .NET Framework build needs `AppDomain.Evidence`
   (`MissingMethodException`), and 3.3.6 has a .NET Standard build (as Roadkill's StructureMap, F25).

## Smoke

- **On 0.5.0:** the build fails with `CS1705: Assembly 'Microsoft.AspNet.SignalR.SqlServer' ... uses
  'Microsoft.AspNet.SignalR.Core, Version=2.4.3.0' which has a higher version than referenced assembly
  'Microsoft.AspNet.SignalR.Core' with identity '..., Version=0.0.0.0'`. webForCore's SignalR assemblies have no version
  (F46, webForCore bug, [#40](https://github.com/jabba2324/webForCore/issues/40)).
- **On the packages of [webForCore#44](https://github.com/jabba2324/webForCore/pull/44):** it builds. Start-up then fails in Nancy 0.20:
  `MissingMethodException: Method not found: 'System.String System.AppDomainSetup.get_PrivateBinPathProbe()'`, from
  `Nancy.Bootstrapper.AppDomainAssemblyTypeScanner`. Nancy 0.20 is .NET Framework only. Nancy 2.x runs on .NET Standard,
  but its API changed enough that moving JabbR's modules would be a port (F50, app/environment).
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** it builds (F46 fixed in 0.6.0). 6/10 probes on Linux and on Windows: start-up fails in Nancy 0.20 (F50).

## Open problems

- F50, app/environment: Nancy 0.20.
- F46, webForCore bug: fixed in [webForCore#44](https://github.com/jabba2324/webForCore/pull/44).

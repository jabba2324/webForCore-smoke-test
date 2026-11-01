# Opserver (MVC 5): migration notes

Status: blocked. Source: https://github.com/opserver/Opserver at `ed57f51d`, the parent of its ASP.NET Core migration
(`a47b1fc`, #355). Licence MIT. Round 4, on webForCore 0.5.0 and webForCore-thirdparty 0.4.1.

## Assessment

- **Frameworks:** MVC 5.2.6, forms authentication. Targets .NET 4.6.1.
- **Projects:**
  - `Opserver`: an old-style project with PackageReferences;
  - `Opserver.Core`: already SDK-style, `net461;netstandard2.0`.
- **Flagged blockers** (EF ObjectContext, LINQ to SQL): only type names in strings, in a table of generic parameter
  names (`Helpers/Utils.cs:376-382`). Not blockers.
- **Database:** none of its own. It monitors the servers its `Config/*.json` lists.

## Migration

1. `scripts/convert.py` on Opserver and Opserver.Core.
   - The converter now leaves an SDK-style project that has a .NET target alone. Opserver.Core builds as its
     `netstandard2.0` target. Before, it was rewritten as old-style: an empty `<Compile>` list removed all 178 of its
     sources.
   - System.DirectoryServices 4.5.0 → 11.0, which webForCore requires (F45).
2. `Opserver.Core/Settings/SettingsProviders/JsonFileSettingsProvider.cs`, in its `netstandard2.0` target: `~\Config\`
   resolves against the site, not `AppDomain.CurrentDomain.BaseDirectory`. On webForCore that's `bin_dotnet`
   (documented in `docs/MIGRATING.md`), a direct subfolder of the site. `\` becomes `/`.
3. `Config/SecuritySettings.config`: the example's "global access for everyone" (`provider="alladmin"`). The example
   uses Active Directory.
4. `Config/SQLSettings.json`: the compose SQL Server, in the example's format.

## Smoke

The probes cover the log-in page, two pages that need a log-in (redirects) and a static file.

- **On 0.5.0:** 9 of 10 checks pass. Every page but the redirects fails when Opserver builds its tabs, which scans every
  loaded assembly's types: `ReflectionTypeLoadException ... Could not load type 'SqlGuidCaster' from assembly
  'System.Data.SqlClient, Version=4.6.1.6' because it contains an object field at offset 0 that is incorrectly aligned or
  overlapped by a non-object field`. System.Data.SqlClient 4.9.1's Unix build doesn't load fully on .NET 11. It
  reproduces in a console app without webForCore, but webForCore brings the package to every app (F47,
  [#41](https://github.com/jabba2324/webForCore/issues/41)).
- **With the scan skipping types that fail to load** (exploration only, not in the overlay), Opserver works:
  - the log-in (a forms-authentication POST);
  - the MVC pages;
  - `/about`;
  - the SQL Server dashboard, live against the compose server, with version, uptime and CPU.

## Open problems

- F47: [webForCore#41](https://github.com/jabba2324/webForCore/issues/41). The fix is a choice for webForCore: lift and
  fix SqlClient, move to Microsoft.Data.SqlClient, or document catching the exception.
- F45, guide gap: [webForCore#42](https://github.com/jabba2324/webForCore/issues/42).

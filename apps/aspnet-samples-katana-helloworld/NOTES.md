# ASP.NET samples: Katana HelloWorld: migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

OWIN (Katana 2.1) on `Microsoft.Owin.Host.SystemWeb`: a `Startup` class whose `app.Run` answers every request with `Hello World` (text/plain). .NET 4.5. No blockers; only Katana packages, replaced by webForCore.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `HelloWorld.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore` and `webForCore.Microsoft.Owin.Host.SystemWeb` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state).

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 8/8 probes.
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 8/8 probes on Linux and on Windows.

**9 of 9 pass**: `/` and any other path return `Hello World` from the OWIN pipeline (the `OwinStartup` discovery and `Microsoft.Owin.Host.SystemWeb` work); the forbidden paths are 404 (the W3C log path gets the app's own `Hello World`, not the log).

### Round 2: webForCore 0.2.0

All 8 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 8 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

None.

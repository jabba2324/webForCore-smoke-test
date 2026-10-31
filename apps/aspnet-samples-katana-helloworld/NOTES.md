# ASP.NET samples: Katana HelloWorld: migration notes

Status: migrated. Source: https://github.com/aspnet/samples at `f7d4a776`, `samples/aspnet/Katana/HelloWorld`. Apache-2.0
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages.

## Assessment

OWIN (Katana 2.1) on `Microsoft.Owin.Host.SystemWeb`: a `Startup` class whose `app.Run` answers every request with `Hello World` (text/plain). .NET 4.5. No blockers; only Katana packages, replaced by webForCore.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `HelloWorld.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore` and `webForCore.Microsoft.Owin.Host.SystemWeb` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state).

## Smoke

**9 of 9 pass**: `/` and any other path return `Hello World` from the OWIN pipeline (the `OwinStartup` discovery and `Microsoft.Owin.Host.SystemWeb` work); the forbidden paths are 404 (the W3C log path gets the app's own `Hello World`, not the log).

## Open problems

None.

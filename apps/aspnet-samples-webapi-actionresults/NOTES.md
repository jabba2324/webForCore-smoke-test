# ASP.NET samples: Web API ActionResults: migration notes

Status: blocked. Source: https://github.com/aspnet/samples at `f7d4a776`, `samples/aspnet/WebApi/ActionResults/ActionResults`. Apache-2.0
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages.

## Assessment

Web API 2 attribute routing with custom `IHttpActionResult`s (text, file download, `NotFound`). .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `ActionResults.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore` and `webForCore.Web.Http.WebHost` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). Newtonsoft.Json 13.0.1 → 13.0.4 ([F7](../../FINDINGS.md#f7)).

## Smoke

**10 of 11 pass**: `/text` (`Hello, world!`), `/file` (the XML download), `/product/2` and `/product/99` (404 from `NotFound()`); the only failure is the served W3C log ([F4](../../FINDINGS.md#f4)).

## Open problems

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

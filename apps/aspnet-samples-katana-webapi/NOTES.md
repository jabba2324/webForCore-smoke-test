# ASP.NET samples: Katana Web API: migration notes

Status: blocked. Source: https://github.com/aspnet/samples at `f7d4a776`, `samples/aspnet/Katana/WebApi`. Apache-2.0
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages.

## Assessment

Web API 2 on OWIN (`UseWebApi`) under System.Web; the configuration removes the JSON formatter and uses `XmlSerializer`. .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `WebApi.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore`, `webForCore.Web.Http.Owin` and `webForCore.Microsoft.Owin.Host.SystemWeb` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). Newtonsoft.Json 13.0.1 → 13.0.4 ([F7](../../FINDINGS.md#f7)).

## Smoke

**8 of 9 pass**: `GET /Customer/7` returns `<Customer ...><FirstName>Mary</FirstName>...` through the OWIN-hosted Web API (XML, as configured); the only failure is the served W3C log ([F4](../../FINDINGS.md#f4)).

## Open problems

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

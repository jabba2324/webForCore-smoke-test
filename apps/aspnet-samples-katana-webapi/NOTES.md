# ASP.NET samples: Katana Web API: migration notes

Status: migrated (webForCore 0.2.0, all probes pass).
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

Web API 2 on OWIN (`UseWebApi`) under System.Web; the configuration removes the JSON formatter and uses `XmlSerializer`. .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `WebApi.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore`, `webForCore.Web.Http.Owin` and `webForCore.Microsoft.Owin.Host.SystemWeb` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). Newtonsoft.Json 13.0.1 → 13.0.4 ([F7](../../FINDINGS.md#f7)).

## Smoke

**8 of 9 pass**: `GET /Customer/7` returns `<Customer ...><FirstName>Mary</FirstName>...` through the OWIN-hosted Web API (XML, as configured); the only failure is the served W3C log ([F4](../../FINDINGS.md#f4)).

### Round 2: webForCore 0.2.0

All 8 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

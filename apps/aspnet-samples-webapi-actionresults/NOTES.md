# ASP.NET samples: Web API ActionResults: migration notes

Status: migrated (webForCore 0.9.0: 14/14 on Linux and Windows, 2 verb probes).
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

Web API 2 attribute routing with custom `IHttpActionResult`s (text, file download, `NotFound`). .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `ActionResults.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore` and `webForCore.Web.Http.WebHost` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). Newtonsoft.Json 13.0.1 → 13.0.4 ([F7](../../FINDINGS.md#f7)).

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 10/10 probes.
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 10/10 probes on Linux and on Windows.
- **Round 6 (webForCore 0.7.0 on .NET 10, webForCore-thirdparty 0.5.0; under webforcore-host; Linux and Windows):** 11/11 probes on Linux and Windows.

**10 of 11 pass**: `/text` (`Hello, world!`), `/file` (the XML download), `/product/2` and `/product/99` (404 from `NotFound()`); the only failure is the served W3C log ([F4](../../FINDINGS.md#f4)).

### Round 2: webForCore 0.2.0

All 10 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 10 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

### Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

Every action is a GET (attribute-routed `Get...` methods returning custom `IHttpActionResult`s), so the app has
nothing that takes another verb. Two verb probes check Web API's 405 for them (a DELETE of `/product/2`, a POST of a
form to `/text`), and a GET after them shows the product is still there. No overlay change. Results:
**14/14 on Linux and Windows** (CI runs 42021158598 and 42026762740), the same in both runs.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

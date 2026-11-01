# ASP.NET samples: Web API BSONSample: migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

Web API 2 with `BsonMediaTypeFormatter`, an MVC home page and the Web API HelpPage area (source package, checked in). .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `BSONSample.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore`, `webForCore.Web.Mvc`, `.Web.Http.WebHost`, `.Web.Helpers`, `.Web.WebPages.Deployment` and `.Web.Optimization` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). EntityFramework 6.0.2 → 6.5.1 ([F7](../../FINDINGS.md#f7)).

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 7/7 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28).

Not run: the build fails with `CS0433: The type 'HttpUtility' exists in both 'System.Web.HttpUtility' and 'System.Web'`, from the HelpPage area's code ([F8](../../FINDINGS.md#f8)). No probes yet.

### Round 2: webForCore 0.2.0

All 7 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 7 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F8](../../FINDINGS.md#f8) (webForCore bug): blocks the build.

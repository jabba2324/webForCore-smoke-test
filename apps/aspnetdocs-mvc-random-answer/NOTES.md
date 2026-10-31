# AspNetDocs: MVCRandomAnswerGenerator (Docker tutorial): migration notes

Status: migrated (webForCore 0.2.0, all probes pass).
(`LICENSE-CODE`; the docs are CC-BY-4.0). Split out of the `aspnetdocs-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

MVC 5.2.7 app from the Windows-containers tutorial: one controller with a GET and a POST action and a session-less list of previous answers. .NET 4.7.2. No blockers.

## Migration

Converter: `MVCRandomAnswerGenerator.csproj` → `.csproj.netfx`; SDK project with the `webForCore`, `webForCore.Web.Mvc`, `.Web.Helpers`, `.Web.WebPages.Deployment` and `.Web.Optimization` 0.1.0 packages; Newtonsoft.Json 13.0.1 → 13.0.4; `packages.config` and `<system.codedom>` removed; `Program.cs` added.

## Smoke

**8 of 9 pass**: the home page (layout, bundles) and About. Only the served W3C log fails ([F4](../../FINDINGS.md#f4)).

### Round 2: webForCore 0.2.0

All 8 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

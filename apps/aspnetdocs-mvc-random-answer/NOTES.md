# AspNetDocs: MVCRandomAnswerGenerator (Docker tutorial): migration notes

Status: migrated (webForCore 0.9.0, 13/13 probes on Linux and Windows).
(`LICENSE-CODE`; the docs are CC-BY-4.0). Split out of the `aspnetdocs-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

MVC 5.2.7 app from the Windows-containers tutorial: one controller with a GET and a POST action and a session-less list of previous answers. .NET 4.7.2. No blockers.

## Migration

Converter: `MVCRandomAnswerGenerator.csproj` → `.csproj.netfx`; SDK project with the `webForCore`, `webForCore.Web.Mvc`, `.Web.Helpers`, `.Web.WebPages.Deployment` and `.Web.Optimization` 0.1.0 packages; Newtonsoft.Json 13.0.1 → 13.0.4; `packages.config` and `<system.codedom>` removed; `Program.cs` added.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 8/8 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28).
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 8/8 probes on Linux and on Windows.
- **Round 6 (webForCore 0.7.0 on .NET 10, webForCore-thirdparty 0.5.0; under webforcore-host; Linux and Windows):** 9/9 probes on Linux and Windows.

**8 of 9 pass**: the home page (layout, bundles) and About. Only the served W3C log fails ([F4](../../FINDINGS.md#f4)).

### Round 2: webForCore 0.2.0

All 8 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 8 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

### Round 0.9.0: verbs (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

The app has one form, the question (`[HttpPost, ValidateAntiForgeryToken] Index`). Added 4 probes, 2 of them POSTs: the
page (its anti-forgery token captured), the question posted with the token and shown with its answer on the page it
returns, the same post without the token refused (500, the anti-forgery error), and the page without that question.
No other verbs: there is no API and nothing else to post. 13/13 on Linux (9 before), twice in a row.

**CI (round 0.9.0, run 42029051731):** 13/13 on Linux, 13/13 on Windows, the same in run 42024931148.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

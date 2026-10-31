# AspNetDocs: MVCRandomAnswerGenerator (Docker tutorial): migration notes

Status: blocked. Source: https://github.com/dotnet/AspNetDocs at `546ffe72`, `aspnet/mvc/overview/deployment/docker-aspnetmvc/samples/MVCRandomAnswerGenerator/MVCRandomAnswerGenerator`. Code under MIT
(`LICENSE-CODE`; the docs are CC-BY-4.0). Split out of the `aspnetdocs-samples` entry. webForCore 0.1.0 packages.

## Assessment

MVC 5.2.7 app from the Windows-containers tutorial: one controller with a GET and a POST action and a session-less list of previous answers. .NET 4.7.2. No blockers.

## Migration

Converter: `MVCRandomAnswerGenerator.csproj` → `.csproj.netfx`; SDK project with the `webForCore`, `webForCore.Web.Mvc`, `.Web.Helpers`, `.Web.WebPages.Deployment` and `.Web.Optimization` 0.1.0 packages; Newtonsoft.Json 13.0.1 → 13.0.4; `packages.config` and `<system.codedom>` removed; `Program.cs` added.

## Smoke

**8 of 9 pass**: the home page (layout, bundles) and About. Only the served W3C log fails ([F4](../../FINDINGS.md#f4)).

## Open problems

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

# ASP.NET samples: Web API WebFormSample: migration notes

Status: blocked. Source: https://github.com/aspnet/samples at `f7d4a776`, `samples/aspnet/WebApi/WebFormSample/WebForm`. Apache-2.0
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages.

## Assessment

Web API 2 controllers inside the Visual Studio 2012 Web Forms template (9 `.aspx`, ScriptManager packages, Universal Providers with LocalDB). .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `WebForm.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore` and `webForCore.Web.Http.WebHost` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). EntityFramework 6.0.0 → 6.5.1 ([F7](../../FINDINGS.md#f7)). Microsoft.AspNet.Providers.Core/LocalDb 2.0 and the ScriptManager packages are kept.

## Smoke

Not run: the build fails with `CS0433` on `HttpUtility` in the template's `ViewSwitcher.ascx.cs` ([F8](../../FINDINGS.md#f8)), as in eshop-legacy-webforms. No probes yet.

## Open problems

- [F8](../../FINDINGS.md#f8) (webForCore bug): blocks the build.

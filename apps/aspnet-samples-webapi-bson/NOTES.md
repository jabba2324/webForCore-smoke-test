# ASP.NET samples: Web API BSONSample: migration notes

Status: blocked. Source: https://github.com/aspnet/samples at `f7d4a776`, `samples/aspnet/WebApi/BSONSample/BSONSample`. Apache-2.0
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages.

## Assessment

Web API 2 with `BsonMediaTypeFormatter`, an MVC home page and the Web API HelpPage area (source package, checked in). .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `BSONSample.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore`, `webForCore.Web.Mvc`, `.Web.Http.WebHost`, `.Web.Helpers`, `.Web.WebPages.Deployment` and `.Web.Optimization` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). EntityFramework 6.0.2 → 6.5.1 ([F7](../../FINDINGS.md#f7)).

## Smoke

Not run: the build fails with `CS0433: The type 'HttpUtility' exists in both 'System.Web.HttpUtility' and 'System.Web'`, from the HelpPage area's code ([F8](../../FINDINGS.md#f8)). No probes yet.

## Open problems

- [F8](../../FINDINGS.md#f8) (webForCore bug): blocks the build.

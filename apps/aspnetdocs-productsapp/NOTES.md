# AspNetDocs: ProductsApp (Web API 2 tutorial): migration notes

Status: blocked. Source: https://github.com/dotnet/AspNetDocs at `546ffe72`, `aspnet/web-api/overview/advanced/calling-a-web-api-from-a-net-client/sample/server/ProductsApp`. Code under MIT
(`LICENSE-CODE`; the docs are CC-BY-4.0). Split out of the `aspnetdocs-samples` entry. webForCore 0.1.0 packages.

## Assessment

Web API 2 (5.2.x) getting-started server: `ProductsController` over an in-memory list, convention routes `api/{controller}/{id}`. .NET 4.6.1. No blockers; only Microsoft's Web API packages.

## Migration

Converter: `ProductsApp.csproj` → `.csproj.netfx`; SDK project with the `webForCore` and `webForCore.Web.Http.WebHost` 0.1.0 packages; Newtonsoft.Json 13.0.1 → 13.0.4 ([F7](../../FINDINGS.md#f7)); `packages.config` and `<system.codedom>` removed; `Program.cs` added. The old project lists `Properties/AssemblyInfo.cs`, which isn't in the repository.

## Smoke

**10 of 11 pass**: `GET /api/products` (JSON list), `/api/products/2`, `/api/products?category=Hardware` (action selection by query string) and `/api/products/99` (404 from `HttpResponseException`). Only the served W3C log fails ([F4](../../FINDINGS.md#f4)).

## Open problems

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

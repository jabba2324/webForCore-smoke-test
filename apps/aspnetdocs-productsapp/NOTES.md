# AspNetDocs: ProductsApp (Web API 2 tutorial): migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
(`LICENSE-CODE`; the docs are CC-BY-4.0). Split out of the `aspnetdocs-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

Web API 2 (5.2.x) getting-started server: `ProductsController` over an in-memory list, convention routes `api/{controller}/{id}`. .NET 4.6.1. No blockers; only Microsoft's Web API packages.

## Migration

Converter: `ProductsApp.csproj` → `.csproj.netfx`; SDK project with the `webForCore` and `webForCore.Web.Http.WebHost` 0.1.0 packages; Newtonsoft.Json 13.0.1 → 13.0.4 ([F7](../../FINDINGS.md#f7)); `packages.config` and `<system.codedom>` removed; `Program.cs` added. The old project lists `Properties/AssemblyInfo.cs`, which isn't in the repository.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 10/10 probes.
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 10/10 probes on Linux and on Windows.
- **Round 6 (webForCore 0.7.0 on .NET 10, webForCore-thirdparty 0.5.0; under webforcore-host; Linux and Windows):** 11/11 probes on Linux and Windows.

**10 of 11 pass**: `GET /api/products` (JSON list), `/api/products/2`, `/api/products?category=Hardware` (action selection by query string) and `/api/products/99` (404 from `HttpResponseException`). Only the served W3C log fails ([F4](../../FINDINGS.md#f4)).

### Round 2: webForCore 0.2.0

All 10 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 10 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

### Probes with verbs (C# runner, November 2026)

Nine probes more, on webForCore 0.8.0: the API's whole CRUD on its in-memory repository. A JSON `POST` (201, the new
product's id captured from `Location`), a `GET` of it, a `PUT` (204), the changed product (by id, and in
`?category=Hardware`), a `DELETE` (204) and a 404 after it; a form-encoded `POST` (Web API binds it like JSON), and a
`PUT` to a product that isn't there (404). 20 checks in all (13 probes, 7 forbidden paths), all passing on Linux and Windows (CI run 41940185337).

### Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

No change to the overlay or the probes: the verb probes above (POST as JSON and as a form, PUT, DELETE, and a PUT to
a product that isn't there; 5 of the 20 checks) already cover everything the API does. Results: RESULTS_PLACEHOLDER

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

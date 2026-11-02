# Clean Architecture in ASP.NET MVC 5: migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
`5cbeba34` (master, 2018). BSD-3-Clause. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

- **Type:** MVC 5.2 (`System.Web.Mvc` 5.2.3), one project at the repository root, .NET Framework 4.5,
  `targetFramework="4.5"`. Feature folders (`Home/`, `Products/`, `Contact/`, each with `Controllers/` and `Views/`)
  and a `CustomRazorViewEngine` that finds views at `~/{1}/Views/{0}.cshtml`. `RouteConfig` sets
  `routes.RouteExistingFiles = true`, because the folders share their names with the controllers. Data is an
  in-memory list; no database.
- No `Views/web.config`: the Razor configuration is in the root web.config, and `.cshtml` files are blocked by a
  `BlockViewHandler` (`HttpNotFoundHandler`) in `<system.webServer><handlers>`.
- **Blockers:** none. **Dependencies:** only Microsoft's MVC packages (replaced by webForCore).
- **Go.**

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`, kept in the job's scratch folder):

1. `CleanArchitectureAspNetMvc5.csproj` → `.csproj.netfx`; SDK-style project with the `webForCore`,
   `webForCore.Web.Mvc` and `webForCore.Web.Helpers` 0.1.0 packages. The old `<Compile>` list equals the glob.
   `packages.config` removed.
2. `Program.cs` (no session state).

No change to the app's code or web.config (it has no `<system.codedom>`).

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 11/11 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28).
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 11/11 probes on Linux and on Windows.

Probes: the home page, the products list and a product, the contact page, and a view file requested directly (must be
404).

**10 of 12 pass.** `/`, `/Products/Details/2` (custom view engine, feature-folder views, layout), the blocked
`.cshtml` (404 from the app's `BlockViewHandler`) and the 7 forbidden paths are right. `/Products` and `/Contact` get
`301 Moved Permanently` to `/Products/` and `/Contact/`: webForCore's trailing-slash redirect for existing directories
runs before routing, although `RouteExistingFiles = true` gives the URL to the controller on IIS
([F14](../../FINDINGS.md#f14)). With the slash, both pages render.

Not probed: static files. With `RouteExistingFiles = true` every request is routed, so `/Content/Site.css` matches
`{controller}/{action}` and MVC answers 404; that is what IIS's integrated pipeline does too, as far as I can tell (not
compared with IIS), so the site's CSS isn't served on either. Not counted as a finding.

### Round 2: webForCore 0.2.0

All 11 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 11 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F14](../../FINDINGS.md#f14) (webForCore bug): directory redirect pre-empts routing.

# Clean Architecture in ASP.NET MVC 5: migration notes

Status: blocked (webForCore bug F14). Source: https://github.com/matthewrenze/clean-architecture-in-asp-net-mvc-5 at
`5cbeba34` (master, 2018). BSD-3-Clause. webForCore 0.1.0 packages.

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

## Open problems

- [F14](../../FINDINGS.md#f14) (webForCore bug): directory redirect pre-empts routing.

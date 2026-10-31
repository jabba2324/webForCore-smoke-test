# eShopLegacyWebForms (eShopModernizing): migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
`63bc9ec4` (archived; same commit as eshop-legacy-mvc). webForCore 0.1.0
from its GitHub release packages (first migrated against the source at `4ec57209`; the results are the same).

## Assessment

- **Type:** Web Forms (the Visual Studio 4.5 template: Site.Master, Site.Mobile.Master, ViewSwitcher, FriendlyUrls,
  ScriptManager with MS AJAX bundles), `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms`, .NET Framework 4.7.2,
  `<httpRuntime targetFramework="4.6.1" requestValidationMode="2.0">`. Page routes (`MapPageRoute`) for the catalog
  pages, ListView with `ItemType` model binding, Autofac.Web property injection modules, EF 6.2, log4net,
  Application Insights 2.9, bundling (`BundleConfig` and `Bundle.config`, `<webopt:BundleReference>`).
- `UseMockData` is `true` in the repo, so no database is needed.
- **Blockers:** none found.
- **Dependencies:** (a) replaced by webForCore: Microsoft.AspNet.Web.Optimization(+.WebForms), WebGrease, Antlr,
  Microsoft.Web.Infrastructure, CodeDom providers. (b) kept: Autofac 4.9.1, log4net, Newtonsoft.Json, EntityFramework
  (upgraded). (c) .NET Framework-only, tested: Autofac.Web 4.0.0, Microsoft.AspNet.FriendlyUrls(.Core) 1.0.2,
  Microsoft.AspNet.ScriptManager.MSAjax/.WebForms 5.0.0, AspNet.ScriptManager.jQuery/.bootstrap, Application Insights
  Web 2.9.1, TelemetryCorrelation 1.0.5, SessionState.SessionStateModule 1.1.0. None of these depends on Microsoft's
  System.Web.* packages, so F5 doesn't apply here.
- **Go.**

## Migration

1. `eShopLegacyWebForms.csproj` → `.csproj.netfx`; SDK-style project per `docs/MIGRATING.md` section 2, the `webForCore`
   package plus `webForCore.Web.Optimization` and `.Web.Optimization.WebForms`, 0.1.0. The old `<Compile>` list equals the
   glob. `packages.config` removed.
2. Packages as in eshop-legacy-mvc: EntityFramework 6.5.1, Newtonsoft.Json 13.0.4 ([F7](../../FINDINGS.md#f7));
   `Microsoft.AI.Web.dll` referenced from the package ([F6](../../FINDINGS.md#f6)).
3. `Program.cs` (session state: `Session_Start`; `HandleAllRequestsWithWebForms()` for the page routes).
4. web.config: `<system.codedom>` removed. The LocalDB connection string is left alone (unused with mock data).
5. `Bundle.config`: `~/Content/Site.css` → `~/Content/site.css`, the file's real name (guide section 6).

**Build: fails** with `CS0433: The type 'HttpUtility' exists in both 'System.Web.HttpUtility' and 'System.Web'` in
`ViewSwitcher.ascx.cs:39` ([F8](../../FINDINGS.md#f8)). Not worked around in the overlay.

## Smoke

Probes: the catalog list (`/`, ListView with `ItemType`), the paged route `/Default/index/1/size/10`, the details page
route, the Create form (`__VIEWSTATE`), `/About`, a static CSS file, the `~/bundles/MsAjaxJs` bundle and a static PNG.

**Exploration past F8 and F1** (not committed: webForCore's `WebForCoreDropFrameworkHttpUtility` target copied into
the project, and the session module lines removed from web.config):

- `/` and `/Default/index/1/size/10`: 500, `Could not load file or assembly 'System.Web.DynamicData'` from the
  ListView's `ItemType` ([F9](../../FINDINGS.md#f9)).
- `/Catalog/Create` renders; the postback (view state, event validation, the button's `WebForm_DoPostBackWithOptions`
  name, dropdowns) returns 302 to `/`.
- `/Catalog/Details/1`: 500, `NullReferenceException` at `Details.aspx:28` (`product.CatalogBrand.Brand`). The mock
  service's `FindCatalogItem` returns items without their brand, so this fails on IIS too (the app's own bug; the MVC
  twin's `DisplayFor` tolerates the null).
- `/About` and `/Contact`: 404. The app doesn't call `EnableFriendlyUrls`, so extensionless page URLs don't exist on
  IIS either; `/About.aspx` is the URL. Probe changed to that.
- Static CSS, the MS AJAX script bundle, the CSS bundle (with WebGrease's "Minification failed" header for Bootstrap 4,
  as on IIS) and PNGs are served. The Autofac.Web, TelemetryCorrelation and Application Insights modules load.
- With the faithful web.config, every request fails on [F1](../../FINDINGS.md#f1), as in eshop-legacy-mvc.

### Round 2: webForCore 0.2.0

All 14 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 14 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F8](../../FINDINGS.md#f8) (webForCore bug): `HttpUtility` is ambiguous in compiled app code. Blocks the build.
- [F1](../../FINDINGS.md#f1) (webForCore bug): the session module can't read web.config. Blocks every request.
- [F9](../../FINDINGS.md#f9) (documented gap): ListView `ItemType` needs System.Web.DynamicData. Blocks the home page.
- App bug: Details page NullReference with mock data (same on IIS).

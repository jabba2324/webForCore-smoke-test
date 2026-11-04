# AspNetDocs: Wingtip Toys (Web Forms 4.5 + EF 6 tutorial): migration notes

Status: blocked (webForCore 0.7.0, 16/19 probes on Linux, 17/19 on Windows; F80, F81 and F82 are webForCore bugs). Source: the
completed project of the AspNetDocs tutorial "Getting Started with ASP.NET 4.5 Web Forms and Visual Studio 2013"
(`aspnet/web-forms/overview/getting-started/getting-started-with-aspnet-45-web-forms` in dotnet/AspNetDocs). The
repository has only the tutorial's snippets; the tutorial links the project on the retired MSDN Code Gallery
(`go.microsoft.com/fwlink/?LinkID=389434`), and Microsoft's archive of the gallery,
[microsoftarchive/msdn-code-gallery-community-d-l](https://github.com/microsoftarchive/msdn-code-gallery-community-d-l),
keeps it, Apache-2.0 (the sample's README). Packages: webForCore 0.7.0, webForCore-thirdparty 0.6.0 (not used),
drawingForCore 0.2.0. webForCore 0.8.0 wasn't released when this was migrated.

The checkout is the whole archive repository (about 1.3 GB of samples), as the runner clones repositories whole, and
the site's path in it is long (about 200 characters); CI's Windows job checks it out and builds it (the runner sets `core.longpaths`).

## Assessment

- **Type:** Web Forms, the Visual Studio 2013 template with individual accounts: Site.Master and Site.Mobile.Master,
  ViewSwitcher, FriendlyUrls (`AutoRedirectMode = Permanent`), ScriptManager with the MS AJAX/WebForms script
  packages, bundling (`Bundle.config`, `<webopt:bundlereference>`), ASP.NET Identity 2.1 on OWIN cookies (an `Account`
  folder of 15 pages), plus the tutorial's store: `ListView`/`FormView` model binding with `ItemType`
  (`SelectMethod`, `[QueryString]`, `[RouteData]`), page routes (`Category/{categoryName}`, `Product/{productName}`),
  a session-keyed shopping cart, PayPal checkout (NVP API in the sandbox), an `Admin` folder for the `canEdit` role,
  ELMAH 1.2.2, `Application_Error` with `Server.Transfer` to `ErrorPage.aspx`, `customErrors mode="On"`.
  .NET 4.5.2 (`<httpRuntime targetFramework="4.5.2">`).
- **Data:** EF 6.1.1 code first, `ProductContext` with `ProductDatabaseInitializer` (`DropCreateDatabaseIfModelChanges`,
  seeds the catalogue) and Identity's `ApplicationDbContext`; `Application_Start` creates the `canEdit` role and user.
  Two LocalDB connection strings (one with `AttachDbFilename`).
- **Blockers:** none (`scripts/wfc.py assess`).
- **Dependencies:** (a) replaced by webForCore: Microsoft.AspNet.Identity.*, Web.Optimization(.WebForms), Katana
  (`Microsoft.Owin`, `.Host.SystemWeb`, `.Security`, `.Cookies`, `.OAuth`), Owin, WebGrease, Antlr,
  Microsoft.Web.Infrastructure. (b) kept: EntityFramework (6.1.1 → 6.5.1, F7), Newtonsoft.Json (6.0.3 → 13.0.4),
  content packages. (c) .NET Framework-only, kept: elmah.corelibrary 1.2.2, Microsoft.AspNet.FriendlyUrls(.Core)
  1.0.2, Microsoft.AspNet.Providers.Core 2.0.0 (named by `<sessionState customProvider>`, unused with `InProc`),
  Microsoft.AspNet.ScriptManager.MSAjax/.WebForms 5.0.0, AspNet.ScriptManager.jQuery/.bootstrap, and the template's
  Katana providers `Microsoft.Owin.Security.Facebook/.Google/.MicrosoftAccount/.Twitter` 2.1.0 (F15; Google is
  registered with placeholder keys).
- **Go.**

## Migration

1. `scripts/convert.py WingtipToys.csproj` (`WEBFORCORE_VERSION=0.7.0`, `THIRDPARTY_VERSION=0.6.0`):
   `WingtipToys.csproj.netfx` and `packages.config.netfx` kept; SDK project with `webForCore`,
   `.Web.Optimization(.WebForms)`, `.Microsoft.Owin.Host.SystemWeb`, `.Microsoft.Owin.Security.Cookies`,
   `.Microsoft.Owin.Security.OAuth`, `.Microsoft.AspNet.Identity.Owin/.EntityFramework/.Core` and `.WebGrease` 0.7.0;
   the app's other packages kept; `Program.cs` with session state. All 84 compiled files kept, none added.
2. `Web.config`: both LocalDB connection strings pointed at the compose SQL Server (catalogs `aspnet-WingtipToys`
   and `wingtiptoys`; no `AttachDbFilename`).

No change to the code.

## Smoke

**Linux, webForCore 0.7.0: 16/19; Windows (CI run 41900268720): 17/19**, the same but for `/elmah.axd`, which works there (F82 is Linux only). Pass: `/ProductList` (the catalogue from the seeded database, model binding),
`/Category/Planes` (page route with `[RouteData]`), `/ProductDetails?productID=2` (`[QueryString]`), `/ShoppingCart`,
`/Account/Login` and `/Account/Register` (Identity pages, view state), `/About` (FriendlyUrls' extensionless URL),
`/Admin/AdminPage` (302 to the login page: `<authorization>` for the `canEdit` role and the OWIN cookie
middleware's 401 handling), `Site.css`, and the paths that mustn't be served. Fail:

- `/`: `301 Location: /Default` ([F81](../../FINDINGS.md#f81)); `/Default` renders.
- `/Product/Convertible%20Car`: 400, empty body ([F80](../../FINDINGS.md#f80)). Every product link on the product list
  is like this (the seeded names have spaces); a path without an escaped character, `/Product/Fast-Car`, is routed
  to the page.
- `/elmah.axd`: 200 with an empty body ([F82](../../FINDINGS.md#f82)).

`/ProductDetails.aspx?productID=2` answers `301 Location: /ProductDetails?productID=2`: FriendlyUrls' permanent
redirect for `.aspx` URLs, as on IIS, so the probe uses the extensionless URL.

By hand:
- Logging in as the seeded `canEditUser@wingtiptoys.com` (a Web Forms postback with view state and event
  validation; Identity's `SignInManager` on EF) returns 302 and sets the cookie; `/Admin/AdminPage` then renders its
  add and remove product forms.
- `/AddToCart?productID=1` adds the item (it is in `CartItems`) and redirects to `ShoppingCart.aspx`, but the cart
  shows "Shopping Cart is Empty": the session the cart ID is kept in is lost. The response to `AddToCart`, which ends
  with `Response.Redirect`, sets `ASP.NET_SessionId` but not 0.7.0's second session cookie `.AspNetCore.Session`
  (webForCore#79), so the next request gets a new session and a new cart ID. On IIS the cart shows the item. 0.8.0
  replaces this provider (one cookie, state in the distributed cache).
- `/AddToCart` without a product ID throws in the page; `Application_Error`'s `Server.Transfer` to `ErrorPage.aspx`
  renders the error page, as on IIS.
- `/elmah.axd/about` and `/elmah.axd/stylesheet` work; `/rss` and `/json` fail with F82's exception.

Runs on this machine with several other agents' servers up also hit "Failed to start monitoring changes" (the user's
128 inotify instances used up), the documented limit (webForCore `docs/IIS-COMPATIBILITY.md`, #107), not this app.

Not covered: the PayPal checkout (it calls PayPal's sandbox) and external logins (Google with placeholder keys).

## Open problems

- [F80](../../FINDINGS.md#f80) (webForCore bug, [#126](https://github.com/xforcore/webForCore/issues/126)): paths
  with a space answer 400.
- [F81](../../FINDINGS.md#f81) (webForCore bug, [#127](https://github.com/xforcore/webForCore/issues/127)): `RawUrl`
  after the default document; FriendlyUrls redirects `/`.
- [F82](../../FINDINGS.md#f82) (webForCore bug, [#128](https://github.com/xforcore/webForCore/issues/128)):
  `Server.MachineName` on Linux; ELMAH.

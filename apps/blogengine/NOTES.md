# BlogEngine.NET: migration notes

Status: blocked (LINQ to SQL; not pursued). Source: https://github.com/BlogEngine/BlogEngine.NET
at `95c84261` (master, 2023). MS-RL (stated in README.md; the repo has no licence file). Not migrated.

## Assessment

- **Type:** Web Forms (16 `.aspx`, CodeBehind) with Web API 2 (5.2.3) for the Angular admin, Razor `.cshtml` admin
  pages (Web Pages 3), SimpleInjector, bundling. .NET Framework 4.5. Two projects: `BlogEngine/BlogEngine.NET` (the
  site) and `BlogEngine/BlogEngine.Core` (a class library with the providers, the extension manager and the
  expression builders). Data is XML files in `App_Data` by default; SQL Server, MySQL, SQL CE and SQLite are optional
  providers (`setup/`).
- **Blockers** (the runner's scan now follows the site's project references, which is how the first two showed up):
  - **LINQ to SQL** (`docs/NAMESPACES.md`: not available): `BlogEngine.Core/Services/FileSystem/FileStoreDb.cs`, a
    `System.Data.Linq.DataContext` used by `Providers/FileSystemProviders/DbFileSystemProvider.cs` (the optional
    database-backed file store); `Services/Compilation/LinqLengthExpressionBuilder.cs:11` uses
    `System.Data.Linq.Mapping`. It is compiled code, so the library doesn't build until something changes. The default
    configuration uses the XML file system provider, not this one.
  - `<system.serviceModel><services>` in `Web.Config:27` (and the `setup/*Web.Config` templates): declares
    ASP.NET's `AuthenticationService` for WCF, but there is no `.svc` file, so nothing is hosted. Not a blocker.
  - `WindowsIdentity.GetCurrent().Name` in `admin/about.cshtml:82`, `setup/upgrade/index.cshtml:63` and
    `AppCode/Api/ToolsController.cs:20`: diagnostics that show the process identity. They will throw
    `PlatformNotSupportedException` on Linux (an admin page and one API call); not a blocker for the blog.
- **Other code to change:** `BinaryFormatter` in `BlogEngine.Core/Web/Extensions/ManagedExtension.cs`
  (documented replacement). `System.Data.Services.Client` (WCF Data Services, OData v3) for the extension gallery's
  service reference (`Service References/GalleryServer`): not part of .NET; the `Microsoft.Data.Services.Client`
  package would have to be tried. `System.Web.UI.Design` in `Services/Compilation/Design/*` (designer-only editors for
  the expression builders): not in webForCore; design-time only.
- **Dependencies:** (a) replaced by webForCore: Microsoft.AspNet.WebApi.*, WebPages, Razor, Web.Optimization,
  WebGrease, Antlr, Microsoft.Web.Infrastructure. (b) packages: Newtonsoft.Json (6.0.8 and 7.0.1, both below
  webForCore's 13.0.4), SimpleInjector 3.1 (+ WebApi integration, ExecutionContextScoping), Microsoft.Web.Xdt 2.1.1,
  NuGet.Core 2.8.2 (for the extension gallery; net40-client only), DynamicQuery 1.0. (c) checked-in binaries in
  `lib/`: AjaxMin, BlogML, ICSharpCode.SharpZipLib.

**Decision (2026-10-31):** webForCore's recommendation for LINQ to SQL is to move the code to EF 6 (`docs/NAMESPACES.md`:
"EF 6.4+ or EF Core"), and that rewrite is the app developers' job, not a platform change, so this test bed doesn't
pursue it. BlogEngine stays blocked: `FileStoreDb` and its users would have to be rewritten on EF 6 before the rest
of the migration is worth doing. Not a webForCore finding (documented gap).

## Migration

Not started (see the decision above).

## Smoke

(Not run.)

## Open problems

- LINQ to SQL in BlogEngine.Core (documented gap): to be rewritten on EF 6 by the app; not pursued here.

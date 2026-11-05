# BlogEngine.NET: migration notes

Status: migrating (webForCore 0.9.0: 48/48 on Linux; LINQ to SQL is provided now). Source: https://github.com/BlogEngine/BlogEngine.NET
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

**Round 0.9.0 (5 November 2026):** webForCore 0.9.0 provides System.Data.Linq (`webForCore.Data.Linq`, which the
`webForCore` package brings), so the decision above no longer applies: `FileStoreDb` and `LinqLengthExpressionBuilder`
compile unchanged, and the migration went ahead on webForCore 0.9.0 and webForCore-thirdparty 0.7.0 (no package of
the latter is needed). The default XML provider (`App_Data`) needs no database, so there's no `[setup]`.

## Migration

1. `scripts/convert.cs BlogEngine.NET/BlogEngine.NET.csproj --lib BlogEngine.Core/BlogEngine.Core.csproj`: SDK
   projects on net10.0 with the `webForCore` 0.9.0 package (the site also Web API's web host, Web Pages, Razor,
   Web.Helpers, Optimization and WebGrease), `Program.cs`, Newtonsoft.Json 6.0.8/7.0.1 → 13.0.4,
   System.ServiceModel.Syndication and System.DirectoryServices as packages, the `lib/` DLLs by hint path. Five `.cs`
   files on disk that the old Core project didn't compile are left out. `app.toml` gets `project`.
2. Both projects `<LangVersion>7.3</LangVersion>` (they set none; MIGRATING.md section 2), and BlogEngine.Core
   `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (a class library that references webForCore, #62).
3. BlogEngine.Core:
   - `Services/Compilation/Design/**` out of the build: four `ExpressionEditor` subclasses, design-time only
     (System.Design; MIGRATING.md, #144). The expression builders name them by string, so nothing else changes.
   - `Microsoft.Data.Services.Client` 5.8.4 for the extension gallery's service reference
     (`System.Data.Services.Client`'s successor; MIGRATING.md, #149).
   - `Web/Extensions/ManagedExtension.cs`: webForCore's `BinaryFormatter`
     (`WebFormsForCore.Serialization.Formatters.Binary`, package `webForCore.Serialization.Formatters`), as the guide
     says; .NET's is `SYSLIB0011`.
   - `Helpers/Utils.cs` `GetCompiledExtensions`: `HttpRuntime.BinDirectory` instead of `MapPath("~/") + "bin"`
     (MIGRATING.md section 6, the build output is `bin_dotnet/`). The extensions (`Custom/Extensions`, compiled into
     the site's assembly) weren't found, so none loaded, and `/contact` was a `NullReferenceException` reading the
     Recaptcha extension's settings.
4. The site:
   - `System.Management` package: web.config's `<compilation><assemblies>` lists it, and every page was a
     Configuration Error ("Could not load file or assembly 'System.Management'"; MIGRATING.md, packages on .NET).
   - SimpleInjector 3.1.0 → 3.3.2 (with `.Extensions.ExecutionContextScoping` and `.Integration.WebApi`). Every Web API
     call was "Make sure that the controller has a parameterless public constructor": ExecutionContextScoping 3.1 is a
     net45-only build that uses `CallContext`, `TypeLoadException: Could not load type
     'System.Runtime.Remoting.Messaging.CallContext' from assembly 'mscorlib'` (F66: webForCore's `CallContext` serves
     only code compiled against webForCore). 3.3.2 has a .NET Standard 1.3 build (`AsyncLocal`), which NuGet picks.
   - `admin/about.cshtml`, `setup/upgrade/index.cshtml` and `AppCode/Api/ToolsController.cs` show the process's
     identity with `WindowsIdentity.GetCurrent().Name`, which throws `PlatformNotSupportedException` off Windows
     (NAMESPACES.md: Windows identity). Off Windows they show `Environment.UserName`.

web.config is unchanged.

## Smoke

**Round 0.9.0, Linux: 48/48** (41 of the app's probes and the runner's 7 forbidden paths), twice in a row, and from a
fresh checkout.

Pages: the home page, a post, a page, the archive, a category, a tag, search, the contact form (the extensions
loaded), RSS, Atom, the site map, OpenSearch and RSD (`.axd` handlers), the log-in form, `/admin/` (302 to the
log-in), a stylesheet and a script.

Verbs (with the log-in's cookie, in one jar):
- the log-in, a Web Forms postback (admin/admin, `App_Data/users.xml`'s default account), then the Angular admin's
  shell, its About page and the tools API's identity check;
- the Web API the admin uses: a post made (`POST /api/posts`, 201), shown at its slug, changed (`PUT
  /api/posts/update/foo`, as the post editor sends it), read back (`GET /api/posts/{id}`);
- a comment on it, sent as the comment form's script sends it: an ASP.NET client callback (`WebForm_DoCallback`:
  the page's view state and event validation, `__CALLBACKID` and `__CALLBACKPARAM`, the fields joined by `-|-` with
  the simple CAPTCHA's answer); the admin's, since an anonymous comment e-mails the owner through `smtp.gmail.com`
  (`App_Data/settings.xml`), an outside service (seen in `App_Data/logger.txt` when tried by hand). It shows at once
  (trusted authenticated users), is unapproved (`PUT /api/comments/processchecked/unapprove`: gone for visitors),
  approved again (shown), and deleted (`DELETE /api/comments/{id}`);
- the post deleted (`DELETE /api/posts/{id}`, 404 after), the trash emptied (`PUT /api/trash/purgeall/all`, so the
  next run gets the same slug; `GET /api/trash` shows none), and log-off (`/Account/login.aspx?logoff`), after which
  `/admin/` redirects to the log-in again.

Not probed: the contact form's post and the newsletter (e-mail), the extension gallery and package installs (an
outside feed), and enabling or disabling an extension, which asks for a restart by touching
`PhysicalApplicationPath + "\\web.config"`: on Linux that path doesn't exist (`\` and the file's `Web.Config` case),
so `Utils.RecycleIIS` logs "Could not find file" and the application doesn't restart (F156, app/environment; it
logged that once, at the first start, with nothing visibly wrong).

## Open problems

- F156 (app/environment): `Utils.RecycleIIS` builds a Windows path to web.config; on Linux the restart it asks for
  doesn't happen.

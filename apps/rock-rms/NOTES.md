# Rock RMS: migration notes

Status: rejected (the licence isn't open source). Source: https://github.com/SparkDevNetwork/Rock at `85705ceb` (tag
19.8.1, the latest release tag; the 20.x and 21.x tags are `-pre`/`-alpha`).

## Licence

Every source file is "Licensed under the Rock Community License" (http://www.rockrms.com/license; the repository has
no licence file). That licence is Apache 2.0's text under a preamble that limits it:

> THIS LICENSE PERMITS THE USE, REPRODUCTION, PREPARATION OF DERIVATIVE WORKS OF, PUBLIC DISPLAY, PUBLIC PERFORMANCE,
> SUBLICENSE, AND DISTRIBUTION OF THE WORK AND DERIVATIVE WORKS SOLELY FOR INTERNAL USE BY FAITH BASED ORGANIZATIONS.
> FOR ANY USE, ... AND DISTRIBUTION OF THE WORK AND DERIVATIVE WORKS FOR ANY OTHER PURPOSE, PLEASE CONTACT SPARK
> DEVELOPMENT, INC. FOR A COMMERCIAL LICENSE

A field-of-use restriction makes it source-available, not open source (the Open Source Definition, point 6), and an
`overlay.patch` published here would be a derivative work distributed for another purpose. So the app is rejected at
CLAUDE.md step 1, as aspnetdocs-bookservice was for having no licence: no migration, no overlay, no CI job. The
assessment below was made from the public source (read only) before that, and is kept because its blocker list
holds for any ASP.NET 4.x app of this shape.

## Assessment

Round 7's packages (webForCore 0.8.0, webForCore-thirdparty 0.6.0, drawingForCore 0.3.0); nothing was built.

- **Frameworks:** Web Forms (69 `.aspx`, 353 `.ascx` blocks, 23 `.ashx`; Rock's pages are database-driven, each a
  layout of blocks that `BlockTypeService` compiles by path with `BuildManager.GetCompiledType`), Web API 2 with OData
  5.7 (`Rock.Rest`, 755 files, and Swashbuckle 5.6), OWIN (`RockWeb/App_Code/Startup.cs`, `[assembly: OwinStartup]`,
  with Katana 4.2.2 OpenID Connect server for `Rock.Oidc`), SignalR 2.4.3 (`RockMessageHub`), EF 6.4.4 with
  `Z.EntityFramework.Plus.EF6`, bundling (`Microsoft.AspNet.Web.Optimization`), and the Obsidian (Vue) front end's
  blocks in `Rock.Blocks`.
- **Target:** .NET Framework 4.7.2 (`<compilation targetFramework="4.7.2">`), so 4.5+ behaviour. `<trust
  level="Full">`, Forms authentication, an explicit `<machineKey>`, `connectionStrings configSource=
  "web.ConnectionStrings.config"`, a `<system.codedom>` entry for `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`
  3.6.0 (to remove).
- **Project shape: a Web Site Project.** `RockWeb` has no `.csproj` (it's a `{E24C65DC-...}` web site in `Rock.sln`):
  every page and control is `CodeFile=` (364, no `CodeBehind=`), `Global.asax`'s class is in `App_Code` (25 files),
  and `Bin/` has 165 `*.dll.refresh` files plus what the libraries copy there (`Directory.Build.targets`,
  `CopyToRockWeb`). The runner's `build` stage and `convert.py` need a web project (`build` failed with "no project
  file" on the first fetch). webForCore runs such a site, but its guide doesn't say how to make the project: the
  template compiles the `CodeFile=` files (F123, guide gap, [#131](https://github.com/xforcore/webForCore/issues/131)).
- **Libraries:** the web site depends on 34 projects; the ones it loads are `Rock` (4,699 `.cs` files), `Rock.Rest`,
  `Rock.Blocks`, `Rock.WebStartup`, `Rock.Migrations` (1,557 files: the database is created and upgraded by EF
  migrations at start-up, so there's no separate installer to run against SQL Server), `Rock.Lava`, `.Lava.Fluid`,
  `.Lava.Shared`, `Rock.ViewModels`, `Rock.Enums`, `Rock.Common` (the last four already multi-target `net9.0`),
  `Rock.Oidc`, `Rock.Version`, `DotLiquid` and Rock's own `Quartz` fork, plus the payment, mail and AI plug-ins.
- **Blockers flagged by `assess`** (it scans `RockWeb` only, as there's no project to follow):
  - `linq-to-sql` (48 hits): all false positives, Rock's own `Grid.SetLinqDataSource( IQueryable )` (EF) and comments
    naming it (`RockWeb/Blocks/Administration/PageViews.ascx.cs:198`, ...). No `System.Data.Linq` anywhere.
  - `windows-auth`: `RockWeb/App_Code/SqlServerTypes/Loader.cs:72`, `WindowsIdentity.GetCurrent()` in the error message
    of the next blocker.
- **Blockers found by hand** in the libraries:
  - **Native SQL Server spatial types (Windows only).** `Microsoft.SqlServer.Types` 11.0.2 (`SqlGeography`, in
    `Rock/Model/Core/Location/Location.Logic.cs:29` and 3 more files) and EF's `DbGeography` (36 files: locations,
    group finders, geofences) need `SqlServerSpatial110.dll`, which `SqlServerTypes.Utilities.LoadNativeAssemblies`
    loads with `kernel32!LoadLibrary` (`RockWeb/App_Code/SqlServerTypes/Loader.cs:28`, called unconditionally from
    `Application_Start`, `RockWeb/App_Code/Global.asax.cs:205`). On Linux that's a `DllNotFoundException` at start-up;
    on .NET the package would have to become `Microsoft.SqlServer.Types` 160 (which has no Linux native library), so
    geography features are Windows only. An app dependency (group c), not webForCore's.
  - **Named `EventWaitHandle`:** `Rock/Model/Core/ExceptionLog/ExceptionLogService.cs:322`, a static field, so
    `ExceptionLogService`'s type initialiser throws `PlatformNotSupportedException` on Linux the first time it's used. Its name has a new GUID
    per process, so an unnamed handle behaves the same (docs/MIGRATING.md section 6; ask before changing).
  - **`~/Bin` paths:** `Rock.WebStartup/RockApplicationStartupHelper.cs:1512` (an `AssemblyResolve` handler loading
    `<BaseDirectory>/Bin/<name>.dll`), `Rock.Rest/Swagger/SwaggerConfig.cs:59` and `SwaggerConfigV2.cs:137` (the XML
    docs in `bin`), `Rock.Blocks/Example/ModelMap.cs:355` (`~/bin/Rock.XML`).
    They'd need `HttpRuntime.BinDirectory` (the guide's step).
  - **P/Invoke into kernel32** besides the loader: `Rock/Jobs/CollectHostingMetrics.cs:202` and
    `Rock/Observability/RockMetricSource.cs:616` (`GlobalMemoryStatusEx`, metrics only).
  - **.NET Framework-only assemblies referenced by `Rock.csproj`:** `System.Data.Entity.Design` (pluralisation, in an
    `Obsolete` extension, `Rock/Utility/ExtensionMethods/Obsolete/StringPluralizationExtensions.cs:18`),
    `System.Data.Services` (`Rock/Data/Model.cs:22`, its `[IgnoreProperties]` attribute), `System.Runtime.Remoting`
    (`Rock/Scripting.Extensions.WebForms.cs:19`, a `ClientSponsor` for CS-Script's remote domain), `PresentationCore`,
    `PresentationFramework` and `WindowsBase` (WPF; no `System.Windows.*` use found), `System.DirectoryServices.
    AccountManagement` (the Active Directory login provider, `Rock/Security/Authentication/ActiveDirectory.cs:20`).
  - **`Thread.Abort`:** none in code (one comment, `Rock/Net/RockResponseContext.WebForms.cs:275`, about
    `Response.Redirect`).
  - None of WCF hosting, LINQ to SQL, EF 1–4, COM+, Registry, metafiles, `NetDataContractSerializer` or protected
    configuration.
- **Notices:** `new SmtpClient( Server, Port )` (`Rock/Communication/Transport/SMTPComponent.cs:287`, configured from
  Rock's own settings, not `<mailSettings>`), `System.Drawing` (21 files: video thumbnails, image handlers), and
  ImageResizer 4.2.8 (11 files, System.Drawing-based) for `GetImage.ashx`.
- **Dependencies** (`RockWeb/packages.config`, `Bin/*.refresh` and the libraries' `PackageReference`s):
  - (a) replaced by webForCore: Web API 5.2.9 (+ Cors, WebHost), Katana 4.2.2 (`Microsoft.Owin`, `.Cors`,
    `.Host.SystemWeb`, `.Security`), SignalR 2.4.3, `Microsoft.AspNet.Web.Optimization` 1.1.3, the ScriptManager
    packages, `Microsoft.Web.Infrastructure`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`;
    `Microsoft.AspNet.WebApi.OData` 5.7 and the Web API `Swashbuckle.Core` 5.6 would need checking against
    webForCore's Web API build (OData isn't in its package list).
  - (b) managed, kept: EntityFramework 6.4.4 (raised to 6.5.1 per the guide), Newtonsoft.Json 13.0.4, Lucene.Net 4.8
    beta, NEST 7, AngleSharp, HtmlAgilityPack, Ical.Net, NodaTime, Fluid, DotLiquid (in the tree), Serilog,
    OpenTelemetry, Twilio, SendGrid, AWSSDK, the Azure SDKs, MassTransit, SixLabors.ImageSharp, QRCoder, PuppeteerSharp,
    Microsoft.Extensions.* 10.0, Semantic Kernel, and Owin.Security.OpenIdConnect.Server 2.0 (Katana-based, built for
    .NET Framework).
  - (c) .NET Framework-only or Windows-only: `Microsoft.SqlServer.Types` 11 (above), ImageResizer 4.2.8 (an IIS
    module/System.Drawing), CS-Script 3.13 (CodeDom, `System.Runtime.Remoting`), `Z.EntityFramework.Extensions` /
    `.Plus.EF6` 5.1 (commercial bulk operations), `Microsoft.Azure.SignalR.AspNet` 1.4 (Azure SignalR for System.Web),
    EPPlus, EXIFextractor and FCM.Net (DLLs in `libs/`), dotless 1.5, `System.Management.Automation` 10.0.10586 (Windows
    PowerShell).
- **Database:** SQL Server only (EF migrations create it at first start; `web.ConnectionStrings.config` names it).

Had the licence allowed it: go on Windows, with the project created as F123 describes; on Linux the spatial types
block start-up until `LoadNativeAssemblies` is skipped there, and geography features stay unavailable.

## Migration

None (rejected).

## Smoke

None (rejected).

## Open problems

- F123: a Web Site Project has no `.csproj`, and the guide's project template compiles its `CodeFile=` code (guide
  gap, [#131](https://github.com/xforcore/webForCore/issues/131); `/FINDINGS.md#f123`).

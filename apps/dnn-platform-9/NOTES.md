# DNN Platform 9: migration notes

Status: blocked (F130). Source: https://github.com/dnnsoftware/Dnn.Platform at `v9.13.10` (`53cdf475`, 29 April 2025), the
last 9.x release. MIT licence. Packages: webForCore 0.8.0, drawingForCore 0.3.0; webForCore-thirdparty 0.6.0 isn't
needed (no WebActivatorEx, no AjaxControlToolkit).

## Assessment

### What it is

DNN Platform 9.13.10 is the CMS (Web Forms at its core, with MVC modules and the Web API "Services Framework"), in
`DNN_Platform.sln`: 137 projects. The site is `DNN Platform/Website` (`DotNetNuke.Website.csproj`, .NET Framework 4.7.2,
an old-style web application project with 181 compiled files). It references 11 projects directly and 18 in all
(`scripts/wfc.py assess`):

| Project | Format | Size |
| --- | --- | --- |
| `Library` (`DotNetNuke.dll`) | old-style, net472 | 1,398 files |
| `DotNetNuke.Web`, `.Web.Client`, `.Web.Mvc`, `.Web.Razor`, `.HttpModules`, `.Instrumentation`, `.Log4Net`, `.Syndication`, `CountryListBox`, `Modules/CoreMessaging`, `Modules/DDRMenu` | old-style, net472 | |
| `DotNetNuke.WebUtility` | old-style **VB** project, net472 | |
| `DotNetNuke.Abstractions`, `.DependencyInjection`, `.Maintenance`, `.ModulePipeline`, `DotNetNuke.Internal.SourceGenerators` | SDK, netstandard2.0 / net472 | the source generator writes the `[DnnDeprecated]` wrappers |

The other ~115 projects are modules, providers, the Persona Bar (`Dnn.AdminExperience`, React front ends built with
yarn), tests and the build. On Windows, DNN's Cake build (`Build/`) compiles the solution with .NET Framework MSBuild,
and each module's and skin's `Module.build` zips it with its `.dnn` manifest into `Website/Install/<type>/`, from which
the install wizard installs it. In the checkout those folders hold only `PlaceHolder.txt`, and `web.config` is a
41-line stub: the build writes the real one from `release.config`.

So "the site" is more than the web project: the install needs
- the skin and containers (`DNN Platform/Skins/Xcillion`, no code; every page of the default site template uses
  `[G]Skins/Xcillion/*.ascx`);
- the providers `release.config` names that aren't in the web project's graph: `DotNetNuke.Providers.AspNetClientCapabilityProvider`
  (the default client capability provider, used by `MobileRedirectModule` on every request) and `DNNConnect.CKEditorProvider`
  (the default HTML editor provider; only edit pages use it);
- the modules the default site template places (`DNN_HTML`, `Console`, `MemberDirectory`, `Journal`, `ResourceManager`,
  `CoreMessaging`): only CoreMessaging is in the web project's graph. A portal template module whose definition isn't
  installed is skipped.

### Frameworks

Web Forms (pages, user controls, skins, the install wizard), MVC 5.3 (`DotNetNuke.Web.Mvc`: MVC modules hosted inside
Web Forms pages, `DesktopModules/MVC`), Web API 5.3 (`DotNetNuke.Web.Api`, routes under `/API/{module}/{controller}/{action}`,
`ServicesRoutingManager`), Web Pages/Razor 3 (`DotNetNuke.Web.Razor`, the Razor host module), and `Microsoft.Extensions.DependencyInjection`
(2.1.1; DNN's startup builds a service provider). `httpRuntime targetFramework="4.7.2"`, `<globalization culture="en-US"
uiCulture="en" fileEncoding="UTF-8">`, forms authentication, ASP.NET's `SqlMembershipProvider` behind DNN's own
membership provider, `requestValidationMode="2.0"`.

### Blockers and Linux problems (file:line)

`scripts/wfc.py assess` flags three groups; with the code read, none stops the migration outright, but several stop
parts of it:

| Hit | What it is | Effect / option |
| --- | --- | --- |
| `Thread.ResetAbort()`: `Library/Entities/Urls/AdvancedUrlRewriter.cs:2615`, `Library/Services/Exceptions/Exceptions.cs:422`, `Library/UI/Modules/ModuleHost.cs:345`, `Library/UI/Modules/ProfileModuleUserControlBase.cs:72`, `Website/admin/Modules/Modulesettings.ascx.cs:522` | DNN catches the `ThreadAbortException` of `Response.Redirect`/`Server.Transfer` and resets the abort, in its URL rewriter (every redirect), its exception handling and module loading | .NET's `Thread.ResetAbort()` throws `PlatformNotSupportedException`, so each catch block throws instead of swallowing. On webForCore the exception is an ordinary instance that isn't re-raised at the end of the `catch`, so there is nothing to reset; NAMESPACES.md says only that code relying on `ResetAbort` "behaves differently" (see Migration) |
| `AppDomain.CreateDomain`: `DotNetNuke.Maintenance/Telerik/TelerikUtils.cs:57,80` | the Telerik removal tool loads `Telerik.Web.UI.dll` in a throw-away AppDomain to read its version | Only on upgrades of sites that still have Telerik; .NET throws `PlatformNotSupportedException`. Not on the install path |
| `System.Data.Linq` in `<compilation><assemblies>`: `release.config:219`, `development.config:218` | a .NET 3.5-era entry | Remove (MIGRATING.md section 5, #37). `Library/Services/Upgrade/Upgrade.cs:1257` only probes for the type by name |
| `WindowsIdentity` in `DotNetNuke.Log4net/log4net/...` (`LoggingEvent.cs:781-875`, `PatternLayout.cs`, `WindowsSecurityContext.cs`) | DNN's own build of log4net 2.0.x | Only for `%username`/`%identity` and `WindowsSecurityContext`, which `Config/DotNetNuke.log4net.config` doesn't use |
| `Microsoft.Win32.Registry`: `Library/Common/Initialize.cs:408`, `Library/Services/Upgrade/Internals/Steps/IISVerificationStep.cs` | reads the .NET Framework release number from HKLM; the IIS check of the install wizard | Both in `try`/`catch`; on Linux the registry isn't there (`PlatformNotSupportedException`, caught) |
| `BinaryFormatter`: `Library/Common/Globals.cs:3166,3214` | `[DnnDeprecated]` Hashtable (de)serialisation helpers, "still required for upgrade purposes" | Not on the install path; throws on .NET 9+ if reached |
| `AppDomain.CurrentDomain.BaseDirectory` as the site root (F3/#3): `Website/Install/Install.aspx.cs:56`, `Website/Install/InstallWizard.aspx.cs:212,235,529,1030`, `Website/Install/UpgradeWizard.aspx.cs:122,224`, `Library/Services/Upgrade/DnnInstallLogger.cs:68`, `Library/Services/Upgrade/Upgrade.cs:518`, `Library/Services/FileSystem/FolderMappings/FolderMappingsConfigController.cs:22`, `Library/Services/Mobile/PreviewProfileController.cs:139`, `Library/Application/ApplicationStatusInfo.cs:276` | the install reads `Install/DotNetNuke.install.config` and writes its status and log files through it | On webForCore it is `bin_dotnet/` (documented, MIGRATING.md section 6): use `HttpRuntime.AppDomainAppPath` / `Globals.ApplicationMapPath` |
| Windows path separators: ~276 string literals with `\` in the projects above (`Library/Services/Installer/Writers/LanguagePackWriter.cs` 68, `Library/UI/Skins/SkinFileProcessor.cs` 18, `Library/Security/PortalSecurity.cs` 15, `Library/Common/Globals.cs` 15, `Library/Services/Upgrade/Upgrade.cs` 11, ...), `providerPath="~\Providers\DataProviders\SqlDataProvider\"` in `release.config`, `.Replace("/", "\\")` | DNN builds physical paths with `\` throughout | Linux only: `\` isn't a separator (app/environment). Fixed only where the probed paths reach them; Windows runs them as they are |
| `QuickIO.NET` 2.6.2 (`SchwabenCode.QuickIO`, net40): `Library/Common/Utilities/FileSystemUtils.cs:19-21` (aliases `Directory`, `DirectoryInfo` and `File` to QuickIO's), `Library/Services/FileSystem/Wrappers/FileWrapper.cs:34` | a Win32 long-path file library (P/Invoke to `kernel32`) | Linux: `DllNotFoundException` wherever DNN deletes or copies files through `FileSystemUtils` (the installer does). Windows works |
| `<probing privatePath="bin;bin\HttpModules;bin\Providers;bin\Modules;bin\Support;" />` (`release.config`) | DNN's packages put provider assemblies in `bin/Providers` (`AspNetClientCapabilityProvider`) | .NET has no private probing paths. The migration builds every assembly into `bin_dotnet/` |
| `UriKind.Absolute`: `Library/Entities/Portals/PortalAliasController.cs:120` (after `Globals.AddHTTP`, so always `http://`), `DotNetNuke.Web.Client/ClientResourceManager.cs:412,544` (`IsAbsoluteUrl`) | #92 | `ClientResourceManager.IsAbsoluteUrl("/Resources/...")` is true on Linux: client resources registered by path may be treated as external URLs (checked at smoke) |

No Windows authentication (`<authentication mode="Forms">`; the Windows block is commented out), no WCF hosting
(no `.svc`), no LINQ to SQL or EF 1-4 (DNN uses PetaPoco and its own DAL), no `App_WebReferences`, no metafiles.

### Dependencies

(a) Replaced by webForCore: `Microsoft.AspNet.Mvc` 5.3.0, `.Razor` 3.3.0, `.WebPages` 3.3.0, `.WebApi`/`.WebApi.Core`/`.WebApi.WebHost` 5.3.0,
`.WebApi.Client` 6.0.0 (`System.Net.Http.Formatting`), `Microsoft.Web.Infrastructure` 1.0.0, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`.
Not replaced: `Microsoft.AspNet.WebPages.Data`/`.WebData` 3.3.0 (`WebMatrix.Data`, `WebMatrix.WebData`: webForCore#130)
and `Microsoft.AspNet.WebHelpers` 3.3.0 (`Microsoft.Web.Helpers`), referenced by `DotNetNuke.Web.Mvc` and
`DotNetNuke.Web.Razor` and imported by every Razor page (`<system.web.webPages.razor>` namespaces).

(b) Managed libraries kept (packages with .NET Standard or .NET builds): Newtonsoft.Json 13.0.3 (→ 13.0.4, F7),
Newtonsoft.Json.Bson 1.0.2, AngleSharp 1.4.0, AngleSharp.Css, HtmlSanitizer 9.1.878-beta, SharpZipLib 1.3.3, MailKit
2.15.0 / MimeKit 2.15.1, Portable.BouncyCastle 1.9.0, Microsoft.Extensions.DependencyInjection 2.1.1 (→ the framework's,
#62), Microsoft.Extensions.FileSystemGlobbing 5.0.0, PetaPoco.Compiled 6.0.524, System.Collections.Immutable and
System.Text.Encoding.CodePages 9.0.2 (part of .NET).

(c) .NET Framework-only binaries to test: checked into the repository, `Controls/DotNetNuke.WebControls/bin/DotNetNuke.WebControls.dll`,
`Components/Lucene.Net/bin/Lucene.Net.dll` 3.0.3 and `Lucene.Net.Contrib.*`, `Components/DataAccessBlock/bin/Microsoft.ApplicationBlocks.Data.dll`
(SqlHelper), `Modules/DDRMenu/_dependencies/Ealo/05.05.00/effority.ealo.dll`; packages with only .NET Framework builds,
`Dnn.ClientDependency` 1.9.10 (net45, System.Web), `WebFormsMvp` 1.4.5 (net40, System.Web), `QuickIO.NET` 2.6.2 (net40,
Win32 P/Invoke: above).

### Go/no-go

Go, as a stress test, with a narrowed scope: the web project and the 18 projects it references, on .NET 10, plus what
the install and the probes need beyond them (the Xcillion skin; the client capability provider). The modules and the
Persona Bar (its React front ends are built with yarn) are out of scope. There are no IIS reference responses; the
probes check that pages render.

## Migration

In order, against the webForCore 0.8.0 packages (`overlay.patch`). `scripts/convert.py` did the mechanical part for the
web project and 13 of the libraries; the rest is by hand.

1. **`scripts/convert.py`** on `Website/DotNetNuke.Website.csproj` with `--lib` for `Library`, `DotNetNuke.Web`,
   `.Web.Client`, `.Web.Mvc`, `.Web.Razor`, `HttpModules`, `DotNetNuke.Instrumentation`, `DotNetNuke.Log4net`,
   `Syndication`, `Controls/CountryListBox`, `Modules/CoreMessaging`, `Modules/DDRMenu` and
   `Providers/ClientCapabilityProviders/Provider.AspNetCCP`: SDK-style net10.0 projects (originals as `.csproj.netfx`,
   `packages.config` as `.netfx`), `Program.cs` (no session state), Microsoft's ASP.NET packages dropped,
   Newtonsoft.Json 13.0.3 → 13.0.4. Then in each converted project:
   - the build-only packages removed (`Microsoft.SourceLink.*`, `Microsoft.Build.Tasks.Git`,
     `Microsoft.NETFramework.ReferenceAssemblies*`, `StyleCop.Analyzers*`), and `Microsoft.Extensions.DependencyInjection(.Abstractions)`
     2.1.1 (the ASP.NET Core shared framework's, #62);
   - `DotNetNuke.Internal.SourceGenerators` referenced as an analyzer (`ReferenceOutputAssembly="false" OutputItemType="Analyzer"`),
     as before (it writes the `[DnnDeprecated]` wrappers): `convert.py` makes it an ordinary reference;
   - `<LangVersion>13.0</LangVersion>` (they had `latest`; MIGRATING.md section 2);
   - `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in every library (#62);
   - project references in place of the old projects' `HintPath`s to each other's `bin/` (`..\Library\bin\DotNetNuke.dll`
     and so on), which `convert.py` drops: Web → Library, HttpModules, Instrumentation, Web.Client, WebUtility;
     Web.Client, HttpModules, Syndication → Instrumentation; Instrumentation → Log4Net; Web.Mvc, Web.Razor → Library;
     CoreMessaging → Instrumentation, Web.Client; DDRMenu → Web.Razor;
   - Newtonsoft.Json.Bson 1.0.2 → 1.0.3 (`NU1605`: webForCore.Net.Http.Formatting needs 1.0.3; "every package webForCore
     depends on is a floor", MIGRATING.md section 3; `convert.py`'s list doesn't have it).
2. **`DotNetNuke.WebUtility.vbproj`** (VB) by hand: `Microsoft.NET.Sdk` net10.0, `<MyType>Empty</MyType>` (was `Windows`;
   the code doesn't use `My`; F71/#121), its `<Import>`s kept, its embedded scripts and `ClientAPICaps.config` listed
   (VB names them by file name, as before), the Web.Client reference as a project reference.
3. **The SDK-style libraries** `DotNetNuke.DependencyInjection`, `.Maintenance` (both netstandard2.0) and `.ModulePipeline`
   (netstandard2.0;net472) → net10.0 with `webForCore` and the framework reference: they reference projects that are
   net10.0 now. ModulePipeline gets `NET472` defined: its `#if NET472` code is the Web Forms module pipeline (without it
   `IModuleControlPipeline` isn't implemented or registered). Their `XCOPY` post-build steps (into `Website\bin`) removed,
   also in `DotNetNuke.Abstractions` (which stays netstandard2.0). Their `TreatWarningsAsErrors` stays, with NU1901-NU1904
   not as errors: the .NET 10 SDK audits transitive packages (`NU1902` for MailKit 2.15, AngleSharp 1.4, HtmlSanitizer, ...),
   which SDK 9 didn't; and SYSLIB0024 in Maintenance (`AppDomain.CreateDomain` in the Telerik removal tool, obsolete on
   .NET: assessment).
4. **`global.json`**: SDK 9.0.202 with `rollForward: latestMinor` → 10.0.100, `latestFeature` (no .NET 9 SDK is needed).
5. **DotNetNuke.Log4net** (DNN's copy of log4net 2.0.14): log4net's own .NET Standard 2.0 defines (`NETSTANDARD;NETSTANDARD2_0`)
   instead of the .NET Framework ones (`NET;NET_2_0;NET_4_0;NET_4_5`, which reach .NET Remoting and
   `AppDomainSetup.ConfigurationFile`), and `log4net/Util/WindowsSecurityContext.cs` out of the build
   (`WindowsImpersonationContext`; only for a `SecurityContext` in the configuration, which DNN's doesn't have). DNN's
   copy had broken log4net's `#if NETSTANDARD` block in `Config/XmlConfigurator.cs` (an `#endif` after the shared
   lines, so CS1513): moved back where log4net has it.
6. **Library**: `Services/Localization/LocalizationExpressionBuilderEditor.cs` out of the build: Visual Studio's
   design-time editor for `dnnLoc:` expressions derives from `System.Web.UI.Design.ExpressionEditor` (System.Design,
   not on .NET; F132/#144). `[ExpressionEditor]` names it by string.
7. **Library**: PetaPoco.Compiled 6.0.524's `net45` build (`ExcludeAssets="compile;runtime"` and a `HintPath`, as
   MIGRATING.md section 3 says for Hangfire): its .NET Standard build has no `Database(string connectionStringName)`
   constructor (CS1503 in `Data/PetaPoco/PetaPocoDataContext.cs:50`).
8. **Library**: 27 calls `settings.GetValueOrDefault("key", "default")` on a `Dictionary<string, string>` in
   `Entities/Portals/PortalSettingsController.cs` and `Security/RegistrationSettings.cs` are `CS0121`, ambiguous between
   DNN's `DotNetNuke.Collections.CollectionExtensions.GetValueOrDefault<T>(IDictionary, string, T)` and .NET's
   `CollectionExtensions.GetValueOrDefault<TKey, TValue>(IReadOnlyDictionary<TKey, TValue>, TKey, TValue)` (.NET Core
   2.0+), where the default is a string. Called as DNN's, statically (MIGRATING.md section 2, "Calls that become
   ambiguous", #93; `GetValueOrDefault` is another such method).
9. **DotNetNuke.Web**: `webForCore.Web.ApplicationServices` with `ExcludeAssets="compile"`: the project didn't reference
   System.Web.ApplicationServices, and DNN's own `RoleProvider` and `MembershipProvider` are CS0104 otherwise
   (`Common/DotNetNukeHttpApplication.cs:160`; MIGRATING.md section 3, #29).
10. **`AppDomain.CurrentDomain.BaseDirectory` → `System.Web.HttpRuntime.AppDomainAppPath`** in the 11 places the
    assessment lists (the install pages, the install logger, `Upgrade.cs`, the folder mappings and preview profiles
    configuration, and `ApplicationStatusInfo.GetCurrentDomainDirectory`, which is `Globals.ApplicationMapPath`): it is
    `bin_dotnet/` on webForCore, the site root on IIS (MIGRATING.md section 6). Without it the install can't find
    `Install/DotNetNuke.install.config`.
11. **`Thread.ResetAbort()`** removed in its five places (a comment in its place): it throws
    `PlatformNotSupportedException` on .NET, and webForCore's `ThreadAbortException` isn't re-raised after the `catch`
    (F131/#143). Not reached yet (F130 stops every request first).
12. **The client capability provider** (`Provider.AspNetCCP`, converted in step 1) referenced from the web project, so it
    is built into `bin_dotnet/`: DNN's packaging puts it in `bin/Providers`, found through `<probing privatePath>`, which
    .NET doesn't have. web.config names it as the default `<clientcapability>` provider.
13. **The Xcillion skin package**: a `PackageXcillionSkin` target in the web project, after the build, zips
    `DNN Platform/Skins/Xcillion` as DNN's `SkinPackage.targets` does (the manifest, licence and release notes, with
    `Resource-Skin.zip` and `Resource-Container.zip`) into `Install/Skin/Skin_Xcillion_09.13.10_Install.zip`, with
    MSBuild's `ZipDirectory` instead of MSBuild.Community.Tasks. The install installs it; the default site template's pages
    use it. (`generated` in `app.toml` keeps it out of the patch.)
14. **`web.config`** = `release.config`, as DNN's build makes it (`Build/Tasks/CopyWebConfig.cs`), with two changes: the
    connection string (the compose SQL Server, database `dnn9`, `TrustServerCertificate=True`; it was SQL Express with
    `User Instance`/`AttachDBFilename`, Windows only) and the `System.Data.Linq` entry removed from
    `<compilation><assemblies>` (#37). The rest (forms authentication, `machineKey` with 3DES/SHA1,
    `<globalization culture="en-US" uiCulture="en" fileEncoding="UTF-8">`, the modules and handlers) is as it was.

Not changed: QuickIO.NET and DNN's Windows paths (Linux only, F133), the Telerik removal tool's AppDomain (upgrades only).
The modules (HTML, Console, Journal, MemberDirectory, ResourceManager), the HTML editor provider (DNNConnect.CKE), the
Persona Bar and the other providers aren't built: the install only gets the skin.

The build: web project and 19 libraries, `dotnet build -c Release`, 0 errors (about 2 minutes from clean with `-m:2`).

## Smoke

`[setup]` creates the `dnn9` database and runs DNN's unattended install, `Install/Install.aspx?mode=install` (the
schema and data scripts, the host and the default site from `DotNetNuke.install.config`). The probes: the home page in
the Xcillion skin, the log-in page, a Web API route of the Services Framework (`/API/InternalServices/CountryRegion/Countries`,
anonymous) and a static file. There's no MVC probe: no module of the default install is an MVC module (DNN's MVC
modules come as separate packages), so it would need a module built and installed for it.

**Linux (webForCore 0.8.0): build pass, setup fail, smoke 7/11.** Every request is a 500 from
`HttpApplication.InitModules`, the static file included: `FileNotFoundException: Could not load file or assembly
'ClientDependency.Core.Mvc'` from `ClientDependencyModule.LoadFilterTypes`, which asks
`BuildManager.GetType(type, throwOnError: false, ...)` for its optional MVC filter. On .NET Framework that returns
`null`; webForCore's throws (F130, webForCore bug, #137). The 7 that pass are the forbidden paths (404).

Past F130 (looked at locally only, with ClientDependency's filters and the two providers whose assemblies aren't
deployed taken out of web.config, none of which is in the overlay):
- DNN's own provider registration hits F130 too: `ProviderInstaller.InstallProvider` registers every provider in
  web.config with `BuildManager.GetType(provider.Type, false, true)` and skips the ones whose type is null. The default
  web.config names `DNNConnect.CKEditorProvider` (an optional package) and `DotNetNuke.Authentication.ActiveDirectory`'s
  ADSI provider (never part of the platform), so on webForCore DNN fails at start with the platform as it is released.
- Then `/` redirects to `/Install/InstallWizard.aspx`, and `Install.aspx?mode=install` fails on Linux: DNN builds every
  physical path with `\`. `Globals.ApplicationMapPath` itself is `BaseDirectory.Replace("/", "\\")`
  (`ApplicationStatusInfo.GetCurrentDomainDirectory`), and `HostMapPath`, `InstallMapPath`, the provider paths and the
  skin paths add `\`-separated parts to it. The install wrote a `web.config` into a folder named
  `\home\...\Website\Config\` inside the site, and its error page failed in `Config.Load` (`SecurityException: Unable to
  load config for "web.config"`: the file's full path doesn't start with `ApplicationMapPath`). That is DNN's
  (app/environment, F133): about 280 such literals, and QuickIO.NET's Win32 calls behind `FileSystemUtils` besides.
  Making DNN's paths portable is a rewrite of its file layer, not a migration step, so Linux stops there.

**Windows (CI):** see `results.windows.json`; F130 is platform-independent, so it stops at the same place. F133 doesn't
apply there.

## Open problems

- [F130](../../FINDINGS.md#f130): `BuildManager.GetType(name, throwOnError: false)` throws `FileNotFoundException` for an
  assembly that isn't there (.NET Framework returns `null`); ClientDependency's module and DNN's provider registration
  rely on it, so every request fails. webForCore bug, [#137](https://github.com/xforcore/webForCore/issues/137). Blocks
  the app on both platforms.
- [F131](../../FINDINGS.md#f131): `Thread.ResetAbort()` throws `PlatformNotSupportedException`; the guide lists it but
  doesn't say to remove it. Guide gap, [#143](https://github.com/xforcore/webForCore/issues/143).
- [F132](../../FINDINGS.md#f132): an application's design-time classes (`System.Web.UI.Design.ExpressionEditor`) don't
  compile. Guide gap, [#144](https://github.com/xforcore/webForCore/issues/144).
- [F133](../../FINDINGS.md#f133): DNN's Windows paths (and QuickIO.NET) on Linux. App/environment.
- The .NET 10 SDK's transitive NuGet audit (`NU1902` as an error under `TreatWarningsAsErrors`) and the
  Newtonsoft.Json.Bson 1.0.3 floor: migration steps, noted above; not findings.

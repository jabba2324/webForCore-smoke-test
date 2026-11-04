# SmartStoreNET: migration notes

Status: blocked at build by [F87](../../FINDINGS.md#f87) (webForCore's System.Web.Optimization has another assembly
identity than Microsoft's, so BundleTransformer, built against Microsoft's, doesn't compile against it). Source:
https://github.com/smartstore/SmartStoreNET at `b6520754` (the head of the default branch, `4.x`: 4.2.x, the last
release line on .NET Framework before Smartstore moved to ASP.NET Core). GPL-3.0. The web project
(`src/Presentation/SmartStore.Web`) targets .NET Framework 4.7.2 with System.Web.

Migrated on webForCore 0.8.0 (.NET 10) and drawingForCore 0.3.0; no webForCore-thirdparty packages (it uses neither
WebActivatorEx nor AjaxControlToolkit). SmartStoreNET started as a fork of nopCommerce, so its shape is
nopCommerce's (`apps/nopcommerce-3.90/`): `SmartStore.Web` with `SmartStore.Admin` in `Administration/` built into the
site's `bin`, `SmartStore.Web.Framework`, `SmartStore.Core`, `.Data`, `.Services`, and plugins built into
`SmartStore.Web/Plugins/`, loaded by a plugin manager in a `PreApplicationStartMethod`.

## Assessment

- **Frameworks:** MVC 5 (store and administration), Web API 2 with OData (`SmartStore.WebApi` plugin, Microsoft.AspNet.OData
  5.10), EF 6.4 code first with migrations.
- **Target:** .NET 4.7.2.
- **Blockers flagged by `assess`**, none a real one:
  - `windows-auth`: the same ACL check as nopCommerce (`FilePermissionHelper.cs:34`, `InstallController.cs`,
    `Administration/CommonController.cs`); fixed as there (below);
  - `ef-objectcontext` in `SmartStore.Data/Extensions/MiscExtensions.cs:9`: a string comparison with EF 4's provider
    type name, no use of it;
  - `com-plus` in `Web.config:96`: a `<remove assembly="System.EnterpriseServices">` in `<compilation><assemblies>`.
- **Notices:** BinaryFormatter (`SerializationUtils`, `ConversionExtensions`, `CommonHelper`), `CallContext`
  (`ContextState`, compiled against webForCore, so it gets webForCore's), the registry (`MimeTypes`, inside a `try`).
- **Dependencies:**
  - (a) replaced by webForCore: MVC 5.2.7, Web API 5.2.7 (+ Cors), Razor and Web Pages 3.2.7, Web.Optimization 1.1.3,
    WebGrease, Microsoft.Web.Infrastructure;
  - (b) managed, kept: Autofac 5.2 (+ Mvc5, WebApi2), EntityFramework 6.4.4 (6.5.1), FluentValidation 7.4, log4net
    2.0.8 (its `net45` build, below), Newtonsoft.Json 13.0.1 (13.0.4), DotLiquid, AngleSharp, HtmlSanitizer, EPPlus,
    ImageProcessor 2.8, MaxMind GeoIP2, NuGet.Core 2.14, PreMailer.Net, UAParser, System.Linq.Dynamic.Core, ncrontab;
  - (c) .NET Framework-only or native, to test: **BundleTransformer** 1.10–1.12 (Core, SassAndScss, Autoprefixer,
    NUglify, JsMin; all bundles and the themes' SCSS go through it), **LibSassHost** with only the `win-x64`/`win-x86`
    native packages, **JavaScriptEngineSwitcher** V8 (`win-x64`/`win-x86` natives) and Msie (Windows only;
    Autoprefixer runs on it), NReco.PdfGenerator (wkhtmltopdf for Windows), Microsoft.SqlServer.Scripting 11 (SMO),
    Microsoft.AspNet.OData 5.10, and two checked-in binaries: `lib/Telerik/Telerik.Web.Mvc.dll` (Telerik Extensions
    for ASP.NET MVC, the old GPL release) and `lib/SmartStore.Licensing/SmartStore.Licensing.dll`.
- **Go**, expecting work on the bundling (LibSassHost and the JavaScript engines need their Linux natives on Linux).

## Migration

1. `scripts/convert.py` on `SmartStore.Web` (web) and `SmartStore.Core`, `.Data`, `.Services`, `SmartStore.Web.Framework`,
   `SmartStore.Admin` (libraries): SDK-style net10.0 projects with the webForCore 0.8.0 packages, EntityFramework
   6.4.4 → 6.5.1, Newtonsoft.Json 13.0.1 → 13.0.4, `Program.cs` (without ASP.NET Core's session), `<system.codedom>`
   removed from `web.config`; the checked-in Telerik and licensing DLLs kept as `HintPath` references.
2. As for nopCommerce: `Administration/**` and `Plugins/**` out of `SmartStore.Web`'s compilation, and a project
   reference to `SmartStore.Admin` (built into the site's `bin` before).
3. `<LangVersion>13.0</LangVersion>` in every project: they set `latest`, which meant C# 7.3–8 then; 13 is the last
   before C# 14's span conversions (`docs/MIGRATING.md` section 2; F83).
4. `System.ComponentModel.Annotations` 4.7.0 → 5.0.0 in `SmartStore.Web` (NU1605 with EntityFramework 6.5.1;
   guide, #62).
5. `SmartStore.Core`:
   - `using System.Runtime.Serialization.Formatters.Binary` → `WebFormsForCore.Serialization.Formatters.Binary` in
     the three files that use BinaryFormatter (SYSLIB0011; `docs/MIGRATING.md` section 6);
   - `ReferenceEqualityComparer` (the app's own, `SmartStore.ComponentModel`) is ambiguous with .NET 5's
     `System.Collections.Generic.ReferenceEqualityComparer` (CS0104): an alias in `CommonHelper.cs` and in
     `SmartStore.Services`' `MessageModelProvider.cs` (the case of webForCore#136);
   - `ExpressionStarter.CompileToMethod` (from LINQKit): `LambdaExpression.CompileToMethod` is .NET Framework only;
     the two members are compiled only for .NET Framework (`#if NETFRAMEWORK`), as LINQKit does;
   - log4net 2.0.8: its .NET Standard build has no `AdoNetAppender` and no `LogManager.GetRepository()` without an
     assembly (CS0246, CS1501, CS7036); the `net45-full` build, in `SmartStore.Core` and `SmartStore.Web` (guide, "A
     package's .NET Standard build can have less");
   - `PluginManager`: `AppDomain.CurrentDomain.DynamicDirectory` → `HttpRuntime.CodegenDir`, as in nopCommerce
     ([F84](../../FINDINGS.md#f84)); `SetNativeDllPath` calls kernel32's `SetDllDirectory` (the app's own P/Invoke,
     for the Windows natives in `bin/x64`) and reads `AppDomainSetup.PrivateBinPath`, which .NET doesn't have
     (CS1061): it now runs only on Windows, with `HttpRuntime.BinDirectory`.
6. `SmartStore.Web.Framework`: `FilePermissionHelper`, as in nopCommerce (`new DirectoryInfo(path).GetAccessControl()`,
   the ACL read before `WindowsIdentity.GetCurrent()`).

Then the build stops in `SmartStore.Web.Framework` (8 errors, 7 distinct), all from BundleTransformer:

```
Theming/Assets/SmartStyleBundle.cs(23,67): error CS0012: The type 'IBundleTransform' is defined in an assembly that is not referenced. You must add a reference to assembly 'System.Web.Optimization, Version=1.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'.
UI/BundleBuilder.cs(59,29): error CS0039: Cannot convert type 'BundleTransformer.Core.Bundles.CustomScriptBundle' to 'System.Web.Optimization.Bundle' ...
```

webForCore's `System.Web.Optimization` (package `webForCore.Web.Optimization`) is
`System.Web.Optimization, Version=11.0.0.0, PublicKeyToken=b03f5f7f11d50a3a`, and its `WebGrease` is
`Version=11.0.0.0, PublicKeyToken=b03f5f7f11d50a3a`; Microsoft's are `1.1.0.0` and `1.6.5135.21930`, both
`PublicKeyToken=31bf3856ad364e35`. webForCore's other ASP.NET assemblies keep Microsoft's names (`System.Web.Mvc,
Version=5.3.1.0, PublicKeyToken=31bf3856ad364e35`, `System.Web.WebPages 3.0.0.0`, `Microsoft.Owin 4.2.3.0`), which is
why .NET Framework packages built on them work. Not worked around (no app change can make the compiler unify them).
[F87](../../FINDINGS.md#f87)

## What's left after F87

From the assessment, not reached yet:
- the 12 plugins (`src/Plugins`), to be rewritten as nopCommerce's were;
- the themes' SCSS: LibSassHost needs `LibSassHost.Native.linux-x64` on Linux, and Autoprefixer a JavaScript engine
  that runs there (V8's `linux-x64` natives; Msie is Windows only): the app's packages are Windows-only natives
  (app/environment);
- NReco.PdfGenerator (PDF invoices; wkhtmltopdf for Windows) and Microsoft.SqlServer.Scripting (database backups in
  the administration), Windows-only features;
- `[setup]`: SmartStore's install wizard, as nopCommerce's.

## Smoke

- **webForCore 0.8.0 (Linux):** build fails (F87, 7 distinct errors), so smoke is skipped.

## Open problems

- [F87](../../FINDINGS.md#f87) (webForCore bug): webForCore's System.Web.Optimization and WebGrease don't have
  Microsoft's assembly identities. Blocks the build.
- [F84](../../FINDINGS.md#f84) (guide gap): `AppDomain.DynamicDirectory` is null; fixed in the overlay.

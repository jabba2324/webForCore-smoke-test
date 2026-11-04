# C1 CMS (Composite C1): migration notes

Status: blocked at build (Windows Workflow Foundation 3, F116; LINQ to SQL and WCF REST hosting, F120). Source:
https://github.com/Orckestra/C1-CMS-Foundation at `98b7e87d` (tag v6.13-hotfix.862, "C1 CMS 6.13 Hotfix 862", the latest
release, September 2026). Licence: MPL 1.1 with Orckestra's amendment of section 11 (`license.txt`; GitHub shows
NOASSERTION because of the amendment). The web project `Website/WebSite.csproj` targets `v4.8` and references
System.Web.

Round 7, on webForCore 0.8.0 (.NET 10), webForCore-thirdparty 0.6.0 (not used: C1 has neither WebActivatorEx nor
AjaxControlToolkit) and drawingForCore 0.3.0.

## Assessment

- **Frameworks:**
  - Web Forms: the admin console's host pages (`/Composite/*.aspx`, `Login.aspx`, `top.aspx`), its controls and
    dialogs, `default.aspx` and the page renderer (`Renderers/Page.aspx`);
  - Web Pages: Razor functions and page templates (in the starter sites), through `System.Web.WebPages.Razor` 3.2;
  - ASMX services (the console's `services/*.asmx`, the setup wizard's `SetupService.asmx`), generic handlers;
  - a WCF REST service (`Composite/services/LogService/LogService.svc`);
  - the console itself is a JavaScript application over those services, and its actions are WF3 workflows.
- **Target:** .NET 4.8 (`Composite/web.config`'s `<httpRuntime>` has `requestValidationMode="2.0"`; the site's own
  `web.config` is made from `DebugBuild.Web.config`/`ReleaseBuild.Web.config` at build).
- **Projects:** `Website/WebSite.csproj` (assembly `Composite.Website`), `Composite/Composite.csproj` (`Composite.dll`,
  2280 files) and `Composite.Workflows/Composite.Workflows.csproj` (282 files).
- **Blockers:**
  - **Windows Workflow Foundation 3**, not flagged by `assess` (its patterns follow the guide's table, which doesn't
    have it: F116). `System.Workflow.Runtime`, `.ComponentModel` and `.Activities` in 48 files of `Composite.dll`
    (`C1Console/Workflow/*`: `WorkflowFacade`, `FlowControllerFacade`, the file persistence service, the state
    machine base classes) and every file of `Composite.Workflows.dll`. `GlobalInitializerFacade.cs:264` starts the
    workflow runtime at application start (`WorkflowFacade.EnsureInitialization()`), so the public site needs it too,
    not just the console.
  - **LINQ to SQL** (`assess`: linq-to-sql), 10 files of `Composite.dll`: the SQL Server data provider
    (`Plugins/Data/DataProviders/MSSqlServerDataProvider/*`), but also `Data/Foundation/DataFacadeQueryable*.cs` (the
    query pipeline every data store goes through) and `Core/Types/CodeGenerationCommon.cs:88` (every generated data
    type references System.Data.Linq). The XML store needs it too (F120).
  - **WCF hosting** (`assess`: wcf-hosting): `Composite/services/LogService/LogService.svc` and its `<services>` in
    `Composite/web.config:71`, with `System.ServiceModel.Web`'s `[WebInvoke]` (F120): the console's log viewer.
  - `assess`'s other hits aren't on a path the site needs: `<identity impersonate="false">` (`Composite/web.config:27`;
    it turns impersonation off), `WindowsIdentity` in `Core/IO/PathUtil.cs:145` (an ACL check of write permissions,
    Windows only), and `Thread.Abort` handling in `Core/Parallelization/ParallelFacade.cs:237-308` and one workflow.
- **Notices:** `BinaryFormatter` (the console's action locks, serialized values, validation rules, WF3 persistence),
  `new SmtpClient()` in `Core/SmtpMailer.cs`.
- **Needed at run time (not reached):** run-time C# compilation through System.CodeDom: `CodeGenerationManager`
  compiles every data type's classes at application start (F117); `App_Data/Composite/*.Composite.config` has
  `binDirectory="~/Bin"` (F61's case, `~/bin_dotnet` here); and the setup wizard downloads its starter sites from
  `https://package.composite.net` (`Composite.SetupConfiguration`), so `[setup]` would need the network.
- **Dependencies:**
  - (a) replaced by webForCore: Razor and Web Pages 3.2.3, Microsoft.Web.Infrastructure; the pages' compiler
    (Orckestra.AspNet.Roslyn 1.0.2 with Roslyn 2.0, named in `<system.codedom>`);
  - (b) managed, kept: Castle.Core 4.2.1, Newtonsoft.Json (6.0.5, raised to 13.0.4), System.Reactive 3.0,
    TPL Dataflow 4.7, WampSharp 18.3 (the console's WebSocket messaging), Microsoft.Extensions.DependencyInjection 1.1,
    and the checked-in `Bin/` DLLs: Enterprise Library 3.0 (Common, Logging, Validation, ObjectBuilder), TidyNet,
    `Composite.XmlSerializers`;
  - (c) .NET Framework-only: WF3 (`System.Workflow.*`), LINQ to SQL, `System.ServiceModel.Web`/`.Activation`
    (WCF hosting), `System.Configuration.Install`, `System.Management` (a package on .NET, Windows only),
    `Microsoft.SqlServer.ConnectionInfo` (SMO, in `Composite.Workflows`), PhantomJS (the UI tests' tool).

No-go: WF3 is at the core of C1's start-up and of every console action, and there's no WF3 on .NET; getting past it is
a rewrite of C1's console, not a migration. The project conversion and the compile fixes up to the blockers were done
anyway, so the build shows exactly what's left.

## Migration

1. `scripts/convert.py Website/WebSite.csproj --lib Composite/Composite.csproj --lib Composite.Workflows/Composite.Workflows.csproj`:
   SDK projects (`.csproj.netfx` copies), the webForCore packages (Web Pages: `webForCore.Web.WebPages.Razor`,
   `.Deployment`, `.Helpers`), `Program.cs` (no session state; C1 doesn't use `Session`), the `Bin/` DLLs by hint path,
   Newtonsoft.Json 13.0.4. Files on disk the old projects didn't compile are left out (3 in the site, 2 in
   `Composite`). `Properties/GitCommitInfo.cs`, which the old projects' `BeforeBuild` wrote (an informational version
   with the git commit), isn't generated: it only carried the version text.
2. `WebSite.csproj`: Microsoft.CodeAnalysis 2.0.0 (Common, CSharp, Analyzers) and Orckestra.AspNet.Roslyn removed:
   they were the pages' CodeDom compiler (`<system.codedom>`), which webForCore replaces; Roslyn 2.0 against
   webForCore.Compilers' 5.9 was `NU1605`.
3. `Composite.csproj` and `Composite.Workflows.csproj`:
   - `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (`docs/MIGRATING.md` section 2, class libraries):
     without it, `NU1605` for Microsoft.Extensions.DependencyInjection.Abstractions 1.1.0 against CoreWCF's 8.0.2;
   - `<LangVersion>7</LangVersion>` and `NoWarn` 618, as the old projects had;
   - `<Deterministic>false</Deterministic>`: `Properties/SharedAssemblyInfo.cs` has `AssemblyVersion("6.13.*")` (CS8357);
   - `<EnableCoreWCFOperationInvokerGenerator>false</EnableCoreWCFOperationInvokerGenerator>`: CoreWCF's source
     generator, which comes with webForCore, wrote C# 11 code for the log service's `[ServiceContract]`: CS8107 with
     C# 7 (F118, webForCore bug). It's CoreWCF's own documented switch;
   - `Composite.csproj`: the `System.Management` package (`ValidateAllWindowsLoginProvider` uses WMI; Windows only).
4. Compile fixes in `Composite.dll`:
   - `using KeyValuePair = Composite.Core.Types.KeyValuePair;` in the 12 files that import both
     `System.Collections.Generic` and `Composite.Core.Types`: .NET's non-generic `KeyValuePair` made C1's ambiguous
     (CS0104, F121);
   - webForCore's `BinaryFormatter` (`WebFormsForCore.Serialization.Formatters.Binary`, as the guide says) in
     `ActionLockingFacade.cs` (its field typed `BinaryFormatter` instead of the obsolete `IFormatter`, SYSLIB0011),
     `SystemSerializableValueXmlSerializer.cs` and `ClientValidationRuleSerializerHandler.cs`. The WF3 persistence
     service's is left, as that file can't build anyway;
   - `PerformanceCounterInstaller.cs` under `#if NETFRAMEWORK`: an `installutil` installer
     (`System.Configuration.Install` isn't on .NET), not used by the site.
5. Not done, as nothing after the build can be checked: the site's `web.config` (made from `DebugBuild.Web.config` by
   the old project's `BeforeBuild`), its `<system.codedom>`, a `<machineKey>`, `<globalization>`, `binDirectory`, and
   a `[setup]` for the setup wizard.

## Smoke

- **Round 7 (webForCore 0.8.0, Linux):** `build` fails with 46 distinct errors (464 lines), all in `Composite.dll`
  and all WF3 (F116), LINQ to SQL or WCF REST hosting (F120); `smoke` is skipped (nothing built). `Composite.Workflows`
  and the site aren't reached: they depend on `Composite.dll`.
- **Windows (CI run 41914624804, PR #21):** the same: `build` fails with the same 46 distinct errors, `smoke` skipped.
- The probes in `app.toml` (`/robots.txt`, the console's `Login.aspx`) are for when it builds. The aim was the setup
  wizard with a starter site on the XML data store, a rendered public page, the console's log-in, an XSLT function's
  output and a static file; none can be reached.

Checked outside the app (minimal sites on the 0.8.0 packages), for what C1 would hit next:
- `CodeDomProvider.CreateProvider("c#").CompileAssemblyFrom*` throws `PlatformNotSupportedException` (F117), which is
  how C1 compiles its data types at start-up;
- webForCore's own `WebFormsForCore.CodeDom.Compiler.CSharpCodeProvider` isn't a System.CodeDom provider, and with
  `GenerateInMemory` it fails with `ArgumentNullException` (F119).

## Open problems

- F116, guide gap ([#132](https://github.com/xforcore/webForCore/issues/132)): WF3 isn't on .NET and isn't in the
  docs. The blocker: a rewrite of C1's console workflows (there's no WF3 port).
- F120, documented gap: LINQ to SQL in the data layer's core, and the WCF REST log service.
- F117, guide gap ([#133](https://github.com/xforcore/webForCore/issues/133)): run-time C# compilation in app code.
- F118, webForCore bug ([#134](https://github.com/xforcore/webForCore/issues/134)): CoreWCF's source generator in
  every app; avoided with CoreWCF's switch.
- F119, webForCore bug ([#135](https://github.com/xforcore/webForCore/issues/135)): webForCore's C# provider in memory.
- F121, guide gap ([#136](https://github.com/xforcore/webForCore/issues/136)): CS0104 for `KeyValuePair`; fixed with
  aliases.

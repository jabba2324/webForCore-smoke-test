# Findings

One entry per distinct problem found while migrating apps. See CLAUDE.md for the format. Every entry was found
with webForCore built from source at `4ec57209` and confirmed on the 0.1.0 release packages (tag `v0.1.0`,
`2942b694`), unless the entry says otherwise. Repro sites are small enough to paste: a `Program.cs` as in
`docs/MIGRATING.md` section 4, an SDK project referencing the `webForCore` package, and the page shown.

| # | Problem | Classification | Apps |
| --- | --- | --- | --- |
| [F1](#f1) | .NET Framework libraries get a second `ConfigurationManager` that can't read web.config | webForCore bug | eshop-legacy-mvc, eshop-legacy-webforms |
| [F2](#f2) | `CacheDependency` on one file under two spellings throws on Linux (bundling) | webForCore bug | eshop-legacy-mvc |
| [F3](#f3) | `AppDomain.CurrentDomain.BaseDirectory` is `bin_dotnet/`, not the site root | webForCore bug | eshop-legacy-mvc |
| [F4](#f4) | W3C logs are written inside the site and served over HTTP | webForCore bug (security) | every app using `AddWebForms()` |
| [F5](#f5) | Third-party packages bring Microsoft's ASP.NET packages back transitively (CS0433) | guide gap | eshop-legacy-mvc |
| [F6](#f6) | A `lib/net45`-only package with a `build/` folder restores with no assembly | guide gap | eshop-legacy-mvc, eshop-legacy-webforms |
| [F7](#f7) | Packages that must be upgraded: EF 6.2 → 6.4+, Newtonsoft.Json 12 → 13 | guide gap | eshop-legacy-mvc, eshop-legacy-webforms |
| [F8](#f8) | `HttpUtility` in app code is CS0433 outside webForCore's repository | webForCore bug | eshop-legacy-webforms |
| [F9](#f9) | Data controls with `ItemType` need System.Web.DynamicData, which isn't built | documented gap (consequence undocumented) | eshop-legacy-webforms |

---

## F1

**.NET Framework libraries get a second `ConfigurationManager` that can't read web.config.** webForCore bug.

- **Apps:** eshop-legacy-mvc, eshop-legacy-webforms (`Microsoft.AspNet.SessionState.SessionStateModule` 1.1.0, which web.config's
  `<system.webServer><modules>` puts in place of the session module). Likely any `net4x` library that calls
  `ConfigurationManager`: it is compiled against `System.Configuration, Version=4.0.0.0`.
- **Error**, on every request, from `SessionStateModuleAsync.Init`:
  ```
  ConfigurationErrorsException: An error occurred creating the configuration section handler for system.web/sessionState:
  Type 'System.Web.Configuration.SessionStateSection' does not inherit from 'System.Configuration.IConfigurationSectionHandler'.
  (.../bin_dotnet/App_Data/machine.config line 106)
     System.Configuration.ClientConfigurationSystem...GetSection(String sectionName)
     Microsoft.AspNet.SessionState.SessionStateModuleAsync.Init(HttpApplication app)
  ```
- **Cause:** a reference to `System.Configuration, Version=4.0.0.0` resolves to the shared framework's
  `System.Configuration.dll` facade in the *default* load context, which forwards to
  `System.Configuration.ConfigurationManager` and loads a second copy of webForCore's ConfigurationManager there. That
  copy isn't the one System.Web configured inside the PseudoAppDomain: it uses the client configuration system
  (`bin_dotnet/App_Data/machine.config`, no web.config), and its `ConfigurationSection` isn't the type System.Web's
  sections derive from, so even `AppSettings` throws. On .NET Framework, `ConfigurationManager` in an ASP.NET app is
  web.config-aware whichever assembly calls it.
- **Repro** (`Default.aspx`; web.config has `<appSettings><add key="probe" value="from web.config" />`):
  ```aspx
  <%@ Page Language="C#" %>
  <script runat="server">
      protected void Page_Load(object sender, EventArgs e)
      {
          // How a .NET Framework library binds ConfigurationManager.
          var cm = Type.GetType("System.Configuration.ConfigurationManager, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", true);
          Response.Write(System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(cm.Assembly) + "<br>");
          Response.Write(cm == typeof(System.Configuration.ConfigurationManager));   // False
          var settings = cm.GetProperty("AppSettings").GetValue(null);                 // throws ConfigurationErrorsException
      }
  </script>
  ```
  The page's own `typeof(ConfigurationManager)` is in `PseudoAppDomain ...` and reads `probe`; the one reached through
  `System.Configuration` 4.0.0.0 is in `"Default"` and throws as above.
- **Fix belongs in webForCore:** resolve `System.Configuration` (4.0.0.0) inside the PseudoAppDomain's load context to
  the domain's own `System.Configuration.ConfigurationManager`, as is presumably done for System.Web.

## F2

**`CacheDependency` on one file under two spellings throws on Linux.** webForCore bug.

- **Apps:** eshop-legacy-mvc: `@Styles.Render`/`@Scripts.Render` in `_Layout.cshtml` failed when two first requests
  rendered the bundles at once (`System.Web.Optimization.HttpContextCache.Put` → `MapPathBasedVirtualPathProvider.GetCacheDependency`).
  Sequential requests rendered, so it showed up as an intermittent 500 on the home page.
- **Error:**
  ```
  ArgumentException: Item has already been added. Key in dictionary: '<site>/Scripts/modernizr-2.6.2.js'  Key being added: '<site>/Scripts/modernizr-2.6.2.js'
     System.Collections.Hashtable.Insert(Object key, Object nvalue, Boolean add)
     System.Web.DirectoryMonitor.AddFileMonitor(String file) in src/WebFormsForCore.Web/FileChangesMonitor.cs:917
     System.Web.DirectoryMonitor.StartMonitoringFileWithAssert(String file, ...) in FileChangesMonitor.cs:1010
     System.Web.FileChangesMonitor.StartMonitoringPath(String alias, ...) in FileChangesMonitor.cs:2181
     System.Web.Caching.CacheDependency.Init(...)
  ```
- **Cause:** `FindFileData.FindFile` (`src/WebFormsForCore.Web/Util/FileUtil.cs`, the non-Windows branch) sets
  `FileNameLong` to the full path; on Windows `FindFirstFile` returns the file name only. `DirectoryMonitor` keys
  `_fileMons` by `FileNameLong` but `FindFileMonitor` looks up by file name, so a second request for a file already
  monitored misses and `Hashtable.Add` throws. The alias cache in `StartMonitoringPath` hides this when the same path
  string is used twice; a different spelling of the path (another alias) reaches it. The same mismatch probably also
  means `DirectoryMonitor` can't find the monitor for a changed file by name, so per-file change notifications in
  non-application directories may be lost (not verified).
- **Repro** (`Scripts/a.js` exists):
  ```aspx
  <%@ Page Language="C#" %>
  <script runat="server">
      protected void Page_Load(object sender, EventArgs e)
      {
          string file = Server.MapPath("~/Scripts/a.js");
          var first = new System.Web.Caching.CacheDependency(file);
          var second = new System.Web.Caching.CacheDependency(file.Replace("/Scripts/", "/Scripts/./"));  // throws on Linux
      }
  </script>
  ```

## F3

**`AppDomain.CurrentDomain.BaseDirectory` is `bin_dotnet/`, not the site root.** webForCore bug (not in
`docs/NAMESPACES.md` or `docs/IIS-COMPATIBILITY.md`).

- **Apps:** eshop-legacy-mvc: `CatalogDBInitializer.ExecuteScript` reads
  `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models/Infrastructure/dbo.catalog_hilo.Sequence.sql")`.
- **Error:** `DirectoryNotFoundException: Could not find a part of the path '<site>/bin_dotnet/Models/Infrastructure/dbo.catalog_hilo.Sequence.sql'`,
  thrown from EF's `Seed`, after `CreateDatabaseIfNotExists` has created the tables. The database is then left
  created and empty, and every later start sees it as existing and never seeds it: the app runs with an empty catalog
  and no error.
- **Cause:** on IIS, an application's AppDomain has `ApplicationBase` = the site's physical path. Under webForCore,
  `AppDomain.CurrentDomain` is the process's, whose base directory is the folder of the entry assembly.
- **Repro:** `<%= AppDomain.CurrentDomain.BaseDirectory %>` vs `<%= HttpRuntime.AppDomainAppPath %>`: equal on IIS,
  `<site>/bin_dotnet/` vs `<site>/` here.
- **Fix belongs in webForCore**, or at least a line in `docs/NAMESPACES.md`: `BaseDirectory` (and
  `SetupInformation.ApplicationBase`) are common in .NET Framework apps for finding files next to the site (log4net
  configs, SQL scripts, data files).

## F4

**W3C logs are written inside the site and served over HTTP.** webForCore bug, with security impact.

- **Apps:** every app whose `Program.cs` calls `builder.Services.AddWebForms()` with the defaults, as
  `docs/MIGRATING.md` section 4 says. Found on eshop-legacy-mvc and confirmed on a minimal site.
- **What happens:** the default log folder (`docs/IIS-COMPATIBILITY.md`: `../logs/W3SVC1`) ends up as
  `<site>/logs/W3SVC1/`, and `GET /logs/W3SVC1/u_ex20261031.0000.txt` returns 200 with the log: client IPs, every URL
  with its query string, user agents and referrers. IIS writes logs to `%SystemDrive%\inetpub\logs\LogFiles`, outside
  any site, and `logs` isn't a hidden segment.
- **Repro:** the minimal site from F1, request any page, then `curl http://127.0.0.1:5000/logs/W3SVC1/u_ex$(date -u +%Y%m%d).0000.txt`.
- **Cause:** `WebFormsServicesOptions.LogDirectory` defaults to `Path.Combine(AppContext.BaseDirectory, "..", "logs", "W3SVC1")`
  (`src/WebFormsForCore.Web/WebFormsForCore/AspNetCoreHosting/IIS/IISServices.cs:33`), and `AppContext.BaseDirectory`
  is `bin_dotnet/`, a direct subfolder of the site.
- **Fix belongs in webForCore:** default to a folder outside the site (the systemd unit's `LogsDirectory`, or
  `/var/log/webforcore/<site>`), or refuse to serve the log folder. The runner now probes the first log file as a
  forbidden path, so every app's smoke shows it.

## F5

**Third-party packages bring Microsoft's ASP.NET packages back transitively.** Guide gap.

- **Apps:** eshop-legacy-mvc: `Autofac.Mvc5` 4.0.2 depends on `Microsoft.AspNet.Mvc` 5.1.0 (and through it Razor 3.1,
  WebPages 3.1, Microsoft.Web.Infrastructure 1.0); `Autofac.WebApi2` 6.0.1 on `Microsoft.AspNet.WebApi.Core` 5.2.0 and
  `.Client` 5.2.0.
- **Error:** 13 distinct `CS0433: The type 'Controller' exists in both 'System.Web.Mvc, Version=5.1.0.0' and
  'System.Web.Mvc, Version=5.3.1.0'` (and the same for `System.Web.Http` 5.2.0.0).
- **What the guide says:** `docs/MIGRATING.md` section 3 says to remove Microsoft's ASP.NET packages, but not what to
  do when another package depends on them. Packages built on MVC/Web API (Autofac, Unity, Ninject, Glimpse, Elmah.Mvc,
  Swashbuckle, ...) all do this.
- **What worked:** reference each transitive package directly with no assets, so NuGet's direct reference wins and
  contributes nothing:
  ```xml
  <PackageReference Include="Microsoft.AspNet.Mvc" Version="5.1.0" ExcludeAssets="all" />
  <PackageReference Include="Microsoft.AspNet.WebApi.Core" Version="5.2.0" ExcludeAssets="all" />
  ```
  `obj/project.assets.json` lists which arrived. webForCore's targets could do this for the whole Microsoft ASP.NET
  set (drop their `ReferencePath`s and runtime assets, as `WebForCoreFixReferences` already does for
  `System.Configuration.ConfigurationManager`), which would make the step unnecessary.

## F6

**A `lib/net45`-only package with a `build/` folder restores with no assembly.** Guide gap.

- **Apps:** eshop-legacy-mvc: `Microsoft.ApplicationInsights.Web` 2.9.1 (`lib/net45/Microsoft.AI.Web.dll`,
  `build/Microsoft.ApplicationInsights.Web.targets`), named by web.config's `ApplicationInsightsWebTracking` module.
- **Error:** at run time, for every request: `Configuration Error: Could not load file or assembly 'Microsoft.AI.Web'`
  (web.config line 36, `<httpModules>`).
- **Cause:** NuGet treats the framework-agnostic `build/` folder as a compatible asset, so `AssetTargetFallback`
  (which only applies when nothing is compatible) never kicks in and `lib/net45` isn't used. A `lib/net45`-only
  package without `build/` (`Autofac.Mvc5`) is picked up fine through the fallback.
- **What the guide says:** `docs/MIGRATING.md` "Before you start" says .NET Framework-only libraries often load and to
  test each one, but not that some don't restore at all.
- **What worked:** reference the DLL from the package folder:
  ```xml
  <PackageReference Include="Microsoft.ApplicationInsights.Web" Version="2.9.1" GeneratePathProperty="true" />
  <Reference Include="Microsoft.AI.Web" HintPath="$(PkgMicrosoft_ApplicationInsights_Web)/lib/net45/Microsoft.AI.Web.dll" />
  ```
  The module then loads and runs on webForCore.

## F7

**Packages that must be upgraded.** Guide gap (small).

- **Apps:** eshop-legacy-mvc.
- `EntityFramework` 6.2.0 has only `net40`/`net45` builds; on .NET it needs 6.4 or later (the guide's "EntityFramework
  6.4+" example implies this but doesn't say to upgrade). 6.5.1 worked, with `System.Data.SqlClient` against SQL Server
  2022.
- `Newtonsoft.Json` below 13.0.4 is a restore error: `NU1605: Detected package downgrade: Newtonsoft.Json from 13.0.4
  to 12.0.1` (`eShopLegacyMVC -> webForCore.Web.Optimization -> Newtonsoft.Json (>= 13.0.4)`; System.Net.Http.Formatting
  references 13.0.4 too). The guide could list the minimum versions webForCore's assemblies
  impose.

## F8

**`HttpUtility` in app code is CS0433 outside webForCore's repository.** webForCore bug.

- **Apps:** eshop-legacy-webforms (`ViewSwitcher.ascx.cs:39`, `HttpUtility.UrlEncode`), the Visual Studio Web Forms
  template's own code. Any compiled code (code-behind, controllers, libraries) that names `HttpUtility`.
- **Error:** `CS0433: The type 'HttpUtility' exists in both 'System.Web.HttpUtility, Version=11.0.0.0, Culture=neutral,
  PublicKeyToken=cc7b13ffcd2ddd51' and 'System.Web, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'`.
- **Cause:** the step that drops the shared framework's `System.Web.HttpUtility.dll` from the references
  (`WebForCoreDropFrameworkHttpUtility`) is in webForCore's own `Directory.Build.targets`, which only its own projects
  import. `src/webForCore.WebStack.targets`, which apps import (and which the `webForCore.Web` package ships as
  `buildTransitive/webForCore.Web.targets`), drops only the `System.Web.dll` facade. webForCore's samples and tests
  never see the problem because they build inside the repository.
- **Repro:** the minimal project from `docs/MIGRATING.md` section 2 with one file:
  ```csharp
  public static class Encode
  {
      public static string Html(string s) => System.Web.HttpUtility.HtmlEncode(s);   // CS0433
  }
  ```
- **Fix belongs in webForCore:** move the target into `webForCore.WebStack.targets` (its condition on a project
  reference to System.Web also needs to accept the package's reference). Run-time compiled pages aren't affected.

## F9

**Data controls with `ItemType` need System.Web.DynamicData, which isn't built.** Documented gap, but the
consequence isn't.

- **Apps:** eshop-legacy-webforms: `Default.aspx`'s `<asp:ListView ... ItemType="eShopLegacyWebForms.Models.CatalogItem">`.
- **Error:** `FileNotFoundException: Could not load file or assembly 'System.Web.DynamicData, Version=4.0.0.0,
  Culture=neutral, PublicKeyToken=31bf3856ad364e35'`, from `DataBoundControlHelper.EnableDynamicData`
  (`src/WebFormsForCore.Web/UI/WebControls/DataBoundControlHelper.cs:133`) in `Control.InitRecursive`.
- **Cause:** as in .NET Framework 4.5+, `GridView`, `ListView`, `Repeater`, `FormView`, `DetailsView`,
  `CheckBoxList` and `RadioButtonList` call `DataControlExtensions.EnableDynamicData` in `OnInit` whenever `ItemType`
  is set, so every strongly-typed data control (the 4.5 model binding pattern) loads System.Web.DynamicData.
  `docs/NAMESPACES.md` lists System.Web.DynamicData as "Not built by the solution" (`WebFormsForCore.slnx` has
  `<Build Project="false" />` for it), which reads as "Dynamic Data sites don't work", not "typed data controls
  don't work".
- **Repro:** `<asp:Repeater runat="server" ItemType="System.String"><ItemTemplate><%# Item %></ItemTemplate></asp:Repeater>`
  in any page.
- **Fix belongs in webForCore:** build System.Web.DynamicData (at least `DataControlExtensions`), or document that
  `ItemType` needs it.

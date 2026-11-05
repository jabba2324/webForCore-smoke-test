# Findings

One entry per distinct problem found while migrating apps. webForCore bugs and guide gaps are raised on
[xforcore/webForCore](https://github.com/xforcore/webForCore/issues), security problems included (webForCore is
pre-release and not recommended for production). See CLAUDE.md for the format. Every entry was found
with webForCore built from source at `4ec57209` and confirmed on the 0.1.0 release packages (tag `v0.1.0`,
`2942b694`), unless the entry says otherwise. F16 onwards were found in round 2, on the released 0.2.0 packages
(tag `v0.2.0`, `1c92215`), where F1–F15 are fixed or documented. F16–F21 and F24 are fixed in [xforcore/webForCore#24](https://github.com/xforcore/webForCore/pull/24) (released in 0.3.0), checked against the apps. Round 3 ran on 0.4.0, with webForCore-thirdparty 0.4.0 for WebActivatorEx (CLAUDE.md); F25 onwards were found there. F28–F33 are fixed or documented in [xforcore/webForCore#32](https://github.com/xforcore/webForCore/pull/32), checked against the apps. Round 3b (subtext, funnelweb, mojoportal) also ran on 0.4.0, with webForCore-thirdparty 0.4.0 and the unreleased `webForCore.WebActivator` 0.4.1 ([xforcore/webForCore-thirdparty#1](https://github.com/xforcore/webForCore-thirdparty/pull/1)); F35 onwards were found there. F35 and F38 are fixed in #32 too, and F36, F37, F41 and F42 documented there, checked against the apps. Round 4 ran on the released 0.5.0 packages (#32 merged), with webForCore-thirdparty 0.4.1: all 24 migrated apps build, the fixes hold in every app that needed them (MVCForum and Subtext with no workarounds, no explicit `webForCore.Web.Helpers`), and there are no new findings. F40 (#33) is 0.5.0's known issue. Round 4's new candidates (jabbr, opserver-mvc5, yafnet-2) found F45 onwards; F46 is fixed and F45, F48 documented in [xforcore/webForCore#44](https://github.com/xforcore/webForCore/pull/44). Round 5 ran on the released 0.6.0 packages (#44 merged), with webForCore-thirdparty 0.4.1, on Linux and, for the first time, on Windows (README.md, "Windows"): every app on both. F46 is fixed (JabbR builds), and the results are the same on both platforms except where F51 is reached; F51 onwards were found there. Round 6 ran on the released 0.7.0 packages, on Linux and Windows:
- **The release:** .NET 10 LTS; every app runs as a worker process under `webforcore-host`; System.Drawing comes from drawingForCore 0.2.0.
- **Third-party packages:** webForCore-thirdparty 0.5.0, the first built for net10.0 (F55 explains why 0.4.1 couldn't be used).
- **Results:** they match round 5 on both platforms, apart from one new probe: `webforcore-host.dll` isn't served.
- **What 0.7.0 fixed:** F51 and F52, so bonobo-git-server passes on Windows too.
- **What's new:** F54 is the one new problem webForCore can act on; F54 onwards were found in this round.
- **The feeds:** miniblog also restores from the NuGet feeds alone (webForCore#53 step 5).
- **Umbraco 6** (a new app, migrated on 0.7.0): F57–F64, and F67 from its first CI run (Ubuntu's `C` locale).
- **Umbraco 7** (a new app, on 0.7.0, on Linux so far): builds, but is blocked at boot by AutoMapper 3.3.1 (F22); F65
  and F66, and more evidence for F37 and F61–F64.
- **Web Forms, Web Pages and MVC 3 candidates** (new apps, migrated on 0.7.0 with webForCore-thirdparty 0.6.0):
  screwturn-wiki (24/24 on Linux and Windows), mvc-music-store (20/20 on both; its accounts are blocked by F75) and
  rabbit-webpages (20/21 on both, F79); F73–F79 were found there.
- **OWIN, SignalR and Web API candidates** (new apps, on 0.7.0): signalr-samples (18/19 on Linux and Windows, F70),
  odata-samples-compositekey (8/15, F68) and identityserver3-example (7/10, F69); F68–F72 were found there.
- **AspNetDocs tutorial candidates** (new apps, on 0.7.0): aspnetdocs-contoso-university (16/16 on Linux and Windows)
  and aspnetdocs-wingtip-toys (16/19 on Linux, 17/19 on Windows, F80–F82); aspnetdocs-bookservice is rejected (no
  licence).

Round 7 ran on the released 0.8.0 packages, with webForCore-thirdparty 0.6.0 and drawingForCore 0.3.0, on Linux and
Windows (CI run 41905418748; eshop-legacy-mvc's Windows job didn't start its server on the first attempt and passed
16/16 on the rerun):
- **Results:** every app matches round 6 or the candidates' 0.7.0 results on both platforms, except two that 0.8.0
  fixed.
- **What 0.8.0 fixed:** F40 (mojoportal: 7/16 → 16/16) and F47 (opserver-mvc5: 10/11 → 11/11), on both platforms. Both
  apps are now migrated. webForCore also closed the issues for F57–F63 and F65–F67 (#87–#95, #97) in 0.8.0, fixed or
  documented.
- **Still blocked:** umbraco-7 (7/14, F22: its overlay doesn't use webForCore-thirdparty 0.6.0's AutoMapper build yet),
  and the apps blocked before.
- **Orchard CMS 1.10.4** (a new app, on 0.8.0): it builds (the framework, Core and all 69 modules, on NHibernate 5 and
  Castle.Core 4) and its setup creates its database, but on Linux it runs out of inotify instances, one per folder it
  watches (blocked, 9/15); F97–F103 were found there.
- **C1 CMS** (composite-c1, a new app on 0.8.0): blocked at build, on Linux and Windows, by Windows Workflow
  Foundation 3 in its core library (F116) and by LINQ to SQL and WCF REST hosting (F120); F116–F121 were found there.
- **Rock RMS** (a candidate, assessed on 0.8.0): rejected, as the Rock Community License limits use, derivative works and
  distribution to faith-based organisations' internal use, so no overlay can be published here. Its assessment (a Web
  Site Project, Windows-only native spatial types, a named `EventWaitHandle`) is in its NOTES.md; F123 was found there.
- **DNN Platform 9** (dnn-platform-9, a new app on 0.8.0, 9.13.10): the web project and its 19 libraries build on Linux
  and Windows, but every request fails at start (7/11 on both, the forbidden paths only), because `BuildManager.GetType(name,
  false)` throws for a missing assembly (F130); on Linux, DNN's Windows paths stop its install after that (F133).
  F130–F133 were found there.
- **Umbraco 7 unblocked** (on 0.8.0): with webForCore-thirdparty 0.6.0's `webForCore.AutoMapperLegacy` for AutoMapper
  3.3.1, F22 no longer stops it: it boots, installs through `[setup]` and passes 21/21 on Linux and Windows (7/14 before). One
  mapping needed a change for F104 (webForCore-thirdparty); F104 and F105 were found there.
- **Stores** (nopcommerce-3.90 and smartstorenet, new apps on 0.8.0): nopCommerce 3.90, with its administration and
  20 plugins, is migrated: 22/23 on Linux (product thumbnails, F86) and 23/23 on Windows, in 2 of 5 Windows runs (7/23
  in the others, F89, intermittent). SmartStoreNET 4.x is blocked at build, on both platforms, by webForCore's
  System.Web.Optimization identity (F87). F83–F89 were found there; for F89 the runner now stops a server's whole
  process tree on Windows and records a failed probe's stack.
- **Kooboo CMS and Umbraco 8** (new apps, round 8, on 0.8.0 with drawingForCore 0.3.0): kooboo-cms 23/23 and
  umbraco-8 21/21, on Linux and Windows (CI run 41920542509). kooboo-cms needs about half of Linux's default inotify
  instances (F92), so its runs on the shared development machine were partial. F90–F94 were found there.

**What "migrated" means (4 Nov 2026):** an app counts as migrated only when it builds, passes every probe on Linux and on
Windows every time, and has no feature known to be broken by webForCore, probed or not. Anything less is blocked, with
the reason. On that rule nopcommerce-3.90 (F86, F89), signalr-samples (F70) and mvc-music-store (log-on and registration,
F75) moved from migrated to blocked. Features the probes can't reach are listed as untested in the app's NOTES.md.

Repro sites are small enough to paste: a `Program.cs` as in
`docs/MIGRATING.md` section 4, an SDK project referencing the `webForCore` package, and the page shown.

| # | Problem | Classification | Apps | Raised |
| --- | --- | --- | --- | --- |
| [F1](#f1) | .NET Framework libraries get a second `ConfigurationManager` that can't read web.config | webForCore bug | eshop-legacy-mvc, eshop-legacy-webforms, bonobo-git-server | [#1](https://github.com/xforcore/webForCore/issues/1) |
| [F2](#f2) | `CacheDependency` on one file under two spellings throws on Linux (bundling) | webForCore bug | eshop-legacy-mvc | [#2](https://github.com/xforcore/webForCore/issues/2) |
| [F3](#f3) | `AppDomain.CurrentDomain.BaseDirectory` is `bin_dotnet/`, not the site root | webForCore bug | eshop-legacy-mvc | [#3](https://github.com/xforcore/webForCore/issues/3) |
| [F4](#f4) | W3C logs are written inside the site and served over HTTP | webForCore bug (security) | every app using `AddWebForms()` (seen on 10) | [#15](https://github.com/xforcore/webForCore/issues/15) |
| [F5](#f5) | Third-party packages bring Microsoft's ASP.NET packages back transitively (CS0433) | guide gap | eshop-legacy-mvc, bonobo-git-server | [#4](https://github.com/xforcore/webForCore/issues/4) |
| [F6](#f6) | A `lib/net45`-only package with a `build/` folder restores with no assembly | guide gap | eshop-legacy-mvc, eshop-legacy-webforms | [#5](https://github.com/xforcore/webForCore/issues/5) |
| [F7](#f7) | Packages that must be upgraded: EF 6.2 → 6.4+, Newtonsoft.Json 12 → 13 | guide gap | eshop-legacy-mvc, eshop-legacy-webforms, bonobo-git-server | [#6](https://github.com/xforcore/webForCore/issues/6) |
| [F8](#f8) | `HttpUtility` in app code is CS0433 outside webForCore's repository | webForCore bug | eshop-legacy-webforms, bonobo-git-server, aspnet-samples-webapi-bson, aspnet-samples-webapi-webforms | [#7](https://github.com/xforcore/webForCore/issues/7) |
| [F9](#f9) | Data controls with `ItemType` need System.Web.DynamicData, which isn't built | documented gap (consequence undocumented) | eshop-legacy-webforms | [#8](https://github.com/xforcore/webForCore/issues/8) |
| [F10](#f10) | .NET Framework assemblies that are NuGet packages on .NET (System.ServiceModel.Syndication) | guide gap | miniblog | [#9](https://github.com/xforcore/webForCore/issues/9) |
| [F11](#f11) | One URL Rewrite feature ASP.NET Core lacks (Wildcard syntax, `{CACHE_URL}`) drops every rule, failing open | webForCore bug | miniblog | [#10](https://github.com/xforcore/webForCore/issues/10) |
| [F12](#f12) | A rewrite to a query-only URL (`?slug={R:1}`) is a bare 500 (`IndexOutOfRangeException`) | webForCore bug | miniblog | [#11](https://github.com/xforcore/webForCore/issues/11) |
| [F13](#f13) | `<handlers>` entries whose `path` starts with `/` never match | webForCore bug | miniblog | [#12](https://github.com/xforcore/webForCore/issues/12) |
| [F14](#f14) | The trailing-slash 301 for a directory pre-empts routing with `RouteExistingFiles = true` | webForCore bug | clean-architecture-mvc5 | [#13](https://github.com/xforcore/webForCore/issues/13) |
| [F15](#f15) | The VS template's social login packages (Facebook, Google, MicrosoftAccount, Twitter) aren't in webForCore | guide gap | aspnetdocs-mvcmovie | [#14](https://github.com/xforcore/webForCore/issues/14) |
| [F16](#f16) | Replaced packages are one version; Identity 2.0 apps collide with 2.2's `SignInStatus` | guide gap | open-order-framework | [#17](https://github.com/xforcore/webForCore/issues/17) |
| [F17](#f17) | Katana's social providers need `System.Net.Http.WebRequest` and fail at start-up when used | guide gap | open-order-framework, socialgoal | [#18](https://github.com/xforcore/webForCore/issues/18) |
| [F18](#f18) | Configuration types referenced through `System.dll` reach the host's ConfigurationManager (ELMAH) | webForCore bug | socialgoal | [#19](https://github.com/xforcore/webForCore/issues/19) |
| [F19](#f19) | `System.Web.Abstractions`/`System.Web.Routing` 4.0.0.0 in web.config can't be loaded | webForCore bug | roadkill | [#20](https://github.com/xforcore/webForCore/issues/20) |
| [F20](#f20) | `Assembly.LoadFrom` returns the default context's copy; WebActivatorEx start methods never run | webForCore bug | applicant-tracking, roadkill | [#21](https://github.com/xforcore/webForCore/issues/21) |
| [F21](#f21) | Glimpse doesn't run on .NET | guide gap | realocate | [#22](https://github.com/xforcore/webForCore/issues/22) |
| [F22](#f22) | AutoMapper before 12 doesn't run on .NET 11 or .NET 10 (umbraco-7: fixed by webForCore-thirdparty 0.6.0's AutoMapperLegacy) | app/environment | realocate, socialgoal, umbraco-7, nopcommerce-3.90 | - |
| [F23](#f23) | `recaptcha` 1.0.5's lib folders aren't framework names (NU1202) | app/environment | roadkill | - |
| [F24](#f24) | A transitive Microsoft.AspNet.Razor replaces webForCore's in the output with no build error | webForCore bug | bonobo-git-server, applicant-tracking, realocate, roadkill | [#23](https://github.com/xforcore/webForCore/issues/23) |
| [F25](#f25) | .NET Framework builds of StructureMap 4.0 and NLog 4.2 need `AppDomainSetup` members .NET lacks; their .NET Standard builds work | app/environment | roadkill | - |
| [F26](#f26) | OpenOrderFramework's Identity seed deadlocks on a fresh database (EF 6 initialization lock + `AsyncHelper.RunSync`) | app/environment | open-order-framework | - |
| [F27](#f27) | LightSpeed (closed-source ORM) needs `AppDomain.DefineDynamicAssembly` | app/environment | roadkill | - |
| [F28](#f28) | A `configSource` in a subfolder can't be loaded on Linux (`\` isn't a separator; `/` is rejected) | webForCore bug | mvcforum | [#26](https://github.com/xforcore/webForCore/issues/26) |
| [F29](#f29) | `relaxedUrlToFileSystemMapping="true"` makes every `MapPath` return `NOT_A_VALID_FILESYSTEM_PATH` | webForCore bug | mvcforum | [#27](https://github.com/xforcore/webForCore/issues/27) |
| [F30](#f30) | `webForCore.Web.Mvc` doesn't bring System.Web.Helpers, which Microsoft.AspNet.Mvc did | webForCore bug | mvcforum | [#28](https://github.com/xforcore/webForCore/issues/28) |
| [F31](#f31) | System.Web.ApplicationServices is visible to every project: a library's own `MembershipUser` is ambiguous | guide gap | mvcforum | [#29](https://github.com/xforcore/webForCore/issues/29) |
| [F32](#f32) | More versions webForCore requires: Humanizer.Core 2.14.1, System.* packages at 11.0 | guide gap | aspnet-mvc5-starter, mvcforum | [#30](https://github.com/xforcore/webForCore/issues/30) |
| [F33](#f33) | Some packages' .NET Standard builds have less than their net45 builds (WindowsAzure.Storage, Hangfire 1.6) | guide gap | aspnet-mvc5-starter, mvcforum | [#31](https://github.com/xforcore/webForCore/issues/31) |
| [F34](#f34) | Build-time tools that don't run on Linux or the .NET 11 SDK (BuildWebCompiler, LibMan.Build 2.0) | app/environment | mvcforum, aspnet-mvc5-starter | - |
| [F35](#f35) | `WsiProfiles`/`SoapBindingUse` in app code are CS0433: the System.Web.Services.Description package (from CoreWCF) has them too | webForCore bug | subtext, mojoportal | [#34](https://github.com/xforcore/webForCore/issues/34) |
| [F36](#f36) | A `.resx` with non-string resources needs System.Resources.Extensions (MSB3822) | guide gap | subtext | [#36](https://github.com/xforcore/webForCore/issues/36) |
| [F37](#f37) | `<compilation><assemblies>` lists .NET Framework assemblies .NET doesn't have (System.Design, System.Data.Linq; System.Data.Linq is provided from 0.9.0) | guide gap | subtext, mojoportal, umbraco-7 | [#37](https://github.com/xforcore/webForCore/issues/37) |
| [F38](#f38) | Types a shared-framework facade forwards to the app's assemblies (System.Data's SqlClient) are a second copy: MissingMethodException | webForCore bug | subtext | [#35](https://github.com/xforcore/webForCore/issues/35) |
| [F39](#f39) | NHibernate 3.3 doesn't run on .NET (proxy validation, `SqlCommandSet` by reflection) | app/environment | funnelweb | - |
| [F40](#f40) | `Assembly.Load("App_GlobalResources")` (Visual Studio's resource designer code) can't work: .NET requires the simple name to match (fixed in 0.8.0) | webForCore bug | mojoportal | [#33](https://github.com/xforcore/webForCore/issues/33) |
| [F41](#f41) | WebActivator 1.x has WebActivatorEx's problem (#21), and the guide names only WebActivatorEx | guide gap | subtext | [#38](https://github.com/xforcore/webForCore/issues/38) |
| [F42](#f42) | SDK-style project references are transitive: NU1605 from versions pinned per project, a namespace that hides a type (CS0118) | guide gap | funnelweb, mojoportal | [#39](https://github.com/xforcore/webForCore/issues/39) |
| [F43](#f43) | Projects that deploy into the site with Windows `xcopy` post-build events | app/environment | mojoportal | - |
| [F44](#f44) | DotNetOpenAuth 3.x reflects on .NET Framework's private `UriParser` fields | app/environment | subtext | - |
| [F45](#f45) | Every package webForCore depends on is a version floor: System.Data.SqlClient 4.9.1, System.DirectoryServices 11.0 (NU1605) | guide gap | opserver-mvc5, yafnet-2 | [#42](https://github.com/xforcore/webForCore/issues/42) |
| [F46](#f46) | webForCore's SignalR and System.Web.DataVisualization assemblies are 0.0.0.0: libraries built against Microsoft's are CS1705 | webForCore bug | jabbr | [#40](https://github.com/xforcore/webForCore/issues/40) |
| [F47](#f47) | `GetTypes()` on System.Data.SqlClient 4.9.1 throws on .NET 11 and .NET 10: apps that scan all assemblies fail (fixed in 0.8.0) | webForCore bug (dependency) | opserver-mvc5 | [#41](https://github.com/xforcore/webForCore/issues/41) |
| [F48](#f48) | The C# version: C# 14's span conversions and C# 11's ref safety break code written for older C# | guide gap | yafnet-2 | [#43](https://github.com/xforcore/webForCore/issues/43) |
| [F49](#f49) | YAF.NET's vendored ServiceStack 5.9 fork only works as .NET Framework code | app/environment | yafnet-2 | - |
| [F50](#f50) | Nancy 0.20 needs `AppDomainSetup.PrivateBinPathProbe` | app/environment | jabbr | - |
| [F51](#f51) | A change while the next start is starting leaves the application dead: 503 for every request | webForCore bug | bonobo-git-server, mojoportal (Windows); any app | [#49](https://github.com/xforcore/webForCore/issues/49) |
| [F52](#f52) | AppDomain data `DataDirectory` isn't set: `\|DataDirectory\|` resolves to `bin_dotnet/` | webForCore bug | bonobo-git-server | [#50](https://github.com/xforcore/webForCore/issues/50) |
| [F53](#f53) | Linux: a change in `bin` or `App_Code` doesn't restart the application | webForCore bug | any app (seen on aspnetdocs-mvcmovie, miniblog) | [#51](https://github.com/xforcore/webForCore/issues/51) |
| [F54](#f54) | .NET 10 prunes fewer framework packages than .NET 11: NU1605 downgrades (System.ComponentModel.Annotations; Microsoft.Extensions.\* in class libraries) | guide gap | mvcforum, yafnet-2 | [#62](https://github.com/xforcore/webForCore/issues/62) |
| [F55](#f55) | webForCore-thirdparty 0.4.x (net11.0 only) restores on .NET 10 with no error and no assembly: the original WebActivatorEx runs | app/environment (fixed in webForCore-thirdparty 0.5.0) | applicant-tracking (bonobo-git-server, mvcforum, roadkill, subtext would have) | - |
| [F56](#f56) | An app's repository is gone from GitHub (devbridge/BetterCMS), and a clone of one waited for credentials | app/environment | better-cms (and prodinner, already rejected for it) | - |
| [F57](#f57) | `<trust level="Medium">` (any partial trust level) is a Configuration Error, though the docs say trust levels are ignored | webForCore bug | umbraco-6 | [#87](https://github.com/xforcore/webForCore/issues/87) |
| [F58](#f58) | `<system.codedom>` naming `Microsoft.CSharp.CSharpCodeProvider`: `InvalidCastException` on every page | webForCore bug | umbraco-6 | [#88](https://github.com/xforcore/webForCore/issues/88) |
| [F59](#f59) | Recycling for an `App_Code` change: a request the old worker is still compiling fails with CS0006 | webForCore bug | umbraco-6 | [#89](https://github.com/xforcore/webForCore/issues/89) |
| [F60](#f60) | Files without a BOM are read as UTF-8, not the ANSI code page: Windows-1252 characters become U+FFFD | webForCore bug | umbraco-6 | [#90](https://github.com/xforcore/webForCore/issues/90) |
| [F61](#f61) | Code or configuration that loads assemblies from `~/bin` must follow `bin_dotnet` | guide gap | umbraco-6, umbraco-7 | [#91](https://github.com/xforcore/webForCore/issues/91) |
| [F62](#f62) | Linux: `Uri.TryCreate("/path", UriKind.Absolute)` succeeds and `Path.VolumeSeparatorChar` is `/` | guide gap | umbraco-6, umbraco-7 | [#92](https://github.com/xforcore/webForCore/issues/92) |
| [F63](#f63) | Calls that become ambiguous on .NET 10 (CS0121): the app's own `DistinctBy`, empty `params` calls | guide gap | umbraco-6, umbraco-7 | [#93](https://github.com/xforcore/webForCore/issues/93) |
| [F64](#f64) | Umbraco on Linux: Windows file names, packages only on its MyGet feed, an installer that needs a JSON POST | app/environment | umbraco-6, umbraco-7 | - |
| [F65](#f65) | Linux: named semaphores and events throw `PlatformNotSupportedException` (only named `Mutex` works) | guide gap | umbraco-7 | [#94](https://github.com/xforcore/webForCore/issues/94) |
| [F66](#f66) | webForCore's `CallContext` doesn't reach prebuilt .NET Framework libraries: Log4Net.Async logs nothing | guide gap | umbraco-7 | [#95](https://github.com/xforcore/webForCore/issues/95) |
| [F67](#f67) | Linux with no locale (`C`/`POSIX`: CI runners, containers): the culture is invariant, so apps that pick resources by culture find none | guide gap | umbraco-6 | [#97](https://github.com/xforcore/webForCore/issues/97) |
| [F68](#f68) | Web API OData (Microsoft.AspNet.OData 7.5.18) needs System.Data.Linq to build any model | documented gap (consequence undocumented) | odata-samples-compositekey | [#120](https://github.com/xforcore/webForCore/issues/120) |
| [F69](#f69) | Windows Identity Foundation (`System.IdentityModel`) isn't on .NET or in NAMESPACES.md: IdentityServer3 can't load | guide gap | identityserver3-example | [#119](https://github.com/xforcore/webForCore/issues/119) |
| [F70](#f70) | `/signalr/hubs` serves the template's unreplaced `[!VERSION!]` | webForCore bug | signalr-samples | [#118](https://github.com/xforcore/webForCore/issues/118) |
| [F71](#f71) | VB projects: `<MyType>Windows</MyType>` doesn't compile on .NET (BC30002 `My.Computer`, `My.User`) | guide gap | signalr-samples | [#121](https://github.com/xforcore/webForCore/issues/121) |
| [F72](#f72) | Linux: the user's inotify instances (128 by default) run out with many apps running: "Failed to start monitoring changes" | app/environment | signalr-samples, identityserver3-example (on this machine) | - |
| [F73](#f73) | MVC 3/4 apps: `webpages:Version` 1.0.0.0/2.0.0.0 fails every request ("Conflicting versions of ASP.NET Web Pages") | guide gap | mvc-music-store | [#122](https://github.com/xforcore/webForCore/issues/122) |
| [F74](#f74) | MVC 3/4 apps: `[Compare]` is ambiguous (CS0104) between MVC's and DataAnnotations' `CompareAttribute` | guide gap | mvc-music-store | [#122](https://github.com/xforcore/webForCore/issues/122) |
| [F75](#f75) | No `aspnet_regsql`: `SqlServices.Install` can't find `InstallCommon.sql`, so SQL membership/roles/profile apps can't create their database | webForCore bug | mvc-music-store | [#123](https://github.com/xforcore/webForCore/issues/123) |
| [F76](#f76) | ICU: `en-US`/`en-GB` `NumberDecimalDigits` is 3, so `{0:F}` prints `8.990` for 8.99 | guide gap | mvc-music-store | [#124](https://github.com/xforcore/webForCore/issues/124) |
| [F77](#f77) | Windows-only data stores (SQL CE, SQL Express user instances) and data written on Windows (`\` in a wiki's page index) | app/environment | screwturn-wiki, mvc-music-store, rabbit-webpages | - |
| [F78](#f78) | WebMatrix.Data and WebMatrix.WebData aren't provided or documented (Microsoft's 3.2.9 packages work) | guide gap | rabbit-webpages | [#130](https://github.com/xforcore/webForCore/issues/130) |
| [F79](#f79) | `Response.End()` after `StatusCode = 401` sends the 401: forms authentication doesn't redirect to the log-in page (fixed in 0.9.0) | webForCore bug | rabbit-webpages | [#129](https://github.com/xforcore/webForCore/issues/129) |
| [F80](#f80) | A request path with an escaped character (a space, non-ASCII) is answered 400 before the application (fixed in 0.9.0) | webForCore bug | aspnetdocs-wingtip-toys | [#126](https://github.com/xforcore/webForCore/issues/126) |
| [F81](#f81) | `Request.RawUrl` is the default document's path for a directory request: FriendlyUrls 301s `/` to `/Default` (fixed in 0.9.0) | webForCore bug | aspnetdocs-wingtip-toys | [#127](https://github.com/xforcore/webForCore/issues/127) |
| [F82](#f82) | Linux: `Server.MachineName` throws `DllNotFoundException` (kernel32); ELMAH's log pages fail (fixed in 0.9.0) | webForCore bug | aspnetdocs-wingtip-toys | [#128](https://github.com/xforcore/webForCore/issues/128) |
| [F83](#f83) | C# 14's span conversions break LINQ to Entities at run time (`Contains(ReadOnlySpan)`), not only the build | guide gap | nopcommerce-3.90 | [#138](https://github.com/xforcore/webForCore/issues/138) |
| [F84](#f84) | `AppDomain.CurrentDomain.DynamicDirectory` is null: plugin managers that shadow-copy there fail at start | guide gap | nopcommerce-3.90, smartstorenet | [#139](https://github.com/xforcore/webForCore/issues/139) |
| [F85](#f85) | System.Linq.Dynamic 1.0.7 (net40) needs .NET Framework's `System.Data.Entity` 4.0 | app/environment | nopcommerce-3.90 | – |
| [F86](#f86) | Linux: a static file in another case is 404 until requested once in its own case, then 200 in any case | webForCore bug | nopcommerce-3.90 | [#140](https://github.com/xforcore/webForCore/issues/140) |
| [F87](#f87) | `System.Web.Optimization` and `WebGrease` don't have Microsoft's identities: BundleTransformer fails with CS0012 | webForCore bug | smartstorenet | [#141](https://github.com/xforcore/webForCore/issues/141) |
| [F88](#f88) | The same as F92: one inotify instance per watched folder; nopCommerce takes more than 50 of the user's 128 | webForCore bug | nopcommerce-3.90 | [#152](https://github.com/xforcore/webForCore/issues/152) |
| [F89](#f89) | Windows, intermittent: nopCommerce's plugin manager can't replace a plugin copy in the compilation folder at start ("rename failed") | app/environment | nopcommerce-3.90 | – |
| [F90](#f90) | The parameterless crypto factories (`KeyedHashAlgorithm.Create()`, `HashAlgorithm.Create()`, ...) throw on .NET | guide gap | kooboo-cms | [#151](https://github.com/xforcore/webForCore/issues/151) |
| [F91](#f91) | DotNetZip 1.9 (`Ionic.Zip.Reduced`) can't extract on Linux | app/environment | kooboo-cms | - |
| [F92](#f92) | Linux: every watched directory is an inotify instance on .NET 10; Kooboo's worker alone takes 48+ of the default 128 | webForCore bug | kooboo-cms (umbraco-8: 20) | [#152](https://github.com/xforcore/webForCore/issues/152) |
| [F93](#f93) | CSharpTest.Net.Collections 14.906 (Umbraco 8's NuCache file store) reflects into `FileStream` and P/Invokes kernel32 | app/environment | umbraco-8 | - |
| [F94](#f94) | An app's own `Microsoft.CodeAnalysis` (Roslyn) package conflicts with webForCore.Compilers' 5.9.0 (NU1107) | guide gap | umbraco-8 | [#153](https://github.com/xforcore/webForCore/issues/153) |
| [F97](#f97) | Linux: each monitored folder takes an inotify instance of its own (two for many), so a site with many folders can't start | webForCore bug | orchard-1.10 | [#145](https://github.com/xforcore/webForCore/issues/145) |
| [F98](#f98) | `<httpRuntime fcnMode>` is ignored: `Disabled` and `Single` still monitor every folder | webForCore bug | orchard-1.10 | [#146](https://github.com/xforcore/webForCore/issues/146) |
| [F99](#f99) | web.config's `<probing privatePath>` isn't applied: `Assembly.Load` doesn't find the assemblies there | webForCore bug | orchard-1.10 | [#147](https://github.com/xforcore/webForCore/issues/147) |
| [F100](#f100) | Autofac.Configuration 3.x, Castle.Core 3.x and NHibernate 4.x don't run on .NET (`AppDomainSetup.ConfigurationFile`, `AppDomain.DefineDynamicAssembly`) | guide gap | orchard-1.10 | [#148](https://github.com/xforcore/webForCore/issues/148) |
| [F101](#f101) | `System.Data.Services.Client` isn't on .NET or in the guide; Microsoft.Data.Services.Client 5.8.4 has the same namespace | guide gap | orchard-1.10 | [#149](https://github.com/xforcore/webForCore/issues/149) |
| [F102](#f102) | Plug-in class libraries built into their own folder lose their NuGet libraries (SDK libraries don't copy packages) | guide gap | orchard-1.10 | [#150](https://github.com/xforcore/webForCore/issues/150) |
| [F103](#f103) | Orchard's run-time module compilation parses the old `.csproj` format, so it can't read the SDK projects | app/environment | orchard-1.10 | - |
| [F104](#f104) | webForCore.AutoMapperLegacy on AutoMapper 12: a member mapped from a value already of its type is mapped again through a base-type map, and the whole configuration fails | app/environment (webForCore-thirdparty) | umbraco-7 | [thirdparty#11](https://github.com/xforcore/webForCore-thirdparty/issues/11) |
| [F105](#f105) | `ApplicationSettingsBase` (Settings.settings, Web Reference SOAP clients) throws `TypeLoadException`: `ConfigurationManagerInternal` is looked up in `System.Configuration` 4.0.0.0 | webForCore bug | umbraco-7 | [#154](https://github.com/xforcore/webForCore/issues/154) |
| [F106](#f106) | webForCore's BinaryFormatter port can't serialize a `System.Type` (.NET Framework writes `UnitySerializationHolder`) | webForCore bug | orchard-1.10 | [#156](https://github.com/xforcore/webForCore/issues/156) |
| [F116](#f116) | Windows Workflow Foundation 3 (`System.Workflow.*`) isn't on .NET, and the docs and blocker search don't say so | guide gap | composite-c1 | [#132](https://github.com/xforcore/webForCore/issues/132) |
| [F117](#f117) | Run-time C# compilation in application code (`CodeDomProvider.CreateProvider("c#")`) throws `PlatformNotSupportedException`; no documented replacement | guide gap | composite-c1 | [#133](https://github.com/xforcore/webForCore/issues/133) |
| [F118](#f118) | CoreWCF's source generator reaches applications: `CS8370`/`CS8107` for any `[ServiceContract]` with `LangVersion` below 11 | webForCore bug | composite-c1 | [#134](https://github.com/xforcore/webForCore/issues/134) |
| [F119](#f119) | webForCore's `CSharpCodeProvider` with `GenerateInMemory` and no PDB throws `ArgumentNullException` | webForCore bug | composite-c1 | [#135](https://github.com/xforcore/webForCore/issues/135) |
| [F120](#f120) | C1's core needs LINQ to SQL (even with the XML data store) and WCF REST hosting | documented gap | composite-c1 | – |
| [F121](#f121) | Types .NET added (`System.Collections.Generic.KeyValuePair`) make the app's own type ambiguous (`CS0104`) | guide gap | composite-c1, smartstorenet | [#136](https://github.com/xforcore/webForCore/issues/136) |
| [F123](#f123) | A Web Site Project has no `.csproj`; the guide's template compiles its `CodeFile=` code (CS0103) | guide gap | rock-rms (assessed only) | [#131](https://github.com/xforcore/webForCore/issues/131) |
| [F130](#f130) | `BuildManager.GetType(name, throwOnError: false)` throws `FileNotFoundException` for a missing assembly (.NET Framework returns `null`): ClientDependency and DNN fail at start | webForCore bug | dnn-platform-9 | [#137](https://github.com/xforcore/webForCore/issues/137) |
| [F131](#f131) | `Thread.ResetAbort()` after `catch (ThreadAbortException)` throws `PlatformNotSupportedException`; the guide doesn't say to remove it | guide gap | dnn-platform-9 | [#143](https://github.com/xforcore/webForCore/issues/143) |
| [F132](#f132) | An application's design-time classes (`System.Web.UI.Design.ExpressionEditor`, System.Design) don't compile | guide gap | dnn-platform-9 | [#144](https://github.com/xforcore/webForCore/issues/144) |
| [F133](#f133) | Linux: DNN builds every physical path with `\` (`ApplicationMapPath` is `BaseDirectory.Replace("/", "\\")`), and QuickIO.NET is Win32 | app/environment | dnn-platform-9 | – |
| [F154](#f154) | A library's relative paths resolve against `BaseDirectory` (`bin_dotnet/`): log4net writes its log into `bin_dotnet`, and every log line restarts the application | guide gap | mojoportal | to raise |
| [F155](#f155) | Web Pages 1 sites meet Razor 3's stricter parser: `@string.Join(...)` in a code block is a parser error | guide gap | rabbit-webpages | to raise |
---

## F1

**.NET Framework libraries get a second `ConfigurationManager` that can't read web.config.** webForCore bug. Raised as [#1](https://github.com/xforcore/webForCore/issues/1).

- **Apps:** bonobo-git-server (WebActivatorEx 2.2.0, which reads `AppSettings` in its PreApplicationStartMethod, so
  the whole site fails); eshop-legacy-mvc, eshop-legacy-webforms (`Microsoft.AspNet.SessionState.SessionStateModule` 1.1.0, which web.config's
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

**`CacheDependency` on one file under two spellings throws on Linux.** webForCore bug. Raised as [#2](https://github.com/xforcore/webForCore/issues/2).

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
`docs/NAMESPACES.md` or `docs/IIS-COMPATIBILITY.md`). Raised as [#3](https://github.com/xforcore/webForCore/issues/3).

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

**W3C logs are written inside the site and served over HTTP.** webForCore bug, with security impact. Raised as [#15](https://github.com/xforcore/webForCore/issues/15).

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

**Third-party packages bring Microsoft's ASP.NET packages back transitively.** Guide gap. Raised as [#4](https://github.com/xforcore/webForCore/issues/4).

- **Apps:** eshop-legacy-mvc, and in round 2 bonobo-git-server, applicant-tracking, realocate and roadkill (where it
  showed as F24, at run time). eshop-legacy-mvc: `Autofac.Mvc5` 4.0.2 depends on `Microsoft.AspNet.Mvc` 5.1.0 (and through it Razor 3.1,
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

**A `lib/net45`-only package with a `build/` folder restores with no assembly.** Guide gap. Raised as [#5](https://github.com/xforcore/webForCore/issues/5).

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

**Packages that must be upgraded.** Guide gap (small). Raised as [#6](https://github.com/xforcore/webForCore/issues/6).

- **Apps:** eshop-legacy-mvc.
- `EntityFramework` 6.2.0 has only `net40`/`net45` builds; on .NET it needs 6.4 or later (the guide's "EntityFramework
  6.4+" example implies this but doesn't say to upgrade). 6.5.1 worked, with `System.Data.SqlClient` against SQL Server
  2022.
- `Newtonsoft.Json` below 13.0.4 is a restore error: `NU1605: Detected package downgrade: Newtonsoft.Json from 13.0.4
  to 12.0.1` (`eShopLegacyMVC -> webForCore.Web.Optimization -> Newtonsoft.Json (>= 13.0.4)`; System.Net.Http.Formatting
  references 13.0.4 too).
- `Microsoft.IdentityModel.Tokens` below 8.19.1 is a restore error too (bonobo-git-server had 5.1.3, from
  WS-Federation): `NU1605 ... webForCore -> webForCore.Web.Extensions -> CoreWCF.Http -> Microsoft.IdentityModel.Tokens
  (>= 8.19.1)`. So every app gets CoreWCF and IdentityModel 8 through the "Always" set. The guide could list the minimum versions webForCore's assemblies
  impose.

## F8

**`HttpUtility` in app code is CS0433 outside webForCore's repository.** webForCore bug. Raised as [#7](https://github.com/xforcore/webForCore/issues/7).

- **Apps:** bonobo-git-server (`IHtmlString`: every type in the framework's `System.Web.HttpUtility.dll` clashes);
  eshop-legacy-webforms (`ViewSwitcher.ascx.cs:39`, `HttpUtility.UrlEncode`), the Visual Studio Web Forms
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
consequence isn't. Raised as [#8](https://github.com/xforcore/webForCore/issues/8).

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

## F10

**.NET Framework assemblies that are NuGet packages on .NET.** Guide gap. Raised as [#9](https://github.com/xforcore/webForCore/issues/9).

- **Apps:** miniblog: `app_code/handlers/FeedHandler.cs` uses `System.ServiceModel.Syndication` (`SyndicationFeed`,
  `Rss20FeedFormatter`).
- **Error:** every page, because `app_code` compiles as a whole: `Compilation Error ... CS1069: The type name
  'SyndicationItem' could not be found in the namespace 'System.ServiceModel.Syndication'. This type has been forwarded
  to assembly 'System.ServiceModel.Syndication, Version=10.0.0.0' Consider adding a reference to that assembly.`
- **Cause:** on .NET Framework the type is in `System.ServiceModel.dll`, which the root web.config's
  `<compilation><assemblies>` references for every site. On .NET it is the `System.ServiceModel.Syndication` package,
  so code compiled at run time (App_Code, pages) only sees it if the app references the package.
- **What the guide says:** `docs/MIGRATING.md` covers NuGet packages and Microsoft's ASP.NET assemblies, but not
  .NET Framework assemblies that became packages. Others in the same position: `System.Data.SqlClient`,
  `System.Data.OleDb`, `System.DirectoryServices`, `System.Runtime.Caching`, `System.ServiceModel.*` (client),
  `System.Drawing.Common`'s friends, `System.Configuration.ConfigurationManager` (which webForCore supplies).
- **What worked:** `<PackageReference Include="System.ServiceModel.Syndication" Version="10.0.0" />`.

## F11

**One URL Rewrite feature ASP.NET Core lacks drops every rule, failing open.** webForCore bug. Raised as [#10](https://github.com/xforcore/webForCore/issues/10).

- **Apps:** miniblog. Its first rule is IIS's own "Remove WWW" template:
  ```xml
  <rule name="Remove WWW" patternSyntax="Wildcard" stopProcessing="true">
    <match url="*" />
    <conditions><add input="{CACHE_URL}" pattern="*://www.*" /></conditions>
    <action type="Redirect" url="{C:1}://{C:2}" redirectType="Permanent" />
  </rule>
  ```
- **Error:** logged once per request, nothing in the response:
  ```
  fail: WebFormsForCore.Hosting.IIS.IISModules[0]
        The <rewrite> rules for / cannot be used.
        System.NotSupportedException: Wildcard syntax is not supported
           at Microsoft.AspNetCore.Rewrite.IISUrlRewrite.UrlRewriteRuleBuilder.AddUrlMatch(...)
  ```
  and, with the rule rewritten as a regular expression, `System.FormatException: Unrecognized parameter type:
  'CACHE_URL'` from `ServerVariables.FindServerVariable`.
- **Impact:** all ten rules are skipped, including `ProtectStorageXml`, which answers `posts/*.xml` with 403: the
  blog's post files (drafts and comment authors' e-mail addresses included) are served as static files. The slug,
  paging, category, robots.txt and sitemap rewrites are 404s.
- **Cause:** `InboundRewrite` hands the section to ASP.NET Core's `IISUrlRewrite` parser, which supports neither
  `patternSyntax="Wildcard"` nor `{CACHE_URL}`, and on any parse error the whole level's rules are dropped.
  `docs/IIS-COMPATIBILITY.md` lists inbound rewrite as "Works" without these limits.
- **Repro:** web.config `<rewrite><rules>` with the rule above followed by
  `<rule name="block"><match url="^secret\.txt$" /><action type="CustomResponse" statusCode="403" /></rule>`, and a
  `secret.txt` file: `GET /secret.txt` returns the file.
- **Suggested fix:** support wildcard patterns (IIS's `*`/`?` matching, `{R:n}` per `*`) and `{CACHE_URL}`; and fail
  closed (500, as IIS does for configuration it can't use) instead of running without the rules.

## F12

**A rewrite to a query-only URL is a bare 500.** webForCore bug. Raised as [#11](https://github.com/xforcore/webForCore/issues/11).

- **Apps:** miniblog: `<action type="Rewrite" url="?slug={R:1}" />` for `^post/(.*)`, and the same for paging and
  categories. On IIS the rewritten URL is resolved against the site root, so `/post/test-1` runs `/?slug=test-1`
  (the default document).
- **Error:** 500 with an empty body; Kestrel logs `System.IndexOutOfRangeException: Index was outside the bounds of the
  array. at Microsoft.AspNetCore.Rewrite.UrlActions.RewriteAction.ApplyAction(...)` from
  `WebFormsForCore.Hosting.IIS.InboundRewrite.ApplyGroup`.
- **Repro:** `<rule name="q"><match url="^a$" /><action type="Rewrite" url="?x=1" /></rule>`, then `GET /a`.
  `GET /?x=1` itself works.
- **Suggested fix:** treat a rewrite URL that starts with `?` as the site root plus that query before handing it to
  ASP.NET Core's action (or catch the exception and answer as IIS does).

## F13

**`<handlers>` entries whose `path` starts with `/` never match.** webForCore bug. Raised as [#12](https://github.com/xforcore/webForCore/issues/12).

- **Apps:** miniblog: `path="/comment.ashx"` (comment posting), `path="/post.ashx"`, `path="/metaweblog"` (Windows Live
  Writer / XML-RPC) and `path="/feed/*"` (RSS and Atom) are all 404. `path="*.css"` and `path="*.js"` in the same
  section work.
- **Cause:** `WebServerModules.FindHandler` (`src/WebFormsForCore.Web/WebFormsForCore/AspNetCoreHosting/IIS/WebServerModules.cs:75`)
  compares a pattern that contains `/` with the request path without its leading `/`
  (`path.TrimStart('/')`), but leaves the pattern's own leading `/`, so `/metaweblog` is matched against `metaweblog`.
- **Repro:** `<system.webServer><handlers><add name="h" verb="*" path="/hello" type="HelloHandler" /></handlers>` with
  `HelloHandler : IHttpHandler` in App_Code: `GET /hello` is 404; with `path="hello"` it works.
- **Suggested fix:** trim the pattern's leading `/` as well.

## F14

**The trailing-slash 301 for a directory pre-empts routing with `RouteExistingFiles = true`.** webForCore bug. Raised as [#13](https://github.com/xforcore/webForCore/issues/13).

- **Apps:** clean-architecture-mvc5: feature folders `Products/` and `Contact/` (controllers and views) share their
  names with the controllers, and `RouteConfig` sets `routes.RouteExistingFiles = true` so that `/Products` reaches
  `ProductsController`, the usual fix for this on IIS.
- **What happens:** `GET /Products` → `301 Moved Permanently`, `Location: /Products/` (then 200 from the controller).
  On IIS the request is routed: with `RouteExistingFiles = true`, `UrlRoutingModule` maps it to `MvcHandler` and
  the native default-document/static-file courtesy redirect never runs. Links generated by `Html.ActionLink`
  (`/Products`) therefore all take a redirect, and a POST to such a URL would be turned into a GET.
- **Cause:** the default-document step in front of the app answers every existing directory without a trailing slash
  with a 301 (`docs/IIS-COMPATIBILITY.md`: "301 to add a missing trailing slash") before System.Web's routing has
  run.
- **Repro** (folder `Foo/` with any file in it, and a `Default.aspx`):
  ```aspx
  <%@ Application Language="C#" %>
  <script runat="server">
      void Application_Start(object sender, EventArgs e)
      {
          System.Web.Routing.RouteTable.Routes.RouteExistingFiles = true;
          System.Web.Routing.RouteTable.Routes.MapPageRoute("foo", "Foo", "~/Default.aspx");
      }
  </script>
  ```
  `GET /Foo` → 301 to `/Foo/`; on IIS, `Default.aspx` through the route.
- **Suggested fix:** skip the courtesy redirect when the application's `RouteTable.Routes.RouteExistingFiles` is true and
  a route matches the URL (or leave directories to System.Web, which runs the redirect itself when no route takes the
  request).

## F15

**The Visual Studio template's social login packages aren't in webForCore.** Guide gap. Raised as [#14](https://github.com/xforcore/webForCore/issues/14).

- **Apps:** aspnetdocs-mvcmovie. Every app made from the Visual Studio 2013-2019 "MVC" or "Web Forms" template with
  individual accounts references `Microsoft.Owin.Security.Facebook`, `.Google`, `.MicrosoftAccount` and `.Twitter`, and
  `App_Start/Startup.Auth.cs` has `using Microsoft.Owin.Security.Google;` even with the providers commented out.
- **Error:** `CS0234: The type or namespace name 'Google' does not exist in the namespace 'Microsoft.Owin.Security'`
  once the converter drops the Katana packages webForCore replaces.
- **What the guide says:** `docs/MIGRATING.md` section 3 lists OWIN security as Cookies and OAuth; `docs/NAMESPACES.md`
  likewise. Neither says what to do with the other Katana packages.
- **What worked:** keep the original 3.0.1 packages and exclude their Katana dependencies, so webForCore's
  `Microsoft.Owin` 4.2.3 is the only one:
  ```xml
  <PackageReference Include="Microsoft.Owin.Security.Google" Version="3.0.1" />
  <PackageReference Include="Microsoft.Owin" Version="3.0.1" ExcludeAssets="all" />
  <PackageReference Include="Microsoft.Owin.Security" Version="3.0.1" ExcludeAssets="all" />
  ```
  It builds and the app runs; the providers themselves weren't exercised (they need the external services). Lifting
  them (they're in the same Katana repository webForCore lifts from) or documenting this would cover the template.
- **Round 2:** 0.2.0 documents this step. It isn't enough once a provider is actually registered: see F17.

## F16

**Packages webForCore replaces are at one version; an app written against an older one can collide with its additions.**
Guide gap. Raised as [#17](https://github.com/xforcore/webForCore/issues/17). Found on 0.2.0.

- **Apps:** open-order-framework (ASP.NET Identity 2.0.0).
- **Error:** `CS0104: 'SignInStatus' is an ambiguous reference between 'OpenOrderFramework.Models.SignInStatus' and
  'Microsoft.AspNet.Identity.Owin.SignInStatus'` in `Controllers/AccountController.cs`. Identity 2.0.0 had no
  `SignInStatus`, and the app's template defines its own in `App_Start/IdentityConfig.cs`. webForCore's
  `Microsoft.AspNet.Identity.Owin` is 2.2, which added `SignInStatus` and `SignInManager`. The app has
  `using Microsoft.AspNet.Identity.Owin;` and `using OpenOrderFramework.Models;`.
- **What the guide says:** `docs/MIGRATING.md` section 1 lists the packages webForCore replaces, but not that each is
  one version (MVC 5.3, Web API 5.3, Web Pages 3.3, Identity 2.2, Katana 4.2), so an app on an older version gets
  an upgrade. On .NET Framework the same clash happens when the app upgrades its packages, but here it happens as
  part of the migration.
- **What worked:** a using alias in the file with the clash:
  `using SignInStatus = OpenOrderFramework.Models.SignInStatus;`.
- **Suggested fix:** in `docs/MIGRATING.md` section 1, say which version each replaced package is, that the migration
  upgrades older ones, and that new types (Identity 2.1/2.2's `SignInStatus`, `SignInManager`) can be ambiguous with
  an app's own copies from the Identity 2.0 samples.

## F17

**The Katana social providers webForCore doesn't lift fail at start-up when they're used:**
`System.Net.Http.WebRequest` doesn't exist on .NET. Guide gap: F15's documented step works only while the providers are
commented out. Raised as [#18](https://github.com/xforcore/webForCore/issues/18). Found on 0.2.0.

- **Apps:** open-order-framework (Katana 2.1.0), socialgoal (Katana 2.0.0). Both call `app.UseGoogleAuthentication()`
  in `Startup.Auth.cs`. MvcMovie (F15), where the providers are commented out, works.
- **Error:** every request, static files included, fails in `OwinHttpModule.Init`:
  ```
  FileNotFoundException: Could not load file or assembly 'System.Net.Http.WebRequest, Version=4.0.0.0,
  Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'.
     at Microsoft.Owin.Security.Google.GoogleAuthenticationMiddleware.ResolveHttpMessageHandler(GoogleAuthenticationOptions options)
     at Microsoft.Owin.Builder.AppBuilder.BuildInternal(Type signature)
     at Microsoft.Owin.Host.SystemWeb.OwinAppContext.Initialize(Action`1 startup)
  ```
  Each Katana provider (Google, Facebook, MicrosoftAccount, Twitter; 2.x to 4.x) does
  `options.BackchannelHttpHandler ?? new WebRequestHandler()`. `WebRequestHandler` is in .NET Framework's
  `System.Net.Http.WebRequest.dll`, which .NET doesn't have. The method can't be compiled, so setting
  `BackchannelHttpHandler` doesn't help.
- **Repro:** the F15 setup (`Microsoft.Owin.Security.Google` 3.0.1 with `Microsoft.Owin`/`.Security` excluded), and in
  the OWIN startup `app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions { ClientId = "x", ClientSecret = "y" });`.
  Any request is a 500 with the error above.
- **Suggested fix:** lift the four providers (they're in the Katana repository webForCore lifts from) with their
  back-channel handler as `HttpClientHandler`. Or ship a `System.Net.Http.WebRequest` assembly with a
  `WebRequestHandler : HttpClientHandler`, which would cover the original packages, `WsFederation` and third-party
  Katana providers too. At least, `docs/MIGRATING.md`'s "Katana packages webForCore doesn't lift" should say that
  the original packages only compile and fail as soon as a provider is registered.

## F18

**Configuration types an assembly references through `System.dll` reach the host's ConfigurationManager, not the
application's:** ELMAH's section handlers fail. webForCore bug: the part of F1 that 0.2.0's fix doesn't cover. Found
on 0.2.0.

- **Apps:** socialgoal (elmah.corelibrary 1.2.2, with Elmah.MVC). ELMAH is one of the most common ASP.NET packages.
- **Error:** every request:
  ```
  Configuration Error. An error occurred creating the configuration section handler for elmah/errorMail:
  Type 'Elmah.ErrorMailSectionHandler' does not inherit from 'System.Configuration.IConfigurationSectionHandler'.
     at System.Configuration.RuntimeConfigurationRecord.CreateSectionFactory(FactoryRecord factoryRecord)
  ```
- **Cause:** `ErrorMailSectionHandler` derives from `System.Configuration.SingleTagSectionHandler`, which ELMAH (built for
  .NET 2.0) references from `System, Version=2.0.0.0`. Its metadata shows `SingleTagSectionHandler -> System 2.0.0.0` and
  `ConfigurationException -> System 2.0.0.0`; `ConfigurationManager` comes from `System.Configuration`. On .NET,
  `System.dll` is a facade that forwards these types to `System.Configuration.ConfigurationManager`. 0.2.0 loads the
  `System.Configuration` facade per PseudoAppDomain (`PseudoAppDomain.PerDomainFrameworkAssemblies`, F1), but `System`
  is still shared from the default context. So its forwards resolve to the host's copy of
  `System.Configuration.ConfigurationManager`, and the application's configuration system, which is the domain's copy,
  sees a different `IConfigurationSectionHandler`.
- **Repro:** a site with `<PackageReference Include="elmah.corelibrary" Version="1.2.2" />`, ELMAH's `<sectionGroup
  name="elmah">` in web.config and its `ErrorMailModule` in `<system.webServer><modules>`. Or, smaller: a net20–net48
  class library with `public class H : System.Configuration.SingleTagSectionHandler {}`, declared as a `<section>` and
  read with `ConfigurationManager.GetSection`.
- **Suggested fix:** add `System` to `PerDomainFrameworkAssemblies` (its other forwards go to framework assemblies,
  which still resolve to the shared ones), and check the other facades that forward into
  `System.Configuration.ConfigurationManager`.

## F19

**`System.Web.Abstractions` and `System.Web.Routing` 4.0.0.0 can't be loaded:** web.configs that list them in
`<compilation><assemblies>` fail on every request. webForCore bug. Raised as [#19](https://github.com/xforcore/webForCore/issues/19). Raised as [#20](https://github.com/xforcore/webForCore/issues/20). Found on 0.2.0.

- **Apps:** roadkill. Web.configs from MVC 2-4 projects have
  `<add assembly="System.Web.Abstractions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35" />` and
  the same for `System.Web.Routing`, and libraries built for .NET 3.5/4.0 reference them.
- **Error:** every request:
  ```
  Configuration Error. Could not load file or assembly 'System.Web.Abstractions, Version=4.0.0.0, Culture=neutral,
  PublicKeyToken=31bf3856ad364e35'. The system cannot find the file specified.
  Source File: .../web.config Line: 34
  ```
- **On .NET Framework:** since 4.0 both are in the GAC as facades that type-forward `HttpContextBase` and the rest of
  the abstractions, and `RouteTable`, `RouteBase` and the rest of routing, to `System.Web`. So the entries resolve,
  and assemblies compiled against them bind.
- **Repro:** any site, with the two `<add assembly=... Version=4.0.0.0 ...>` lines in `<compilation><assemblies>`.
- **Suggested fix:** ship `System.Web.Abstractions.dll` and `System.Web.Routing.dll` 4.0.0.0 facades (type forwarders
  to `System.Web`) in the `webForCore` package, as .NET does for its own facades. That fixes the web.config entries and
  old assemblies that reference them.

## F20

**`Assembly.LoadFrom` in an application returns the default load context's copy:** WebActivatorEx's start methods
never run. webForCore bug. Raised as [#21](https://github.com/xforcore/webForCore/issues/21). Found on 0.2.0.

- **Apps:** applicant-tracking (Unity.Mvc 5 registers its resolver from `UnityMvcActivator` by
  `WebActivatorEx.PreApplicationStartMethod`), roadkill (StructureMap from `LocatorStartup` in `Roadkill.Core`).
  WebActivatorEx is how Unity.Mvc, Ninject.MVC, StructureMap.MVC, Glimpse and many other packages start; Bonobo Git
  Server has the same activator but calls `SetResolver` again from `Global.asax`, so it doesn't show.
- **Error:** none at start-up. The activators silently don't run, and the first symptom is
  `Cannot dynamically create an instance of type '...Controller'. Reason: No parameterless constructor defined.`
  (applicant-tracking) or a `NullReferenceException` on the container in the OWIN startup (roadkill).
- **Cause:** WebActivatorEx's `ActivationManager` (started by its own `System.Web.PreApplicationStartMethod`) finds the
  attributes by `Assembly.LoadFrom` on every DLL in `HttpRuntime.BinDirectory`, then
  `GetCustomAttributes(typeof(WebActivatorEx.PreApplicationStartMethodAttribute))`. On .NET, `Assembly.LoadFrom` always
  loads into `AssemblyLoadContext.Default`, so it returns a second copy whose attributes are a different
  `WebActivatorEx` type from the one running in the PseudoAppDomain. Checked from `Application_Start` in
  applicant-tracking:
  ```
  IsHosted=True bin=<site>/bin_dotnet/
  mine ALC=PseudoAppDomain 4f77251c-1 (<site>)
  LoadFrom ALC="Default" System.Runtime.Loader.DefaultAssemblyLoadContext #0 same=False attrs=0
  ```
  On .NET Framework, `LoadFrom` in the application's AppDomain returns the assembly already loaded there.
- **Repro:** a site with `<PackageReference Include="WebActivatorEx" Version="2.2.0" />` and
  `[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Start), "Run")]` on a class whose `Run` sets a static
  field that a page prints. The page prints the unset value. Or, in `Application_Start`:
  `ReferenceEquals(Assembly.LoadFrom(Path.Combine(HttpRuntime.BinDirectory, "App.dll")), typeof(Global).Assembly)` is
  `false`.
- **Suggested fix:** .NET has no hook to redirect `LoadFrom`. Since it's one package used everywhere, webForCore could
  lift WebActivatorEx (Apache 2.0) with `Assembly.LoadFrom` replaced by a load into the calling assembly's context (or
  `BuildManager.GetReferencedAssemblies()`). It would also be worth listing `Assembly.LoadFrom` as a difference in
  `docs/IIS-COMPATIBILITY.md`, since application code that does the same has the same problem.

## F21

**Glimpse doesn't run on .NET.** Guide gap. Raised as [#22](https://github.com/xforcore/webForCore/issues/22). Found on 0.2.0.

- **Apps:** realocate (Glimpse 1.8/1.9: Core, Ado, AspNet, EF6, Mvc5).
- **Errors:** Glimpse.Ado's pre-application-start method throws `NullReferenceException` in
  `Glimpse.Ado.AlternateType.Support.FindDbProviderFactoryTable` (it reflects into .NET Framework's private
  `DbProviderFactories` table). Without Glimpse.Ado, `Glimpse.AspNet.HttpModule.Init` throws `MissingMethodException:
  'System.String System.AppDomainSetup.get_ShadowCopyFiles()'`.
- **What the guide says:** `docs/MIGRATING.md` mentions Glimpse only as a package that brings MVC back (F5).
- **What worked:** remove Glimpse (a development diagnostics tool): its packages, the `glimpse` config section, module
  and handler, and the app's `GlimpseSecurityPolicy`.
- **Suggested fix:** say in `docs/MIGRATING.md` that Glimpse 1.x can't run on .NET and must be removed.

## F22

**AutoMapper before 12 doesn't run on .NET 11.** App/environment (not webForCore: it reproduces in a plain .NET 11
console app). Found on 0.2.0.

- **Apps:** realocate (AutoMapper 4.2.0, upgraded to 12.0.1), socialgoal (AutoMapper 3.1.1, static API, not upgraded).
- **Error:** configuring a map throws from `MakeGenericMethod`. AutoMapper 3.x/4.x: `The type or method has 3 generic
  parameter(s), but 1 generic argument(s) were provided`. 5.x to 11.x: `GenericArguments[0], 'System.Char', on 'T
  MaxFloat[T](IEnumerable`1[T])' violates the constraint of type 'T'`. AutoMapper scans `System.Linq.Enumerable`'s
  extension methods, non-public ones included, for flattening, and .NET 11's include `MaxInteger<T>`/`MaxFloat<T>`,
  which have generic math constraints.
- **Repro:** .NET 11, `#:package AutoMapper@11.0.1`,
  `new MapperConfiguration(c => c.CreateMap(typeof(Src), typeof(Dst)))` where `Src` has `string Name` and `Dst` has
  `string Name` and `int NameLength`. It throws; with AutoMapper 12.0.1 it works.
- **Note:** AutoMapper 12 has a different configuration API from 3–4 (no static `Mapper`, `IMapperConfiguration` is
  `IMapperConfigurationExpression`). A line in `docs/MIGRATING.md`'s package list would save the search.
- **.NET 10 (round 6):** unchanged. socialgoal (AutoMapper 3.1) fails the same way, and the repro on .NET 10.0.14
  fails with AutoMapper 4.2.0 and 11.0.1 and works with 12.0.1.
- **umbraco-7** (AutoMapper 3.3.1, static API, not upgraded): Umbraco configures its maps in its boot
  (`CoreBootManager.InitializeModelMappers` → `Mapper.Initialize`), and the first one,
  `CodeFileDisplayMapper.ConfigureMappings` line 41, throws the 3.x error, so `Application_Start` fails and every request
  is a 500. The repro fails with 3.3.1 on .NET 10.0.14 too. Upgrading means rewriting Umbraco's mapping layer (about 40
  mapper classes), so the app was blocked here.
- **Fixed for umbraco-7 by webForCore-thirdparty 0.6.0** (round 7, webForCore 0.8.0): its `webForCore.AutoMapperLegacy`
  (AutoMapper 3.x's static API on AutoMapper 12.0.1) replaces AutoMapper 3.3.1 in Umbraco.Core, Umbraco.Web and
  umbraco.businesslogic with the steps its README gives (the package, its global usings, one more alias for Umbraco's
  own `MapperConfiguration`, C# 14, `AutoMapperLegacy.ITypeConverter<,>` in the 2 classes that implement it). The
  rest of the 90 files that use AutoMapper compile unchanged, the boot configures every map, and the app installs
  and passes 21/21 on Linux and Windows (7/14 before). One mapping had to change, for F104 (an AutoMapper 12 difference the
  package passes through). socialgoal and realocate aren't moved to it here.
- **nopcommerce-3.90** (AutoMapper 5.2.0, instance API: `MapperConfiguration`, `IMapper`): `Method System.Linq.Enumerable.MaxInteger: type argument 'System.String' violates the constraint of type parameter 'T'` on every page; upgraded to 12.0.1, which compiles unchanged (round 7).

## F23

**`recaptcha` 1.0.5's lib folders aren't framework names**, so NuGet won't restore it for net11.0. App/environment.
Found on 0.2.0.

- **Apps:** roadkill.
- **Error:** `NU1202: Package recaptcha 1.0.5 is not compatible with net11.0`. The package has `lib/.NetFramework 2.0/`
  and `lib/.NetFramework 4.0/`.
- **What worked:** restore it for its path only and reference the DLL, as for F6:
  ```xml
  <PackageReference Include="recaptcha" Version="1.0.5.0" GeneratePathProperty="true" ExcludeAssets="all" />
  <Reference Include="Recaptcha" HintPath="$(Pkgrecaptcha)/lib/.NetFramework 4.0/Recaptcha.dll" />
  ```

## F24

**A transitive Microsoft.AspNet.Razor/WebPages replaces webForCore's in the output with no build error;** every view
then fails. webForCore bug (F5's documented step is the workaround). Raised as [#23](https://github.com/xforcore/webForCore/issues/23). Found on 0.2.0.

- **Apps:** bonobo-git-server (Unity.Mvc 5.11 → Microsoft.AspNet.Mvc 5.2.7), applicant-tracking (Unity.Mvc 5.0.13 →
  5.2.3), realocate (Autofac.Mvc5 3.3.4 → 5.1.0), roadkill (FixedDisplayModes and Swashbuckle → MVC 5.0, Web API 4.0).
- **Error:** the build succeeds, because the app's code compiles against webForCore's `System.Web.Mvc`. But the
  transitive package's `System.Web.Razor.dll` (3.x, 2018) and `System.Web.WebPages*.dll` are copied to `bin_dotnet/`
  over webForCore's, and every Razor view fails:
  ```
  MissingMethodException: Method not found: 'System.CodeDom.CodeTypeDeclaration
  System.Web.Razor.Generator.CodeGeneratorContext.get_GeneratedClass()'.
     at System.Web.Mvc.Razor.MvcCSharpRazorCodeGenerator.SetBaseType(String modelTypeName)
  ```
  F5 is about the build-time CS0433 this causes when the app's code uses the types. This is the same cause with no
  build error, so nothing tells the developer.
- **Repro:** an MVC site (one controller, one Razor view) with `<PackageReference Include="Unity.Mvc" Version="5.11.1" />`.
  The build passes and the view is a 500 with the error above.
- **Suggested fix:** webForCore's targets drop the compile and runtime assets of the Microsoft packages it replaces
  (`Microsoft.AspNet.Mvc`, `.Razor`, `.WebPages`, `.WebApi.*`, `.Web.Optimization`, `Microsoft.Web.Infrastructure`, the
  Katana packages it lifts, ...) when they arrive transitively, as 0.2.0 already does for the framework `HttpUtility`
  (F8). That would make F5's manual step unnecessary too.

## F25

**StructureMap 4.0's .NET Framework build needs `AppDomainSetup.PrivateBinPath`.** App/environment. Found on the
packages of xforcore/webForCore#24 (it was behind F19 and F20).

- **Apps:** roadkill (`structuremap` 4.0.0.315, through `StructureMap.MVC5` and `structuremap.web`).
- **Error:** every request, from Roadkill's WebActivatorEx start method:
  ```
  MissingMethodException: Method not found: 'System.String System.AppDomainSetup.get_PrivateBinPath()'.
     at StructureMap.Graph.AssemblyFinder.FindBinPath()
     at StructureMap.Graph.AssemblyScannerExtensions.AssembliesFromApplicationBaseDirectory(IAssemblyScanner scanner, ...)
  ```
  .NET's `AppDomainSetup` has no `PrivateBinPath`. The `lib/net40` StructureMap is built for .NET Framework.
- **What worked (round 3):** `structuremap` 4.0.0.315 → 4.7.1, which has a .NET Standard 2.0 build (no code change).
  The next library had the same kind of problem: NLog 4.2.0's `LogManager` needs `AppDomainSetup.ConfigurationFile`
  (`TypeInitializationException`); NLog 4.7.15 has a .NET Standard build. Both are what `docs/MIGRATING.md` section 3
  already says ("preferring versions that target .NET Standard or .NET"), so no guide gap.

## F26

**OpenOrderFramework's Identity seed deadlocks on a fresh database.** App/environment. Found on 0.4.0 (in round 2 it
looked like a slow first request).

- **Apps:** open-order-framework.
- **Symptom:** against an empty database every request times out. The database is created, and the next start of the
  app works: its initializer finds the model up to date and doesn't seed.
- **Cause** (managed stacks from `dotnet-stack`; SQL Server showed no blocked sessions):
  - The layout's `ShoppingCart.GetCount()` starts EF 6's database initialization for `ApplicationDbContext` and holds
    its lock (`LazyInternalContext.InitializeDatabaseAction` → `RetryAction.PerformAction`).
  - `ApplicationDbInitializer.Seed` → `InitializeIdentityForEF` uses the OWIN context's `RoleManager`, which has
    another `ApplicationDbContext`.
  - `RoleManager.FindByName` is Identity's `AsyncHelper.RunSync`, which runs `FindByNameAsync` on a thread-pool thread.
    That thread waits for the same initialization lock (`Monitor.Enter`), held by the request thread, which waits for it.
- **Why not webForCore:** none of it is webForCore code, and EF 6 and Identity run the same logic on .NET Framework. Not
  confirmed on .NET Framework (none here). The app's own workflow creates the database with `Update-Database`
  (Migrations), which doesn't run this seed. The smoke test creates it on the first run, so a run against a fresh
  database fails and the next passes.

## F27

**LightSpeed, Roadkill's ORM, needs `AppDomain.DefineDynamicAssembly`.** App/environment. Found on 0.4.0 (it was behind
F19, F20 and F25).

- **Apps:** roadkill.
- **Error:** every page:
  ```
  MissingMethodException: Method not found: 'System.Reflection.Emit.AssemblyBuilder
  System.AppDomain.DefineDynamicAssembly(System.Reflection.AssemblyName, System.Reflection.Emit.AssemblyBuilderAccess)'.
  [TypeInitializationException: The type initializer for 'ActiveSharp.PropertyMapping.OffsetUtil' threw an exception.]
     at Mindscape.LightSpeed.Entity.GetInferredFieldName(TField&)
  ```
- **Why it stops there:** LightSpeed (Mindscape, 2012) is closed source, checked into the app's `lib/` as .NET
  Framework DLLs, and is Roadkill's data layer for every SQL database; the only other backend is MongoDB, on an old
  driver. .NET has `AssemblyBuilder.DefineDynamicAssembly` instead, but the DLLs can't be rebuilt.

## F28

**A `configSource` in a subfolder can't be loaded on Linux.** webForCore bug. Raised as [#26](https://github.com/xforcore/webForCore/issues/26). Found on 0.4.0.

- **Apps:** mvcforum (ImageProcessor's `<security configSource="Config\imageprocessor\security.config"/>`, and `cache`,
  `processing`). Splitting sections into a `Config\` folder is common (ImageProcessor, Umbraco, connection strings and
  app settings in their own files).
- **Error:** every request: `Configuration Error. Unable to open configSource file 'Config\imageprocessor\processing.config'.`
- **Cause:** `InternalConfigHost.StaticGetStreamNameForConfigSource` (the System.Configuration port) does
  `Path.Combine(dirStream, configSource)`. On Linux `\` isn't a directory separator, so the result names a file called
  `Config\imageprocessor\processing.config` in the site folder. Writing the path with `/` doesn't help: System.Configuration
  rejects it, as on .NET Framework (`The configSource attribute must be a relative physical path, so the '/' character is
  not allowed`). So on Linux no `configSource` file can be in a subfolder. The client host already does
  `configSource.Replace('\\', '/')` (`ClientConfigurationHost.cs:355`); the web host doesn't.
- **Repro:** a site whose web.config has `<appSettings configSource="Config\app.config" />` and a `Config/app.config`
  with `<appSettings><add key="k" value="v"/></appSettings>`; any request is a Configuration Error.
- **Suggested fix:** in `StaticGetStreamNameForConfigSource`, treat `\` as the directory separator when combining on a
  non-Windows OS (the validation that rejects `/` stays, as on .NET Framework).

## F29

**`relaxedUrlToFileSystemMapping="true"` makes every `MapPath` return `NOT_A_VALID_FILESYSTEM_PATH` on Linux.**
webForCore bug. Raised as [#27](https://github.com/xforcore/webForCore/issues/27). Found on 0.4.0.

- **Apps:** mvcforum (`<httpRuntime relaxedUrlToFileSystemMapping="true" ...>`, common in apps that allow special
  characters in URLs).
- **Error:** `HostingEnvironment.MapPath("~/App_Data/forum.config")` returns `<site>/NOT_A_VALID_FILESYSTEM_PATH`, so
  `Could not find file '<site>/NOT_A_VALID_FILESYSTEM_PATH'` from the app's `XmlDocument.Load`, in its EF seed (every
  request).
- **Cause:** with the setting on, `HttpRuntime.GetRelaxedMapPathResult` replaces a mapped path that contains any of
  `s_InvalidPhysicalPathChars` with that placeholder. The list, from .NET Framework, is
  `{ '/', '?', '*', '<', '>', '|', '"' }`. On Windows a physical path has no `/`; on Linux every physical path does.
- **Repro:** a site with `<httpRuntime relaxedUrlToFileSystemMapping="true" />` and a page that writes
  `Server.MapPath("~/x.txt")`: it prints `<site>/NOT_A_VALID_FILESYSTEM_PATH`.
- **Suggested fix:** leave `/` out of `s_InvalidPhysicalPathChars` when the directory separator is `/`.

## F30

**`webForCore.Web.Mvc` doesn't bring System.Web.Helpers, which Microsoft.AspNet.Mvc did.** webForCore bug (packaging).
Found on 0.4.0.

- **Apps:** mvcforum (web.config `<add assembly="System.Web.Helpers, Version=3.0.0.0, ..."/>`).
- **Error:** `Configuration Error. Could not load file or assembly 'System.Web.Helpers, Version=3.0.0.0'`.
- **Cause:** on .NET Framework, `Microsoft.AspNet.Mvc` depends on `Microsoft.AspNet.WebPages`, which contains
  `System.Web.Helpers.dll`, so every MVC 5 app has it (WebGrid, Chart, Crypto, Json). webForCore has it as a separate
  package, `webForCore.Web.Helpers`, which `webForCore.Web.Mvc` doesn't depend on; `docs/MIGRATING.md` section 3 maps
  packages one to one and doesn't say an MVC app needs it.
- **Repro:** an MVC site referencing `webForCore` and `webForCore.Web.Mvc`, with System.Web.Helpers in web.config's
  `<compilation><assemblies>` (or a view using `@using System.Web.Helpers` / `WebGrid`).
- **What worked:** `<PackageReference Include="webForCore.Web.Helpers" Version="0.4.0" />`.
- **Suggested fix:** make `webForCore.Web.Mvc` depend on `webForCore.Web.Helpers`, as Microsoft.AspNet.Mvc's graph does.

## F31

**System.Web.ApplicationServices is visible to every project, so a library's own `MembershipUser` becomes ambiguous.**
Guide gap. Raised as [#29](https://github.com/xforcore/webForCore/issues/29). Found on 0.4.0.

- **Apps:** mvcforum (`MVCForum.Core` defines `MembershipUser` and `MembershipCreateStatus` and has
  `using System.Web.Security;`).
- **Error:** 35 × `CS0104: 'MembershipUser' is an ambiguous reference between 'MvcForum.Core.Models.Entities.MembershipUser'
  and 'System.Web.Security.MembershipUser'`.
- **Cause:** on .NET Framework the library compiled against `System.Web` only (here through the `SystemWebPackage`
  package; a plain `<Reference Include="System.Web" />` is the same). `System.Web` forwards the membership types
  (`MembershipUser`, `MembershipCreateStatus`, `MembershipProvider`, `Roles`, ...) to `System.Web.ApplicationServices`,
  and forwarded types aren't visible without that reference. The `webForCore` package brings
  `System.Web.ApplicationServices` to every project.
- **What worked:** in that library,
  `<PackageReference Include="webForCore.Web.ApplicationServices" Version="0.4.0" ExcludeAssets="compile" />`.
- **Suggested fix:** say this in `docs/MIGRATING.md`, or have the targets leave `System.Web.ApplicationServices` out of
  a project's compile references unless it asks for it, as on .NET Framework.

## F32

**More versions webForCore requires: Humanizer.Core 2.14.1 and the System.* packages at 11.0.** Guide gap (extends
F7, #6). Raised as [#30](https://github.com/xforcore/webForCore/issues/30). Found on 0.4.0.

- **Apps:** aspnet-mvc5-starter (`Humanizer.Core` 2.6.2), mvcforum (`System.Configuration.ConfigurationManager` and
  `System.Runtime.Caching` 4.5.0-preview1).
- **Error:** `NU1605: Detected package downgrade: Humanizer.Core from 2.14.1 to 2.6.2 ... webForCore 0.4.0 ->
  webForCore.Compilers 0.4.0 -> Microsoft.CodeAnalysis 5.9.0 -> Humanizer.Core (>= 2.14.1)`, and the same for
  `System.Runtime.Caching` and `System.Configuration.ConfigurationManager` from 11.0.0-rc.3.
- **What the guide says:** `docs/MIGRATING.md`'s "Versions webForCore requires" lists Newtonsoft.Json 13.0.4,
  Microsoft.IdentityModel.Tokens 8.19.1 and EntityFramework 6.4+.
- **What worked:** Humanizer.Core raised to 2.14.1; the two System.* references removed (webForCore brings them).
  `scripts/convert.cs` now does both.

## F33

**Some packages' .NET Standard builds have less than their `net45` builds, and NuGet picks the .NET Standard one.**
Guide gap. Raised as [#31](https://github.com/xforcore/webForCore/issues/31). Found on 0.4.0.

- **Apps:**
  - aspnet-mvc5-starter: WindowsAzure.Storage 9.3.3's `netstandard1.3` build has no synchronous blob methods (`Exists`,
    `OpenRead`, `UploadFromStream`, `GetPermissions`, ...): CS1061.
  - mvcforum: Hangfire.Core 1.6.17's `netstandard1.3` build has no OWIN extensions (`app.UseHangfireServer`): CS1929.
  - mvcforum: Hangfire.SqlServer 1.6.17's can't resolve a connection string name from web.config, so
    `UseSqlServerStorage("MVCForumContext")` throws `Format of the initialization string does not conform to
    specification` at start-up.
- **What the guide says:** `docs/MIGRATING.md` section 3: "Keep other NuGet packages, preferring versions that target
  .NET Standard or .NET". Here the same version's .NET Standard build is the problem.
- **What worked:** restore the package for its path only and reference its `net45` DLL (as for F6), in every project
  that gets it, the web project included:
  ```xml
  <PackageReference Include="Hangfire.Core" Version="1.6.17" GeneratePathProperty="true" ExcludeAssets="compile;runtime" />
  <Reference Include="Hangfire.Core" HintPath="$(PkgHangfire_Core)/lib/net45/Hangfire.Core.dll" />
  ```
- **Suggested fix:** a bullet in `docs/MIGRATING.md`'s "Packages that need more than a reference".

## F34

**Build-time tools that don't run on Linux or the .NET 11 SDK.** App/environment. Found on 0.4.0.

- **Apps:**
  - mvcforum: BuildWebCompiler 1.11 (Sass/LESS at build time) starts `cmd.exe`. Removed; its output is checked in.
  - aspnet-mvc5-starter: Microsoft.Web.LibraryManager.Build 2.0.48's restore task fails to load its providers by
    reflection (`MSB4018 ... Dependencies.GetProvidersFromReflection`). LibMan.Build 3.0.114 works and restores the client
    libraries from cdnjs at build time.
- Also seen: MVCForum's plugins project copies its output into `App_Data/Plugins` in a post-build step; the folder is
  created empty instead.

## F35

**`WsiProfiles` and `SoapBindingUse` in app code are CS0433.** webForCore bug. Raised as
[#34](https://github.com/xforcore/webForCore/issues/34). Found on 0.4.0. Fixed in #32.

- **Apps:**
  - mojoportal: its `.ashx` handlers, from Visual Studio's generic handler template
    (`[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]`).
  - subtext: its Web Reference proxy (`SoapBindingUse.Literal`).
- **Error:** `error CS0433: The type 'WsiProfiles' exists in both 'System.Web.Services.Description, Version=8.1.2.0, ...'
  and 'System.Web.Services, Version=11.0.0.0, ...'`.
- **Cause:** webForCore's System.Web.Services has the `System.Web.Services.Description` namespace, as on .NET Framework.
  CoreWCF, which System.Web.Extensions' WCF client support uses, depends on the System.Web.Services.Description
  package. That package's compile reference defines the same types.
- **Repro:** a site referencing `webForCore` and `webForCore.Web.Services`, with a class in the project marked
  `[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]`.
- **Fix (#32):** `webForCore.WebStack.targets` drops the package's compile reference wherever webForCore's
  System.Web.Services is referenced. The package smoke test compiles such a handler. Both apps build on the fix
  branch's packages.

## F36

**A `.resx` with non-string resources needs System.Resources.Extensions.** Guide gap. Raised as
[#36](https://github.com/xforcore/webForCore/issues/36). Found on 0.4.0.

- **Apps:** subtext (`Subtext.Web`'s resources include images).
- **Error:** `error MSB3822: Non-string resources require the System.Resources.Extensions assembly at runtime, but it
  was not found in this project's references.`
- **Cause:** SDK-style projects on .NET embed non-string resources pre-serialized, which needs the
  System.Resources.Extensions package. `docs/MIGRATING.md` doesn't mention it.
- **What worked:** `<GenerateResourceUsePreserializedResources>true</GenerateResourceUsePreserializedResources>` and
  `<PackageReference Include="System.Resources.Extensions" Version="11.0.0-rc.3.26522.148" />`.

## F37

**web.config's `<compilation><assemblies>` lists .NET Framework assemblies that .NET doesn't have.** Guide gap. Raised
as [#37](https://github.com/xforcore/webForCore/issues/37). Found on 0.4.0.

- **Apps:** subtext (`System.Design, Version=4.0.0.0`), mojoportal (`System.Data.Linq, Version=4.0.0.0`), umbraco-7
  (round 6: `System.Runtime.InteropServices.WindowsRuntime`, `System.ServiceModel.Http` and `.NetTcp`, from the list of
  .NET 4.5 facades its web.config adds; the guide's step, documented since 0.5.0, covered it).
- **Error:** every page: `Configuration Error ... Could not load file or assembly 'System.Design, Version=4.0.0.0,
  Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. The system cannot find the file specified.`
- **Cause:** `docs/NAMESPACES.md` lists both assemblies as not on .NET. The guide doesn't say an `<add assembly>` entry
  for one is a Configuration Error even when no page uses it. The apps' pages don't use either.
- **What worked:** removing the entries.
- **0.9.0:** System.Data.Linq is provided (`webForCore.Data.Linq`, webForCore#142), and both 3.5.0.0 and 4.0.0.0 entries
  resolve: mojoportal's overlay puts its `<add assembly="System.Data.Linq, ...">` back, and it passes 49/49 on Linux.
  System.Design is still not on .NET, so Subtext's entry stays removed.

## F38

**Types that a shared-framework facade forwards to one of the app's assemblies are a second copy.** webForCore bug.
Raised as [#35](https://github.com/xforcore/webForCore/issues/35). Found on 0.4.0. Fixed in #32.

- **Apps:** subtext. `Microsoft.ApplicationBlocks.Data.dll`, the checked-in .NET Framework 1.1 data access block,
  references `SqlDataReader` in `System.Data, Version=1.0.5000.0`.
- **Error:** every request: `MissingMethodException: Method not found: 'System.Data.SqlClient.SqlDataReader
  Microsoft.ApplicationBlocks.Data.SqlHelper.ExecuteReader(System.String, System.Data.CommandType, System.String)'`.
  The same call works in a plain .NET 11 console app.
- **Cause:** this is the PseudoAppDomain's problem from #1 and #19. On .NET, `System.Data.dll` is a shared-framework
  facade that forwards the SqlClient types to `System.Data.SqlClient`, which the app deploys (package 4.9.1). The facade
  is loaded in the default context, so its forwards resolve there, to a second `System.Data.SqlClient`. The app's own
  code gets the domain's copy. There are more such facades:
  - System.Data: SqlClient, OleDb and Odbc;
  - System.Drawing: System.Drawing.Common;
  - System.ServiceModel.Web: syndication;
  - System.Core and System: PerformanceCounter;
  - others.

  A scan of the shared framework's assemblies finds 29 forwards to assemblies outside it.
- **Repro:** in a site referencing `webForCore`,
  `Type.GetType("System.Data.SqlClient.SqlDataReader, System.Data, Version=4.0.0.0, Culture=neutral,
  PublicKeyToken=b77a5c561934e089") == typeof(System.Data.SqlClient.SqlDataReader)` is `false`.
- **Fix (#32):** the PseudoAppDomain loads into the domain any shared-framework assembly whose forwards go to an
  assembly the app deploys. Forwards to the shared framework still resolve to the shared copy. With the fix, Subtext's
  install wizard, home page, posts, archives, contact form, RSS and MVC routes work: 15 of 16 probes pass; the log-in
  page is F44.

## F39

**NHibernate 3.3 doesn't run on .NET.** App/environment. Found on 0.4.0.

- **Apps:** funnelweb (NHibernate 3.3.1, FluentNHibernate 1.3).
- **Errors:**
  - `The following types may not be used as proxies: FunnelWeb.Model.Authentication.Role: method MemberwiseClone
    should be 'public/protected virtual' or 'protected internal virtual'`, for every entity. On .NET,
    `Object.MemberwiseClone` is `protected internal`. On .NET Framework it is `protected`.
  - With the proxy check off, `NHibernate.AdoNet.SqlClientSqlCommandSet..ctor` throws `ArgumentNullException`. It finds
    the internal `System.Data.SqlClient.SqlCommandSet` by reflection in System.Data, which on .NET is a facade.
- **What would work:** NHibernate 5.1+ (it runs on .NET) and FluentNHibernate 3, an upgrade of the app's data layer.
  With webForCore 0.4.0, FunnelWeb's installer (the Admin area: log-in, connection test, 32 upgrade scripts) works.

## F40

**`Assembly.Load("App_GlobalResources")` can't work on .NET.** webForCore bug. Raised as
[#33](https://github.com/xforcore/webForCore/issues/33). Found on 0.4.0. Fixed in 0.8.0: mojoportal passes 16/16 on Linux and Windows.

- **Apps:** mojoportal. Its `App_GlobalResources/*.Designer.cs`, Visual Studio's generated code, does
  `new ResourceManager("Resources.Resource", Assembly.Load("App_GlobalResources"))`.
- **Error:** in `Application_Start` and every page: `Could not load file or assembly 'App_GlobalResources,
  Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.`
- **Cause:** ASP.NET compiles App_Code and App_GlobalResources to assemblies with random names
  (`App_GlobalResources.abcd1234`). It answers `AssemblyResolve` for the fixed name from `_assemblyResolveMapping`
  (VSWhidbey 276776). On .NET, the event gets the display name (`App_GlobalResources, Culture=neutral,
  PublicKeyToken=null`), so the mapping isn't found. Matching the simple name doesn't help either: .NET refuses an
  assembly whose simple name differs from the one requested ("Resolved assembly's simple name must be the same as of
  the requested assembly"). In the PseudoAppDomain the runtime reports "Resolving to a collectible assembly is not
  supported". The first error reproduces without webForCore: in an `AssemblyLoadContext`, a `Resolving` handler
  that answers `Assembly.Load("Target")` with an assembly named `Target.x1` gets it.
- **Workaround that shows it:** `<compilation optimizeCompilations="true">` gives top-level assemblies their fixed
  names (`BuildManager.GenerateRandomAssemblyName`). With it, mojoPortal's Setup installs its schema and site on SQL
  Server, and the home page, log-in, register, MVC file manager, Web API (JSON 401), handlers and static files all
  work. Not in the overlay.
- **Suggested fix:** on .NET, compile App_Code and App_GlobalResources to their fixed simple names (each domain is its
  own load context). They would go in a per-build folder, so a domain that is still unloading keeps its file.

## F41

**WebActivator 1.x has the same problem as WebActivatorEx (#21).** Guide gap. Raised as
[#38](https://github.com/xforcore/webForCore/issues/38). Found on 0.4.0.

- **Apps:** subtext. Its Ninject bootstrapper, `App_Start/NinjectMVC3.cs`, uses
  `[assembly: WebActivator.PreApplicationStartMethod(...)]`. It is WebActivator 1.5, the package before WebActivatorEx
  2.0; NuGet's MVC 3 era packages (Ninject.MVC3, ...) bring it.
- **Cause:** as #21: its start methods are never run, so the container isn't set up. `docs/MIGRATING.md` and
  webForCore-thirdparty cover WebActivatorEx only.
- **What worked:** `webForCore.WebActivator` 0.4.1, a build of WebActivator 1.5 for webForCore, added to
  webForCore-thirdparty in [xforcore/webForCore-thirdparty#1](https://github.com/xforcore/webForCore-thirdparty/pull/1).
  Its smoke test fails without the package and passes with it, on webForCore 0.3.0 and 0.4.0. Calling
  `WebActivator.ActivationManager`'s methods from `Application_Start` is the workaround without it.

## F42

**SDK-style project references are transitive.** Guide gap. Raised as
[#39](https://github.com/xforcore/webForCore/issues/39). Found on 0.4.0.

- **Apps:**
  - funnelweb: the web project pins `Iesi.Collections` 3.2.0.4000 in packages.config, and the `FunnelWeb` library pins
    3.3.2.4000. Restore fails with `NU1605: Detected package downgrade: Iesi.Collections from 3.3.2.4000 to 3.2.0.4000.
    FunnelWeb.Web -> FunnelWeb -> Iesi.Collections (>= 3.3.2.4000)`.
  - mojoportal: `mojoPortal.Features.UI` references `mojoPortal.Features.Business`, which references the data layer
    `mojoPortal.Features.Data.MSSQL`. In Features.UI the data layer's namespace `mojoPortal.MediaPlayer` then hides the
    `MediaPlayer` class: `error CS0118: 'MediaPlayer' is a namespace but is used like a type`.
- **Cause:** on .NET Framework a project saw only the projects and packages it referenced directly. In SDK-style
  projects, references flow to the projects that reference them.
- **What worked:** raising the web project's version to the library's (FunnelWeb), and `PrivateAssets="all"` on
  the data layer's `ProjectReference` (mojoPortal).

## F43

**Projects that deploy themselves into the site with Windows `xcopy` post-build events.** App/environment. Found on
0.4.0.

- **Apps:** mojoportal. `mojoPortal.Features.UI` (blog, forums, galleries, ...) is a separate web project. Its
  post-build event copies its assemblies, `.aspx`/`.ascx`/`.ashx` files, resources and setup scripts into `Web`, with
  62 `xcopy` lines. Without them the default theme's skin fails (`Could not load file or assembly
  'mojoPortal.Features.UI'`).
- **What worked:** the same copies as an MSBuild target (`Web\bin` becomes `bin_dotnet`), and `also_build` in
  app.toml to build the feature projects after the site.

## F44

**DotNetOpenAuth 3.x reflects on .NET Framework's private `UriParser` fields.** App/environment. Found on 0.4.0.

- **Apps:** subtext (its log-in page has DotNetOpenAuth 3.4's `OpenIdLogin` control).
- **Error:** `TypeInitializationException: The type initializer for 'DotNetOpenAuth.OpenId.UriIdentifier' threw an
  exception`, from a `NullReferenceException` in `UriIdentifier.NonPathCompressingUriParser.Initialize`.
- **Cause:** DotNetOpenAuth sets private fields of `UriParser` by reflection, and they don't exist on .NET.
  DotNetOpenAuth is unmaintained and has no .NET build. OpenID 2.0 itself is obsolete.

## F45

**Every package webForCore depends on is a floor for the application's reference to it.** Guide gap (extends F7 and F32).
Raised as [#42](https://github.com/xforcore/webForCore/issues/42). Found on 0.5.0. Documented in #44.

- **Apps:** yafnet-2 (ServiceStack.OrmLite.SqlServer has System.Data.SqlClient 4.8.1), opserver-mvc5 (Opserver.Core has
  System.DirectoryServices 4.5.0).
- **Error:** `NU1605: Detected package downgrade: System.Data.SqlClient from 4.9.1 to 4.8.1 ... webForCore 0.5.0 ->
  webForCore.Web.Extensions 0.5.0 -> System.Data.SqlClient (>= 4.9.1)`; the same for System.DirectoryServices from
  `webForCore.Web.Services` (>= 11.0.0-rc.3).
- **What worked:** raising the references. `scripts/convert.cs` does it now.

## F46

**webForCore's SignalR and System.Web.DataVisualization assemblies have version 0.0.0.0.** webForCore bug. Raised as
[#40](https://github.com/xforcore/webForCore/issues/40). Found on 0.5.0. Fixed in #44.

- **Apps:** jabbr. It keeps Microsoft's SignalR SQL Server and Service Bus scale-out packages, which webForCore doesn't
  lift.
- **Error:** `CS1705: Assembly 'Microsoft.AspNet.SignalR.SqlServer' ... uses 'Microsoft.AspNet.SignalR.Core,
  Version=2.4.3.0' which has a higher version than referenced assembly 'Microsoft.AspNet.SignalR.Core' with identity
  '..., Version=0.0.0.0'`. At run time it wouldn't bind either.
- **Cause:**
  - SignalR.Core, SystemWeb and Client: KoreBuild generated SignalR's version attributes, and the lift turns off the
    SDK's generation.
  - System.Web.DataVisualization: Microsoft's `AssemblyInfo.cs` applies the version only `#if VS_BUILD`.
  - Every other replaced assembly has Microsoft's version or higher (checked across the 0.5.0 packages).
- **Fix (#44):**
  - `lift_aspnet.py` stamps SignalR 2.4.3.0;
  - a webForCore file stamps DataVisualization 4.0.0.0;
  - `tools/ci/package-smoke-mvc` compiles a call into Microsoft's SQL Server scale-out and checks the versions. It fails
    on 0.5.0 with CS1705.
  - With it JabbR builds; it then stops at F50.

## F47

**`GetTypes()` on System.Data.SqlClient 4.9.1 throws on .NET 11.** webForCore bug (its dependency). Raised as
[#41](https://github.com/xforcore/webForCore/issues/41). Found on 0.5.0. Fixed in 0.8.0, which builds System.Data.SqlClient from Microsoft's source with `GetTypes()` fixed: opserver-mvc5 passes 11/11 on Linux and Windows.

- **Apps:** opserver-mvc5. It builds its tabs by scanning every loaded assembly's types. Any app that scans all
  assemblies is affected: DI registration, AutoMapper profiles, MEF, plugin loaders.
- **Error:** `ReflectionTypeLoadException ... Could not load type 'SqlGuidCaster' from assembly 'System.Data.SqlClient,
  Version=4.6.1.6' because it contains an object field at offset 0 that is incorrectly aligned or overlapped by a
  non-object field`.
- **Cause:** the package's Unix build has a type .NET 11's runtime refuses to load.
  - It reproduces in a .NET 11 console app with no webForCore.
  - Guid columns read and write fine.
  - webForCore depends on the package (as on .NET Framework), so every webForCore app has it.
- **What showed it:** with Opserver's scan catching the exception (exploration only), the app works, its SQL Server
  dashboard included.
- **.NET 10 (round 6):** unchanged. opserver-mvc5 fails the same probe, and `GetTypes()` throws the same
  `SqlGuidCaster` error in a .NET 10.0.14 console app.

## F48

**The C# version code was written for.** Guide gap. Raised as [#43](https://github.com/xforcore/webForCore/issues/43).
Found on 0.5.0. Documented in #44.

- **Apps:** yafnet-2 (its vendored ServiceStack.Text).
- **Errors:**
  - net11.0 defaults to the newest C#, and C# 14's first-class span conversions give
    `CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<char>' to 'string'`, CS1503 and CS1929.
  - C# 11's ref-safety rules, which apply on .NET 7+ whatever the language version, give
    `CS8347 ... may expose variables referenced by parameter 'index' outside of their declaration scope` and CS8168.
- **What worked:** `<LangVersion>` pinned (7.3 if the project set none, 13 for "latest") fixes the first.
  `scripts/convert.cs` does it for SDK-style projects. The second needs `scoped` in the source.

## F49

**YAF.NET's vendored ServiceStack 5.9 fork only works as .NET Framework code.** App/environment. Found on 0.5.0.

- **Apps:** yafnet-2.
- **Cause:** ServiceStack picks its platform with `#if NET472` / `NETCORE2_1` / `NETSTANDARD`.
  - The `NET472` path YAF.NET ran needs `AppDomain.DefineDynamicAssembly` (`PclExport.Net45.cs:964`), as Roadkill's
    LightSpeed does (F27).
  - The .NET Standard and .NET Core paths call `LicenseUtils`, which YAF.NET removed from its fork.
  - On net11.0 neither symbol is defined: `CS0246: The type or namespace name 'NetStandardPclExport' could not be found`.

## F50

**Nancy 0.20 needs `AppDomainSetup.PrivateBinPathProbe`.** App/environment. Found on 0.5.0.

- **Apps:** jabbr (pages are Nancy 0.20 modules).
- **Error:** at start-up, `MissingMethodException: Method not found: 'System.String
  System.AppDomainSetup.get_PrivateBinPathProbe()'`, from `Nancy.Bootstrapper.AppDomainAssemblyTypeScanner`.
- **Cause:** Nancy 0.20 is .NET Framework only. Nancy 2.x has a .NET Standard build, but moving JabbR's modules to its API
  is a port, not a migration. Ninject 3.0 had the same kind of problem (`AppDomain.Evidence`); 3.3.6 fixed it.

## F51

**A change while the next start is starting leaves the application dead.** webForCore bug. Raised as
[#49](https://github.com/xforcore/webForCore/issues/49). Found on 0.6.0, on Linux and Windows.

- **Apps:** bonobo-git-server and mojoportal on Windows, where the app itself writes into `bin_dotnet` (F52, F3); any app
  when files keep changing during a restart (a deployment copying a build).
- **Error:** every request gets `503 Server Too Busy` (`HttpRuntime`'s `_disposingHttpRuntime` check) until the
  process restarts. The log has `(start 2) is unloading` before `Started application ... (start 2 ...)`, and no
  start 3. On IIS the application restarts again and recovers.
- **Repro:** MvcMovie; while its first request runs, change a file in `bin_dotnet` (Windows) or `Web.config` (Linux)
  every 100 ms for 3 s; later requests are all 503. A single change restarts it cleanly.
- **Cause (from the source):** the new start's HttpRuntime shuts down inside `AspNetCoreHost.CreateForMiddleware`;
  `WebFormsMiddleware` neither hears of it nor starts again, and `current ??= StartApplication()` keeps the dead start.

## F52

**AppDomain data `DataDirectory` isn't set.** webForCore bug. Raised as
[#50](https://github.com/xforcore/webForCore/issues/50). Found on 0.6.0 (and 0.5.0), on Linux and Windows.

- **Apps:** bonobo-git-server: `Data Source=|DataDirectory|Bonobo.Git.Server.db` (System.Data.SQLite) puts the database
  in `bin_dotnet/`, not `App_Data/`. On Windows that write is a change in `bin`, and the app then hits F51.
- **Repro:** `<%= AppDomain.CurrentDomain.GetData("DataDirectory") ?? "(null)" %>` in a page: `(null)`; on IIS
  `<site>\App_Data`.
- **Cause:** `HttpRuntime.SetUpDataDirectory` on .NET only calls `ApplicationManager.SetLoadContextData`, which only
  webForCore's own `SqlConnectionHelper` reads.

## F53

**Linux: a change in `bin` or `App_Code` doesn't restart the application.** webForCore bug. Raised as
[#51](https://github.com/xforcore/webForCore/issues/51). Found on 0.6.0 (and 0.5.0).

- **Apps:** any; checked with aspnetdocs-mvcmovie (a new `.dll` in `bin_dotnet`) and miniblog (a new `.cs` in
  `app_code`). `Web.config` restarts it; on Windows all three do. docs/IIS-COMPATIBILITY.md says they work.
- **Cause:** `HttpRuntime` starts the bin and critical-directory monitors only `if (OSInfo.IsWindows)`, a guard kept
  from WebFormsForCore's early Linux work; webForCore's log of the lift said file-change notifications were on everywhere on net11.0.

## F54

**.NET 10 prunes fewer framework packages than .NET 11, so restores fail with NU1605 package downgrades.** Guide
gap. Raised as [#62](https://github.com/xforcore/webForCore/issues/62). Found in round 6, on 0.7.0 (.NET 10), on
Linux and Windows.

- **Apps:**
  - mvcforum: `System.ComponentModel.Annotations from 5.0.0 to 4.4.1`.
  - yafnet-2: `Microsoft.Extensions.Primitives from 8.0.0 to 5.0.1` and `.Configuration.Abstractions from 8.0.0 to
    5.0.0`, in its Lucene.Net class libraries.
  - Both restored on 0.6.0 (.NET 11).
- **Cause:** the SDK prunes package references that the framework provides, as listed in the targeting packs'
  `PackageOverrides.txt`. The lists differ between the two versions:
  - .NET 11's Microsoft.NETCore.App covers System.ComponentModel.Annotations up to 5.0.0. .NET 10's covers it only up
    to 4.3.0, so MVCForum's 4.4.1 pin now conflicts with EntityFramework 6.5.1's `>= 5.0.0`.
  - .NET 11's Microsoft.NETCore.App also covers Microsoft.Extensions.\*. On .NET 10 they are only in
    Microsoft.AspNetCore.App. So a plain `Microsoft.NET.Sdk` class library that references `webForCore` gets their
    8.0.0 packages (through webForCore.Web.Extensions → CoreWCF.Http 1.9.1), and those conflict with its older pins.
  - webForCore.Web's nuspec declares the AspNetCore framework reference, but pruning only uses the project's own
    `FrameworkReference` items.
- **Repro:** net10.0 class libraries with `webForCore` 0.7.0:
  - adding `Microsoft.Extensions.Primitives` 5.0.1 fails;
  - adding `EntityFramework` 6.5.1 and `System.ComponentModel.Annotations` 4.4.1 fails;
  - the first with `<FrameworkReference Include="Microsoft.AspNetCore.App" />` added restores.
- **Fix in the app:** mvcforum references Annotations 5.0.0 and passes 15/15. That's the guide step the issue
  proposes. yafnet-2 stays blocked on F48 and F49 regardless.
- **Round 0.9.0:** MIGRATING.md has the step (#62, closed). yafnet-2's overlay adds
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to its 18 class libraries and its restore passes; its
  build then stops only at F49.

## F55

**webForCore-thirdparty 0.4.x targets only net11.0, and on .NET 10 it restores with no error and no assembly.**
App/environment; fixed in webForCore-thirdparty 0.5.0 (net10.0, built against webForCore 0.7.0). Found in round 6.

- **Apps:** applicant-tracking ran the original WebActivatorEx 2.2.0 (net40), whose start methods don't run (F20).
  Its probes don't depend on them, so it still passed. bonobo-git-server, mvcforum, roadkill and subtext reference
  the same packages and would have done the same.
- **Cause:** the packages' TFM-less `buildTransitive/` targets make NuGet treat them as compatible with net10.0, so
  there's no NU1202. Their `lib/net11.0` assembly is simply not used. The original package of the same assembly,
  which Unity.Mvc and similar packages bring, is then the only one in the build.
- **Now:** round 6 uses webForCore-thirdparty 0.5.0, and applicant-tracking's output has `lib/net10.0/WebActivatorEx.dll`.

## F56

**An app's repository is gone from GitHub, and a clone of it waited for credentials.** App/environment. Found in
round 6.

- **Apps:**
  - better-cms: `devbridge/BetterCMS`, not migrated yet. It is now rejected.
  - prodinner: `omuleanu/ProDinner` was already rejected for this. Its clone was still attempted, because the run
    named it.
- **What happens:** the page is 404 and the git endpoint answers 401. git then asks for credentials, through
  `SSH_ASKPASS` (a desktop dialog) where that's set, so the clone waited for hours. The runner now gives git no
  prompt of either kind, and has a stall timeout, so such a clone fails in seconds.

## F57

**`<trust level="Medium">` (any partial trust level) is a Configuration Error, though the docs say trust levels are
ignored.** webForCore bug. Raised as [#87](https://github.com/xforcore/webForCore/issues/87). Found on 0.7.0, on Linux.

- **Apps:** umbraco-6: its development `web.Template.config` has `<trust level="Medium" originUrl=".*"/>`. The release
  transform (`web.Template.Release.config`) removes it, and the migration builds `Web.config` with that transform, so the
  probes don't reach it.
- **Error:** every request: `Parser Error Message: Unable to read the security policy file for trust level 'Medium'.`
- **Docs:** `docs/NAMESPACES.md`: "Code Access Security, partial trust | Trust levels, CAS attributes | Ignored:
  everything runs in full trust".
- **Cause:** `HttpRuntime.SetTrustLevel` / `ApplicationManager.GetPartialTrustPolicyLevel` still look for the level's
  policy file (`web_mediumtrust.config`), which webForCore doesn't ship.
- **Repro:** the package `Program.cs`, any `Default.aspx`, and `<system.web><trust level="Medium" /></system.web>`.

## F58

**A `<system.codedom>` compiler naming `Microsoft.CSharp.CSharpCodeProvider` fails every page.** webForCore bug.
Raised as [#88](https://github.com/xforcore/webForCore/issues/88). Found on 0.7.0, on Linux.

- **Apps:** umbraco-6: `web.Template.config` has the .NET 3.5/4.0 template's entry, `Microsoft.CSharp.CSharpCodeProvider,
  System, Version=2.0.0.0` with `CompilerVersion` `v4.0`.
- **Error:** `InvalidCastException: Unable to cast object of type 'Microsoft.CSharp.CSharpCodeProvider' to type
  'WebFormsForCore.CodeDom.Compiler.CodeDomProvider'`, on every page that compiles.
- **Cause:** `CompilationUtil.CreateCodeDomProvider*` casts the configured type to webForCore's own `CodeDomProvider`.
- **Docs:** the guide says to remove the `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` entries only.
  `scripts/convert.cs` removes any `<system.codedom>`, which hides this in apps whose `web.config` is in the
  repository; Umbraco's is made from a template at build, so the migration removes it from the template.

## F59

**Recycling for an `App_Code` change: a request the old worker is still compiling fails with CS0006.** webForCore
bug. Raised as [#89](https://github.com/xforcore/webForCore/issues/89). Found on 0.7.0, on Linux, under `webforcore-host`.

- **Apps:** umbraco-6, in its install (3 times out of 3): the wizard's last step writes `web.config`; the next start
  (the first configured one) creates `~/App_Code` in `Application_Start`; the host recycles that worker
  (`CodeDirChangeOrDirectoryRename`) and starts the next while the first still compiles `/umbraco/login.aspx`.
- **Error:** that request: 500, `CS0006: Metadata file '/tmp/Temporary_ASP.NET_Files-1000/root/<hash>/App_global.asax.<x>.dll'
  could not be found`. The next request works.
- **Cause (most likely):** the new worker's start, finding the code directories changed, removes the shared
  compilation folder's assemblies that the old worker's compilation references. `docs/MIGRATING.md` section 8 says no
  request fails in a recycle.
- **In the app:** a `may_fail` request after the wizard's last step absorbs it, so setup's later steps work.

## F60

**Files without a BOM are read as UTF-8, not the ANSI code page as on .NET Framework.** webForCore bug (or a
difference to document). Raised as [#90](https://github.com/xforcore/webForCore/issues/90). Found on 0.7.0; it's the
same on Windows (`Encoding.Default` is UTF-8 on .NET everywhere).

- **Apps:** umbraco-6: `install/steps/theend.ascx` is Windows-1252 (`You’re`, byte `0x92`), and renders as `You�re done`.
- **Cause:** with no `<globalization fileEncoding>`, `GlobalizationSection.FileEncoding` is `Encoding.Default`: the
  ANSI code page on .NET Framework, UTF-8 on .NET.
- **Repro:** a Windows-1252 `Default.aspx` without a BOM containing `’` or `é`.
- **Probes:** none depend on it (the setup step checks `done...now what?`).

## F61

**Code or configuration that loads assemblies from `~/bin` must follow `bin_dotnet`.** Guide gap. Raised as
[#91](https://github.com/xforcore/webForCore/issues/91). Found on 0.7.0, on Linux.

- **Apps:** umbraco-6: the back office's create dialogs load their task classes with
  `Assembly.LoadFrom(MapPath("~/bin/umbraco.dll"))`, so creating a document type failed with `FileNotFoundException:
  Could not load file or assembly '<site>/bin/umbraco.dll'`.
- **Fix in the app:** Umbraco's own setting, `<add key="umbracoBinDirectory" value="~/bin_dotnet" />`.
- **umbraco-7:** the same code (`LegacyDialogHandler.cs`) and the same setting, applied; not reached, as the app
  doesn't boot (F22).
- **Guide:** section 2 makes the output `bin_dotnet`, but nothing says that `~/bin` paths in the app must follow it
  (`HttpRuntime.BinDirectory`).

## F62

**Linux: `Uri.TryCreate("/path", UriKind.Absolute)` succeeds and `Path.VolumeSeparatorChar` is `/`.** Guide gap
(.NET's behaviour on Unix, not webForCore's). Raised as [#92](https://github.com/xforcore/webForCore/issues/92). Found on
0.7.0, on Linux.

- **Apps:** umbraco-6:
  - the back office's log-in page checks `redir` with `Uri.TryCreate(url, UriKind.Absolute, ...)`. On Linux
    `/umbraco/Default.aspx` is a `file:` URI with no host, so every redirected log-in threw "There was attempt to
    redirect to '/umbraco/Default.aspx' which is another domain": `/umbraco/` ended in a 500;
  - `IOHelper.MapPath` took `path[1] == Path.VolumeSeparatorChar` as "already mapped", so every `~/...` path came back
    unmapped: the back office's language files weren't found (`[login]` for "Login") and Umbraco wrote its folders
    under one named `~` in the site.
- **Fix in the app:** `absoluteUri.IsFile == false` in the first; the second checks for `:` and, on Linux, for the
  site's folder.
- **umbraco-7:** the same `IOHelper.MapPath`, fixed the same way (not reached past the boot, F22). Its log-in page is
  AngularJS, so the first doesn't apply.
- **Guide:** section 6 lists case-sensitive paths and `/`, not these.

## F63

**Calls that become ambiguous on .NET 10 (CS0121).** Guide gap. Raised as
[#93](https://github.com/xforcore/webForCore/issues/93). Found on 0.7.0.

- **Apps:** umbraco-6:
  - `umbraco.cms`'s `ContentType.cs` calls Umbraco.Core's own `DistinctBy`, which .NET 6's `Enumerable.DistinctBy`
    now matches too (`CS0121 ... 'System.Linq.Enumerable.DistinctBy' and 'Umbraco.Core.EnumerableExtensions.DistinctBy'`);
  - `SqlCEHelper.cs` calls `ExecuteReader("...")` and `ExecuteNonQuery("...")` with no parameters, between
    `params IParameter[]` and `params P[]` overloads: the Visual Studio 2012/2013 compiler resolved them, Roslyn doesn't.
- **Fix in the app:** qualify the call (`EnumerableExtensions.DistinctBy(list, ...)`); pass `new IParameter[0]`.
  `<LangVersion>7.3</LangVersion>` helps with neither.
- **umbraco-7:** three `DistinctBy` calls (`ContentType.cs`, twice in `Umbraco.Web/Editors/EntityController.cs`, where
  the ambiguity also gave CS1503 on the next line); a chained call becomes a static call around the chain. Commented
  on #93.

## F64

**Umbraco on Linux: Windows file names, packages only on its MyGet feed, an installer that needs a JSON POST.**
App/environment. Found on 0.7.0, on Linux.

- **Apps:** umbraco-6, umbraco-7 (below).
- **Windows file names** (each fixed in the app's code or files, `NOTES.md`):
  - the `.resx` files' linked SQL scripts (`sql\total.sql` for `Sql/Total.sql`, and so on): `MSB3103`;
  - `web.config` in code for the site's `Web.config` (the installer's connection string handling: `FileNotFoundException`)
    and the `Views` folder's `Web.config`;
  - log4net's configuration and log file, backslashed paths relative to `AppDomain.BaseDirectory` (`bin_dotnet`,
    documented): no log was written.
- **Packages:** `log4net-mediumtrust`, `SqlServerCE`, `Tidy.Net`, `uGoLive`, `UrlRewritingNet.UrlRewriter` and
  `Microsoft.Web.Helpers` 1.0 aren't on nuget.org (`NU1101`), only on Umbraco's MyGet feed, which its
  `.nuget/NuGet.targets` names. The overlay adds a `src/NuGet.Config` with it; a build depends on that feed staying up.
- **SQL CE 4** (its embedded database option) is Windows-native; the migration uses SQL Server.
- **Its install wizard's database step** is an ASP.NET AJAX page method, `InstallerRestService.aspx/Install`, which
  takes only a JSON POST. The runner's `[setup]` steps sent only forms, so at first setup stopped there and the home
  page probe failed (1 of 16). The runner now has `json` steps: setup completes and all 16 pass.
- **umbraco-7:**
  - the same kinds of names: seven `.resx` links (`blockingweb.config` for `BlockingWeb.config`, `jsinitialize.js`,
    `..\..\umbraco.web.ui\views\web.config`, ...: `MSB3103`), and `web.config` for `Web.config` in five places in code;
  - **one folder in two cases:** the web project has files under both `umbraco/` (188) and `Umbraco/` (46: the
    installer's script, `AuthorizeUpgrade.cshtml`, the preview views, three language files, the partial view macro
    templates, `umbraco.aspx`). On Windows they're one folder; on Linux `/umbraco/...` reaches only `umbraco/`. The
    installer's view is asked for as `~/umbraco/install/views/index.cshtml` and is `umbraco/Install/Views/Index.cshtml`.
    Not fixed (the app doesn't boot, F22): it needs the folders merged and those paths fixed;
  - the repository's own `src/NuGet.Config` already names the MyGet feed;
  - the back office is an AngularJS client built with npm, bower and gulp 3 (not in the repository); gulp 3 doesn't
    run on current Node (it needs Node 10–12).

## F65

**Linux: named semaphores and events throw `PlatformNotSupportedException`; only named `Mutex` works.** Guide gap
(.NET's behaviour on Unix, not webForCore's). Raised as [#94](https://github.com/xforcore/webForCore/issues/94). Found
on 0.7.0, on Linux.

- **Apps:** umbraco-7: `MainDom` (which app domain owns the indexes, scheduled tasks and XML cache) makes a named
  `Semaphore` (through `AsyncLock`) and a named `EventWaitHandle` in `ApplicationContext.Init`, so the boot failed:
  `PlatformNotSupportedException: The named version of this synchronization primitive is not supported on this
  platform` at `Semaphore..ctor` ← `AsyncLock..ctor` ← `MainDom..ctor`.
- **Repro:** on Linux, `new Semaphore(1, 1, "x")` and `new EventWaitHandle(false, EventResetMode.AutoReset, "x")` throw
  it (checked on .NET 10.0.14); `new Mutex(false, "x")` works. On Windows all three work.
- **Fix in the app:** off Windows, unnamed (process-local) ones. The old worker still releases MainDom when the host
  stops it (`IRegisteredObject.Stop`), but a new worker no longer signals it. Under `webforcore-host` a recycle is a new
  process, which is what such code coordinates, so a guide note should name the options (a named `Mutex`, a file lock).
- **Guide:** section 6 (Linux) doesn't mention it.

## F66

**webForCore's `CallContext` doesn't reach prebuilt .NET Framework libraries: Log4Net.Async logs nothing.** Guide gap.
Raised as [#95](https://github.com/xforcore/webForCore/issues/95). Found on 0.7.0, on Linux (the same on any OS).

- **Apps:** umbraco-7: its `log4net.config` puts Log4Net.Async 2.0.4's `ParallelForwardingAppender` (net40, its last
  release) in front of the file appender. Every event failed, and log4net swallows it, so the log was empty; with
  `log4net.Internal.Debug`: `TypeLoadException: Could not load type 'System.Runtime.Remoting.Messaging.CallContext'
  from assembly 'mscorlib, Version=4.0.0.0'` at `Log4Net.Async.AsyncForwardingAppenderBase.get_HttpContext()`.
- **Cause:** webForCore's `System.Web.dll` carries `CallContext` (`docs/NAMESPACES.md`), which code compiled against
  webForCore binds to (Umbraco.Core's own copy of the appender base compiles and loads). A .NET Framework binary was
  compiled against mscorlib's, and .NET's mscorlib facade has no forward for it; webForCore can't add one.
- **Fix in the app:** the root logger writes through the file appender directly (synchronous logging).
- **Guide:** `docs/NAMESPACES.md` should say the carried types serve code compiled against webForCore, and
  Log4Net.Async belongs in `docs/MIGRATING.md`'s "Libraries that don't run on .NET".

## F67

**Linux with no locale: the culture is invariant, so apps that pick resources or language files by culture find
none.** Guide gap. Raised as [#97](https://github.com/xforcore/webForCore/issues/97). Found on 0.7.0, in CI on
GitHub's `ubuntu-24.04` runner (its locale is `C`); it passed on this machine (`en_GB.UTF-8`) and on Windows.

- **Apps:** umbraco-6: the back office's log-in page rendered its language keys (`value="[login]"`, `[username]`), so
  `[setup]`'s log-in failed and the home page was never made: 14/16 in CI. The same site under `LANG=C LC_ALL=C`
  locally shows it.
- **Cause:** .NET on Linux takes the culture from the locale; with none (`C`/`POSIX`) `CurrentCulture` and
  `CurrentUICulture` are `CultureInfo.InvariantCulture` (name `""`). On .NET Framework under IIS the culture is always
  a real one.
- **Fix in the app:** `<globalization culture="en-US" uiCulture="en-US">` in `web.config`, which webForCore honours:
  16/16 under `LANG=C`. Setting `LANG` for the server would do too.
- **Guide:** `docs/MIGRATING.md` should say that a Linux server or container without a locale runs with the invariant
  culture, and to set `<globalization culture uiCulture>` or `LANG`.

## F68

**Web API OData (Microsoft.AspNet.OData 7.5.18) needs System.Data.Linq to build any model.** Documented gap
(consequence undocumented). Raised as [#120](https://github.com/xforcore/webForCore/issues/120). Found on 0.7.0, on
Linux (the same on any OS).

- **Apps:** odata-samples-compositekey: `Application_Start` fails in `ODataConventionModelBuilder.GetEdmModel()`, so
  `MapODataServiceRoute` never runs and every OData URL is a 404 (the first request gets the 500, as on IIS): 8/15.
  ```
  System.TypeInitializationException: The type initializer for 'Microsoft.AspNet.OData.Formatter.EdmLibHelpers' threw an exception.
   ---> System.IO.FileNotFoundException: Could not load file or assembly 'System.Data.Linq, Version=4.0.0.0, ...'
  ```
- **Cause:** `EdmLibHelpers`' static CLR-to-EDM type table maps LINQ to SQL's `System.Data.Linq.Binary` to
  `Edm.Binary`, so every model touches `System.Data.Linq`, whatever the app's data access. The package has only a
  `net45` build. LINQ to SQL is listed as unavailable (`docs/NAMESPACES.md`), but not that it takes Web API OData with
  it.
- **Repro:** an SDK project with `webForCore`, `webForCore.Web.Http.WebHost` and `Microsoft.AspNet.OData` 7.5.18, and
  `new ODataConventionModelBuilder().EntitySet<Person>("People"); builder.GetEdmModel();` in `Application_Start`.
- **Options (for webForCore):** document it; or ship a `System.Data.Linq` 4.0.0.0 with just `Binary`; or build
  Microsoft.AspNet.OData (Microsoft's, MIT) with the mapping compiled out, as MVC's `LinqBinaryModelBinder` is.

## F69

**Windows Identity Foundation (`System.IdentityModel`) isn't on .NET or in NAMESPACES.md: IdentityServer3 can't
load.** Guide gap. Raised as [#119](https://github.com/xforcore/webForCore/issues/119). Found on 0.7.0, on Linux (the
same on any OS).

- **Apps:** identityserver3-example: IdentityServer3 2.5.0 (`lib/net45` only) on Katana under
  `Microsoft.Owin.Host.SystemWeb`. The app builds and its OWIN startup runs, then fails at `UseIdentityServer`: every
  request under `/core` is a 500 (7/10).
  ```
  System.IO.FileNotFoundException: Could not load file or assembly 'System.IdentityModel, Version=4.0.0.0, ...'
     Owin.UseIdentityServerExtension.UseIdentityServer(IAppBuilder app, IdentityServerOptions options)
  ```
- **Cause:** IdentityServer3 uses WIF (`X509SigningCredentials`, `SecurityTokenHandler`, `X509SecurityToken`,
  `System.IdentityModel.Protocols.WSTrust`), a .NET Framework assembly with no .NET build; webForCore doesn't provide
  it. Not webForCore's to fix.
- **Guide:** `docs/NAMESPACES.md` should list WIF (`System.IdentityModel`, `System.IdentityModel.Services`:
  `<system.identityModel>`, `WSFederationAuthenticationModule`, `SessionAuthenticationModule`) as unavailable, with
  the options (Katana's WS-Federation middleware, `Microsoft.IdentityModel.Tokens`), and the Phase 0 blocker search
  should look for it.

## F70

**`/signalr/hubs` serves the template's unreplaced `[!VERSION!]`.** webForCore bug (minor). Raised as
[#118](https://github.com/xforcore/webForCore/issues/118). Found on 0.7.0, on Linux (the same on any OS).

- **Apps:** signalr-samples: the generated hub proxy starts `* ASP.NET SignalR JavaScript Library [!VERSION!]`;
  Microsoft's `Microsoft.AspNet.SignalR.Core` 2.4.3 says `2.4.3` there. One probe: 18/19.
- **Cause:** SignalR's build stamps the version into `Scripts/hubs.js` before embedding it;
  `webForCore.Microsoft.AspNet.SignalR.Core` embeds the source file as it is.
- **Repro:** any app with `app.MapSignalR()` and a hub: `curl /signalr/hubs | head -3`.
- **Effect:** cosmetic for the JavaScript client, but a response that differs from IIS.

## F71

**VB projects: `<MyType>Windows</MyType>` doesn't compile on .NET.** Guide gap. Raised as
[#121](https://github.com/xforcore/webForCore/issues/121). Found on 0.7.0.

- **Apps:** signalr-samples: its VB class library (one hub) has Visual Studio's default `MyType` `Windows`. As an
  SDK-style `net10.0` project:
  ```
  vbc : error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.ApplicationBase' is not defined.
  vbc : error BC30002: Type 'Global.Microsoft.VisualBasic.Devices.Computer' is not defined.
  ```
- **Cause:** the `My` namespace for `Windows` needs `Microsoft.VisualBasic.Forms`, which is Windows Desktop only on
  .NET.
- **Fix in the app:** `<MyType>Empty</MyType>` (nothing in the library uses `My.Computer`, `My.User` or
  `My.Application`).
- **Guide:** `docs/MIGRATING.md` section 2 shows only C# projects; it could say what a `.vbproj` needs (`MyType`, the
  `<Import Include>` namespaces, `GenerateAssemblyInfo` false with `My Project/AssemblyInfo.vb`).

## F72

**Linux: the user's inotify instances (128 by default) run out with many apps running.** App/environment. Found on
0.7.0, on this machine (Fedora, `fs.inotify.max_user_instances` = 128), with several smoke runs of other agents
running at the same time as the same user.

- **Apps:** signalr-samples (one run: `/Hubs/DemoHub/`, `/Hubs/Chat/index.htm` and `/Scripts/jquery.signalR.js` were
  500s, `Configuration Error: Failed to start monitoring changes to '.../Hubs/DemoHub'` from
  `System.Web.DirMonCompletion`), identityserver3-example (a run by hand didn't start: `IOException: The configured user
  limit (128) on the number of inotify instances has been reached` from ASP.NET Core's configuration file watcher).
  Both passed on a rerun once other processes had stopped.
- **Cause:** every process with a `FileSystemWatcher` holds at least one inotify instance; each webForCore worker
  held 11 to 23 here (counted from `/proc/<pid>/fd`), though .NET 10 shares one inotify instance between a process's
  watchers (a test program with 5 watchers held 1). Where the others come from wasn't established, so this isn't
  raised as a webForCore bug. A server running many sites under one user may need a higher
  `fs.inotify.max_user_instances`. (Round 8: that test ran on .NET 11; on .NET 10 each watcher is an instance, F92.)
- **In the runs:** `DOTNET_USE_POLLING_FILE_WATCHER=1` (ASP.NET Core's configuration watchers poll) for the final
  Linux runs; nothing committed changes.

## F73

**MVC 3/4 apps: `webpages:Version` 1.0.0.0 (or 2.0.0.0) fails every request with "Conflicting versions of ASP.NET
Web Pages detected".** Guide gap. Raised as [#122](https://github.com/xforcore/webForCore/issues/122). Found on 0.7.0
(candidates round), on Linux.

- **Apps:** mvc-music-store (MVC 3).
- **Error:** `System.InvalidOperationException: Conflicting versions of ASP.NET Web Pages detected: specified version
  is "1.0.0.0", but the version in bin is "3.0.0.0". To continue, remove files from the application's bin directory or
  remove the version specification in web.config.` on the first request, from `<add key="webpages:Version"
  value="1.0.0.0"/>` (the MVC 3 template's; MVC 4's says `2.0.0.0`).
- **Fix in the app:** `value="3.0.0.0"`, as Microsoft's MVC 3/4 → 5 upgrade on .NET Framework. The other MVC 3
  version numbers in web.config (`<compilation><assemblies>`, `Views/Web.config`'s `System.Web.WebPages.Razor,
  Version=1.0.0.0` sections and `MvcWebRazorHostFactory, System.Web.Mvc, Version=3.0.0.0`) work unchanged.
- **Guide:** `docs/MIGRATING.md` says an app on an older MVC gets the upgrade, but not that MVC 3/4 apps need this.

## F74

**MVC 3/4 apps: `[Compare]` is ambiguous (CS0104) between MVC's and DataAnnotations' `CompareAttribute`.** Guide
gap. Raised as [#122](https://github.com/xforcore/webForCore/issues/122) (with F73). Found on 0.7.0, on Linux.

- **Apps:** mvc-music-store (the MVC 3 template's `Models/AccountModels.cs`; every MVC 3/4 template app has it).
- **Error:** `CS0104: 'Compare' is an ambiguous reference between
  'System.ComponentModel.DataAnnotations.CompareAttribute' and 'System.Web.Mvc.CompareAttribute'`. .NET Framework 4.5
  added the DataAnnotations one; the app targeted 4.0, where only MVC's existed.
- **Fix in the app:** `using CompareAttribute = System.Web.Mvc.CompareAttribute;` in that file (what it compiled to
  on 4.0; the Register form keeps `data-val-equalto`).
- **Guide:** like #17's `SignInStatus`, a type the newer framework added; `docs/MIGRATING.md` doesn't mention it.

## F75

**No `aspnet_regsql`: `SqlServices.Install` looks for `InstallCommon.sql` in .NET's runtime folder, so apps on the
default SQL membership, role and profile providers can't create their database.** webForCore bug. Raised as
[#123](https://github.com/xforcore/webForCore/issues/123). Found on 0.7.0, on Linux (the same on any OS).

- **Apps:** mvc-music-store: `AccountController`'s `Membership.ValidateUser`/`CreateUser` use machine.config's
  `AspNetSqlMembershipProvider` on `LocalSqlServer` (SQL Express user instance, `aspnetdb.mdf`), which can't exist on
  Linux; posting log-on or register is a 500, "SQLExpress database file auto-creation error". Checkout and the store
  manager (a user, the Administrator role) are untested.
- **Cause:** on .NET Framework the providers' schema scripts (`InstallCommon.sql`, `InstallMembership.sql`,
  `InstallRoles.sql`, `InstallProfile.sql`, `InstallSqlState.sql`, ...) are in the framework folder, which
  `System.Web.Management.SqlServices`, `aspnet_regsql.exe` and the SQL Express auto-creation read
  (`HttpRuntime.AspInstallDirectory`). webForCore ships none of them and no `aspnet_regsql` equivalent;
  `AspInstallDirectory` is .NET's runtime folder.
- **Repro:** a page that calls
  `SqlServices.Install("aspnetdb", SqlFeatures.Membership, "<a SQL Server connection string>")`:
  `FileNotFoundException: Could not find file '<dotnet>/shared/Microsoft.NETCore.App/10.0.14/InstallCommon.sql'`.
- **Affects:** every MVC 2–4 and Web Forms 3.5/4.0 template app's accounts, `SqlWebEventProvider`, and SQL Server
  session state (`InstallSqlState.sql`).

## F76

**ICU culture data: `NumberDecimalDigits` is 3 for `en-US`/`en-GB`, so `{0:F}` prints `8.990` for 8.99.** Guide gap
(.NET's culture data on ICU, not webForCore's). Raised as [#124](https://github.com/xforcore/webForCore/issues/124).
Found on 0.7.0, on Linux (Fedora 44, ICU 77.1).

- **Apps:** mvc-music-store: the album page's price, `@String.Format("{0:F}", Model.Price)`, is `8.990` (IIS:
  `8.99`). Not probed.
- **Repro:** plain .NET 10, `string.Format(new CultureInfo("en-US"), "{0:F}", 8.99m)` is `8.990`; `NumberDecimalDigits`
  is 3 for `en-US` and `en-GB` (ICU's pattern `#,##0.###`), 2 for the invariant culture and on Windows NLS.
- **Guide:** `docs/MIGRATING.md` section 6 says ICU's formats differ "slightly" (currency and date patterns); this one
  changes ordinary numbers (`F`, `N` with no precision).

## F77

**Windows-only data stores and data written on Windows: SQL CE, SQL Express user instances, `\` in a wiki's page
index.** App/environment. Found on 0.7.0 (candidates round), on Linux.

- **Apps:**
  - screwturn-wiki: its page index (`public/Pages.cs`) lists the `Dev` namespace's main page as `Dev\MainPage.cs`;
    `Path.Combine` with the pages folder finds no such file on Linux, so `/Dev.MainPage.ashx` rendered empty. The
    data file says `Dev/MainPage.cs` in the overlay; the app writes new entries with `Path.DirectorySeparatorChar`, so
    only data made on Windows has it (an existing wiki moved to Linux needs the same edit).
  - mvc-music-store: Entity Framework on SQL Server Compact 4.0 (Windows-native, no .NET build) and membership on a
    SQL Express user instance (`LocalSqlServer`). The catalogue uses the test bed's SQL Server; membership is F75.
  - rabbit-webpages: WebMatrix's `Database.Open("StarterSite")` with no connection string opens
    `App_Data/StarterSite.sdf` (SQL CE); a `StarterSite` connection string to SQL Server replaces it.
- **Guide:** covered (section 5: "LocalDB doesn't exist on Linux"; section 6: file paths), except that the guide
  doesn't name SQL CE or `User Instance=true` (the SQL Express of the MVC 3/Web Forms 4.0 templates).

## F78

**WebMatrix.Data and WebMatrix.WebData aren't provided or documented; Microsoft's 3.2.9 packages work.** Guide gap.
Raised as [#130](https://github.com/xforcore/webForCore/issues/130). Found on 0.7.0 (candidates round), on Linux.

- **Apps:** rabbit-webpages (WebMatrix's Starter Site: `WebSecurity`, `Database.Open`). Every WebMatrix template site
  and the MVC 4 Internet Application template use them.
- **What's missing:** webForCore builds Web Pages, Razor, Deployment and Helpers from AspNetWebStack, but not
  `WebMatrix.Data`, `WebMatrix.WebData` or `System.Web.WebPages.Administration` (its fork has them removed), and
  `docs/NAMESPACES.md`/`docs/MIGRATING.md` don't say so. On WebMatrix sites they came from the GAC, so there's
  nothing in the site's `bin`.
- **What works:** `Microsoft.AspNet.WebPages.Data` and `Microsoft.AspNet.WebPages.WebData` 3.2.9, unchanged; their
  Web Pages and Razor dependencies lose to webForCore's. SimpleMembership creates its tables on SQL Server; register,
  log in, log out and an authenticated page work, and WebData's pre-start method runs.

## F79

**`Response.End()` after `StatusCode = 401` sends the 401, so forms authentication doesn't redirect to the log-in
page.** webForCore bug. Raised as [#129](https://github.com/xforcore/webForCore/issues/129). Found on 0.7.0, on Linux.

- **Apps:** rabbit-webpages: `Account/ChangePassword.cshtml` calls `WebSecurity.RequireAuthenticatedUser()`, which is
  `Response.StatusCode = 401; Response.End();`; an anonymous request gets an empty 401, where IIS redirects to
  `/Account/Login?ReturnUrl=...` (1 of 21 probes).
- **Repro:** in a forms-authentication site, `@{ Response.StatusCode = 401; }` gets the 302 to the log-in page;
  `@{ Response.StatusCode = 401; Response.End(); }` gets the 401. The same in `.aspx`.
- **Cause (likely):** on .NET Framework, `Response.End()` in the handler aborts the thread and the response stays
  buffered, so `FormsAuthenticationModule`'s EndRequest still replaces the 401 with its redirect. webForCore has no
  thread abort (documented), and its `End()` sends the response first.
- **Affects:** every Web Pages page with `WebSecurity.RequireAuthenticatedUser()`/`RequireRoles()`, and Web Forms code
  that sends users to the log-in page with a 401 and `Response.End()`.
- **Fixed in 0.9.0** (webForCore#129): `Response.End()` leaves the response to `EndRequest`, so forms authentication
  turns the 401 into its redirect; rabbit-webpages passes 37/37 on Linux, Change Password included.

## F80

**A request path with a percent-escaped character (a space, any non-ASCII character) is answered 400 with an empty
body, before the application sees it.** webForCore bug. Raised as
[#126](https://github.com/xforcore/webForCore/issues/126). Found on 0.7.0 (Linux and Windows), in the candidates round for the
AspNetDocs tutorial apps.

- **Apps:** aspnetdocs-wingtip-toys: its product list links each product as
  `GetRouteUrl("ProductByNameRoute", new { productName = Item.ProductName })`, `/Product/Convertible%20Car`, so every
  product link answers 400 (IIS: 200). `/About%20x` is 400 too (IIS: 404).
- **Cause:** `AspNetCoreWorkerRequest.ParseRequest` builds the path as `$"{Context.Request.PathBase}{Context.Request.Path}"`;
  `PathString.ToString()` is the escaped form, so the path has `%20`, and `IsBadPath()` rejects any `%`.
- **Repro:** any site, `GET /a%20b.htm` with a file `a b.htm`, or a page route with a value containing a space.
- **Suggested fix:** the decoded `PathBase.Value + Path.Value`.
- **Fixed in 0.9.0** (webForCore#126): the path reaches the application decoded, as on IIS;
  `/Product/Convertible%20Car` renders the product.

## F81

**`Request.RawUrl` is the default document's path for a directory request, so FriendlyUrls redirects `/` to
`/Default`.** webForCore bug. Raised as [#127](https://github.com/xforcore/webForCore/issues/127). Found on 0.7.0
(Linux and Windows), in the candidates round for the AspNetDocs tutorial apps.

- **Apps:** aspnetdocs-wingtip-toys (the Visual Studio 2013 Web Forms template's
  `EnableFriendlyUrls(new FriendlyUrlSettings { AutoRedirectMode = RedirectMode.Permanent })`): `GET /` is
  `301 Location: /Default`; IIS serves `Default.aspx` with 200. `/Default` itself renders.
- **Cause:** `AspNetCoreHost.IsLegacyRequest` rewrites `Request.Path` to `/Default.aspx`, and `GetRawUrl()` returns that
  path. On IIS `RawUrl` stays `/`. FriendlyUrls' `FriendlyUrlsModule.RedirectToFriendlyUrl` redirects only when
  `RawUrl` contains the `.aspx` path, so that default-document requests aren't redirected.
- **Repro:** a Web Forms site with `Default.aspx` and `RouteTable.Routes.EnableFriendlyUrls(new FriendlyUrlSettings
  { AutoRedirectMode = RedirectMode.Permanent })` (Microsoft.AspNet.FriendlyUrls 1.0.2); `GET /`.
- **Suggested fix:** `RawUrl` from the request's raw target, taken before the default-document rewrite.
- **Fixed in 0.9.0** (webForCore#127): `RawUrl` is `/` for the directory, and FriendlyUrls serves `/`;
  aspnetdocs-wingtip-toys' home page probe passes.

## F82

**Linux: `HttpServerUtility.MachineName` throws `DllNotFoundException` (`kernel32` `GetComputerName`).** webForCore
bug. Raised as [#128](https://github.com/xforcore/webForCore/issues/128). Found on 0.7.0 (Linux), in the candidates
round for the AspNetDocs tutorial apps.

- **Apps:** aspnetdocs-wingtip-toys, through ELMAH 1.2.2 (`Elmah.Environment.TryGetMachineName`): `/elmah.axd/rss`
  and `/json` fail with the exception (behind the app's custom error page), and `/elmah.axd`, the error log, answers
  200 with an empty body. `/elmah.axd/about` and `/stylesheet` work. ELMAH is one of the most common ASP.NET packages.
- **Error:** `DllNotFoundException: Unable to load shared library 'kernel32.dll'` at
  `System.Web.UnsafeNativeMethods.GetComputerName` ← `HttpServerUtility.GetMachineNameInternal()` ←
  `HttpServerUtility.get_MachineName()`.
- **Repro:** a page with `<%= Server.MachineName %>`, on Linux.
- **Suggested fix:** `Environment.MachineName` off Windows.
- **Fixed in 0.9.0** (webForCore#128): `Server.MachineName` is `Environment.MachineName`. On Linux `/elmah.axd`, its RSS and
  JSON work; aspnetdocs-wingtip-toys passes 39/39 on Linux.

## F83

**C# 14's first-class span conversions change LINQ expression trees, so LINQ to Entities fails at run time; the guide
describes only the compile errors.** Guide gap. Raised as [#138](https://github.com/xforcore/webForCore/issues/138)
(the step itself is #43, F48). Found on 0.8.0 (Linux), migrating nopCommerce 3.90.

- **Apps:** nopcommerce-3.90 (every product page: `ProductService.GetProductsByIds`, `where productIds.Contains(p.Id)`).
  smartstorenet pins C# 13 from the start, so it can't hit it.
- **Error:** the project builds; at run time `System.NotSupportedException: LINQ to Entities does not recognize the
  method 'Boolean Contains[Int32](System.ReadOnlySpan`1[System.Int32], Int32)' method, and this method cannot be
  translated into a store expression.` With no `<LangVersion>`, net10.0 compiles C# 14, which binds `int[].Contains(x)`
  inside an expression tree to `MemoryExtensions.Contains(ReadOnlySpan<T>, T)`; EF 6 can't translate it.
- **What worked:** `<LangVersion>7.3</LangVersion>` (the projects set none), as `docs/MIGRATING.md` section 2 says. But
  its symptoms there are only `CS0029`/`CS1503`, so a project that compiles looks as if it didn't need it, and
  `docs/AGENT-MIGRATION.md`'s Phase 2 doesn't list the property. `scripts/convert.cs` pins it only for SDK-style
  projects, not for the old-style ones it rewrites.
- **Repro:** net10.0, EntityFramework 6.5.1, no `<LangVersion>`: `var ids = new[] { 1, 2 };
  db.Products.Where(p => ids.Contains(p.Id)).ToList()` throws; with `<LangVersion>13.0</LangVersion>` it works.

## F84

**`AppDomain.CurrentDomain.DynamicDirectory` is null on .NET.** Guide gap. Raised as
[#139](https://github.com/xforcore/webForCore/issues/139). Found on 0.8.0, migrating nopCommerce 3.90.

- **Apps:** nopcommerce-3.90 and smartstorenet: their plugin managers (Umbraco's design, a `PreApplicationStartMethod`)
  copy each plugin's DLLs into the code generation folder, `Assembly.Load` them by name and add them to `BuildManager`.
- **Error:** `ArgumentNullException: Value cannot be null. (Parameter 'path')` at `new DirectoryInfo(
  AppDomain.CurrentDomain.DynamicDirectory)` in `PluginManager.PerformFileDeploy`, from `BuildManager.
  InvokePreStartInitMethodsCore`: every request fails. A plain .NET console app prints `null` for it too, so webForCore
  can't change it.
- **What worked:** `HttpRuntime.CodegenDir`, which is the folder `DynamicDirectory` was on IIS; webForCore's resolver
  probes it, so nopCommerce's 20 plugins load, are referenced by the views' compilation and work. SmartStoreNET also
  reads `AppDomainSetup.PrivateBinPath` (no such property on .NET, CS1061): `HttpRuntime.BinDirectory`.
- **Suggested fix:** a line beside `BaseDirectory` in `docs/NAMESPACES.md` and `docs/MIGRATING.md` section 6, and
  `DynamicDirectory` in AGENT-MIGRATION.md's search list.

## F85

**System.Linq.Dynamic 1.0.7 is a .NET Framework 4.0 library that references `System.Data.Entity` 4.0 (EF 1–4).**
App/environment. Found on 0.8.0, migrating nopCommerce 3.90.

- **Apps:** nopcommerce-3.90: every administration edit page (`BaseNopValidator` builds its length rules with
  `DynamicExpression.ParseLambda`), the Kendo grids' filters and sorting, and message templates' conditions.
- **Error:** `FileNotFoundException: Could not load file or assembly 'System.Data.Entity, Version=4.0.0.0,
  Culture=neutral, PublicKeyToken=b77a5c561934e089'`.
- **What worked:** its successor, System.Linq.Dynamic.Core 1.2.1 (MIT; SmartStoreNET 4 uses it): the namespace,
  `DynamicExpressionParser.ParseLambda<T, TResult>(null, false, name)`, and `.AsQueryable()` before a `Where(string)`
  on an array. Three files.

## F86

**Linux: a static file requested in another case than its own is a 404 until it has been requested once in its own
case; then it is a 200 in any case.** webForCore bug. Raised as [#140](https://github.com/xforcore/webForCore/issues/140).
Found on 0.8.0 (Linux), migrating nopCommerce 3.90.

- **Apps:** nopcommerce-3.90: it writes every product picture's URL as `/content/images/thumbs/<file>`, and the folder
  is `Content/Images/Thumbs` (where `MapPath("~/content/images/thumbs")` puts the files, as webForCore resolves the
  folder's case). Every picture on the store's pages is a 404 on Linux, unless it was requested in its own case since
  the worker started. On Windows it's a 200, as on IIS.
- **Repro:** a minimal site (`Program.cs` as in `docs/MIGRATING.md` section 4) with `Content/a.txt` and
  `Content/b.txt`: `/content/a.txt` 404, `/Content/a.txt` 200, `/content/a.txt` 200, `/CONTENT/A.TXT` 200,
  `/content/b.txt` 404, `/Content/b.txt` 200, `/content/b.txt` 200.
- **Cause (likely):** the path mapping cache is keyed case-insensitively, as on .NET Framework, so a request in the
  file's own case caches its mapping for every case; a first request in another case maps literally and misses.
- **Suggested fix:** one rule: resolve the case on every lookup off Windows (as `MapPath` does for folders, and as IIS
  on NTFS behaves), or key the cache case-sensitively off Windows so the answer is always a 404.
- Not worked around in the app: its probe of a thumbnail at the URL the store writes shows it.

## F87

**webForCore's `System.Web.Optimization` and `WebGrease` don't have Microsoft's assembly identities**, so libraries
built on Microsoft's bundling don't compile against them. webForCore bug. Raised as
[#141](https://github.com/xforcore/webForCore/issues/141). Found on 0.8.0, migrating SmartStoreNET 4.x. The same kind of
problem as F40 (SignalR and DataVisualization at 0.0.0.0, #40).

- **Apps:** smartstorenet: all its bundles (and its themes' SCSS) go through BundleTransformer 1.10–1.12. The build
  stops in `SmartStore.Web.Framework` with 7 distinct errors: `CS0012: The type 'IBundleTransform' is defined in an
  assembly that is not referenced. You must add a reference to assembly 'System.Web.Optimization, Version=1.1.0.0,
  Culture=neutral, PublicKeyToken=31bf3856ad364e35'`, `CS0029`, `CS0039`.
- **Identities:** webForCore 0.8.0's `System.Web.Optimization` and `WebGrease` are `Version=11.0.0.0,
  PublicKeyToken=b03f5f7f11d50a3a`; Microsoft's are `System.Web.Optimization 1.1.0.0` and `WebGrease 1.6.5135.21930`,
  `PublicKeyToken=31bf3856ad364e35`. webForCore's MVC, Web Pages, Web API and Katana keep Microsoft's.
- **Repro:** a net10.0 library with `webForCore.Web.Optimization` 0.8.0 and `BundleTransformer.Core` 1.10.0:
  `System.Web.Optimization.IBundleTransform t = new BundleTransformer.Core.Transformers.StyleTransformer();` is CS0012.
- **Cause:** the packaged project (`WebFormsForCore.Web.Optimization.csproj`) is signed with webForCore's own key and
  versioned with webForCore, unlike the `System.Web.Optimization.csproj` beside it (Microsoft's shared key).
- **Suggested fix:** Microsoft's names, versions and key for both, as #40 did for SignalR.

## F88

**The same as [F92](#f92) (found the same day with Kooboo CMS, raised as
[#152](https://github.com/xforcore/webForCore/issues/152)): one inotify instance per watched folder; nopCommerce 3.90's
worker takes more than 50 of the user's 128.** webForCore bug (`docs/IIS-COMPATIBILITY.md` says about 15). Found on
0.8.0 (Linux), migrating nopCommerce 3.90.

- **Apps:** nopcommerce-3.90: its worker had 48 inotify instances after a dozen store pages (from `/proc/<pid>/fd`), and
  more after the administration: each folder with a `web.config` (23 in the site and its plugins) and each folder of
  views its theme view engine looks in. With this machine's other processes holding about 70, pages fail with
  `IOException: The configured user limit (128) on the number of inotify instances has been reached` from
  `DirMonCompletion.StartManagedDirectoryWatch`, for whichever folder comes next (a theme's `Content/Web.config`,
  `Administration/Web.config`); the local runs never passed every probe, CI's did.
- `<httpRuntime fcnMode="Single">` didn't change it (each folder still got its own watcher), as F92 found for Kooboo.

## F89

**Windows, intermittent: nopCommerce's plugin manager can't replace a plugin's copy in the compilation folder at start
("rename failed, cannot initialize plugin"), so every request fails.** App/environment (nopCommerce's plugin manager,
which copies the same way on IIS). Found on 0.8.0 (Windows, CI), with nopCommerce 3.90; never on Linux.

- **Apps:** nopcommerce-3.90 on Windows: setup passes (3/3) every time; then in some runs every smoke request is a 500,
  "The process cannot access the file because it is being used by another process" (7/23: only the forbidden-path
  checks pass); in the others the smoke run passes 23/23.
- **Where** (the stack the runner now records, CI run 41926373548): `PluginManager.Initialize`, a
  `PreApplicationStartMethod`, copies each plugin's DLL into the compilation folder over the copy setup's workers loaded
  (`File.Copy(..., overwrite: true)`); when that fails it renames the old copy aside, and that failed too:
  `IOException: ~\AppData\Local\Temp\Temporary_ASP.NET_Files\root\<hash>\Nop.Plugin.Shipping.UPS.dll rename failed,
  cannot initialize plugin` ← `IOException: The process cannot access the file because it is being used by another
  process`. The plugins before it in nopCommerce's order were replaced; UPS (one of the largest, with its SOAP proxies)
  wasn't.
- **Runs:** 2 of 5 Windows runs passed (41918820640, 41924554094); 3 failed (41916845600, 41921438048, 41926373548).
  The runner's stop on Windows now ends the server's whole process tree (`terminate()` ended only `webforcore-host`,
  leaving setup's worker to stop by itself in the same compilation folder); failures happened with and without it, so
  what holds the file is something else: a handle without `FILE_SHARE_DELETE` (an antivirus scan of a DLL that was
  just written is the usual one on Windows runners) at the moment the plugin manager runs.
- **Runner changes:** `Server.stop` ends the process tree on Windows; a failed probe records the error page's exception
  lines and first stack frames (`stack`, paths scrubbed as in the logs).
- Not raised: nothing shown to differ from IIS, where nopCommerce copies the same way (its code's comments describe
  locked plugin files there, and the rename is its workaround).

## F90

**The parameterless crypto factories (`KeyedHashAlgorithm.Create()`, `HashAlgorithm.Create()`, ...) throw on .NET.**
Guide gap (.NET's behaviour, not webForCore's). Raised as [#151](https://github.com/xforcore/webForCore/issues/151).
Found on 0.8.0, on Linux, in round 8.

- **Apps:** kooboo-cms: its password hashing (`Kooboo.CMS.Account/Services/PasswordProvider.cs`) calls
  `KeyedHashAlgorithm.Create()`, so every back-office log-in was a 500.
- **Error:** `PlatformNotSupportedException: This platform does not allow the automatic selection of an algorithm.` at
  `System.Security.Cryptography.KeyedHashAlgorithm.Create()`.
- **Cause:** on .NET Framework the parameterless `Create()` of `HashAlgorithm`, `KeyedHashAlgorithm`, `HMAC`,
  `SymmetricAlgorithm` and `AsymmetricAlgorithm` returned machine.config's default (`SHA1CryptoServiceProvider`,
  `HMACSHA1`, `RijndaelManaged`, `RSACryptoServiceProvider`). .NET obsoletes them (SYSLIB0007) and throws; the named
  overloads (`HashAlgorithm.Create("SHA256")`) still work. Hashes the old site stored were made with the default, so
  the replacement has to be that class.
- **Fix in the app:** `new HMACSHA1()`. The `admin` user shipped in Kooboo's `Cms_Data` then logs in (`admin`/`admin`).
- **Repro:** a page with `<%= System.Security.Cryptography.KeyedHashAlgorithm.Create().GetType() %>`: `HMACSHA1` on
  IIS, the exception on webForCore.

## F91

**DotNetZip 1.9 (`Ionic.Zip.Reduced`) can't extract on Linux.** App/environment. Found on 0.8.0, on Linux, in round 8.

- **Apps:** kooboo-cms: creating a site from a template (a zip in `Cms_Data/SiteTemplates`) failed, so there was no site
  to serve.
- **Error:** `ArgumentException: The value cannot be an empty string. (Parameter 'path')` at
  `System.IO.Directory.CreateDirectory` ← `Ionic.Zip.ZipEntry.InternalExtract` ← `ZipFile.ExtractAll`.
- **Cause:** DotNetZip 1.9.1.8 (2011, .NET Framework) makes the output path with Windows separators, so on Linux the
  entry's folder comes out empty. The .NET Standard releases (1.13 and later) extract correctly.
- **Fix in the app:** the DotNetZip 1.16.0 package (the same `Ionic.Zip` namespace and API) for the checked-in
  `Ionic.Zip.Reduced.dll`, in the three projects that use it.

## F92

**Linux: every directory ASP.NET watches is an inotify instance of its own on .NET 10, so one CMS's worker takes most
of the default 128.** webForCore bug (the docs say about 15). Raised as
[#152](https://github.com/xforcore/webForCore/issues/152). Found on 0.8.0, on Linux (Fedora,
`fs.inotify.max_user_instances` = 128), in round 8.

- **Apps:** kooboo-cms: its worker held 38 inotify instances after ten page types of one sample site and 48 when its
  16 probe paths ran it out (counted from `/proc/<pid>/fd`; the host holds 1 more). Each Razor template folder it renders
  from (under `Cms_Data/Sites/<site>/Templates`) adds one or two, so a bigger site or a second one takes more. With the
  other processes of this machine's user holding about 70, its probes failed with `Configuration Error: Failed to start
  monitoring changes to '<site>/Cms_Data/...'` and it never passed every probe here; twice its worker didn't start.
  umbraco-8 (no content, `fcnMode="Single"` in its `web.config`) took 20.
- **Where they go (Kooboo, one run):** 32 instances watching one directory each (the site root, `App_Data`, each level of
  `Cms_Data` down to the template folders, most template folders twice), 4 watching the whole site recursively (464
  directories each) and 1 watching `bin_dotnet/App_Data` recursively.
- **Cause:** `DirMonCompletion` creates a `FileSystemWatcher` per directory (IIS had a `ReadDirectoryChangesW` handle per
  directory), and on .NET 10 each `FileSystemWatcher` is its own inotify instance: a console program with ten watchers
  on ten folders held ten on .NET 10.0.14, and one on .NET 11. F72's note that ".NET 10 shares one inotify instance" came
  from a test run on .NET 11. `<httpRuntime fcnMode="Single" />` didn't change Kooboo's count (48 either way, the same
  requests).
- **Docs:** `docs/IIS-COMPATIBILITY.md` (process model): "Each worker also takes about 15 inotify instances".
- **Suggested fix:** one watcher per application (a recursive one on its folder, which `fcnMode="Single"` meant on IIS)
  or one inotify instance per process; at least document that the count grows with the directories a site serves and
  compiles from.

## F93

**CSharpTest.Net.Collections 14.906 (Umbraco 8's NuCache file store) fails on .NET: it reflects into `FileStream` and
P/Invokes kernel32.** App/environment. Found on 0.8.0, on Linux, in round 8.

- **Apps:** umbraco-8: the first request after the install built NuCache's local B+ tree files
  (`App_Data/TEMP/NuCache/*.db`) and failed: `NullReferenceException` at
  `CSharpTest.Net.IO.TransactedCompoundFile.FlushStream` ← `BTreeFileStoreV2.CreateRoot` ← `BPlusTree..ctor` ←
  `Umbraco.Web.PublishedCache.NuCache.DataSource.BTree.GetTree`.
- **Cause:** that build's `FlushStream` reads `FileStream`'s private `_handle` field (which .NET's `FileStream` doesn't
  have) and calls kernel32's `FlushFileBuffers`. Umbraco's own .NET build of the library (`Umbraco.CSharpTest.Net.Collections`,
  for Umbraco 9 and later) uses `FileStream.Flush(true)` there.
- **Fix in the app:** `Umbraco.CSharpTest.Net.Collections` 14.906.1403.1085 for `CSharpTest.Net.Collections`: the same
  assembly name and API.

## F94

**An app's own `Microsoft.CodeAnalysis` (Roslyn) package conflicts with webForCore.Compilers' 5.9.0 (NU1107).** Guide
gap. Raised as [#153](https://github.com/xforcore/webForCore/issues/153). Found on 0.8.0, in round 8.

- **Apps:** umbraco-8: its embedded ModelsBuilder compiles models with Roslyn (`Microsoft.CodeAnalysis.CSharp` 2.10.0, in
  `Umbraco.ModelsBuilder.Embedded` and the web project).
- **Error:** `NU1107: Version conflict detected for Microsoft.CodeAnalysis.Common` (`webForCore 0.8.0 -> webForCore.Compilers
  0.8.0 -> Microsoft.CodeAnalysis.VisualBasic 5.9.0 -> Microsoft.CodeAnalysis.Common (= 5.9.0)` against `Microsoft.CodeAnalysis.CSharp
  2.10.0 -> Microsoft.CodeAnalysis.Common (= 2.10.0)`).
- **Fix in the app:** `Microsoft.CodeAnalysis.CSharp` 5.9.0; it builds and the site runs. It's the guide's "every package
  webForCore depends on is a floor", but Roslyn's packages pin each other exactly, so it's NU1107, not the NU1605 the
  guide describes.

## F97

**Linux: each monitored folder takes an inotify instance of its own (two for many), so a site with many folders can't
start.** webForCore bug. Raised as [#145](https://github.com/xforcore/webForCore/issues/145). Found on 0.8.0 (Linux),
migrating Orchard CMS 1.10.4.

- **Apps:** orchard-1.10: every request is a 500 (`IOException: The configured user limit (128) on the number of inotify
  instances has been reached`), static files included; Linux is blocked at 9/15. Probably any module or plug-in based
  app with many folders.
- **Cause:** off Windows, `FileChangesMonitor` gives each `DirectoryMonitor` a `FileSystemWatcher`
  (`DirMonCompletion.StartManagedDirectoryWatch`), and each is an inotify instance, 128 per user by default
  (`fs.inotify.max_user_instances`). `CacheDependency`, `MapPathBasedVirtualPathProvider.FileExists` (which caches
  with a dependency on the file) and web.config reads each monitor the folder they touch; many folders get two
  instances, and each worker has 4-5 recursive watchers on the site as well. IIS's monitors are directory handles with
  no comparable limit. `IIS-COMPATIBILITY.md` documents about 15 instances per worker, not that the count grows with
  the folders the site uses.
- **Measured** (the worker run as a user with no other instances): Orchard with its 69 modules reached 127 (+1 for the
  host) during its first request, probing each module's folder and `bin`. With only the 17 modules its Core recipe
  needs, it started at about 65 and ran out during the recipe.
- **Repro:** a page that creates a `CacheDependency` on a file in each of 200 new folders fails at folder 118 (#145
  has it).
- **Suggested fix:** one recursive watcher on the application root dispatching to the directory monitors (what
  `fcnMode="Single"` asks for, F98), or at least one instance per folder.

## F98

**`<httpRuntime fcnMode>` is ignored: `Disabled` and `Single` still monitor every folder.** webForCore bug. Raised as
[#146](https://github.com/xforcore/webForCore/issues/146). Found on 0.8.0 (Linux), migrating Orchard CMS 1.10.4.

- **Apps:** orchard-1.10 (tried as the IIS setting for sites with many folders, F97; reverted).
- **Error:** with `fcnMode="Disabled"` or `"Single"`, F97's repro fails at the same folder as without the attribute,
  in `DirMonCompletion.StartManagedDirectoryWatch`. `FileChangesMonitor` has the code for both modes, and
  `ApplicationManager.Core.cs` reads the setting, but the worker's monitor doesn't get it.
- **Repro:** F97's page with `<httpRuntime targetFramework="4.8" fcnMode="Disabled" />` (#146).

## F99

**web.config's `<runtime><assemblyBinding><probing privatePath>` isn't applied: `Assembly.Load` doesn't find the
assemblies there.** webForCore bug. Raised as [#147](https://github.com/xforcore/webForCore/issues/147). Found on 0.8.0,
migrating Orchard CMS 1.10.4.

- **Apps:** orchard-1.10: Orchard copies each module's assembly to `App_Data/Dependencies` and loads it by name, with
  `<probing privatePath="App_Data/Dependencies" />`. No module loaded (`FileNotFoundException: Could not load file or
  assembly 'Orchard.Setup'`), and the setup shell failed (`DependencyResolutionException: ... Cannot resolve parameter
  'Orchard.IWorkContextAccessor'`).
- **Cause:** on .NET Framework the application's AppDomain had web.config as its configuration file, and fusion probed
  `privatePath` as well as `bin`. webForCore's resolver (`AssemblyLoaderNetCore.Resolve`) looks in `bin_dotnet` and
  the compilation folder only.
- **Workaround in the overlay** (so the app's later stages could be tested; it doesn't count as a pass): an
  `AppDomain.AssemblyResolve` handler in `Global.asax.cs` that loads `App_Data/Dependencies/<name>.dll`.
- **Repro:** a library only in `App_Data/Dependencies`, that web.config's `<probing>`, and a page that calls
  `Assembly.Load("ReproLib")` (#147).

## F100

**Autofac.Configuration 3.x, Castle.Core 3.x and NHibernate 4.x don't run on .NET.** Guide gap (the libraries aren't
webForCore's; MIGRATING.md section 3's "Libraries that don't run on .NET" could list them). Raised as
[#148](https://github.com/xforcore/webForCore/issues/148). Found on 0.8.0, migrating Orchard CMS 1.10.4.

- **Apps:** orchard-1.10 (all three).
- **Errors:** Autofac.Configuration 3.3.0 reading a file: `MissingMethodException: ... AppDomainSetup.get_ConfigurationFile()`.
  Castle.Core 3.3.1's proxies and NHibernate 4.1.2's lazy-loading proxies: `MissingMethodException: ...
  AppDomain.DefineDynamicAssembly(...)` (NHibernate: "Creating a proxy instance failed", then "illegal access to loading
  collection"); NHibernate 4.1's SQL Server batcher: `TypeInitializationException` in `SqlClientSqlCommandSet`.
- **What worked:** `SectionHandler.Deserialize(XmlReader)` in a `ConfigurationModule` of the app's own (Autofac 4,
  which Autofac.Configuration 4 needs, is a bigger change); Castle.Core 4.1.1 (the last before `ILogger.Trace`);
  NHibernate 5.1.7 with FluentNHibernate 2.1.2 and Iesi.Collections 4.0.4 (a few API changes: NOTES.md, step 7). With
  them Orchard builds and its setup creates its schema on SQL Server, batching included.
- **Next problem (Windows, CI run 41924348101):** Orchard's prebuilt `NHibernate.Linq` (the old separate LINQ provider)
  calls `IEntityPersister.GetConcreteProxyClass(EntityMode)`, which NHibernate 5 removed, so the Core recipe's first content
  query throws `MissingMethodException` and the tenant stays "initializing" (503). Orchard's queries need moving to
  NHibernate 5's built-in LINQ.

## F101

**`System.Data.Services.Client` (WCF Data Services' client) isn't on .NET, and the guide doesn't say what to use.**
Guide gap. Raised as [#149](https://github.com/xforcore/webForCore/issues/149). Found on 0.8.0, migrating Orchard CMS
1.10.4.

- **Apps:** orchard-1.10 (Orchard.Packaging's generated gallery proxy: `CS0234: The type or namespace name 'Services'
  does not exist in the namespace 'System.Data'`).
- **What worked:** the `Microsoft.Data.Services.Client` 5.8.4 package (.NET Standard 1.1), which keeps the
  `System.Data.Services.Client` namespace, so the generated code compiles unchanged. The gallery feed itself wasn't
  exercised.

## F102

**Plug-in class libraries built into their own folder lose their NuGet libraries.** Guide gap. Raised as
[#150](https://github.com/xforcore/webForCore/issues/150). Found on 0.8.0, migrating Orchard CMS 1.10.4.

- **Apps:** orchard-1.10: modules build into `Modules/<id>/bin`, which the web project doesn't reference.
  `Could not load file or assembly 'Microsoft.Data.Services.Client, Version=5.8.4.0'` when Orchard.Packaging loaded.
- **Cause:** a .NET Framework project copied its packages.config references to its output; an SDK class library
  doesn't copy its packages' assemblies (`CopyLocalLockFileAssemblies` is false), and setting it copies webForCore's
  hundreds of assemblies too.
- **What worked:** `CopyLocalLockFileAssemblies` with a target that keeps only the plug-in's own packages in
  `ReferenceCopyLocalPaths` (#150 has it), and `Private="false"` on references to the host's own libraries.

## F103

**Orchard's run-time module compilation parses the old `.csproj` format, so it can't read the SDK projects.**
App/environment. Found on 0.8.0, migrating Orchard CMS 1.10.4.

- **Apps:** orchard-1.10: with the modules' projects converted in place, Orchard's `DynamicExtensionLoader` fails at
  every start (`InvalidOperationException: Sequence contains no elements` in `DefaultProjectFileParser.GetAssemblyName`,
  which reads MSBuild 2003's `<AssemblyName>`).
- **What the overlay does:** turns that loader off in `Config/HostComponents.config` (Orchard's own switch; a deployed
  Orchard has no project files either), so the modules load from their prebuilt `bin` folders. The consequence for
  this test bed: Orchard's own run-time compilation of modules through `BuildManager` and its `.csproj` build provider
  isn't exercised (its Razor views still compile at run time).

## F104

**webForCore.AutoMapperLegacy on AutoMapper 12: a member mapped from a value that already has the member's type is
mapped again, through a map between base types of its elements, and the whole configuration fails.** App/environment
for webForCore (it isn't webForCore's: AutoMapper 12's behaviour, which webForCore-thirdparty's compatibility package
passes through). Raised as
[xforcore/webForCore-thirdparty#11](https://github.com/xforcore/webForCore-thirdparty/issues/11). Found on webForCore
0.8.0 with webForCore-thirdparty 0.6.0 (Linux).

- **Apps:** umbraco-7. `ContentTypeModelMapperExtensions.MapPropertyGroupBasicToPropertyGroupDisplay` maps
  `PropertyGroupBasic<MemberPropertyTypeBasic>` to `PropertyGroupDisplay<MemberPropertyTypeDisplay>` with
  `.ForMember(g => g.Properties, o => o.MapFrom(s => s.Properties.Select(Mapper.Map<MemberPropertyTypeDisplay>)))`, and
  there's a `PropertyTypeBasic -> PropertyTypeDisplay` map (`MemberPropertyTypeDisplay : PropertyTypeDisplay :
  PropertyTypeBasic`). AutoMapper 3.3.1 assigned the elements (no map for the element type to itself). AutoMapper
  12.0.1 maps the `IEnumerable<MemberPropertyTypeDisplay>` again, element by element, picks the base-type map, and
  can't add its `PropertyTypeDisplay` to the `List<MemberPropertyTypeDisplay>`.
- **Error:** at the first `Mapper.Map` (the package builds one AutoMapper 12 configuration from every registered map,
  on first use), whatever it maps: in Umbraco, `BackOfficeUserStore.FindByIdAsync` in the installer's last step, which
  then fails on every retry.
  ```
  System.ArgumentException: Expression of type 'Umbraco.Web.Models.ContentEditing.PropertyTypeDisplay' cannot be used for parameter of type 'Umbraco.Web.Models.ContentEditing.MemberPropertyTypeDisplay' of method 'Void Add(Umbraco.Web.Models.ContentEditing.MemberPropertyTypeDisplay)' (Parameter 'arg0')
     at AutoMapper.Internal.Mappers.CollectionMapper.<MapExpression>g__MapCollectionCore|3_1(...)
     ...
     at AutoMapper.TypeMap.Seal(IGlobalConfiguration configuration)
     at AutoMapper.MapperConfiguration..ctor(Action`1 configure)
     at AutoMapperLegacy.RootConfiguration.Build()
     at AutoMapperLegacy.Mapper.get_Current()
     at AutoMapperLegacy.Mapper.Map[TDestination](Object source)
  ```
- **Repro** (`dotnet run`, .NET 10, `#:package webForCore.AutoMapperLegacy@0.6.0`): `Mapper.CreateMap<PropertyBasic,
  PropertyDisplay>()`, `Mapper.CreateMap<MemberPropertyBasic, MemberPropertyDisplay>()`,
  `Mapper.CreateMap<GroupBasic<MemberPropertyBasic>, GroupDisplay<MemberPropertyDisplay>>().ForMember(d =>
  d.Properties, o => o.MapFrom(s => s.Properties.Select(Mapper.Map<MemberPropertyDisplay>)))` and an unrelated
  `Mapper.CreateMap<Unrelated, UnrelatedDto>()`; `Mapper.Map<UnrelatedDto>(new Unrelated())` throws the error above.
  `ResolveUsing` instead of `MapFrom` fails the same way. Plain AutoMapper 12.0.1 (no facade) throws building the same
  configuration. With AutoMapper 13.0.1 or 14.0.0 (also in the package's range) the configuration builds, but mapping
  the member throws `NotSupportedException: Specified method is not supported` (plain 14.0.0 too).
- **What worked:** in the app, `Ignore()` the member and assign it in an `AfterMap` (`dest.Properties =
  source.Properties.Select(Mapper.Map<TPropertyTypeDisplay>).ToList()`): one change in Umbraco 7. Umbraco then installs,
  and its back-office API (member types included) answers.
- **Suggested fix (webForCore-thirdparty):** have the facade assign a resolved value that is already of the destination
  member's type, as 3.x did, or document the rewrite under "What an application may still have to change"; and say in
  the README that one faulty map makes every map fail.

## F105

**`ApplicationSettingsBase` throws `TypeLoadException`, so a Visual Studio settings class (`Settings.settings`, and every
"Web Reference" SOAP client, which reads its URL from one) can't read a setting.** webForCore bug. Raised as
[#154](https://github.com/xforcore/webForCore/issues/154). Found on 0.8.0 (Linux).

- **Apps:** umbraco-7: `InstallHelper.InstallStatus`, in the installer's last step, creates
  `org.umbraco.update.CheckForUpgrade` (a Web Reference client), whose constructor reads
  `Settings.Default.umbraco_org_umbraco_update_CheckForUpgrade`. Umbraco catches and logs it, so the install goes on;
  the back office's upgrade check uses the same client.
- **Error:**
  ```
  System.TypeLoadException: Could not resolve type 'System.Configuration.Internal.ConfigurationManagerInternal' in assembly 'System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'.
     at System.Type.GetType(String typeName, Boolean throwOnError)
     at System.Configuration.TypeUtil.CreateInstanceWithReflectionPermission(String typeString)
     at System.Configuration.ConfigurationManagerInternalFactory.get_Instance()
     at System.Configuration.ApplicationSettingsBase.get_Initializer()
     ...
     at Umbraco.Web.Properties.Settings.get_umbraco_org_umbraco_update_CheckForUpgrade()
  ```
- **Cause:** webForCore's System.Configuration is the reference source's, in the assembly
  `System.Configuration.ConfigurationManager` 14.5.0.0. Its `ConfigurationManagerInternalFactory` creates
  `ConfigurationManagerInternal` by name, `"System.Configuration.Internal.ConfigurationManagerInternal, " +
  AssemblyRef.SystemConfiguration`; on .NET `System.Configuration` 4.0.0.0 is the runtime's forwarding facade, which
  doesn't have the (internal) type. .NET Framework returns the setting.
- **Repro:** a console app (or a page) with the `webForCore` 0.8.0 package and `class Settings :
  ApplicationSettingsBase { [ApplicationScopedSetting, DefaultSettingValue("http://example.com/")] public string Url =>
  (string)this["Url"]; }`: `new Settings().Url` throws.
- **Suggested fix:** create the type directly, or name webForCore's own assembly in the type string.

## F106

**webForCore's BinaryFormatter port can't serialize a `System.Type`.** webForCore bug. Raised as
[#156](https://github.com/xforcore/webForCore/issues/156). Found on 0.8.0, in orchard-1.10's Windows CI run 41924348101.

- **Apps:** orchard-1.10. `Orchard.Data.SessionConfigurationCache` (switched to
  `WebFormsForCore.Serialization.Formatters.Binary.BinaryFormatter`, as MIGRATING.md says) caches NHibernate's
  `Configuration` in `App_Data`. Each start logs "Error storing new NHibernate cache configuration" and rebuilds it.
- **Error:** `SerializationException: Type 'System.RuntimeType' in Assembly 'System.Private.CoreLib, ...' is not marked as
  serializable.`
- **Cause:** on .NET Framework, `RuntimeType` serializes itself as a `System.UnitySerializationHolder`. On .NET it isn't
  serializable, and the port has no surrogate for it.
- **Repro:** a `[Serializable]` class with a `Type` field, serialized with the port on the 0.8.0 packages (#156).

## F116

**Windows Workflow Foundation 3 (`System.Workflow.Runtime`, `.ComponentModel`, `.Activities`) isn't on .NET, and
neither `docs/NAMESPACES.md` nor the guide's blocker search mentions it.** Guide gap. Raised as
[#132](https://github.com/xforcore/webForCore/issues/132). Found on 0.8.0 (Linux and Windows: a build error).

- **Apps:** composite-c1 (C1 CMS 6.13). `Composite.dll` has 48 files on WF3 (`WorkflowFacade`, the console's flow
  controllers, `FilePersistenceService`, the state machine base classes), and `GlobalInitializerFacade` starts the
  workflow runtime at application start, so the public site needs it too. `Composite.Workflows.dll` (282 files) is
  WF3 state machine workflows: every action in the admin console, and the data type and function editors.
- **Error:** `CS0234: The type or namespace name 'Workflow' does not exist in the namespace 'System'` (105 times),
  then `CS0246` for `Activity`, `StateActivity`, `SetStateActivity`, `WorkflowRuntime`, `WorkflowInstance`,
  `DependencyProperty`, `HandleExternalEventActivity`, `ExternalDataExchangeService`, ... (about 450 errors in
  `Composite.dll` alone).
- **Why it can't be referenced:** WF3's assemblies exist only in .NET Framework's GAC on Windows (no package), and
  there's no port: CoreWF is WF4 (`System.Activities`), a different API. Getting C1 past this is a rewrite of its
  console's actions, not a migration.
- **Also:** `scripts/wfc.cs assess` doesn't look for `System.Workflow` either (its patterns follow the guide's table),
  so it reported C1 without this blocker.
- **Suggested docs:** a row in NAMESPACES.md's summary (WF3 `System.Workflow.*`, and WF4 `System.Activities`), and
  `System.Workflow`/`System.Activities` in AGENT-MIGRATION.md's Phase 0 search, as for Windows Identity Foundation
  (#119).

## F117

**Application code that compiles C# at run time through System.CodeDom gets `PlatformNotSupportedException`, and the
docs don't say what to use instead.** Guide gap. Raised as [#133](https://github.com/xforcore/webForCore/issues/133).
Found on 0.8.0, on Linux (it is .NET's System.CodeDom, the same on Windows).

- **Apps:** composite-c1 (what it would hit after F116): `CodeGenerationManager` compiles the data types' classes at
  application start with `(CSharpCodeProvider)CodeDomProvider.CreateProvider("c#")` and `CompileAssemblyFromDom`, and
  `CodeCompatibilityChecker` compiles App_Code to check it. On IIS its web.config's `<system.codedom>` gave it
  `Orckestra.AspNet.Roslyn.CSharpCodeProvider`; .NET's System.CodeDom reads no configuration.
- **Repro:** a page in a minimal site:
  `CodeDomProvider.CreateProvider("c#").CompileAssemblyFromSource(new CompilerParameters { GenerateInMemory = true }, "public class G {}")`
  → provider `Microsoft.CSharp.CSharpCodeProvider, System.CodeDom, Version=10.0.0.0`,
  `System.PlatformNotSupportedException: Operation is not supported on this platform.`
- **What webForCore has:** `WebFormsForCore.CodeDom.Compiler.CSharpCodeProvider`, which compiles with Roslyn in
  process, but it derives from webForCore's own `CodeDomProvider`, not System.CodeDom's, so code typed against
  System.CodeDom can't get it, and it isn't documented for applications. Used directly it fails in memory (F119).

## F118

**CoreWCF's source generator reaches every application, and fails the build of any project with a `[ServiceContract]`
and a `LangVersion` below 11.** webForCore bug. Raised as [#134](https://github.com/xforcore/webForCore/issues/134).
Found on 0.8.0 (Linux; a build error, so the same on Windows).

- **Apps:** composite-c1: `Composite.csproj` keeps its `<LangVersion>7</LangVersion>` (`docs/MIGRATING.md` section 2),
  and its WCF log service's contract gets `CS8107: Feature 'file types' is not available in C# 7.0` and
  `'module initializers'` in `obj/.../CoreWCF.BuildTools.OperationInvokerGenerator/OperationInvoker.g.cs`.
- **Cause:** `webForCore.Web.Extensions` depends on `CoreWCF.Http` and `CoreWCF.Primitives` with
  `exclude="Build,Analyzers"`, but `CoreWCF.Http`'s own dependency on `CoreWCF.Primitives` excludes nothing, so its
  analyzers and `buildTransitive` targets flow to the app; the targets turn the generator on for net7.0 and later.
- **Repro:** the minimal site with `<LangVersion>7.3</LangVersion>` and a `[ServiceContract] interface` with one
  `[OperationContract]` and a class implementing it: 3 × `CS8370 'file types'`, 1 × `'module initializers'`.
- **Workaround in the overlay (CoreWCF's documented switch, not webForCore's internals):**
  `<EnableCoreWCFOperationInvokerGenerator>false</EnableCoreWCFOperationInvokerGenerator>` in the two libraries.

## F119

**webForCore's `CSharpCodeProvider` with `GenerateInMemory = true` and no debug information throws
`ArgumentNullException: Value cannot be null. (Parameter 'buffer')`.** webForCore bug. Raised as
[#135](https://github.com/xforcore/webForCore/issues/135). Found on 0.8.0, on Linux, checking what C1 could use instead
of System.CodeDom's provider (F117).

- **Apps:** none yet (composite-c1's `CodeCompatibilityChecker` compiles in memory, but C1 doesn't build: F116).
- **Cause:** `Compiler.FromFileBatch` (`src/WebFormsForCore.Web/WebFormsForCore/CodeDom/Compiler.cs`) leaves
  `symbolsBuff` null when there's no PDB, then calls `alc.LoadFromStream(new MemoryStream(assemblyBuff), new
  MemoryStream(symbolsBuff))` in its .NET branch. .NET Framework's `Assembly.Load(bytes, null, evidence)` accepts it.
- **Repro:** `new WebFormsForCore.CodeDom.Compiler.CSharpCodeProvider().CompileAssemblyFromSource(new CompilerParameters
  { GenerateInMemory = true, CompilerOptions = "" }, "public class G {}")`. With `CompilerOptions` left null it fails
  earlier, with a `NullReferenceException` in `CSharpCompiler.FixUpCompilerParameters` (code from Microsoft's
  RoslynCodeDomProvider).

## F120

**C1 CMS's core needs LINQ to SQL, even with its XML data store, and hosts a WCF REST service.** Documented gap
(`docs/NAMESPACES.md`: LINQ to SQL, WCF hosting). Not raised. Found on 0.8.0.

- **Apps:** composite-c1. Behind F116's errors, `Composite.dll` has 10 more files that don't build:
  - LINQ to SQL (`System.Data.Linq`): the SQL Server data provider (`MSSqlServerDataProvider`, expected), but also the
    data layer's query pipeline (`DataFacadeQueryableExpressionVisitor` recognises `System.Data.Linq.Table<T>` queries
    by type and reads their `DataContext`), and the run-time compilation of every data type references
    `System.Data.Linq.Mapping.TableAttribute`'s assembly (`CodeGenerationCommon.AddCommonAssemblies`). So the XML data
    store, which the starter sites use, still needs System.Data.Linq.
  - WCF REST hosting: `Composite/services/LogService/LogService.svc` (`System.ServiceModel.Web`'s `[WebInvoke]`,
    `System.ServiceModel.Activation`'s `[AspNetCompatibilityRequirements]`, a `<services>` entry in
    `Composite/web.config`): the console's log viewer.
- **Also:** `System.Configuration.Install` (an `installutil` installer for performance counters) isn't on .NET; the
  overlay leaves that class out (`#if NETFRAMEWORK`), as it isn't used by the site.

## F121

**A type .NET added makes the application's own type of the same name ambiguous: `CS0104: 'KeyValuePair' is an
ambiguous reference between 'Composite.Core.Types.KeyValuePair' and 'System.Collections.Generic.KeyValuePair'`.**
Guide gap. Raised as [#136](https://github.com/xforcore/webForCore/issues/136). Found on 0.8.0 (a build error, any OS).

- **Apps:** composite-c1: 12 files in `Composite.dll` import both `System.Collections.Generic` and
  `Composite.Core.Types`. .NET Core 2.0 added a non-generic static `System.Collections.Generic.KeyValuePair`
  (`KeyValuePair.Create`); .NET Framework 4.8 has only the generic struct.
- **Fix in the overlay:** `using KeyValuePair = Composite.Core.Types.KeyValuePair;` in each file (an alias has no type
  parameters, so `KeyValuePair<TKey, TValue>` still binds to .NET's).
- **Docs:** MIGRATING.md section 2 covers the method case (`DistinctBy`, CS0121, #93) and webForCore's newer
  versions' types (`SignInStatus`, #17), not types .NET added.
- **smartstorenet** (round 7): its own `SmartStore.ComponentModel.ReferenceEqualityComparer` against .NET 5's `System.Collections.Generic.ReferenceEqualityComparer`, in two files; an alias in each.

## F123

**A Web Site Project has no `.csproj`, and the guide's project template compiles its `CodeFile=` code files
(CS0103).** Guide gap. Raised as [#131](https://github.com/xforcore/webForCore/issues/131). Found on 0.8.0 (Linux),
assessing rock-rms (rejected for its licence, so not migrated).

- **Apps:** rock-rms: `RockWeb` is a Visual Studio web site (`{E24C65DC-...}` in `Rock.sln`), with 364 `CodeFile=`
  pages and controls and no `CodeBehind=`, `Global.asax`'s class in `App_Code`, and a `Bin` of 165 `*.dll.refresh`
  files plus the assemblies the solution's libraries copy there after building (`CopyToRockWeb`).
- **The gap:** `docs/MIGRATING.md` section 2 and `docs/AGENT-MIGRATION.md` Phase 2 convert an existing `.csproj`, and
  the agent guide's source of truth for `Compile` is "the old project's `<Compile Include>` list", which a web site
  hasn't got. The template removes only `App_Code\**` from `Compile`, so the SDK's glob compiles every `CodeFile=` file
  into the project, where its controls aren't declared: `error CS0103: The name 'lbl' does not exist in the current
  context`, and App_Code's classes aren't there either.
- **Repro:** section 2's project and section 4's `Program.cs` in a folder with `Default.aspx` (`CodeFile=
  "Default.aspx.cs" Inherits="DefaultPage"`, an `asp:Label ID="lbl"`), its `Default.aspx.cs` setting `lbl.Text` from
  `App_Code/Helper.cs`; `dotnet build`. With `<Compile Remove="**\*.cs" />` and `<Compile Include="Program.cs" />` it
  builds, and under `webforcore-host` the page renders with App_Code's text: webForCore runs the site, only the guide
  is missing.
- **Suggested fix:** a "Web Site Project" paragraph in both guides: create the project in the site folder compiling only
  `Program.cs`; turn `Bin\*.dll.refresh` files into `PackageReference`s and the solution's copy-to-`Bin` projects into
  `ProjectReference`s (their copy targets go: the assemblies belong in `bin_dotnet/`); remove the old `Bin` DLLs.

## F130

**`BuildManager.GetType(name, throwOnError: false)` throws `FileNotFoundException` for an assembly that isn't there;
.NET Framework returns `null`.** webForCore bug. Raised as [#137](https://github.com/xforcore/webForCore/issues/137).
Found on 0.8.0 (Linux; the code is platform-independent), with DNN Platform 9.13.10.

- **Apps:** dnn-platform-9: every request is a 500 in `HttpApplication.InitModules`, the static files included, so the
  install can't start (7/11, the forbidden paths only). Twice before DNN's first page:
  - ClientDependency 1.9.10 (`Dnn.ClientDependency`; Umbraco ships the same library): `ClientDependencyModule.Init`
    loads its default filters with `BuildManager.GetType(type, false, true)`, one of them
    `ClientDependency.Core.Mvc.MvcFilter, ClientDependency.Core.Mvc`, which DNN doesn't deploy;
  - DNN's `ProviderInstaller.InstallProvider` (`Library/ComponentModel/ProviderInstaller.cs:86`), for every provider
    in web.config, skipping a null type: DNN's default web.config names `DNNConnect.CKEditorProvider` (an optional
    package) and the ADSI provider of `DotNetNuke.Authentication.ActiveDirectory` (not part of the platform).
- **Error:** `FileNotFoundException: Could not load file or assembly 'ClientDependency.Core.Mvc, Culture=neutral,
  PublicKeyToken=null'` at `AssemblyLoadContext.LoadFromAssemblyName` ← `BuildManager.<>c__DisplayClass144_0.<GetType>b__0`
  ← `Type.GetType(String, Func, Func, Boolean, Boolean)` ← `BuildManager.GetType(String, Boolean, Boolean)`.
- **Cause:** `BuildManager.GetType` passes `alc.LoadFromAssemblyName` as `Type.GetType`'s assembly resolver.
  `LoadFromAssemblyName` throws when the assembly isn't found, and `Type.GetType` lets a resolver's exception through
  whatever `throwOnError` is; a resolver should return `null` for "not found".
- **Repro:** a page `<%= System.Web.Compilation.BuildManager.GetType("Missing.Type, Missing.Assembly", false) == null ? "null" : "found" %>`:
  `null` on IIS, `FileNotFoundException` on webForCore.
- **Suggested fix:** a resolver that returns `null` for `FileNotFoundException` (so `Type.GetType` returns `null`, or
  throws its own exception when `throwOnError` is true); `FileLoadException` and `BadImageFormatException` still throw.

## F131

**`Thread.ResetAbort()` after `catch (ThreadAbortException)` throws `PlatformNotSupportedException`; the guide lists it
but doesn't say to remove it.** Guide gap. Raised as [#143](https://github.com/xforcore/webForCore/issues/143). Found on
0.8.0, with DNN Platform 9.13.10.

- **Apps:** dnn-platform-9, in five places: its URL rewriter's top-level `catch` (`Library/Entities/Urls/AdvancedUrlRewriter.cs:2615`,
  every request it redirects), `Exceptions.ThreadAbortCheck` (`Library/Services/Exceptions/Exceptions.cs:422`), module
  loading (`Library/UI/Modules/ModuleHost.cs:345`), `ProfileModuleUserControlBase` and the module settings page. Found by
  the assessment; not reached at run time (F130 comes first), as .NET throws on every `ResetAbort` call.
- **Error:** `PlatformNotSupportedException: Thread abort is not supported on this platform.` from inside the `catch`, in
  place of the `ThreadAbortException` the code swallowed.
- **Gap:** AGENT-MIGRATION.md's blocker table says only "Different on .NET", NAMESPACES.md that such code "behaves
  differently". On webForCore the call can go: the `ThreadAbortException` of `Response.End`/`Redirect(url, true)` is an
  ordinary instance, not re-raised at the end of the `catch`. The overlay removes the five calls.
- **Suggested fix:** a line in Phase 2 step 7 and MIGRATING.md section 6 ("remove `Thread.ResetAbort()` after
  `catch (ThreadAbortException)`"), and the symptom in the Phase 3 table.

## F132

**An application's design-time classes don't compile: `System.Web.UI.Design.ExpressionEditor` and the rest of
System.Design aren't on .NET.** Guide gap. Raised as [#144](https://github.com/xforcore/webForCore/issues/144). Found on
0.8.0, with DNN Platform 9.13.10.

- **Apps:** dnn-platform-9: `Library/Services/Localization/LocalizationExpressionBuilderEditor.cs`, the Visual Studio
  editor of its `dnnLoc:` expression builder, derives from `ExpressionEditor`: `CS0234: The type or namespace name
  'Design' does not exist in the namespace 'System.Web.UI'`. The overlay takes it out of the build;
  `[ExpressionEditor("...")]` names it by string, so nothing at run time changes.
- **Gap:** MIGRATING.md mentions System.Design only as a `<compilation><assemblies>` entry (F37). Control designers
  (`ControlDesigner`, `DataSourceDesigner`) and System.Design's editors in app libraries are the same case.
- **Suggested fix:** a line in MIGRATING.md section 2 or 6: remove the design-time classes from the build; attributes
  that name them by string keep working.

## F133

**Linux: DNN builds every physical path with `\`, and its file utilities use QuickIO.NET's Win32 calls.**
App/environment. Found on 0.8.0 (Linux), with DNN Platform 9.13.10, past F130 (with F130 worked around locally, not in
the overlay).

- **Apps:** dnn-platform-9. `Globals.ApplicationMapPath` is `AppDomain.CurrentDomain.BaseDirectory.Replace("/", "\\")`
  (`Library/Application/ApplicationStatusInfo.cs`, with `HttpRuntime.AppDomainAppPath` in the overlay for F3), and
  `HostMapPath` (`@"Portals\_default\"`), `InstallMapPath` (`"\\Install\\"`), the providers' `providerPath="~\..."`, the
  skin and portal paths add `\`-separated parts to it: about 280 such literals in the projects the site builds.
  `Install.aspx?mode=install` wrote a `web.config` into a folder named `\home\...\Website\Config\` inside the site, and
  the error page then failed in `Config.Load` (`SecurityException: Unable to load config for "web.config"`).
  `FileSystemUtils` aliases `Directory`, `DirectoryInfo` and `File` to QuickIO.NET 2.6.2's (`SchwabenCode.QuickIO`,
  net40, P/Invoke to `kernel32`).
- **Why it isn't webForCore's:** `\` isn't a separator on Linux (MIGRATING.md section 6). Portable paths would mean
  rewriting DNN's file layer, so the app runs on Windows only (where it stops at F130 for now).

## F154

**A library's relative paths resolve against `AppDomain.CurrentDomain.BaseDirectory`, which is `bin_dotnet/`: log4net
writes its log there, and each write restarts the application.** Guide gap. Found on 0.9.0 (and in round 0.8.0's
logs), on Linux; the cause is the same on Windows.

- **Apps:** mojoportal. `Global.asax.cs` has `[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config",
  Watch = true)]`, and `log4net.config`'s `RollingFileAppender` has `<file value="Data/currentlog.config" />`. log4net
  resolves both against `AppDomain.CurrentDomain.BaseDirectory` (`SystemInfo.ApplicationBaseDirectory`): on IIS the
  site, here `bin_dotnet/` (the build copies `log4net.config` there, so the configuration is found).
- **What happens:** the log goes to `bin_dotnet/Data/currentlog.config`. `bin_dotnet` is the application's `bin`, so
  each write is a `BinDirChangeOrDirectoryRename` and the application restarts, as IIS would for a file written into
  `bin`. mojoPortal logs "Application Started" when it starts, so it restarts in a loop: 14 restarts in one smoke run
  (16 in round 0.8.0's committed server log), the probes served by whichever worker was up. Session state goes with
  each restart, and starting a worker a second takes inotify instances on a shared machine (a by-hand run ended with
  "The configured user limit (128) on the number of inotify instances has been reached" and 503s). The admin's Server
  Log page (`~/Data/currentlog.config`) showed nothing.
- **What worked (overlay):** `<file value="../Data/currentlog.config" />`, the site's `Data` folder, as on IIS. No more
  restarts (0 in a run of 49 probes), and `/Admin/ServerLog.aspx` shows the log (a probe).
- **Guide:** MIGRATING.md section 6 and NAMESPACES.md say `BaseDirectory` is `bin_dotnet/` and to find the site's
  files with `HttpRuntime.AppDomainAppPath`; they don't say that libraries resolve their relative paths there too
  (log4net's `ConfigFile` and appender files; NLog's and Enterprise Library's are the same kind), nor that a file
  written there restarts the application. A line for each, with log4net's fix, would have found it.

## F155

**Web Pages 1 sites meet Razor 3's stricter parser.** Guide gap. Found on 0.9.0, on Linux (the parser is the same on
Windows).

- **Apps:** rabbit-webpages (WebMatrix 1.0, 2011): `Views/Tags/_Page_Edit.cshtml`, the Pages module's editor, has
  ```cshtml
  @if (Page.Tags != null && Page.Tags.Length > 0)
  {
      @string.Join(", ", Page.Tags)
  }
  ```
- **Error:** `/Pages/Edit/About`: `Parser Error Message: Unexpected "string" keyword after "@" character. Once inside
  code, you do not need to prefix constructs like "string" with "@".` Web Pages 1's Razor accepted it (the app shipped
  this editor); Razor 2 and 3 tokenize every C# keyword and refuse one after `@` in a code block. webForCore has Razor
  3 only, so a Web Pages 1 or 2 site runs on 3, as the guide's MVC 3 and 4 section says for `webpages:Version`.
- **What worked (overlay):** `@(string.Join(", ", Page.Tags))`. The editor renders, and its AJAX save, a read back
  as JSON and the tag page pass (probes).
- **Guide:** the MVC 3/4 step in MIGRATING.md (#122) covers the version setting; Razor 1's parsing differences (this
  one: `@` before a C# keyword inside code) aren't mentioned, nor the fix.

# SignalR 2 samples: migration notes

Status: blocked by F70 (webForCore#118): 18/19 probes on Linux and on Windows, on webForCore 0.7.0 and 0.8.0. The one failure is a
webForCore bug in the hub proxy's header comment.

SignalR/SignalR at tag `2.4.3` (`7f53f266`), Apache-2.0 (`LICENSE.txt`). `samples/Microsoft.AspNet.SignalR.Samples`:
SignalR's own sample site. Web Forms pages on a master page (one per sample), 24 hubs (one in a VB.NET class library,
`samples/Microsoft.AspNet.SignalR.Samples.VB`), four persistent connections, OWIN branches with `Microsoft.Owin.Cors`
(`/cors`) and cookie authentication (`/cookieauth`), a hub pipeline module, a background thread broadcasting through
`GlobalHost.ConnectionManager`, and static HTML samples. webForCore 0.7.0 packages (0.8.0 wasn't released when this
was migrated), webForCore-thirdparty 0.6.0 (not used by the app), drawingForCore 0.2.0.

## Assessment

`scripts/wfc.py assess`: "blocked (appdomain-thread-abort)" at `src/Microsoft.AspNet.SignalR.Utils/Commands/GenerateHubProxyCommand.cs:90`.
That is `signalr.exe`, the build-time tool the sample's `AfterBuild` ran to write `Scripts/hubs.js`, not the site:
not a blocker (it goes with the tool, below). No blockers in the site. SignalR + OWIN + Web Forms, .NET Framework 4.5
(`<httpRuntime targetFramework="4.5">`).

- (a) replaced by webForCore: SignalR itself (the sample referenced the repository's `Microsoft.AspNet.SignalR.Core`
  project), Microsoft.Owin, .Host.SystemWeb, .Security, .Security.Cookies, .Cors 3.0.0, Owin, Microsoft.AspNet.Cors
  5.2.2;
- (b) kept: Newtonsoft.Json 11.0.2 (→ 13.0.4); the client script `jquery.signalR.js`, which the original build copied
  from the repository's `Microsoft.AspNet.SignalR.JS` project, now from Microsoft's `Microsoft.AspNet.SignalR.JS` 2.4.3
  package (the same release);
- content-only script packages (jQuery 1.8.2, jQuery UI, jQuery Mobile, Bootstrap 2.1.1, json2, ...): their files are
  committed under `Scripts/` and `Content/`, so they aren't referenced.

The `web.config` connection strings (SQL Server scale-out) are only used by commented-out code. The repository's root
`global.json` (SDK 2.1.400 and KoreBuild's SDK) and `Directory.Build.props` (KoreBuild/Arcade imports, strong naming,
warnings as errors) are for building SignalR itself.

No IIS reference responses; SignalR's protocol responses are checked against what the 2.4.3 client expects.

## Migration

- `scripts/convert.py`: `Microsoft.AspNet.SignalR.Samples.csproj` → `.csproj.netfx`; SDK project with `webForCore`,
  `webForCore.Web.Cors`, `webForCore.Microsoft.Owin.Host.SystemWeb`, `.Owin.Security.Cookies` and `.Owin.Cors` 0.7.0;
  the linked `AuthenticatedEchoConnection.cs` from `test/Microsoft.AspNet.SignalR.Tests.Common` kept (as
  `Connections/AuthenticatedEchoConnection.cs`, its old link); `packages.config` → `.netfx`; `Program.cs` (no session
  state). The `<Compile>` list otherwise equals the glob.
- By hand, in the web project:
  - the `Microsoft.AspNet.SignalR.Core` project reference → `webForCore.Microsoft.AspNet.SignalR.SystemWeb` 0.7.0; the
    order-only references to the `.JS` and `.Utils` projects removed;
  - the content-only script packages dropped (above);
  - `CopyJs` kept, copying `jquery.signalR-2.4.3(.min).js` from the `Microsoft.AspNet.SignalR.JS` 2.4.3 package
    (`ExcludeAssets="all"`, `GeneratePathProperty`) to `Scripts/jquery.signalR(.min).js`, as it copied the
    repository's build. The repository ignores those files; `generated` in `app.toml` keeps them out of the patch;
  - `AfterBuild`'s `signalr.exe ghp` (writes `Scripts/hubs.js` from the built site; a .NET Framework tool in this
    repository) dropped. Only `Hubs/RealtimeBroadcast/index.html` uses `Scripts/hubs.js`; every other page uses the
    proxy the server generates (`/signalr/hubs`). That page's script is a 404 now.
- `samples/Microsoft.AspNet.SignalR.Samples.VB`: rewritten as an SDK-style `net10.0` VB class library (the old one
  kept as `.vbproj.netfx`): `webForCore` and `webForCore.Microsoft.AspNet.SignalR.Core` 0.7.0 instead of the Core
  project reference, `FrameworkReference Microsoft.AspNetCore.App` (class libraries that reference webForCore), its
  `Option*` settings, `<Import Include>` namespaces and `NoWarn` kept, `GenerateAssemblyInfo` false (`My Project/AssemblyInfo.vb`).
  `MyType` `Windows` → `Empty`: `Windows` fails with `BC30002` for `My.Computer`, `My.User` and `My.Application`, which
  need the Windows desktop on .NET; the library doesn't use them ([F71](../../FINDINGS.md#f71)).
- `Directory.Build.props` (empty) in both project folders, so the repository's own build settings stay out, and
  `global.json` in the site folder (SDK 10.0.100, `latestFeature`), so the root one (SDK 2.1.400) doesn't apply when
  building from there.
- Nothing else; `web.config` and the app's code are unchanged.

The build passes.

## Smoke

Round 7 candidates (webForCore 0.7.0): **18/19 on Linux and on Windows** (Windows: CI run 41898913795, the same failure).

- Web Forms: the home page and a sample's folder page (`/Hubs/DemoHub/`, default document, master page).
- SignalR: `/signalr/hubs` lists every hub, `VBDemo` from the VB library included; hub negotiation and a persistent
  connection's (`/raw-connection`) return connection tokens; `/signalr/ping`.
- OWIN: `/cors/signalr/negotiate` (CORS branch); `/cookieauth/echo/negotiate` redirects to the cookie middleware's
  log-in path (302).
- Static files: a sample's HTML page and the copied `jquery.signalR.js` (2.4.3).
- The only failure: `/signalr/hubs`' header comment is `ASP.NET SignalR JavaScript Library [!VERSION!]`, where
  Microsoft's 2.4.3 says `2.4.3` ([F70](../../FINDINGS.md#f70)).

Checked by hand as well (not probes, which are single GETs): a whole long-polling hub conversation with curl,
negotiate → `connect` (`{"C":...,"S":1,"M":[]}`) → `start` (`{ "Response": "started" }`) → `send` of
`demo.GetValue` (`{"R":10,"I":"0"}`); `GetHttpContextHandler` returns null, as SignalR's own test expects on IIS. CORS
answers `Access-Control-Allow-Origin` for the request's origin. WebSockets weren't tried.

One run lost three probes to the machine running out of inotify instances (other smoke runs at the same time;
[F72](../../FINDINGS.md#f72)): `Configuration Error: Failed to start monitoring changes to '.../Hubs/DemoHub'`. The
recorded run is the one after.

## Open problems

- [F70](../../FINDINGS.md#f70) (webForCore bug, #118): `[!VERSION!]` in the generated proxy.
- [F71](../../FINDINGS.md#f71) (guide gap, #121): VB projects' `MyType`.
- `Hubs/RealtimeBroadcast/index.html` needs `Scripts/hubs.js`, which only the dropped .NET Framework tool wrote.

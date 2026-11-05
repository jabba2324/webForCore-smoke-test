# Working in this repo

This is the smoke test bed for webForCore (`../webForCore`, or `$WEBFORCORE_DIR`), which runs ASP.NET 4.x apps on
.NET 10. Read `README.md` first, then webForCore's `docs/AGENT-MIGRATION.md`, `docs/MIGRATING.md`,
`docs/NAMESPACES.md` and `docs/IIS-COMPATIBILITY.md`.

## The goal

Get as many real open-source ASP.NET MVC 5 and Web API 2 apps as possible (Web Forms, Web Pages, OWIN and SignalR
apps are welcome too) through fetch → assess → migrate → build → smoke, and record precisely what fails and why.
Coverage and accurate findings matter more than a green table.

## The loop, per app

Each app is a folder, `apps/<id>/` (see README.md). Nothing goes at the top level except the runner (`scripts/wfc.cs`, and `scripts/convert.cs`), the services, the CI
workflow (`.github/workflows/smoke.yml`), `FINDINGS.md` and the generated `RESULTS.md`; never add a top-level project
or solution.

1. **Verify `apps/<id>/app.toml`** before anything else: the repo exists, the licence is open source, the web project
   targets .NET Framework 4.x with System.Web, and `site`/`project` are right. Pin `ref` to a commit hash. Set
   `status` to `verified`, or `rejected` with the reason in `notes`.
2. `dotnet run scripts/wfc.cs -- fetch <id>` and `dotnet run scripts/wfc.cs -- assess <id>`. Write the assessment in `apps/<id>/NOTES.md`.
3. Migrate in `apps/<id>/src/` following `docs/AGENT-MIGRATION.md`, against webForCore's release packages
   (`PackageReference Include="webForCore" Version="0.9.0"` and the framework packages; `dotnet run scripts/wfc.cs -- packages`
   downloads them), not its source. Keep changes minimal; don't modernise the app. Record each change and its
   reason in `NOTES.md` as you go, not afterwards.
4. `dotnet run scripts/wfc.cs -- build <id>` and `dotnet run scripts/wfc.cs -- smoke <id>` on Linux while migrating, then the CI workflow for
   both platforms (README.md, "CI"): every round runs on Linux and Windows, and `RESULTS.md` shows both. An app whose database needs preparing gets a `[setup]` in `app.toml`, so `setup` (and CI) can do it. Add `[[probes]]` to `app.toml` for each kind of page the
   app has (home page, an MVC action, a Web API route returning JSON, a form, a static file), with `contains` text
   that proves the page rendered. Where the app has forms or an API, probe them with the verbs they take, too:
   a form posted (with its anti-forgery token or view state captured from the GET before it), an API's POST, PUT and
   DELETE (README.md, "Requests: probes and setup steps").
   Write up the smoke results in `NOTES.md`; the runner appends each run to `LOG.md` and keeps the latest logs in
   `logs/`.
5. `dotnet run scripts/wfc.cs -- save-overlay <id>`, update `status` and `notes` in `app.toml` (`migrated` only when every probe passes on both platforms, every time, and no feature is known to be broken by webForCore; otherwise `blocked`, with the reason. Features the probes can't reach go in the notes as untested), `dotnet run scripts/wfc.cs -- report`, and
   commit the app's folder (this repo only, one app per commit).

## Third-party libraries

webForCore ships only Microsoft assemblies. Builds of third-party libraries for it are in
[webForCore-thirdparty](https://github.com/xforcore/webForCore-thirdparty), for this test bed: an app that uses one
of them references the package from there. `dotnet run scripts/wfc.cs -- packages` downloads its release (`THIRDPARTY_VERSION`,
default 0.7.0) into the same folder as webForCore's; by hand:

```bash
gh release download --repo xforcore/webForCore-thirdparty --pattern '*.nupkg' --dir .packages/v<webForCore version> --skip-existing
```

| The app uses | Reference | Without it |
| --- | --- | --- |
| WebActivatorEx (directly, or through Unity.Mvc, Ninject.MVC, StructureMap.MVC, ...) | `webForCore.WebActivatorEx` | Its start methods never run (webForCore#21): the DI container is never set up, "No parameterless constructor defined" |
| WebActivator 1.x (Ninject.MVC3 2.x and other MVC 3-era packages) | `webForCore.WebActivator` (0.4.1+) | Its start methods never run either (webForCore#38) |
| AjaxControlToolkit | `webForCore.AjaxControlToolkit` (+ `.StaticResources`, `.HtmlEditor.Sanitizer`) | Not provided by webForCore; the original package is built for .NET Framework |
| AutoMapper 3.x or 4.x (static API: `Mapper.CreateMap`, `Profile.Configure`, ...) | `webForCore.AutoMapperLegacy` (0.6.0+), with its global usings (its README; umbraco-7 has the steps) | Configuring a map throws on .NET (F22). Not a replacement: its own namespace on AutoMapper 12-14, which maps differently in places (F104) |

Record in the app's `NOTES.md` which webForCore-thirdparty version it used. webForCore's own guide gives the
workaround without these packages (calling WebActivatorEx's start methods from `Application_Start`); a problem only
the workaround shows is still a finding. webForCore 0.1.0-0.3.0 shipped `webForCore.WebActivatorEx` (0.3.0) and the
AjaxControlToolkit packages itself; from 0.4.0 they come only from webForCore-thirdparty.

## Recording failures

Every failure goes in `FINDINGS.md`, one entry per distinct problem, with the apps it affects, the error, a minimal
reproduction where you can make one, and the classification:

- **webForCore bug**: behaviour differs from .NET Framework 4.8 on IIS. Don't work around it in the app.
- **documented gap**: listed in webForCore's `docs/NAMESPACES.md` or `docs/IIS-COMPATIBILITY.md`.
- **guide gap**: a migration step `docs/MIGRATING.md` doesn't mention.
- **app/environment**: the app's own problem (needs a database, a Windows path, a missing package).

Don't change webForCore itself from here. Write the finding up so it can be fixed there, and raise each webForCore
bug and guide gap as an issue on [xforcore/webForCore](https://github.com/xforcore/webForCore/issues) (check for an
existing one first), with the repro; link it from `FINDINGS.md`. Security problems are public issues too: webForCore is pre-release and
not recommended for production. App/environment problems stay here.

## Rules

- No app source in this repo: only each app's folder (minus `src/`) and the findings.
- Nothing machine-specific in committed files: no absolute paths, hostnames or usernames. Reference webForCore by package version.
- Databases come from `services/compose.yml`, bound to 127.0.0.1. Remove containers you start when you're done.
- On Fedora, `dotnet` needs `OPENSSL_ENABLE_SHA1_SIGNATURES=1` (the runner sets it).
- Commit freely here, but don't add a remote or push without asking. Issues go only to webForCore's repo, as above.
- Start with the small, self-contained apps (eShopLegacy, the samples, Bonobo, BlogEngine, MiniBlog) before the big
  CMSs and stores.

## Contributing rules (from 3 Nov 2026)

webForCore's CONTRIBUTING.md holds every repository in the project to the same rules; for this repo they mean:

- **Overlays don't work around webForCore.** A migration never uses reflection into .NET's or webForCore's
  internals, unsafe code, P/Invoke, or GC tricks (`GC.Collect`, `WaitForPendingFinalizers`, `AddMemoryPressure`,
  `GCHandle`s, weak-reference polling) to get an app running. If an app seems to need one, that's a finding (a
  webForCore bug or a guide gap, raised as an issue), not an overlay change. Reflection over the app's own types,
  as the app already does, is fine.
- **The runner and scripts are C#** (.NET file-based programs: `dotnet run scripts/wfc.cs -- <command>`), like
  every other tool in the project; Python isn't needed (from November 2026; it was Python 3.11+ before).
- **The runner and scripts: slow, stable and simple beat fast.** Every wait has a timeout and every external call
  fails rather than hangs (git runs with a stall timeout and no credential prompt, for example). No parallelism or
  caching tricks to save time inside the runner. CI's jobs running side by side is fine: each is one app on its own
  machine, running the runner as it runs anywhere else.
- **Report numbers.** Each app's result is probes passed out of the total, per platform, in `RESULTS.md` and in the
  round's PR; a finding says how many apps it affects.
- **Both platforms.** A round counts once it has run on Linux and on Windows (in CI, once a run has covered both);
  until then it's in progress.

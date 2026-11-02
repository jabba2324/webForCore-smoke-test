# Working in this repo

This is the smoke test bed for webForCore (`../webForCore`, or `$WEBFORCORE_DIR`), which runs ASP.NET 4.x apps on
.NET 11. Read `README.md` first, then webForCore's `docs/AGENT-MIGRATION.md`, `docs/MIGRATING.md`,
`docs/NAMESPACES.md` and `docs/IIS-COMPATIBILITY.md`.

## The goal

Get as many real open-source ASP.NET MVC 5 and Web API 2 apps as possible (Web Forms, Web Pages, OWIN and SignalR
apps are welcome too) through fetch → assess → migrate → build → smoke, and record precisely what fails and why.
Coverage and accurate findings matter more than a green table.

## The loop, per app

Each app is a folder, `apps/<id>/` (see README.md). Nothing goes at the top level except the runner (and `scripts/convert.py`), the services,
`FINDINGS.md` and the generated `RESULTS.md`; never add a top-level project or solution.

1. **Verify `apps/<id>/app.toml`** before anything else: the repo exists, the licence is open source, the web project
   targets .NET Framework 4.x with System.Web, and `site`/`project` are right. Pin `ref` to a commit hash. Set
   `status` to `verified`, or `rejected` with the reason in `notes`.
2. `scripts/wfc.py fetch <id>` and `scripts/wfc.py assess <id>`. Write the assessment in `apps/<id>/NOTES.md`.
3. Migrate in `apps/<id>/src/` following `docs/AGENT-MIGRATION.md`, against webForCore's release packages
   (`PackageReference Include="webForCore" Version="0.6.0"` and the framework packages; `scripts/wfc.py packages`
   downloads them), not its source. Keep changes minimal; don't modernise the app. Record each change and its
   reason in `NOTES.md` as you go, not afterwards.
4. `scripts/wfc.py build <id>` and `scripts/wfc.py smoke <id>` on Linux, then `scripts/wfc.py windows run <id>` on Windows
   (README.md, "Windows"): every round runs on both, and `RESULTS.md` shows both. Add `[[probes]]` to `app.toml` for each kind of page the
   app has (home page, an MVC action, a Web API route returning JSON, a form, a static file), with `contains` text
   that proves the page rendered.
   Write up the smoke results in `NOTES.md`; the runner appends each run to `LOG.md` and keeps the latest logs in
   `logs/`.
5. `scripts/wfc.py save-overlay <id>`, update `status` and `notes` in `app.toml`, `scripts/wfc.py report`, and
   commit the app's folder (this repo only, one app per commit).

## Third-party libraries

webForCore ships only Microsoft assemblies. Builds of third-party libraries for it are in
[webForCore-thirdparty](https://github.com/jabba2324/webForCore-thirdparty), for this test bed: an app that uses one
of them references the package from there. `scripts/wfc.py packages` downloads its release (`THIRDPARTY_VERSION`,
default 0.4.1) into the same folder as webForCore's; by hand:

```bash
gh release download --repo jabba2324/webForCore-thirdparty --pattern '*.nupkg' --dir .packages/v<webForCore version> --skip-existing
```

| The app uses | Reference | Without it |
| --- | --- | --- |
| WebActivatorEx (directly, or through Unity.Mvc, Ninject.MVC, StructureMap.MVC, ...) | `webForCore.WebActivatorEx` | Its start methods never run (webForCore#21): the DI container is never set up, "No parameterless constructor defined" |
| WebActivator 1.x (Ninject.MVC3 2.x and other MVC 3-era packages) | `webForCore.WebActivator` (0.4.1+) | Its start methods never run either (webForCore#38) |
| AjaxControlToolkit | `webForCore.AjaxControlToolkit` (+ `.StaticResources`, `.HtmlEditor.Sanitizer`) | Not provided by webForCore; the original package is built for .NET Framework |

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
bug and guide gap as an issue on [jabba2324/webForCore](https://github.com/jabba2324/webForCore/issues) (check for an
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

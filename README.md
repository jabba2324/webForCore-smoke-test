# webForCore smoke test

A test bed for [webForCore](https://github.com/xforcore/webForCore): it takes real open-source ASP.NET 4.x
applications (MVC 5, Web API 2, Web Forms, Web Pages, OWIN, SignalR 2), migrates each one to .NET 10 with webForCore
following its migration guide, then builds it, runs it on Kestrel and probes it over HTTP, on Linux and on Windows.

webForCore's own suites are Microsoft's and Mono's unit tests. This repo asks a different question: does an app
somebody actually shipped run, and what breaks when it doesn't? Each failure is either a bug in webForCore, a
documented platform gap, or a migration step the guide doesn't cover yet.

## How it works

There's no top-level project or solution: each app is a folder of its own under `apps/`.

| `apps/<id>/` | What's in it |
| --- | --- |
| `app.toml` | Where the app comes from (repo, pinned commit), its web project, licence, status and probes |
| `NOTES.md` | The migration notes: the assessment, every change made and why, the smoke results, open problems |
| `LOG.md` | One line per stage run, with its result, appended by the runner |
| `overlay.patch` | The migration, as a patch on the pinned commit |
| `results.json` | The latest result of each stage on Linux, in detail (`results.windows.json`: on Windows) |
| `logs/` | The latest build and server logs on Linux (`logs.windows/`: on Windows) |
| `src/` | The checkout (not committed) |

At the top level: `scripts/wfc.py` (the runner, Python 3.11+, standard library only), `services/compose.yml` (SQL
Server, MySQL and PostgreSQL for apps that need a database; `services/windows-mssql.ps1` sets up the same SQL Server on
Windows), `.github/workflows/smoke.yml` (the CI workflow that runs the runner), `FINDINGS.md` (the problems found,
across apps) and `RESULTS.md` (the summary table, generated).

Only the patches are committed, never the apps' code, so each app keeps its own licence and the repo stays small.

Each app goes through these stages:

1. **fetch**: clone the repo at the pinned commit and apply the overlay.
2. **assess**: Phase 0 of webForCore's `docs/AGENT-MIGRATION.md`: which frameworks it uses, its target framework, and
   every blocker (Windows authentication, WCF hosting, LINQ to SQL, ...) with file and line.
3. **build**: `dotnet build -c Release` of the converted project.
4. **setup**, for an app with `[setup]` in `app.toml`: create its database, load it, or run the app's install
   wizard, once per new database.
5. **smoke**: start the app on a free port (under `bin_dotnet/webforcore-host/webforcore-host.dll` from webForCore 0.7.0, as its worker; `bin_dotnet/<App>.dll` before), request the app's probes, and check that
   `web.config`, `App_Data`, `bin_dotnet`, `obj` and source files aren't served.

## Usage

Apps reference webForCore's NuGet packages, which are attached to its GitHub releases (not on nuget.org).
`scripts/wfc.py packages` downloads a release into `.packages/v<version>/` (needs `gh`), and builds restore from
there, together with webForCore-thirdparty's release (WebActivatorEx and AjaxControlToolkit built for webForCore; see
CLAUDE.md) and, from webForCore 0.7.0, drawingForCore's (System.Drawing, which the `webForCore` package depends on).
`WEBFORCORE_VERSION`, `THIRDPARTY_VERSION` and `DRAWING_VERSION` pick the releases (default 0.8.0, 0.6.0 and 0.3.0). An app migrated against webForCore's source instead
refers to it through `$(WEBFORCORE_DIR)` (default `../webForCore`).

```sh
scripts/wfc.py packages                  # webForCore's and webForCore-thirdparty's release packages, into .packages/
scripts/wfc.py list                      # every app and its last result
scripts/wfc.py fetch eshop-legacy-mvc    # clone at the pinned commit, apply the overlay
scripts/wfc.py assess eshop-legacy-mvc
# ... migrate in apps/eshop-legacy-mvc/src (see below), noting each step in NOTES.md, then:
scripts/wfc.py save-overlay eshop-legacy-mvc
scripts/wfc.py run eshop-legacy-mvc      # fetch, build, setup, smoke
scripts/wfc.py run --all
scripts/wfc.py report                    # RESULTS.md
```

`fetch` leaves an existing checkout alone, so unsaved migration work isn't lost. `fetch --reset` discards it and
reapplies the overlay. An app that needs a database (`needs`) needs its service running first:
`docker compose -f services/compose.yml up -d --wait mssql`, and `down -v` when you're done.

## CI

`.github/workflows/smoke.yml` runs the same commands in GitHub Actions. Each migrated app (it has an `overlay.patch`,
and isn't `rejected`) is its own job on Linux (`ubuntu-24.04`) and on Windows (`windows-2025`), so a round runs in
parallel. A `packages` job downloads the release packages once and hands them to the app jobs (each downloading
them went past GitHub's API rate limit). Each app job runs `scripts/wfc.py` on the runner itself:

1. The packages, then the app's database if it `needs` one: the compose SQL Server on Linux, SQL Server Express from
   `services/windows-mssql.ps1` on Windows.
2. `fetch`, `build`, `setup` and `smoke`.
3. `ci-result`: compares the results with the committed `results.json` (or `results.windows.json`) and adds a line to
   the run's summary. The job fails if the app does worse: a check that passed fails, or a build that passed fails.
   An app that's still blocked passes as long as it's no worse.

Then a `report` job builds `RESULTS.md` from every job's results, shows it in the run's summary, and uploads it with
every app's results as the `round` artifact.

It runs:
- **By hand,** for every migrated app or the ones you name, and on any webForCore release:

  ```sh
  gh workflow run smoke.yml                                  # every migrated app
  gh workflow run smoke.yml -f apps="miniblog bonobo-git-server"
  gh workflow run smoke.yml -f webforcore_version=0.8.0 -f thirdparty_version=0.6.0 -f drawing_version=0.3.0
  ```
- **On a pull request or a push to `main`,** for the apps it changes (`apps/<id>/`), or for every app if it changes
  `scripts/`, `services/` or the workflow.

To follow a run and take its results:

```sh
gh run list --workflow smoke.yml
gh run watch <run-id>
gh run download <run-id> -n round      # RESULTS.md and apps/*/results*.json, to commit
gh run download <run-id> -n results-miniblog-windows   # one job's results and logs
```

A round on a new release is a manual run on that release. Download `round`, commit its `RESULTS.md` and results with
the round's notes and findings, and the round counts once that run has covered both platforms.

## Windows

Every round runs on Linux and on Windows; `RESULTS.md` has a build and a smoke column for each. Migrations are done
once (on Linux), and the same overlay is built and smoked on both. CI's Windows jobs are the usual way (see "CI").

To run on a Windows machine of your own, clone this repo there and use `scripts/wfc.py` as on Linux. It needs the
.NET 10 SDK, Python 3.11+, git and `gh`. Results go to `results.windows.json` and `logs.windows/`, and LOG.md lines say
`(windows)`. For apps that need SQL Server, `services/windows-mssql.ps1` installs SQL Server Express where the overlays
look for it. It's meant for a throwaway machine: it sets a test-only `sa` password.

## Migrating an app

`scripts/convert.py <web project>.csproj [--lib <library>.csproj]` does the mechanical part of Phase 2 (the SDK
project, webForCore's packages, `Program.cs`, `<system.codedom>`) and prints what it changed; check it and note it
in `NOTES.md`.

Follow webForCore's `docs/AGENT-MIGRATION.md` in the checkout, with the package route (`docs/MIGRATING.md`
section 1). Reference the packages by version; the runner adds `.packages/` as a restore source, so the patches
need no `nuget.config` and work on any machine:

```xml
<PackageReference Include="webForCore" Version="0.8.0" />
<PackageReference Include="webForCore.Web.Mvc" Version="0.8.0" />
```

Change only what the platform needs, as the guide says. A workaround in the app for a webForCore bug doesn't count
as a pass: note it in `NOTES.md` and `FINDINGS.md`, and fix webForCore instead.

## `app.toml`

| Field | Meaning |
| --- | --- |
| `name` | Display name |
| `repo` | Git URL |
| `ref` | Pinned commit (preferred) or tag; `overlay.patch` applies to exactly this |
| `site` | The web project's folder in the checkout (the one with `web.config`) |
| `project` | The web project file in the checkout, converted in place by the overlay (default: the `.csproj` in `site`) |
| `also_build` | Further project files in the checkout, built after `project` (projects that deploy themselves into the site, as mojoPortal's features do) |
| `kinds` | `webforms`, `mvc`, `webapi`, `webpages`, `owin`, `signalr`, `identity` |
| `licence` | SPDX id, or the project's own licence name |
| `status` | `candidate` (listed, unchecked), `verified` (repo, ref, licence and paths checked), `migrating`, `migrated` (builds, passes every probe on Linux and on Windows, and has no feature known to be broken by webForCore), `blocked` (anything less: a probe fails, on either platform, even now and then, or a feature is broken by a webForCore bug or gap, probed or not), `rejected` (not a fit; kept so it isn't added again) |
| `needs` | Services from `services/compose.yml`: `mssql`, `mysql`, `postgres` (CI starts them for the app's job) |
| `[setup]` | What the app's database needs before the probes, run by `setup` when a database it lists is new: `databases` (SQL Server databases to create, empty), `sql_files` (scripts in the checkout, run against the first), `[[setup.steps]]` (requests to the running app, such as an install wizard: `get` or `post` a path, with `fields`; `button` and `value` for a Web Forms postback; `json` to post a JSON body instead (an AJAX page method, an MVC action); `contains`, `timeout`, `may_fail`, `retries`) |
| `[ci]` | `linux = false` or `windows = false` to leave the app out of CI on that platform |
| `env` | Extra environment for the server process (a table) |
| `remove` | Globs of files `fetch` deletes before applying the overlay, kept out of the patch (the app's checked-in DLLs) |
| `generated` | Globs of files the build or the app writes into the checkout, kept out of the patch (mojoPortal's features copy themselves into the site) |
| `https` | `true` to serve the app over HTTPS with the .NET development certificate (`dotnet dev-certs https`), for apps with `RequireHttpsAttribute` |
| `[[probes]]` | `path` (`{setup_date}` in it is the day `setup` set the database up), `status` (default 200), `contains` (text that proves the page rendered), `absent` (text that must not be in the response), `cookies = true` (the probe sends and keeps cookies in a jar shared with the app's other `cookies` probes, in order, for a flow that needs a session; redirects are still not followed) |
| `[forbidden."<path>"]` | Overrides for one of the paths that must not be served (`/web.config`, `/packages.config`, ...), for an app whose error handler answers 200 with its own page: `status`, `absent`, `contains` |
| `notes` | One line for the summary; the detail goes in `NOTES.md` |

## Adding apps

Make `apps/<id>/` with an `app.toml` (`status = "candidate"`) and a `NOTES.md` (copy another app's headings), then
check it: the repo exists, the licence is open source, and the web project targets .NET Framework
(`<TargetFrameworkVersion>v4.x`) with `System.Web`. Pin a commit and set `status = "verified"`. Apps that turn out
not to fit stay, as `rejected` with the reason, so they aren't added twice.

The entries so far were listed from memory, and all start as candidates.

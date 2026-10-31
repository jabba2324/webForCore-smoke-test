# webForCore smoke test

A test bed for [webForCore](https://github.com/jabba2324/webForCore): it takes real open-source ASP.NET 4.x
applications (MVC 5, Web API 2, Web Forms, Web Pages, OWIN, SignalR 2), migrates each one to .NET 11 with webForCore
following its migration guide, then builds it, runs it on Kestrel and probes it over HTTP.

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
| `results.json` | The latest result of each stage, in detail |
| `logs/` | The latest build and server logs |
| `src/` | The checkout (not committed) |

At the top level: `scripts/wfc.py` (the runner, Python 3.11+, standard library only), `services/compose.yml` (SQL
Server, MySQL and PostgreSQL for apps that need a database), `FINDINGS.md` (the problems found, across apps) and
`RESULTS.md` (the summary table, generated).

Only the patches are committed, never the apps' code, so each app keeps its own licence and the repo stays small.

Each app goes through four stages:

1. **fetch**: clone the repo at the pinned commit and apply the overlay.
2. **assess**: Phase 0 of webForCore's `docs/AGENT-MIGRATION.md`: which frameworks it uses, its target framework, and
   every blocker (Windows authentication, WCF hosting, LINQ to SQL, ...) with file and line.
3. **build**: `dotnet build -c Release` of the converted project.
4. **smoke**: start `bin_dotnet/<App>.dll` on a free port, request the app's probes, and check that
   `web.config`, `App_Data`, `bin_dotnet`, `obj` and source files aren't served.

## Usage

webForCore is expected next to this repo (`../webForCore`), built as its README says. Set `WEBFORCORE_DIR` to use
another checkout.

```sh
scripts/wfc.py list                      # every app and its last result
scripts/wfc.py fetch eshop-legacy-mvc    # clone at the pinned commit, apply the overlay
scripts/wfc.py assess eshop-legacy-mvc
# ... migrate in apps/eshop-legacy-mvc/src (see below), noting each step in NOTES.md, then:
scripts/wfc.py save-overlay eshop-legacy-mvc
scripts/wfc.py run eshop-legacy-mvc      # fetch, build, smoke
scripts/wfc.py run --all
scripts/wfc.py report                    # RESULTS.md
```

`fetch` leaves an existing checkout alone, so unsaved migration work isn't lost. `fetch --reset` discards it and
reapplies the overlay.

## Migrating an app

Follow webForCore's `docs/AGENT-MIGRATION.md` in the checkout. In migrated projects, refer to webForCore through the
`WEBFORCORE_DIR` property, which the runner sets, so the patches work on any machine:

```xml
<Import Project="$(WEBFORCORE_DIR)/src/webForCore.WebStack.targets" />
<ProjectReference Include="$(WEBFORCORE_DIR)/src/WebFormsForCore.Web/WebFormsForCore.Web.csproj" />
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
| `kinds` | `webforms`, `mvc`, `webapi`, `webpages`, `owin`, `signalr`, `identity` |
| `licence` | SPDX id, or the project's own licence name |
| `status` | `candidate` (listed, unchecked), `verified` (repo, ref, licence and paths checked), `migrating`, `migrated` (builds and passes its probes), `blocked` (needs something webForCore doesn't provide), `rejected` (not a fit; kept so it isn't added again) |
| `needs` | Services from `services/compose.yml`: `mssql`, `mysql`, `postgres` |
| `env` | Extra environment for the server process (a table) |
| `[[probes]]` | `path`, `status` (default 200), `contains` (text that proves the page rendered) |
| `notes` | One line for the summary; the detail goes in `NOTES.md` |

## Adding apps

Make `apps/<id>/` with an `app.toml` (`status = "candidate"`) and a `NOTES.md` (copy another app's headings), then
check it: the repo exists, the licence is open source, and the web project targets .NET Framework
(`<TargetFrameworkVersion>v4.x`) with `System.Web`. Pin a commit and set `status = "verified"`. Apps that turn out
not to fit stay, as `rejected` with the reason, so they aren't added twice.

The entries so far were listed from memory, and all start as candidates.

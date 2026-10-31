# webForCore smoke test

A test bed for [webForCore](https://github.com/jabba2324/webForCore): it takes real open-source ASP.NET 4.x
applications (MVC 5, Web API 2, Web Forms, Web Pages, OWIN, SignalR 2), migrates each one to .NET 11 with webForCore
following its migration guide, then builds it, runs it on Kestrel and probes it over HTTP.

webForCore's own suites are Microsoft's and Mono's unit tests. This repo asks a different question: does an app
somebody actually shipped run, and what breaks when it doesn't? Each failure is either a bug in webForCore, a
documented platform gap, or a migration step the guide doesn't cover yet.

## How it works

| Folder | What's in it |
| --- | --- |
| `catalog/apps.toml` | The apps: repo, pinned commit, web project path, licence, status, probes |
| `apps/<id>/` | The checkouts (not committed) |
| `overlays/<id>.patch` | Each app's migration, as a patch on its pinned commit |
| `results/<id>.json` | The latest result of each stage |
| `RESULTS.md` | The summary table, generated from `results/` |
| `services/compose.yml` | SQL Server, MySQL and PostgreSQL for apps that need a database |
| `scripts/wfc.py` | The runner (Python 3.11+, standard library only) |

Only the patches are committed, never the apps' code, so each app keeps its own licence and the repo stays small.

Each app goes through four stages:

1. **fetch**: clone the repo at the pinned commit and apply the overlay.
2. **assess**: Phase 0 of webForCore's `docs/AGENT-MIGRATION.md`: which frameworks it uses, its target framework, and
   every blocker (Windows authentication, WCF hosting, LINQ to SQL, ...) with file and line.
3. **build**: `dotnet build -c Release` of the converted project.
4. **smoke**: start `bin_dotnet/<App>.dll` on a free port, request the catalog's probes, and check that
   `web.config`, `App_Data`, `bin_dotnet`, `obj` and source files aren't served.

## Usage

webForCore is expected next to this repo (`../webForCore`), built as its README says. Set `WEBFORCORE_DIR` to use
another checkout.

```sh
scripts/wfc.py list                      # every app and its last result
scripts/wfc.py fetch eshop-legacy-mvc    # clone at the pinned commit, apply the overlay
scripts/wfc.py assess eshop-legacy-mvc
# ... migrate in apps/eshop-legacy-mvc (see below), then:
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
as a pass: note it in the catalog and fix webForCore instead.

## Adding apps

Add an entry to `catalog/apps.toml` with `status = "candidate"`, then check it: the repo exists, the licence is open
source, and the web project targets .NET Framework (`<TargetFrameworkVersion>v4.x`) with `System.Web`. Pin a commit
and set `status = "verified"`. Apps that turn out not to fit stay listed as `rejected`, with the reason, so they
aren't added twice.

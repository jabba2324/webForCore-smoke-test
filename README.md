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

At the top level: `scripts/wfc.cs` (the runner: a .NET file-based program, `dotnet run scripts/wfc.cs -- <command>`,
which needs only the .NET 10 SDK; it reads `app.toml` with the Tomlyn package, which `dotnet run` restores from NuGet),
`scripts/convert.cs` (Phase 2 of a migration, below), `services/compose.yml` (SQL Server, MySQL and PostgreSQL for apps
that need a database; `services/windows-mssql.ps1` sets up the same SQL Server on Windows),
`.github/workflows/smoke.yml` (the CI workflow that runs the runner), `FINDINGS.md` (the problems found, across apps)
and `RESULTS.md` (the summary table, generated).

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
`dotnet run scripts/wfc.cs -- packages` downloads a release into `.packages/v<version>/` (needs `gh`), and builds restore from
there, together with webForCore-thirdparty's release (WebActivatorEx and AjaxControlToolkit built for webForCore; see
CLAUDE.md) and, from webForCore 0.7.0, drawingForCore's (System.Drawing, which the `webForCore` package depends on).
`WEBFORCORE_VERSION`, `THIRDPARTY_VERSION` and `DRAWING_VERSION` pick the releases (default 0.10.0, 0.7.0 and 0.3.0). An app migrated against webForCore's source instead
refers to it through `$(WEBFORCORE_DIR)` (default `../webForCore`).

```sh
dotnet run scripts/wfc.cs -- packages                  # webForCore's and webForCore-thirdparty's release packages, into .packages/
dotnet run scripts/wfc.cs -- list                      # every app and its last result
dotnet run scripts/wfc.cs -- fetch eshop-legacy-mvc    # clone at the pinned commit, apply the overlay
dotnet run scripts/wfc.cs -- assess eshop-legacy-mvc
# ... migrate in apps/eshop-legacy-mvc/src (see below), noting each step in NOTES.md, then:
dotnet run scripts/wfc.cs -- save-overlay eshop-legacy-mvc
dotnet run scripts/wfc.cs -- run eshop-legacy-mvc      # fetch, build, setup, smoke
dotnet run scripts/wfc.cs -- run --all
dotnet run scripts/wfc.cs -- report                    # RESULTS.md
```

`fetch` leaves an existing checkout alone, so unsaved migration work isn't lost. `fetch --reset` discards it and
reapplies the overlay. An app that needs a database (`needs`) needs its service running first:
`docker compose -f services/compose.yml up -d --wait mssql`, and `down -v` when you're done.

## CI

`.github/workflows/smoke.yml` runs the same commands in GitHub Actions. Each migrated app (it has an `overlay.patch`,
and isn't `rejected`) is its own job on Linux (`ubuntu-24.04`) and on Windows (`windows-2025`), so a round runs in
parallel. A `packages` job downloads the release packages once and hands them to the app jobs (each downloading
them went past GitHub's API rate limit). Each app job runs `scripts/wfc.cs` on the runner itself, with the runner image's .NET SDK:

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
  gh workflow run smoke.yml -f webforcore_version=0.10.0 -f thirdparty_version=0.7.0 -f drawing_version=0.3.0
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

To run on a Windows machine of your own, clone this repo there and use `dotnet run scripts/wfc.cs -- <command>` as on Linux. It
needs the .NET 10 SDK, git and `gh` (Python isn't needed: the runner was Python 3.11+ before November 2026). Results go to `results.windows.json` and `logs.windows/`, and LOG.md lines say
`(windows)`. For apps that need SQL Server, `services/windows-mssql.ps1` installs SQL Server Express where the overlays
look for it. It's meant for a throwaway machine: it sets a test-only `sa` password.

## Migrating an app

`dotnet run scripts/convert.cs -- <web project>.csproj [--lib <library>.csproj]` does the mechanical part of Phase 2 (the SDK
project, webForCore's packages, `Program.cs`, `<system.codedom>`) and prints what it changed; check it and note it
in `NOTES.md`.

Follow webForCore's `docs/AGENT-MIGRATION.md` in the checkout, with the package route (`docs/MIGRATING.md`
section 1). Reference the packages by version; the runner adds `.packages/` as a restore source, so the patches
need no `nuget.config` and work on any machine:

```xml
<PackageReference Include="webForCore" Version="0.10.0" />
<PackageReference Include="webForCore.Web.Mvc" Version="0.10.0" />
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
| `[setup]` | What the app's database needs before the probes, run by `setup` when a database it lists is new: `databases` (SQL Server databases to create, empty), `sql_files` (scripts in the checkout, run against the first), `[[setup.steps]]` (requests to the running app, such as an install wizard: `get` or `post` a path, with `fields`; `button` and `value` for a Web Forms postback; `json` to post a JSON body instead (an AJAX page method, an MVC action); `contains`, `timeout`, `may_fail`, `retries`; and what a probe's request can have: `method`, `headers`, `body`, `form`, `json` as a table, `multipart`, `content_type`, `capture`, `follow_redirects` (default `true` for a step); "Requests", below; and `restart = true`, a step that stops the app and starts it again). Steps keep cookies, in a jar of their own |
| `[ci]` | `linux = false` or `windows = false` to leave the app out of CI on that platform |
| `env` | Extra environment for the server process (a table) |
| `remove` | Globs of files `fetch` deletes before applying the overlay, kept out of the patch (the app's checked-in DLLs) |
| `generated` | Globs of files the build or the app writes into the checkout, kept out of the patch (mojoPortal's features copy themselves into the site) |
| `https` | `true` to serve the app over HTTPS with the .NET development certificate (`dotnet dev-certs https`), for apps with `RequireHttpsAttribute` |
| `[[probes]]` | Requests made in order, each with what its response must be: `path` (`{setup_date}` in it is the day `setup` set the database up), `status` (default 200), `contains` (text that proves the page rendered), `absent` (text that must not be in the response; with it, the status isn't checked), `cookies = true` (the probe sends and keeps cookies in a jar shared with the app's other `cookies` probes, in order, for a flow that needs a session), and the request's `method`, `headers`, `body`, `form`, `json`, `multipart`, `content_type`, `capture`, `follow_redirects` (default `false`) and `timeout` ("Requests", below) |
| `[forbidden."<path>"]` | Overrides for one of the paths that must not be served (`/web.config`, `/packages.config`, ...), for an app whose error handler answers 200 with its own page: `status`, `absent`, `contains` |
| `notes` | One line for the summary; the detail goes in `NOTES.md` |

## Requests: probes and setup steps

A probe is a request and what its response must be; a `[setup]` step is a request too, with what it needs to go on.
Both are built the same way. Probes run in order, after `setup`, and later ones can use what earlier ones captured.

| Key | Meaning |
| --- | --- |
| `path` | The path and query (a setup step's `get` or `post`) |
| `method` | `GET` (the default; for a step's `post`, `POST`), `POST`, `PUT`, `DELETE`, `PATCH`, `HEAD` or `OPTIONS` |
| `headers` | A table of request headers, e.g. `{ X-Requested-With = "XMLHttpRequest" }` |
| `form` | A table, sent `application/x-www-form-urlencoded` (a step's `fields` are this too) |
| `json` | A table (or a `[probes.json]` sub-table), sent as JSON; or a string, sent as it is. `application/json; charset=utf-8` |
| `multipart` | A table of text fields, sent `multipart/form-data`; a field that's a table is sent as its JSON |
| `body` | A string, sent as it is (give it a `content_type`) |
| `content_type` | The body's `Content-Type`, instead of the one above |
| `cookies` | `true`: the probe sends and keeps cookies, in one jar shared by the app's `cookies` probes. Steps always keep cookies, in a jar of their own |
| `follow_redirects` | `true` follows redirects (GET and HEAD after any; POST after 301, 302 and 303, as a GET; any method after a 303). A probe's default is `false`, a step's `true` |
| `capture` | Values for later requests: `{ name = "regex" }` over the response's body; `{ name = { header = "Location", regex = "..." } }` over a header's values; `{ name = { cookie = "Name" } }` the jar's cookie (a `cookies` probe), optionally with a `regex` too. The value is the regex's first group, or all it matched (a .NET regular expression). A request that captures nothing for a name fails |
| `{name}` | In `path`, `headers`, `form`, `json`, `multipart` and `body`: a value captured before (and, in probes, `{setup_date}`). Other braces are left as they are, so JSON in a `body` needs no escaping |
| `timeout` | Seconds. A probe's default is 120 for the first (it compiles the site) and 30 for the others; a step's is 120 |
| `status`, `contains`, `absent` | For probes, what the response must be (above). A step must answer below 400, with its `contains` |

One of `form`, `json`, `multipart` and `body` at most. In `results.json` a probe whose method isn't GET has a `method`,
and `ci-result` tells it apart by its method (and a path probed more than once by its place: `/x`, `/x #2`, ...).

A Web Forms postback (eshop-legacy-webforms): the page first, for its view state and event validation, then the post
with the button's name:

```toml
[[probes]]
path = "/Catalog/Create"
cookies = true
capture = { viewstate = 'id="__VIEWSTATE" value="([^"]*)"', generator = 'id="__VIEWSTATEGENERATOR" value="([^"]*)"', validation = 'id="__EVENTVALIDATION" value="([^"]*)"' }

[[probes]]
path = "/Catalog/Create"
method = "POST"
cookies = true
status = 302
[probes.form]
__VIEWSTATE = "{viewstate}"
__VIEWSTATEGENERATOR = "{generator}"
__EVENTVALIDATION = "{validation}"
"ctl00$MainContent$Name" = "Smoke Test Mug"
"ctl00$MainContent$Price" = "4.50"
"ctl00$MainContent$ctl06" = "[ Create ]"   # the button that was clicked

[[probes]]
path = "/Default/index/0/size/50"
cookies = true
contains = "Smoke Test Mug"
capture = { item = 'Smoke Test Mug[\s\S]*?/Catalog/Edit/(\d+)' }   # then /Catalog/Edit/{item}, ...
```


An MVC form with an anti-forgery token (eshop-legacy-mvc): the GET sets the token's cookie and puts the token in the
form, and the post sends both:

```toml
[[probes]]
path = "/Catalog/Create"
cookies = true
capture = { token = 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"' }

[[probes]]
path = "/Catalog/Create"
method = "POST"
cookies = true
status = 302
form = { __RequestVerificationToken = "{token}", Name = "Smoke Test Hoodie", Price = "12.50", CatalogBrandId = "2", CatalogTypeId = "2" }
```


A Web API's POST, PUT and DELETE with JSON (aspnetdocs-productsapp), the new product's id taken from `Location`:

```toml
[[probes]]
path = "/api/products"
method = "POST"
json = { Name = "Smoke Gadget", Category = "Hardware", Price = 2.5 }
status = 201
capture = { product = { header = "Location", regex = "/api/products/(\\d+)$" } }

[[probes]]
path = "/api/products/{product}"
method = "PUT"
json = { Name = "Smoke Gadget II", Category = "Hardware", Price = 3.75 }
status = 204

[[probes]]
path = "/api/products/{product}"
method = "DELETE"
status = 204

[[probes]]
path = "/api/products/{product}"
status = 404
```


Umbraco's back office (umbraco-8): the log-in sets the `UMB-XSRF-TOKEN` cookie, and every API call after it sends
that value in the `X-UMB-XSRF-TOKEN` header:

```toml
[[probes]]
path = "/umbraco/backoffice/UmbracoApi/Authentication/PostLogin"
method = "POST"
cookies = true
json = { username = "admin@example.com", password = "Smoke_test_1234" }
capture = { xsrf = { cookie = "UMB-XSRF-TOKEN" } }

[[probes]]
path = "/umbraco/backoffice/UmbracoApi/Content/PostSave"
method = "POST"
cookies = true
headers = { X-UMB-XSRF-TOKEN = "{xsrf}" }
multipart = { contentItem = { id = 0, parentId = -1, contentTypeAlias = "smokePage", templateAlias = "SmokePage", action = "publishNew", variants = [{ name = "Smoke Probe Page", properties = [], save = true, publish = true }] } }
capture = { url = '"urls":\[\{[^}]*"text":"([^"]+)"' }

[[probes]]
path = "{url}"
contains = "Rendered by Umbraco 8: Smoke Probe Page"
```

The same keys work in `[[setup.steps]]` (umbraco-8 makes its template, document type and home page there), and a
step with `restart = true` stops the app and starts it again between two steps (Umbraco 8 needs one after its
unattended install).


## Adding apps

Make `apps/<id>/` with an `app.toml` (`status = "candidate"`) and a `NOTES.md` (copy another app's headings), then
check it: the repo exists, the licence is open source, and the web project targets .NET Framework
(`<TargetFrameworkVersion>v4.x`) with `System.Web`. Pin a commit and set `status = "verified"`. Apps that turn out
not to fit stay, as `rejected` with the reason, so they aren't added twice.

The entries so far were listed from memory, and all start as candidates.

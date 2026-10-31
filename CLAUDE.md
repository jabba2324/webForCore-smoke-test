# Working in this repo

This is the smoke test bed for webForCore (`../webForCore`, or `$WEBFORCORE_DIR`), which runs ASP.NET 4.x apps on
.NET 11. Read `README.md` first, then webForCore's `docs/AGENT-MIGRATION.md`, `docs/MIGRATING.md`,
`docs/NAMESPACES.md` and `docs/IIS-COMPATIBILITY.md`.

## The goal

Get as many real open-source ASP.NET MVC 5 and Web API 2 apps as possible (Web Forms, Web Pages, OWIN and SignalR
apps are welcome too) through fetch → assess → migrate → build → smoke, and record precisely what fails and why.
Coverage and accurate findings matter more than a green table.

## The loop, per app

1. **Verify the catalog entry** before anything else: the repo exists, the licence is open source, the web project
   targets .NET Framework 4.x with System.Web, and `site`/`project` are right. Pin `ref` to a commit hash. Set
   `status` to `verified`, or `rejected` with the reason in `notes`.
2. `scripts/wfc.py fetch <id>` and `scripts/wfc.py assess <id>`.
3. Migrate in `apps/<id>/` following `docs/AGENT-MIGRATION.md`, referring to webForCore only through
   `$(WEBFORCORE_DIR)`. Keep changes minimal; don't modernise the app.
4. `scripts/wfc.py build <id>` and `scripts/wfc.py smoke <id>`. Add probes to the catalog for each kind of page the
   app has (home page, an MVC action, a Web API route returning JSON, a form, a static file), with `contains` text
   that proves the page rendered.
5. `scripts/wfc.py save-overlay <id>`, update the entry's `status` and `notes`, `scripts/wfc.py report`, and commit
   (this repo only, one app per commit).

## Recording failures

Every failure goes in `FINDINGS.md`, one entry per distinct problem, with the apps it affects, the error, a minimal
reproduction where you can make one, and the classification:

- **webForCore bug**: behaviour differs from .NET Framework 4.8 on IIS. Don't work around it in the app.
- **documented gap**: listed in webForCore's `docs/NAMESPACES.md` or `docs/IIS-COMPATIBILITY.md`.
- **guide gap**: a migration step `docs/MIGRATING.md` doesn't mention.
- **app/environment**: the app's own problem (needs a database, a Windows path, a missing package).

Don't change webForCore itself from here. Write the finding up so it can be fixed there.

## Rules

- No app source in this repo: only overlays, catalog entries, results and findings.
- Nothing machine-specific in committed files: no absolute paths, hostnames or usernames. Use `$(WEBFORCORE_DIR)`.
- Databases come from `services/compose.yml`, bound to 127.0.0.1. Remove containers you start when you're done.
- On Fedora, `dotnet` needs `OPENSSL_ENABLE_SHA1_SIGNATURES=1` (the runner sets it).
- Commit freely here, but don't add a remote, push, or open issues anywhere without asking.
- Start with the small, self-contained apps (eShopLegacy, the samples, Bonobo, BlogEngine, MiniBlog) before the big
  CMSs and stores.

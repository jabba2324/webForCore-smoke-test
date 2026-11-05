# AspNetDocs: Contoso University (MVC 5 + EF 6 tutorial): migration notes

Status: migrated (webForCore 0.9.0, 37/37 probes on Linux and Windows). Source: the completed project of the AspNetDocs tutorial
"Getting Started with Entity Framework 6 Code First using MVC 5"
(`aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc` in dotnet/AspNetDocs). The repository has
only the tutorial's snippets; the tutorial links the project on the retired MSDN Code Gallery
(`code.msdn.microsoft.com/ASPNET-MVC-Application-b01a9fe8`), and Microsoft's archive of the gallery,
[microsoftarchive/msdn-code-gallery-community-a-c](https://github.com/microsoftarchive/msdn-code-gallery-community-a-c),
keeps it, Apache-2.0 (the sample's README). Packages: webForCore 0.7.0, webForCore-thirdparty 0.6.0 (not used),
drawingForCore 0.2.0. webForCore 0.8.0 wasn't released when this was migrated.

The checkout is the whole archive repository (about 1.5 GB of samples), as the runner clones repositories whole.

## Assessment

- **Type:** MVC 5.2 with Razor views, .NET 4.5 (`<httpRuntime targetFramework="4.5">`). One web project
  (`ContosoUniversity.csproj`), no libraries.
- **Data:** EF 6.1.1 code first, `SchoolContext` with six migrations (`.resx` model snapshots), `Configuration.Seed`
  for the tutorial's data, stored procedures for `Department` (`MapToStoredProcedures`), a row-version concurrency
  token, TPH inheritance (`Person`), a `DbConfiguration` with `SqlAzureExecutionStrategy`, two `DbInterception`
  interceptors, raw SQL (`/Home/About`, `/Course/UpdateCourseCredits`), async actions (`DepartmentController`).
  LocalDB connection string (`(LocalDb)\v11.0`, `Integrated Security`).
- **Blockers:** none (`scripts/wfc.py assess`).
- **Dependencies:** (a) replaced by webForCore: Microsoft.AspNet.Mvc, .Razor, .WebPages, .Web.Optimization,
  WebGrease, Antlr, Microsoft.Web.Infrastructure. (b) kept: EntityFramework (6.1.1 → 6.5.1, F7), Newtonsoft.Json
  (6.0.3 → 13.0.4), PagedList 1.17 and PagedList.Mvc 4.5 (`lib/net40`, built against System.Web.Mvc 4/5), and the
  content packages (bootstrap, jQuery, ...). (c) .NET Framework-only: PagedList.Mvc, which loads.
- **Go.**

## Migration

1. `scripts/convert.py ContosoUniversity.csproj` (`WEBFORCORE_VERSION=0.7.0`, `THIRDPARTY_VERSION=0.6.0`):
   `ContosoUniversity.csproj.netfx` and `packages.config.netfx` kept; SDK project with `webForCore`,
   `webForCore.Web.Mvc`, `.Web.WebPages.Deployment`, `.Web.Optimization` and `.WebGrease` 0.7.0; EntityFramework
   6.1.1 → 6.5.1 and Newtonsoft.Json 6.0.3 → 13.0.4 (F7); `Program.cs` without session state (the app doesn't use
   it). All 40 compiled files kept, none added.
2. `Web.config`: the LocalDB connection string pointed at the compose SQL Server (same catalog, `ContosoUniversity2`).
3. `Web.config`: a `MigrateDatabaseToLatestVersion` initializer for `SchoolContext`. The tutorial creates the
   database with `Update-Database` in Visual Studio's Package Manager Console, which also runs `Configuration.Seed`;
   its deployment step ("Execute Code First Migrations") adds exactly this initializer to the deployed `Web.config`.
   Without it EF's default `CreateDatabaseIfNotExists` creates the schema from the migrations (that works) but doesn't
   seed, so the lists are empty and `/Instructor?id=...` throws "Sequence contains no elements", as on IIS.

No change to the code.

## Smoke

**Linux and Windows (CI run 41900268720), webForCore 0.7.0: 16/16.** The probes: the home page, `/Student` (the seeded students, sorted, paged with
PagedList), `/Student` with a sort and a search (PagedList's pager), the Create form (anti-forgery token),
`/Course`, `/Instructor` with an instructor and a course selected (eager and explicit loading, enrollments),
`/Department` (async, the stored-procedure mapped entity), `/Home/About` (the raw SQL group-by), `Site.css`, and the
paths that mustn't be served.

The first request migrates and seeds the database (about 12 s).

One run on this machine failed `/Department` with "Failed to start monitoring changes to .../Views/Department": the
user's 128 inotify instances were used up by other agents' servers running at the same time. That is the documented
limit (webForCore `docs/IIS-COMPATIBILITY.md`, #107), not this app; the next run passed.

By hand: `POST /Student/Create` with the form's anti-forgery token returns 302 to `/Student` and the student is
listed; without the token it is a 500 (`HttpAntiForgeryException`), as on IIS. `/Course/UpdateCourseCredits` and
`/Student/Details/1` render.

### Round 0.9.0: verbs (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

21 probes added, 9 of them POSTs, all with the forms' anti-forgery tokens:
- **Students:** Create posted without a last name (the form comes back with "The Last Name field is required."),
  then posted properly (302); the search finds the student and gives its id; Edit posted (`TryUpdateModel`) and the
  details show it; Delete posted and the details are 404.
- **Departments** (the tutorial's optimistic concurrency, a `[Timestamp] RowVersion`): Create (302), Edit with the row
  version from the edit page (302), the same edit again with that now stale row version (the page comes back with
  "was modified by another user after you got the original value"), Delete with the stale row version (redirected
  with `concurrencyError=True`) and with the current one (redirected to the list), then 404.
- The run deletes what it made. **37/37 on Linux** (16 before), twice in a row.

**CI (round 0.9.0, run 42029051731, both attempts):** 37/37 on Linux, 37/37 on Windows, the same in run 42024931148.

## Open problems

None.

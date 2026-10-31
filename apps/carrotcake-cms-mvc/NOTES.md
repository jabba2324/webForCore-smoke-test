# CarrotCake CMS (MVC): migration notes

Status: blocked (LINQ to SQL data layer; not pursued). Source: https://github.com/ninianne98/CarrotCakeCMS-MVC at `bfc5c38e`. Found by searching GitHub on 2026-10-31; the
repo, licence (MIT), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API.

## Assessment

Round 2 (webForCore 0.2.0). MVC 5 CMS (`CMSAdmin`) with Web Forms pieces, Identity 2 on EF 6, and plugin web projects.
.NET Framework 4.5.

- **Blocker: the data layer is LINQ to SQL.** `CarrotCMSData/CarrotCMS.dbml` (`CarrotCMS.designer.cs`, a
  `System.Data.Linq.DataContext`) is the CMS's data access, used throughout `CMSCore` (`CompiledQueries.cs`
  compiles LINQ to SQL queries), and each plugin has its own `.dbml` (Northwind, calendars, FAQ, photo gallery).
  `CMSAdmin/Web.config:87` adds `System.Data.Linq` to the compilation assemblies. LINQ to SQL isn't on .NET
  (`docs/NAMESPACES.md`).
- Also: `BinaryFormatter` in `CMSCore/Utilities/CoreHelper.cs` (documented replacement), `new SmtpClient()` in
  `WebComponents/Email/EmailHelper.cs` (documented replacement).

**Decision:** webForCore's recommendation for LINQ to SQL is to move the data access to EF 6, which is the
application's work, not a platform change (the same call as for BlogEngine.NET). Not migrated.

## Migration

Not started.

## Smoke

Not run.

## Open problems

- LINQ to SQL data layer (documented gap): to be moved to EF 6 by the application.

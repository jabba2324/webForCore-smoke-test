# CarrotCake CMS (MVC): migration notes

Status: migrating (webForCore 0.9.0: 39/39 on Linux; LINQ to SQL is provided now). Source: https://github.com/ninianne98/CarrotCakeCMS-MVC at `bfc5c38e`. Found by searching GitHub on 2026-10-31; the
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

**Round 0.9.0 (5 November 2026):** webForCore 0.9.0 provides System.Data.Linq (`webForCore.Data.Linq`, brought by the
`webForCore` package), so the decision above no longer applies: `CarrotCMS.dbml`'s DataContext, `CompiledQueries.cs`
and the rest compile and run unchanged, and the CMS was migrated on webForCore 0.9.0 (no webForCore-thirdparty
package needed). The plug-in projects (Northwind, calendars, FAQ, photo gallery, Lorem Ipsum) aren't referenced by
the admin project and aren't built.

## Migration

1. `scripts/convert.cs CMSAdmin/CMSAdminMVC.csproj` with `--lib` for CMSData, CMSComponents, CMSCore, CMSDBUpdater,
   CMSInterfaces, CMSSecurity and WebComponents: SDK projects on net10.0 with webForCore's packages (MVC, Web Pages,
   Optimization, Katana, Identity), EntityFramework 6.5.2, Newtonsoft.Json 13.0.4, System.ServiceModel.Syndication as a
   package, `Program.cs` with session state. `Assets/Admin/CMS.asmx.cs` (not compiled by the old project) left out.
2. Every project `<LangVersion>7.3</LangVersion>`, and each class library
   `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (MIGRATING.md section 2).
3. `CMSCore/Utilities/CoreHelper.cs`: webForCore's `BinaryFormatter` (`webForCore.Serialization.Formatters`), as the
   guide says.
4. `CMSAdmin/Web.config`: `CarrotwareCMSConnectionString` points at the compose SQL Server. The
   `<add assembly="System.Data.Linq, ...">` entry stays (provided in 0.9.0). The mail pickup folder (`C:\Temp`) isn't
   changed: nothing the probes do sends mail.
5. `CMSCore/ConfigHelper/CMSConfigHelper.cs` (twice): `ServicePointManager.SecurityProtocol` without
   `SecurityProtocolType.Ssl3`. .NET throws `NotSupportedException: The requested security protocol is not supported`
   for SSL 3, and the setter runs in `CMSConfigHelper`'s constructor, so every page was a 500 (F158).
6. `CarrotCMSData/LinqHelper.cs` (its batch delete and update, which take the SQL that LINQ to SQL generates for a
   query from `GetCommand(...).CommandText` and splice it): `selectText.IndexOf(Environment.NewLine + "FROM ")` (and
   `ORDER BY`) instead of `"\r\nFROM "`. LINQ to SQL writes its SQL with `AppendLine()`, so on Linux the lines end
   with `\n` and the search found nothing: `ArgumentOutOfRangeException` in `Substring`, which the admin's home page
   hits (`CMSConfigHelper.CleanUpSerialData`) right after the log-in (F157).
7. `WebComponents/CarrotWeb.cs` `GetAssembly`: `HttpRuntime.BinDirectory` instead of
   `AppDomain.CurrentDomain.RelativeSearchPath`, which is null on .NET (on IIS the application's `bin`): the embedded
   resource handler (`/carrotwarewebresource.ashx`, the admin's and the templates' CSS and scripts) threw
   `ArgumentNullException (Parameter 'path1')` (F159).

## Smoke

**Round 0.9.0, Linux: 39/39** (32 of the app's probes and the runner's 7 forbidden paths), on a new database and again
on the same one, and from a fresh checkout.

`[setup]` does what the README's first run does, through the app: `/c3-admin/DatabaseSetup` creates the schema
(CMSDBUpdater's scripts), the first admin is created (`CreateFirstAdmin`, an MVC form with its anti-forgery token),
logs in, and creates the site (`SiteInfo`: a home and an index page; time zone `UTC`, which Windows and Linux both
know, where the form's default is the server's).

Probes: the home and index pages, RSS, an embedded resource, the admin's log-in page and its redirect for anonymous
users, a static stylesheet. Verbs, MVC forms with their anti-forgery tokens in one cookie jar: a wrong password and a
log-in; a category made, renamed and deleted (`CategoryAddEdit`, `CategoryDelete`); a blog post written
(`BlogPostAddEdit`, with ISO dates, which any culture parses, and the file name the editor's script would fill in),
shown at `/2026/01/02/smoke-probe-post`, edited, found by the site search, deleted (`DeleteContentPage`) and gone
from the post list; and log-off (a POST).

Not probed: the public contact and comment form (its CAPTCHA image), the plug-ins, and e-mail (password reset).

## Open problems

- F157 (guide gap): LINQ to SQL's generated SQL has the platform's newline; code that parses it for `\r\n` fails on
  Linux (fixed in the app here).
- F158 (guide gap): `SecurityProtocolType.Ssl3` throws on .NET (removed here).
- F159 (guide gap): `AppDomain.CurrentDomain.RelativeSearchPath` is null on .NET (`HttpRuntime.BinDirectory` here).

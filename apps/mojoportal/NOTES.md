# mojoPortal: migration notes

Status: migrated (webForCore 0.8.0: all 16 probes on Linux and Windows; F40 was fixed in 0.8.0). Source: https://github.com/i7MEDIA/mojoportal at `31b4fb1e`. Found by searching GitHub on 2026-10-31; the
repo, licence (EPL-1.0), web project and its `<TargetFrameworkVersion>` were checked through the GitHub API.

Round 3b, on webForCore 0.4.0 (no webForCore-thirdparty packages).

## Assessment

- **Frameworks:**
  - Web Forms (131 `.aspx`, and the features' `.ascx` modules);
  - MVC (the file manager);
  - Web API (user profile, file upload, bad-word check).
- **Target:** .NET 4.8.1.
- **Projects:**
  - **The site and its libraries:**
    - `Web`;
    - `mojoPortal.Core`, `.Business`, `.Business.WebHelpers`;
    - `.Web.Controls`, `.Web.Editor`, `.Web.Framework`;
    - `Brettle.Web.NeatHtml`;
    - the MSSQL data layer.
  - **The features:**
    - `mojoPortal.Features.UI` (blog, forums, galleries, ...: a separate web project that copies itself into `Web`);
    - `.Features.Business`;
    - `.Features.Data.MSSQL`.
- **Blocker:** WCF service hosting, in `Components/mojoServiceHost*.cs`, `Services/AuthenticationService.svc` and
  `RoleService.svc`. These are optional services for remote clients.
- **Database:** SQL Server, MySQL, PostgreSQL or SQLite, picked by build configuration (the data layer project). Here
  SQL Server from `services/compose.yml`, with an empty `mojoportal` database. `/Setup/Default.aspx` installs the schema
  and a default site.

Go, without the WCF services.

## Migration

1. `scripts/convert.py` on `Web` with the eight libraries and the three feature projects.
   - The data layer is a conditional `ProjectReference` (`'$(Configuration)'=='Debug'` / `'Release'` for MSSQL). The
     converter now keeps Debug and Release references.
   - The hint path to `_libs/Npgsql2011/Mono.Security.dll` is dropped; that file isn't in the repository.
2. `Controls/TagCloudControl.cs`: `System.Web.UI.Design` and the `[TypeConverter(typeof(DataFieldConverter))]` attribute
   are under `#if NETFRAMEWORK` (designers: documented gap).
3. `mojoPortal.Web.csproj`:
   - `Components/mojoServiceHost.cs` and `mojoServiceHostFactory.cs` are left out of the build (WCF hosting:
     documented gap). So is `Components/SMTP/TrustAllCertificatePolicy.cs`: .NET has no `ICertificatePolicy`, and
     nothing uses the class.
   - `Microsoft.Data.Services.Client` 5.8.5 is added for the Bing search control (System.Data.Services.Client on .NET
     Framework).
4. `Web.config`:
   - `MSSQLConnectionString` (the connection string and the `appSettings` key, which mojoPortal reads) points at the
     compose SQL Server;
   - `<add assembly="System.Data.Linq, ...">` is removed: .NET has no System.Data.Linq, and every page was a
     Configuration Error (F37).
5. `mojoPortal.Features.Business.csproj`: `PrivateAssets="all"` on the data layer's `ProjectReference`. With the
   reference transitive, the data layer's namespace `mojoPortal.MediaPlayer` hid the `MediaPlayer` class in
   Features.UI (CS0118, F42).
6. `mojoPortal.Features.UI.csproj` and `mojoPortal.Features.Data.MSSQL.csproj`: their Windows `xcopy` post-build events
   become an MSBuild target (`MojoDeployToWeb`) that copies the same files into `Web`, with `Web\bin` as `bin_dotnet`
   (F43). `app.toml`:
   - `also_build` builds both after the site;
   - `generated` keeps the copied help files out of the overlay.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 8/15 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28); it builds (F35 fixed); every page still fails on `Assembly.Load("App_GlobalResources")` (F40, #33, the release's known issue). With `optimizeCompilations="true"` all 15 checks pass on the released packages (not in the overlay).
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 6/15 probes on Linux and on Windows. The `mojoportal` database is new this round, so every page goes to `/Setup/Default.aspx`, which fails on `Assembly.Load("App_GlobalResources")` (F40, #33, 0.6.0's known issue; round 4's database had the schema, hence its 8/15). On Linux the other pages redirect to setup; on Windows they are 503: log4net's `Data/currentlog.config` is written relative to `BaseDirectory` (`bin_dotnet/`, F3), the change restarts the application, and the next start is shut down while it starts (F51, #49).
- **Round 6 (webForCore 0.7.0 on .NET 10, webForCore-thirdparty 0.5.0; under webforcore-host; Linux and Windows):** 7/16 probes on Linux and Windows. Unchanged: setup fails on `Assembly.Load("App_GlobalResources")` (F40, #33).

The probes cover:
- Setup;
- the home page;
- log-in and register (Web Forms forms);
- the file manager (MVC);
- a Web API controller (JSON 401);
- two generic handlers;
- a static file from the skin.

- **On 0.4.0:** the build fails with `CS0433: The type 'WsiProfiles' exists in both 'System.Web.Services.Description'
  and 'System.Web.Services'`, in the `.ashx` handlers (F35, webForCore bug,
  [#34](https://github.com/xforcore/webForCore/issues/34)).
- **On the packages of xforcore/webForCore#32** (F35 fixed): it builds. Every page then fails with
  `Could not load file or assembly 'App_GlobalResources, Culture=neutral, PublicKeyToken=null'`: the
  `App_GlobalResources` designer code does `Assembly.Load("App_GlobalResources")` (F40, webForCore bug,
  [#33](https://github.com/xforcore/webForCore/issues/33), not fixed). 8 of 15 checks pass: the forbidden-path checks
  and the static file.
- **With `<compilation optimizeCompilations="true">`** (exploration only, not in the overlay; it gives App_Code and
  App_GlobalResources their fixed names), all 9 probes pass. Setup installs the schema (2.3.4.8 → 3.2.0.0) and the
  default site on SQL Server. Then the home page, log-in, register, MVC, Web API, handlers and static file all work.

### Round 0.9.0 (webForCore 0.9.0, webForCore-thirdparty 0.7.0)

49/49 on Linux, on a new database and again on the same one. Changes:
- **System.Data.Linq is back in web.config.** 0.9.0 provides it (F37), so the overlay no longer removes
  `<add assembly="System.Data.Linq, Version=4.0.0.0, ...">`; every page compiles with it.
- **log4net writes to the site's `Data` folder** (`log4net.config`: `<file value="../Data/currentlog.config" />`,
  F154). log4net resolves its relative paths against `AppDomain.CurrentDomain.BaseDirectory`, which is
  `bin_dotnet/`, so the log went to `bin_dotnet/Data/currentlog.config`, and each line written into the
  application's `bin` restarted it (`BinDirChangeOrDirectoryRename`). mojoPortal logs "Application Started" at each
  start, so it restarted in a loop: 14 restarts in a smoke run (16 in round 0.8.0's server log), with the probes
  passing on whichever worker was up. Now there are none, and the admin's Server Log page shows the log (a probe).
- **The setup probe** looks for "Setup has finished successfully" instead of "Your installation is up to date",
  which `Setup/Default.aspx` shows only on its first run on a new database (`newInstall`): a second run on the same
  database failed it.

33 probes more, for verbs, all Web Forms postbacks (Admin pages check that a post's `Referer` is the same host,
`SiteUtils.IsFishyPost`, so they send one, as a browser does). mojoPortal splits its view state into fields of 500
characters (`maxPageStateFieldLength`), as many as a page needs (from 3 to more than 16 here), so each GET captures
`__VIEWSTATEFIELDCOUNT` and up to 40 `__VIEWSTATE<n>` fields, each with a regex that captures nothing when the
field isn't there, and its post sends them all (ASP.NET reads as many as the count says). The flow:
- a visitor registers (`Secure/Register.aspx`, the CreateUserWizard: "Your account has been created."), logs out,
  fails to log in, logs in, reads and sets a profile property through the Web API (`GET` and `POST
  /api/UserProfile/Property/FirstName`), checks a string with `POST /BadWord/CheckString`, and logs out;
- the administrator (`admin@admin.com`) logs in, reads the server log, makes a page (`Admin/PageSettings.aspx`), adds
  an HTML module to it (`Admin/PageLayout.aspx`), writes its content (`HtmlEdit.aspx`; anonymous users see it),
  deletes the page (404 after), finds the visitor in the member list and deletes the account
  (`Admin/ManageUsers.aspx`), and logs out, after which the admin pages redirect to the log-in.

## Open problems

- F40, webForCore bug: `Assembly.Load("App_GlobalResources")`. Not fixed; the issue has the analysis and a suggested
  fix.
- F35, webForCore bug: fixed in #32.
- F43, app/environment: the feature projects' post-build copies, done as MSBuild here.
- F37, F42: guide gaps, documented in #32.
- WCF hosting (the optional `.svc` services): a documented gap.

# ReaLocate: migration notes

Status: migrated (webForCore 0.2.0, all probes pass). Source: https://github.com/MichaelaIvanova/ReaLocate at
`0c2a3bd7`. Found by searching GitHub on 2026-10-31; the repo, licence (MIT), web project and its
`<TargetFrameworkVersion>` were checked through the GitHub API. webForCore 0.2.0 packages.

## Assessment

MVC 5 real-estate site: listings with maps, agencies, invoices, a SignalR 2 chat, ASP.NET Identity 2, EF 6 code first
(automatic migrations with a seed of roles and an admin), Autofac, AutoMapper 4.2, Glimpse, Rotativa, .NET Framework
4.6. Web project plus six libraries (data, models, services, web infrastructure). `scripts/wfc.py assess`: no blockers,
no notices.

- Dependencies: (a) replaced by webForCore: MVC 5, Web Pages, Optimization, Katana, Identity 2, SignalR 2. (b)
  .NET-compatible: EntityFramework (upgraded), Newtonsoft.Json (upgraded), Autofac 3.4, AutoMapper 4.2,
  Geocoding.net. (c) .NET Framework-only, tested: Autofac.Mvc5 3.3.4, Glimpse 1.x (Core, Ado, AspNet, EF6, Mvc5),
  Rotativa 1.6 (PDF invoices, not exercised).
- **Go**, with SQL Server from `services/compose.yml`.

## Migration

1. `scripts/convert.py` on the web project and the six libraries: SDK-style projects with the webForCore 0.2.0
   packages, EntityFramework → 6.5.1 and Newtonsoft.Json 8.0.2 → 13.0.4 (F7), `Program.cs`, `<system.codedom>`
   removed, `packages.config` removed. Builds first time.
2. web.config: `DefaultConnection` pointed at the compose SQL Server.
3. First smoke: Razor `MissingMethodException` (as in ApplicantTracking): Autofac.Mvc5 3.3.4 pulls in
   Microsoft.AspNet.Mvc 5.1.0 and its Razor and Web Pages. The four transitive packages referenced with
   `ExcludeAssets="all"`. [F5](../../FINDINGS.md#f5)
4. Next: `NullReferenceException` in `Glimpse.Ado.AlternateType.Support.FindDbProviderFactoryTable` from Glimpse.Ado's
   pre-application-start method (it reflects into .NET Framework's private `DbProviderFactories` table). With
   Glimpse.Ado and Glimpse.EF6 removed, `Glimpse.AspNet.HttpModule.Init` fails: `Method not found: 'System.String
   System.AppDomainSetup.get_ShadowCopyFiles()'`. Glimpse 1.x can't run on .NET; it's a development diagnostics tool,
   so all five Glimpse packages, the `glimpse` config section, its module and handler (`<httpModules>`, `<modules>`,
   `<httpHandlers>`, `<handlers>`) and `App_Start/GlimpseSecurityPolicy.cs` (`Compile Remove`) were removed.
   [F21](../../FINDINGS.md#f21)
5. Next: `ArgumentException: The type or method has 3 generic parameter(s), but 1 generic argument(s) were provided`
   in `AutoMapper.TypeDetails.BuildPublicNoArgExtensionMethods`, from `AutoMapperConfig.Execute`. AutoMapper scans
   `System.Linq.Enumerable`'s extension methods, non-public ones included, and calls `MakeGenericMethod` on each; .NET
   11's `Enumerable` has methods it can't handle (`MaxInteger<T>`, `MaxFloat<T>` with `IBinaryInteger`/`IFloatingPoint`
   constraints). Reproduced on plain .NET 11 with no webForCore: AutoMapper 11.0.1 fails, 12.0.1 works. Tried 4.2.1,
   5.2.0, 6.2.2, 10.1.1 and 11.0.1, all fail. AutoMapper 4.2.0 → 12.0.1 in both projects that reference it; the only
   source change is `IMapperConfiguration` → `IMapperConfigurationExpression` in
   `ReaLocate.Web.Infrastructure/Mapping/AutoMapperConfig.cs` and `IHaveCustomMappings.cs`.
   [F22](../../FINDINGS.md#f22)

## Smoke

Probes: log-in and register pages, the paged listing (`/Home/GetAllRealEstatesList`: EF 6 query through AutoMapper's
`ProjectTo`), an `[Authorize]` controller (302 to the log-in page from OWIN cookie authentication), the SignalR chat
page and the generated hub proxy (`/signalr/hubs`), and a static CSS file.

**All 7 app probes pass; the 6 forbidden paths return 404.** EF migrations create the database and seed the roles and
the admin user.

Not probed: `/` is a 500 (`Sequence contains no elements`) because `Views/Home/Index.cshtml:34` calls `m.First()` on the
listings in a `TextBoxFor` lambda, and the seed creates none. MVC 5 catches only `NullReferenceException` there, so it's
the same on .NET Framework. The PDF invoices (Rotativa, which runs `wkhtmltopdf.exe`) weren't exercised.

## Open problems

- [F22](../../FINDINGS.md#f22) (app/environment): AutoMapper before 12 doesn't run on .NET 11. Upgraded.
- [F21](../../FINDINGS.md#f21) (guide gap): Glimpse doesn't run on .NET. Removed.

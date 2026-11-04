# OData Web API samples: composite key: migration notes

Status: blocked (webForCore 0.7.0, 8/15 probes on Linux and on Windows): Microsoft.AspNet.OData 7.5.18 needs System.Data.Linq
([F68](../../FINDINGS.md#f68)).

OData/ODataSamples at `2e60476b`, MIT (`License.txt`). `WebApiClassic/ODataCompositeKeySample/ODataCompositeKeySample.AspNet`:
an OData v4 service on Web API 2 with Microsoft.AspNet.OData 7.5.18, hosted by System.Web (`Global.asax`,
`GlobalConfiguration`), one entity set (`People`) with a two-part key, in memory. webForCore 0.7.0 packages
(0.8.0 wasn't released when this was migrated), webForCore-thirdparty 0.6.0 (not used by the app), drawingForCore 0.2.0.

## Assessment

`scripts/wfc.py assess`: no blockers. Web API 2 (`webapi`), .NET Framework 4.8 (`<compilation targetFramework="4.8">`;
no `<httpRuntime targetFramework>`, so webForCore's 4.5+ behaviour is what it gets). Packages:

- (a) replaced by webForCore: Microsoft.AspNet.WebApi.Core, .WebHost, .Client;
- (b) kept: Microsoft.AspNet.OData 7.5.18 (`lib/net45` only: there is no .NET build; ASP.NET Core has its own
  Microsoft.AspNetCore.OData), Microsoft.OData.Core/Edm/Client and Microsoft.Spatial 7.12.2 (.NET Standard),
  Microsoft.Extensions.DependencyInjection 6.0;
- (c) none.

## Migration

- `scripts/convert.py`: `ODataCompositeKeySample.AspNet.csproj` → `.csproj.netfx`; SDK project with `webForCore` and
  `webForCore.Web.Http.WebHost` 0.7.0; the app's packages kept, Newtonsoft.Json 13.0.1 → 13.0.4 (F7); the System.*
  packages that are part of .NET dropped; `packages.config` → `packages.config.netfx`; `Program.cs` (no session state).
  The `<Compile>` list equals the glob. Microsoft.AspNet.OData depends on Microsoft.AspNet.WebApi.Core 5.2.x, which
  restores, and webForCore's build targets keep webForCore's `System.Web.Http` (F5): no CS0433.
- Nothing else. `web.config` is unchanged.

The build passes with no warnings about it.

## Smoke

Round 7 candidates (webForCore 0.7.0): **8/15 on Linux and on Windows** (Windows: CI run 41898913795). The seven forbidden-path checks pass; every OData probe is a
404 (`StaticFileHandler`: no route).

`Application_Start` throws while building the EDM model, so `MapODataServiceRoute` never runs:

```
System.TypeInitializationException: The type initializer for 'Microsoft.AspNet.OData.Formatter.EdmLibHelpers' threw an exception.
 ---> System.IO.FileNotFoundException: Could not load file or assembly 'System.Data.Linq, Version=4.0.0.0, ...'
   at Microsoft.AspNet.OData.Formatter.EdmLibHelpers..cctor()
   at Microsoft.AspNet.OData.Builder.ODataConventionModelBuilder.GetEdmModel()
   at ODataCompositeKeySample.WebApiApplication.Application_Start() in Global.asax.cs:line 26
```

As on IIS, the error is the first request's (a 500 with that exception, checked by hand); the runner's readiness
request takes it, and later requests run with no routes. `EdmLibHelpers` maps `System.Data.Linq.Binary` to
`Edm.Binary` in its static type table, so any OData model needs LINQ to SQL's assembly, which .NET doesn't have
(F68).

## Open problems

- [F68](../../FINDINGS.md#f68) (documented gap, consequence undocumented; raised as [#120](https://github.com/xforcore/webForCore/issues/120)): Microsoft.AspNet.OData 7.5.18
  needs `System.Data.Linq` for `Binary`. Blocks every OData model.

# YAF.NET 2.x: migration notes

Status: blocked. Source: https://github.com/YAFNET/YAFNET at `7435be38` (tag v2.31.16; master is ASP.NET Core now).
Licence Apache-2.0. Round 4, on webForCore 0.5.0 and webForCore-thirdparty 0.4.1.

## Assessment

- **Frameworks:** Web Forms, with Web API controllers in YAF.Core. Targets .NET 4.7.2.
- **Projects:**
  - **The site:** `yafsrc/YetAnotherForum.NET` (old-style project);
  - **YAF's libraries:** Configuration, Core, Data.MsSql, Providers, Types, UrlRewriter, Utils, Web. These are already
    SDK-style but target only `net472`;
  - **Vendored libraries:** Lucene.Net (5 projects, old-style) and ServiceStack 5.9 (Common, Interfaces, OrmLite,
    OrmLite.SqlServer, Text). The ServiceStack projects are SDK-style.
- **Flagged blockers:**
  - `System.Data.Linq.Mapping` in the 20 `YAF.Types/Models`: `[Table(Name = ...)]` attributes that nothing reads.
  - `WindowsIdentity.GetCurrent()` / `Impersonate()` in `YAF.Core/Tasks/IntermittentBackgroundTask.cs:75,103`. It throws on
    Linux, but only in the background tasks.
- **Configuration:** web.config ships as `recommended.web.config`, which the install guide copies. SQL Server.

## Migration

1. `scripts/convert.py` on the site and the 18 libraries. The converter needed three changes for this app:
   - **SDK-style projects that target only .NET Framework** are edited in place: `net11.0`, their framework references
     and Microsoft packages replaced with webForCore's, everything else kept. Rewritten from scratch as before, they
     lost their globbed sources: `YAF.Core.BaseControls` and the rest were missing.
   - `System.Data.SqlClient` 4.8.1 → 4.9.1, the version webForCore requires (F45).
   - **The C# version** they were compiled with is kept: 13 where "latest" was set, 7.3 where nothing was. This fixes
     C# 14's span-conversion errors in ServiceStack.Text (CS0029/CS1503/CS1929, F48).
2. `YAF.Types/Models/*.cs`: the inert `using System.Data.Linq.Mapping;` and `[Table(Name = ...)]` removed (20 files).
   System.Data.Linq isn't on .NET (documented gap); OrmLite uses its own attributes.

## Smoke

Not reached. The build stops in the vendored ServiceStack.Text.

- **C# 11 ref safety**, which the compiler applies on .NET 7+ whatever the language version: `CS8347: Cannot use a result
  of 'JsonTypeSerializer.UnescapeJsonString(ReadOnlySpan<char>, ref int)' in this context because it may expose
  variables referenced by parameter 'index'`, and `CS8168`. Fixable with `scoped ref int index` (F48).
- **`NetStandardPclExport` missing.** ServiceStack chooses its platform with `#if NET472` / `NETCORE2_1` / `NETSTANDARD`.
  On net11.0 none is defined, and the fallback branch's file is compiled out.
  - The `NET472` path YAF.NET ran needs `AppDomain.DefineDynamicAssembly` (`PclExport.Net45.cs:964`), which .NET doesn't
    have (as Roadkill's F27).
  - The .NET Standard and .NET Core paths call `LicenseUtils`, which YAF.NET's fork removed.

  Either way the vendored library would need repairing (F49, app/environment).

## Open problems

- F49, app/environment: YAF.NET's vendored ServiceStack 5.9 fork only works as .NET Framework code.
- F48, guide gap: [webForCore#43](https://github.com/jabba2324/webForCore/issues/43).
- F45, guide gap: [webForCore#42](https://github.com/jabba2324/webForCore/issues/42).

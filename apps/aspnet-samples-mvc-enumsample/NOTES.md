# ASP.NET samples: MVC EnumSample: migration notes

Status: migrated (webForCore 0.4.0, all probes pass).
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages (0.2.0 from round 2).

## Assessment

MVC 5.1's enum support (`EnumDropDownListFor`, `[Display]` names, a `[Flags]` enum, display and editor templates) over EF 6.1 code first, LocalDB with `AttachDbFilename`. .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `EnumSample.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore`, `webForCore.Web.Mvc`, `.Web.Helpers` and `.Web.WebPages.Deployment` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). EntityFramework 6.1.1 → 6.5.1 ([F7](../../FINDINGS.md#f7)). web.config: the LocalDB connection string (with `AttachDbFilename`, which LocalDB-less Linux can't do) pointed at the compose SQL Server.

## Smoke

- **Round 4 (webForCore 0.5.0, webForCore-thirdparty 0.4.1):** 8/8 probes; the explicit `webForCore.Web.Helpers` reference is removed, as webForCore.Web.Mvc brings it since 0.5.0 (#28).
- **Round 5 (webForCore 0.6.0, webForCore-thirdparty 0.4.1; Linux and Windows):** 8/8 probes on Linux and on Windows.

**8 of 9 pass**: the index page and the Create form render (the enum dropdown shows the `[Display]` names: Zéro, Un, Duex, ...); EF creates the database on SQL Server. By hand: `POST /Home/Create` with `Enum1=1&FlagsEnum=2` returns 302 and the row is listed. The only failure is the served W3C log ([F4](../../FINDINGS.md#f4)).

### Round 2: webForCore 0.2.0

All 8 probes pass on the released 0.2.0 packages (the overlay's `PackageReference`s moved from 0.1.0 to 0.2.0). The round-1 failures above were fixed in webForCore PR #16 and are kept as the record of round 1.

### Round 3: webForCore 0.4.0

All 8 probes pass on the 0.4.0 packages. The overlay moved to 0.4.0.

## Open problems

Round 1 (webForCore 0.1.0); the webForCore bugs below are fixed in 0.2.0 (PR #16) and kept for the record.

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

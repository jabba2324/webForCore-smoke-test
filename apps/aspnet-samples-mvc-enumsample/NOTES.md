# ASP.NET samples: MVC EnumSample: migration notes

Status: blocked. Source: https://github.com/aspnet/samples at `f7d4a776`, `samples/aspnet/MVC/EnumSample/EnumSample`. Apache-2.0
(`LICENSE.txt`). Split out of the `aspnet-samples` entry. webForCore 0.1.0 packages.

## Assessment

MVC 5.1's enum support (`EnumDropDownListFor`, `[Display]` names, a `[Flags]` enum, display and editor templates) over EF 6.1 code first, LocalDB with `AttachDbFilename`. .NET 4.5. No blockers.

## Migration

Done with the mechanical converter (Phase 2 of `docs/AGENT-MIGRATION.md`): `EnumSample.csproj` → `.csproj.netfx`; SDK-style
project with the `webForCore`, `webForCore.Web.Mvc`, `.Web.Helpers` and `.Web.WebPages.Deployment` 0.1.0 packages; the old `<Compile>` list equals the glob; `packages.config` removed; `Program.cs` added (no
session state). EntityFramework 6.1.1 → 6.5.1 ([F7](../../FINDINGS.md#f7)). web.config: the LocalDB connection string (with `AttachDbFilename`, which LocalDB-less Linux can't do) pointed at the compose SQL Server.

## Smoke

**8 of 9 pass**: the index page and the Create form render (the enum dropdown shows the `[Display]` names: Zéro, Un, Duex, ...); EF creates the database on SQL Server. By hand: `POST /Home/Create` with `Enum1=1&FlagsEnum=2` returns 302 and the row is listed. The only failure is the served W3C log ([F4](../../FINDINGS.md#f4)).

## Open problems

- [F4](../../FINDINGS.md#f4) (webForCore bug): W3C log served.

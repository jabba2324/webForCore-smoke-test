#!/usr/bin/env python3
"""Phase 2 of webForCore's docs/AGENT-MIGRATION.md, done mechanically against webForCore's release packages.

    scripts/convert.py apps/<id>/src/<web project>.csproj [--lib apps/<id>/src/<library>.csproj]...

- copies each project to <name>.csproj.netfx (once) and rewrites it as an SDK-style net11.0 project;
- adds the webForCore packages for the frameworks it references ($WEBFORCORE_VERSION, default 0.1.0); keeps the app's
  other packages (from packages.config or PackageReference), drops Microsoft's ASP.NET packages and the System.*
  packages that are part of .NET, and raises the versions webForCore forces (FINDINGS.md F7); keeps HintPath
  references outside packages/;
- removes from the build any .cs file the glob would add that the old <Compile> list didn't have;
- gives the web project a Program.cs (with session state if the code uses Session) and removes <system.codedom>.

It prints what it did, for NOTES.md. It is a starting point, not the whole migration: check its output, then build.
It doesn't handle packages that bring Microsoft's ASP.NET packages back transitively (F5), Katana packages
webForCore doesn't provide (F15), lib/net45 assemblies NuGet skips (F6), connection strings or Linux paths.
Standard library only (Python 3.11+)."""
import re, shutil, sys
from pathlib import Path

WFC = __import__("os").environ.get("WEBFORCORE_VERSION", "0.1.0")
DROP = re.compile(r"(?i)^(Microsoft\.AspNet\.(Mvc|Razor|WebPages|WebApi.*|Web\.Optimization.*|Cors|SignalR.*|Identity.*|Providers.*|FriendlyUrls.*|ScriptManager.*|TelemetryCorrelation|SessionState.*)|Microsoft\.Owin.*|Owin|Microsoft\.Web\.Infrastructure|WebGrease|Antlr|Microsoft\.CodeDom\.Providers\..*|Microsoft\.Net\.Compilers.*|Microsoft\.Net\.Compilers\.Toolset|"
                  r"System\.(Buffers|Memory|Numerics\.Vectors|Runtime\.CompilerServices\.Unsafe|Threading\.Tasks\.Extensions|ValueTuple|Net\.Http|IO|IO\.Compression|IO\.Compression\.ZipFile|Diagnostics\.DiagnosticSource|Threading\.Channels|IO\.Pipelines|Text\.Encodings\.Web|Text\.Json|Runtime|Collections\.Immutable|Reflection\.Metadata|Security\.Cryptography\..*|Net\.Primitives|Net\.Sockets|Linq|Console|Globalization)|Microsoft\.Bcl.*|NETStandard\.Library|Microsoft\.CSharp)$")
# Microsoft packages webForCore doesn't replace, kept (they may load): the ones in DROP's Microsoft.AspNet group that
# aren't replaced are re-added below from KEEP_MS.
KEEP_MS = re.compile(r"(?i)^Microsoft\.AspNet\.(FriendlyUrls.*|ScriptManager.*|TelemetryCorrelation|SessionState.*|Providers.*)$")
MIN_VERSIONS = {"newtonsoft.json": "13.0.4", "entityframework": "6.5.1"}
FRAMEWORKS = [  # (regex over references and packages, webForCore package)
    (r"System\.Web\.Mvc\b|Microsoft\.AspNet\.Mvc\b", "webForCore.Web.Mvc"),
    (r"Microsoft\.Web\.Mvc\b|Mvc\.Futures", "webForCore.Microsoft.Web.Mvc"),
    (r"System\.Web\.Http\.WebHost|Microsoft\.AspNet\.WebApi\.WebHost", "webForCore.Web.Http.WebHost"),
    (r"System\.Web\.Http\.Owin|Microsoft\.AspNet\.WebApi\.Owin", "webForCore.Web.Http.Owin"),
    (r"System\.Web\.Http\.Cors|Microsoft\.AspNet\.WebApi\.Cors", "webForCore.Web.Http.Cors"),
    (r"System\.Web\.Cors\b|Microsoft\.AspNet\.Cors\b", "webForCore.Web.Cors"),
    (r"System\.Web\.Helpers\b", "webForCore.Web.Helpers"),
    (r"System\.Web\.WebPages\.Razor|Microsoft\.AspNet\.WebPages\b", "webForCore.Web.WebPages.Razor"),
    (r"System\.Web\.WebPages\.Deployment", "webForCore.Web.WebPages.Deployment"),
    (r"System\.Web\.Optimization\b|Microsoft\.AspNet\.Web\.Optimization\b", "webForCore.Web.Optimization"),
    (r"Microsoft\.AspNet\.Web\.Optimization\.WebForms", "webForCore.Web.Optimization.WebForms"),
    (r"Microsoft\.Owin\.Host\.SystemWeb", "webForCore.Microsoft.Owin.Host.SystemWeb"),
    (r"Microsoft\.Owin\.Security\.Cookies", "webForCore.Microsoft.Owin.Security.Cookies"),
    (r"Microsoft\.Owin\.Security\.OAuth", "webForCore.Microsoft.Owin.Security.OAuth"),
    (r"Microsoft\.Owin\.Cors", "webForCore.Microsoft.Owin.Cors"),
    (r"Microsoft\.AspNet\.Identity\.Owin", "webForCore.Microsoft.AspNet.Identity.Owin"),
    (r"Microsoft\.AspNet\.Identity\.EntityFramework", "webForCore.Microsoft.AspNet.Identity.EntityFramework"),
    (r"Microsoft\.AspNet\.Identity\.Core", "webForCore.Microsoft.AspNet.Identity.Core"),
    (r"Microsoft\.AspNet\.SignalR\.SystemWeb|Microsoft\.AspNet\.SignalR\b", "webForCore.Microsoft.AspNet.SignalR.SystemWeb"),
    (r"System\.Web\.DataVisualization", "webForCore.Web.DataVisualization"),
    (r"AjaxControlToolkit", "webForCore.AjaxControlToolkit"),
]
WEB_PROPS = """    <TargetFramework>net11.0</TargetFramework>
    <OutputType>Exe</OutputType>{names}
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>{asminfo}
    <OutputPath>bin_dotnet</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
    <GenerateDependencyFile>true</GenerateDependencyFile>
    <NoWarn>$(NoWarn);NU1701</NoWarn>
    <PredefinedCulturesOnly>true</PredefinedCulturesOnly>{razor}"""
LIB_PROPS = """    <TargetFramework>net11.0</TargetFramework>{names}
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>{asminfo}
    <NoWarn>$(NoWarn);NU1701</NoWarn>"""
RAZOR = """
    <EnableDefaultRazorGenerateItems>false</EnableDefaultRazorGenerateItems>
    <RazorCompileOnBuild>false</RazorCompileOnBuild>
    <RazorCompileOnPublish>false</RazorCompileOnPublish>"""


def version_key(v):
    return [int(x) if x.isdigit() else 0 for x in re.split(r"[.-]", v)]


def convert(proj, web):
    report = []
    netfx = proj.with_name(proj.name + ".netfx")
    if not netfx.exists():
        shutil.copy(proj, netfx)
    old = netfx.read_text(encoding="utf-8-sig", errors="replace")
    d = proj.parent
    asm = re.search(r"<AssemblyName>([^<]+)<", old)
    ns = re.search(r"<RootNamespace>([^<]+)<", old)
    names = "".join(f"\n    <{t}>{m.group(1)}</{t}>" for t, m in (("RootNamespace", ns), ("AssemblyName", asm)) if m)
    compiles = [c.replace("\\", "/") for c in re.findall(r'<Compile Include="([^"]+)"', old)]
    asminfo = "\n    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>" if any(c.endswith("AssemblyInfo.cs") for c in compiles) else ""
    # packages
    pkgs = {}
    pc = d / "packages.config"
    if pc.exists():
        for m in re.finditer(r'<package\s+id="([^"]+)"\s+version="([^"]+)"', pc.read_text(errors="replace")):
            pkgs[m.group(1)] = m.group(2)
    for m in re.finditer(r'<PackageReference\s+Include="([^"]+)"(?:\s+Version="([^"]+)")?\s*/?>(?:\s*<Version>([^<]+)</Version>)?', old):
        pkgs[m.group(1)] = m.group(2) or m.group(3) or "*"
    refs = re.findall(r'<Reference\s+Include="([^"]+)"', old)
    everything = "\n".join(refs + list(pkgs))
    wfc = ["webForCore"] + [p for pat, p in FRAMEWORKS if re.search(pat, everything)]
    if "webForCore.Web.Mvc" in wfc:  # MVC brings Razor and Web Pages
        wfc = [p for p in wfc if p not in ("webForCore.Web.WebPages.Razor",)]
    kept, dropped = [], []
    for name, ver in sorted(pkgs.items(), key=lambda kv: kv[0].lower()):
        if DROP.match(name) and not KEEP_MS.match(name):
            dropped.append(name)
            continue
        low = name.lower()
        if low in MIN_VERSIONS and version_key(ver) < version_key(MIN_VERSIONS[low]):
            report.append(f"{name} {ver} -> {MIN_VERSIONS[low]}")
            ver = MIN_VERSIONS[low]
        kept.append((name, ver))
    hints = []
    for m in re.finditer(r'<Reference\s+Include="([^",]+)[^"]*"[^>]*>\s*(?:<[^H][^>]*>[^<]*</[^>]+>\s*)*<HintPath>([^<]+)</HintPath>', old):
        name, hint = m.group(1), m.group(2).replace("\\", "/")
        if "/packages/" in "/" + hint:
            continue
        hints.append((name, hint))
    # compile list: anything the SDK glob adds that the old project didn't compile
    globbed = {str(p.relative_to(d)) for p in d.rglob("*.cs") if not any(x in p.relative_to(d).parts for x in ("bin", "obj", "bin_dotnet"))
               and not str(p.relative_to(d)).lower().startswith("app_code/")}
    extra = sorted(globbed - set(compiles) - {"Program.cs"})
    missing = sorted(set(c for c in compiles if not c.startswith("..")) - globbed)
    linked = [c for c in compiles if c.startswith("..")]
    projrefs = [r.replace("\\", "/") for r in re.findall(r'<ProjectReference\s+Include="([^"]+)"', old)]
    props = (WEB_PROPS if web else LIB_PROPS).format(names=names, asminfo=asminfo, razor=RAZOR if web and list(d.rglob("*.cshtml")) else "")
    out = [f'<Project Sdk="Microsoft.NET.Sdk{".Web" if web else ""}">',
           f"  <!-- Migrated to webForCore (.NET 11). The .NET Framework project is {netfx.name}. -->",
           "  <PropertyGroup>", props, "  </PropertyGroup>", ""]
    items = ['    <Compile Remove="App_Code/**;bin_dotnet/**;obj/**" />'] if web else ['    <Compile Remove="bin/**;obj/**" />']
    if web:
        items += ['    <Content Remove="bin_dotnet/**" />', '    <None Remove="bin_dotnet/**" />']
    items += [f'    <Compile Remove="{e}" />' for e in extra] + [f'    <Compile Include="{c}" Link="{Path(c).name}" />' for c in linked]
    out += ["  <ItemGroup>", *items, "  </ItemGroup>", ""]
    out += ["  <ItemGroup>", f"    <!-- webForCore {WFC} packages (GitHub release): \"webForCore\" is the \"Always\" set. -->",
            *[f'    <PackageReference Include="{p}" Version="{WFC}" />' for p in wfc], "  </ItemGroup>", ""]
    if kept or hints:
        out += ["  <ItemGroup>", "    <!-- The app's own libraries. -->", *[f'    <PackageReference Include="{n}" Version="{v}" />' for n, v in kept],
                *[f'    <Reference Include="{n}" HintPath="{h}" />' for n, h in hints], "  </ItemGroup>", ""]
    if projrefs:
        out += ["  <ItemGroup>", *[f'    <ProjectReference Include="{r}" />' for r in projrefs], "  </ItemGroup>", ""]
    out.append("</Project>")
    proj.write_text("\n".join(out) + "\n")
    print(f"== {proj.name}: webForCore {', '.join(wfc)}")
    print(f"   kept: {', '.join(f'{n} {v}' for n, v in kept) or '-'}")
    print(f"   dropped: {', '.join(dropped) or '-'}")
    print(f"   hint paths: {hints or '-'}; project refs: {projrefs or '-'}")
    print(f"   compile: {len(compiles)} listed, {len(extra)} extra on disk removed {extra[:5]}, {len(missing)} listed but missing {missing[:5]}, linked {linked[:3]}")
    for r in report:
        print("   upgraded:", r)
    if pc.exists():
        pc.unlink()
        print("   packages.config removed")
    if web:
        uses_session = any(re.search(r"\bSession\[|\bSession\.|Session_Start", p.read_text(errors="replace"))
                           for p in d.rglob("*") if p.suffix.lower() in (".cs", ".cshtml", ".aspx", ".ascx", ".asax", ".master") and "bin_dotnet" not in p.parts)
        if not (d / "Program.cs").exists():
            ns_name = (ns.group(1) if ns else proj.stem)
            session_svc = "\n            builder.Services.AddDistributedMemoryCache();\n            builder.Services.AddSession();" if uses_session else ""
            (d / "Program.cs").write_text(f"""using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebFormsForCore.Hosting.IIS;

namespace {ns_name}
{{
    // webForCore host (docs/MIGRATING.md section 4).{" The app uses session state." if uses_session else ""}
    public class Program
    {{
        public static void Main(string[] args)
        {{
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddWebForms();{session_svc}

            var app = builder.Build();{chr(10) + "            app.UseSession();" if uses_session else ""}
            app.UseWebForms(o => o
                .HandleAllRequestsWithWebForms(){chr(10) + "                .UseAspNetCoreSessionProvider()" if uses_session else ""});
            app.Run();
        }}
    }}
}}
""")
            print(f"   Program.cs added (session: {uses_session})")
        wc = next((p for p in d.iterdir() if p.name.lower() == "web.config"), None)
        if wc:
            text = wc.read_text(encoding="utf-8-sig", errors="replace")
            new = re.sub(r"\n?[ \t]*<system\.codedom>.*?</system\.codedom>", "", text, flags=re.S)
            if new != text:
                wc.write_text(new, encoding="utf-8-sig" if wc.read_bytes().startswith(b"\xef\xbb\xbf") else "utf-8")
                print(f"   {wc.name}: <system.codedom> removed")


args = sys.argv[1:]
if not args or args[0] in ("-h", "--help") or not Path(args[0]).is_file():
    sys.exit(__doc__ if not args or args[0] in ("-h", "--help") else f"no such project: {args[0]}")
web_proj = Path(args[0])
libs = [Path(a) for i, a in enumerate(args) if i > 0 and args[i - 1] == "--lib"]
convert(web_proj, True)
for lib in libs:
    convert(lib, False)

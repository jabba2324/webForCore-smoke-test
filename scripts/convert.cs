// Phase 2 of webForCore's docs/AGENT-MIGRATION.md, done mechanically against webForCore's release packages.
//
//     dotnet run scripts/convert.cs -- apps/<id>/src/<web project>.csproj [--lib apps/<id>/src/<library>.csproj]...
//
// - copies each project to <name>.csproj.netfx (once) and rewrites it as an SDK-style net10.0 project;
// - adds the webForCore packages for the frameworks it references ($WEBFORCORE_VERSION, default 0.10.0); keeps the
//   app's other packages (from packages.config or PackageReference), drops Microsoft's ASP.NET packages and the System.*
//   packages that are part of .NET, and raises the versions webForCore forces (FINDINGS.md F7); keeps HintPath
//   references outside packages/;
// - removes from the build any .cs file the glob would add that the old <Compile> list didn't have;
// - gives the web project a Program.cs (with session state if the code uses Session) and removes <system.codedom>.
//
// It prints what it did, for NOTES.md. It is a starting point, not the whole migration: check its output, then build.
// It keeps the Katana providers webForCore doesn't provide (Google, Facebook, WsFederation, ...); webForCore's own
// Katana packages win over their dependencies (0.3.0). For WebActivatorEx and AjaxControlToolkit it adds the builds
// from webForCore-thirdparty ($THIRDPARTY_VERSION, default 0.7.0; CLAUDE.md). It doesn't handle lib/net45 assemblies
// NuGet skips (F6), connection strings or Linux paths.
#:property PublishAot=false
#:property NuGetAudit=false
#:property Nullable=disable
using System.Text;
using System.Text.RegularExpressions;

return Convert.Entry(args);

static class Convert
{
    const string Doc = """
        Phase 2 of webForCore's docs/AGENT-MIGRATION.md, done mechanically against webForCore's release packages.

            dotnet run scripts/convert.cs -- apps/<id>/src/<web project>.csproj [--lib apps/<id>/src/<library>.csproj]...

        The rest is at the top of scripts/convert.cs.
        """;

    static readonly string Wfc = Environment.GetEnvironmentVariable("WEBFORCORE_VERSION") ?? "0.10.0";
    static readonly string Thirdparty = Environment.GetEnvironmentVariable("THIRDPARTY_VERSION") ?? "0.7.0";
    // webForCore-thirdparty's builds: (regex over references and packages, its package). The original package is kept
    // (packages such as Unity.Mvc bring it anyway); the build wins the conflict.
    static readonly (string Pattern, string Package)[] ThirdpartyPackages = [("WebActivatorEx", "webForCore.WebActivatorEx"), ("AjaxControlToolkit", "webForCore.AjaxControlToolkit")];
    static readonly Regex Drop = new(@"(?i)^(Microsoft\.AspNet\.(Mvc|Razor|WebPages|WebApi.*|Web\.Optimization.*|Cors|SignalR.*|Identity.*|Providers.*|FriendlyUrls.*|ScriptManager.*|TelemetryCorrelation|SessionState.*)|Microsoft\.Owin.*|Owin|Microsoft\.Web\.Infrastructure|WebGrease|Antlr|Microsoft\.CodeDom\.Providers\..*|Microsoft\.Net\.Compilers.*|Microsoft\.Net\.Compilers\.Toolset|" +
        @"System\.(Buffers|Memory|Numerics\.Vectors|Runtime\.CompilerServices\.Unsafe|Threading\.Tasks\.Extensions|ValueTuple|Net\.Http|IO|IO\.Compression|IO\.Compression\.ZipFile|Diagnostics\.DiagnosticSource|Threading\.Channels|IO\.Pipelines|Text\.Encodings\.Web|Text\.Json|Runtime|Collections\.Immutable|Reflection\.Metadata|Security\.Cryptography\..*|Net\.Primitives|Net\.Sockets|Linq|Console|Globalization|Configuration\.ConfigurationManager|Runtime\.Caching|CodeDom|Security\.Permissions)|Microsoft\.Bcl.*|NETStandard\.Library|Microsoft\.CSharp)$");
    // Microsoft packages webForCore doesn't replace, kept (they may load): the ones in Drop's Microsoft.AspNet group that
    // aren't replaced are re-added below from KeepMs.
    static readonly Regex KeepMs = new(@"(?i)^Microsoft\.AspNet\.(FriendlyUrls.*|ScriptManager.*|TelemetryCorrelation|SessionState.*|Providers.*)$");
    // Katana packages webForCore doesn't lift (docs/MIGRATING.md, "Katana packages webForCore doesn't lift"): kept.
    static readonly Regex KatanaKeep = new(@"(?i)^Microsoft\.Owin\.Security\.(Facebook|Google|MicrosoftAccount|Twitter|WsFederation|Jwt|OpenIdConnect|ActiveDirectory)$");
    // Other Katana and SignalR packages webForCore doesn't ship (JabbR: Owin Diagnostics, FileSystems and StaticFiles;
    // SignalR's SQL Server and Service Bus scale-out), kept at the versions webForCore lifted (Katana 4.2.3, SignalR
    // 2.4.3; docs/NAMESPACES.md), so their own dependencies are webForCore's.
    static readonly Dictionary<string, string> OwinSignalrKeep = new()
    {
        ["microsoft.owin.diagnostics"] = "4.2.3", ["microsoft.owin.filesystems"] = "4.2.3", ["microsoft.owin.staticfiles"] = "4.2.3",
        ["microsoft.owin.selfhost"] = "4.2.3", ["microsoft.aspnet.signalr.sqlserver"] = "2.4.3",
        ["microsoft.aspnet.signalr.servicebus"] = "2.4.3", ["microsoft.aspnet.signalr.redis"] = "2.4.3",
    };
    // Versions webForCore's packages need (NU1605 below them): its Web API (Newtonsoft.Json), EF 6's first .NET build,
    // and Roslyn in webForCore.Compilers (Humanizer.Core).
    static readonly Dictionary<string, string> MinVersions = new()
    {
        ["newtonsoft.json"] = "13.0.4", ["entityframework"] = "6.5.1", ["humanizer.core"] = "2.14.1",
        // packages webForCore depends on: its versions are the floor (NU1605 below them; FINDINGS F45)
        ["system.data.sqlclient"] = "4.9.1", ["system.directoryservices"] = "10.0.14",
        ["system.directoryservices.accountmanagement"] = "10.0.14",
    };
    // .NET Framework assemblies that are NuGet packages on .NET (docs/MIGRATING.md, #9): a <Reference> to one becomes a
    // PackageReference at the .NET version webForCore is built with.
    const string NetPackagesVersion = "10.0.14";
    static readonly string[] FrameworkPackages = ["System.ComponentModel.Composition", "System.ServiceModel.Syndication", "System.Data.OleDb",
        "System.DirectoryServices", "System.DirectoryServices.AccountManagement", "System.Data.Odbc"];
    static readonly (string Pattern, string Package)[] Frameworks = // (regex over references and packages, webForCore package)
    [
        (@"System\.Web\.Mvc\b|Microsoft\.AspNet\.Mvc\b", "webForCore.Web.Mvc"),
        (@"Microsoft\.Web\.Mvc\b|Mvc\.Futures", "webForCore.Microsoft.Web.Mvc"),
        (@"System\.Web\.Http\.WebHost|Microsoft\.AspNet\.WebApi\.WebHost", "webForCore.Web.Http.WebHost"),
        (@"System\.Web\.Http\.Owin|Microsoft\.AspNet\.WebApi\.Owin", "webForCore.Web.Http.Owin"),
        (@"System\.Web\.Http\.Cors|Microsoft\.AspNet\.WebApi\.Cors", "webForCore.Web.Http.Cors"),
        (@"System\.Web\.Cors\b|Microsoft\.AspNet\.Cors\b", "webForCore.Web.Cors"),
        // Microsoft.AspNet.WebPages contains System.Web.Helpers. webForCore.Web.Mvc brings webForCore.Web.Helpers since
        // 0.5.0 (webForCore#28), so it's added only for projects without MVC (below).
        (@"System\.Web\.Helpers\b|Microsoft\.AspNet\.WebPages\b", "webForCore.Web.Helpers"),
        (@"System\.Web\.WebPages\.Razor|Microsoft\.AspNet\.WebPages\b", "webForCore.Web.WebPages.Razor"),
        (@"System\.Web\.WebPages\.Deployment", "webForCore.Web.WebPages.Deployment"),
        (@"System\.Web\.Optimization\b|Microsoft\.AspNet\.Web\.Optimization\b", "webForCore.Web.Optimization"),
        (@"Microsoft\.AspNet\.Web\.Optimization\.WebForms", "webForCore.Web.Optimization.WebForms"),
        (@"Microsoft\.Owin\.Host\.SystemWeb", "webForCore.Microsoft.Owin.Host.SystemWeb"),
        (@"Microsoft\.Owin\.Security\.Cookies", "webForCore.Microsoft.Owin.Security.Cookies"),
        (@"Microsoft\.Owin\.Security\.OAuth", "webForCore.Microsoft.Owin.Security.OAuth"),
        (@"Microsoft\.Owin\.Cors", "webForCore.Microsoft.Owin.Cors"),
        (@"Microsoft\.AspNet\.Identity\.Owin", "webForCore.Microsoft.AspNet.Identity.Owin"),
        (@"Microsoft\.AspNet\.Identity\.EntityFramework", "webForCore.Microsoft.AspNet.Identity.EntityFramework"),
        (@"Microsoft\.AspNet\.Identity\.Core", "webForCore.Microsoft.AspNet.Identity.Core"),
        (@"Microsoft\.AspNet\.SignalR\.SystemWeb|Microsoft\.AspNet\.SignalR\b", "webForCore.Microsoft.AspNet.SignalR.SystemWeb"),
        (@"System\.Web\.DataVisualization", "webForCore.Web.DataVisualization"),
        (@"(?<![\w.])WebGrease\b", "webForCore.WebGrease"),
    ];
    const string WebProps = """
            <TargetFramework>net10.0</TargetFramework>
            <OutputType>Exe</OutputType>{names}
            <ImplicitUsings>disable</ImplicitUsings>
            <Nullable>disable</Nullable>{asminfo}
            <OutputPath>bin_dotnet</OutputPath>
            <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
            <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
            <GenerateDependencyFile>true</GenerateDependencyFile>
            <NoWarn>$(NoWarn);NU1701</NoWarn>
            <PredefinedCulturesOnly>true</PredefinedCulturesOnly>{razor}
        """;
    const string LibProps = """
            <TargetFramework>net10.0</TargetFramework>{names}
            <ImplicitUsings>disable</ImplicitUsings>
            <Nullable>disable</Nullable>{asminfo}
            <NoWarn>$(NoWarn);NU1701</NoWarn>
        """;
    const string Razor = """

            <EnableDefaultRazorGenerateItems>false</EnableDefaultRazorGenerateItems>
            <RazorCompileOnBuild>false</RazorCompileOnBuild>
            <RazorCompileOnPublish>false</RazorCompileOnPublish>
        """;

    static readonly bool OnWindows = OperatingSystem.IsWindows();

    // A version as a list of numbers ("1.2-beta" is 1, 2, 0), compared as Python compares lists.
    static int CompareVersions(string a, string b)
    {
        static long[] Key(string v) => Regex.Split(v, "[.-]").Select(x => x.Length > 0 && x.All(char.IsAsciiDigit) ? long.Parse(x) : 0).ToArray();
        var (ka, kb) = (Key(a), Key(b));
        for (var i = 0; i < Math.Min(ka.Length, kb.Length); i++)
            if (ka[i] != kb[i])
                return ka[i].CompareTo(kb[i]);
        return ka.Length.CompareTo(kb.Length);
    }

    // rel (a / path relative to base) with each segment in its case on disk, where it differs only by case.
    static string RealCase(string baseDir, string rel)
    {
        var cur = baseDir;
        var parts = new List<string>();
        foreach (var seg in rel.Split('/'))
        {
            if (seg is "" or "." or ".." || Exists(Path.Combine(cur, seg)) || !Directory.Exists(cur))
            {
                parts.Add(seg);
                cur = Path.Combine(cur, seg);
                continue;
            }
            var match = Directory.EnumerateFileSystemEntries(cur).Select(Path.GetFileName).FirstOrDefault(c => c.ToLowerInvariant() == seg.ToLowerInvariant()) ?? seg;
            parts.Add(match);
            cur = Path.Combine(cur, match);
        }
        return string.Join("/", parts);
    }

    static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    // As Python's read_text: UTF-8 (a BOM dropped only with sig), \r\n and \r read as \n.
    static string Read(string path, bool sig = false)
    {
        var text = Encoding.UTF8.GetString(File.ReadAllBytes(path));
        if (sig && text.StartsWith('﻿'))
            text = text[1..];
        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    // As write_text: \n as the platform's line end.
    static void Write(string path, string text, bool sig = false) =>
        File.WriteAllText(path, Environment.NewLine == "\n" ? text : text.Replace("\n", Environment.NewLine), new UTF8Encoding(sig));

    // Every file under dir (pathlib's rglob, files only), and its path's parts.
    static IEnumerable<string> FilesUnder(string dir)
    {
        var stack = new Stack<string>();
        stack.Push(dir);
        while (stack.Count > 0)
        {
            var d = stack.Pop();
            string[] entries;
            try { entries = Directory.EnumerateFileSystemEntries(d).ToArray(); }
            catch (IOException) { continue; }
            catch (UnauthorizedAccessException) { continue; }
            foreach (var e in entries)
            {
                var info = new DirectoryInfo(e);
                if (info.Exists)
                {
                    if (info.LinkTarget == null)
                        stack.Push(e);
                }
                else if (File.Exists(e))
                    yield return e;
            }
        }
    }

    static string[] Parts(string path) => path.Split(OnWindows ? ['\\', '/'] : ['/'], StringSplitOptions.RemoveEmptyEntries);
    static string Rel(string dir, string path) => Path.GetRelativePath(dir, path).Replace('\\', '/');

    // Python's repr() of a string, a list of strings, or a list of pairs of them, as the report printed them.
    static string Repr(string s)
    {
        var quote = s.Contains('\'') && !s.Contains('"') ? '"' : '\'';
        var sb = new StringBuilder().Append(quote);
        foreach (var c in s)
        {
            if (c == quote || c == '\\')
                sb.Append('\\').Append(c);
            else if (c == '\n')
                sb.Append("\\n");
            else if (c == '\r')
                sb.Append("\\r");
            else if (c == '\t')
                sb.Append("\\t");
            else if (c < 0x20 || c == 0x7f)
                sb.Append($"\\x{(int)c:x2}");
            else
                sb.Append(c);
        }
        return sb.Append(quote).ToString();
    }

    static string Repr(IEnumerable<string> items) => "[" + string.Join(", ", items.Select(Repr)) + "]";
    static string Repr(IEnumerable<(string, string)> items) => "[" + string.Join(", ", items.Select(p => $"({Repr(p.Item1)}, {Repr(p.Item2)})")) + "]";
    static string OrDash(string repr) => repr == "[]" ? "-" : repr;

    // An SDK-style project that targets only .NET Framework: net10.0; framework references (no HintPath) and the
    // Microsoft packages webForCore replaces removed; package versions raised; webForCore's packages added.
    static void ConvertSdkInPlace(string proj, string old, List<string> wfc, List<(string Name, string Version)> kept, List<string> dropped,
        Dictionary<string, string> pkgs, List<string> report)
    {
        var first = Regex.Match(old, "<TargetFrameworks?>[^<]+</TargetFrameworks?>");
        var text = old[..first.Index] + "\0" + old[(first.Index + first.Length)..];
        text = Regex.Replace(text, @"\s*<TargetFrameworks?>[^<]+</TargetFrameworks?>", ""); // any others
        text = text.Replace("\0", "<TargetFramework>net10.0</TargetFramework>");
        // The C# it was written for. net10.0 defaults to the newest; C# 14's first-class span conversions change
        // overload resolution in code written for older C# (ServiceStack.Text: CS0029/CS1503 on ReadOnlySpan<char>).
        // Unset (a .NET Framework project's default, C# 7.3) stays 7.3; "latest"/"preview"/"default", here or in a
        // Directory.Build.props, becomes 13, the last version before that change. (C# 11's ref-safety rules apply on
        // net7.0+ whatever the version.)
        var dir = Path.GetDirectoryName(proj);
        var propsFiles = new[] { Path.Combine(dir, "Directory.Build.props"), Path.Combine(Path.GetDirectoryName(dir), "Directory.Build.props") }.Where(File.Exists);
        var lang = Regex.Matches(old + string.Concat(propsFiles.Select(f => Read(f))), "<LangVersion>([^<]+)</LangVersion>").Select(m => m.Groups[1].Value).ToList();
        var pin = lang.Count == 0 ? "7.3" : lang[^1].Trim().ToLowerInvariant() is "latest" or "preview" or "default" or "latestmajor" ? "13.0" : null;
        if (pin != null)
        {
            text = Regex.Replace(text, "<LangVersion>[^<]+</LangVersion>", "");
            text = new Regex("(<TargetFramework>net10.0</TargetFramework>)").Replace(text, m => m.Groups[1].Value + $"\n    <LangVersion>{pin}</LangVersion>", 1);
            report.Add($"LangVersion {pin} ({(lang.Count > 0 ? lang[^1] : "unset")} before)");
        }
        text = Regex.Replace(text, @"\s*<Reference\s+Include=""[^""]+""\s*/>", "");
        text = Regex.Replace(text, @"\s*<Reference\s+Include=""[^""]+""\s*>(?:(?!<HintPath>).)*?</Reference>", "", RegexOptions.Singleline);
        foreach (var name in dropped)
            text = Regex.Replace(text, @"\s*<PackageReference\s+Include=""" + Regex.Escape(name) + @"""[^>]*?(/>|>.*?</PackageReference>)", "", RegexOptions.Singleline);
        var versions = new Dictionary<string, string>();
        foreach (var (n, v) in kept)
            versions[n] = v;
        foreach (var (name, ver) in pkgs)
            if (versions.TryGetValue(name, out var want) && want != ver)
                text = Regex.Replace(text, @"(<PackageReference\s+Include=""" + Regex.Escape(name) + @"""\s+Version="")[^""]*""", m => m.Groups[1].Value + want + "\"");
        var added = kept.Where(k => !pkgs.ContainsKey(k.Name)).ToList();
        const string props = "\n    <ImplicitUsings>disable</ImplicitUsings>\n    <Nullable>disable</Nullable>\n    <NoWarn>$(NoWarn);NU1701</NoWarn>";
        text = new Regex("(<TargetFramework>net10.0</TargetFramework>)").Replace(text, m => m.Groups[1].Value + props, 1);
        var group = new List<string> { "  <ItemGroup>", $"    <!-- Migrated to webForCore (.NET 10) in place; the .NET Framework project is {Path.GetFileName(proj)}.netfx. -->" };
        group.AddRange(wfc.Select(p => $"    <PackageReference Include=\"{p}\" Version=\"{Wfc}\" />"));
        group.AddRange(added.Select(a => $"    <PackageReference Include=\"{a.Name}\" Version=\"{a.Version}\" />"));
        group.Add("  </ItemGroup>");
        var end = text.IndexOf("</Project>", StringComparison.Ordinal);
        if (end >= 0)
            text = text[..end] + string.Join("\n", group) + "\n</Project>" + text[(end + "</Project>".Length)..];
        Write(proj, text);
        Console.WriteLine($"== {Path.GetFileName(proj)}: SDK-style, edited in place: webForCore {string.Join(", ", wfc)}");
        Console.WriteLine($"   dropped: {(dropped.Count > 0 ? string.Join(", ", dropped) : "-")}; added: {(added.Count > 0 ? string.Join(", ", added.Select(a => $"{a.Name} {a.Version}")) : "-")}");
        foreach (var r in report)
            Console.WriteLine("   note: " + r);
    }

    static void ConvertProject(string proj, bool web)
    {
        var report = new List<string>();
        var netfx = proj + ".netfx";
        if (!File.Exists(netfx))
            File.Copy(proj, netfx);
        var old = Read(netfx, sig: true);
        var d = Path.GetDirectoryName(Path.GetFullPath(proj));
        // Already SDK-style. With a .NET target (Opserver.Core: net461;netstandard2.0) it's left as it is: .NET uses
        // that target. With only .NET Framework targets (YAF.NET's libraries: net472) it's edited in place, below.
        var sdk = Regex.IsMatch(old, @"\A\s*<Project\s+Sdk=");
        if (sdk)
        {
            var tfms = string.Join(";", Regex.Matches(old, "<TargetFrameworks?>([^<]+)</TargetFrameworks?>").Select(m => m.Groups[1].Value));
            if (Regex.IsMatch(tfms, @"netstandard|netcoreapp|net[5-9]\.|net1\d\."))
            {
                if (Read(proj, sig: true) != old)
                    File.Copy(netfx, proj, overwrite: true);
                Console.WriteLine($"== {Path.GetFileName(proj)}: already SDK-style with a .NET target ({tfms}), left as it is");
                return;
            }
        }
        var asm = Regex.Match(old, "<AssemblyName>([^<]+)<");
        var ns = Regex.Match(old, "<RootNamespace>([^<]+)<");
        var names = string.Concat(new[] { ("RootNamespace", ns), ("AssemblyName", asm) }.Where(x => x.Item2.Success).Select(x => $"\n    <{x.Item1}>{x.Item2.Groups[1].Value}</{x.Item1}>"));
        var compiles = Regex.Matches(old, @"<Compile Include=""([^""]+)""").Select(m => m.Groups[1].Value.Replace('\\', '/')).ToList();
        var asminfo = compiles.Any(c => c.EndsWith("AssemblyInfo.cs")) ? "\n    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>" : "";
        // AssemblyVersion("2.0.*") is CS8357 in an SDK-style project, which is deterministic by default.
        if (FilesUnder(d).Where(f => Path.GetFileName(f) == "AssemblyInfo.cs").Any(f => Regex.IsMatch(Read(f), @"AssemblyVersion\(""[^""]*\*")))
            asminfo += "\n    <Deterministic>false</Deterministic>";
        // packages
        var pkgs = new Dictionary<string, string>();
        var pc = Path.Combine(d, "packages.config");
        // packages.config is kept as packages.config.netfx (like the project), so converting again reads the same packages.
        var pcNetfx = Path.Combine(d, "packages.config.netfx");
        if (File.Exists(pc) && !File.Exists(pcNetfx))
            File.Copy(pc, pcNetfx);
        if (File.Exists(pcNetfx))
            pc = pcNetfx;
        if (File.Exists(pc))
            foreach (Match m in Regex.Matches(Read(pc), @"<package\s+id=""([^""]+)""\s+version=""([^""]+)"""))
                pkgs[m.Groups[1].Value] = m.Groups[2].Value;
        foreach (Match m in Regex.Matches(old, @"<PackageReference\s+Include=""([^""]+)""(?:\s+Version=""([^""]+)"")?\s*/?>(?:\s*<Version>([^<]+)</Version>)?"))
            pkgs[m.Groups[1].Value] = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Success ? m.Groups[3].Value : "*";
        var refs = Regex.Matches(old, @"<Reference\s+Include=""([^""]+)""").Select(m => m.Groups[1].Value).ToList();
        var everything = string.Join("\n", refs.Concat(pkgs.Keys));
        var wfc = new List<string> { "webForCore" };
        wfc.AddRange(Frameworks.Where(f => Regex.IsMatch(everything, f.Pattern)).Select(f => f.Package));
        if (wfc.Contains("webForCore.Web.Mvc")) // MVC brings Razor, Web Pages and (since 0.5.0) System.Web.Helpers
            wfc = wfc.Where(p => p is not ("webForCore.Web.WebPages.Razor" or "webForCore.Web.Helpers")).ToList();
        var kept = new List<(string Name, string Version)>();
        var dropped = new List<string>();
        foreach (var (name, v) in pkgs.OrderBy(kv => kv.Key.ToLowerInvariant(), StringComparer.Ordinal))
        {
            var ver = v;
            var low = name.ToLowerInvariant();
            if (OwinSignalrKeep.TryGetValue(low, out var lifted))
            {
                if (CompareVersions(ver, lifted) != 0)
                    report.Add($"{name} {ver} -> {lifted} (not in webForCore; the version it lifted)");
                kept.Add((name, lifted));
                continue;
            }
            if (Drop.IsMatch(name) && !KeepMs.IsMatch(name) && !KatanaKeep.IsMatch(name))
            {
                dropped.Add(name);
                continue;
            }
            // The IdentityModel packages move together (Tokens 8.19.1 through webForCore's CoreWCF client support).
            if ((low.StartsWith("microsoft.identitymodel.") || low == "system.identitymodel.tokens.jwt") && CompareVersions(ver, "8.19.1") < 0)
            {
                report.Add($"{name} {ver} -> 8.19.1");
                ver = "8.19.1";
            }
            if (MinVersions.TryGetValue(low, out var min) && CompareVersions(ver, min) < 0)
            {
                report.Add($"{name} {ver} -> {min}");
                ver = min;
            }
            kept.Add((name, ver));
        }
        // Syndication was in System.ServiceModel.Web (.NET 4.0) and System.ServiceModel (4.5): add its package when used.
        if (FilesUnder(d).Where(f => f.EndsWith(".cs") && !Parts(f).Contains("obj")).Any(f => Read(f).Contains("System.ServiceModel.Syndication")))
            refs = [.. refs, "System.ServiceModel.Syndication"];
        foreach (var r in refs.Distinct())
        {
            var name = r.Split(',')[0].Trim();
            if (FrameworkPackages.Contains(name) && !pkgs.ContainsKey(name))
            {
                kept.Add((name, NetPackagesVersion));
                report.Add($"{name}: a .NET Framework assembly, the package on .NET");
            }
        }
        var thirdparty = ThirdpartyPackages.Where(t => Regex.IsMatch(everything, t.Pattern)).Select(t => t.Package).ToList();
        kept = kept.Where(k => !Regex.IsMatch(k.Name, "(?i)^AjaxControlToolkit$")).ToList(); // replaced, not overridden: net40 only
        var hints = new List<(string, string)>();
        foreach (Match m in Regex.Matches(old, @"<Reference\s+Include=""([^"",]+)[^""]*""[^>]*>\s*(?:<[^H][^>]*>[^<]*</[^>]+>\s*)*<HintPath>([^<]+)</HintPath>"))
        {
            var (name, hint) = (m.Groups[1].Value, m.Groups[2].Value.Replace('\\', '/'));
            if (("/" + hint).Contains("/packages/"))
                continue;
            // An assembly webForCore replaces (Subtext's System.Web.Mvc from a developer's D: drive), or a file that isn't there.
            if (Frameworks.Any(f => Regex.IsMatch(name, @"\A(?:" + f.Pattern + "$)")) || Drop.IsMatch(name) || !Exists(Path.Combine(d, RealCase(d, hint))))
            {
                report.Add($"hint path dropped: {name} ({hint})");
                continue;
            }
            hints.Add((name, RealCase(d, hint)));
        }
        // compile list: anything the SDK glob adds that the old project didn't compile
        var globbed = FilesUnder(d).Where(f => f.EndsWith(".cs")).Select(f => Rel(d, f))
            .Where(r => !r.Split('/').Any(x => x is "bin" or "obj" or "bin_dotnet") && !r.ToLowerInvariant().StartsWith("app_code/")).ToHashSet();
        var listed = compiles.Select(c => c.ToLowerInvariant()).ToHashSet();
        var onDisk = globbed.Select(g => g.ToLowerInvariant()).ToHashSet();
        var extra = globbed.Where(g => !listed.Contains(g.ToLowerInvariant()) && g != "Program.cs").OrderBy(g => g, StringComparer.Ordinal).ToList();
        var missing = compiles.Distinct().Where(c => !c.StartsWith("..") && !onDisk.Contains(c.ToLowerInvariant())).OrderBy(c => c, StringComparer.Ordinal).ToList();
        var linked = compiles.Where(c => c.StartsWith("..")).ToList();
        // A reference conditioned on the build configuration (mojoPortal picks its data layer by configuration) is kept
        // when the condition is Debug or Release, the configurations the build uses.
        var projrefs = new List<string>();
        foreach (Match m in Regex.Matches(old, "<ProjectReference\\s+([^>]*)>"))
        {
            var attrs = m.Groups[1].Value;
            var inc = Regex.Match(attrs, @"Include=""([^""]+)""");
            var cond = Regex.Match(attrs, @"Condition=""([^""]+)""");
            if (!inc.Success || (cond.Success && !Regex.IsMatch(cond.Groups[1].Value, @"\A\s*'\$\(Configuration\)'\s*==\s*'(Debug|Release)'\s*\z")))
                continue;
            var r = RealCase(d, inc.Groups[1].Value.Replace('\\', '/'));
            if (!projrefs.Contains(r))
                projrefs.Add(r);
        }
        if (sdk && !web)
        {
            ConvertSdkInPlace(proj, old, wfc, kept, dropped, pkgs, report);
            return;
        }
        var props = (web ? WebProps : LibProps).Replace("{names}", names).Replace("{asminfo}", asminfo)
            .Replace("{razor}", web && FilesUnder(d).Any(f => f.EndsWith(".cshtml")) ? Razor : "");
        var output = new List<string> { $"<Project Sdk=\"Microsoft.NET.Sdk{(web ? ".Web" : "")}\">",
            $"  <!-- Migrated to webForCore (.NET 10). The .NET Framework project is {Path.GetFileName(netfx)}. -->",
            "  <PropertyGroup>", props, "  </PropertyGroup>", "" };
        var items = web ? new List<string> { "    <Compile Remove=\"App_Code/**;bin_dotnet/**;obj/**\" />" } : ["    <Compile Remove=\"bin/**;obj/**\" />"];
        if (web)
            items.AddRange(["    <Content Remove=\"bin_dotnet/**\" />", "    <None Remove=\"bin_dotnet/**\" />"]);
        items.AddRange(extra.Select(e => $"    <Compile Remove=\"{e}\" />"));
        items.AddRange(linked.Select(c => $"    <Compile Include=\"{c}\" Link=\"{c[(c.LastIndexOf('/') + 1)..]}\" />"));
        output.AddRange(["  <ItemGroup>", .. items, "  </ItemGroup>", ""]);
        // Embedded resources other than .resx (the SDK globs only those): Subtext's installation scripts. Same manifest
        // names as before (RootNamespace + path), or the LogicalName the project gave.
        var embedded = new List<string>();
        foreach (Match m in Regex.Matches(old, @"<EmbeddedResource\s+Include=""([^""]+)""\s*(/>|>(.*?)</EmbeddedResource>)", RegexOptions.Singleline))
        {
            var inc = m.Groups[1].Value.Replace('\\', '/');
            if (inc.ToLowerInvariant().EndsWith(".resx"))
                continue;
            var logicalMatch = Regex.Match(m.Groups[3].Success ? m.Groups[3].Value : "", "<LogicalName>([^<]+)</LogicalName>");
            var logical = logicalMatch.Success ? logicalMatch.Groups[1].Value : null;
            var onDiskPath = RealCase(d, inc);
            if (logical == null && onDiskPath != inc && !inc.StartsWith(".."))
            {
                // the manifest name comes from the path as the project wrote it (Windows ignores the case on disk)
                var root = Regex.Match(old, "<RootNamespace>([^<]+)</RootNamespace>");
                var slash = inc.LastIndexOf('/');
                var (folders, name) = slash >= 0 ? (inc[..slash], inc[(slash + 1)..]) : ("", inc);
                logical = string.Join(".", (root.Success ? [root.Groups[1].Value] : Array.Empty<string>())
                    .Concat(folders.Split('/').Where(f => f.Length > 0).Select(f => Regex.Replace(f, @"[^\w.]", "_"))).Append(name));
            }
            embedded.Add($"    <EmbeddedResource Include=\"{onDiskPath}\"" + (logical != null ? $" LogicalName=\"{logical}\"" : "") + " />");
        }
        if (embedded.Count > 0)
        {
            output.AddRange(["  <ItemGroup>", "    <!-- Embedded resources (the SDK only globs .resx). -->", .. embedded, "  </ItemGroup>", ""]);
            report.Add($"{embedded.Count} embedded resources kept");
        }
        output.AddRange(["  <ItemGroup>", $"    <!-- webForCore {Wfc} packages (GitHub release): \"webForCore\" is the \"Always\" set. -->",
            .. wfc.Select(p => $"    <PackageReference Include=\"{p}\" Version=\"{Wfc}\" />"), "  </ItemGroup>", ""]);
        if (thirdparty.Count > 0)
            output.AddRange(["  <ItemGroup>", $"    <!-- webForCore-thirdparty {Thirdparty} (GitHub release): builds of third-party libraries for webForCore. -->",
                .. thirdparty.Select(p => $"    <PackageReference Include=\"{p}\" Version=\"{Thirdparty}\" />"), "  </ItemGroup>", ""]);
        if (kept.Count > 0 || hints.Count > 0)
            output.AddRange(["  <ItemGroup>", "    <!-- The app's own libraries. -->", .. kept.Select(k => $"    <PackageReference Include=\"{k.Name}\" Version=\"{k.Version}\" />"),
                .. hints.Select(h => $"    <Reference Include=\"{h.Item1}\" HintPath=\"{h.Item2}\" />"), "  </ItemGroup>", ""]);
        if (projrefs.Count > 0)
            output.AddRange(["  <ItemGroup>", .. projrefs.Select(r => $"    <ProjectReference Include=\"{r}\" />"), "  </ItemGroup>", ""]);
        output.Add("</Project>");
        Write(proj, string.Join("\n", output) + "\n");
        Console.WriteLine($"== {Path.GetFileName(proj)}: webForCore {string.Join(", ", wfc)}" + (thirdparty.Count > 0 ? $"; webForCore-thirdparty {string.Join(", ", thirdparty)}" : ""));
        Console.WriteLine($"   kept: {(kept.Count > 0 ? string.Join(", ", kept.Select(k => $"{k.Name} {k.Version}")) : "-")}");
        Console.WriteLine($"   dropped: {(dropped.Count > 0 ? string.Join(", ", dropped) : "-")}");
        Console.WriteLine($"   hint paths: {OrDash(Repr(hints))}; project refs: {OrDash(Repr(projrefs))}");
        Console.WriteLine($"   compile: {compiles.Count} listed, {extra.Count} extra on disk removed {Repr(extra.Take(5))}, {missing.Count} listed but missing {Repr(missing.Take(5))}, linked {Repr(linked.Take(3))}");
        foreach (var r in report)
            Console.WriteLine("   note: " + r);
        if (File.Exists(Path.Combine(d, "packages.config")))
        {
            File.Delete(Path.Combine(d, "packages.config"));
            Console.WriteLine("   packages.config removed (kept as packages.config.netfx)");
        }
        if (!web)
            return;
        string[] sessionFiles = [".cs", ".cshtml", ".aspx", ".ascx", ".asax", ".master"];
        var usesSession = FilesUnder(d).Where(f => sessionFiles.Contains(Path.GetExtension(f).ToLowerInvariant()) && !Parts(f).Contains("bin_dotnet"))
            .Any(f => Regex.IsMatch(Read(f), @"\bSession\[|\bSession\.|Session_Start"));
        if (!File.Exists(Path.Combine(d, "Program.cs")))
        {
            var nsName = ns.Success ? ns.Groups[1].Value : Path.GetFileNameWithoutExtension(proj);
            var sessionServices = usesSession ? "\n            builder.Services.AddDistributedMemoryCache();\n            builder.Services.AddSession();" : "";
            Write(Path.Combine(d, "Program.cs"), $$"""
                using Microsoft.AspNetCore.Builder;
                using Microsoft.Extensions.DependencyInjection;
                using WebFormsForCore.Hosting.IIS;

                namespace {{nsName}}
                {
                    // webForCore host (docs/MIGRATING.md section 4).{{(usesSession ? " The app uses session state." : "")}}
                    public class Program
                    {
                        public static void Main(string[] args)
                        {
                            var builder = WebApplication.CreateBuilder(args);
                            builder.Services.AddWebForms();{{sessionServices}}

                            var app = builder.Build();{{(usesSession ? "\n            app.UseSession();" : "")}}
                            app.UseWebForms(o => o
                                .HandleAllRequestsWithWebForms(){{(usesSession ? "\n                .UseAspNetCoreSessionProvider()" : "")}});
                            app.Run();
                        }
                    }
                }

                """.Replace("\r\n", "\n"));
            Console.WriteLine($"   Program.cs added (session: {(usesSession ? "True" : "False")})");
        }
        var wc = Directory.EnumerateFileSystemEntries(d).FirstOrDefault(p => Path.GetFileName(p).ToLowerInvariant() == "web.config");
        if (wc != null)
        {
            var text = Read(wc, sig: true);
            var changed = Regex.Replace(text, @"\n?[ \t]*<system\.codedom>.*?</system\.codedom>", "", RegexOptions.Singleline);
            if (changed != text)
            {
                var bytes = File.ReadAllBytes(wc);
                Write(wc, changed, sig: bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
                Console.WriteLine($"   {Path.GetFileName(wc)}: <system.codedom> removed");
            }
        }
    }

    public static int Entry(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" || !File.Exists(args[0]))
        {
            Console.Error.WriteLine(args.Length == 0 || args[0] is "-h" or "--help" ? Doc : $"no such project: {args[0]}");
            return 1;
        }
        var libs = args.Where((a, i) => i > 0 && args[i - 1] == "--lib").ToList();
        try
        {
            ConvertProject(args[0], true);
            foreach (var lib in libs)
                ConvertProject(lib, false);
        }
        catch (Exception ex) // what failed (a library's project file that isn't there), with exit status 1
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        return 0;
    }
}

// The webForCore smoke test bed: fetch open-source ASP.NET 4.x apps, assess them, build their migrated projects
// against webForCore, run them on Kestrel and probe them. A .NET file-based program (the .NET 10 SDK; Tomlyn reads
// app.toml):
//
//     dotnet run scripts/wfc.cs -- packages [--version 0.9.0]  # download webForCore's (and webForCore-thirdparty's) release packages into .packages/
//     dotnet run scripts/wfc.cs -- list
//     dotnet run scripts/wfc.cs -- fetch <id>... | --all [--reset]
//     dotnet run scripts/wfc.cs -- assess <id>... | --all
//     dotnet run scripts/wfc.cs -- build <id>...
//     dotnet run scripts/wfc.cs -- setup <id>...             # the app's database, as app.toml's [setup] says (once per new database)
//     dotnet run scripts/wfc.cs -- smoke <id>...
//     dotnet run scripts/wfc.cs -- run <id>... | --all        # fetch, build, setup and smoke
//     dotnet run scripts/wfc.cs -- save-overlay <id>         # the app checkout's changes -> apps/<id>/overlay.patch
//     dotnet run scripts/wfc.cs -- report                    # apps/*/results.json (and results.windows.json) -> RESULTS.md
//     dotnet run scripts/wfc.cs -- ci-matrix [<id>...] [--changed-since <ref>]  # the GitHub Actions job list (.github/workflows/smoke.yml)
//     dotnet run scripts/wfc.cs -- ci-result <id>            # a CI job's result against the committed one; stages it for upload
//
// Every round runs on Linux and Windows: in CI (README.md, "CI"), or by running this on a Windows machine. On Windows
// the results go to results.windows.json and logs.windows/, and LOG.md lines say (windows).
//
// Slow, stable and simple (CLAUDE.md): one thing at a time, every wait has a timeout, and every external call fails
// rather than hangs.
#:package Tomlyn@2.10.1
#:property PublishAot=false
#:property NuGetAudit=false
#:property Nullable=disable
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;

return Wfc.Entry(args);

static class Wfc
{
    // Each app has its own folder, apps/<id>/:
    //   app.toml        where it comes from, how to build it, what to probe
    //   NOTES.md        the migration notes: assessment, every change and why, open problems
    //   LOG.md          one line per stage run, appended by this program
    //   overlay.patch   the migration, as a patch on the pinned commit
    //   results.json    the latest result of each stage (Linux; results.windows.json on Windows)
    //   logs/           the latest build and server logs (logs.windows/ on Windows)
    //   src/            the checkout (not committed)
    static readonly bool OnWindows = OperatingSystem.IsWindows();
    // The OS this run is on. Linux keeps the original file names; Windows' results sit beside them.
    static readonly string Platform = OnWindows ? "windows" : "linux";
    static readonly string Suffix = OnWindows ? ".windows" : "";
    static readonly string Root = Files.Resolve(Path.Combine(
        AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Path.Combine(Environment.CurrentDirectory, "scripts"), ".."));
    static readonly string Apps = Path.Combine(Root, "apps");

    static readonly string Webforcore = Files.Resolve(Environment.GetEnvironmentVariable("WEBFORCORE_DIR") is { } dir
        ? (dir == "" ? "." : dir) : Path.Combine(Path.GetDirectoryName(Root), "webForCore"));
    // webForCore's NuGet packages come from its GitHub releases, not nuget.org. `packages` downloads a release here, and
    // builds restore from it (apps reference the packages, docs/MIGRATING.md section 1). WEBFORCORE_DIR is only needed by
    // apps still migrated against webForCore's source.
    const string WebforcoreRepo = "xforcore/webForCore";
    static string WebforcoreVersion = EnvOr("WEBFORCORE_VERSION", "0.9.0");
    // Builds of third-party libraries for webForCore (WebActivatorEx, AjaxControlToolkit; CLAUDE.md), from
    // webForCore-thirdparty's releases, go in the same folder.
    const string ThirdpartyRepo = "xforcore/webForCore-thirdparty";
    static readonly string ThirdpartyVersion = EnvOr("THIRDPARTY_VERSION", "0.7.0");
    static readonly string[] ThirdpartyPackages = ["webForCore.WebActivatorEx", "webForCore.AjaxControlToolkit"];
    // System.Drawing, which the webForCore package depends on from 0.7.0 (drawingForCore and drawingForCore.Common),
    // goes there too.
    const string DrawingRepo = "xforcore/drawingForCore";
    static readonly string DrawingVersion = EnvOr("DRAWING_VERSION", "0.3.0");
    static readonly string Packages = Path.Combine(Root, ".packages");
    // WFC_CHECKOUTS: a folder for the apps' checkouts (<folder>/<id>) instead of apps/<id>/src, e.g. on another disk:
    // the big apps are hundreds of thousands of files, and their builds gigabytes.
    static readonly string Checkouts = Environment.GetEnvironmentVariable("WFC_CHECKOUTS") is { Length: > 0 } c ? Files.Resolve(c) : null;

    // Phase 0 of webForCore's docs/AGENT-MIGRATION.md.
    static readonly (string Name, Regex Pattern)[] Blockers =
    [
        ("windows-auth", new(@"mode=""Windows""|WindowsIdentity|WindowsPrincipal|<identity\s+impersonate")),
        ("wcf-hosting", new(@"ServiceHost\b|<system\.serviceModel>(?:(?!</system\.serviceModel>)[\s\S])*?<services>")),
        ("linq-to-sql", new(@"System\.Data\.Linq|LinqDataSource")),
        // EF 1-4's ObjectContext (System.Data.Objects); EF 6's (System.Data.Entity.Core.Objects) runs on .NET.
        ("ef-objectcontext", new(@"System\.Data\.Objects\b|EntityDataSource")),
        ("web-references", new(@"App_WebReferences")),
        ("windows-only", new(@"Microsoft\.Win32\.Registry|\[ComImport\]|Marshal\.GetActiveObject|System\.Messaging|Microsoft\.Office\.Interop")),
        ("metafiles", new(@"\bMetafile\b")),
        ("netdatacontract", new(@"NetDataContractSerializer")),
        ("appdomain-thread-abort", new(@"AppDomain\.CreateDomain|Thread\.Abort|Thread\.ResetAbort")),
        ("com-plus", new(@"System\.EnterpriseServices|Transaction=""")),
        ("dpapi-config", new(@"DataProtectionConfigurationProvider")),
    ];
    // Not blockers, but code to change (docs/NAMESPACES.md): listed by assess without blocking.
    static readonly (string Name, Regex Pattern)[] Notices =
    [
        ("binaryformatter", new(@"(?<!Formatters\.Binary\.)\bnew BinaryFormatter\b|System\.Runtime\.Serialization\.Formatters\.Binary")),
        ("smtpclient-config", new(@"new SmtpClient\(\s*\)")),
    ];
    static readonly HashSet<string> Scanned = [".cs", ".vb", ".aspx", ".ascx", ".master", ".asax", ".ashx", ".asmx", ".cshtml", ".vbhtml", ".config", ".svc"];

    // Framework detection from packages.config / csproj references.
    static readonly (string Kind, Regex Pattern)[] Kinds =
    [
        ("webforms", new(@"\.aspx$")),
        ("mvc", new(@"Microsoft\.AspNet\.Mvc|System\.Web\.Mvc")),
        ("webapi", new(@"Microsoft\.AspNet\.WebApi|System\.Web\.Http")),
        ("webpages", new(@"Microsoft\.AspNet\.WebPages|System\.Web\.WebPages")),
        ("owin", new(@"Microsoft\.Owin")),
        ("signalr", new(@"Microsoft\.AspNet\.SignalR")),
        ("identity", new(@"Microsoft\.AspNet\.Identity")),
        ("ef6", new(@"id=""EntityFramework""|Include=""EntityFramework")),
    ];

    // Must never be served (docs/MIGRATING.md section 7).
    static readonly string[] Forbidden = ["/web.config", "/App_Data/", "/Global.asax.cs", "/obj/", "/packages.config"];

    static string EnvOr(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : fallback;

    static List<TomlTable> Catalog()
    {
        var apps = new List<TomlTable>();
        var dirs = Directory.Exists(Apps) ? Directory.GetDirectories(Apps).Where(d => File.Exists(Path.Combine(d, "app.toml"))) : [];
        foreach (var d in dirs.OrderBy(Path.GetFileName, Files.NameComparer))
        {
            var app = TomlSerializer.Deserialize<TomlTable>(Files.ReadText(Path.Combine(d, "app.toml")));
            app["id"] = Path.GetFileName(d);
            apps.Add(app);
        }
        return apps;
    }

    static string AppDir(string id) => Path.Combine(Apps, id);

    internal static string LogsDir(string id)
    {
        var d = Path.Combine(AppDir(id), $"logs{Suffix}");
        Directory.CreateDirectory(d);
        return d;
    }

    // The environment for dotnet: the runner's own, with Fedora's SHA-1 switch and webForCore's source folder.
    internal static Dictionary<string, string> Env()
    {
        var e = Proc.CurrentEnvironment();
        e.TryAdd("OPENSSL_ENABLE_SHA1_SIGNATURES", "1"); // Fedora/RHEL
        e.TryAdd("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
        e.TryAdd("DOTNET_NOLOGO", "1");
        e["WEBFORCORE_DIR"] = Webforcore; // projects migrated against the source import $(WEBFORCORE_DIR)/src/...
        // `dotnet run` sets this for the runner itself; the apps' builds get MSBuild's default.
        e.Remove("MSBUILDFAILONDRIVEENUMERATINGWILDCARD");
        return e;
    }

    static string PackageSource() => Path.Combine(Packages, $"v{WebforcoreVersion}");

    static bool DownloadPackages()
    {
        var dest = PackageSource();
        Directory.CreateDirectory(dest);
        var ok = true;
        foreach (var (repo, version) in new[] { (WebforcoreRepo, WebforcoreVersion), (ThirdpartyRepo, ThirdpartyVersion), (DrawingRepo, DrawingVersion) })
        {
            Proc.Result r = null;
            for (var attempt = 0; attempt < 3; attempt++) // release asset downloads fail with 503 now and then
            {
                r = Proc.Run("gh", ["release", "download", $"v{version}", "--repo", repo, "--pattern", "*.nupkg", "--dir", dest, "--skip-existing"],
                    timeout: TimeSpan.FromMinutes(10));
                if (r.Code == 0)
                    break;
                Thread.Sleep(5000);
            }
            if (r.Code != 0)
            {
                Console.WriteLine($"{repo} v{version}: {Py.Slice(r.Err.Trim(), 0, 200)}");
                ok = false;
            }
        }
        var count = Directory.GetFiles(dest, "*.nupkg").Length;
        Console.WriteLine($"{Path.GetRelativePath(Root, dest)}: {count} packages (webForCore {WebforcoreVersion}, webForCore-thirdparty {ThirdpartyVersion}, drawingForCore {DrawingVersion})");
        return ok;
    }

    // Which webForCore the project builds against, for the record.
    static string WebforcoreUsed(string proj)
    {
        if (Files.ReadText(proj).Contains("$(WEBFORCORE_DIR)"))
        {
            var commit = Git(["rev-parse", "--short=12", "HEAD"], cwd: Webforcore, check: false).Out.Trim();
            return $"source {commit}";
        }
        var used = $"packages {WebforcoreVersion}";
        // The web project or one of the solution's libraries (beside it, or one folder up).
        var projects = new[] { proj }.Concat(Files.RGlob(Path.GetDirectoryName(Path.GetDirectoryName(proj)), "*.csproj"));
        if (projects.Select(f => Files.ReadText(f)).Any(text => ThirdpartyPackages.Any(p => text.Contains($"Include=\"{p}"))))
            used += $", webForCore-thirdparty {ThirdpartyVersion}";
        return used;
    }

    static string Now() => DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + "+00:00";

    // Logs are committed, so this machine's paths become placeholders.
    internal static string Scrub(string log)
    {
        var text = ScrubText(Files.ReadText(log));
        Files.WriteText(log, text);
        return text;
    }

    internal static string ScrubText(string text)
    {
        // Longest first: the repo's path can start with webForCore's (../webForCore-smoke-test).
        var pairs = new List<(string Path, string Name)> { (Webforcore, "$WEBFORCORE_DIR"), (Root, "<repo>"), (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "~") };
        if (Checkouts != null)
            pairs.Add((Checkouts, "$WFC_CHECKOUTS"));
        pairs.AddRange(pairs.Where(p => p.Path.Contains('\\')).Select(p => (p.Path.Replace('\\', '/'), p.Name)).ToList()); // Windows paths, also written with /
        foreach (var (path, name) in pairs.OrderBy(p => -p.Path.Length))
            text = text.Replace(path, name);
        return text;
    }

    static string ResultsPath(string id, string platform) =>
        Path.Combine(AppDir(id), $"results{(platform == "linux" ? "" : "." + platform)}.json");

    internal static JsonObject LoadResult(string id, string platform = null)
    {
        var p = ResultsPath(id, platform ?? Platform);
        return File.Exists(p) ? JsonNode.Parse(Files.ReadText(p)).AsObject() : new JsonObject { ["id"] = id, ["stages"] = new JsonObject() };
    }

    static JsonObject Stage(JsonObject result, string stage) => result["stages"]?[stage] as JsonObject ?? new JsonObject();
    static string Str(JsonNode n) => n is JsonValue v && v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : null;

    // status: pass | fail | blocked | skip. Details are the stage's fields in results.json; null values are kept.
    static string Record(string id, string stage, string status, JsonObject details = null)
    {
        details ??= [];
        var r = LoadResult(id);
        var entry = new JsonObject { ["status"] = status, ["at"] = Now() };
        foreach (var (k, v) in details.ToList())
        {
            details.Remove(k);
            entry[k] = v;
        }
        r["stages"].AsObject()[stage] = entry;
        Files.WriteText(Path.Combine(AppDir(id), $"results{Suffix}.json"), PyJson.Dumps(r, indent: 2, sortKeys: true) + "\n");
        var where = OnWindows ? $" ({Platform})" : "";
        var summary = Str(entry["summary"]);
        var line = $"- {Now()} **{stage}**{where}: {status}" + (string.IsNullOrEmpty(summary) ? "" : $" ({summary})");
        if (Str(entry["commit"]) is { Length: > 0 } commit)
            line += $" at `{Py.Slice(commit, 0, 12)}`";
        var log = Path.Combine(AppDir(id), "LOG.md");
        if (!File.Exists(log))
            Files.WriteText(log, $"# {id}: run log\n\nAppended by `scripts/wfc.cs`, one line per stage run.\n\n");
        File.AppendAllText(log, line + "\n");
        Console.WriteLine($"[{id}] {stage}{where}: {status}" + (string.IsNullOrEmpty(summary) ? "" : $" ({summary})"));
        return status;
    }

    // Files as committed (overlays are byte-for-byte patches) and Windows paths over 260 characters (mojoPortal). A
    // transfer slower than 1 KB/s for 2 minutes fails rather than hanging the run, and so does a repository that's
    // gone: GitHub answers 401, and git asked for a username, in a desktop dialog when SSH_ASKPASS is set (KDE's
    // ksshaskpass), so BetterCMS's clone waited for hours. No prompt of either kind, so it fails. And no git command
    // runs longer than an hour.
    static Proc.Result Git(string[] args, string cwd = null, bool check = true)
    {
        var env = Proc.CurrentEnvironment();
        env.Remove("SSH_ASKPASS");
        env.Remove("GIT_ASKPASS");
        env["GIT_TERMINAL_PROMPT"] = "0";
        env["GCM_INTERACTIVE"] = "never";
        string[] config = ["-c", "core.autocrlf=false", "-c", "core.longpaths=true", "-c", "core.askPass=",
            "-c", "http.lowSpeedLimit=1000", "-c", "http.lowSpeedTime=120"];
        var r = Proc.Run("git", [.. config, .. args], cwd: cwd, env: env, timeout: TimeSpan.FromHours(1));
        if (check && r.Code != 0)
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed with {r.Code}: {r.Err}");
        return r;
    }

    internal static string Checkout(TomlTable app) =>
        Checkouts != null ? Path.Combine(Checkouts, Id(app)) : Path.Combine(AppDir(Id(app)), "src");

    static string Id(TomlTable app) => (string)app["id"];

    // A path for the record: relative to the repo, or to WFC_CHECKOUTS.
    internal static string Shown(string path)
    {
        if (Checkouts != null && Files.IsRelativeTo(path, Checkouts))
            return "$WFC_CHECKOUTS/" + Path.GetRelativePath(Checkouts, path).Replace('\\', '/');
        return Path.GetRelativePath(Root, path);
    }

    static string SiteDir(TomlTable app) =>
        Files.Join(Checkout(app), Toml.Str(app, "site") ?? Toml.Str(app, "project_dir") ?? "");

    // --- fetch -----------------------------------------------------------------------------------------------------

    // Clones the app at its pinned ref and applies its overlay. An existing checkout is left alone (it may hold
    // migration work not yet saved with save-overlay) unless reset is given.
    static string Fetch(TomlTable app, bool reset)
    {
        var id = Id(app);
        var dest = Checkout(app);
        if (Files.Exists(dest) && !reset)
        {
            var head = Git(["rev-parse", "HEAD"], cwd: dest).Out.Trim();
            return Record(id, "fetch", "pass", new() { ["commit"] = head, ["summary"] = "existing checkout" });
        }
        Proc.Result r;
        if (!Files.Exists(dest))
        {
            r = Git(["clone", "--filter=blob:none", "--no-checkout", Toml.Str(app, "repo"), dest], check: false);
            if (r.Code != 0)
                return Record(id, "fetch", "fail", new() { ["summary"] = "clone failed", ["error"] = Py.Tail(r.Err, 2000) });
        }
        var gitRef = Toml.Truthy(Toml.Get(app, "ref")) ? Toml.Str(app, "ref") : "HEAD";
        r = Git(["checkout", "--force", "--detach", gitRef], cwd: dest, check: false);
        if (r.Code != 0)
        {
            Git(["fetch", "origin", gitRef], cwd: dest, check: false);
            r = Git(["checkout", "--force", "--detach", gitRef], cwd: dest, check: false);
            if (r.Code != 0)
                return Record(id, "fetch", "fail", new() { ["summary"] = $"no ref {gitRef}", ["error"] = Py.Tail(r.Err, 2000) });
        }
        Git(["clean", "-fdx", "-e", "bin_dotnet", "-e", "obj"], cwd: dest, check: false);
        // Files the migration deletes but whose content shouldn't be in the patch (the app's checked-in binaries).
        foreach (var pattern in Toml.List(app, "remove"))
            foreach (var f in Files.Glob(dest, pattern).Where(File.Exists))
                File.Delete(f);
        var commit = Git(["rev-parse", "HEAD"], cwd: dest).Out.Trim();
        var patch = Path.Combine(AppDir(id), "overlay.patch");
        if (File.Exists(patch))
        {
            r = Git(["apply", "--whitespace=nowarn", patch], cwd: dest, check: false);
            if (r.Code != 0)
                return Record(id, "fetch", "fail", new() { ["commit"] = commit, ["summary"] = "overlay does not apply", ["error"] = Py.Tail(r.Err, 2000) });
        }
        return Record(id, "fetch", "pass", new() { ["commit"] = commit, ["overlay"] = File.Exists(patch) });
    }

    static void SaveOverlay(TomlTable app)
    {
        var dest = Checkout(app);
        // Build output and files webForCore generates at run time aren't part of the migration. The apps' own
        // .gitignore files predate bin_dotnet.
        var generated = new List<string> { ":(exclude,glob)**/bin_dotnet/**", ":(exclude,glob)**/obj/**", ":(exclude,glob)**/App_Data/machine.config",
            ":(exclude,glob)**/logs/W3SVC*/**", ":(exclude,glob)**/logs/*/W3SVC*/**" };
        generated.AddRange(Toml.List(app, "remove").Concat(Toml.List(app, "generated")).Select(p => $":(exclude,glob){p}"));
        Git(["reset", "-q"], cwd: dest, check: false); // drops intent-to-add entries from an earlier save
        // Deletions must stay unstaged, or they drop out of the diff.
        Git(["add", "-N", "--ignore-removal", "--", ".", .. generated], cwd: dest);
        // As bytes: the apps' files with CRLF line endings must keep them, or the patch no longer applies to the checkout.
        var r = Proc.Run("git", ["-c", "core.autocrlf=false", "diff", "--binary", "--", ".", .. generated], cwd: dest, timeout: TimeSpan.FromHours(1));
        if (r.Code != 0)
            throw new InvalidOperationException($"git diff failed with {r.Code}: {r.Err}");
        var p = Path.Combine(AppDir(Id(app)), "overlay.patch");
        if (r.OutBytes.Length > 0)
        {
            File.WriteAllBytes(p, r.OutBytes);
            Console.WriteLine($"wrote {Path.GetRelativePath(Root, p)} ({r.OutBytes.Count(b => b == (byte)'\n')} lines)");
        }
        else
            Console.WriteLine("no changes");
    }

    // --- assess ----------------------------------------------------------------------------------------------------

    // base/rel with each segment in its case on disk, as Windows resolves it (Subtext's Subtext.Framework.csproj is
    // SubText.Framework.csproj).
    static string CaseInsensitive(string baseDir, string rel)
    {
        var cur = baseDir;
        foreach (var seg in rel.Split('/'))
        {
            if (seg is "" or ".")
                continue;
            if (seg == ".." || Files.Exists(Path.Combine(cur, seg)) || !Directory.Exists(cur))
            {
                cur = seg == ".." ? Files.Resolve(Path.Combine(cur, seg)) : Path.Combine(cur, seg);
                continue;
            }
            var match = Directory.EnumerateFileSystemEntries(cur).Select(Path.GetFileName)
                .FirstOrDefault(n => n.ToLowerInvariant() == seg.ToLowerInvariant()) ?? seg;
            cur = Path.Combine(cur, match);
        }
        return Files.Resolve(cur);
    }

    // The folders of the projects a project references, transitively (a class library's blockers block the site).
    static HashSet<string> ReferencedProjects(string proj, HashSet<string> seen = null)
    {
        seen ??= new HashSet<string>(Files.PathEquality);
        var text = File.Exists(proj) ? Files.ReadText(proj) : "";
        var netfx = proj + ".netfx"; // the original project, if already converted
        text += File.Exists(netfx) ? Files.ReadText(netfx) : "";
        foreach (Match m in Regex.Matches(text, @"<ProjectReference\s+Include=""([^""$]+)"""))
        {
            var target = CaseInsensitive(Path.GetDirectoryName(proj), m.Groups[1].Value.Replace('\\', '/'));
            if (Files.Exists(target) && seen.Add(target))
                ReferencedProjects(target, seen);
        }
        return seen;
    }

    static string Assess(TomlTable app)
    {
        var id = Id(app);
        var root = SiteDir(app);
        if (!Files.Exists(root))
            return Record(id, "assess", "fail", new() { ["summary"] = $"no folder {Shown(root)}" });
        var hits = Blockers.Concat(Notices).ToDictionary(b => b.Name, _ => new List<string>());
        var kinds = new SortedSet<string>(StringComparer.Ordinal);
        var targetFrameworks = new SortedSet<string>(StringComparer.Ordinal);
        var refsText = new List<string>();
        var proj = ProjectFile(app);
        var libraries = proj != null
            ? ReferencedProjects(proj).Select(Path.GetDirectoryName).Distinct(Files.PathEquality)
                .Where(l => !Files.PathEquality.Equals(l, root)).OrderBy(l => l, Files.PathOrder).ToList()
            : [];
        var files = Files.RGlobAll(root).Select(p => (Base: root, Path: p))
            .Concat(libraries.SelectMany(lib => Files.RGlobAll(lib).Select(p => (Base: lib, Path: p))));
        var excluded = new HashSet<string> { "bin", "obj", "bin_dotnet", "packages", "node_modules", ".git" };
        foreach (var (baseDir, p) in files)
        {
            if (!File.Exists(p) || Files.Parts(p).Any(excluded.Contains))
                continue;
            var name = Path.GetFileName(p);
            if (name == "machine.config" && Path.GetFileName(Path.GetDirectoryName(p)) == "App_Data") // webForCore extracts its own here at run time
                continue;
            var rel = Path.GetRelativePath(baseDir == root ? root : Checkout(app), p);
            var suffix = Files.Suffix(p).ToLowerInvariant();
            if (suffix == ".aspx")
                kinds.Add("webforms");
            if (name == "packages.config" || Files.Suffix(p) == ".csproj" || name.EndsWith(".csproj.netfx"))
            {
                var t = Files.ReadText(p);
                refsText.Add(t);
                foreach (Match m in Regex.Matches(t, "<TargetFrameworkVersion>([^<]+)<"))
                    targetFrameworks.Add(m.Groups[1].Value);
            }
            if (Scanned.Contains(suffix))
            {
                string text;
                try { text = Files.ReadText(p); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                if (suffix == ".config") // commented-out settings (mojoPortal's <authentication mode="Windows">)
                    text = Regex.Replace(text, "<!--.*?-->", c => new string('\n', c.Value.Count(ch => ch == '\n')), RegexOptions.Singleline);
                foreach (var (bname, pattern) in Blockers.Concat(Notices))
                    foreach (Match m in pattern.Matches(text))
                    {
                        var line = text.AsSpan(0, m.Index).Count('\n') + 1;
                        hits[bname].Add($"{rel}:{line}");
                    }
            }
        }
        var joined = string.Join("\n", refsText);
        foreach (var (kind, pattern) in Kinds)
            if (kind != "webforms" && pattern.IsMatch(joined))
                kinds.Add(kind);
        var found = hits.Where(kv => kv.Value.Count > 0).ToDictionary(kv => kv.Key,
            kv => kv.Value.Take(20).Concat(kv.Value.Count > 20 ? [$"... {kv.Value.Count - 20} more"] : Array.Empty<string>()).ToList());
        var notices = new JsonObject();
        foreach (var (n, _) in Notices)
            if (found.Remove(n, out var list))
                notices[n] = Json.Array(list);
        var blockers = new JsonObject();
        foreach (var (k, v) in found)
            blockers[k] = Json.Array(v);
        var status = found.Count > 0 ? "blocked" : "pass";
        var names = found.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        return Record(id, "assess", status, new()
        {
            ["kinds"] = Json.Array(kinds),
            ["target_frameworks"] = Json.Array(targetFrameworks),
            ["libraries"] = Json.Array(libraries.Select(l => Path.GetRelativePath(Checkout(app), l))),
            ["notices"] = notices,
            ["blockers"] = blockers,
            ["summary"] = names.Count > 0 ? string.Join(", ", names) : "no blockers found",
        });
    }

    // --- build -----------------------------------------------------------------------------------------------------

    internal static string ProjectFile(TomlTable app)
    {
        if (Toml.Truthy(Toml.Get(app, "project")))
            return Files.Join(Checkout(app), Toml.Str(app, "project"));
        return Files.Glob(SiteDir(app), "*.csproj").OrderBy(p => p, Files.PathOrder).FirstOrDefault();
    }

    internal static string AssemblyName(string proj)
    {
        var m = Regex.Match(Files.ReadText(proj), "<AssemblyName>([^<]+)<");
        return m.Success ? m.Groups[1].Value : Path.GetFileNameWithoutExtension(proj);
    }

    static string Build(TomlTable app)
    {
        var id = Id(app);
        var proj = ProjectFile(app);
        if (proj == null || !File.Exists(proj))
            return Record(id, "build", "fail", new() { ["summary"] = "no project file" });
        if (!Files.ReadText(proj).Contains("Microsoft.NET.Sdk"))
            return Record(id, "build", "skip", new() { ["summary"] = "not migrated yet (no SDK-style project)" });
        var log = Path.Combine(LogsDir(id), "build.log");
        var watch = Stopwatch.StartNew();
        var code = 0;
        using (var sink = new LogSink(log))
        {
            // also_build: projects built after the web project, that deploy themselves into the site (mojoPortal's features)
            foreach (var p in new[] { proj }.Concat(Toml.List(app, "also_build").Select(extra => Files.Join(Checkout(app), extra))))
            {
                // One MSBuild node, no compiler server, nothing left running after it: slower, but a big solution's build
                // doesn't take the memory the app, its database and the Windows VM need (a run was stopped for low
                // memory). A build that takes over 50 minutes has hung (CI gives the whole job 60).
                code = Proc.Run("dotnet", ["build", p, "-c", "Release", "-nologo", "-clp:ErrorsOnly;Summary",
                        "-m:1", "-nodeReuse:false", "-p:UseSharedCompilation=false", $"-p:RestoreAdditionalProjectSources={PackageSource()}"],
                    cwd: Path.GetDirectoryName(p), env: Env(), timeout: TimeSpan.FromMinutes(50), sink: sink).Code;
                if (code != 0)
                    break;
            }
        }
        var text = Scrub(log);
        var errors = Regex.Matches(text, @"error [A-Z]+\d+: [^\[\n]+").Select(m => m.Value).Distinct().OrderBy(e => e, StringComparer.Ordinal).ToList();
        return Record(id, "build", code == 0 ? "pass" : "fail", new()
        {
            ["seconds"] = Py.Round(watch.Elapsed.TotalSeconds),
            ["webforcore"] = WebforcoreUsed(proj),
            ["errors"] = Json.Array(errors.Take(30)),
            ["error_count"] = errors.Count,
            ["log"] = Path.GetRelativePath(Root, log),
            ["summary"] = code == 0 ? null : $"{errors.Count} distinct errors",
        });
    }

    // --- smoke -----------------------------------------------------------------------------------------------------

    static string Smoke(TomlTable app)
    {
        var id = Id(app);
        var server = new Server(app, "smoke");
        var checks = new List<JsonObject>();
        try
        {
            var why = server.Start();
            if (why != null)
            {
                var status = server.Process == null ? "skip" : "fail";
                var details = new JsonObject { ["summary"] = why };
                if (server.Process != null)
                    details["log_tail"] = Py.Tail(server.Stop(), 3000);
                return Record(id, "smoke", status, details);
            }
            var probes = Toml.Tables(app, "probes");
            if (probes.Count == 0)
                probes = [new TomlTable { ["path"] = "/", ["status"] = 200L }];
            // Probes with cookies = true share one cookie jar, in order (a session: add to a cart, then view it), and
            // all of them share the values they capture.
            using var http = new Http();
            var vars = new Dictionary<string, string> { ["setup_date"] = SetupDate(id) };
            for (var i = 0; i < probes.Count; i++)
                // The first request compiles the site, as on IIS.
                checks.Add(RunProbe(http, server.Base, probes[i], i == 0 ? 120 : 30, vars));
            // AddWebForms() writes W3C logs under the site folder by default (FINDINGS.md); they must not be served.
            // The log is flushed every second, and entries not yet flushed are lost when the server stops.
            Thread.Sleep(1500);
            var w3c = Files.Glob(Path.Combine(server.Site, "logs"), "W3SVC*/*.txt").OrderBy(p => p, Files.PathOrder)
                .Take(1).Select(f => "/" + Path.GetRelativePath(server.Site, f).Replace('\\', '/')).ToList();
            // An app whose error handler answers 200 with its own "not found" page sets [forbidden."<path>"] in app.toml,
            // e.g. { status = 200, absent = "<package " }: what matters is that the file's content isn't served.
            var overrides = Toml.Table(app, "forbidden");
            var forbidden = Forbidden.Append($"/bin_dotnet/{Path.GetFileName(server.Dll)}").ToList();
            if (File.Exists(server.Host))
                forbidden.Add("/bin_dotnet/webforcore-host/webforcore-host.dll");
            foreach (var path in forbidden)
            {
                var probe = new TomlTable { ["path"] = path, ["status"] = 404L, ["forbidden"] = true };
                if (Toml.Table(overrides, path) is { } o)
                    foreach (var (k, v) in o)
                        probe[k] = v;
                checks.Add(RunProbe(http, server.Base, probe, 30));
            }
            foreach (var path in w3c) // an app that answers every URL itself (OWIN) may return 200: what matters is the log's content
                checks.Add(RunProbe(http, server.Base, new TomlTable { ["path"] = path, ["status"] = 404L, ["forbidden"] = true, ["absent"] = "#Fields:" }, 30));
        }
        finally
        {
            server.Stop();
        }
        var failed = checks.Count(c => !(bool)c["ok"]);
        return Record(id, "smoke", failed > 0 ? "fail" : "pass", new()
        {
            ["checks"] = new JsonArray([.. checks]),
            ["summary"] = $"{checks.Count - failed}/{checks.Count} probes",
            ["log"] = Path.GetRelativePath(Root, server.Log),
        });
    }

    // {setup_date}: the date setup last set up the app's database (Subtext's post URLs carry the day its install
    // wizard ran), as yyyy/MM/dd.
    static string SetupDate(string id)
    {
        var date = Str(Stage(LoadResult(id), "setup")["date"]);
        return (string.IsNullOrEmpty(date) ? Py.Slice(Now(), 0, 10) : date).Replace('-', '/');
    }

    // One probe: its request (Http.Request), then what the response must be. Without app_id it's a forbidden path:
    // no {setup_date}, no cookies, no captures.
    static JsonObject RunProbe(Http http, string baseUrl, TomlTable probe, double timeout, Dictionary<string, string> vars = null)
    {
        var path = Toml.Str(probe, "path");
        var check = new JsonObject { ["path"] = path };
        if (Toml.Str(probe, "method")?.ToUpperInvariant() is { } named && named != "GET")
            check["method"] = named;
        var watch = Stopwatch.StartNew();
        Http.Response response;
        try
        {
            var request = Http.BuildRequest(probe, Http.MethodOf(probe, "GET"), path, vars ?? []);
            request.FollowRedirects = Toml.Truthy(Toml.Get(probe, "follow_redirects"));
            if (Toml.Get(probe, "timeout") is { } t)
                timeout = Convert.ToDouble(t, CultureInfo.InvariantCulture);
            response = http.Send(baseUrl, request, timeout, Toml.Truthy(Toml.Get(probe, "cookies")));
        }
        catch (Exception ex) // a probe reports any failure
        {
            check["ok"] = false;
            check["error"] = Http.Describe(ex);
            return check;
        }
        var status = response.Status;
        var body = response.Body;
        var expected = Toml.Get(probe, "status") is { } s ? Convert.ToInt64(s, CultureInfo.InvariantCulture) : 200;
        var ok = status == expected || (Toml.Truthy(Toml.Get(probe, "forbidden")) && status is 403 or 404);
        // absent replaces the status check: the content must not be there, whatever the status.
        if (Toml.Truthy(Toml.Get(probe, "absent")))
            ok = !body.Contains(Toml.Str(probe, "absent"));
        if (ok && Toml.Truthy(Toml.Get(probe, "contains")) && !body.Contains(Toml.Str(probe, "contains")))
            ok = false;
        var missing = vars != null ? http.Capture(probe, response, vars) : [];
        if (missing.Count > 0)
            ok = false;
        check["ok"] = ok;
        check["status"] = status;
        check["ms"] = Py.Round(watch.Elapsed.TotalMilliseconds);
        if (!ok)
        {
            check["title"] = Title(body);
            // And, for an error page with a stack trace, where it was thrown (on CI there's no other way to see it):
            // the exception lines ("[IOException: ...]") and the first frames under each.
            var at = body.IndexOf("Stack Trace:", StringComparison.Ordinal);
            if (at >= 0)
            {
                var trace = WebUtility.HtmlDecode(Regex.Replace(body[(at + "Stack Trace:".Length)..], "<[^>]+>", ""));
                var lines = Py.SplitLines(trace).Where(x => x.TrimStart().StartsWith('[') || x.TrimStart().StartsWith("at ", StringComparison.Ordinal))
                    .Select(x => x.TrimEnd()).Take(40);
                check["stack"] = Json.Array(lines.Select(x => Py.Slice(ScrubText(x), 0, 200)));
            }
            if (Toml.Truthy(Toml.Get(probe, "contains")))
                check["expected_text"] = Toml.Str(probe, "contains");
            if (missing.Count > 0)
                check["not_captured"] = Json.Array(missing);
        }
        return check;
    }

    // The ASP.NET error page's title is the most useful line; without one, the start of the response.
    static string Title(string body)
    {
        var m = Regex.Match(body, "<title>(.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return Py.Slice(m.Success ? m.Groups[1].Value.Trim() : Py.Slice(body, 0, 300), 0, 300);
    }

    // --- setup -----------------------------------------------------------------------------------------------------
    // What an app's database needs before the probes mean anything, from app.toml's [setup] (README.md):
    //   databases  SQL Server databases to create, empty, in services/compose.yml's SQL Server
    //   sql_files  scripts in the checkout run against the first of them, when setup has just created it
    //   steps      requests made to the running app, in order, with cookies kept and redirects followed (an install
    //              wizard): {get = path} or {post = path, fields = {...}}, and with button = name (and value) a Web Forms
    //              postback, whose hidden fields come from a GET of the page first; {post = path, json = "<body>"} posts
    //              that body as application/json (an ASP.NET AJAX page method, an MVC action). timeout (s, default
    //              120), contains (text the response must have), may_fail = true (the step's outcome doesn't matter),
    //              retries (times to run it again, 5 s apart, while it fails). A step can also say what a probe can
    //              (Http.Request): method, headers, body, form, json as a table, content_type, capture.
    // When every database it lists already exists, the app is set up already and nothing runs. On a database that's
    // gone (`docker compose down`), it runs again.

    const string MssqlPassword = "Smoke_test_1234"; // services/compose.yml's test-only value

    // sqlcmd against services/compose.yml's SQL Server (127.0.0.1:14330): the one on PATH if there is one (the Windows
    // CI job installs it with SQL Server), otherwise the container's own. script: a file to run.
    static (int Code, string Output) Sqlcmd(string[] args, string script = null)
    {
        List<string> cmd;
        byte[] stdin = null;
        if (Proc.Which("sqlcmd") != null)
        {
            cmd = ["sqlcmd", "-C", "-b", "-S", "127.0.0.1,14330", "-U", "sa", "-P", MssqlPassword, .. args];
            if (script != null)
                cmd.AddRange(["-i", script]);
        }
        else
        {
            cmd = ["docker", "compose", "-f", Path.Combine(Root, "services", "compose.yml"), "exec", "-T", "mssql",
                "/opt/mssql-tools18/bin/sqlcmd", "-C", "-b", "-S", "localhost", "-U", "sa", "-P", MssqlPassword, .. args];
            stdin = script != null ? File.ReadAllBytes(script) : null;
        }
        var r = Proc.Run(cmd[0], cmd.Skip(1), stdin: stdin ?? (cmd[0] == "docker" ? [] : null), timeout: TimeSpan.FromSeconds(600), text: false);
        if (r.TimedOut)
            return (1, "sqlcmd timed out after 600 s");
        return (r.Code, Encoding.UTF8.GetString([.. r.OutBytes, .. r.ErrBytes]));
    }

    // Runs [setup] steps against baseUrl; returns a list of what each did. restart: stops the app and starts it again,
    // for a restart = true step, and gives its new address (or throws, saying why it didn't start).
    static List<JsonObject> WebSteps(string baseUrl, List<TomlTable> steps, Func<string> restart)
    {
        using var http = new Http();
        var vars = new Dictionary<string, string>();

        JsonObject Run(TomlTable step, string path, double timeout)
        {
            try
            {
                Http.Request request;
                if (Toml.Has(step, "get"))
                    request = Http.BuildRequest(step, Http.MethodOf(step, "GET"), path, vars);
                else if (Toml.Has(step, "json") || Toml.Has(step, "body") || Toml.Has(step, "form") || Toml.Has(step, "multipart"))
                    request = Http.BuildRequest(step, Http.MethodOf(step, "POST"), path, vars);
                else
                {
                    // A form post: the page's hidden fields first, for a Web Forms postback (button), then the step's.
                    var fields = new OrderedDictionary<string, string>();
                    if (Toml.Truthy(Toml.Get(step, "button")))
                    {
                        var page = http.Send(baseUrl, new Http.Request { Path = Http.Expand(path, vars), FollowRedirects = true }, timeout, cookies: true).Body;
                        foreach (Match m in Regex.Matches(page, @"<input type=""hidden"" name=""([^""]+)"" id=""[^""]*"" value=""([^""]*)"""))
                            fields[WebUtility.HtmlDecode(m.Groups[1].Value)] = WebUtility.HtmlDecode(m.Groups[2].Value);
                        fields[Toml.Str(step, "button")] = Toml.Get(step, "value") is { } v ? Py.Str(v) : "";
                    }
                    foreach (var (k, v) in Toml.Table(step, "fields") ?? [])
                        fields[k] = Http.Expand(Py.Str(v), vars);
                    request = Http.BuildRequest(step, Http.MethodOf(step, "POST"), path, vars);
                    request.Body = Encoding.ASCII.GetBytes(Http.UrlEncode(fields));
                    request.ContentType ??= "application/x-www-form-urlencoded";
                }
                request.FollowRedirects = !Toml.Has(step, "follow_redirects") || Toml.Truthy(Toml.Get(step, "follow_redirects"));
                var response = http.Send(baseUrl, request, timeout, cookies: true);
                var contains = Toml.Get(step, "contains") is { } c ? Py.Str(c) : "";
                var missing = http.Capture(step, response, vars);
                var result = new JsonObject { ["step"] = path, ["status"] = response.Status, ["ok"] = response.Status < 400 && response.Body.Contains(contains) && missing.Count == 0 };
                if (Toml.Has(step, "method"))
                    result["method"] = request.Method;
                if (!(bool)result["ok"]) // new in the C# runner: what the failed step answered
                    result["title"] = Title(response.Body);
                if (missing.Count > 0)
                    result["not_captured"] = Json.Array(missing);
                return result;
            }
            catch (Exception ex) // a step reports any failure
            {
                return new JsonObject { ["step"] = path, ["ok"] = false, ["error"] = Http.Describe(ex) };
            }
        }

        var done = new List<JsonObject>();
        foreach (var step in steps)
        {
            // restart = true: the app stopped and started again (new in the C# runner). Umbraco 8 composes its back office's
            // services when it starts, so after its unattended install they're there only once it has restarted.
            if (Toml.Truthy(Toml.Get(step, "restart")))
            {
                var restarted = new JsonObject { ["step"] = "restart", ["ok"] = true };
                try { baseUrl = restart(); }
                catch (Exception ex)
                {
                    restarted["ok"] = false;
                    restarted["error"] = ex.Message;
                }
                done.Add(restarted);
                if (!(bool)restarted["ok"])
                    break;
                continue;
            }
            var path = Toml.Truthy(Toml.Get(step, "get")) ? Toml.Str(step, "get") : Toml.Str(step, "post") ?? Toml.Str(step, "path");
            // retries: a step the app's own restart can catch (Umbraco 6's App_Code recycle, F59) runs again, 5 s apart.
            var retries = Toml.Get(step, "retries") is { } rt ? Convert.ToInt32(rt, CultureInfo.InvariantCulture) : 0;
            var timeout = Toml.Get(step, "timeout") is { } to ? Convert.ToDouble(to, CultureInfo.InvariantCulture) : 120;
            JsonObject result = null;
            var attempt = 0;
            for (attempt = 0; attempt < 1 + retries; attempt++)
            {
                if (attempt > 0)
                    Thread.Sleep(5000);
                result = Run(step, path, timeout);
                if ((bool)result["ok"])
                    break;
            }
            attempt = Math.Min(attempt, retries);
            if (attempt > 0)
                result["attempts"] = attempt + 1;
            if (Toml.Truthy(Toml.Get(step, "may_fail")))
                result["may_fail"] = true;
            done.Add(result);
            if (!(bool)result["ok"] && !Toml.Truthy(Toml.Get(step, "may_fail")))
                break;
        }
        return done;
    }

    static string Setup(TomlTable app)
    {
        var id = Id(app);
        var s = Toml.Table(app, "setup");
        if (s == null || s.Count == 0) // not recorded: most apps have no setup stage
        {
            Console.WriteLine($"[{id}] setup: nothing to set up");
            return "skip";
        }
        var created = new List<string>();
        var databases = Toml.List(s, "databases");
        foreach (var db in databases)
        {
            var (code, output) = Sqlcmd(["-h", "-1", "-Q", $"SET NOCOUNT ON; IF DB_ID('{db}') IS NULL BEGIN CREATE DATABASE [{db}]; SELECT 'created' END ELSE SELECT 'exists'"]);
            if (code != 0)
                return Record(id, "setup", "fail", new() { ["summary"] = $"can't create database {db}", ["error"] = Py.Tail(output, 2000) });
            if (output.Contains("created"))
                created.Add(db);
        }
        var previous = Stage(LoadResult(id), "setup");
        if (databases.Count > 0 && created.Count == 0)
            return Record(id, "setup", "pass", new() { ["summary"] = "already set up", ["date"] = previous["date"]?.DeepClone() });
        foreach (var f in Toml.List(s, "sql_files"))
        {
            var (code, output) = Sqlcmd(["-d", databases[0]], script: Files.Join(Checkout(app), f));
            if (code != 0)
                return Record(id, "setup", "fail", new() { ["summary"] = $"{f} failed", ["error"] = Py.Tail(output, 2000) });
        }
        var steps = new List<JsonObject>();
        var stepTables = Toml.Tables(s, "steps");
        if (stepTables.Count > 0)
        {
            var server = new Server(app, "setup");
            try
            {
                var why = server.Start();
                if (why != null)
                    return Record(id, "setup", "fail", new() { ["summary"] = why });
                steps = WebSteps(server.Base, stepTables, () =>
                {
                    server.Stop();
                    server = new Server(app, "setup") { AppendLog = true };
                    return server.Start() is { } failed ? throw new InvalidOperationException(failed) : server.Base;
                });
            }
            finally
            {
                server.Stop();
            }
        }
        var failed = steps.Count(x => !(bool)x["ok"] && x["may_fail"] == null);
        var allowed = steps.Count(x => !(bool)x["ok"] && x["may_fail"] != null);
        var summary = $"{steps.Count(x => (bool)x["ok"])}/{steps.Count} steps" + (allowed > 0 ? $", {allowed} allowed to fail" : "");
        return Record(id, "setup", failed > 0 ? "fail" : "pass", new()
        {
            ["date"] = Py.Slice(Now(), 0, 10),
            ["created"] = Json.Array(created),
            ["steps"] = new JsonArray([.. steps]),
            ["summary"] = steps.Count > 0 ? summary : $"created {string.Join(", ", created)}",
        });
    }

    // --- report ----------------------------------------------------------------------------------------------------

    static readonly string[] Stages = ["fetch", "assess", "build", "smoke"];
    static readonly string[] Platforms = ["linux", "windows"];
    static string Icon(string status) => status switch
    {
        "pass" => "pass", "fail" => "**fail**", "blocked" => "blocked", "skip" => "–", null => "", _ => null,
    };

    static string[] PlatformCells(string id, string platform)
    {
        var r = LoadResult(id, platform);
        var build = Stage(r, "build");
        var smoke = Stage(r, "smoke");
        var smokeStatus = Str(smoke["status"]);
        var probes = smokeStatus is "pass" or "fail" ? Str(smoke["summary"]) ?? "" : "";
        return [Icon(Str(build["status"])), Icon(smokeStatus) + (probes != "" ? $" {probes}" : "")];
    }

    static void Report(List<TomlTable> apps)
    {
        var rows = new List<string>();
        foreach (var app in apps)
        {
            var r = LoadResult(Id(app), "linux");
            var cells = new List<string> { Icon(Str(Stage(r, "fetch")["status"])), Icon(Str(Stage(r, "assess")["status"])) };
            foreach (var platform in Platforms)
                cells.AddRange(PlatformCells(Id(app), platform));
            var assess = Stage(r, "assess");
            var kinds = assess.ContainsKey("kinds")
                ? assess["kinds"].AsArray().Select(k => Str(k))
                : Toml.List(app, "kinds");
            rows.Add($"| [{Toml.Str(app, "name")}](apps/{Id(app)}/NOTES.md) | {string.Join(", ", kinds)} | " + string.Join(" | ", cells) + $" | {Toml.Str(app, "status") ?? ""} |");
        }
        var output = new List<string>
        {
            "# Results", "",
            $"Generated by `scripts/wfc.cs report` on {Py.Slice(Now(), 0, 10)}. Each app's notes, log and results are in `apps/<id>/`. " +
            "Build and smoke run on Linux and on Windows (`results.json`, `results.windows.json`); smoke cells give the " +
            "checks that passed (the app's probes and the forbidden paths).", "",
            "| App | Kinds | Fetch | Assess | Linux build | Linux smoke | Windows build | Windows smoke | Catalog status |",
            "| --- | --- | --- | --- | --- | --- | --- | --- | --- |",
        };
        output.AddRange(rows);
        output.Add("");
        Files.WriteText(Path.Combine(Root, "RESULTS.md"), string.Join("\n", output));
        Console.WriteLine($"wrote RESULTS.md ({rows.Count} apps)");
    }

    // --- ci --------------------------------------------------------------------------------------------------------
    // .github/workflows/smoke.yml runs each app as its own job, on Linux and on Windows (README.md, "CI").

    static readonly (string Platform, string Runner)[] CiRunners = [("linux", "ubuntu-24.04"), ("windows", "windows-2025")];
    // A change here runs every app; elsewhere outside apps/ (FINDINGS.md, README.md, ...), none.
    static readonly string[] CiShared = ["scripts/", "services/", ".github/workflows/smoke.yml"];

    // Prints the job list for $GITHUB_OUTPUT: every migrated app (it has an overlay, and isn't rejected) on each
    // platform its [ci] allows; only the given apps, or with since, only the apps the changes since that commit touch.
    static void CiMatrix(List<TomlTable> apps, List<string> ids, string since)
    {
        var chosen = apps.Where(a => File.Exists(Path.Combine(Apps, Id(a), "overlay.patch")) && Toml.Str(a, "status") != "rejected").Select(Id).ToList();
        if (ids.Count > 0)
            chosen = chosen.Where(ids.Contains).ToList();
        if (since != null)
        {
            var r = Git(["diff", "--name-only", $"{since}...HEAD"], cwd: Root, check: false);
            // an unknown commit (a new branch's push): every app
            var files = r.Code == 0 ? r.Out.Split((char[])null, StringSplitOptions.RemoveEmptyEntries) : null;
            if (files != null && !files.Any(f => CiShared.Any(f.StartsWith)))
            {
                var touched = files.Where(f => f.StartsWith("apps/") && f.Count(ch => ch == '/') >= 2).Select(f => f.Split('/')[1]).ToHashSet();
                chosen = chosen.Where(touched.Contains).ToList();
            }
        }
        var byId = apps.ToDictionary(Id);
        var jobs = new JsonArray();
        foreach (var i in chosen)
            foreach (var (p, runner) in CiRunners)
                if (!Toml.Has(Toml.Table(byId[i], "ci"), p) || Toml.Truthy(Toml.Get(Toml.Table(byId[i], "ci"), p)))
                    jobs.Add(new JsonObject { ["app"] = i, ["platform"] = p, ["runner"] = runner, ["needs"] = string.Join(" ", Toml.List(byId[i], "needs")) });
        Console.WriteLine("matrix=" + PyJson.Dumps(new JsonObject { ["include"] = jobs }, indent: null, sortKeys: false, itemSeparator: ",", keySeparator: ":"));
        Console.WriteLine($"count={jobs.Count}");
    }

    // Compares this run's results with the committed ones, says so in the job's summary, and copies the results and
    // logs to ci-out/ for the job to upload. Exit status 1 if the app does worse: a check that passed fails, or a build
    // that passed fails.
    static int CiResult(string id)
    {
        var rel = $"apps/{id}/results{Suffix}.json";
        var current = LoadResult(id)["stages"].AsObject();
        var r = Git(["show", $"HEAD:{rel}"], cwd: Root, check: false);
        var old = r.Code == 0 ? JsonNode.Parse(r.Out)["stages"].AsObject() : new JsonObject();

        // The W3C log's file name and Subtext's post URL carry dates. A probe with a method other than GET is told
        // apart by it, and a request made more than once (a GET before a PUT and after it) by its place: "/x #2".
        HashSet<string> Passed(JsonObject stages)
        {
            var seen = new Dictionary<string, int>();
            var passed = new HashSet<string>();
            foreach (var c in stages["smoke"]?["checks"]?.AsArray() ?? [])
            {
                var key = (Str(c["method"]) is { } m ? m + " " : "") +
                          Regex.Replace(Regex.Replace(Str(c["path"]), @"/\d{4}/\d{2}/\d{2}/", "/{setup_date}/"), "/logs/W3SVC.*", "/logs/W3SVC*");
                seen[key] = seen.GetValueOrDefault(key) + 1;
                if ((bool)c["ok"])
                    passed.Add(seen[key] > 1 ? $"{key} #{seen[key]}" : key);
            }
            return passed;
        }

        string Says(JsonObject stages)
        {
            var build = Str(stages["build"]?["status"]) ?? "not run";
            var summary = Str(stages["smoke"]?["summary"]);
            return $"build {build}" + (string.IsNullOrEmpty(summary) ? "" : $", {summary}");
        }

        var lost = Passed(old).Except(Passed(current)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var gained = Passed(current).Except(Passed(old)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var worse = lost.Count > 0 || (Str(old["build"]?["status"]) == "pass" && Str(current["build"]?["status"]) != "pass");
        var text = $"**{id}** ({Platform}): {Says(current)}; committed: {(old.Count > 0 ? Says(old) : "none")}";
        if (lost.Count > 0)
            text += $". **No longer passing:** {string.Join(", ", lost)}";
        if (gained.Count > 0)
            text += $". Now passing: {string.Join(", ", gained)}";
        Console.WriteLine(text);
        if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } summaryFile)
            File.AppendAllText(summaryFile, $"- {(worse ? "❌" : "✅")} {text}\n");
        var output = Path.Combine(Root, "ci-out", "apps", id);
        Directory.CreateDirectory(output);
        if (File.Exists(Path.Combine(Root, rel)))
            File.Copy(Path.Combine(Root, rel), Path.Combine(output, Path.GetFileName(rel)), overwrite: true);
        var logs = Path.Combine(AppDir(id), $"logs{Suffix}");
        if (Directory.Exists(logs))
            Files.CopyTree(logs, Path.Combine(output, $"logs{Suffix}"));
        return worse ? 1 : 0;
    }

    // --- main ------------------------------------------------------------------------------------------------------

    const string Usage = "usage: dotnet run scripts/wfc.cs -- {packages,list,fetch,assess,build,setup,smoke,run,save-overlay,report,ci-matrix,ci-result} [ids ...] [--all] [--reset] [--changed-since REF] [--version VERSION]";

    static int UsageError(string message)
    {
        Console.Error.WriteLine(Usage);
        Console.Error.WriteLine($"wfc.cs: error: {message}");
        return 2;
    }

    static int Exit(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    public static int Entry(string[] argv)
    {
        try
        {
            return Run(argv);
        }
        catch (Exception ex) // as Python's traceback: what failed, and exit status 1
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    static int Run(string[] argv)
    {
        string[] commands = ["packages", "list", "fetch", "assess", "build", "setup", "smoke", "run", "save-overlay", "report", "ci-matrix", "ci-result"];
        string command = null, since = null, version = null;
        var ids = new List<string>();
        bool all = false, reset = false;
        for (var i = 0; i < argv.Length; i++)
        {
            var a = argv[i];
            if (a is "-h" or "--help")
            {
                Console.WriteLine(Usage + "\n\nThe commands are described at the top of scripts/wfc.cs and in README.md.");
                return 0;
            }
            if (a == "--all")
                all = true;
            else if (a == "--reset")
                reset = true;
            else if (a is "--changed-since" or "--version")
            {
                if (i + 1 >= argv.Length)
                    return UsageError($"argument {a}: expected one argument");
                if (a == "--version")
                    version = argv[++i];
                else
                    since = argv[++i];
            }
            else if (a.StartsWith("--changed-since="))
                since = a["--changed-since=".Length..];
            else if (a.StartsWith("--version="))
                version = a["--version=".Length..];
            else if (a.StartsWith('-') && a.Length > 1)
                return UsageError($"unrecognized arguments: {a}");
            else if (command == null)
                command = a;
            else
                ids.Add(a);
        }
        if (command == null)
            return UsageError("the following arguments are required: command");
        if (!commands.Contains(command))
            return UsageError($"argument command: invalid choice: '{command}' (choose from {string.Join(", ", commands.Select(c => $"'{c}'"))})");
        if (command == "packages")
        {
            WebforcoreVersion = version ?? WebforcoreVersion;
            return DownloadPackages() ? 0 : 1;
        }
        var apps = Catalog();
        var byId = apps.ToDictionary(Id);
        if (command == "list")
        {
            foreach (var app in apps)
            {
                var r = LoadResult(Id(app));
                Console.WriteLine($"{Id(app),-28} {Toml.Str(app, "status") ?? "",-10} " + string.Join(" ", Stages.Select(st => $"{st}={Str(Stage(r, st)["status"]) ?? "-"}")));
            }
            return 0;
        }
        if (command == "report")
        {
            Report(apps);
            return 0;
        }
        if (command == "ci-matrix")
        {
            var unknownApps = ids.Where(i => !byId.ContainsKey(i)).ToList();
            if (unknownApps.Count > 0)
                return Exit($"unknown app(s): {string.Join(", ", unknownApps)}");
            CiMatrix(apps, ids, since);
            return 0;
        }
        var chosen = all ? apps.Select(Id).ToList() : ids;
        var unknown = chosen.Where(i => !byId.ContainsKey(i)).ToList();
        if (unknown.Count > 0 || chosen.Count == 0)
            return Exit(unknown.Count > 0 ? $"unknown app(s): {string.Join(", ", unknown)}" : "give app ids or --all");
        if (command == "ci-result")
            return chosen.Count == 1 ? CiResult(chosen[0]) : Exit("ci-result: give one app");
        if (command is "build" or "run" && !Files.Glob(PackageSource(), "webForCore.Web.*.nupkg").Any())
            return Exit($"no webForCore {WebforcoreVersion} packages in {Path.GetRelativePath(Root, PackageSource())}: run dotnet run scripts/wfc.cs -- packages");
        foreach (var i in chosen)
        {
            var app = byId[i];
            switch (command)
            {
                case "fetch": Fetch(app, reset); break;
                case "assess": Assess(app); break;
                case "build": Build(app); break;
                case "setup": Setup(app); break;
                case "smoke": Smoke(app); break;
                case "save-overlay": SaveOverlay(app); break;
                case "run":
                    if (Fetch(app, reset) == "pass" && Build(app) == "pass")
                    {
                        if (Toml.Truthy(Toml.Get(app, "setup")))
                            Setup(app); // a failed setup still smokes: the probes say what it broke
                        Smoke(app);
                    }
                    break;
            }
        }
        return 0;
    }
}

// The built app, started on a free port (smoke and setup). Start() returns null, or why it couldn't start.
sealed class Server
{
    readonly TomlTable app;
    public readonly string Proj, Site, Dll, Host, Log;
    public string Base;
    public bool AppendLog; // a restart's server log goes after the first one's
    public Process Process;
    LogSink sink;
    Task[] pumps = [];

    public Server(TomlTable app, string stage)
    {
        this.app = app;
        var proj = Wfc.ProjectFile(app);
        Proj = proj != null && File.Exists(proj) ? proj : null;
        Site = Proj != null ? Path.GetDirectoryName(Proj) : null;
        Dll = Proj != null ? Path.Combine(Site, "bin_dotnet", Wfc.AssemblyName(Proj) + ".dll") : null;
        // From webForCore 0.7.0 the application runs as webforcore-host's worker process (webForCore#56), which the
        // webForCore package puts in the output; earlier releases run it directly.
        Host = Proj != null ? Path.Combine(Site, "bin_dotnet", "webforcore-host", "webforcore-host.dll") : null;
        Log = Path.Combine(Wfc.LogsDir((string)app["id"]), stage == "smoke" ? "server.log" : $"{stage}-server.log");
    }

    bool Running => Process != null && !Process.HasExited;

    public string Start()
    {
        if (Proj == null)
            return "no project";
        if (!File.Exists(Dll))
            return $"not built ({Wfc.Shown(Dll)} missing)";
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        Base = $"{(Toml.Truthy(Toml.Get(app, "https")) ? "https" : "http")}://127.0.0.1:{port}";
        var env = Wfc.Env();
        foreach (var (k, v) in Toml.Table(app, "env") ?? [])
            env[k] = Py.Str(v);
        var start = File.Exists(Host) ? Host : Dll;
        sink = new LogSink(Log, AppendLog);
        (Process, pumps) = Proc.Start("dotnet", [Path.GetRelativePath(Site, start), "--urls", Base], Site, env, sink);
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline && Running)
        {
            try
            {
                using var client = new TcpClient();
                if (client.ConnectAsync(IPAddress.Loopback, port).Wait(TimeSpan.FromSeconds(1)) && client.Connected)
                    break;
            }
            catch (Exception) { }
            Thread.Sleep(500);
        }
        // The host answers 503 until its worker serves.
        using var http = new Http();
        while (File.Exists(Host) && DateTime.UtcNow < deadline.AddSeconds(60) && Running)
        {
            try
            {
                if (http.Send(Base, new Http.Request { Path = "/" }, 120, cookies: false).Status != 503)
                    break;
            }
            catch (Exception) { }
            Thread.Sleep(1000);
        }
        return Running ? null : $"server exited with {Process.ExitCode}";
    }

    public string Stop()
    {
        if (Process != null)
        {
            if (Running)
            {
                // On Windows ending the process ends only webforcore-host; its worker then stops by itself, which takes a
                // while, and still has the site's compilation folder (and the assemblies loaded from it) when the next
                // stage starts a host for the same site (nopCommerce's plugin manager copies its plugins there at start:
                // "The process cannot access the file", FINDINGS.md F89, which this makes rarer but not gone). Ending
                // the whole tree frees them, as SIGTERM (a graceful stop of the host and its workers) does on Linux.
                if (OperatingSystem.IsWindows())
                {
                    try { Proc.Run("taskkill", ["/T", "/F", "/PID", Process.Id.ToString(CultureInfo.InvariantCulture)], timeout: TimeSpan.FromSeconds(30)); }
                    catch (Exception) { }
                    try { if (!Process.HasExited) Process.Kill(); }
                    catch (Exception) { }
                }
                else
                {
                    try { Proc.Run("kill", ["-TERM", Process.Id.ToString(CultureInfo.InvariantCulture)], timeout: TimeSpan.FromSeconds(30)); }
                    catch (Exception) { }
                }
                if (!Process.WaitForExit(10_000))
                {
                    try { Process.Kill(); }
                    catch (Exception) { }
                    Process.WaitForExit(10_000);
                }
            }
            // What the server wrote before it stopped; a worker that outlives it writes no more to the log.
            Task.WaitAll(pumps, TimeSpan.FromSeconds(5));
            sink?.Dispose();
        }
        return File.Exists(Log) ? Wfc.Scrub(Log) : "";
    }
}

// The HTTP side of probes and setup steps: one request model for both.
sealed class Http : IDisposable
{
    public const int MaxBody = 4_000_000; // bytes read per response: bundles are large, and larger on Windows (CRLF line ends)
    public static readonly string[] Methods = ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"];

    // Apps with https = true (RequireHttpsAttribute) are served over HTTPS with the .NET development certificate
    // (`dotnet dev-certs https`), which isn't verified. Redirects are followed here, by Send, only when asked; cookies
    // go to the one jar only for requests that ask for it.
    readonly HttpClient plain = NewClient(null);
    public readonly CookieContainer Jar = new();
    readonly HttpClient withJar;

    public Http() => withJar = NewClient(Jar);

    static HttpClient NewClient(CookieContainer jar) => new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = jar != null,
        CookieContainer = jar ?? new CookieContainer(),
        UseProxy = false,
        AutomaticDecompression = DecompressionMethods.None,
        SslOptions = new SslClientAuthenticationOptions { RemoteCertificateValidationCallback = (_, _, _, _) => true },
    }) { Timeout = Timeout.InfiniteTimeSpan };

    public void Dispose()
    {
        plain.Dispose();
        withJar.Dispose();
    }

    public sealed class Request
    {
        public string Method = "GET";
        public string Path;
        public List<(string Name, string Value)> Headers = [];
        public byte[] Body;
        public string ContentType;
        public bool FollowRedirects;
    }

    public sealed class Response
    {
        public int Status;
        public List<(string Name, string Value)> Headers = [];
        public string Body = "";
    }

    public static string MethodOf(TomlTable t, string fallback)
    {
        var method = Toml.Str(t, "method")?.ToUpperInvariant() ?? fallback;
        return Methods.Contains(method) ? method : throw new ArgumentException($"method {method}: not one of {string.Join(", ", Methods)}");
    }

    // {name}: a value captured by an earlier probe (or step), or {setup_date}. Other braces are left as they are.
    public static string Expand(string text, Dictionary<string, string> vars) =>
        vars.Count == 0 ? text : Regex.Replace(text, @"\{([A-Za-z_][A-Za-z0-9_]*)\}", m => vars.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);

    // A probe's or a step's request: method, path, headers, and one of body (raw), form (a table, urlencoded), json (a
    // table, or a string sent as it is) or multipart (a table of text fields); content_type overrides the body's type.
    // {name} in the path, the header values and the body takes a captured value.
    public static Request BuildRequest(TomlTable t, string method, string path, Dictionary<string, string> vars)
    {
        var r = new Request { Method = method, Path = Expand(path, vars) };
        foreach (var (k, v) in Toml.Table(t, "headers") ?? [])
            r.Headers.Add((k, Expand(Py.Str(v), vars)));
        if (new[] { "body", "form", "json", "multipart" }.Count(k => Toml.Has(t, k)) > 1)
            throw new ArgumentException("body, form, json and multipart: one at most");
        if (Toml.Get(t, "body") is { } body)
            r.Body = Encoding.UTF8.GetBytes(Expand(Py.Str(body), vars));
        else if (Toml.Get(t, "form") is TomlTable fields)
        {
            var values = new OrderedDictionary<string, string>();
            foreach (var (k, v) in fields)
                values[k] = Expand(Py.Str(v), vars);
            r.Body = Encoding.ASCII.GetBytes(UrlEncode(values));
            r.ContentType = "application/x-www-form-urlencoded";
        }
        else if (Toml.Get(t, "json") is { } json)
        {
            r.Body = Encoding.UTF8.GetBytes(json is string s ? Expand(s, vars) : JsonText(json, vars));
            r.ContentType = "application/json; charset=utf-8";
        }
        else if (Toml.Get(t, "multipart") is TomlTable parts)
        {
            // Text fields only; a field that's a table (Umbraco's contentItem) goes as its JSON.
            const string boundary = "webForCore-smoke-test-boundary";
            var sb = new StringBuilder();
            foreach (var (k, v) in parts)
                sb.Append($"--{boundary}\r\nContent-Disposition: form-data; name=\"{k}\"\r\n\r\n")
                    .Append(v is string text ? Expand(text, vars) : v is TomlTable or TomlArray or TomlTableArray ? JsonText(v, vars) : Py.Str(v)).Append("\r\n");
            sb.Append($"--{boundary}--\r\n");
            r.Body = Encoding.UTF8.GetBytes(sb.ToString());
            r.ContentType = $"multipart/form-data; boundary={boundary}";
        }
        if (Toml.Str(t, "content_type") is { } type)
            r.ContentType = type;
        return r;
    }

    static string JsonText(object value, Dictionary<string, string> vars) =>
        ToJson(value, vars).ToJsonString(new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

    static JsonNode ToJson(object value, Dictionary<string, string> vars) => value switch
    {
        null => null,
        string s => JsonValue.Create(Expand(s, vars)),
        bool b => JsonValue.Create(b),
        long l => JsonValue.Create(l),
        double d => JsonValue.Create(d),
        TomlTable t => new JsonObject(t.Select(kv => KeyValuePair.Create(kv.Key, ToJson(kv.Value, vars)))),
        TomlTableArray ta => new JsonArray([.. ta.Select(x => ToJson(x, vars))]),
        TomlArray a => new JsonArray([.. a.Select(x => ToJson(x, vars))]),
        _ => JsonValue.Create(value.ToString()),
    };

    // As Python's urllib.parse.urlencode: UTF-8, letters, digits and _.-~ as they are, space as +.
    public static string UrlEncode(IEnumerable<KeyValuePair<string, string>> fields) =>
        string.Join("&", fields.Select(kv => Quote(kv.Key) + "=" + Quote(kv.Value)));

    static string Quote(string s)
    {
        var sb = new StringBuilder();
        foreach (var b in Encoding.UTF8.GetBytes(s))
        {
            if (b is >= (byte)'a' and <= (byte)'z' or >= (byte)'A' and <= (byte)'Z' or >= (byte)'0' and <= (byte)'9' || b is (byte)'_' or (byte)'.' or (byte)'-' or (byte)'~')
                sb.Append((char)b);
            else if (b == ' ')
                sb.Append('+');
            else
                sb.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    // As urllib follows them: GET and HEAD after any redirect, POST after 301, 302 and 303 as a GET; and any method
    // after a 303 as a GET. Others (a PUT's 307) are answered as they are. At most 10.
    static string RedirectMethod(int status, string method)
    {
        if (status is 301 or 302 or 303 or 307 or 308 && method is "GET" or "HEAD")
            return method;
        if (status is 301 or 302 or 303 && method == "POST" || status == 303)
            return "GET";
        return null;
    }

    public Response Send(string baseUrl, Request request, double timeoutSeconds, bool cookies)
    {
        var client = cookies ? withJar : plain;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var url = baseUrl + request.Path;
        var method = request.Method;
        var body = request.Body;
        var contentType = request.ContentType;
        var headers = request.Headers;
        for (var hop = 0; ; hop++)
        {
            using var message = new HttpRequestMessage(new HttpMethod(method), url);
            message.Headers.TryAddWithoutValidation("User-Agent", "webForCore-smoke-test");
            message.Headers.TryAddWithoutValidation("Accept-Encoding", "identity");
            message.Headers.ConnectionClose = true;
            if (body != null)
            {
                message.Content = new ByteArrayContent(body);
                if (contentType != null)
                    message.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }
            foreach (var (name, value) in headers)
            {
                message.Headers.Remove(name);
                if (!message.Headers.TryAddWithoutValidation(name, value) && message.Content != null)
                {
                    message.Content.Headers.Remove(name);
                    message.Content.Headers.TryAddWithoutValidation(name, value);
                }
            }
            using var response = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cts.Token).GetAwaiter().GetResult();
            var status = (int)response.StatusCode;
            if (request.FollowRedirects && hop < 10 && response.Headers.TryGetValues("Location", out var locations)
                && RedirectMethod(status, method) is { } next)
            {
                url = new Uri(new Uri(url), locations.First()).AbsoluteUri;
                method = next;
                // The redirected request has no body, and no content headers.
                body = null;
                contentType = null;
                headers = headers.Where(h => !h.Name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)).ToList();
                continue;
            }
            var result = new Response { Status = status };
            foreach (var h in response.Headers.Concat(response.Content.Headers))
                foreach (var v in h.Value)
                    result.Headers.Add((h.Key, v));
            using var stream = response.Content.ReadAsStreamAsync(cts.Token).GetAwaiter().GetResult();
            var buffer = new MemoryStream();
            var chunk = new byte[81920];
            while (buffer.Length < MaxBody)
            {
                var n = stream.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, MaxBody - buffer.Length)), cts.Token).AsTask().GetAwaiter().GetResult();
                if (n == 0)
                    break;
                buffer.Write(chunk, 0, n);
            }
            result.Body = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
            return result;
        }
    }

    // capture = { name = "regex" } (over the body), { name = { header = "Name", regex = "..." } } (over that header's
    // values, one per line) or { name = { cookie = "Name" } } (the jar's cookie, for a cookies probe); the regex's first
    // group if it has one, else what it matched. Sets vars and returns the names that found nothing.
    public List<string> Capture(TomlTable t, Response response, Dictionary<string, string> vars)
    {
        var missing = new List<string>();
        foreach (var (name, spec) in Toml.Table(t, "capture") ?? [])
        {
            string value;
            switch (spec)
            {
                case string pattern:
                    value = Match(pattern, response.Body);
                    break;
                case TomlTable s when Toml.Str(s, "cookie") is { } cookie:
                    value = Jar.GetAllCookies().LastOrDefault(c => c.Name == cookie && !c.Expired)?.Value;
                    if (value != null && Toml.Str(s, "regex") is { } regex)
                        value = Match(regex, value);
                    break;
                case TomlTable s when Toml.Str(s, "header") is { } header:
                    var values = string.Join("\n", response.Headers.Where(x => string.Equals(x.Name, header, StringComparison.OrdinalIgnoreCase)).Select(x => x.Value));
                    value = Toml.Str(s, "regex") is { } re ? Match(re, values) : values.Length > 0 ? values : null;
                    break;
                default:
                    throw new ArgumentException($"capture {name}: a regex, or a table with header (and regex) or cookie");
            }
            if (value == null)
                missing.Add(name);
            else
                vars[name] = value;
        }
        return missing;
    }

    static string Match(string pattern, string text)
    {
        var m = Regex.Match(text, pattern);
        return !m.Success ? null : m.Groups.Count > 1 ? m.Groups[1].Value : m.Value;
    }

    // An exception, for the record: what kind and why.
    public static string Describe(Exception ex)
    {
        while (ex is AggregateException { InnerException: { } inner })
            ex = inner;
        if (ex is OperationCanceledException)
            return "TimeoutError: timed out";
        var message = ex.Message;
        if (ex is HttpRequestException { InnerException: { } cause })
            message += $" ({cause.Message})";
        return $"{ex.GetType().Name}: {message}";
    }
}

// app.toml's values, read as Python's dicts were: a missing key is null, and truthiness is Python's.
static class Toml
{
    public static object Get(TomlTable t, string key) => t != null && t.TryGetValue(key, out var v) ? v : null;
    public static bool Has(TomlTable t, string key) => t != null && t.ContainsKey(key);
    public static string Str(TomlTable t, string key) => Get(t, key) is { } v ? Py.Str(v) : null;
    public static TomlTable Table(TomlTable t, string key) => Get(t, key) as TomlTable;
    public static List<string> List(TomlTable t, string key) => Get(t, key) is TomlArray a ? a.Select(Py.Str).ToList() : [];

    public static List<TomlTable> Tables(TomlTable t, string key) => Get(t, key) switch
    {
        TomlTableArray ta => [.. ta],
        TomlArray a => [.. a.OfType<TomlTable>()],
        _ => [],
    };

    public static bool Truthy(object v) => v switch
    {
        null => false,
        bool b => b,
        string s => s.Length > 0,
        long l => l != 0,
        double d => d != 0,
        TomlTable t => t.Count > 0,
        TomlArray a => a.Count > 0,
        TomlTableArray ta => ta.Count > 0,
        _ => true,
    };
}

// What Python did that .NET does differently: str(), slices, round(), splitlines().
static class Py
{
    public static string Str(object v) => v switch
    {
        null => "None",
        string s => s,
        bool b => b ? "True" : "False",
        long l => l.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        _ => v.ToString(),
    };

    public static string Slice(string s, int start, int length) => s.Length <= start ? "" : s.Substring(start, Math.Min(length, s.Length - start));
    public static string Tail(string s, int length) => s.Length <= length ? s : s[^length..];
    public static long Round(double d) => (long)Math.Round(d, MidpointRounding.ToEven);
    // str.splitlines(): every line break Python knows, and nothing after a final one.
    public static List<string> SplitLines(string s)
    {
        var parts = Regex.Split(s, @"\r\n|[\n\r\v\f\x1c\x1d\x1e\x85\u2028\u2029]").ToList();
        if (parts.Count > 0 && parts[^1].Length == 0)
            parts.RemoveAt(parts.Count - 1);
        return parts;
    }
}

// Results as Python's json.dumps wrote them: ASCII only (\uXXXX), sorted keys, two-space indent.
static class PyJson
{
    public static string Dumps(JsonNode node, int? indent, bool sortKeys, string itemSeparator = null, string keySeparator = ": ")
    {
        var sb = new StringBuilder();
        Write(sb, node, indent, sortKeys, 0, itemSeparator ?? (indent == null ? ", " : ","), keySeparator);
        return sb.ToString();
    }

    static void NewLine(StringBuilder sb, int? indent, int level)
    {
        if (indent != null)
            sb.Append('\n').Append(' ', indent.Value * level);
    }

    static void Write(StringBuilder sb, JsonNode node, int? indent, bool sort, int level, string itemSep, string keySep)
    {
        switch (node)
        {
            case null:
                sb.Append("null");
                break;
            case JsonObject o:
                if (o.Count == 0)
                {
                    sb.Append("{}");
                    break;
                }
                sb.Append('{');
                var first = true;
                foreach (var (k, v) in sort ? o.OrderBy(kv => kv.Key, StringComparer.Ordinal) : o.AsEnumerable())
                {
                    if (!first)
                        sb.Append(itemSep);
                    first = false;
                    NewLine(sb, indent, level + 1);
                    WriteString(sb, k);
                    sb.Append(keySep);
                    Write(sb, v, indent, sort, level + 1, itemSep, keySep);
                }
                NewLine(sb, indent, level);
                sb.Append('}');
                break;
            case JsonArray a:
                if (a.Count == 0)
                {
                    sb.Append("[]");
                    break;
                }
                sb.Append('[');
                for (var i = 0; i < a.Count; i++)
                {
                    if (i > 0)
                        sb.Append(itemSep);
                    NewLine(sb, indent, level + 1);
                    Write(sb, a[i], indent, sort, level + 1, itemSep, keySep);
                }
                NewLine(sb, indent, level);
                sb.Append(']');
                break;
            case JsonValue v:
                switch (v.GetValueKind())
                {
                    case JsonValueKind.String: WriteString(sb, v.GetValue<string>()); break;
                    case JsonValueKind.True: sb.Append("true"); break;
                    case JsonValueKind.False: sb.Append("false"); break;
                    case JsonValueKind.Null: sb.Append("null"); break;
                    default: sb.Append(v.ToJsonString()); break;
                }
                break;
        }
    }

    static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 0x20 || c > 0x7e)
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}

static class Json
{
    public static JsonArray Array(IEnumerable<string> items) => new([.. items.Select(i => (JsonNode)JsonValue.Create(i))]);
}

// Files and paths as Python's pathlib saw them.
static class Files
{
    static readonly bool OnWindows = OperatingSystem.IsWindows();
    public static readonly StringComparer NameComparer = OnWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    public static readonly StringComparer PathEquality = OnWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    // Paths sort part by part ("a/x" before "a-b/x"), case-insensitively on Windows.
    public static readonly IComparer<string> PathOrder = Comparer<string>.Create((a, b) =>
    {
        var pa = Parts(a);
        var pb = Parts(b);
        for (var i = 0; i < Math.Min(pa.Length, pb.Length); i++)
        {
            var c = OnWindows ? string.CompareOrdinal(pa[i].ToLowerInvariant(), pb[i].ToLowerInvariant()) : string.CompareOrdinal(pa[i], pb[i]);
            if (c != 0)
                return c;
        }
        return pa.Length.CompareTo(pb.Length);
    });

    public static string[] Parts(string path) => path.Split(OnWindows ? ['\\', '/'] : ['/'], StringSplitOptions.RemoveEmptyEntries);
    public static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);
    public static string Join(string a, string b) => Path.GetFullPath(Path.Combine(a, b));

    public static bool IsRelativeTo(string path, string dir)
    {
        var rel = Path.GetRelativePath(dir, path);
        return rel != ".." && !rel.StartsWith(".." + Path.DirectorySeparatorChar) && !Path.IsPathRooted(rel);
    }

    // Python's suffix: from the name's last dot, but not a leading one (".gitignore" has none).
    public static string Suffix(string path)
    {
        var name = Path.GetFileName(path);
        var i = name.LastIndexOf('.');
        return i <= 0 || i == name.Length - 1 ? "" : name[i..];
    }

    // An absolute path with its symbolic links followed, as Path.resolve() gives.
    public static string Resolve(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full);
        var current = root;
        foreach (var part in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            var next = Path.Combine(current, part);
            FileSystemInfo info = Directory.Exists(next) ? new DirectoryInfo(next) : new FileInfo(next);
            if (info.LinkTarget != null && info.ResolveLinkTarget(true) is { } target)
                next = target.FullName;
            current = next;
        }
        return current;
    }

    // As Python's read_text(errors="replace"): UTF-8 (a BOM kept, unless sig), with \r\n and \r read as \n.
    public static string ReadText(string path, bool sig = false)
    {
        var text = Encoding.UTF8.GetString(File.ReadAllBytes(path));
        if (sig && text.StartsWith('\uFEFF'))
            text = text[1..];
        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    // As write_text: \n written as the platform's line end, UTF-8 (with a BOM if sig).
    public static void WriteText(string path, string text, bool sig = false) =>
        File.WriteAllText(path, Environment.NewLine == "\n" ? text : text.Replace("\n", Environment.NewLine), new UTF8Encoding(sig));

    // Every file and folder under root, in the order Python 3.13+'s rglob("*") gives them: each folder's entries as
    // the OS lists them, folders taken from a stack (the last found first), symbolic links to folders not followed.
    public static IEnumerable<string> RGlobAll(string root)
    {
        if (!Directory.Exists(root))
            yield break;
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            string[] entries;
            try { entries = Directory.EnumerateFileSystemEntries(dir).ToArray(); }
            catch (IOException) { continue; }
            catch (UnauthorizedAccessException) { continue; }
            foreach (var e in entries)
            {
                yield return e;
                var info = new DirectoryInfo(e);
                if (info.Exists && info.LinkTarget == null)
                    stack.Push(e);
            }
        }
    }

    public static IEnumerable<string> RGlob(string root, string pattern)
    {
        var regex = SegmentRegex(pattern);
        return RGlobAll(root).Where(p => File.Exists(p) && regex.IsMatch(Path.GetFileName(p)));
    }

    // Python's Path.glob: * ? [...] within a name, ** for any folders; case-insensitive on Windows.
    public static IEnumerable<string> Glob(string root, string pattern)
    {
        IEnumerable<string> current = Directory.Exists(root) ? [root] : [];
        foreach (var segment in pattern.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == "**")
            {
                current = current.SelectMany(d => new[] { d }.Concat(RGlobAll(d).Where(x => new DirectoryInfo(x) is { Exists: true, LinkTarget: null }))).Distinct(PathEquality).ToList();
                continue;
            }
            var regex = SegmentRegex(segment);
            current = current.Where(Directory.Exists)
                .SelectMany(d => Directory.EnumerateFileSystemEntries(d).Where(e => regex.IsMatch(Path.GetFileName(e)))).ToList();
        }
        return current;
    }

    static Regex SegmentRegex(string pattern)
    {
        var sb = new StringBuilder("^");
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '*')
                sb.Append(".*");
            else if (c == '?')
                sb.Append('.');
            else if (c == '[' && i + 2 < pattern.Length && pattern.IndexOf(']', i + 2) is var end && end > i)
            {
                var set = pattern[(i + 1)..end];
                sb.Append('[').Append(set.StartsWith('!') ? "^" + Regex.Escape(set[1..]) : Regex.Escape(set)).Append(']');
                i = end;
            }
            else
                sb.Append(Regex.Escape(c.ToString()));
        }
        return new Regex(sb.Append('$').ToString(), RegexOptions.Singleline | (OnWindows ? RegexOptions.IgnoreCase : RegexOptions.None));
    }

    public static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from))
            File.Copy(f, Path.Combine(to, Path.GetFileName(f)), overwrite: true);
        foreach (var d in Directory.GetDirectories(from))
            CopyTree(d, Path.Combine(to, Path.GetFileName(d)));
    }
}

// A log file that a process's output and error both go to, as they come.
sealed class LogSink : IDisposable
{
    readonly FileStream file;
    bool closed;

    public LogSink(string path, bool append = false) =>
        file = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

    public void Write(byte[] buffer, int count)
    {
        lock (this)
        {
            if (closed)
                return;
            file.Write(buffer, 0, count);
            file.Flush();
        }
    }

    public void Dispose()
    {
        lock (this)
        {
            if (closed)
                return;
            closed = true;
            file.Dispose();
        }
    }
}

// Processes: each with a timeout, its output read as it comes so it never blocks on a full pipe.
static class Proc
{
    public sealed class Result
    {
        public int Code;
        public bool TimedOut;
        public byte[] OutBytes = [];
        public byte[] ErrBytes = [];
        public string Out = "";
        public string Err = "";
    }

    public static Dictionary<string, string> CurrentEnvironment()
    {
        var env = new Dictionary<string, string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (System.Collections.DictionaryEntry kv in Environment.GetEnvironmentVariables())
            env[(string)kv.Key] = (string)kv.Value;
        return env;
    }

    // The program on PATH (with PATHEXT's extensions on Windows), or null.
    public static string Which(string name)
    {
        var exts = OperatingSystem.IsWindows() ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE").Split(';') : [""];
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            foreach (var ext in exts)
                if (File.Exists(Path.Combine(dir, name + ext)))
                    return Path.Combine(dir, name + ext);
        return null;
    }

    static ProcessStartInfo Info(string file, IEnumerable<string> args, string cwd, Dictionary<string, string> env, bool stdin)
    {
        var psi = new ProcessStartInfo(file)
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = stdin,
            CreateNoWindow = true, WorkingDirectory = cwd ?? "",
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);
        if (env != null)
        {
            psi.Environment.Clear();
            foreach (var (k, v) in env)
                psi.Environment[k] = v;
        }
        return psi;
    }

    static async Task Pump(Stream from, LogSink sink)
    {
        var buffer = new byte[8192];
        int n;
        while ((n = await from.ReadAsync(buffer)) > 0)
            sink.Write(buffer, n);
    }

    // A long-running process (the app's server), its output and error going to sink.
    public static (Process, Task[]) Start(string file, IEnumerable<string> args, string cwd, Dictionary<string, string> env, LogSink sink)
    {
        var p = Process.Start(Info(file, args, cwd, env, false));
        return (p, [Pump(p.StandardOutput.BaseStream, sink), Pump(p.StandardError.BaseStream, sink)]);
    }

    // Runs a program to the end, or until timeout (then it's killed, with its children). text: the output decoded as
    // Python's text mode did (UTF-8, \r\n as \n). sink: the output goes there instead.
    public static Result Run(string file, IEnumerable<string> args, string cwd = null, Dictionary<string, string> env = null,
        TimeSpan? timeout = null, byte[] stdin = null, bool text = true, LogSink sink = null)
    {
        using var p = Process.Start(Info(file, args, cwd, env, stdin != null));
        Task<byte[]> ReadAll(Stream s) => Task.Run(async () =>
        {
            var ms = new MemoryStream();
            await s.CopyToAsync(ms);
            return ms.ToArray();
        });
        Task outTask, errTask;
        Task<byte[]> outBytes = null, errBytes = null;
        if (sink != null)
        {
            outTask = Pump(p.StandardOutput.BaseStream, sink);
            errTask = Pump(p.StandardError.BaseStream, sink);
        }
        else
        {
            outTask = outBytes = ReadAll(p.StandardOutput.BaseStream);
            errTask = errBytes = ReadAll(p.StandardError.BaseStream);
        }
        if (stdin != null)
            Task.Run(() =>
            {
                try
                {
                    p.StandardInput.BaseStream.Write(stdin);
                    p.StandardInput.Close();
                }
                catch (IOException) { }
            });
        var result = new Result();
        if (!p.WaitForExit(timeout ?? Timeout.InfiniteTimeSpan))
        {
            result.TimedOut = true;
            try { p.Kill(entireProcessTree: true); }
            catch (Exception) { }
            p.WaitForExit(10_000);
        }
        // The output to its end, unless something the program started still holds it.
        Task.WaitAll([outTask, errTask], TimeSpan.FromSeconds(30));
        result.Code = p.HasExited ? p.ExitCode : -1;
        if (outBytes is { IsCompletedSuccessfully: true })
            result.OutBytes = outBytes.Result;
        if (errBytes is { IsCompletedSuccessfully: true })
            result.ErrBytes = errBytes.Result;
        if (text)
        {
            result.Out = Encoding.UTF8.GetString(result.OutBytes).Replace("\r\n", "\n").Replace('\r', '\n');
            result.Err = Encoding.UTF8.GetString(result.ErrBytes).Replace("\r\n", "\n").Replace('\r', '\n');
        }
        return result;
    }
}

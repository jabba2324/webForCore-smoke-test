#!/usr/bin/env python3
"""The webForCore smoke test bed: fetch open-source ASP.NET 4.x apps, assess them, build their
migrated projects against webForCore, run them on Kestrel and probe them.

Standard library only (Python 3.11+, for tomllib).

    scripts/wfc.py packages [--version 0.8.0]  # download webForCore's (and webForCore-thirdparty's) release packages into .packages/
    scripts/wfc.py list
    scripts/wfc.py fetch <id>... | --all [--reset]
    scripts/wfc.py assess <id>... | --all
    scripts/wfc.py build <id>...
    scripts/wfc.py setup <id>...             # the app's database, as app.toml's [setup] says (once per new database)
    scripts/wfc.py smoke <id>...
    scripts/wfc.py run <id>... | --all        # fetch, build, setup and smoke
    scripts/wfc.py save-overlay <id>         # the app checkout's changes -> apps/<id>/overlay.patch
    scripts/wfc.py report                    # apps/*/results.json (and results.windows.json) -> RESULTS.md
    scripts/wfc.py ci-matrix [<id>...] [--changed-since <ref>]  # the GitHub Actions job list (.github/workflows/smoke.yml)
    scripts/wfc.py ci-result <id>            # a CI job's result against the committed one; stages it for upload

Every round runs on Linux and Windows: in CI (README.md, "CI"), or by running this on a Windows machine. On Windows the
results go to results.windows.json and logs.windows/, and LOG.md lines say (windows).
"""

import argparse
import datetime
import html
import http.cookiejar
import json
import os
import re
import shutil
import socket
import subprocess
import sys
import time
import tomllib
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
APPS = ROOT / "apps"
# Each app has its own folder, apps/<id>/:
#   app.toml        where it comes from, how to build it, what to probe
#   NOTES.md        the migration notes: assessment, every change and why, open problems
#   LOG.md          one line per stage run, appended by this script
#   overlay.patch   the migration, as a patch on the pinned commit
#   results.json    the latest result of each stage (Linux; results.windows.json on Windows)
#   logs/           the latest build and server logs (logs.windows/ on Windows)
#   src/            the checkout (not committed)

WEBFORCORE = Path(os.environ.get("WEBFORCORE_DIR", ROOT.parent / "webForCore")).resolve()
# webForCore's NuGet packages come from its GitHub releases, not nuget.org. `packages` downloads a release here, and
# builds restore from it (apps reference the packages, docs/MIGRATING.md section 1). WEBFORCORE_DIR is only needed by
# apps still migrated against webForCore's source.
WEBFORCORE_REPO = "xforcore/webForCore"
WEBFORCORE_VERSION = os.environ.get("WEBFORCORE_VERSION") or "0.8.0"
# Builds of third-party libraries for webForCore (WebActivatorEx, AjaxControlToolkit; CLAUDE.md), from
# webForCore-thirdparty's releases, go in the same folder.
THIRDPARTY_REPO = "xforcore/webForCore-thirdparty"
THIRDPARTY_VERSION = os.environ.get("THIRDPARTY_VERSION") or "0.6.0"
THIRDPARTY_PACKAGES = ("webForCore.WebActivatorEx", "webForCore.AjaxControlToolkit")
# System.Drawing, which the webForCore package depends on from 0.7.0 (drawingForCore and drawingForCore.Common), goes
# there too.
DRAWING_REPO = "xforcore/drawingForCore"
DRAWING_VERSION = os.environ.get("DRAWING_VERSION") or "0.3.0"
PACKAGES = ROOT / ".packages"
# The OS this run is on. Linux keeps the original file names; Windows' results sit beside them.
PLATFORM = "windows" if os.name == "nt" else "linux"
SUFFIX = "" if PLATFORM == "linux" else ".windows"

# Phase 0 of webForCore's docs/AGENT-MIGRATION.md.
BLOCKERS = [
    ("windows-auth", r'mode="Windows"|WindowsIdentity|WindowsPrincipal|<identity\s+impersonate'),
    ("wcf-hosting", r"ServiceHost\b|<system\.serviceModel>(?:(?!</system\.serviceModel>)[\s\S])*?<services>"),
    ("linq-to-sql", r"System\.Data\.Linq|LinqDataSource"),
    # EF 1-4's ObjectContext (System.Data.Objects); EF 6's (System.Data.Entity.Core.Objects) runs on .NET.
    ("ef-objectcontext", r"System\.Data\.Objects\b|EntityDataSource"),
    ("web-references", r"App_WebReferences"),
    ("windows-only", r"Microsoft\.Win32\.Registry|\[ComImport\]|Marshal\.GetActiveObject|System\.Messaging|Microsoft\.Office\.Interop"),
    ("metafiles", r"\bMetafile\b"),
    ("netdatacontract", r"NetDataContractSerializer"),
    ("appdomain-thread-abort", r"AppDomain\.CreateDomain|Thread\.Abort|Thread\.ResetAbort"),
    ("com-plus", r"System\.EnterpriseServices|Transaction=\""),
    ("dpapi-config", r"DataProtectionConfigurationProvider"),
]
# Not blockers, but code to change (docs/NAMESPACES.md): listed by assess without blocking.
NOTICES = [
    ("binaryformatter", r"(?<!Formatters\.Binary\.)\bnew BinaryFormatter\b|System\.Runtime\.Serialization\.Formatters\.Binary"),
    ("smtpclient-config", r"new SmtpClient\(\s*\)"),
]
SCANNED = {".cs", ".vb", ".aspx", ".ascx", ".master", ".asax", ".ashx", ".asmx", ".cshtml", ".vbhtml", ".config", ".svc"}

# Framework detection from packages.config / csproj references.
KINDS = {
    "webforms": r"\.aspx$",
    "mvc": r"Microsoft\.AspNet\.Mvc|System\.Web\.Mvc",
    "webapi": r"Microsoft\.AspNet\.WebApi|System\.Web\.Http",
    "webpages": r"Microsoft\.AspNet\.WebPages|System\.Web\.WebPages",
    "owin": r"Microsoft\.Owin",
    "signalr": r"Microsoft\.AspNet\.SignalR",
    "identity": r"Microsoft\.AspNet\.Identity",
    "ef6": r'id="EntityFramework"|Include="EntityFramework',
}

# Must never be served (docs/MIGRATING.md section 7).
FORBIDDEN = ["/web.config", "/App_Data/", "/Global.asax.cs", "/obj/", "/packages.config"]


def catalog():
    apps = {}
    for f in sorted(APPS.glob("*/app.toml")):
        with f.open("rb") as fh:
            apps[f.parent.name] = {**tomllib.load(fh), "id": f.parent.name}
    return apps


def app_dir(app_id):
    return APPS / app_id


def logs_dir(app_id):
    d = app_dir(app_id) / f"logs{SUFFIX}"
    d.mkdir(exist_ok=True)
    return d


def env():
    e = dict(os.environ)
    e.setdefault("OPENSSL_ENABLE_SHA1_SIGNATURES", "1")  # Fedora/RHEL
    e.setdefault("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
    e.setdefault("DOTNET_NOLOGO", "1")
    e["WEBFORCORE_DIR"] = str(WEBFORCORE)  # projects migrated against the source import $(WEBFORCORE_DIR)/src/...
    return e


def package_source():
    return PACKAGES / f"v{WEBFORCORE_VERSION}"


def download_packages():
    dest = package_source()
    dest.mkdir(parents=True, exist_ok=True)
    ok = True
    for repo, version in ((WEBFORCORE_REPO, WEBFORCORE_VERSION), (THIRDPARTY_REPO, THIRDPARTY_VERSION),
                          (DRAWING_REPO, DRAWING_VERSION)):
        for attempt in range(3):  # release asset downloads fail with 503 now and then
            r = subprocess.run(["gh", "release", "download", f"v{version}", "--repo", repo,
                                "--pattern", "*.nupkg", "--dir", str(dest), "--skip-existing"], capture_output=True, text=True)
            if r.returncode == 0:
                break
            time.sleep(5)
        if r.returncode:
            print(f"{repo} v{version}: {r.stderr.strip()[:200]}")
            ok = False
    count = len(list(dest.glob("*.nupkg")))
    print(f"{dest.relative_to(ROOT)}: {count} packages (webForCore {WEBFORCORE_VERSION}, webForCore-thirdparty {THIRDPARTY_VERSION}, drawingForCore {DRAWING_VERSION})")
    return ok


def webforcore_used(proj):
    """Which webForCore the project builds against, for the record."""
    if "$(WEBFORCORE_DIR)" in proj.read_text(errors="replace"):
        commit = git("rev-parse", "--short=12", "HEAD", cwd=WEBFORCORE, check=False).stdout.strip()
        return f"source {commit}"
    used = f"packages {WEBFORCORE_VERSION}"
    # The web project or one of the solution's libraries (beside it, or one folder up).
    projects = {proj, *proj.parent.parent.rglob("*.csproj")}
    if any(f'Include="{p}' in f.read_text(errors="replace") for f in projects for p in THIRDPARTY_PACKAGES):
        used += f", webForCore-thirdparty {THIRDPARTY_VERSION}"
    return used


def now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat(timespec="seconds")


def scrub(log):
    """Logs are committed, so this machine's paths become placeholders."""
    text = log.read_text(errors="replace")
    # Longest first: the repo's path can start with webForCore's (../webForCore-smoke-test).
    pairs = [(str(WEBFORCORE), "$WEBFORCORE_DIR"), (str(ROOT), "<repo>"), (str(Path.home()), "~")]
    if CHECKOUTS:
        pairs.append((str(CHECKOUTS), "$WFC_CHECKOUTS"))
    pairs += [(a.replace("\\", "/"), b) for a, b in pairs if "\\" in a]  # Windows paths, also written with /
    for path, name in sorted(pairs, key=lambda p: -len(p[0])):
        text = text.replace(path, name)
    log.write_text(text)
    return text


def load_result(app_id, platform=PLATFORM):
    p = app_dir(app_id) / f"results{'' if platform == 'linux' else '.' + platform}.json"
    return json.loads(p.read_text()) if p.exists() else {"id": app_id, "stages": {}}


def record(app_id, stage, status, **details):
    """status: pass | fail | blocked | skip"""
    r = load_result(app_id)
    r["stages"][stage] = {"status": status, "at": now(), **details}
    (app_dir(app_id) / f"results{SUFFIX}.json").write_text(json.dumps(r, indent=2, sort_keys=True) + "\n")
    where = "" if PLATFORM == "linux" else f" ({PLATFORM})"
    line = f"- {now()} **{stage}**{where}: {status}" + (f" ({details['summary']})" if details.get("summary") else "")
    if details.get("commit"):
        line += f" at `{details['commit'][:12]}`"
    log = app_dir(app_id) / "LOG.md"
    if not log.exists():
        log.write_text(f"# {app_id}: run log\n\nAppended by `scripts/wfc.py`, one line per stage run.\n\n")
    with log.open("a", newline="\n") as f:
        f.write(line + "\n")
    print(f"[{app_id}] {stage}{where}: {status}" + (f" ({details.get('summary')})" if details.get("summary") else ""))
    return status


def git(*args, cwd=None, check=True):
    # Files as committed (overlays are byte-for-byte patches) and Windows paths over 260 characters (mojoPortal). A
    # transfer slower than 1 KB/s for 2 minutes fails rather than hanging the run, and so does a repository that's gone:
    # GitHub answers 401, and git asked for a username, in a desktop dialog when SSH_ASKPASS is set (KDE's ksshaskpass),
    # so BetterCMS's clone waited for hours. No prompt of either kind, so it fails.
    quiet = {k: v for k, v in os.environ.items() if k not in ("SSH_ASKPASS", "GIT_ASKPASS")}
    return subprocess.run(["git", "-c", "core.autocrlf=false", "-c", "core.longpaths=true", "-c", "core.askPass=",
                           "-c", "http.lowSpeedLimit=1000", "-c", "http.lowSpeedTime=120", *args], cwd=cwd, check=check,
                          capture_output=True, text=True,
                          env={**quiet, "GIT_TERMINAL_PROMPT": "0", "GCM_INTERACTIVE": "never"})


# WFC_CHECKOUTS: a folder for the apps' checkouts (<folder>/<id>) instead of apps/<id>/src, e.g. on another disk: the
# big apps are hundreds of thousands of files, and their builds gigabytes.
CHECKOUTS = Path(os.environ["WFC_CHECKOUTS"]).resolve() if os.environ.get("WFC_CHECKOUTS") else None


def checkout(app):
    return CHECKOUTS / app["id"] if CHECKOUTS else app_dir(app["id"]) / "src"


def shown(path):
    """A path for the record: relative to the repo, or to WFC_CHECKOUTS."""
    if CHECKOUTS and path.is_relative_to(CHECKOUTS):
        return f"$WFC_CHECKOUTS/{path.relative_to(CHECKOUTS).as_posix()}"
    return str(path.relative_to(ROOT))


def site_dir(app):
    return checkout(app) / app.get("site", app.get("project_dir", ""))


# --- fetch ---------------------------------------------------------------------------------------

def fetch(app, reset=False):
    """Clones the app at its pinned ref and applies its overlay. An existing checkout is left alone
    (it may hold migration work not yet saved with save-overlay) unless reset is given."""
    dest = checkout(app)
    if dest.exists() and not reset:
        commit = git("rev-parse", "HEAD", cwd=dest).stdout.strip()
        return record(app["id"], "fetch", "pass", commit=commit, summary="existing checkout")
    if not dest.exists():
        r = git("clone", "--filter=blob:none", "--no-checkout", app["repo"], str(dest), check=False)
        if r.returncode:
            return record(app["id"], "fetch", "fail", summary="clone failed", error=r.stderr[-2000:])
    ref = app.get("ref") or "HEAD"
    r = git("checkout", "--force", "--detach", ref, cwd=dest, check=False)
    if r.returncode:
        git("fetch", "origin", ref, cwd=dest, check=False)
        r = git("checkout", "--force", "--detach", ref, cwd=dest, check=False)
        if r.returncode:
            return record(app["id"], "fetch", "fail", summary=f"no ref {ref}", error=r.stderr[-2000:])
    git("clean", "-fdx", "-e", "bin_dotnet", "-e", "obj", cwd=dest, check=False)
    # Files the migration deletes but whose content shouldn't be in the patch (the app's checked-in binaries).
    for pattern in app.get("remove", []):
        for f in dest.glob(pattern):
            f.unlink()
    commit = git("rev-parse", "HEAD", cwd=dest).stdout.strip()
    patch = app_dir(app["id"]) / "overlay.patch"
    if patch.exists():
        r = git("apply", "--whitespace=nowarn", str(patch), cwd=dest, check=False)
        if r.returncode:
            return record(app["id"], "fetch", "fail", commit=commit, summary="overlay does not apply", error=r.stderr[-2000:])
    return record(app["id"], "fetch", "pass", commit=commit, overlay=patch.exists())


def save_overlay(app):
    dest = checkout(app)
    # Build output and files webForCore generates at run time aren't part of the migration. The apps' own
    # .gitignore files predate bin_dotnet.
    generated = [":(exclude,glob)**/bin_dotnet/**", ":(exclude,glob)**/obj/**", ":(exclude,glob)**/App_Data/machine.config",
                 ":(exclude,glob)**/logs/W3SVC*/**", ":(exclude,glob)**/logs/*/W3SVC*/**"] + [f":(exclude,glob){p}" for p in app.get("remove", []) + app.get("generated", [])]
    git("reset", "-q", cwd=dest, check=False)  # drops intent-to-add entries from an earlier save
    # Deletions must stay unstaged, or they drop out of the diff.
    git("add", "-N", "--ignore-removal", "--", ".", *generated, cwd=dest)
    # As bytes: the apps' files with CRLF line endings must keep them, or the patch no longer applies to the checkout.
    diff = subprocess.run(["git", "-c", "core.autocrlf=false", "diff", "--binary", "--", ".", *generated], cwd=dest, check=True,
                          capture_output=True).stdout
    p = app_dir(app["id"]) / "overlay.patch"
    if diff:
        p.write_bytes(diff)
        print(f"wrote {p.relative_to(ROOT)} ({diff.count(b'\n')} lines)")
    else:
        print("no changes")


# --- assess --------------------------------------------------------------------------------------

def case_insensitive(base, rel):
    """base/rel with each segment in its case on disk, as Windows resolves it (Subtext's Subtext.Framework.csproj is
    SubText.Framework.csproj)."""
    cur = base
    for seg in rel.split("/"):
        if seg in ("", "."):
            continue
        if seg == ".." or (cur / seg).exists() or not cur.is_dir():
            cur = (cur / seg).resolve() if seg == ".." else cur / seg
            continue
        cur = cur / next((c.name for c in cur.iterdir() if c.name.lower() == seg.lower()), seg)
    return cur.resolve()


def referenced_projects(proj, seen=None):
    """The folders of the projects a project references, transitively (a class library's blockers block the site)."""
    seen = seen if seen is not None else set()
    text = proj.read_text(errors="replace") if proj.exists() else ""
    netfx = proj.with_name(proj.name + ".netfx")  # the original project, if already converted
    text += netfx.read_text(errors="replace") if netfx.exists() else ""
    for ref in re.findall(r'<ProjectReference\s+Include="([^"$]+)"', text):
        target = case_insensitive(proj.parent, ref.replace("\\", "/"))
        if target.exists() and target not in seen:
            seen.add(target)
            referenced_projects(target, seen)
    return seen


def assess(app):
    root = site_dir(app)
    if not root.exists():
        return record(app["id"], "assess", "fail", summary=f"no folder {shown(root)}")
    hits = {name: [] for name, _ in BLOCKERS + NOTICES}
    kinds = set()
    target_frameworks = set()
    refs_text = []
    proj = project_file(app)
    libraries = sorted({t.parent for t in referenced_projects(proj)} - {root}) if proj else []
    files = [(root, p) for p in root.rglob("*")] + [(lib, p) for lib in libraries for p in lib.rglob("*")]
    for base, p in files:
        if not p.is_file() or any(part in {"bin", "obj", "bin_dotnet", "packages", "node_modules", ".git"} for part in p.parts):
            continue
        if p.name == "machine.config" and p.parent.name == "App_Data":  # webForCore extracts its own here at run time
            continue
        rel = str(p.relative_to(root if base == root else checkout(app)))
        if p.suffix.lower() == ".aspx":
            kinds.add("webforms")
        if p.name == "packages.config" or p.suffix == ".csproj" or p.name.endswith(".csproj.netfx"):
            text = p.read_text(errors="replace")
            refs_text.append(text)
            target_frameworks.update(re.findall(r"<TargetFrameworkVersion>([^<]+)<", text))
        if p.suffix.lower() in SCANNED:
            try:
                text = p.read_text(errors="replace")
            except OSError:
                continue
            if p.suffix.lower() == ".config":  # commented-out settings (mojoPortal's <authentication mode="Windows">)
                text = re.sub(r"<!--.*?-->", lambda c: "\n" * c.group(0).count("\n"), text, flags=re.S)
            for name, pattern in BLOCKERS + NOTICES:
                for m in re.finditer(pattern, text):
                    line = text.count("\n", 0, m.start()) + 1
                    hits[name].append(f"{rel}:{line}")
    joined = "\n".join(refs_text)
    for kind, pattern in KINDS.items():
        if kind != "webforms" and re.search(pattern, joined):
            kinds.add(kind)
    hits = {k: v[:20] + ([f"... {len(v) - 20} more"] if len(v) > 20 else []) for k, v in hits.items() if v}
    notices = {k: hits.pop(k) for k, _ in NOTICES if k in hits}
    status = "blocked" if hits else "pass"
    return record(app["id"], "assess", status, kinds=sorted(kinds), target_frameworks=sorted(target_frameworks),
                  libraries=[str(lib.relative_to(checkout(app))) for lib in libraries], notices=notices,
                  blockers=hits, summary=", ".join(sorted(hits)) or "no blockers found")


# --- build ---------------------------------------------------------------------------------------

def project_file(app):
    if app.get("project"):
        return checkout(app) / app["project"]
    found = sorted(site_dir(app).glob("*.csproj"))
    return found[0] if found else None


def assembly_name(proj):
    text = proj.read_text(errors="replace")
    m = re.search(r"<AssemblyName>([^<]+)<", text)
    return m.group(1) if m else proj.stem


def build(app):
    proj = project_file(app)
    if not proj or not proj.exists():
        return record(app["id"], "build", "fail", summary="no project file")
    if "Microsoft.NET.Sdk" not in proj.read_text(errors="replace"):
        return record(app["id"], "build", "skip", summary="not migrated yet (no SDK-style project)")
    log = logs_dir(app["id"]) / "build.log"
    t = time.time()
    with log.open("w") as f:
        # also_build: projects built after the web project, that deploy themselves into the site (mojoPortal's features)
        for p in [proj] + [checkout(app) / extra for extra in app.get("also_build", [])]:
            f.flush()
            # One MSBuild node, no compiler server, nothing left running after it: slower, but a big solution's build
            # doesn't take the memory the app, its database and the Windows VM need (a run was stopped for low memory).
            r = subprocess.run(["dotnet", "build", str(p), "-c", "Release", "-nologo", "-clp:ErrorsOnly;Summary",
                                "-m:1", "-nodeReuse:false", "-p:UseSharedCompilation=false",
                                f"-p:RestoreAdditionalProjectSources={package_source()}"],
                               cwd=p.parent, env=env(), stdout=f, stderr=subprocess.STDOUT)
            if r.returncode != 0:
                break
    text = scrub(log)
    errors = sorted(set(re.findall(r"error [A-Z]+\d+: [^\[\n]+", text)))
    return record(app["id"], "build", "pass" if r.returncode == 0 else "fail", seconds=round(time.time() - t),
                  webforcore=webforcore_used(proj), errors=errors[:30], error_count=len(errors), log=str(log.relative_to(ROOT)),
                  summary=None if r.returncode == 0 else f"{len(errors)} distinct errors")


# --- smoke ---------------------------------------------------------------------------------------

def free_port():
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k):
        return None


# Apps with https = true (RequireHttpsAttribute) are served over HTTPS with the .NET development certificate
# (`dotnet dev-certs https`), which isn't verified.
_TLS = __import__("ssl").create_default_context()
_TLS.check_hostname = False
_TLS.verify_mode = __import__("ssl").CERT_NONE
OPENER = urllib.request.build_opener(NoRedirect, urllib.request.HTTPSHandler(context=_TLS))


MAX_BODY = 4_000_000  # bytes read per response: bundles are large, and larger on Windows (CRLF line ends)


def get(url, timeout, opener=OPENER):
    req = urllib.request.Request(url, headers={"User-Agent": "webForCore-smoke-test"})
    try:
        with opener.open(req, timeout=timeout) as r:
            return r.status, dict(r.headers), r.read(MAX_BODY).decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, dict(e.headers), e.read(MAX_BODY).decode("utf-8", "replace")


class Server:
    """The built app, started on a free port (smoke and setup). `start()` returns None, or why it couldn't start."""

    def __init__(self, app, stage):
        self.app = app
        proj = project_file(app)
        self.proj = proj if proj and proj.exists() else None
        self.site = proj.parent if self.proj else None
        self.dll = self.site / "bin_dotnet" / f"{assembly_name(proj)}.dll" if self.proj else None
        # From webForCore 0.7.0 the application runs as webforcore-host's worker process (webForCore#56), which the
        # webForCore package puts in the output; earlier releases run it directly.
        self.host = self.site / "bin_dotnet" / "webforcore-host" / "webforcore-host.dll" if self.proj else None
        self.log = logs_dir(app["id"]) / ("server.log" if stage == "smoke" else f"{stage}-server.log")
        self.process = None
        self.base = None

    def start(self):
        if not self.proj:
            return "no project"
        if not self.dll.exists():
            return f"not built ({shown(self.dll)} missing)"
        port = free_port()
        self.base = f"{'https' if self.app.get('https') else 'http'}://127.0.0.1:{port}"
        e = env()
        e.update(self.app.get("env", {}))
        start = self.host if self.host.exists() else self.dll
        with self.log.open("w") as f:
            self.process = subprocess.Popen(["dotnet", str(start.relative_to(self.site)), "--urls", self.base],
                                            cwd=self.site, env=e, stdout=f, stderr=subprocess.STDOUT)
        deadline = time.time() + 60
        while time.time() < deadline and self.process.poll() is None:
            try:
                socket.create_connection(("127.0.0.1", port), timeout=1).close()
                break
            except OSError:
                time.sleep(0.5)
        # The host answers 503 until its worker serves.
        while self.host.exists() and time.time() < deadline + 60 and self.process.poll() is None:
            try:
                if get(self.base + "/", timeout=120)[0] != 503:
                    break
            except OSError:
                pass
            time.sleep(1)
        return f"server exited with {self.process.returncode}" if self.process.poll() is not None else None

    def stop(self):
        if self.process:
            self.process.terminate()
            try:
                self.process.wait(10)
            except subprocess.TimeoutExpired:
                self.process.kill()
                self.process.wait(10)
        return scrub(self.log) if self.log.exists() else ""


def smoke(app):
    server = Server(app, "smoke")
    checks = []
    try:
        why = server.start()
        if why:
            status = "skip" if server.process is None else "fail"
            return record(app["id"], "smoke", status, summary=why,
                          **({"log_tail": server.stop()[-3000:]} if server.process else {}))
        base, site, dll = server.base, server.site, server.dll
        probes = app.get("probes") or [{"path": "/", "status": 200}]
        # Probes with cookies = true share one cookie jar, in order (a session: add to a cart, then view it).
        cookie_opener = urllib.request.build_opener(NoRedirect, urllib.request.HTTPSHandler(context=_TLS),
                                                    urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        for i, probe in enumerate(probes):
            # The first request compiles the site, as on IIS.
            checks.append(run_probe(base, probe, timeout=120 if i == 0 else 30, app_id=app["id"],
                                    opener=cookie_opener if probe.get("cookies") else OPENER))
        # AddWebForms() writes W3C logs under the site folder by default (FINDINGS.md); they must not be served.
        # The log is flushed every second, and entries not yet flushed are lost when the server stops.
        time.sleep(1.5)
        w3c = sorted((site / "logs").glob("W3SVC*/*.txt"))
        w3c = [f"/{w3c[0].relative_to(site).as_posix()}"] if w3c else []
        # An app whose error handler answers 200 with its own "not found" page sets [forbidden."<path>"] in app.toml,
        # e.g. { status = 200, absent = "<package " }: what matters is that the file's content isn't served.
        overrides = app.get("forbidden", {})
        for path in FORBIDDEN + [f"/bin_dotnet/{dll.name}"] + (["/bin_dotnet/webforcore-host/webforcore-host.dll"] if server.host.exists() else []):
            probe = {"path": path, "status": 404, "forbidden": True, **overrides.get(path, {})}
            checks.append(run_probe(base, probe, timeout=30))
        for path in w3c:  # an app that answers every URL itself (OWIN) may return 200: what matters is the log's content
            checks.append(run_probe(base, {"path": path, "status": 404, "forbidden": True, "absent": "#Fields:"}, timeout=30))
    finally:
        server.stop()
    failed = [c for c in checks if not c["ok"]]
    return record(app["id"], "smoke", "fail" if failed else "pass", checks=checks,
                  summary=f"{len(checks) - len(failed)}/{len(checks)} probes",
                  log=str(server.log.relative_to(ROOT)))


def probe_url(app_id, path):
    """A probe's path, with {setup_date} as the date setup last set up the app's database (Subtext's post URLs carry
    the day its install wizard ran)."""
    if "{setup_date}" in path:
        date = load_result(app_id)["stages"].get("setup", {}).get("date") or now()[:10]
        path = path.replace("{setup_date}", date.replace("-", "/"))
    return path


def run_probe(base, probe, timeout, app_id=None, opener=OPENER):
    path = probe["path"]
    url = probe_url(app_id, path) if app_id else path
    t = time.time()
    try:
        status, headers, body = get(base + url, timeout, opener)
    except Exception as ex:  # noqa: BLE001 - a probe reports any failure
        return {"path": path, "ok": False, "error": f"{type(ex).__name__}: {ex}"}
    ok = status == probe.get("status", 200) or bool(probe.get("forbidden") and status in (403, 404))
    if probe.get("absent"):
        ok = probe["absent"] not in body
    if ok and probe.get("contains") and probe["contains"] not in body:
        ok = False
    check = {"path": path, "ok": ok, "status": status, "ms": round((time.time() - t) * 1000)}
    if not ok:
        # The ASP.NET error page's title is the most useful line.
        m = re.search(r"<title>(.*?)</title>", body, re.S | re.I)
        check["title"] = (m.group(1).strip() if m else body[:300])[:300]
        if probe.get("contains"):
            check["expected_text"] = probe["contains"]
    return check


# --- setup ---------------------------------------------------------------------------------------
# What an app's database needs before the probes mean anything, from app.toml's [setup] (README.md):
#   databases  SQL Server databases to create, empty, in services/compose.yml's SQL Server
#   sql_files  scripts in the checkout run against the first of them, when setup has just created it
#   steps      requests made to the running app, in order, with cookies kept and redirects followed (an install
#              wizard): {get = path} or {post = path, fields = {...}}, and with button = name (and value) a Web Forms
#              postback, whose hidden fields come from a GET of the page first; {post = path, json = "<body>"} posts
#              that body as application/json (an ASP.NET AJAX page method, an MVC action). timeout (s, default 120), contains
#              (text the response must have), may_fail = true (the step's outcome doesn't matter), retries
#              (times to run it again, 5 s apart, while it fails)
# When every database it lists already exists, the app is set up already and nothing runs. On a database that's gone
# (`docker compose down`), it runs again.

MSSQL_PASSWORD = "Smoke_test_1234"  # services/compose.yml's test-only value


def sqlcmd(*args, script=None):
    """sqlcmd against services/compose.yml's SQL Server (127.0.0.1:14330): the one on PATH if there is one (the Windows
    CI job installs it with SQL Server), otherwise the container's own. script: a file to run."""
    if shutil.which("sqlcmd"):
        cmd = ["sqlcmd", "-C", "-b", "-S", "127.0.0.1,14330", "-U", "sa", "-P", MSSQL_PASSWORD, *args]
        stdin = None
        if script:
            cmd += ["-i", str(script)]
    else:
        cmd = ["docker", "compose", "-f", str(ROOT / "services" / "compose.yml"), "exec", "-T", "mssql",
               "/opt/mssql-tools18/bin/sqlcmd", "-C", "-b", "-S", "localhost", "-U", "sa", "-P", MSSQL_PASSWORD, *args]
        stdin = script.read_bytes() if script else None
    try:
        r = subprocess.run(cmd, input=stdin, capture_output=True, timeout=600)
    except subprocess.TimeoutExpired:
        return 1, "sqlcmd timed out after 600 s"
    return r.returncode, (r.stdout + r.stderr).decode("utf-8", "replace")


def web_steps(base, steps):
    """Runs [setup] steps against base; returns a list of what each did."""
    jar = http.cookiejar.CookieJar()
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar),
                                         urllib.request.HTTPSHandler(context=_TLS))

    def request(path, data, timeout, json_body=None):
        headers = {"User-Agent": "webForCore-smoke-test"}
        body = urllib.parse.urlencode(data).encode() if data is not None else None
        if json_body is not None:
            body = json_body.encode()
            headers["Content-Type"] = "application/json; charset=utf-8"
        req = urllib.request.Request(base + path, data=body, headers=headers)
        try:
            with opener.open(req, timeout=timeout) as r:
                return r.status, r.read(MAX_BODY).decode("utf-8", "replace")
        except urllib.error.HTTPError as e:
            return e.code, e.read(MAX_BODY).decode("utf-8", "replace")

    def run(step, path, timeout):
        try:
            if "get" in step:
                status, body = request(path, None, timeout)
            elif "json" in step:
                status, body = request(path, None, timeout, json_body=step["json"])
            else:
                fields = {}
                if step.get("button"):
                    _, page = request(path, None, timeout)
                    fields = {html.unescape(m.group(1)): html.unescape(m.group(2)) for m in
                              re.finditer(r'<input type="hidden" name="([^"]+)" id="[^"]*" value="([^"]*)"', page)}
                    fields[step["button"]] = step.get("value", "")
                fields.update(step.get("fields", {}))
                status, body = request(path, fields, timeout)
            return {"step": path, "status": status, "ok": status < 400 and step.get("contains", "") in body}
        except Exception as ex:  # noqa: BLE001 - a step reports any failure
            return {"step": path, "ok": False, "error": f"{type(ex).__name__}: {ex}"}

    done = []
    for step in steps:
        path = step.get("get") or step.get("post")
        # retries: a step the app's own restart can catch (Umbraco 6's App_Code recycle, F59) runs again, 5 s apart.
        for attempt in range(1 + step.get("retries", 0)):
            if attempt:
                time.sleep(5)
            result = run(step, path, step.get("timeout", 120))
            if result["ok"]:
                break
        if attempt:
            result["attempts"] = attempt + 1
        if step.get("may_fail"):
            result["may_fail"] = True
        done.append(result)
        if not result["ok"] and not step.get("may_fail"):
            break
    return done


def setup(app):
    s = app.get("setup")
    if not s:  # not recorded: most apps have no setup stage
        print(f"[{app['id']}] setup: nothing to set up")
        return "skip"
    created = []
    for db in s.get("databases", []):
        code, out = sqlcmd("-h", "-1", "-Q", f"SET NOCOUNT ON; IF DB_ID('{db}') IS NULL BEGIN CREATE DATABASE [{db}]; "
                                             "SELECT 'created' END ELSE SELECT 'exists'")
        if code:
            return record(app["id"], "setup", "fail", summary=f"can't create database {db}", error=out[-2000:])
        if "created" in out:
            created.append(db)
    previous = load_result(app["id"])["stages"].get("setup", {})
    if s.get("databases") and not created:
        return record(app["id"], "setup", "pass", summary="already set up", date=previous.get("date"))
    for f in s.get("sql_files", []):
        code, out = sqlcmd("-d", s["databases"][0], script=checkout(app) / f)
        if code:
            return record(app["id"], "setup", "fail", summary=f"{f} failed", error=out[-2000:])
    steps = []
    if s.get("steps"):
        server = Server(app, "setup")
        try:
            why = server.start()
            if why:
                return record(app["id"], "setup", "fail", summary=why)
            steps = web_steps(server.base, s["steps"])
        finally:
            server.stop()
    failed = [x for x in steps if not x["ok"] and not x.get("may_fail")]
    allowed = sum(1 for x in steps if not x["ok"] and x.get("may_fail"))
    summary = f"{sum(x['ok'] for x in steps)}/{len(steps)} steps" + (f", {allowed} allowed to fail" if allowed else "")
    return record(app["id"], "setup", "fail" if failed else "pass", date=now()[:10], created=created, steps=steps,
                  summary=summary if steps else f"created {', '.join(created)}")


# --- report --------------------------------------------------------------------------------------

STAGES = ["fetch", "assess", "build", "smoke"]
PLATFORMS = ["linux", "windows"]
ICON = {"pass": "pass", "fail": "**fail**", "blocked": "blocked", "skip": "–", None: ""}


def platform_cells(app_id, platform):
    s = load_result(app_id, platform)["stages"]
    build, smoke = s.get("build", {}), s.get("smoke", {})
    probes = smoke.get("summary", "") if smoke.get("status") in ("pass", "fail") else ""
    return [ICON.get(build.get("status")), ICON.get(smoke.get("status")) + (f" {probes}" if probes else "")]


def report(apps):
    rows = []
    for app in apps.values():
        s = load_result(app["id"], "linux")["stages"]
        cells = [ICON.get(s.get(st, {}).get("status")) for st in ("fetch", "assess")]
        for platform in PLATFORMS:
            cells += platform_cells(app["id"], platform)
        kinds = ", ".join(s.get("assess", {}).get("kinds", app.get("kinds", [])))
        rows.append(f"| [{app['name']}](apps/{app['id']}/NOTES.md) | {kinds} | " + " | ".join(cells) + f" | {app.get('status', '')} |")
    out = ["# Results", "",
           f"Generated by `scripts/wfc.py report` on {now()[:10]}. Each app's notes, log and results are in `apps/<id>/`. "
           "Build and smoke run on Linux and on Windows (`results.json`, `results.windows.json`); smoke cells give the "
           "checks that passed (the app's probes and the forbidden paths).", "",
           "| App | Kinds | Fetch | Assess | Linux build | Linux smoke | Windows build | Windows smoke | Catalog status |",
           "| --- | --- | --- | --- | --- | --- | --- | --- | --- |", *rows, ""]
    (ROOT / "RESULTS.md").write_text("\n".join(out))
    print(f"wrote RESULTS.md ({len(rows)} apps)")


# --- ci ------------------------------------------------------------------------------------------
# .github/workflows/smoke.yml runs each app as its own job, on Linux and on Windows (README.md, "CI").

CI_RUNNERS = {"linux": "ubuntu-24.04", "windows": "windows-2025"}
# A change here runs every app; elsewhere outside apps/ (FINDINGS.md, README.md, ...), none.
CI_SHARED = ("scripts/", "services/", ".github/workflows/smoke.yml")


def ci_matrix(apps, ids, since):
    """Prints the job list for $GITHUB_OUTPUT: every migrated app (it has an overlay, and isn't rejected) on each
    platform its [ci] allows; only the given apps, or with since, only the apps the changes since that commit touch."""
    chosen = [i for i, app in apps.items() if (APPS / i / "overlay.patch").exists() and app.get("status") != "rejected"]
    if ids:
        chosen = [i for i in chosen if i in ids]
    if since:
        r = git("diff", "--name-only", f"{since}...HEAD", cwd=ROOT, check=False)
        files = r.stdout.split() if r.returncode == 0 else None  # an unknown commit (a new branch's push): every app
        if files is not None and not any(f.startswith(CI_SHARED) for f in files):
            touched = {f.split("/")[1] for f in files if f.startswith("apps/") and f.count("/") >= 2}
            chosen = [i for i in chosen if i in touched]
    jobs = [{"app": i, "platform": p, "runner": runner, "needs": " ".join(apps[i].get("needs", []))}
            for i in chosen for p, runner in CI_RUNNERS.items() if apps[i].get("ci", {}).get(p, True)]
    print(f"matrix={json.dumps({'include': jobs}, separators=(',', ':'))}")
    print(f"count={len(jobs)}")


def ci_result(app_id):
    """Compares this run's results with the committed ones, says so in the job's summary, and copies the results and
    logs to ci-out/ for the job to upload. Exit status 1 if the app does worse: a check that passed fails, or a build
    that passed fails."""
    rel = f"apps/{app_id}/results{SUFFIX}.json"
    new = load_result(app_id)["stages"]
    r = git("show", f"HEAD:{rel}", cwd=ROOT, check=False)
    old = json.loads(r.stdout)["stages"] if r.returncode == 0 else {}

    def passed(stages):
        # The W3C log's file name and Subtext's post URL carry dates.
        return {re.sub(r"/logs/W3SVC.*", "/logs/W3SVC*", re.sub(r"/\d{4}/\d{2}/\d{2}/", "/{setup_date}/", c["path"]))
                for c in stages.get("smoke", {}).get("checks", []) if c["ok"]}

    def says(stages):
        build, smoke = stages.get("build", {}).get("status", "not run"), stages.get("smoke", {})
        return f"build {build}" + (f", {smoke.get('summary')}" if smoke.get("summary") else "")

    lost, gained = sorted(passed(old) - passed(new)), sorted(passed(new) - passed(old))
    worse = bool(lost) or (old.get("build", {}).get("status") == "pass" and new.get("build", {}).get("status") != "pass")
    text = f"**{app_id}** ({PLATFORM}): {says(new)}; committed: {says(old) if old else 'none'}"
    if lost:
        text += f". **No longer passing:** {', '.join(lost)}"
    if gained:
        text += f". Now passing: {', '.join(gained)}"
    print(text)
    if os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as f:
            f.write(f"- {'❌' if worse else '✅'} {text}\n")
    out = ROOT / "ci-out" / "apps" / app_id
    out.mkdir(parents=True, exist_ok=True)
    if (ROOT / rel).exists():
        shutil.copy2(ROOT / rel, out)
    if (app_dir(app_id) / f"logs{SUFFIX}").exists():
        shutil.copytree(app_dir(app_id) / f"logs{SUFFIX}", out / f"logs{SUFFIX}", dirs_exist_ok=True)
    return 1 if worse else 0


# --- main ----------------------------------------------------------------------------------------

def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("command", choices=["packages", "list", "fetch", "assess", "build", "setup", "smoke", "run", "save-overlay", "report",
                                       "ci-matrix", "ci-result"])
    p.add_argument("ids", nargs="*", help="app ids")
    p.add_argument("--all", action="store_true")
    p.add_argument("--reset", action="store_true", help="fetch, run: discard the checkout's changes and reapply the overlay")
    p.add_argument("--changed-since", metavar="REF", help="ci-matrix: only the apps the changes since REF touch")
    p.add_argument("--version", help="packages: the webForCore release (default: $WEBFORCORE_VERSION or 0.8.0; webForCore-thirdparty: $THIRDPARTY_VERSION or 0.6.0)")
    a = p.parse_args()
    if a.command == "packages":
        global WEBFORCORE_VERSION
        WEBFORCORE_VERSION = a.version or WEBFORCORE_VERSION
        sys.exit(0 if download_packages() else 1)
    apps = catalog()
    if a.command == "list":
        for app in apps.values():
            s = load_result(app["id"])["stages"]
            print(f"{app['id']:28} {app.get('status', ''):10} " + " ".join(f"{st}={s.get(st, {}).get('status', '-')}" for st in STAGES))
        return
    if a.command == "report":
        return report(apps)
    if a.command == "ci-matrix":
        unknown = [i for i in a.ids if i not in apps]
        if unknown:
            sys.exit(f"unknown app(s): {', '.join(unknown)}")
        return ci_matrix(apps, a.ids, a.changed_since)
    ids = list(apps) if a.all else a.ids
    unknown = [i for i in ids if i not in apps]
    if unknown or not ids:
        sys.exit(f"unknown app(s): {', '.join(unknown)}" if unknown else "give app ids or --all")
    if a.command == "ci-result":
        sys.exit(ci_result(ids[0]) if len(ids) == 1 else "ci-result: give one app")
    if a.command in ("build", "run") and not any(package_source().glob("webForCore.Web.*.nupkg")):
        sys.exit(f"no webForCore {WEBFORCORE_VERSION} packages in {package_source().relative_to(ROOT)}: run scripts/wfc.py packages")
    for i in ids:
        app = apps[i]
        if a.command == "fetch":
            fetch(app, a.reset)
        elif a.command == "assess":
            assess(app)
        elif a.command == "build":
            build(app)
        elif a.command == "setup":
            setup(app)
        elif a.command == "smoke":
            smoke(app)
        elif a.command == "save-overlay":
            save_overlay(app)
        elif a.command == "run":
            if fetch(app, a.reset) == "pass" and build(app) == "pass":
                if app.get("setup"):
                    setup(app)  # a failed setup still smokes: the probes say what it broke
                smoke(app)


if __name__ == "__main__":
    main()

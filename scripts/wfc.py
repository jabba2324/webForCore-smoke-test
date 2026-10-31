#!/usr/bin/env python3
"""The webForCore smoke test bed: fetch open-source ASP.NET 4.x apps, assess them, build their
migrated projects against webForCore, run them on Kestrel and probe them.

Standard library only (Python 3.11+, for tomllib).

    scripts/wfc.py packages [--version 0.2.0]  # download webForCore's release packages into .packages/
    scripts/wfc.py list
    scripts/wfc.py fetch <id>... | --all [--reset]
    scripts/wfc.py assess <id>... | --all
    scripts/wfc.py build <id>...
    scripts/wfc.py smoke <id>...
    scripts/wfc.py run <id>... | --all        # fetch, build and smoke
    scripts/wfc.py save-overlay <id>         # the app checkout's changes -> apps/<id>/overlay.patch
    scripts/wfc.py report                    # apps/*/results.json -> RESULTS.md
"""

import argparse
import datetime
import json
import os
import re
import socket
import subprocess
import sys
import time
import tomllib
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
APPS = ROOT / "apps"
# Each app has its own folder, apps/<id>/:
#   app.toml        where it comes from, how to build it, what to probe
#   NOTES.md        the migration notes: assessment, every change and why, open problems
#   LOG.md          one line per stage run, appended by this script
#   overlay.patch   the migration, as a patch on the pinned commit
#   results.json    the latest result of each stage
#   logs/           the latest build and server logs
#   src/            the checkout (not committed)

WEBFORCORE = Path(os.environ.get("WEBFORCORE_DIR", ROOT.parent / "webForCore")).resolve()
# webForCore's NuGet packages come from its GitHub releases, not nuget.org. `packages` downloads a release here, and
# builds restore from it (apps reference the packages, docs/MIGRATING.md section 1). WEBFORCORE_DIR is only needed by
# apps still migrated against webForCore's source.
WEBFORCORE_REPO = "jabba2324/webForCore"
WEBFORCORE_VERSION = os.environ.get("WEBFORCORE_VERSION", "0.2.0")
PACKAGES = ROOT / ".packages"

# Phase 0 of webForCore's docs/AGENT-MIGRATION.md.
BLOCKERS = [
    ("windows-auth", r'mode="Windows"|WindowsIdentity|WindowsPrincipal|<identity\s+impersonate'),
    ("wcf-hosting", r"ServiceHost\b|<services>"),
    ("linq-to-sql", r"System\.Data\.Linq|LinqDataSource"),
    ("ef-objectcontext", r"\bObjectContext\b|EntityDataSource"),
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
    d = app_dir(app_id) / "logs"
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
    for attempt in range(3):  # release asset downloads fail with 503 now and then
        r = subprocess.run(["gh", "release", "download", f"v{WEBFORCORE_VERSION}", "--repo", WEBFORCORE_REPO,
                            "--pattern", "*.nupkg", "--dir", str(dest), "--skip-existing"], capture_output=True, text=True)
        if r.returncode == 0:
            break
        time.sleep(5)
    count = len(list(dest.glob("*.nupkg")))
    print(f"{dest.relative_to(ROOT)}: {count} packages" + (f" (last error: {r.stderr.strip()[:200]})" if r.returncode else ""))
    return r.returncode == 0


def webforcore_used(proj):
    """Which webForCore the project builds against, for the record."""
    if "$(WEBFORCORE_DIR)" in proj.read_text(errors="replace"):
        commit = git("rev-parse", "--short=12", "HEAD", cwd=WEBFORCORE, check=False).stdout.strip()
        return f"source {commit}"
    return f"packages {WEBFORCORE_VERSION}"


def now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat(timespec="seconds")


def scrub(log):
    """Logs are committed, so this machine's paths become placeholders."""
    text = log.read_text(errors="replace")
    # Longest first: the repo's path can start with webForCore's (../webForCore-smoke-test).
    pairs = ((str(WEBFORCORE), "$WEBFORCORE_DIR"), (str(ROOT), "<repo>"), (str(Path.home()), "~"))
    for path, name in sorted(pairs, key=lambda p: -len(p[0])):
        text = text.replace(path, name)
    log.write_text(text)
    return text


def load_result(app_id):
    p = app_dir(app_id) / "results.json"
    return json.loads(p.read_text()) if p.exists() else {"id": app_id, "stages": {}}


def record(app_id, stage, status, **details):
    """status: pass | fail | blocked | skip"""
    r = load_result(app_id)
    r["stages"][stage] = {"status": status, "at": now(), **details}
    (app_dir(app_id) / "results.json").write_text(json.dumps(r, indent=2, sort_keys=True) + "\n")
    line = f"- {now()} **{stage}**: {status}" + (f" ({details['summary']})" if details.get("summary") else "")
    if details.get("commit"):
        line += f" at `{details['commit'][:12]}`"
    log = app_dir(app_id) / "LOG.md"
    if not log.exists():
        log.write_text(f"# {app_id}: run log\n\nAppended by `scripts/wfc.py`, one line per stage run.\n\n")
    with log.open("a") as f:
        f.write(line + "\n")
    print(f"[{app_id}] {stage}: {status}" + (f" ({details.get('summary')})" if details.get("summary") else ""))
    return status


def git(*args, cwd=None, check=True):
    return subprocess.run(["git", *args], cwd=cwd, check=check, capture_output=True, text=True)


def checkout(app):
    return app_dir(app["id"]) / "src"


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
                 ":(exclude,glob)**/logs/W3SVC*/**", ":(exclude,glob)**/logs/*/W3SVC*/**"] + [f":(exclude,glob){p}" for p in app.get("remove", [])]
    git("reset", "-q", cwd=dest, check=False)  # drops intent-to-add entries from an earlier save
    # Deletions must stay unstaged, or they drop out of the diff.
    git("add", "-N", "--ignore-removal", "--", ".", *generated, cwd=dest)
    # As bytes: the apps' files with CRLF line endings must keep them, or the patch no longer applies to the checkout.
    diff = subprocess.run(["git", "diff", "--binary", "--", ".", *generated], cwd=dest, check=True, capture_output=True).stdout
    p = app_dir(app["id"]) / "overlay.patch"
    if diff:
        p.write_bytes(diff)
        print(f"wrote {p.relative_to(ROOT)} ({diff.count(b'\n')} lines)")
    else:
        print("no changes")


# --- assess --------------------------------------------------------------------------------------

def referenced_projects(proj, seen=None):
    """The folders of the projects a project references, transitively (a class library's blockers block the site)."""
    seen = seen if seen is not None else set()
    text = proj.read_text(errors="replace") if proj.exists() else ""
    netfx = proj.with_name(proj.name + ".netfx")  # the original project, if already converted
    text += netfx.read_text(errors="replace") if netfx.exists() else ""
    for ref in re.findall(r'<ProjectReference\s+Include="([^"$]+)"', text):
        target = (proj.parent / ref.replace("\\", "/")).resolve()
        if target.exists() and target not in seen:
            seen.add(target)
            referenced_projects(target, seen)
    return seen


def assess(app):
    root = site_dir(app)
    if not root.exists():
        return record(app["id"], "assess", "fail", summary=f"no folder {root.relative_to(ROOT)}")
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
        r = subprocess.run(["dotnet", "build", str(proj), "-c", "Release", "-nologo", "-clp:ErrorsOnly;Summary",
                            f"-p:RestoreAdditionalProjectSources={package_source()}"],
                           cwd=proj.parent, env=env(), stdout=f, stderr=subprocess.STDOUT)
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


def get(url, timeout):
    req = urllib.request.Request(url, headers={"User-Agent": "webForCore-smoke-test"})
    try:
        with OPENER.open(req, timeout=timeout) as r:
            return r.status, dict(r.headers), r.read(200_000).decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, dict(e.headers), e.read(200_000).decode("utf-8", "replace")


def smoke(app):
    proj = project_file(app)
    if not proj or not proj.exists():
        return record(app["id"], "smoke", "skip", summary="no project")
    site = proj.parent
    dll = site / "bin_dotnet" / f"{assembly_name(proj)}.dll"
    if not dll.exists():
        return record(app["id"], "smoke", "skip", summary=f"not built ({dll.relative_to(ROOT)} missing)")
    port = free_port()
    base = f"{'https' if app.get('https') else 'http'}://127.0.0.1:{port}"
    log = logs_dir(app["id"]) / "server.log"
    e = env()
    e.update(app.get("env", {}))
    with log.open("w") as f:
        server = subprocess.Popen(["dotnet", str(dll.relative_to(site)), "--urls", base], cwd=site, env=e,
                                  stdout=f, stderr=subprocess.STDOUT)
    checks = []
    try:
        deadline = time.time() + 60
        while time.time() < deadline and server.poll() is None:
            try:
                socket.create_connection(("127.0.0.1", port), timeout=1).close()
                break
            except OSError:
                time.sleep(0.5)
        if server.poll() is not None:
            return record(app["id"], "smoke", "fail", summary=f"server exited with {server.returncode}",
                          log_tail=scrub(log)[-3000:])
        probes = app.get("probes") or [{"path": "/", "status": 200}]
        for i, probe in enumerate(probes):
            # The first request compiles the site, as on IIS.
            checks.append(run_probe(base, probe, timeout=120 if i == 0 else 30))
        # AddWebForms() writes W3C logs under the site folder by default (FINDINGS.md); they must not be served.
        # The log is flushed every second, and entries not yet flushed are lost when the server stops.
        time.sleep(1.5)
        w3c = sorted((site / "logs").glob("W3SVC*/*.txt"))
        w3c = [f"/{w3c[0].relative_to(site).as_posix()}"] if w3c else []
        # An app whose error handler answers 200 with its own "not found" page sets [forbidden."<path>"] in app.toml,
        # e.g. { status = 200, absent = "<package " }: what matters is that the file's content isn't served.
        overrides = app.get("forbidden", {})
        for path in FORBIDDEN + [f"/bin_dotnet/{dll.name}"]:
            probe = {"path": path, "status": 404, "forbidden": True, **overrides.get(path, {})}
            checks.append(run_probe(base, probe, timeout=30))
        for path in w3c:  # an app that answers every URL itself (OWIN) may return 200: what matters is the log's content
            checks.append(run_probe(base, {"path": path, "status": 404, "forbidden": True, "absent": "#Fields:"}, timeout=30))
    finally:
        server.terminate()
        try:
            server.wait(10)
        except subprocess.TimeoutExpired:
            server.kill()
        scrub(log)
    failed = [c for c in checks if not c["ok"]]
    return record(app["id"], "smoke", "fail" if failed else "pass", checks=checks,
                  summary=f"{len(checks) - len(failed)}/{len(checks)} probes",
                  log=str(log.relative_to(ROOT)))


def run_probe(base, probe, timeout):
    path = probe["path"]
    t = time.time()
    try:
        status, headers, body = get(base + path, timeout)
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


# --- report --------------------------------------------------------------------------------------

STAGES = ["fetch", "assess", "build", "smoke"]
ICON = {"pass": "pass", "fail": "**fail**", "blocked": "blocked", "skip": "–", None: ""}


def report(apps):
    rows = []
    for app in apps.values():
        r = load_result(app["id"])
        s = r["stages"]
        cells = [ICON.get(s.get(st, {}).get("status")) for st in STAGES]
        smoke_summary = s.get("smoke", {}).get("summary", "")
        kinds = ", ".join(s.get("assess", {}).get("kinds", app.get("kinds", [])))
        rows.append(f"| [{app['name']}](apps/{app['id']}/NOTES.md) | {kinds} | " + " | ".join(cells) + f" | {smoke_summary} | {app.get('status', '')} |")
    out = ["# Results", "", f"Generated by `scripts/wfc.py report` on {now()[:10]}. Each app's notes, log and results are in `apps/<id>/`.", "",
           "| App | Kinds | Fetch | Assess | Build | Smoke | Probes | Catalog status |",
           "| --- | --- | --- | --- | --- | --- | --- | --- |", *rows, ""]
    (ROOT / "RESULTS.md").write_text("\n".join(out))
    print(f"wrote RESULTS.md ({len(rows)} apps)")


# --- main ----------------------------------------------------------------------------------------

def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("command", choices=["packages", "list", "fetch", "assess", "build", "smoke", "run", "save-overlay", "report"])
    p.add_argument("ids", nargs="*")
    p.add_argument("--all", action="store_true")
    p.add_argument("--reset", action="store_true", help="fetch: discard the checkout's changes and reapply the overlay")
    p.add_argument("--version", help="packages: the webForCore release (default: $WEBFORCORE_VERSION or 0.2.0)")
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
    ids = list(apps) if a.all else a.ids
    unknown = [i for i in ids if i not in apps]
    if unknown or not ids:
        sys.exit(f"unknown app(s): {', '.join(unknown)}" if unknown else "give app ids or --all")
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
        elif a.command == "smoke":
            smoke(app)
        elif a.command == "save-overlay":
            save_overlay(app)
        elif a.command == "run":
            if fetch(app) == "pass" and build(app) == "pass":
                smoke(app)


if __name__ == "__main__":
    main()

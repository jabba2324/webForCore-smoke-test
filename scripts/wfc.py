#!/usr/bin/env python3
"""The webForCore smoke test bed: fetch open-source ASP.NET 4.x apps, assess them, build their
migrated projects against webForCore, run them on Kestrel and probe them.

Standard library only (Python 3.11+, for tomllib).

    scripts/wfc.py list
    scripts/wfc.py fetch <id>... | --all [--reset]
    scripts/wfc.py assess <id>... | --all
    scripts/wfc.py build <id>...
    scripts/wfc.py smoke <id>...
    scripts/wfc.py run <id>... | --all        # fetch, build and smoke
    scripts/wfc.py save-overlay <id>         # the app checkout's changes -> overlays/<id>.patch
    scripts/wfc.py report                    # results/*.json -> RESULTS.md
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
CATALOG = ROOT / "catalog" / "apps.toml"
APPS = ROOT / "apps"          # checkouts (ignored by git)
LOGS = ROOT / "logs"          # build and server logs (ignored by git)
OVERLAYS = ROOT / "overlays"  # the migration of each app, as a patch on its pinned commit
RESULTS = ROOT / "results"

WEBFORCORE = Path(os.environ.get("WEBFORCORE_DIR", ROOT.parent / "webForCore")).resolve()

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
    with CATALOG.open("rb") as f:
        apps = tomllib.load(f)["app"]
    return {a["id"]: a for a in apps}


def env():
    e = dict(os.environ)
    e.setdefault("OPENSSL_ENABLE_SHA1_SIGNATURES", "1")  # Fedora/RHEL
    e.setdefault("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
    e.setdefault("DOTNET_NOLOGO", "1")
    e["WEBFORCORE_DIR"] = str(WEBFORCORE)  # migrated projects import $(WEBFORCORE_DIR)/src/...
    return e


def now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat(timespec="seconds")


def load_result(app_id):
    p = RESULTS / f"{app_id}.json"
    return json.loads(p.read_text()) if p.exists() else {"id": app_id, "stages": {}}


def record(app_id, stage, status, **details):
    """status: pass | fail | blocked | skip"""
    r = load_result(app_id)
    r["stages"][stage] = {"status": status, "at": now(), **details}
    RESULTS.mkdir(exist_ok=True)
    (RESULTS / f"{app_id}.json").write_text(json.dumps(r, indent=2, sort_keys=True) + "\n")
    print(f"[{app_id}] {stage}: {status}" + (f" ({details.get('summary')})" if details.get("summary") else ""))
    return status


def git(*args, cwd=None, check=True):
    return subprocess.run(["git", *args], cwd=cwd, check=check, capture_output=True, text=True)


def checkout(app):
    return APPS / app["id"]


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
        APPS.mkdir(exist_ok=True)
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
    commit = git("rev-parse", "HEAD", cwd=dest).stdout.strip()
    patch = OVERLAYS / f"{app['id']}.patch"
    if patch.exists():
        r = git("apply", "--whitespace=nowarn", str(patch), cwd=dest, check=False)
        if r.returncode:
            return record(app["id"], "fetch", "fail", commit=commit, summary="overlay does not apply", error=r.stderr[-2000:])
    return record(app["id"], "fetch", "pass", commit=commit, overlay=patch.exists())


def save_overlay(app):
    dest = checkout(app)
    git("add", "-A", "-N", ".", cwd=dest)
    diff = git("diff", "--binary", cwd=dest).stdout
    OVERLAYS.mkdir(exist_ok=True)
    p = OVERLAYS / f"{app['id']}.patch"
    if diff:
        p.write_text(diff)
        print(f"wrote {p.relative_to(ROOT)} ({diff.count(chr(10))} lines)")
    else:
        print("no changes")


# --- assess --------------------------------------------------------------------------------------

def assess(app):
    root = site_dir(app)
    if not root.exists():
        return record(app["id"], "assess", "fail", summary=f"no folder {root.relative_to(ROOT)}")
    hits = {name: [] for name, _ in BLOCKERS}
    kinds = set()
    target_frameworks = set()
    refs_text = []
    for p in root.rglob("*"):
        if not p.is_file() or any(part in {"bin", "obj", "bin_dotnet", "packages", "node_modules", ".git"} for part in p.parts):
            continue
        rel = str(p.relative_to(root))
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
            for name, pattern in BLOCKERS:
                for m in re.finditer(pattern, text):
                    line = text.count("\n", 0, m.start()) + 1
                    hits[name].append(f"{rel}:{line}")
    joined = "\n".join(refs_text)
    for kind, pattern in KINDS.items():
        if kind != "webforms" and re.search(pattern, joined):
            kinds.add(kind)
    hits = {k: v[:20] + ([f"... {len(v) - 20} more"] if len(v) > 20 else []) for k, v in hits.items() if v}
    status = "blocked" if hits else "pass"
    return record(app["id"], "assess", status, kinds=sorted(kinds), target_frameworks=sorted(target_frameworks),
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
    LOGS.mkdir(exist_ok=True)
    log = LOGS / f"{app['id']}-build.log"
    t = time.time()
    with log.open("w") as f:
        r = subprocess.run(["dotnet", "build", str(proj), "-c", "Release", "-nologo", "-clp:ErrorsOnly;Summary"],
                           cwd=proj.parent, env=env(), stdout=f, stderr=subprocess.STDOUT)
    text = log.read_text(errors="replace")
    errors = sorted(set(re.findall(r"error [A-Z]+\d+: [^\[\n]+", text)))
    return record(app["id"], "build", "pass" if r.returncode == 0 else "fail", seconds=round(time.time() - t),
                  errors=errors[:30], error_count=len(errors), log=str(log.relative_to(ROOT)),
                  summary=None if r.returncode == 0 else f"{len(errors)} distinct errors")


# --- smoke ---------------------------------------------------------------------------------------

def free_port():
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k):
        return None


OPENER = urllib.request.build_opener(NoRedirect)


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
    base = f"http://127.0.0.1:{port}"
    LOGS.mkdir(exist_ok=True)
    log = LOGS / f"{app['id']}-server.log"
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
                          log_tail=log.read_text(errors="replace")[-3000:])
        probes = app.get("probes") or [{"path": "/", "status": 200}]
        for i, probe in enumerate(probes):
            # The first request compiles the site, as on IIS.
            checks.append(run_probe(base, probe, timeout=120 if i == 0 else 30))
        for path in FORBIDDEN + [f"/bin_dotnet/{dll.name}"]:
            checks.append(run_probe(base, {"path": path, "status": 404, "forbidden": True}, timeout=30))
    finally:
        server.terminate()
        try:
            server.wait(10)
        except subprocess.TimeoutExpired:
            server.kill()
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
    ok = status == probe.get("status", 200) or (probe.get("forbidden") and status in (403, 404))
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
        rows.append(f"| [{app['name']}]({app['repo']}) | {kinds} | " + " | ".join(cells) + f" | {smoke_summary} | {app.get('status', '')} |")
    out = ["# Results", "", f"Generated by `scripts/wfc.py report` on {now()[:10]}. Details are in `results/<id>.json`.", "",
           "| App | Kinds | Fetch | Assess | Build | Smoke | Probes | Catalog status |",
           "| --- | --- | --- | --- | --- | --- | --- | --- |", *rows, ""]
    (ROOT / "RESULTS.md").write_text("\n".join(out))
    print(f"wrote RESULTS.md ({len(rows)} apps)")


# --- main ----------------------------------------------------------------------------------------

def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("command", choices=["list", "fetch", "assess", "build", "smoke", "run", "save-overlay", "report"])
    p.add_argument("ids", nargs="*")
    p.add_argument("--all", action="store_true")
    p.add_argument("--reset", action="store_true", help="fetch: discard the checkout's changes and reapply the overlay")
    a = p.parse_args()
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
    if not (WEBFORCORE / "WebFormsForCore.slnx").exists() and a.command in ("build", "run"):
        sys.exit(f"webForCore not found at {WEBFORCORE}; set WEBFORCORE_DIR")
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

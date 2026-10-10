#!/usr/bin/env python3
"""Static Unity project integrity checks that need no Unity license.

Checks (on git-tracked files only, so local caches cannot mask or cause failures):
  1. every tracked file/folder under Assets/ has a tracked .meta, and every .meta has an asset;
  2. every .meta has a well-formed, project-unique guid;
  3. no merge-conflict markers in text assets (.cs .meta .unity .asset .prefab .mat .shader .json .asmdef);
  4. scenes/prefabs/assets are Unity YAML (header present) and every script GUID they
     reference (m_Script) resolves to a tracked .cs.meta/.dll.meta guid, or a known Unity built-in;
  5. Packages/manifest.json and packages-lock.json parse as JSON; ProjectVersion.txt present.
Exit 0 = pass, 1 = failures (all listed), never silently skips.
"""
import json, os, re, subprocess, sys

root = subprocess.check_output(["git", "rev-parse", "--show-toplevel"], text=True).strip()
os.chdir(root)
tracked = subprocess.check_output(["git", "ls-files", "-z"]).decode("utf-8", "surrogateescape").split("\0")
tracked = [t for t in tracked if t]
tset = set(tracked)
errors = []

def ignored(path):
    # Unity ignores hidden files/folders, names ending in ~, and cvs/.tmp
    for part in path.split("/"):
        if part.startswith(".") or part.endswith("~") or part.lower() == "cvs" or part.endswith(".tmp"):
            return True
    return False

assets = [t for t in tracked if t.startswith("Assets/") and not ignored(t)]
if not assets:
    errors.append("no tracked files under Assets/ (is this a Unity project?)")

# 1. meta pairing
dirs = set()
for t in assets:
    parts = t.split("/")
    for i in range(1, len(parts)):
        dirs.add("/".join(parts[:i]))
dirs.discard("Assets")
for d in sorted(dirs):
    if ignored(d):
        continue
    if d + ".meta" not in tset:
        errors.append(f"folder without tracked .meta: {d}/")
for t in assets:
    if t.endswith(".meta"):
        base = t[:-5]
        if base not in tset and base not in dirs:
            errors.append(f"orphan .meta (asset not tracked): {t}")
    else:
        if t + ".meta" not in tset:
            errors.append(f"asset without tracked .meta: {t}")

# 2. guids
guid_re = re.compile(r"^guid: ([0-9a-f]{32})\s*$", re.M)
guids = {}
for t in assets:
    if not t.endswith(".meta"):
        continue
    try:
        txt = open(t, encoding="utf-8", errors="replace").read()
    except OSError as e:
        errors.append(f"cannot read {t}: {e}"); continue
    m = guid_re.search(txt)
    if not m:
        errors.append(f"missing/malformed guid: {t}"); continue
    g = m.group(1)
    if g in guids:
        errors.append(f"duplicate guid {g}: {t} and {guids[g]}")
    guids[g] = t

# 3. conflict markers
TEXT_EXT = (".cs", ".meta", ".unity", ".asset", ".prefab", ".mat", ".shader", ".json", ".asmdef", ".md", ".uxml", ".uss")
conflict = re.compile(r"^(<{7}( |$)|={7}$|>{7}( |$))", re.M)
for t in tracked:
    if t.lower().endswith(TEXT_EXT) and os.path.isfile(t):
        data = open(t, encoding="utf-8", errors="replace").read()
        if conflict.search(data):
            errors.append(f"merge-conflict marker in {t}")

# 4. YAML header + script references
BUILTIN_PREFIX = ("0000000000000000",)  # built-in resources: 0000000000000000e000000000000000 / f000...
script_re = re.compile(r"m_Script: \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: (\d)\}")
yaml_ext = (".unity", ".prefab", ".asset", ".mat", ".controller", ".anim")
for t in assets:
    if not t.lower().endswith(yaml_ext) or not os.path.isfile(t):
        continue
    head = open(t, "rb").read(200)
    if not head.startswith(b"%YAML"):
        # binary-serialised or text-less assets are allowed only if explicitly Unity binary
        errors.append(f"not Unity YAML (no %YAML header; force-text serialization expected): {t}")
        continue
    txt = open(t, encoding="utf-8", errors="replace").read()
    for fid, g, typ in script_re.findall(txt):
        if g.startswith(BUILTIN_PREFIX):
            continue
        if g not in guids and not (typ == "3" and fid == "0" and g.startswith("00000000")):
            # Packages (e.g. URP, Input System) are not tracked in Assets/: accept package-origin guids
            # only via the explicit allow-list file, never silently.
            allow = "scripts/unity-external-guids.txt"
            ext = set()
            if os.path.isfile(allow):
                ext = {l.split("#")[0].strip() for l in open(allow) if l.split("#")[0].strip()}
            if g not in ext:
                errors.append(f"{t}: m_Script guid {g} matches no tracked script .meta (missing script?) ")

# 5. project files
for f in ("Packages/manifest.json", "Packages/packages-lock.json"):
    try:
        json.load(open(f, encoding="utf-8-sig"))
    except Exception as e:
        errors.append(f"{f}: {e}")
if not os.path.isfile("ProjectSettings/ProjectVersion.txt"):
    errors.append("ProjectSettings/ProjectVersion.txt missing")

if errors:
    seen = []
    for e in errors:
        if e not in seen:
            seen.append(e)
    print(f"unity-integrity: FAILED, {len(seen)} problem(s):")
    for e in seen[:200]:
        print("  -", e)
    if len(seen) > 200:
        print(f"  ... and {len(seen) - 200} more")
    sys.exit(1)
print(f"unity-integrity: OK ({len(assets)} tracked asset paths, {len(guids)} guids, {len(dirs)} folders)")

#!/usr/bin/env bash
# Verification gate: Rummikub-Master-Unity (Unity C# game): Unity project hygiene (.meta/GUID/YAML integrity) + C# syntax parse; no Unity license needed
#
# NOT CHECKED (do not claim these from a green run): the game does NOT get compiled in Unity (the C# check is syntax-only; semantic errors from missing UnityEngine are ignored by design), no EditMode/PlayMode tests exist or run (baseline 0), no player build, no gameplay. Package-script GUIDs in scripts/unity-external-guids.txt are assumed, not verified.
#
# Usage: scripts/verify.sh   (exit 0 = pass). Non-blocking steps print "WARN" and never fail the run.
set -euo pipefail
cd "$(dirname "$0")/.."

step() { printf '\n== %s\n' "$*"; }
need() { command -v "$1" >/dev/null 2>&1 || { echo "verify: required tool '$1' not found" >&2; exit 2; }; }
# warn_step "name" cmd...: run a check that fails today for pre-existing reasons; report but do not block.
warn_step() { local n="$1"; shift; step "$n (non-blocking)"; if ! "$@"; then echo "WARN: '$n' failed (pre-existing, non-blocking)"; fi; }
need git; need bash

step "shell syntax"
for f in scripts/*.sh; do bash -n "$f"; done

step "workflow lint (actionlint)"
if command -v actionlint >/dev/null 2>&1; then actionlint
elif command -v pipx >/dev/null 2>&1; then pipx run --spec actionlint-py==1.7.12.25 actionlint
else echo "verify: actionlint (or pipx) required" >&2; exit 2; fi

need python3; need dotnet
step "unity project integrity"
python3 scripts/check_unity_integrity.py
step "C# syntax parse (Assets/Scripts)"
scripts/check_csharp_syntax.sh Assets/Scripts

printf '\nPASS: Unity project integrity + C# syntax parse + actionlint + shell syntax.\n'
printf 'NOT CHECKED: the game does NOT get compiled in Unity (the C# check is syntax-only; semantic errors from missing UnityEngine are ignored by design), no EditMode/PlayMode tests exist or run (baseline 0), no player build, no gameplay. Package-script GUIDs in scripts/unity-external-guids.txt are assumed, not verified.\n'

# AGENTS.md

Instructions for AI coding agents and contributors.

## Verification gate (required before claiming anything works)

Run exactly this and read the output (bash, python3, .NET 9 SDK):

```
scripts/verify.sh
```

Exit code 0 = pass; anything else = not done. CI (`.github/workflows/gate.yml`) runs the same script (job `verify`) plus `scripts/check-test-integrity.sh` (job `test-integrity`). What it runs: Unity project integrity (scripts/check_unity_integrity.py: .meta pairing, unique GUIDs, conflict markers, YAML scenes/prefabs, script GUID references vs allow-list scripts/unity-external-guids.txt), C# syntax-only parse of Assets/Scripts (scripts/check_csharp_syntax.sh), actionlint

**Not checked by the gate:** the game does NOT get compiled in Unity (the C# check is syntax-only; semantic errors from missing UnityEngine are ignored by design), no EditMode/PlayMode tests exist or run (baseline 0), no player build, no gameplay. Package-script GUIDs in scripts/unity-external-guids.txt are assumed, not verified.

Rules: never delete tests, add skip/ignore/xfail markers, remove assertions or loosen expected values to get green. `test-integrity` also fails if the number of test cases drops below `scripts/test-baseline.txt`; only the owner may approve a test change (PR label `test-change-approved`; refresh the baseline with `scripts/check-test-integrity.sh --update-baseline`). Do not edit `.github/`, `scripts/verify.sh` or `scripts/check-test-integrity.sh` to make a failing check pass; they are owned by @grloper (CODEOWNERS).

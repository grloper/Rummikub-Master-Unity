# Verification record — 2026-10-01

Inspected baseline: `ebbd63471388abe447ded4a268d1c3c9f2572882`.

| Check | Result | Limit |
|---|---|---|
| Node rule tests | 7 pass, including 400 seeded racks and 50 two-player simulations | Pure engine; crowded-board fixture explicitly synthetic |
| C# core harness | 121 assertions pass against linked production files | Unity adapters, not editor or AI coroutine |
| Local Chrome `/play/` | Computer-mode deal; sort; rejected empty confirm; draw/pass and computer response; undo; drag/drop of an actual dealt orange 9, 10 and joker, then successful 30-point opening commit | One browser, random deal; no full-game completion claimed |
| Unity 6000.4.0f1 batch import | Blocked: no valid Unity Editor license; missing headless entitlement | No activation or credential changes; no scene compile/play claim |
| PeerJS rooms | Source inspected | No multi-device, reconnect or adversarial peer test |

Commands: `node --test tests/browser-rules.test.cjs` and `dotnet run --project tests/core/CoreChecks.csproj`.

## Reproduced defects

1. AI opening meld could become permanent below 30 points when only one planned set fit. It now uses the human commit evaluator before becoming permanent.
2. Duplicate rank interrupted greedy run extraction; it is skipped for that run and preserved in the rack.
3. Duplicate physical IDs in separate valid sets were accepted and unknown IDs could throw. Board dimensions and physical IDs are checked before set evaluation.
4. C# set probes inserted an already-present card then removed it from the membership HashSet, losing the original card's membership. They reject duplicates without mutation.

## Assessment

Worth keeping as a game/data-structure project. Unity's layered opponent and the browser's greedy opponent are separate artifacts. The browser adaptation is usable locally; the editor product remains unverified here. Do not label this an optimal AI, fully validated rule implementation, universally O(1), or proven online multiplayer.

`images/browser-verified.png` captures actual local Chrome gameplay. Historical Unity screenshots were not regenerated or treated as runtime evidence.

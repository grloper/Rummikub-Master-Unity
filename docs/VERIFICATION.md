# Verification record — 2026-10-01

Inspected baseline: `ebbd63471388abe447ded4a268d1c3c9f2572882`.

| Check | Result | Limit |
|---|---|---|
| Node rule tests | 7 pass, including 400 seeded racks and 50 two-player simulations | Pure engine; crowded-board fixture explicitly synthetic |
| C# core harness | Initial 121 assertions expanded to 902,630 passing assertions, including 3,000 seeded list operations and CardsSet combine/split | Unity adapters, not editor, Unity object lifecycle or AI coroutine |
| Local Chrome `/play/` | Computer-mode deal; sort; rejected empty confirm; draw/pass and computer response; undo; drag/drop of an actual dealt orange 9, 10 and joker, then successful 30-point opening commit | One browser, random deal; no full-game completion claimed |
| Unity 6000.4.0f1 batch import | Blocked: no valid Unity Editor license; missing headless entitlement | No activation or credential changes; no scene compile/play claim |
| PeerJS rooms | Source inspected | No multi-device, reconnect or adversarial peer test |
| Structure lab in Chrome | Short-set rejection; legal 30-point opening commit; mixed-set rejection; undo preserving committed board; reset and keyboard Enter; no page overflow at 320/390/768/1440px, no page errors | Synthetic fixture executes browser rules; Unity nodes explicitly source-based illustration |
| Optional Release list measurements | Seven-trial medians for 1,000/10,000/100,000 integer nodes captured in `structure-measurements.json` | .NET linked source, not Unity or game latency; methodology/noise documented in `DATA-STRUCTURES.md` |

Commands: `node --test tests/browser-rules.test.cjs` and `dotnet run --project tests/core/CoreChecks.csproj`.

## Reproduced defects

1. AI opening meld could become permanent below 30 points when only one planned set fit. It now uses the human commit evaluator before becoming permanent.
2. Duplicate rank interrupted greedy run extraction; it is skipped for that run and preserved in the rack.
3. Duplicate physical IDs in separate valid sets were accepted and unknown IDs could throw. Board dimensions and physical IDs are checked before set evaluation.
4. C# set probes inserted an already-present card then removed it from the membership HashSet, losing the original card's membership. They reject duplicates without mutation.
5. List membership disappeared when removing one duplicate value; the index now counts multiplicity, including null values. The pre-change list fails this regression.
6. Append left donor and receiver sharing mutable nodes. It now moves nodes, transfers ownership and empties the reusable donor. Self append and foreign/detached node removal cannot corrupt the list.

## Assessment

Worth keeping as a game/data-structure project. Unity's layered opponent and the browser's greedy opponent are separate artifacts. The browser adaptation is usable locally; the editor product remains unverified here. Do not label this an optimal AI, fully validated rule implementation, universally O(1), or proven online multiplayer.

`images/browser-verified.png` captures actual local Chrome gameplay. Historical Unity screenshots were not regenerated or treated as runtime evidence.

`images/structure-lab-desktop.png` and `images/structure-lab-mobile.png` capture the actual interactive synthetic fixture. Native keyboard checks are recorded in `tests/browser-lab-flow.js`, a Playwright CLI `run-code` callback intended to run from this repository after opening `/play/lab.html`. The source-based diagram remains an illustration.

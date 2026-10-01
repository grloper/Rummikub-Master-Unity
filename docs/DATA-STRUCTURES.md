# Actual data structures and operation costs

This describes inspected Unity C# implementation, not the separate JavaScript browser engine. `tests/core` links the actual production files with minimal Unity adapters; these checks do not run a Unity scene.

## Linked set with membership index

`DoublyLinkedList<T>` stores ordered node handles, head/tail/count, and a hash dictionary of value multiplicities (plus a null count). Equal values can occur more than once. Removing one occurrence preserves membership until the final occurrence is removed. Hash/equality of values must remain stable while stored. The linked harness verifies distinct physical `Card` objects with identical number/color remain separate. Actual `Card` inherits Unity `MonoBehaviour`, so Unity object equality, destruction and lifecycle behavior remain unverified by these minimal adapters.

| Operation | Cost and conditions |
| --- | --- |
| Read head, tail, count; follow known next/previous link | O(1) |
| Add at either end | Expected amortized O(1), including hash-index maintenance; resize/collision effects prevent a blanket worst-case O(1) claim |
| Contains(value) | Expected O(1); worst-case O(n) hash collisions, excluding user-defined hash/equality cost |
| Remove known owned node, first or last | O(1) pointer work and expected O(1) hash-index work |
| Reject foreign, detached or null node removal | O(1) ownership check; leaves both lists unchanged |
| Append m nodes from another list | Expected O(m): O(1) pointer splice, O(m) owner/index maintenance and source cleanup |
| Self append | O(1) harmless no-op, preventing a cycle |
| Enumerate/copy n nodes | O(n) |
| Find a card by walking nodes (`CardsSet.RemoveCard`) | O(n) search, followed by expected O(1) removal |
| Validate an ordered run/group | Traverses set: O(n); game rule caps bound valid set sizes but do not make arbitrary generic lists constant-size |

Append is a **move**, not an alias: the donor becomes empty and reusable. Existing moved node handles become owned by the receiver. Previously both list objects shared mutable nodes, allowing donor mutation to corrupt receiver traversal and count. Public node values are immutable; public links expose traversal without public setters. Internal link setters are for list maintenance, and code within the same assembly must respect that invariant.

The game caller `GameBoard.CombineSets` updates the donor's board positions before moving its nodes and removes the consumed donor board entry afterward. No other production append caller was found. No production caller assigns node values or topology.

## Other structures and limits

`PlayerHand` uses color/rank buckets. Choosing a bucket is constant index arithmetic; removal in a bucket depends on its actual container and lookup implementation. Deck and AI candidate search costs must be evaluated separately; an O(1) linked-node edit cannot establish O(1) move planning or a whole O(1) game. The separate browser implementation uses arrays and scans rather than these Unity lists.

## Execution evidence

The production-linked C# suite covers repeated equal and null values, foreign/detached handles, self append, donor reuse, transferred ownership, and 3,000 seeded operations compared to a standard list. Every step checks order, count, all eight membership keys, reverse traversal and bidirectional links. This establishes tested correctness of these operations, not a latency benchmark or a Unity runtime claim.

## Optional measured observations

Run `dotnet run --project tests/core/CoreChecks.csproj -c Release -- --measure`. The separate measurement mode links the same production list with integer keys; it does not execute Unity. [Raw observations](structure-measurements.json) record runtime, timestamp, trial count and methodology. On this machine with .NET 9.0.14:

| Nodes | Median Contains (ns/query) | Median full Append (ms) |
| --- | ---: | ---: |
| 1,000 | 9.050 | 0.0535 |
| 10,000 | 8.260 | 0.3916 |
| 100,000 | 9.399 | 3.5029 |

Seven trials used 200,000 membership queries each after warmup. Lookup includes loop/modulo/hash work. Append excludes donor construction, but includes receiver index allocation and ownership transfer. Lookup stayed approximately flat in this run, while append grew with the transferred node count. Those observations support the explanation; they do not prove asymptotic bounds, universal latency or Unity/game performance. JIT, GC, scheduling, key equality and hash collisions can change results.

## Browser visualization checks

`/play/lab.html` uses a labeled synthetic rack and actual browser rules to show short-set rejection, a 30-point opening meld, commit, mixed-color rejection and undo preserving the committed board. Native button keyboard activation passed. Browser checks at 320, 390, 768 and 1440 pixels found no page horizontal overflow and no JavaScript page errors. The linked nodes are explicitly a source-based Unity illustration, not the JavaScript engine's storage or a Unity capture.

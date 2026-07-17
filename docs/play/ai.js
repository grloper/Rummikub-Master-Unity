/* Rummikub — rule-based computer opponent (deterministic, no search tree). */
(function (root) {
  "use strict";
  var RK = root.RK;

  // Group a rack into candidate valid sets. Greedy: runs first (colour-sorted), then groups,
  // with jokers used to bridge single gaps or complete groups.
  function findSets(rackTiles) {
    var used = {};
    var sets = [];
    var jokers = rackTiles.filter(function (t) { return t.joker; });
    var jokerIdx = 0;

    // Runs: per colour, ascending, collect consecutive; one joker may bridge a gap of 1.
    RK.COLORS.forEach(function (color) {
      var pool = rackTiles.filter(function (t) { return !t.joker && t.color === color && !used[t.id]; })
        .sort(function (a, b) { return a.number - b.number; });
      var i = 0;
      while (i < pool.length) {
        var run = [pool[i]];
        var j = i + 1;
        while (j < pool.length) {
          var prev = run[run.length - 1].number;
          if (pool[j].number === prev + 1) { run.push(pool[j]); j++; }
          else if (pool[j].number === prev + 2 && jokerIdx < jokers.length) {
            run.push(jokers[jokerIdx]); jokerIdx++; run.push(pool[j]); j++;
          } else break;
        }
        if (run.length >= 3) {
          run.forEach(function (t) { used[t.id] = true; });
          sets.push(run);
        }
        i = (run.length >= 3) ? j : i + 1;
      }
    });

    // Groups: per number, distinct colours.
    for (var n = 1; n <= 13; n++) {
      var byColor = {};
      rackTiles.forEach(function (t) {
        if (!t.joker && t.number === n && !used[t.id] && !byColor[t.color]) byColor[t.color] = t;
      });
      var members = Object.keys(byColor).map(function (k) { return byColor[k]; });
      if (members.length >= 3) {
        members.forEach(function (t) { used[t.id] = true; });
        sets.push(members);
      } else if (members.length === 2 && jokerIdx < jokers.length) {
        members.push(jokers[jokerIdx]); jokerIdx++;
        members.forEach(function (t) { if (!t.joker) used[t.id] = true; });
        sets.push(members);
      }
    }
    return sets;
  }

  function setValue(setTiles) {
    var res = RK.validateSet(setTiles);
    return res.valid ? res.points : 0;
  }

  // Find room for `len` tiles that keeps at least one empty cell between this set and any
  // neighbouring set (otherwise two sets on the same row would merge into one invalid run).
  // Returns {row, col} or null.
  function findSpace(board, len) {
    for (var r = 0; r < RK.ROWS; r++) {
      var c = 0;
      while (c < RK.COLS) {
        if (board[r][c] != null) { c++; continue; }
        var s = c;
        while (c < RK.COLS && board[r][c] == null) c++;
        var e = c - 1;                          // maximal empty run [s..e]
        var leftMargin = (s === 0) ? 0 : 1;     // leave a gap next to an existing set
        var rightMargin = (e === RK.COLS - 1) ? 0 : 1;
        if ((e - s + 1) - leftMargin - rightMargin >= len) return { row: r, col: s + leftMargin };
      }
    }
    return null;
  }

  function placeSet(board, setTiles, pos) {
    for (var k = 0; k < setTiles.length; k++) board[pos.row][pos.col + k] = setTiles[k].id;
  }

  // Try to append single rack tiles onto existing board sets (only meaningful once melded).
  function tryAppends(board, tiles, rackIds) {
    var changed = false;
    var rack = rackIds.slice();
    var sets = RK.extractSets(board);
    for (var s = 0; s < sets.length; s++) {
      var set = sets[s];
      var leftCol = set.col, rightCol = set.col + set.ids.length - 1, row = set.row;
      for (var ri = 0; ri < rack.length; ri++) {
        var tid = rack[ri];
        // try right edge
        if (rightCol + 1 < RK.COLS && board[row][rightCol + 1] == null) {
          board[row][rightCol + 1] = tid;
          if (RK.validateBoard(board, tiles).valid) {
            rack.splice(ri, 1); changed = true; rightCol++; ri = -1; continue;
          }
          board[row][rightCol + 1] = null;
        }
        // try left edge
        if (leftCol - 1 >= 0 && board[row][leftCol - 1] == null) {
          board[row][leftCol - 1] = tid;
          if (RK.validateBoard(board, tiles).valid) {
            rack.splice(ri, 1); changed = true; leftCol--; ri = -1; continue;
          }
          board[row][leftCol - 1] = null;
        }
      }
    }
    return { changed: changed, rack: rack };
  }

  // Compute the AI's move. Returns { board, rack, melded, drew, played }.
  function takeTurn(board, tiles, rackIds, hasMelded) {
    var work = RK.cloneBoard(board);
    var rackTiles = rackIds.map(function (id) { return tiles[id]; });
    var candidates = findSets(rackTiles).filter(function (st) { return RK.validateSet(st).valid; });
    candidates.sort(function (a, b) { return setValue(b) - setValue(a); });

    var rackLeft = rackIds.slice();
    var played = false;
    var meldedNow = hasMelded;

    if (!hasMelded) {
      // Assemble disjoint sets until we reach the 30-point threshold.
      var chosen = [], total = 0, tid = {};
      for (var i = 0; i < candidates.length; i++) {
        var overlaps = candidates[i].some(function (t) { return tid[t.id]; });
        if (overlaps) continue;
        chosen.push(candidates[i]);
        candidates[i].forEach(function (t) { tid[t.id] = true; });
        total += setValue(candidates[i]);
        if (total >= RK.MELD_MIN) break;
      }
      if (total >= RK.MELD_MIN) {
        for (var s = 0; s < chosen.length; s++) {
          var pos = findSpace(work, chosen[s].length);
          if (!pos) break;
          placeSet(work, chosen[s], pos);
          chosen[s].forEach(function (t) {
            var idx = rackLeft.indexOf(t.id);
            if (idx >= 0) rackLeft.splice(idx, 1);
          });
          played = true;
        }
        if (played) meldedNow = true;
      }
    } else {
      // Play any full sets we can.
      for (var c = 0; c < candidates.length; c++) {
        var stillHave = candidates[c].every(function (t) { return rackLeft.indexOf(t.id) >= 0; });
        if (!stillHave) continue;
        var p = findSpace(work, candidates[c].length);
        if (!p) continue;
        placeSet(work, candidates[c], p);
        candidates[c].forEach(function (t) {
          var idx = rackLeft.indexOf(t.id);
          if (idx >= 0) rackLeft.splice(idx, 1);
        });
        played = true;
      }
      // Append loose tiles onto existing sets.
      var ap = tryAppends(work, tiles, rackLeft);
      if (ap.changed) { rackLeft = ap.rack; played = true; }
    }

    if (!played) return { board: board, rack: rackIds, melded: hasMelded, drew: true, played: false };
    return { board: work, rack: rackLeft, melded: meldedNow, drew: false, played: true };
  }

  var api = { findSets: findSets, takeTurn: takeTurn };
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  else { root.RK = root.RK || {}; root.RK.AI = api; }
})(typeof window !== "undefined" ? window : globalThis);

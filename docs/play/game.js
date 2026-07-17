/* Rummikub — core game logic (pure, no DOM).
 * Runs in the browser (attaches to window.RK) and in Node (module.exports) for tests. */
(function (root) {
  "use strict";

  var COLORS = ["red", "blue", "orange", "black"];
  var ROWS = 8;
  var COLS = 22;
  var RACK_START = 14;
  var MELD_MIN = 30;

  // ---- deck ----------------------------------------------------------------

  // Build the 106-tile bag: 1..13 in four colors, two copies each, plus two jokers.
  function makeTiles() {
    var tiles = {};
    var id = 0;
    for (var copy = 0; copy < 2; copy++) {
      for (var c = 0; c < COLORS.length; c++) {
        for (var n = 1; n <= 13; n++) {
          tiles[id] = { id: id, color: COLORS[c], number: n, joker: false };
          id++;
        }
      }
    }
    tiles[id] = { id: id, color: "joker", number: 0, joker: true }; id++;
    tiles[id] = { id: id, color: "joker", number: 0, joker: true }; id++;
    return tiles;
  }

  // Fisher–Yates using a supplied rng (host is authoritative, so determinism isn't required,
  // but taking an rng keeps it testable).
  function shuffle(arr, rng) {
    rng = rng || Math.random;
    var a = arr.slice();
    for (var i = a.length - 1; i > 0; i--) {
      var j = Math.floor(rng() * (i + 1));
      var t = a[i]; a[i] = a[j]; a[j] = t;
    }
    return a;
  }

  // ---- board helpers -------------------------------------------------------

  function emptyBoard() {
    var b = [];
    for (var r = 0; r < ROWS; r++) {
      var row = [];
      for (var c = 0; c < COLS; c++) row.push(null);
      b.push(row);
    }
    return b;
  }

  function cloneBoard(b) { return b.map(function (row) { return row.slice(); }); }

  // Every maximal run of horizontally-adjacent tiles is one candidate set.
  // Returns [{row, col, ids:[...]}] in board (left-to-right) order.
  function extractSets(board) {
    var sets = [];
    for (var r = 0; r < ROWS; r++) {
      var c = 0;
      while (c < COLS) {
        if (board[r][c] != null) {
          var start = c;
          var ids = [];
          while (c < COLS && board[r][c] != null) { ids.push(board[r][c]); c++; }
          sets.push({ row: r, col: start, ids: ids });
        } else c++;
      }
    }
    return sets;
  }

  function idsToTiles(ids, tiles) {
    return ids.map(function (id) { return tiles[id]; });
  }

  // ---- set validation ------------------------------------------------------

  // Validate an ordered list of tile objects as a run or group.
  // Returns { valid, kind:'run'|'group', points, values:[perTile], jokerAs:[...] }.
  function validateSet(tileObjs) {
    if (!tileObjs || tileObjs.length < 3) return { valid: false };

    var asGroup = tryGroup(tileObjs);
    if (asGroup.valid) return asGroup;
    var asRun = tryRun(tileObjs);
    if (asRun.valid) return asRun;
    return { valid: false };
  }

  function tryGroup(tileObjs) {
    if (tileObjs.length > 4) return { valid: false };
    var number = null;
    var seenColors = {};
    for (var i = 0; i < tileObjs.length; i++) {
      var t = tileObjs[i];
      if (t.joker) continue;
      if (number === null) number = t.number;
      else if (t.number !== number) return { valid: false };
      if (seenColors[t.color]) return { valid: false };
      seenColors[t.color] = true;
    }
    if (number === null) return { valid: false }; // all jokers — ambiguous, reject
    var values = tileObjs.map(function () { return number; });
    var points = values.reduce(function (a, b) { return a + b; }, 0);
    return { valid: true, kind: "group", points: points, values: values };
  }

  function tryRun(tileObjs) {
    // All non-jokers must share a color; numbers ascend by exactly 1 across the placed order.
    var color = null;
    var base = null; // number implied at index 0
    var i, t;
    for (i = 0; i < tileObjs.length; i++) {
      t = tileObjs[i];
      if (t.joker) continue;
      if (color === null) color = t.color;
      else if (t.color !== color) return { valid: false };
      var implied = t.number - i; // what index 0 would be
      if (base === null) base = implied;
      else if (implied !== base) return { valid: false };
    }
    if (base === null) return { valid: false }; // all jokers
    if (base < 1) return { valid: false };
    if (base + tileObjs.length - 1 > 13) return { valid: false };

    var values = [];
    var points = 0;
    for (i = 0; i < tileObjs.length; i++) {
      var v = base + i;
      values.push(v);
      points += v;
    }
    return { valid: true, kind: "run", points: points, values: values };
  }

  // Every set on the board is well-formed. Returns { valid, badAt:[{row,col}] }.
  function validateBoard(board, tiles) {
    var sets = extractSets(board);
    var bad = [];
    for (var i = 0; i < sets.length; i++) {
      var res = validateSet(idsToTiles(sets[i].ids, tiles));
      if (!res.valid) bad.push({ row: sets[i].row, col: sets[i].col });
    }
    return { valid: bad.length === 0, badAt: bad };
  }

  // ---- turn evaluation -----------------------------------------------------

  function boardTileIdSet(board) {
    var s = {};
    for (var r = 0; r < ROWS; r++)
      for (var c = 0; c < COLS; c++)
        if (board[r][c] != null) s[board[r][c]] = true;
    return s;
  }

  // Given the board at turn start and the board now, which tile ids were newly placed?
  function newlyPlaced(startBoard, nowBoard) {
    var before = boardTileIdSet(startBoard);
    var after = boardTileIdSet(nowBoard);
    var added = [];
    for (var id in after) if (!before[id]) added.push(Number(id));
    return added;
  }

  // Points a player scores from tiles they placed this turn (jokers count as their run/group value).
  // Only counts newly-placed tiles, using the value each takes in its validated set.
  function meldValue(nowBoard, tiles, addedIds) {
    var addedSet = {};
    addedIds.forEach(function (id) { addedSet[id] = true; });
    var sets = extractSets(nowBoard);
    var total = 0;
    for (var i = 0; i < sets.length; i++) {
      var res = validateSet(idsToTiles(sets[i].ids, tiles));
      if (!res.valid) continue;
      for (var j = 0; j < sets[i].ids.length; j++) {
        if (addedSet[sets[i].ids[j]]) total += res.values[j];
      }
    }
    return total;
  }

  // Did the player disturb tiles that were on the board at the start of the turn?
  // (Before a player has made their initial meld they may only add their own tiles.)
  function existingTilesUndisturbed(startBoard, nowBoard) {
    // Every set that existed at turn start must still exist intact somewhere.
    var beforeSets = extractSets(startBoard).map(function (s) { return s.ids.join(","); });
    var nowSets = extractSets(nowBoard).map(function (s) { return s.ids.join(","); });
    var nowKey = {};
    nowSets.forEach(function (k) { nowKey[k] = (nowKey[k] || 0) + 1; });
    for (var i = 0; i < beforeSets.length; i++) {
      if (!nowKey[beforeSets[i]]) return false;
      nowKey[beforeSets[i]]--;
    }
    return true;
  }

  // Validate a proposed turn commit. Returns { ok, reason }.
  // player = { rack:[...], hasMelded } ; startBoard = board at turn start ; nowBoard = proposed.
  function evaluateCommit(startBoard, nowBoard, tiles, player) {
    var board = validateBoard(nowBoard, tiles);
    if (!board.valid) return { ok: false, reason: "Some sets on the board aren't valid runs or groups." };

    // Tiles already on the board are communal — they may be rearranged but never taken into hand.
    var startIds = boardTileIdSet(startBoard);
    var nowIds = boardTileIdSet(nowBoard);
    for (var sid in startIds) {
      if (!nowIds[sid]) return { ok: false, reason: "Every tile already on the board has to stay on the board." };
    }

    var added = newlyPlaced(startBoard, nowBoard);
    if (added.length === 0) return { ok: false, reason: "You must place at least one tile, or draw from the deck." };

    // All newly-placed tiles must have come from this player's rack.
    var rackSet = {};
    player.rack.forEach(function (id) { rackSet[id] = true; });
    for (var i = 0; i < added.length; i++) {
      if (!rackSet[added[i]]) return { ok: false, reason: "You can only place tiles from your own rack." };
    }

    if (!player.hasMelded) {
      if (!existingTilesUndisturbed(startBoard, nowBoard))
        return { ok: false, reason: "Until your first meld, you can't rearrange tiles already on the board." };
      var value = meldValue(nowBoard, tiles, added);
      if (value < MELD_MIN)
        return { ok: false, reason: "Your first meld must be worth at least " + MELD_MIN + " points (this one is " + value + ")." };
    }

    return { ok: true, added: added };
  }

  var api = {
    COLORS: COLORS, ROWS: ROWS, COLS: COLS, RACK_START: RACK_START, MELD_MIN: MELD_MIN,
    makeTiles: makeTiles, shuffle: shuffle,
    emptyBoard: emptyBoard, cloneBoard: cloneBoard,
    extractSets: extractSets, idsToTiles: idsToTiles,
    validateSet: validateSet, validateBoard: validateBoard,
    newlyPlaced: newlyPlaced, meldValue: meldValue, evaluateCommit: evaluateCommit,
    boardTileIdSet: boardTileIdSet
  };

  if (typeof module !== "undefined" && module.exports) module.exports = api;
  else { root.RK = root.RK || {}; Object.assign(root.RK, api); }
})(typeof window !== "undefined" ? window : globalThis);

/* Rummikub — UI, drag-and-drop, and mode orchestration. */
(function () {
  "use strict";
  var RK = window.RK, AI = RK.AI, Net = RK.Net;

  // ---- DOM helpers ---------------------------------------------------------
  function $(id) { return document.getElementById(id); }
  function show(id) { $(id).classList.remove("hidden"); }
  function hide(id) { $(id).classList.add("hidden"); }
  function screen(name) {
    ["screen-menu", "screen-host-lobby", "screen-join", "screen-game"].forEach(hide);
    show("screen-" + name);
  }

  // ---- module state --------------------------------------------------------
  var mode = null;              // 'ai' | 'local' | 'host' | 'guest'
  var TILES = null;             // id -> tile object (shared, non-secret)
  var state = null;             // authoritative state (offline & host)
  var net = null;               // transport handle
  var mySeat = 0;               // this browser's seat
  var connSeat = new Map();     // host: conn -> seat
  var work = null;              // { board, rack } editable copy for the active local player
  var turnStartBoard = null;
  var guestView = null;         // guest: latest snapshot
  var pendingName = "";

  // ======================================================================
  //  Engine (authoritative) — used by offline modes and by the host.
  // ======================================================================
  function deal(playerDefs) {
    TILES = RK.makeTiles();
    var bag = RK.shuffle(Object.keys(TILES).map(Number));
    var players = playerDefs.map(function (d) {
      return { name: d.name, isAI: !!d.isAI, rack: bag.splice(0, RK.RACK_START), hasMelded: false };
    });
    state = {
      board: RK.emptyBoard(), deck: bag, players: players,
      turn: 0, phase: "playing", winner: null
    };
  }

  function advanceTurn() {
    state.turn = (state.turn + 1) % state.players.length;
  }

  function checkWin(seat) {
    if (state.players[seat].rack.length === 0) {
      state.phase = "over";
      state.winner = seat;
      return true;
    }
    return false;
  }

  // Apply a validated commit for `seat` given a proposed board. Returns {ok, reason}.
  function engineCommit(seat, proposedBoard) {
    var player = state.players[seat];
    var ev = RK.evaluateCommit(state.board, proposedBoard, TILES,
      { rack: player.rack, hasMelded: player.hasMelded });
    if (!ev.ok) return ev;
    var addedSet = {};
    ev.added.forEach(function (id) { addedSet[id] = true; });
    state.board = RK.cloneBoard(proposedBoard);
    player.rack = player.rack.filter(function (id) { return !addedSet[id]; });
    player.hasMelded = true;
    checkWin(seat);
    if (state.phase === "playing") advanceTurn();
    return { ok: true };
  }

  function engineDraw(seat) {
    if (state.deck.length > 0) state.players[seat].rack.push(state.deck.pop());
    advanceTurn();
  }

  // ======================================================================
  //  View model — render() consumes this, populated per mode.
  // ======================================================================
  function buildView() {
    if (mode === "guest") {
      var s = guestView;
      if (!s) return null;
      var acting = s.turn === mySeat && s.phase === "playing";
      return {
        board: acting && work ? work.board : s.board,
        rack: acting && work ? work.rack : (s.myRack || []),
        players: s.players, turn: s.turn, phase: s.phase, winner: s.winner,
        mySeat: mySeat, canAct: acting,
        hasMelded: s.players[mySeat] ? s.players[mySeat].hasMelded : false
      };
    }
    // authoritative (ai / local / host)
    if (!state) return null;
    var actingSeat = (mode === "host") ? 0 : state.turn;
    var isMyTurn = (state.turn === actingSeat) && !state.players[actingSeat].isAI && state.phase === "playing";
    var useWork = isMyTurn && work;
    return {
      board: useWork ? work.board : state.board,
      rack: useWork ? work.rack : state.players[actingSeat].rack,
      players: state.players.map(function (p) {
        return { name: p.name, rackCount: p.rack.length, hasMelded: p.hasMelded, isAI: p.isAI };
      }),
      turn: state.turn, phase: state.phase, winner: state.winner,
      mySeat: actingSeat, canAct: isMyTurn,
      hasMelded: state.players[actingSeat].hasMelded
    };
  }

  // ======================================================================
  //  Rendering
  // ======================================================================
  function tileEl(id, draggable) {
    var t = TILES[id];
    var d = document.createElement("div");
    d.className = "tile " + (t.joker ? "joker" : t.color);
    d.dataset.id = id;
    if (t.joker) { d.textContent = "☺"; }
    else {
      d.appendChild(document.createTextNode(String(t.number)));
      var dot = document.createElement("span"); dot.className = "dot"; d.appendChild(dot);
    }
    if (draggable) d.dataset.grab = "1";
    return d;
  }

  function render() {
    var V = buildView();
    if (!V) return;

    // turn pill
    var pill = $("turn-pill");
    if (V.phase === "over") { pill.textContent = "Game over"; pill.className = "turn-pill"; }
    else {
      var name = V.players[V.turn] ? V.players[V.turn].name : "?";
      var mine = V.canAct;
      pill.textContent = mine ? "Your turn" : (name + "'s turn");
      pill.className = "turn-pill" + (mine ? " mine" : "");
    }

    // players strip
    var strip = $("players-strip"); strip.innerHTML = "";
    V.players.forEach(function (p, i) {
      var c = document.createElement("div");
      c.className = "pchip" + (i === V.turn ? " active" : "");
      c.innerHTML = '<span>' + escapeHtml(p.name) + (p.isAI ? " 🤖" : "") + '</span>' +
        '<span class="cnt">' + p.rackCount + '</span>' +
        (p.hasMelded ? '<span class="meld">✓meld</span>' : '');
      strip.appendChild(c);
    });

    // board
    var boardEl = $("board");
    boardEl.style.gridTemplateColumns = "repeat(" + RK.COLS + ", 34px)";
    boardEl.innerHTML = "";
    var bad = RK.validateBoard(V.board, TILES).badAt.reduce(function (m, b) { m[b.row + ":" + b.col] = true; return m; }, {});
    // precompute which cells belong to a bad set
    var badCells = {};
    RK.extractSets(V.board).forEach(function (set) {
      if (bad[set.row + ":" + set.col]) set.ids.forEach(function (_, k) { badCells[set.row + ":" + (set.col + k)] = true; });
    });
    for (var r = 0; r < RK.ROWS; r++) {
      for (var c = 0; c < RK.COLS; c++) {
        var cell = document.createElement("div");
        cell.className = "cell";
        cell.dataset.row = r; cell.dataset.col = c;
        var id = V.board[r][c];
        if (id != null) {
          var te = tileEl(id, V.canAct);
          if (badCells[r + ":" + c]) te.classList.add("badset");
          cell.appendChild(te);
        }
        boardEl.appendChild(cell);
      }
    }

    // rack
    var rackEl = $("rack");
    rackEl.innerHTML = "";
    rackEl.classList.toggle("locked", !V.canAct);
    V.rack.forEach(function (id) { rackEl.appendChild(tileEl(id, V.canAct)); });

    // controls
    var lock = !V.canAct || V.phase !== "playing";
    ["btn-confirm", "btn-undo", "btn-draw", "btn-sort-run", "btn-sort-grp"].forEach(function (b) {
      $(b).disabled = lock;
    });

    // meld meter
    var meter = $("meld-meter");
    if (V.canAct && !V.hasMelded) {
      var added = RK.newlyPlaced(turnStartBoard || state && state.board || guestView.board, V.board);
      var val = RK.meldValue(V.board, TILES, added);
      meter.innerHTML = "First meld: <b>" + val + "</b> / " + RK.MELD_MIN + " points";
    } else meter.textContent = "";

    if (V.phase === "over") showGameOver(V);
  }

  function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, function (ch) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
    });
  }

  function setMsg(text, kind) {
    var m = $("msg");
    m.textContent = text || "";
    m.className = "msg" + (kind ? " " + kind : "");
  }

  // ======================================================================
  //  Turn lifecycle
  // ======================================================================
  function startLocalTurnIfHuman() {
    // Set up the working copy for the active human seat (offline) or host seat 0.
    var actingSeat = (mode === "host") ? 0 : state.turn;
    if (state.turn === actingSeat && !state.players[actingSeat].isAI && state.phase === "playing") {
      turnStartBoard = RK.cloneBoard(state.board);
      work = { board: RK.cloneBoard(state.board), rack: state.players[actingSeat].rack.slice() };
    } else {
      work = null;
    }
  }

  function afterAuthoritativeChange() {
    if (mode === "host") broadcastState();
    if (state.phase === "over") { render(); return; }
    var cur = state.players[state.turn];
    if (cur.isAI) {
      render();
      scheduleAI();
    } else {
      startLocalTurnIfHuman();
      // host: only seat 0 edits locally; when it's a guest's turn, host just waits/renders.
      render();
    }
  }

  var aiTimer = null;
  function scheduleAI() {
    setMsg(state.players[state.turn].name + " is thinking…");
    aiTimer = setTimeout(function () {
      aiTimer = null;
      if (!state || state.phase !== "playing" || !state.players[state.turn].isAI) return;
      var seat = state.turn;
      var p = state.players[seat];
      var res = AI.takeTurn(state.board, TILES, p.rack, p.hasMelded);
      if (res.drew) {
        engineDraw(seat);
        setMsg(p.name + " drew a tile.");
      } else {
        state.board = res.board;
        p.rack = res.rack;
        p.hasMelded = res.melded;
        setMsg(p.name + " played.");
        if (!checkWin(seat)) advanceTurn();
      }
      afterAuthoritativeChange();
    }, 850);
  }

  // ======================================================================
  //  Local player actions (offline active player, host seat 0, guest)
  // ======================================================================
  function doConfirm() {
    if (!work) return;
    if (mode === "guest") {
      net.send({ type: "commit", board: work.board });
      setMsg("Sent — waiting for host…");
      return;
    }
    var seat = (mode === "host") ? 0 : state.turn;
    var res = engineCommit(seat, work.board);
    if (!res.ok) { setMsg(res.reason, "err"); return; }
    setMsg("");
    afterAuthoritativeChange();
  }

  function doDraw() {
    if (mode === "guest") {
      net.send({ type: "draw" });
      setMsg("Drawing…");
      return;
    }
    var seat = (mode === "host") ? 0 : state.turn;
    engineDraw(seat);
    setMsg("You drew a tile.");
    afterAuthoritativeChange();
  }

  function doUndo() {
    if (!work) return;
    var rackSource;
    if (mode === "guest") rackSource = guestView.myRack;
    else rackSource = state.players[(mode === "host") ? 0 : state.turn].rack;
    work.board = RK.cloneBoard(turnStartBoard);
    work.rack = rackSource.slice();
    setMsg("");
    render();
  }

  function sortRack(kind) {
    if (!work) return;
    work.rack.sort(function (a, b) {
      var ta = TILES[a], tb = TILES[b];
      if (ta.joker) return 1; if (tb.joker) return -1;
      if (kind === "run") {
        var ci = RK.COLORS.indexOf(ta.color) - RK.COLORS.indexOf(tb.color);
        return ci !== 0 ? ci : ta.number - tb.number;
      } else {
        return ta.number - tb.number || RK.COLORS.indexOf(ta.color) - RK.COLORS.indexOf(tb.color);
      }
    });
    render();
  }

  // ======================================================================
  //  Drag & drop (pointer based)
  // ======================================================================
  var drag = null; // { id, fromType, row, col, ghost }

  function onPointerDown(e) {
    var V = buildView();
    if (!V || !V.canAct) return;
    var tile = e.target.closest(".tile");
    if (!tile || !tile.dataset.grab) return;
    e.preventDefault();
    var id = Number(tile.dataset.id);
    var cell = tile.closest(".cell");
    drag = {
      id: id,
      fromType: cell ? "board" : "rack",
      row: cell ? Number(cell.dataset.row) : -1,
      col: cell ? Number(cell.dataset.col) : -1,
      ghost: null
    };
    tile.classList.add("dragging");
    var ghost = tile.cloneNode(true);
    ghost.classList.add("floating");
    ghost.classList.remove("dragging");
    document.body.appendChild(ghost);
    drag.ghost = ghost;
    moveGhost(e);
    window.addEventListener("pointermove", onPointerMove);
    window.addEventListener("pointerup", onPointerUp);
  }

  function moveGhost(e) {
    if (!drag || !drag.ghost) return;
    drag.ghost.style.left = (e.clientX - 17) + "px";
    drag.ghost.style.top = (e.clientY - 23) + "px";
  }

  function onPointerMove(e) {
    moveGhost(e);
    document.querySelectorAll(".cell.drop-ok").forEach(function (c) { c.classList.remove("drop-ok"); });
    var under = document.elementFromPoint(e.clientX, e.clientY);
    var cell = under && under.closest && under.closest(".cell");
    if (cell && !cell.querySelector(".tile")) cell.classList.add("drop-ok");
  }

  function onPointerUp(e) {
    window.removeEventListener("pointermove", onPointerMove);
    window.removeEventListener("pointerup", onPointerUp);
    if (drag.ghost) drag.ghost.remove();
    document.querySelectorAll(".cell.drop-ok").forEach(function (c) { c.classList.remove("drop-ok"); });

    var under = document.elementFromPoint(e.clientX, e.clientY);
    var cell = under && under.closest && under.closest(".cell");
    var rack = under && under.closest && under.closest(".rack");

    if (cell) {
      var r = Number(cell.dataset.row), c = Number(cell.dataset.col);
      if (work.board[r][c] == null) {
        removeFromSource();
        work.board[r][c] = drag.id;
      }
    } else if (rack) {
      if (drag.fromType === "board") {
        work.board[drag.row][drag.col] = null;
        if (work.rack.indexOf(drag.id) < 0) work.rack.push(drag.id);
      }
      // rack→rack: leave order unchanged
    }
    drag = null;
    setMsg("");
    render();
  }

  function removeFromSource() {
    if (drag.fromType === "board") work.board[drag.row][drag.col] = null;
    else { var i = work.rack.indexOf(drag.id); if (i >= 0) work.rack.splice(i, 1); }
  }

  // ======================================================================
  //  Game over
  // ======================================================================
  var overShown = false;
  function showGameOver(V) {
    if (overShown) return;
    overShown = true;
    var winnerName = V.players[V.winner] ? V.players[V.winner].name : "Someone";
    $("winner-name").textContent = winnerName;
    $("over-detail").textContent = (V.winner === V.mySeat && mode !== "local")
      ? "You emptied your rack first. Nicely played." : "They emptied their rack first.";
    show("overlay");
  }

  // ======================================================================
  //  Online — host
  // ======================================================================
  function personalSnapshot(seat) {
    return {
      type: "state",
      board: state.board,
      deckCount: state.deck.length,
      turn: state.turn, phase: state.phase, winner: state.winner,
      players: state.players.map(function (p) {
        return { name: p.name, rackCount: p.rack.length, hasMelded: p.hasMelded, isAI: p.isAI };
      }),
      myRack: state.players[seat].rack.slice()
    };
  }

  function broadcastState() {
    connSeat.forEach(function (seat, conn) { net.sendTo(conn, personalSnapshot(seat)); });
  }

  function refreshHostLobby() {
    var ul = $("host-players"); ul.innerHTML = "";
    hostPlayers.forEach(function (p) {
      var li = document.createElement("li");
      li.innerHTML = '<span class="dot"></span>' + escapeHtml(p.name);
      ul.appendChild(li);
    });
    $("host-start").disabled = hostPlayers.length < 2;
    $("host-status").textContent = hostPlayers.length < 2
      ? "Waiting for players to join…" : "Ready — you can start whenever.";
  }

  var hostPlayers = [];
  function createRoom() {
    mode = "host";
    hostPlayers = [{ name: "You (host)", isAI: false }];
    connSeat = new Map();
    try {
      net = Net.host({
        onOpen: function (code) { $("host-code").textContent = code; refreshHostLobby(); },
        onError: function (err) { $("host-status").textContent = "Connection error: " + (err && err.type || err); },
        onGuestJoin: function (conn) { /* wait for join message with name */ },
        onData: function (conn, msg) { hostOnData(conn, msg); },
        onGuestLeave: function (conn) {
          var seat = connSeat.get(conn);
          setMsg((state && state.players[seat] ? state.players[seat].name : "A player") + " left.");
        }
      });
    } catch (e) { screen("menu"); alert(e.message); return; }
    screen("host-lobby");
    $("host-code").textContent = "····";
  }

  function hostOnData(conn, msg) {
    if (!msg || !msg.type) return;
    if (msg.type === "join") {
      if (state) return; // game already started
      var seat = hostPlayers.length;
      hostPlayers.push({ name: (msg.name || "Player " + (seat + 1)).slice(0, 14), isAI: false });
      connSeat.set(conn, seat);
      net.sendTo(conn, { type: "welcome", seat: seat });
      broadcastLobby();
      refreshHostLobby();
    } else if (msg.type === "commit") {
      var s = connSeat.get(conn);
      if (s == null || state.turn !== s) return;
      var res = engineCommit(s, msg.board);
      if (!res.ok) { net.sendTo(conn, { type: "reject", reason: res.reason }); return; }
      afterAuthoritativeChange();
    } else if (msg.type === "draw") {
      var sd = connSeat.get(conn);
      if (sd == null || state.turn !== sd) return;
      engineDraw(sd);
      afterAuthoritativeChange();
    }
  }

  function broadcastLobby() {
    var names = hostPlayers.map(function (p) { return p.name; });
    net.conns().forEach(function (c) { c.send({ type: "lobby", players: names }); });
  }

  function hostStart() {
    if (hostPlayers.length < 2) return;
    deal(hostPlayers);
    // tell everyone the static tile map + kick off
    connSeat.forEach(function (seat, conn) {
      net.sendTo(conn, { type: "start", tiles: TILES, seat: seat });
    });
    screen("game");
    overShown = false;
    startLocalTurnIfHuman();
    broadcastState();
    render();
    if (state.players[state.turn].isAI) scheduleAI();
  }

  // ======================================================================
  //  Online — guest
  // ======================================================================
  function joinRoom(code, name) {
    mode = "guest";
    pendingName = name;
    $("join-status").textContent = "Connecting…";
    try {
      net = Net.join(code, {
        onOpen: function () { net.send({ type: "join", name: pendingName }); $("join-status").textContent = "Joined. Waiting for host to start…"; },
        onData: function (msg) { guestOnData(msg); },
        onClose: function () { setMsg("Disconnected from host.", "err"); },
        onError: function (err) { $("join-status").textContent = "Could not connect: " + (err && err.type || err) + ". Check the code."; }
      });
    } catch (e) { alert(e.message); }
  }

  function guestOnData(msg) {
    if (!msg || !msg.type) return;
    if (msg.type === "welcome") {
      mySeat = msg.seat;
    } else if (msg.type === "lobby") {
      var ul = $("join-players"); ul.innerHTML = "";
      msg.players.forEach(function (n) {
        var li = document.createElement("li");
        li.innerHTML = '<span class="dot"></span>' + escapeHtml(n);
        ul.appendChild(li);
      });
    } else if (msg.type === "start") {
      TILES = msg.tiles;
      mySeat = msg.seat;
      screen("game");
      overShown = false;
      work = null;
    } else if (msg.type === "state") {
      var wasMyTurn = guestView && guestView.turn === mySeat;
      guestView = msg;
      if (msg.turn === mySeat && msg.phase === "playing") {
        // entering my turn — build a fresh working copy
        turnStartBoard = RK.cloneBoard(msg.board);
        work = { board: RK.cloneBoard(msg.board), rack: msg.myRack.slice() };
        setMsg("Your turn — place tiles, then Confirm (or Draw & pass).");
      } else {
        work = null;
        if (msg.phase === "playing") setMsg("");
      }
      render();
    } else if (msg.type === "reject") {
      // restore editing from last snapshot
      if (guestView) { work = { board: RK.cloneBoard(guestView.board), rack: guestView.myRack.slice() }; turnStartBoard = RK.cloneBoard(guestView.board); }
      setMsg(msg.reason, "err");
      render();
    }
  }

  // ======================================================================
  //  Offline setup
  // ======================================================================
  function startOffline(kind) {
    mode = kind === "ai" ? "ai" : "local";
    var defs = kind === "ai"
      ? [{ name: "You", isAI: false }, { name: "Computer", isAI: true }]
      : [{ name: "Player 1", isAI: false }, { name: "Player 2", isAI: false }];
    deal(defs);
    screen("game");
    overShown = false;
    setMsg("");
    startLocalTurnIfHuman();
    render();
  }

  // ======================================================================
  //  Wiring
  // ======================================================================
  function leaveToMenu() {
    if (aiTimer) { clearTimeout(aiTimer); aiTimer = null; }
    if (net && net.close) { try { net.close(); } catch (e) {} }
    net = null; state = null; work = null; guestView = null; mode = null;
    connSeat = new Map(); hostPlayers = []; overShown = false;
    hide("overlay");
    screen("menu");
    setMsg("");
  }

  document.addEventListener("click", function (e) {
    var b = e.target.closest("[data-act]");
    if (!b) return;
    var act = b.dataset.act;
    if (act === "ai") startOffline("ai");
    else if (act === "local") startOffline("local");
    else if (act === "create") createRoom();
    else if (act === "join") { screen("join"); }
    else if (act === "menu") leaveToMenu();
  });

  $("host-start").addEventListener("click", hostStart);
  $("join-go").addEventListener("click", function () {
    var code = ($("join-code").value || "").trim().toUpperCase();
    var name = ($("join-name").value || "").trim() || "Guest";
    if (code.length !== 4) { $("join-status").textContent = "Enter the 4-character code."; return; }
    joinRoom(code, name);
  });
  $("join-code").addEventListener("input", function () { this.value = this.value.toUpperCase(); });

  $("btn-confirm").addEventListener("click", doConfirm);
  $("btn-undo").addEventListener("click", doUndo);
  $("btn-draw").addEventListener("click", doDraw);
  $("btn-sort-run").addEventListener("click", function () { sortRack("run"); });
  $("btn-sort-grp").addEventListener("click", function () { sortRack("group"); });
  $("over-menu").addEventListener("click", leaveToMenu);
  $("leave-game").addEventListener("click", function (e) { e.preventDefault(); leaveToMenu(); });

  $("board").addEventListener("pointerdown", onPointerDown);
  $("rack").addEventListener("pointerdown", onPointerDown);

  // expose a tiny hook for automated testing
  window.__rk = {
    state: function () { return state; },
    view: buildView,
    forceAI: scheduleAI,
    place: function (id, r, c) { if (work) { var i = work.rack.indexOf(id); if (i >= 0) work.rack.splice(i, 1); work.board[r][c] = id; render(); } },
    autoMove: function () {
      if (!work) return false;
      var seat = (mode === "host") ? 0 : state.turn;
      var res = AI.takeTurn(turnStartBoard, TILES, work.rack, state.players[seat].hasMelded);
      if (!res.played) return false;
      work.board = res.board; work.rack = res.rack; render(); return true;
    },
    confirm: doConfirm, draw: doDraw, mode: function () { return mode; },
    rigWin: function () {
      // test-only: give seat 0 exactly a 30-point group so confirming empties the rack
      function findId(color, number) {
        for (var k in TILES) if (TILES[k].color === color && TILES[k].number === number) return Number(k);
      }
      var ids = [findId("red", 10), findId("blue", 10), findId("orange", 10)];
      state.board = RK.emptyBoard();
      state.players[0].rack = ids.slice();
      state.players[0].hasMelded = false;
      state.turn = 0; state.phase = "playing"; state.winner = null;
      startLocalTurnIfHuman(); render();
    }
  };
})();

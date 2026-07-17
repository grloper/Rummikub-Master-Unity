/* Rummikub — peer-to-peer transport over WebRTC (PeerJS public broker).
 * Star topology: the host is the hub and the authority; guests connect to the host.
 * No server of ours is involved — PeerJS's free broker only introduces peers, then
 * traffic flows browser-to-browser. */
(function (root) {
  "use strict";

  var PREFIX = "rkub-v1-"; // namespaces our ids on the shared public broker

  function peerId(code) { return PREFIX + code.toLowerCase(); }

  // Short, unambiguous room codes (no 0/O/1/I).
  function makeCode() {
    var abc = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    var s = "";
    for (var i = 0; i < 4; i++) s += abc[Math.floor(Math.random() * abc.length)];
    return s;
  }

  function ensurePeerLib() {
    if (typeof root.Peer === "undefined")
      throw new Error("PeerJS failed to load (network blocked?). Online play needs the peerjs library.");
  }

  // Host a room. handlers: { onOpen(code), onGuestJoin(conn), onData(conn,msg), onGuestLeave(conn), onError(err) }
  function host(handlers) {
    ensurePeerLib();
    var code = makeCode();
    var conns = [];
    var peer = new root.Peer(peerId(code), { debug: 1 });

    peer.on("open", function () { handlers.onOpen && handlers.onOpen(code); });
    peer.on("error", function (err) { handlers.onError && handlers.onError(err); });
    peer.on("connection", function (conn) {
      conn.on("open", function () {
        conns.push(conn);
        handlers.onGuestJoin && handlers.onGuestJoin(conn);
      });
      conn.on("data", function (msg) { handlers.onData && handlers.onData(conn, msg); });
      conn.on("close", function () {
        conns = conns.filter(function (c) { return c !== conn; });
        handlers.onGuestLeave && handlers.onGuestLeave(conn);
      });
    });

    return {
      code: code,
      peer: peer,
      conns: function () { return conns; },
      broadcast: function (msg) { conns.forEach(function (c) { if (c.open) c.send(msg); }); },
      sendTo: function (conn, msg) { if (conn && conn.open) conn.send(msg); },
      close: function () { conns.forEach(function (c) { c.close(); }); peer.destroy(); }
    };
  }

  // Join a room by code. handlers: { onOpen(), onData(msg), onClose(), onError(err) }
  function join(code, handlers) {
    ensurePeerLib();
    var peer = new root.Peer({ debug: 1 });
    var conn = null;

    peer.on("open", function () {
      conn = peer.connect(peerId(code), { reliable: true });
      conn.on("open", function () { handlers.onOpen && handlers.onOpen(); });
      conn.on("data", function (msg) { handlers.onData && handlers.onData(msg); });
      conn.on("close", function () { handlers.onClose && handlers.onClose(); });
      conn.on("error", function (err) { handlers.onError && handlers.onError(err); });
    });
    peer.on("error", function (err) { handlers.onError && handlers.onError(err); });

    return {
      peer: peer,
      send: function (msg) { if (conn && conn.open) conn.send(msg); },
      close: function () { if (conn) conn.close(); peer.destroy(); }
    };
  }

  root.RK = root.RK || {};
  root.RK.Net = { host: host, join: join, makeCode: makeCode };
})(typeof window !== "undefined" ? window : globalThis);

/* Presentation fixture; all move validation delegates to the unchanged production RK rules. */
(function () {
  'use strict';
  const tiles = RK.makeTiles();
  const fixture = [34, 35, 104, 19]; // orange 9, orange 10, joker, blue 7: physical IDs
  let committed, board, rack, hasMelded;
  const $ = id => document.getElementById(id);
  function evaluate() { return RK.evaluateCommit(committed, board, tiles, {rack: fixture.filter(id => !RK.boardTileIdSet(committed)[id]), hasMelded}); }
  function tile(id, interactive) {
    const t = tiles[id], el = document.createElement(interactive ? 'button' : 'div');
    el.className = 'tile ' + t.color;
    el.textContent = t.joker ? '\u2605' : t.number;
    if (interactive) { el.setAttribute('aria-label', 'Place ' + (t.joker ? 'joker' : t.color + ' ' + t.number)); el.onclick = () => { const c = board[0].indexOf(null); if (c >= 0) {board[0][c] = id; rack = rack.filter(x => x !== id); $('status').textContent = 'Tile placed in the draft. Check before committing.'; render();} }; }
    const small = document.createElement('small'); small.textContent = '#' + id; el.append(small); return el;
  }
  function render() {
    $('rack').replaceChildren(...rack.map(id => tile(id, true)));
    $('board').replaceChildren(...board[0].slice(0, 10).map((id,c) => { const el = document.createElement('div'); el.className = 'cell'; el.setAttribute('aria-label', 'Column ' + c + (id === null ? ', empty' : ', tile ' + id)); if (id !== null) el.append(tile(id, false)); return el; }));
    const ids = board[0].filter(id => id !== null);
    const nodes = [];
    ids.forEach((id, i) => {if(i){const arrow=document.createElement('span');arrow.className='arrow';arrow.textContent='\u21c4';nodes.push(arrow);}const node=document.createElement('div');node.className='node';node.textContent=(i===0?'HEAD / ':'')+'#'+id+(i===ids.length-1?' / TAIL':'');nodes.push(node);});
    $('links').replaceChildren(...nodes); if (!nodes.length) $('links').textContent = 'HEAD = TAIL = null';
    $('membership').textContent = '{ ' + ids.map(id => '#' + id).join(', ') + ' }';
    const added = RK.newlyPlaced(committed, board);
    $('points').textContent = RK.validateBoard(board,tiles).valid ? RK.meldValue(board, tiles, added) : '\u2014';
    $('remaining').textContent = rack.length;
    $('phase').textContent = added.length ? 'Draft' : (hasMelded ? 'Committed' : 'Draft');
    $('commit').disabled = !evaluate().ok;
    $('result').textContent = JSON.stringify(evaluate(), null, 2);
  }
  function reset() { committed=RK.emptyBoard(); board=RK.cloneBoard(committed); rack=fixture.slice(); hasMelded=false; $('status').textContent='Select orange 9, orange 10, then the joker.'; render(); }
  $('validate').onclick = () => { const result=evaluate(); $('status').textContent=result.ok ? 'Legal initial meld. Commit to apply this board.' : result.reason; render(); };
  $('commit').onclick = () => { const result=evaluate(); if(!result.ok) return; committed=RK.cloneBoard(board); hasMelded=true; $('status').textContent='Committed: three physical tiles leave the rack; the board is now authoritative.'; render(); };
  $('undo').onclick = () => {board=RK.cloneBoard(committed);rack=fixture.filter(id=>!RK.boardTileIdSet(committed)[id]);$('status').textContent='Draft restored to the committed board.';render();};
  $('reset').onclick=reset;
  reset();
})();

const test = require('node:test');
const assert = require('node:assert/strict');
const RK = require('../docs/play/game.js');
globalThis.RK = RK;
const AI = require('../docs/play/ai.js');
const tiles = RK.makeTiles();
const id = (color, number, copy = 0) => copy * 52 + RK.COLORS.indexOf(color) * 13 + number - 1;

test('bag contains 106 distinct physical tiles and two jokers', () => {
  assert.equal(Object.keys(tiles).length, 106);
  assert.equal(Object.values(tiles).filter(t => t.joker).length, 2);
});

test('groups reject duplicate colors and runs reject wrapping', () => {
  assert.equal(RK.validateSet([tiles[0], tiles[52], tiles[13]]).valid, false);
  assert.equal(RK.validateSet([tiles[11], tiles[12], tiles[0]]).valid, false);
  assert.equal(RK.validateSet([tiles[11], tiles[12], tiles[104], tiles[105]]).valid, false);
  assert.equal(RK.validateSet([tiles[9], tiles[104], tiles[11]]).points, 33);
});

test('board rejects cloned physical IDs, unknown IDs and malformed dimensions', () => {
  const board = RK.emptyBoard();
  board[0].splice(0, 3, 0, 1, 2);
  board[1].splice(0, 3, 0, 1, 2);
  assert.equal(RK.validateBoard(board, tiles).valid, false);
  board[1].splice(0, 3, 999, 13, 26);
  assert.equal(RK.validateBoard(board, tiles).valid, false);
  assert.equal(RK.validateBoard([], tiles).valid, false);
  assert.equal(RK.validateSet([undefined, tiles[1], tiles[2]]).valid, false);
});

test('opening meld is atomic when only one of two required sets fits', () => {
  // Deliberately crowded synthetic board; no networking or saved game needed.
  const board = RK.emptyBoard();
  const synthetic = { ...tiles };
  let next = 200;
  for (let r = 0; r < RK.ROWS; r++) {
    for (const start of (r === 7 ? [2, 8, 14] : [2, 8, 14, 18])) {
      for (let c = 0; c < 3; c++) {
        synthetic[next] = { id: next, color: RK.COLORS[c], number: 1, joker: false };
        board[r][start + c] = next++;
      }
    }
  }
  const rack = [id('red', 6), id('blue', 6), id('orange', 6), id('red', 7), id('blue', 7), id('orange', 7)];
  assert.equal(RK.validateBoard(board, synthetic).valid, true);
  const result = AI.takeTurn(board, synthetic, rack, false);
  assert.equal(result.played, false);
  assert.equal(result.melded, false);
  assert.deepEqual(result.rack, rack);
  assert.deepEqual(result.board, board);
});

test('duplicate numbers do not hide a valid greedy run', () => {
  const rack = [id('red', 10), id('red', 11), id('red', 11, 1), id('red', 12)];
  const result = AI.takeTurn(RK.emptyBoard(), tiles, rack, false);
  assert.equal(result.played, true);
  assert.equal(result.rack.length, 1);
  assert.equal(RK.evaluateCommit(RK.emptyBoard(), result.board, tiles, { rack, hasMelded: false }).ok, true);
});

test('seeded racks preserve tile ownership and every played turn is valid', () => {
  let seed = 20261001;
  const rng = () => ((seed = (1664525 * seed + 1013904223) >>> 0) / 4294967296);
  for (let i = 0; i < 400; i++) {
    const rack = RK.shuffle(Object.values(tiles).map(t => t.id), rng).slice(0, 14);
    const start = RK.emptyBoard();
    const result = AI.takeTurn(start, tiles, rack, i % 2 === 0);
    assert.deepEqual(start, RK.emptyBoard());
    if (result.played) {
      assert.equal(RK.evaluateCommit(start, result.board, tiles, { rack, hasMelded: i % 2 === 0 }).ok, true);
      const placed = RK.newlyPlaced(start, result.board);
      assert.equal(new Set([...placed, ...result.rack]).size, rack.length);
      assert.equal(placed.length + result.rack.length, rack.length);
    } else assert.deepEqual(result.rack, rack);
  }
});

test('50 seeded two-player simulations preserve all 106 tiles across turns', () => {
  let seed = 87125;
  const rng = () => ((seed = (1664525 * seed + 1013904223) >>> 0) / 4294967296);
  for (let game = 0; game < 50; game++) {
    const bag = RK.shuffle(Object.values(tiles).map(t => t.id), rng);
    const players = [0, 1].map(() => ({ rack: bag.splice(0, 14), hasMelded: false }));
    let board = RK.emptyBoard(), dryTurns = 0;
    for (let turn = 0; turn < 600 && dryTurns < 2; turn++) {
      const p = players[turn % 2];
      const result = AI.takeTurn(board, tiles, p.rack, p.hasMelded);
      if (result.played) {
        assert.equal(RK.evaluateCommit(board, result.board, tiles, p).ok, true);
        board = result.board; p.rack = result.rack; p.hasMelded = result.melded;
        dryTurns = 0;
      } else if (bag.length) { p.rack.push(bag.pop()); dryTurns = 0; }
      else dryTurns++;
      const all = [...board.flat().filter(id => id != null), ...bag, ...players.flatMap(p => p.rack)];
      assert.equal(all.length, 106);
      assert.equal(new Set(all).size, 106);
      assert.equal(RK.validateBoard(board, tiles).valid, true);
      if (!p.rack.length) break;
    }
  }
});

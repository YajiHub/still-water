import { describe, it } from "node:test";
import assert from "node:assert/strict";

import { decoyDepth, generateLevel, type GenOptions } from "../scripts/generate-level.js";
import { edgeGap, PLAYER_REACH_M, reachesExit, validateLayout, type PlatformSpot, type Rect } from "../src/logic/layout.js";

const opts: GenOptions = {
  id: "levelTest",
  name: "Test Hall",
  width: 30,
  length: 30,
  platforms: 22,
  routeMin: 6,
  routeMax: 9,
  gapMin: 2.0,
  gapMax: 2.6,
  sizes: [2, 1.6],
  smallChance: 0.25,
  seed: 7,
};

describe("generateLevel", () => {
  const layout = generateLevel(opts);
  const rect = (id: number): Rect => {
    const p = layout.platforms.find((q) => q.id === id)!;
    return { x: p.x, z: p.z, width: p.size, depth: p.size };
  };

  it("makes a valid layout flagged as one dry route", () => {
    assert.deepEqual(validateLayout(layout), []);
    assert.equal(layout.oneDryRoute, true);
    assert.equal(layout.id, "levelTest");
    assert.equal(layout.name, "Test Hall");
  });

  it("has the requested platform count and a route length in range", () => {
    assert.equal(layout.platforms.length, 22);
    assert.ok(layout.safePath.length >= 6 && layout.safePath.length <= 9, `route ${layout.safePath.length}`);
  });

  it("keeps every route jump inside the gap range", () => {
    const steps = [layout.start, ...layout.safePath.map(rect), layout.exit];
    for (let i = 1; i < steps.length; i++) {
      const g = edgeGap(steps[i - 1], steps[i]);
      assert.ok(g >= opts.gapMin - 1e-9 && g <= opts.gapMax + 1e-9, `step ${i}: ${g.toFixed(2)} m`);
    }
  });

  it("spaces platforms at least gapMin apart and only uses the given sizes", () => {
    for (const a of layout.platforms) {
      assert.ok(opts.sizes.includes(a.size), `platform ${a.id} size ${a.size}`);
      for (const b of layout.platforms) {
        if (a.id < b.id) assert.ok(edgeGap(rect(a.id), rect(b.id)) >= opts.gapMin - 1e-9, `${a.id}–${b.id}`);
      }
    }
  });

  it("needs every route platform (the rest are dead ends)", () => {
    for (const id of layout.safePath) assert.equal(reachesExit(layout, [id], PLAYER_REACH_M), false, `without ${id}`);
  });

  it("is the same for the same seed and different for another", () => {
    assert.deepEqual(generateLevel(opts), layout);
    assert.notDeepEqual(generateLevel({ ...opts, seed: 8 }), layout);
  });

  it("numbers platforms 1..n from the start side", () => {
    assert.deepEqual(layout.platforms.map((p) => p.id), layout.platforms.map((_, i) => i + 1));
    for (let i = 1; i < layout.platforms.length; i++) assert.ok(layout.platforms[i].z >= layout.platforms[i - 1].z);
  });
});

describe("generateLevel with decoys and ferries", () => {
  const big: GenOptions = {
    ...opts,
    width: 36,
    length: 48,
    platforms: 40,
    routeMin: 8,
    routeMax: 13,
    gapMin: 2.4,
    gapMax: 2.8,
    decoys: [2, 2],
    decoyLength: [4, 5],
    ferries: [1, 2],
    ferryTravel: [4, 5],
    ferryGaps: [2.0, 2.4],
    ferrySeconds: 3,
    ferryPause: 1,
    seed: 1, // seeds can fail cleanly ("try another seed"); this one fits 2 decoys and a ferry
  };
  const layout = generateLevel(big);
  const at = (p: PlatformSpot): Rect => ({ x: p.x, z: p.z, width: p.size, depth: p.size });
  const atB = (p: PlatformSpot): Rect => ({ x: p.x + p.move!.dx, z: p.z + p.move!.dz, width: p.size, depth: p.size });
  const byId = (id: number) => layout.platforms.find((p) => p.id === id)!;

  it("makes a valid one-dry-route layout", () => {
    assert.deepEqual(validateLayout(layout), []);
    assert.equal(layout.platforms.length, 40);
  });

  it("puts 1–2 ferries on the route, never first or last, travelling 4–5 m", () => {
    const route = layout.safePath.map(byId);
    const ferries = route.filter((p) => p.move);
    assert.ok(ferries.length >= 1 && ferries.length <= 2, `ferries ${ferries.length}`);
    assert.ok(!route[0].move && !route.at(-1)!.move);
    for (const f of ferries) {
      const travel = Math.hypot(f.move!.dx, f.move!.dz);
      assert.ok(travel >= 4 - 0.1 && travel <= 5 + 0.1, `travel ${travel}`);
      assert.equal(f.move!.seconds, 3);
      assert.equal(f.move!.pause, 1);
    }
  });

  it("boards a ferry at end A and leaves it at end B with the ferry gaps", () => {
    const route = layout.safePath.map(byId);
    route.forEach((p, i) => {
      if (!p.move) return;
      const prev = route[i - 1].move ? atB(route[i - 1]) : at(route[i - 1]);
      const board = edgeGap(prev, at(p));
      const leave = edgeGap(atB(p), at(route[i + 1]));
      assert.ok(board >= 2.0 - 1e-9 && board <= 2.4 + 1e-9, `board ${board.toFixed(2)}`);
      assert.ok(leave >= 2.0 - 1e-9 && leave <= 2.4 + 1e-9, `leave ${leave.toFixed(2)}`);
    });
  });

  it("needs every route platform, ferries included", () => {
    for (const id of layout.safePath) assert.equal(reachesExit(layout, [id], PLAYER_REACH_M), false, `without ${id}`);
  });

  it("has a decoy branch at least 4 jumps deep", () => {
    assert.ok(decoyDepth(layout, big.gapMax) >= 4, `deepest decoy ${decoyDepth(layout, big.gapMax)}`);
  });

  it("only route platforms move", () => {
    for (const p of layout.platforms) if (p.move) assert.ok(layout.safePath.includes(p.id));
  });
});

describe("generateLevel with spread and sinking tiles (Level 4)", () => {
  const sinky: GenOptions = {
    ...opts,
    width: 42,
    length: 48,
    platforms: 50,
    routeMin: 9,
    routeMax: 18,
    gapMin: 2.6,
    gapMax: 2.8,
    maxTurnDeg: 70,
    decoys: [2, 3],
    decoyLength: [4, 6],
    spread: true,
    crumble: 0.5,
    onewayDecoys: true,
    seed: 7, // spread makes many seeds fail cleanly in a hall this small; this one fits
  };
  const layout = generateLevel(sinky);
  const third = (x: number) => (x < -sinky.width / 6 ? 0 : x > sinky.width / 6 ? 2 : 1);

  it("makes a valid one-dry-route layout", () => {
    assert.deepEqual(validateLayout(layout), []);
    for (const id of layout.safePath) assert.equal(reachesExit(layout, [id], PLAYER_REACH_M), false, `without ${id}`);
  });

  it("fills left, centre and right: each third holds at least 20% of the platforms", () => {
    const counts = [0, 0, 0];
    for (const p of layout.platforms) counts[third(p.x)]++;
    for (const c of counts) assert.ok(c >= 0.2 * layout.platforms.length, `thirds ${counts.join("/")}`);
  });

  it("makes half the route crumble (rounded), and nothing else crumbles", () => {
    const crumbling = layout.platforms.filter((p) => p.sink === "crumble");
    assert.equal(crumbling.length, Math.round(0.5 * layout.safePath.length));
    for (const p of crumbling) assert.ok(layout.safePath.includes(p.id), `crumbling ${p.id} is off the route`);
  });

  it("makes decoy tiles one-way, never route tiles", () => {
    const oneway = layout.platforms.filter((p) => p.sink === "oneway");
    assert.ok(oneway.length >= 2 * 4, `one-way tiles ${oneway.length}`); // ≥ 2 decoys × 4 jumps
    for (const p of oneway) assert.ok(!layout.safePath.includes(p.id), `one-way ${p.id} is on the route`);
  });
});

describe("generateLevel with meander (S-curve routes)", () => {
  const curvy: GenOptions = { ...opts, width: 40, length: 40, platforms: 30, routeMin: 9, routeMax: 30, gapMin: 2.6, gapMax: 2.8, maxTurnDeg: 85, meander: true, seed: 2 };
  const layout = generateLevel(curvy);
  const route = layout.safePath.map((id) => layout.platforms.find((p) => p.id === id)!);

  it("keeps every route jump within 2.6–2.8 m, diagonals included", () => {
    const steps = [layout.start, ...route.map((p) => ({ x: p.x, z: p.z, width: p.size, depth: p.size })), layout.exit];
    for (let i = 1; i < steps.length; i++) {
      const g = edgeGap(steps[i - 1], steps[i]);
      assert.ok(g >= 2.6 - 0.08 && g <= 2.8 + 1e-9, `step ${i}: ${g.toFixed(2)} m`); // 5 cm rounding
    }
  });

  it("wanders sideways instead of running straight", () => {
    const xs = route.map((p) => p.x);
    assert.ok(Math.max(...xs) - Math.min(...xs) >= 10, `route spans only ${(Math.max(...xs) - Math.min(...xs)).toFixed(1)} m across`);
  });
});

import { describe, it } from "node:test";
import assert from "node:assert/strict";

import { generateLevel, type GenOptions } from "../scripts/generate-level.js";
import { edgeGap, PLAYER_REACH_M, reachesExit, validateLayout, type Rect } from "../src/logic/layout.js";

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

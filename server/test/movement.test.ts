import { describe, it } from "node:test";
import assert from "node:assert/strict";

import { isMovePlausible, type MoveLimits } from "../src/logic/movement.js";

// Test numbers only (DESIGN.md §3.3 draft: run 4.5 m/s). Real values come from config.
const limits: MoveLimits = { maxHorizontalSpeed: 4.5, maxVerticalSpeed: 12, tolerance: 0.25 };
const origin = { x: 0, y: 0, z: 0 };

describe("isMovePlausible", () => {
  it("accepts standing still", () => {
    assert.equal(isMovePlausible(origin, origin, 100, limits), true);
  });

  it("accepts a normal run step (4.5 m/s for 100 ms = 0.45 m)", () => {
    assert.equal(isMovePlausible(origin, { x: 0.45, y: 0, z: 0 }, 100, limits), true);
  });

  it("uses the diagonal distance, not x and z separately", () => {
    // 0.4 m on x and 0.4 m on z = 0.566 m in 100 ms, just over the allowed 4.5 × 1.25 × 0.1 = 0.5625 m.
    assert.equal(isMovePlausible(origin, { x: 0.4, y: 0, z: 0.4 }, 100, limits), false);
  });

  it("accepts a step slightly over the limit but inside the tolerance", () => {
    assert.equal(isMovePlausible(origin, { x: 0.55, y: 0, z: 0 }, 100, limits), true);
  });

  it("rejects a teleport", () => {
    assert.equal(isMovePlausible(origin, { x: 10, y: 0, z: 0 }, 100, limits), false);
  });

  it("accepts a fast vertical move (falling) that would be too fast horizontally", () => {
    assert.equal(isMovePlausible(origin, { x: 0, y: -1.2, z: 0 }, 100, limits), true);
  });

  it("rejects a vertical teleport", () => {
    assert.equal(isMovePlausible(origin, { x: 0, y: 5, z: 0 }, 100, limits), false);
  });

  it("rejects a zero or negative time step", () => {
    assert.equal(isMovePlausible(origin, origin, 0, limits), false);
    assert.equal(isMovePlausible(origin, origin, -16, limits), false);
  });

  it("rejects non-finite coordinates", () => {
    assert.equal(isMovePlausible(origin, { x: NaN, y: 0, z: 0 }, 100, limits), false);
    assert.equal(isMovePlausible(origin, { x: 0, y: Infinity, z: 0 }, 100, limits), false);
  });
});

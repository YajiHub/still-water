import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";

import { edgeGap, PLAYER_REACH_M, reachesExit, validateLayout, type LevelLayout } from "../src/logic/layout.js";

const levelsDir = new URL("../src/levels/", import.meta.url);

// Smallest valid layout: start → one platform → exit, 2 m gaps.
function tiny(): LevelLayout {
  return {
    id: "test",
    name: "Test",
    water: { y: 0, minX: -3, maxX: 3, minZ: 0, maxZ: 8 },
    platformTopY: 0.5,
    exitTopY: 1.1,
    start: { x: 0, z: -2, width: 6, depth: 4 },
    exit: { x: 0, z: 8, width: 6, depth: 4 },
    platforms: [{ id: 1, x: 0, z: 3, size: 2 }],
    safePath: [1],
  };
}

describe("edgeGap", () => {
  it("measures edge to edge, not centre to centre", () => {
    // centres 4 m apart, each 2 m wide → 2 m of water between them
    assert.equal(edgeGap({ x: 0, z: 0, width: 2, depth: 2 }, { x: 0, z: 4, width: 2, depth: 2 }), 2);
  });

  it("is 0 when the rectangles overlap", () => {
    assert.equal(edgeGap({ x: 0, z: 0, width: 2, depth: 2 }, { x: 1, z: 1, width: 2, depth: 2 }), 0);
  });

  it("uses the diagonal for corner-to-corner gaps", () => {
    assert.equal(edgeGap({ x: 0, z: 0, width: 2, depth: 2 }, { x: 5, z: 6, width: 2, depth: 2 }), 5);
  });
});

describe("validateLayout", () => {
  it("accepts a valid layout", () => {
    assert.deepEqual(validateLayout(tiny()), []);
  });

  it("rejects duplicate platform ids", () => {
    const l = tiny();
    l.platforms.push({ id: 1, x: 0, z: 6, size: 2 });
    assert.match(validateLayout(l).join("\n"), /duplicate platform id 1/);
  });

  it("rejects a platform with size 0", () => {
    const l = tiny();
    l.platforms[0].size = 0;
    assert.match(validateLayout(l).join("\n"), /platform 1: size must be > 0/);
  });

  it("rejects an exit that can't be reached by jumping", () => {
    const l = tiny();
    l.exit.z = 20;
    assert.match(validateLayout(l).join("\n"), /exit is not reachable/);
  });

  it("rejects an exit lower than the platforms (it must be too tall to climb from the water)", () => {
    const l = tiny();
    l.exitTopY = 0.4;
    assert.match(validateLayout(l).join("\n"), /exitTopY must be above platformTopY/);
  });

  it("rejects an empty safe path", () => {
    const l = tiny();
    l.safePath = [];
    assert.match(validateLayout(l).join("\n"), /safePath is empty/);
  });

  it("rejects a safe path with an unknown platform id", () => {
    const l = tiny();
    l.safePath = [1, 9];
    assert.match(validateLayout(l).join("\n"), /safePath: no platform 9/);
  });

  it("rejects a safe path with a step too far to jump", () => {
    const l = tiny();
    l.platforms.push({ id: 2, x: 0, z: 8, size: 2 }); // 3 m past platform 1
    l.safePath = [1, 2];
    assert.match(validateLayout(l).join("\n"), /safePath: 1 → 2 is 3\.00 m/);
  });

  it("rejects a safe path that doesn't start next to the start deck", () => {
    const l = tiny();
    l.platforms.push({ id: 2, x: 0, z: 8, size: 2 });
    l.safePath = [2];
    assert.match(validateLayout(l).join("\n"), /safePath: start → 2/);
  });

  it("rejects a platform outside the water", () => {
    const l = tiny();
    l.platforms[0].x = 10;
    assert.match(validateLayout(l).join("\n"), /platform 1: outside the water/);
  });
});

describe("reachesExit", () => {
  it("is true when the platforms form a chain", () => {
    assert.equal(reachesExit(tiny(), [], PLAYER_REACH_M), true);
  });

  it("is false when the only platform is skipped", () => {
    assert.equal(reachesExit(tiny(), [1], PLAYER_REACH_M), false);
  });
});

describe("level01 (tutorial: no fakes, so the other platforms must be dead ends)", () => {
  const layout = JSON.parse(readFileSync(new URL("level01.json", levelsDir), "utf8")) as LevelLayout;

  for (const id of layout.safePath) {
    it(`can't reach the exit without path platform ${id}, even with a ${PLAYER_REACH_M} m jump`, () => {
      assert.equal(reachesExit(layout, [id], PLAYER_REACH_M), false);
    });
  }
});

describe("level files", () => {
  const files = readdirSync(levelsDir).filter((f) => f.endsWith(".json"));

  it("includes level01.json", () => {
    assert.ok(files.includes("level01.json"));
  });

  for (const file of files) {
    it(`${file} is valid`, () => {
      const layout = JSON.parse(readFileSync(new URL(file, levelsDir), "utf8")) as LevelLayout;
      assert.deepEqual(validateLayout(layout), []);
    });
  }
});

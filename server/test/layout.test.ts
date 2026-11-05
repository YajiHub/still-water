import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";

import { edgeGap, validateLayout, type LevelLayout } from "../src/logic/layout.js";

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
    exit: { x: 0, z: 10, width: 6, depth: 4 },
    platforms: [{ id: 1, x: 0, z: 4, size: 2 }],
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

  it("rejects a platform outside the water", () => {
    const l = tiny();
    l.platforms[0].x = 10;
    assert.match(validateLayout(l).join("\n"), /platform 1: outside the water/);
  });
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

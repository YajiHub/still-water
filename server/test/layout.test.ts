import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";

import { easiestRoute, edgeGap, PLAYER_REACH_M, reachesExit, validateLayout, type LevelLayout } from "../src/logic/layout.js";

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

describe("per-level max gap (edge-of-the-jump levels)", () => {
  // start → 1 → exit with a 3.0 m step: too far for the default 2.8 m.
  function long(): LevelLayout {
    const l = tiny();
    l.platforms[0].z = 4;
    l.exit.z = 9;
    l.water.maxZ = 7;
    return l;
  }

  it("rejects a 3.0 m step by default", () => {
    assert.match(validateLayout(long()).join("\n"), /safePath: start → 1 is 3\.00 m \(max 2\.8\)/);
  });

  it("accepts it when the level allows up to 3.1 m", () => {
    assert.deepEqual(validateLayout({ ...long(), maxGap: 3.1 }), []);
  });

  it("never allows more than 3.1 m (the jump can't make it)", () => {
    assert.match(validateLayout({ ...long(), maxGap: 3.5 }).join("\n"), /maxGap 3\.5 is beyond the jump \(max 3\.1\)/);
  });
});

describe("easiestRoute", () => {
  // Two ways across: a short way with one 2.7 m jump, a longer way with only 2.2 m jumps.
  function twoWays(): LevelLayout {
    return {
      id: "test",
      name: "Two ways",
      water: { y: 0, minX: -10, maxX: 10, minZ: 0, maxZ: 12 },
      platformTopY: 0.5,
      exitTopY: 1.1,
      start: { x: 0, z: -2, width: 4, depth: 4 },
      exit: { x: 0, z: 14, width: 4, depth: 4 },
      platforms: [
        { id: 1, x: 0, z: 3.7, size: 2 }, // 2.7 m from the start
        { id: 2, x: 0, z: 8.4, size: 2 }, // then 2.7 m, then 2.6 m to the exit
        { id: 3, x: -4.2, z: 2.2, size: 2 }, // the side way: 2.2 m jumps only
        { id: 4, x: -4.2, z: 6.4, size: 2 },
        { id: 5, x: -2.0, z: 9.8, size: 2 },
      ],
      safePath: [],
    };
  }

  it("picks the way whose longest jump is shortest", () => {
    assert.deepEqual(easiestRoute(twoWays()), [3, 4, 5]);
  });

  it("is null when the exit can't be reached within the level's max gap", () => {
    const l = twoWays();
    l.exit.z = 30;
    assert.equal(easiestRoute(l), null);
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

// An 11 m channel: only a ferry (2 m tile sliding 5 m along z) gets you across.
function ferry(): LevelLayout {
  return {
    id: "test",
    name: "Ferry",
    water: { y: 0, minX: -3, maxX: 3, minZ: 0, maxZ: 11 },
    platformTopY: 0.5,
    exitTopY: 1.1,
    start: { x: 0, z: -2, width: 6, depth: 4 },
    exit: { x: 0, z: 13, width: 6, depth: 4 },
    platforms: [{ id: 1, x: 0, z: 3, size: 2, move: { dx: 0, dz: 5, seconds: 3, pause: 1 } }],
    safePath: [1],
  };
}

describe("ferries (moving route platforms)", () => {
  it("accepts a ferry that carries you across a channel", () => {
    assert.deepEqual(validateLayout(ferry()), []);
  });

  it("counts the whole strip it slides across: the exit is reachable", () => {
    assert.equal(reachesExit(ferry(), [], PLAYER_REACH_M), true);
  });

  it("is needed: without it the channel can't be crossed", () => {
    assert.equal(reachesExit(ferry(), [1], PLAYER_REACH_M), false);
  });

  it("would not reach the exit if it stood still", () => {
    const l = ferry();
    delete l.platforms[0].move;
    assert.match(validateLayout(l).join("\n"), /exit is not reachable/);
  });

  it("rejects a moving platform that isn't on the route (decoys never move)", () => {
    const l = { ...ferry(), oneDryRoute: true };
    l.platforms.push({ id: 2, x: -2, z: 9, size: 1, move: { dx: 1, dz: 0, seconds: 2, pause: 0 } });
    assert.match(validateLayout(l).join("\n"), /platform 2: only route platforms may move/);
  });

  it("allows a moving decoy on a hand-tuned level (no one-dry-route proof)", () => {
    const l = ferry();
    l.platforms.push({ id: 2, x: -2, z: 9, size: 1, move: { dx: 1, dz: 0, seconds: 2, pause: 0 } });
    assert.doesNotMatch(validateLayout(l).join("\n"), /only route platforms may move/);
  });

  it("rejects a ferry with no travel time", () => {
    const l = ferry();
    l.platforms[0].move!.seconds = 0;
    assert.match(validateLayout(l).join("\n"), /platform 1: move\.seconds must be > 0/);
  });

  it("rejects a negative pause", () => {
    const l = ferry();
    l.platforms[0].move!.pause = -1;
    assert.match(validateLayout(l).join("\n"), /platform 1: move\.pause must be ≥ 0/);
  });

  it("rejects a ferry that doesn't go anywhere", () => {
    const l = ferry();
    l.platforms[0].move!.dz = 0;
    assert.match(validateLayout(l).join("\n"), /platform 1: move must go somewhere/);
  });

  it("rejects a platform in a ferry's way (it would graze or shove you)", () => {
    const l = ferry();
    l.platforms.push({ id: 2, x: 2.2, z: 6, size: 1 }); // 0.2 m beside the strip it slides along
    assert.match(validateLayout(l).join("\n"), /platform 2 is in ferry 1's way/);
  });

  it("rejects a ferry that slides out of the water", () => {
    const l = ferry();
    l.platforms[0].move!.dz = 9;
    assert.match(validateLayout(l).join("\n"), /platform 1: outside the water/);
  });
});

describe("level files", () => {
  const files = readdirSync(levelsDir).filter((f) => f.endsWith(".json"));
  const load = (file: string) => JSON.parse(readFileSync(new URL(file, levelsDir), "utf8")) as LevelLayout;

  it("includes level01.json", () => {
    assert.ok(files.includes("level01.json"));
  });

  it("level01 is a one-dry-route level (tutorial: no fakes, so the rest are dead ends)", () => {
    assert.equal(load("level01.json").oneDryRoute, true);
  });

  for (const file of files) {
    const layout = load(file);

    it(`${file} is valid`, () => {
      assert.deepEqual(validateLayout(layout), []);
    });

    // No fakes: the other platforms must be dead ends, or the route could be skipped.
    if (layout.oneDryRoute) {
      for (const id of layout.safePath) {
        it(`${file}: can't reach the exit without path platform ${id}, even with a ${PLAYER_REACH_M} m jump`, () => {
          assert.equal(reachesExit(layout, [id], PLAYER_REACH_M), false);
        });
      }
    }
  }
});

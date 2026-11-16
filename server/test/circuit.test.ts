import { describe, it } from "node:test";
import assert from "node:assert/strict";
import {
  CIRCUIT_CONFIG,
  addPlayer,
  applyPosition,
  buildCircuit,
  createCircuitState,
  handleDisconnect,
  markReady,
  push,
  spawnFor,
  tickCircuit,
  toSnapshot,
  type CircuitState,
} from "../src/logic/circuit.js";
import { CIRCUIT_LAYOUTS, CIRCUIT_ROOMS } from "../src/logic/circuitLevels.js";

const fixed = () => 0.5;

/** A running circuit with these players, everyone ready. */
function running(ids: string[]): CircuitState {
  const s = createCircuitState(CIRCUIT_ROOMS, CIRCUIT_CONFIG, fixed);
  for (const id of ids) addPlayer(s, id);
  for (const id of ids) markReady(s, id);
  tickCircuit(s, CIRCUIT_CONFIG.lobbySeconds + 0.1, CIRCUIT_CONFIG, fixed); // lobby → countdown (or straight there if full)
  tickCircuit(s, CIRCUIT_CONFIG.countdownSeconds + 0.1, CIRCUIT_CONFIG, fixed);
  assert.equal(s.phase, "running");
  return s;
}

/** Puts a player somewhere without the speed check (as if they got there legitimately). */
function place(s: CircuitState, id: string, p: { x: number; y: number; z: number; yaw?: number }, nowMs = 1000): boolean {
  s.players.get(id)!.lastPosAtMs = null;
  return applyPosition(s, id, p, nowMs);
}

const doorOf = (s: CircuitState, room: number) => {
  const r = s.rooms[room];
  return { x: r.door.x, y: r.exitTopY + 0.05, z: (r.doorZone.minZ + r.doorZone.maxZ) / 2 };
};

const waterOf = (s: CircuitState, room: number) => {
  const w = s.rooms[room].water;
  return { x: (w.minX + w.maxX) / 2, y: -0.5, z: w.minZ + 2 };
};

describe("buildCircuit", () => {
  it("joins the five layouts end to end with the doors lined up", () => {
    assert.equal(CIRCUIT_ROOMS.length, 5);
    assert.equal(CIRCUIT_LAYOUTS[0].id, "level01");
    for (let i = 1; i < CIRCUIT_ROOMS.length; i++) {
      const prev = CIRCUIT_ROOMS[i - 1];
      const room = CIRCUIT_ROOMS[i];
      assert.ok(Math.abs(room.minZ - prev.maxZ) < 1e-6, `room ${i} starts where room ${i - 1} ends`);
      assert.ok(Math.abs(room.spawn.x - prev.door.x) < 1e-6, `room ${i} start lines up with the door`);
      assert.ok(room.water.minZ > prev.water.maxZ, "rooms don't overlap");
    }
  });

  it("keeps room 1 where Level01 is (the online scene)", () => {
    const r0 = buildCircuit(CIRCUIT_LAYOUTS)[0];
    assert.equal(r0.offsetX, 0);
    assert.equal(r0.offsetZ, 0);
    assert.deepEqual(r0.spawn, { x: 0, y: 0.55, z: -2 });
  });
});

describe("lobby", () => {
  it("waits for more players, then starts with the ones it has", () => {
    const s = createCircuitState(CIRCUIT_ROOMS, CIRCUIT_CONFIG, fixed);
    addPlayer(s, "a");
    addPlayer(s, "b");
    markReady(s, "a");
    tickCircuit(s, 30, CIRCUIT_CONFIG, fixed);
    assert.equal(s.phase, "waiting", "b isn't ready");
    markReady(s, "b", 1);
    tickCircuit(s, CIRCUIT_CONFIG.lobbySeconds - 1, CIRCUIT_CONFIG, fixed);
    assert.equal(s.phase, "waiting", "still filling");
    tickCircuit(s, 1.5, CIRCUIT_CONFIG, fixed);
    assert.equal(s.phase, "countdown");
    assert.equal(s.players.get("b")!.body, 1);
  });

  it("starts at once when full and turns away a fifth player", () => {
    const s = createCircuitState(CIRCUIT_ROOMS, CIRCUIT_CONFIG, fixed);
    for (const id of ["a", "b", "c", "d"]) assert.ok(addPlayer(s, id));
    assert.equal(addPlayer(s, "e"), false);
    for (const id of ["a", "b", "c", "d"]) markReady(s, id);
    tickCircuit(s, 0.05, CIRCUIT_CONFIG, fixed);
    assert.equal(s.phase, "countdown");
  });
});

describe("open doors: a race", () => {
  it("lets you through a door on your own, scoring by arrival order per door", () => {
    const s = running(["a", "b"]);
    assert.ok(place(s, "a", doorOf(s, 0)));
    assert.equal(s.players.get("a")!.score, 3);
    assert.ok(place(s, "a", s.rooms[1].spawn), "no waiting: room 2 is open");
    assert.equal(s.players.get("a")!.room, 1);
    assert.ok(place(s, "b", doorOf(s, 0)));
    assert.equal(s.players.get("b")!.score, 2, "second through door 1");
  });

  it("scores each door once", () => {
    const s = running(["a", "b"]);
    place(s, "a", doorOf(s, 0));
    place(s, "a", { ...doorOf(s, 0), x: doorOf(s, 0).x + 1 });
    assert.equal(s.players.get("a")!.score, 3);
  });

  it("gives finishing places, and the team bonus when the last one is through", () => {
    const s = running(["a", "b"]);
    for (let r = 0; r < s.rooms.length; r++) place(s, "a", doorOf(s, r));
    const a = s.players.get("a")!;
    assert.equal(a.place, 1);
    assert.equal(s.phase, "running", "b is still out there");
    for (let r = 0; r < s.rooms.length; r++) place(s, "b", doorOf(s, r));
    assert.equal(s.players.get("b")!.place, 2);
    assert.equal(s.phase, "finished");
    assert.equal(s.result, "cleared");
    assert.equal(a.score, 5 * 3 + CIRCUIT_CONFIG.points.finish[0] + CIRCUIT_CONFIG.points.teamClear);
  });

  it("finishes when the only one still running leaves", () => {
    const s = running(["a", "b"]);
    for (let r = 0; r < s.rooms.length; r++) place(s, "a", doorOf(s, r));
    handleDisconnect(s, "b");
    assert.equal(s.result, "cleared");
  });
});

describe("each room's Listener is as fast as in its solo level", () => {
  it("uses the per-level tuning, getting faster room by room", () => {
    const hunt = CIRCUIT_ROOMS.map((r) => r.listener.huntSpeed);
    assert.deepEqual(hunt, [8, 11, 14, 18, 20]);
    assert.equal(CIRCUIT_ROOMS[0].listener.alertDelaySeconds, 0.6);
    assert.equal(CIRCUIT_ROOMS[3].listener.alertDelaySeconds, 0);
    assert.equal(CIRCUIT_ROOMS[4].listener.roamSpeed, 1);
    assert.equal(CIRCUIT_ROOMS[4].listener.reachedDistance, CIRCUIT_CONFIG.listener.reachedDistance, "defaults fill the rest");
  });

  it("hunts at its room's speed (no alert delay in room 4)", () => {
    const s = running(["a", "b"]);
    const l = s.listeners[3];
    const from = { x: l.x, z: l.z };
    place(s, "a", { x: l.x + 10, y: -0.5, z: l.z });
    tickCircuit(s, 0.25, CIRCUIT_CONFIG, fixed); // roaming → alerted (0 s) → hunting
    tickCircuit(s, 0.25, CIRCUIT_CONFIG, fixed);
    const moved = Math.hypot(s.listeners[3].x - from.x, s.listeners[3].z - from.z);
    assert.ok(moved >= 18 * 0.25 * 0.9, `moved ${moved.toFixed(2)} m in a quarter second`);
  });
});

describe("a Listener in every room", () => {
  it("hears a splash in its own room only", () => {
    const s = running(["a", "b"]);
    place(s, "b", waterOf(s, 1));
    assert.equal(s.listeners[1].state, "alerted");
    assert.equal(s.listeners[0].state, "roaming");
    assert.equal(s.players.get("b")!.score, CIRCUIT_CONFIG.points.splash);
  });

  it("catches a swimmer in its room: back to that room's start, and tells where it happened", () => {
    const s = running(["a", "b"]);
    const l = s.listeners[2];
    place(s, "a", { x: l.x + 0.5, y: -0.5, z: l.z });
    const caught = [...tickCircuit(s, 1, CIRCUIT_CONFIG, fixed), ...tickCircuit(s, 0.2, CIRCUIT_CONFIG, fixed)];
    assert.equal(caught.length, 1);
    assert.equal(caught[0].room, 2);
    assert.ok(Math.abs(caught[0].caughtX - (l.x + 0.5)) < 1.5);
    const a = s.players.get("a")!;
    assert.deepEqual({ x: a.x, y: a.y, z: a.z }, spawnFor(s.rooms[2], a.slot));
    assert.equal(a.catches, 1);
    assert.equal(a.score, CIRCUIT_CONFIG.points.splash + CIRCUIT_CONFIG.points.caught);
  });
});

describe("pushing", () => {
  it("shoves the player in front of you, then waits for the cooldown", () => {
    const s = running(["a", "b"]);
    place(s, "a", { x: 0, y: 0.55, z: -2, yaw: 0 }); // facing +z
    place(s, "b", { x: 0, y: 0.55, z: -1, yaw: 0 });
    const shove = push(s, "a", 5000);
    assert.ok(shove);
    assert.equal(shove!.target, "b");
    assert.ok(shove!.dz > 0.9, "away from the pusher");
    assert.equal(push(s, "a", 5100), null, "cooldown");
    assert.ok(push(s, "a", 5000 + CIRCUIT_CONFIG.push.cooldownSeconds * 1000 + 1));
  });

  it("doesn't reach someone behind you or too far away", () => {
    const s = running(["a", "b"]);
    place(s, "a", { x: 0, y: 0.55, z: -2, yaw: 0 });
    place(s, "b", { x: 0, y: 0.55, z: -3, yaw: 0 });
    assert.equal(push(s, "a", 5000), null, "behind");
    place(s, "b", { x: 0, y: 0.55, z: 1, yaw: 0 });
    assert.equal(push(s, "a", 9000), null, "3 m away");
  });

  it("lets the pushed player fly faster than running for a moment", () => {
    const s = running(["a", "b"]);
    place(s, "a", { x: 0, y: 0.55, z: -2, yaw: 0 }, 5000);
    place(s, "b", { x: 0, y: 0.55, z: -1, yaw: 0 }, 5000);
    push(s, "a", 5000);
    // 1 m in 0.1 s = 10 m/s: over the running limit (6.5 × 1.5), allowed while being pushed.
    assert.ok(applyPosition(s, "b", { x: 0, y: 0.55, z: 0 }, 5100));
  });
});

describe("the clock and leaving", () => {
  it("ends when time runs out", () => {
    const s = running(["a", "b"]);
    tickCircuit(s, CIRCUIT_CONFIG.matchSeconds + 1, CIRCUIT_CONFIG, fixed);
    assert.equal(s.phase, "finished");
    assert.equal(s.result, "timeout");
  });

  it("ends when everyone leaves", () => {
    const s = running(["a", "b"]);
    handleDisconnect(s, "a");
    assert.equal(s.phase, "running", "b plays on");
    handleDisconnect(s, "b");
    assert.equal(s.result, "abandoned");
  });

  it("puts what the client needs in the snapshot", () => {
    const s = running(["a", "b"]);
    const snap = toSnapshot(s);
    assert.equal(snap.roomCount, 5);
    assert.equal(snap.listeners.length, 5);
    assert.ok(["room", "reached", "place", "score", "body"].every((k) => k in snap.players[0]));
  });
});

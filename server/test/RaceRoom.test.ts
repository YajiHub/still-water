import { describe, it, before, after, beforeEach } from "node:test";
import assert from "node:assert/strict";
import { ColyseusTestServer, boot } from "@colyseus/testing";
import type { Room as SDKRoom } from "@colyseus/sdk";

import appConfig from "../src/app.config.js";
import { RACE_CONFIG, type RaceSnapshot } from "../src/logic/race.js";

function waitForSnapshot(
  client: SDKRoom,
  predicate: (snap: RaceSnapshot) => boolean,
  timeoutMs = 6000,
): Promise<RaceSnapshot> {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      unbind();
      reject(new Error(`Timed out waiting for snapshot after ${timeoutMs}ms`));
    }, timeoutMs);
    const unbind = client.onMessage<string>("snapshot", (raw) => {
      const snap = JSON.parse(raw) as RaceSnapshot;
      if (predicate(snap)) {
        clearTimeout(timer);
        unbind();
        resolve(snap);
      }
    });
  });
}

describe("RaceRoom integration", () => {
  let colyseus: ColyseusTestServer<typeof appConfig>;

  before(async () => { colyseus = await boot(appConfig); });
  after(async () => { await colyseus.shutdown(); });

  beforeEach(async () => {
    await colyseus.cleanup();
  });

  async function startTwoPlayers() {
    const room = await colyseus.createRoom("race", {});
    const c1 = await colyseus.connectTo(room);
    const c2 = await colyseus.connectTo(room);
    const racing1 = waitForSnapshot(c1, (s) => s.phase === "racing");
    const racing2 = waitForSnapshot(c2, (s) => s.phase === "racing");
    c1.send("ready");
    c2.send("ready");
    await Promise.all([racing1, racing2]);
    return { room, c1, c2 };
  }

  it("counts down once both players are ready, rejects teleports, and finishes at the door", async () => {
    const { c1, c2 } = await startTwoPlayers();

    // First valid packet establishes baseline, teleport is ignored, then a plausible step succeeds.
    c1.send("pos", JSON.stringify({ x: 0, y: 0.55, z: -2, yaw: 0 }));
    c1.send("pos", { x: 10, y: 0.55, z: 10, yaw: 0 });
    c1.send("pos", { x: 0, y: 0.55, z: -1.9, yaw: 0 });
    const afterStep = await waitForSnapshot(c1, (s) => s.players.some((p) => p.id === c1.sessionId && Math.abs(p.z - -1.9) < 0.001));
    const p1 = afterStep.players.find((p) => p.id === c1.sessionId)!;
    assert.equal(p1.x, 0);

    const finished = waitForSnapshot(c2, (s) => s.phase === "finished");
    c2.send("pos", { x: RACE_CONFIG.door.x, y: RACE_CONFIG.door.y, z: RACE_CONFIG.door.z, yaw: 0 });
    const endSnap = await finished;
    assert.equal(endSnap.winner, c2.sessionId);
  });

  it("sends respawn and applies penalty when caught in the water", async () => {
    const { c1 } = await startTwoPlayers();
    const respawned = c1.waitForMessage("respawn", 4000);
    const penalized = waitForSnapshot(c1, (s) => {
      const me = s.players.find((p) => p.id === c1.sessionId);
      return Boolean(me && me.penalty > 0);
    });

    // Drop directly onto Listener start position in deep water.
    c1.send("pos", { x: RACE_CONFIG.listener.startX, y: -0.3, z: RACE_CONFIG.listener.startZ, yaw: 0 });

    const [rawRespawn, snap] = await Promise.all([respawned, penalized]);
    const respawn = JSON.parse(String(rawRespawn)) as { x: number; y: number; z: number };
    assert.deepEqual(respawn, RACE_CONFIG.spawn);
    const me = snap.players.find((p) => p.id === c1.sessionId)!;
    assert.equal(me.splashes, 1);
    assert.ok(me.penalty > 0);
  });

  it("awards forfeit win when an opponent disconnects mid-race", async () => {
    const { c1, c2 } = await startTwoPlayers();
    const forfeit = waitForSnapshot(c1, (s) => s.phase === "finished");
    await c2.leave(true);
    const snap = await forfeit;
    assert.equal(snap.winner, c1.sessionId);
    assert.match(snap.reason, /disconnected/i);
  });
});

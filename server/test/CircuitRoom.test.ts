import { describe, it, before, after, beforeEach } from "node:test";
import assert from "node:assert/strict";
import { ColyseusTestServer, boot } from "@colyseus/testing";
import type { Room as SDKRoom } from "@colyseus/sdk";

import appConfig from "../src/app.config.js";
import type { CircuitRoom as Geometry, CircuitSnapshot } from "../src/logic/circuit.js";

function waitFor<T>(client: SDKRoom, type: string, predicate: (msg: T) => boolean, timeoutMs = 25000): Promise<T> {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      unbind();
      reject(new Error(`Timed out waiting for "${type}"`));
    }, timeoutMs);
    const unbind = client.onMessage<string>(type, (raw) => {
      const msg = JSON.parse(raw) as T;
      if (predicate(msg)) {
        clearTimeout(timer);
        unbind();
        resolve(msg);
      }
    });
  });
}

describe("CircuitRoom integration", () => {
  let colyseus: ColyseusTestServer<typeof appConfig>;

  before(async () => { colyseus = await boot(appConfig); });
  after(async () => { await colyseus.shutdown(); });
  beforeEach(async () => { await colyseus.cleanup(); });

  it("sends the geometry on request, starts a full team at once, scores doors and relays pushes", async () => {
    const room = await colyseus.createRoom("circuit", {});
    const clients: SDKRoom[] = [];
    for (let i = 0; i < 4; i++) clients.push(await colyseus.connectTo(room));

    const geometry = waitFor<{ rooms: Geometry[] }>(clients[0], "circuit", (g) => g.rooms.length === 5);
    clients[0].send("geometry");
    const rooms = (await geometry).rooms;

    const running = waitFor<CircuitSnapshot>(clients[0], "snapshot", (s) => s.phase === "running");
    clients.forEach((c, i) => c.send("ready", JSON.stringify({ body: i % 2 })));
    const snap = await running;
    assert.equal(snap.players.length, 4);
    assert.deepEqual(snap.players.map((p) => p.body).sort(), [0, 0, 1, 1]);

    // Two players face each other at the start: a shoves b.
    const [a, b] = clients;
    a.send("pos", JSON.stringify({ x: 0, y: 0.55, z: -2, yaw: 0 }));
    b.send("pos", JSON.stringify({ x: 0, y: 0.55, z: -1, yaw: 180 }));
    await waitFor<CircuitSnapshot>(a, "snapshot", (s) => s.players.some((p) => p.id === b.sessionId && Math.abs(p.z - -1) < 1e-3));
    const pushed = waitFor<{ dx: number; dz: number; speed: number }>(b, "pushed", () => true);
    a.send("push");
    const shove = await pushed;
    assert.ok(shove.dz > 0.9 && shove.speed > 0);

    // Doors are open: a's arrival at door 1 scores 3 at once.
    const r0 = rooms[0];
    const door = { x: r0.door.x, y: r0.exitTopY + 0.05, z: (r0.doorZone.minZ + r0.doorZone.maxZ) / 2 };
    clients[2].send("pos", JSON.stringify({ ...door, yaw: 0 }));
    const scored = await waitFor<CircuitSnapshot>(a, "snapshot", (s) => s.players.some((p) => p.id === clients[2].sessionId && p.score === 3));
    assert.equal(scored.players.find((p) => p.id === clients[2].sessionId)!.reached, 0);
  });
});

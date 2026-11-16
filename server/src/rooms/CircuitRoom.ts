import { Room, type Client } from "colyseus";
import {
  CIRCUIT_CONFIG,
  addPlayer,
  applyPosition,
  createCircuitState,
  handleDisconnect,
  markReady,
  push,
  tickCircuit,
  toSnapshot,
  type CircuitState,
} from "../logic/circuit.js";
import { CIRCUIT_ROOMS } from "../logic/circuitLevels.js";

const TICK_MS = 50;
const SNAPSHOT_EVERY_MS = 100;

/**
 * 2–4 player co-op race circuit room (NoState, JSON strings like RaceRoom, so Android IL2CPP needs no schema code).
 * Sends "circuit" (the rooms' geometry) to each joiner and again on "geometry". Receives "ready" ({ body }), "pos"
 * and "push". Sends "respawn" to a caught player and "pushed" ({ dx, dz, speed }) to a pushed one. Broadcasts
 * "caught" ({ id, x, z, room }: every client plays the catch there), "shove" ({ pusher, target }) and "snapshot"
 * at 10 Hz (and at once on any phase change or catch). Joining is closed once the countdown starts.
 */
export class CircuitRoom extends Room {
  maxClients = CIRCUIT_CONFIG.maxPlayers;
  private circuit: CircuitState = createCircuitState(CIRCUIT_ROOMS);
  private snapshotAccumMs = 0;

  onCreate(): void {
    this.circuit = createCircuitState(CIRCUIT_ROOMS);
    this.snapshotAccumMs = 0;

    this.onMessage("ready", (client, raw) => {
      markReady(this.circuit, client.sessionId, parseBody(raw));
      this.broadcastSnapshot();
    });

    // The client asks once its handlers are registered (a message sent at join can arrive before they are).
    this.onMessage("geometry", (client) => {
      client.send("circuit", JSON.stringify({ rooms: this.circuit.rooms }));
    });

    this.onMessage("push", (client) => {
      const shove = push(this.circuit, client.sessionId, Date.now());
      if (!shove) return;
      const target = this.clients.find((c) => c.sessionId === shove.target);
      if (target) target.send("pushed", JSON.stringify({ dx: shove.dx, dz: shove.dz, speed: shove.speed }));
      this.broadcast("shove", JSON.stringify({ pusher: shove.pusher, target: shove.target }));
    });

    this.onMessage("pos", (client, raw) => {
      const pos = parsePos(raw);
      if (!pos) return;
      const phase = this.circuit.phase;
      applyPosition(this.circuit, client.sessionId, pos, Date.now());
      if (this.circuit.phase !== phase) this.broadcastSnapshot();
    });

    this.setTimestep((dtMs) => {
      const phase = this.circuit.phase;
      const respawns = tickCircuit(this.circuit, dtMs / 1000);
      if (this.circuit.phase !== phase && phase === "waiting") void this.lock();
      for (const r of respawns) {
        const target = this.clients.find((c) => c.sessionId === r.id);
        if (target) target.send("respawn", JSON.stringify({ x: r.x, y: r.y, z: r.z }));
        this.broadcast("caught", JSON.stringify({ id: r.id, x: r.caughtX, z: r.caughtZ, room: r.room }));
      }
      this.snapshotAccumMs += dtMs;
      if (this.circuit.phase !== phase || respawns.length > 0 || this.snapshotAccumMs >= SNAPSHOT_EVERY_MS) {
        this.snapshotAccumMs = 0;
        this.broadcastSnapshot();
      }
    }, TICK_MS);
  }

  onJoin(client: Client): void {
    addPlayer(this.circuit, client.sessionId);
    client.send("circuit", JSON.stringify({ rooms: this.circuit.rooms }));
    if (this.circuit.players.size >= this.maxClients) void this.lock();
    this.broadcastSnapshot();
  }

  onLeave(client: Client): void {
    handleDisconnect(this.circuit, client.sessionId);
    if (this.circuit.phase === "waiting") void this.unlock();
    this.broadcastSnapshot();
  }

  private broadcastSnapshot(): void {
    this.broadcast("snapshot", JSON.stringify(toSnapshot(this.circuit)));
  }
}

function parseBody(raw: unknown): number | undefined {
  let obj: unknown = raw;
  if (typeof raw === "string") {
    try {
      obj = JSON.parse(raw);
    } catch {
      return undefined;
    }
  }
  if (!obj || typeof obj !== "object") return undefined;
  const body = (obj as Record<string, unknown>).body;
  return body === 0 || body === 1 ? body : undefined;
}

function parsePos(raw: unknown): { x: number; y: number; z: number; yaw?: number } | null {
  let obj: unknown = raw;
  if (typeof raw === "string") {
    try {
      obj = JSON.parse(raw);
    } catch {
      return null;
    }
  }
  if (!obj || typeof obj !== "object") return null;
  const rec = obj as Record<string, unknown>;
  if (typeof rec.x !== "number" || typeof rec.y !== "number" || typeof rec.z !== "number") return null;
  const yaw = typeof rec.yaw === "number" ? rec.yaw : undefined;
  return { x: rec.x, y: rec.y, z: rec.z, yaw };
}

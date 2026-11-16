import { Room, type Client } from "colyseus";
import {
  RACE_CONFIG,
  addPlayer,
  applyPosition,
  createRaceState,
  handleDisconnect,
  markReady,
  tickRace,
  toSnapshot,
  type RaceState,
} from "../logic/race.js";

const TICK_MS = 50;
const SNAPSHOT_EVERY_MS = 100;

/**
 * 1v1 LAN race room (NoState): receives "ready" and "pos", runs the server simulation,
 * sends "respawn" when caught, and broadcasts "snapshot" JSON strings at 10 Hz.
 */
export class RaceRoom extends Room {
  maxClients = RACE_CONFIG.maxPlayers;
  private race: RaceState = createRaceState();
  private snapshotAccumMs = 0;

  onCreate(): void {
    this.race = createRaceState();
    this.snapshotAccumMs = 0;

    this.onMessage("ready", (client) => {
      const before = this.race.phase;
      markReady(this.race, client.sessionId);
      if (this.race.phase !== before) void this.lock();
      this.broadcastSnapshot();
    });

    this.onMessage("pos", (client, raw) => {
      const pos = parsePos(raw);
      if (!pos) return;
      const before = this.race.phase;
      applyPosition(this.race, client.sessionId, pos, Date.now());
      if (this.race.phase !== before) {
        if (this.race.phase === "finished") void this.lock();
        this.broadcastSnapshot();
      }
    });

    this.setTimestep((dtMs) => {
      const before = this.race.phase;
      const respawns = tickRace(this.race, dtMs / 1000);
      for (const r of respawns) {
        const target = this.clients.find((c) => c.sessionId === r.id);
        if (target) target.send("respawn", JSON.stringify({ x: r.x, y: r.y, z: r.z }));
      }
      this.snapshotAccumMs += dtMs;
      if (this.race.phase !== before || respawns.length > 0 || this.snapshotAccumMs >= SNAPSHOT_EVERY_MS) {
        this.snapshotAccumMs = 0;
        this.broadcastSnapshot();
      }
    }, TICK_MS);
  }

  onJoin(client: Client): void {
    addPlayer(this.race, client.sessionId);
    if (this.race.players.size >= this.maxClients) void this.lock();
    this.broadcastSnapshot();
  }

  onLeave(client: Client): void {
    handleDisconnect(this.race, client.sessionId);
    if (this.race.phase === "waiting") void this.unlock();
    this.broadcastSnapshot();
  }

  private broadcastSnapshot(): void {
    this.broadcast("snapshot", JSON.stringify(toSnapshot(this.race)));
  }
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

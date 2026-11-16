// Online co-op race circuit (decision 07): 2–4 players run the five level layouts joined end to end.
// Revised 2026-11-16 after the first co-op test. The doors are open: it's a race, and you can still wait for or
// help a teammate. Each room has its own Listener, which hears and hunts whoever is swimming in it. Players can push
// each other. Points: first to each door, the finishing places, minus splashes and catches; plus a team bonus if
// everyone gets through before time runs out. Pure rules: no Colyseus imports, so node:test checks each one directly.
import type { LevelLayout } from "./layout.js";
import { isMovePlausible, type MoveLimits } from "./movement.js";

export type CircuitPhase = "waiting" | "countdown" | "running" | "finished";
export type CircuitResult = "" | "cleared" | "timeout" | "abandoned";
export type MonsterState = "roaming" | "alerted" | "hunting" | "lingering";

interface Box {
  minX: number;
  maxX: number;
  minZ: number;
  maxZ: number;
}

/** One room of the circuit, in world coordinates (the layout moved by offsetX/offsetZ). */
export interface CircuitRoom {
  index: number;
  id: string;
  name: string;
  offsetX: number;
  offsetZ: number;
  minZ: number; // south edge of the start deck
  maxZ: number; // north edge of the exit deck: the wall with the door in it
  water: Box & { surfaceY: number };
  spawn: { x: number; y: number; z: number };
  exitTopY: number;
  door: { x: number; y: number; z: number };
  doorZone: Box & { minY: number }; // standing here (out of the water) = reached this room's door
  listenerStart: { x: number; z: number };
  listener: ListenerTuning; // this room's Listener: the same numbers as the solo level (listenerTuning.json)
}

export type ListenerTuning = CircuitConfig["listener"];

export interface CircuitConfig {
  minPlayers: number;
  maxPlayers: number;
  lobbySeconds: number; // once ≥ minPlayers are all ready, wait this long for more (starts at once when full)
  countdownSeconds: number;
  matchSeconds: number;
  catchPenaltySeconds: number;
  doorZoneHalfWidth: number; // metres either side of the door that count as "at the door"
  slotSpacing: number; // players spawn side by side
  roamMargin: number;
  entryDepth: number; // below the surface by this much = in the water
  moveLimits: MoveLimits;
  push: {
    range: number; // metres
    coneDegrees: number; // half-angle in front of the pusher
    speed: number; // m/s given to the pushed player
    cooldownSeconds: number;
    graceSeconds: number; // the pushed player may move faster than the speed check allows for this long
  };
  listener: {
    roamSpeed: number;
    roamPauseSeconds: number;
    alertDelaySeconds: number;
    huntSpeed: number;
    speedPerSplash: number;
    lingerSeconds: number;
    catchDistance: number;
    reachedDistance: number;
  };
  points: { door: number[]; finish: number[]; splash: number; caught: number; teamClear: number };
}

// MovementConfig / LevelConfig defaults, as in RACE_CONFIG; the match is long enough for five rooms.
export const CIRCUIT_CONFIG: CircuitConfig = {
  minPlayers: 2,
  maxPlayers: 4,
  lobbySeconds: 15,
  countdownSeconds: 3,
  matchSeconds: 900,
  catchPenaltySeconds: 3,
  doorZoneHalfWidth: 4,
  slotSpacing: 1.6,
  roamMargin: 1.5,
  entryDepth: 0.05,
  moveLimits: { maxHorizontalSpeed: 6.5, maxVerticalSpeed: 22, tolerance: 0.5 },
  push: { range: 1.8, coneDegrees: 70, speed: 7, cooldownSeconds: 1.2, graceSeconds: 1.5 },
  listener: {
    roamSpeed: 3,
    roamPauseSeconds: 1.5,
    alertDelaySeconds: 0.6,
    huntSpeed: 8,
    speedPerSplash: 0.1,
    lingerSeconds: 2,
    catchDistance: 0.9,
    reachedDistance: 0.3,
  },
  points: { door: [3, 2, 1, 0], finish: [10, 6, 3, 1], splash: -1, caught: -3, teamClear: 5 },
};

/**
 * Joins the layouts end to end along +z: each room's start deck begins where the previous exit deck ends, and its
 * start lines up in x with the previous door. The client builds the same rooms from the offsets it is sent.
 * `tuning` gives each room's Listener its solo level's numbers, by level id (missing values: `config.listener`).
 */
export function buildCircuit(
  layouts: LevelLayout[],
  config: CircuitConfig = CIRCUIT_CONFIG,
  tuning: Record<string, Partial<ListenerTuning>> = {},
): CircuitRoom[] {
  const rooms: CircuitRoom[] = [];
  let offsetX = 0;
  let offsetZ = 0;
  layouts.forEach((l, index) => {
    const startSouth = l.start.z - l.start.depth / 2;
    const exitNorth = l.exit.z + l.exit.depth / 2;
    if (index > 0) {
      const prev = rooms[index - 1];
      offsetZ = prev.maxZ - startSouth;
      offsetX = prev.door.x - l.start.x;
    }
    const exitSouth = l.exit.z - l.exit.depth / 2;
    const door = { x: l.exit.x + offsetX, y: l.exitTopY + 1.1, z: exitNorth + offsetZ - 0.15 };
    rooms.push({
      index,
      id: l.id,
      name: l.name,
      offsetX,
      offsetZ,
      minZ: startSouth + offsetZ,
      maxZ: exitNorth + offsetZ,
      water: {
        surfaceY: l.water.y,
        minX: l.water.minX + offsetX,
        maxX: l.water.maxX + offsetX,
        minZ: l.water.minZ + offsetZ,
        maxZ: l.water.maxZ + offsetZ,
      },
      spawn: { x: l.start.x + offsetX, y: l.platformTopY + 0.05, z: l.start.z + offsetZ },
      exitTopY: l.exitTopY,
      door,
      doorZone: {
        minX: door.x - config.doorZoneHalfWidth,
        maxX: door.x + config.doorZoneHalfWidth,
        minZ: exitSouth + offsetZ,
        maxZ: exitNorth + offsetZ,
        minY: l.exitTopY - 0.3,
      },
      listenerStart: {
        x: (l.water.minX + l.water.maxX) / 2 + offsetX,
        z: (l.water.minZ + l.water.maxZ) / 2 + offsetZ,
      },
      listener: { ...config.listener, ...(tuning[l.id] ?? {}) },
    });
  });
  return rooms;
}

export interface CircuitPlayer {
  id: string;
  slot: number;
  body: number; // the character picked in the menu (0 female, 1 male): shown to the others
  x: number;
  y: number;
  z: number;
  yaw: number;
  ready: boolean;
  room: number; // the room they're in (by position)
  reached: number; // highest room whose door they've reached (-1 = none yet)
  place: number; // finishing place (1 = first through the last door), 0 = still running
  inWater: boolean;
  splashes: number;
  catches: number;
  penalty: number;
  score: number;
  lastPosAtMs: number | null;
  pushReadyAtMs: number;
  pushGraceUntilMs: number;
}

export interface CircuitListener {
  room: number;
  x: number;
  z: number;
  state: MonsterState;
  timer: number;
  targetX: number;
  targetZ: number;
  roamX: number;
  roamZ: number;
  pause: number;
  splashes: number; // heard in its room: each one makes it hunt faster
}

export interface CircuitState {
  rooms: CircuitRoom[];
  phase: CircuitPhase;
  lobby: number;
  countdown: number;
  remaining: number;
  result: CircuitResult;
  reason: string;
  doorOrder: string[][]; // per room: who reached its door, in order
  finishOrder: string[];
  listeners: CircuitListener[];
  players: Map<string, CircuitPlayer>;
}

export interface CircuitRespawn {
  id: string;
  x: number;
  y: number;
  z: number;
  // where it caught them, so every client can play the catch there
  caughtX: number;
  caughtZ: number;
  room: number;
}

export interface CircuitPush {
  pusher: string;
  target: string;
  dx: number;
  dz: number;
  speed: number;
}

export interface CircuitSnapshot {
  phase: CircuitPhase;
  lobby: number;
  countdown: number;
  remaining: number;
  roomCount: number;
  result: CircuitResult;
  reason: string;
  listeners: Array<{ room: number; x: number; z: number; state: MonsterState }>;
  players: Array<{
    id: string;
    slot: number;
    body: number;
    x: number;
    y: number;
    z: number;
    yaw: number;
    room: number;
    reached: number;
    place: number;
    splashes: number;
    catches: number;
    penalty: number;
    score: number;
  }>;
}

export function createCircuitState(rooms: CircuitRoom[], config: CircuitConfig = CIRCUIT_CONFIG, rng: () => number = Math.random): CircuitState {
  return {
    rooms,
    phase: "waiting",
    lobby: config.lobbySeconds,
    countdown: config.countdownSeconds,
    remaining: config.matchSeconds,
    result: "",
    reason: "",
    doorOrder: rooms.map((): string[] => []),
    finishOrder: [],
    listeners: rooms.map((r) => freshListener(r, config, rng)),
    players: new Map(),
  };
}

export function spawnFor(room: CircuitRoom, slot: number, config: CircuitConfig = CIRCUIT_CONFIG) {
  return { x: room.spawn.x + (slot - 1.5) * config.slotSpacing, y: room.spawn.y, z: room.spawn.z };
}

export function addPlayer(state: CircuitState, id: string, body = 0, config: CircuitConfig = CIRCUIT_CONFIG): boolean {
  if (state.phase !== "waiting" || state.players.has(id) || state.players.size >= config.maxPlayers) return false;
  const taken = new Set(Array.from(state.players.values()).map((p) => p.slot));
  let slot = 0;
  while (taken.has(slot)) slot++;
  const at = spawnFor(state.rooms[0], slot, config);
  state.players.set(id, {
    id,
    slot,
    body: body === 1 ? 1 : 0,
    ...at,
    yaw: 0,
    ready: false,
    room: 0,
    reached: -1,
    place: 0,
    inWater: false,
    splashes: 0,
    catches: 0,
    penalty: 0,
    score: 0,
    lastPosAtMs: null,
    pushReadyAtMs: 0,
    pushGraceUntilMs: 0,
  });
  state.lobby = config.lobbySeconds; // someone new: give the others time to join too
  return true;
}

export function markReady(state: CircuitState, id: string, body?: number): void {
  const p = state.players.get(id);
  if (!p || state.phase !== "waiting") return;
  p.ready = true;
  if (body === 0 || body === 1) p.body = body;
}

/** Which room a point is in (by its z), clamped to the circuit. */
export function roomAt(state: CircuitState, z: number): number {
  for (let i = state.rooms.length - 1; i >= 0; i--) if (z >= state.rooms[i].minZ) return i;
  return 0;
}

export function applyPosition(
  state: CircuitState,
  id: string,
  input: { x: number; y: number; z: number; yaw?: number },
  nowMs: number,
  config: CircuitConfig = CIRCUIT_CONFIG,
): boolean {
  if (state.phase !== "running") return false;
  const p = state.players.get(id);
  if (!p || p.penalty > 0) return false;
  if (![input.x, input.y, input.z, nowMs].every(Number.isFinite)) return false;
  if (!insideCircuit(state, input)) return false;
  if (p.lastPosAtMs !== null) {
    const rawDt = nowMs - p.lastPosAtMs;
    const dtMs = rawDt === 0 ? 20 : rawDt > 0 ? Math.max(rawDt, 20) : rawDt;
    const pushed = nowMs < p.pushGraceUntilMs;
    const limits = pushed
      ? { ...config.moveLimits, maxHorizontalSpeed: config.moveLimits.maxHorizontalSpeed + config.push.speed }
      : config.moveLimits;
    if (!isMovePlausible(p, input, dtMs, limits)) return false;
  }
  p.x = input.x;
  p.y = input.y;
  p.z = input.z;
  if (input.yaw !== undefined && Number.isFinite(input.yaw)) p.yaw = input.yaw;
  p.lastPosAtMs = nowMs;
  p.room = roomAt(state, p.z);

  const room = state.rooms[p.room];
  const wet = inWater(room, p, config);
  if (wet && !p.inWater) {
    p.splashes += 1;
    p.score += config.points.splash;
    hearSplash(state.listeners[p.room], p, room.listener);
  }
  p.inWater = wet;

  // Reaching a door you haven't reached before: points by arrival order in that room; the last door finishes you.
  if (!wet && p.room > p.reached && inBox(room.doorZone, p) && p.y >= room.doorZone.minY) {
    p.reached = p.room;
    const order = state.doorOrder[p.room];
    order.push(p.id);
    p.score += config.points.door[order.length - 1] ?? 0;
    if (p.room === state.rooms.length - 1 && p.place === 0) {
      state.finishOrder.push(p.id);
      p.place = state.finishOrder.length;
      p.score += config.points.finish[p.place - 1] ?? 0;
      finishIfEveryoneIsThrough(state, config);
    }
  }
  return true;
}

/**
 * A push: the nearest other player within range in front of the pusher (not in a catch penalty) is shoved away.
 * Null if nobody is there or the pusher's push isn't ready.
 */
export function push(state: CircuitState, pusherId: string, nowMs: number, config: CircuitConfig = CIRCUIT_CONFIG): CircuitPush | null {
  if (state.phase !== "running") return null;
  const pusher = state.players.get(pusherId);
  if (!pusher || pusher.penalty > 0 || pusher.place > 0 || nowMs < pusher.pushReadyAtMs) return null;
  const yaw = (pusher.yaw * Math.PI) / 180; // Unity: yaw 0 faces +z, 90 faces +x
  const fx = Math.sin(yaw);
  const fz = Math.cos(yaw);
  const cone = Math.cos((config.push.coneDegrees * Math.PI) / 180);
  let best: CircuitPlayer | null = null;
  let bestDist = Infinity;
  for (const p of state.players.values()) {
    if (p.id === pusherId || p.penalty > 0 || p.place > 0) continue;
    const dx = p.x - pusher.x;
    const dz = p.z - pusher.z;
    const d = Math.hypot(dx, dz);
    if (d > config.push.range || Math.abs(p.y - pusher.y) > 1.5) continue;
    if (d > 0.05 && (dx * fx + dz * fz) / d < cone) continue;
    if (d < bestDist) {
      best = p;
      bestDist = d;
    }
  }
  pusher.pushReadyAtMs = nowMs + config.push.cooldownSeconds * 1000;
  if (!best) return null;
  let dx = best.x - pusher.x;
  let dz = best.z - pusher.z;
  const len = Math.hypot(dx, dz);
  if (len < 0.05) {
    dx = fx;
    dz = fz;
  } else {
    dx /= len;
    dz /= len;
  }
  best.pushGraceUntilMs = nowMs + config.push.graceSeconds * 1000;
  return { pusher: pusherId, target: best.id, dx, dz, speed: config.push.speed };
}

/** Steps lobby, countdown, penalties, every room's Listener and the clock. Returns the players caught this step. */
export function tickCircuit(
  state: CircuitState,
  dt: number,
  config: CircuitConfig = CIRCUIT_CONFIG,
  rng: () => number = Math.random,
): CircuitRespawn[] {
  if (dt <= 0 || !Number.isFinite(dt)) return [];
  if (state.phase === "waiting") {
    const all = Array.from(state.players.values());
    const allReady = all.length >= config.minPlayers && all.every((p) => p.ready);
    if (!allReady) {
      state.lobby = config.lobbySeconds;
    } else if (all.length >= config.maxPlayers) {
      startCountdown(state, config);
    } else {
      state.lobby = Math.max(0, state.lobby - dt);
      if (state.lobby === 0) startCountdown(state, config);
    }
    return [];
  }
  if (state.phase === "countdown") {
    state.countdown = Math.max(0, state.countdown - dt);
    if (state.countdown === 0) state.phase = "running";
    return [];
  }
  if (state.phase !== "running") return [];

  for (const p of state.players.values()) {
    if (p.penalty > 0) {
      p.penalty = Math.max(0, p.penalty - dt);
      if (p.penalty === 0) p.lastPosAtMs = null;
    }
  }
  const respawns: CircuitRespawn[] = [];
  for (const l of state.listeners) respawns.push(...tickListener(state, l, dt, config, rng));
  state.remaining = Math.max(0, state.remaining - dt);
  if (state.remaining === 0 && state.phase === "running") {
    state.phase = "finished";
    state.result = "timeout";
    state.reason = `Time's up: ${state.finishOrder.length} of ${state.players.size} made it through.`;
  }
  return respawns;
}

export function handleDisconnect(state: CircuitState, id: string, config: CircuitConfig = CIRCUIT_CONFIG): void {
  state.players.delete(id);
  if (state.phase === "countdown" || state.phase === "running") {
    if (state.players.size === 0) {
      state.phase = "finished";
      state.result = "abandoned";
      state.reason = "Everyone left.";
      return;
    }
    if (state.phase === "running") finishIfEveryoneIsThrough(state, config);
  }
}

export function toSnapshot(state: CircuitState): CircuitSnapshot {
  return {
    phase: state.phase,
    lobby: state.lobby,
    countdown: state.countdown,
    remaining: state.remaining,
    roomCount: state.rooms.length,
    result: state.result,
    reason: state.reason,
    listeners: state.listeners.map((l) => ({ room: l.room, x: l.x, z: l.z, state: l.state })),
    players: Array.from(state.players.values()).map((p) => ({
      id: p.id,
      slot: p.slot,
      body: p.body,
      x: p.x,
      y: p.y,
      z: p.z,
      yaw: p.yaw,
      room: p.room,
      reached: p.reached,
      place: p.place,
      splashes: p.splashes,
      catches: p.catches,
      penalty: p.penalty,
      score: p.score,
    })),
  };
}

// ---------------------------------------------------------------- internals

function startCountdown(state: CircuitState, config: CircuitConfig): void {
  state.phase = "countdown";
  state.countdown = config.countdownSeconds;
  state.remaining = config.matchSeconds;
}

function inBox(b: Box, p: { x: number; z: number }): boolean {
  return p.x >= b.minX && p.x <= b.maxX && p.z >= b.minZ && p.z <= b.maxZ;
}

function inWater(room: CircuitRoom, p: { x: number; y: number; z: number }, config: CircuitConfig): boolean {
  return p.y < room.water.surfaceY - config.entryDepth && inBox(room.water, p);
}

function insideCircuit(state: CircuitState, p: { x: number; y: number; z: number }): boolean {
  const minX = Math.min(...state.rooms.map((r) => r.water.minX)) - 2;
  const maxX = Math.max(...state.rooms.map((r) => r.water.maxX)) + 2;
  const minZ = state.rooms[0].minZ - 1;
  const maxZ = state.rooms[state.rooms.length - 1].maxZ + 0.5;
  return p.x >= minX && p.x <= maxX && p.z >= minZ && p.z <= maxZ && p.y >= -4 && p.y <= 8;
}

function finishIfEveryoneIsThrough(state: CircuitState, config: CircuitConfig): void {
  const all = Array.from(state.players.values());
  if (all.length === 0 || !all.every((p) => p.place > 0)) return;
  state.phase = "finished";
  state.result = "cleared";
  state.reason = "Everyone made it through every room.";
  for (const p of all) p.score += config.points.teamClear;
}

function freshListener(room: CircuitRoom, config: CircuitConfig, rng: () => number): CircuitListener {
  const roam = pickRoam(room, config, rng);
  return {
    room: room.index,
    x: room.listenerStart.x,
    z: room.listenerStart.z,
    state: "roaming",
    timer: 0,
    targetX: room.listenerStart.x,
    targetZ: room.listenerStart.z,
    roamX: roam.x,
    roamZ: roam.z,
    pause: 0,
    splashes: 0,
  };
}

function hearSplash(l: CircuitListener, p: CircuitPlayer, c: ListenerTuning): void {
  l.splashes += 1;
  l.targetX = p.x;
  l.targetZ = p.z;
  if (l.state === "roaming") {
    l.state = "alerted";
    l.timer = c.alertDelaySeconds;
  } else if (l.state === "lingering") {
    l.state = "hunting";
  }
}

function tickListener(state: CircuitState, l: CircuitListener, dt: number, config: CircuitConfig, rng: () => number): CircuitRespawn[] {
  const room = state.rooms[l.room];
  const c = room.listener;
  // Swimming is noisy: it always knows where the nearest swimmer in its room is.
  const swimmers = Array.from(state.players.values()).filter((p) => p.room === l.room && p.inWater && p.penalty <= 0);
  let nearest: CircuitPlayer | null = null;
  for (const p of swimmers) {
    if (!nearest || Math.hypot(p.x - l.x, p.z - l.z) < Math.hypot(nearest.x - l.x, nearest.z - l.z)) nearest = p;
  }
  if (nearest) {
    l.targetX = nearest.x;
    l.targetZ = nearest.z;
  }
  l.timer -= dt;
  const atTarget = Math.hypot(l.targetX - l.x, l.targetZ - l.z) <= c.reachedDistance;

  switch (l.state) {
    case "roaming":
      if (nearest) {
        l.state = "alerted";
        l.timer = c.alertDelaySeconds;
      }
      break;
    case "alerted":
      if (l.timer <= 0) l.state = "hunting";
      break;
    case "hunting":
      if (!nearest && atTarget) {
        l.state = "lingering";
        l.timer = c.lingerSeconds;
      }
      break;
    case "lingering":
      if (nearest) l.state = "hunting";
      else if (l.timer <= 0) {
        l.state = "roaming";
        const next = pickRoam(room, config, rng);
        l.roamX = next.x;
        l.roamZ = next.z;
        l.pause = 0;
      }
      break;
  }

  const respawns: CircuitRespawn[] = [];
  if (l.state === "roaming") {
    if (l.pause > 0) l.pause = Math.max(0, l.pause - dt);
    else {
      step(l, l.roamX, l.roamZ, c.roamSpeed * dt);
      if (Math.hypot(l.roamX - l.x, l.roamZ - l.z) <= c.reachedDistance) {
        l.pause = c.roamPauseSeconds;
        const next = pickRoam(room, config, rng);
        l.roamX = next.x;
        l.roamZ = next.z;
      }
    }
  } else if (l.state === "hunting") {
    const speed = c.huntSpeed * (1 + c.speedPerSplash * Math.max(0, l.splashes - 1));
    step(l, l.targetX, l.targetZ, speed * dt);
    for (const p of swimmers) {
      if (Math.hypot(p.x - l.x, p.z - l.z) > c.catchDistance) continue;
      const caughtX = p.x;
      const caughtZ = p.z;
      const at = spawnFor(room, p.slot, config);
      Object.assign(p, at, { inWater: false, penalty: config.catchPenaltySeconds, lastPosAtMs: null });
      p.catches += 1;
      p.score += config.points.caught;
      respawns.push({ id: p.id, ...at, caughtX, caughtZ, room: l.room });
    }
    if (respawns.length > 0) {
      l.state = "lingering";
      l.timer = c.lingerSeconds;
    }
  }
  return respawns;
}

function step(l: CircuitListener, tx: number, tz: number, maxStep: number): void {
  const dx = tx - l.x;
  const dz = tz - l.z;
  const d = Math.hypot(dx, dz);
  if (d <= maxStep || d === 0) {
    l.x = tx;
    l.z = tz;
    return;
  }
  l.x += (dx / d) * maxStep;
  l.z += (dz / d) * maxStep;
}

function pickRoam(room: CircuitRoom, config: CircuitConfig, rng: () => number): { x: number; z: number } {
  const w = room.water;
  const m = config.roamMargin;
  return {
    x: w.minX + m + rng() * Math.max(0, w.maxX - w.minX - 2 * m),
    z: w.minZ + m + rng() * Math.max(0, w.maxZ - w.minZ - 2 * m),
  };
}

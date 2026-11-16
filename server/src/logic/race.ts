// Pure 1v1 online race rules: no Colyseus imports, so node:test verifies every rule directly.
import { isMovePlausible, type MoveLimits } from "./movement.js";

export type RacePhase = "waiting" | "countdown" | "racing" | "finished";
export type MonsterState = "roaming" | "alerted" | "hunting" | "lingering";

export interface RaceConfig {
  maxPlayers: number;
  countdownSeconds: number;
  matchSeconds: number;
  catchPenaltySeconds: number;
  spawn: { x: number; y: number; z: number };
  door: { x: number; y: number; z: number; reach: number; minY: number };
  water: { surfaceY: number; minX: number; maxX: number; minZ: number; maxZ: number; roamMargin: number; entryY: number };
  bounds: { minX: number; maxX: number; minY: number; maxY: number; minZ: number; maxZ: number };
  moveLimits: MoveLimits;
  listener: {
    startX: number;
    startZ: number;
    roamSpeed: number;
    roamPauseSeconds: number;
    alertDelaySeconds: number;
    huntSpeed: number;
    speedPerSplash: number;
    lingerSeconds: number;
    catchDistance: number;
    reachedDistance: number;
  };
}

// Level01 geometry + MovementConfig/LevelConfig_01 defaults (validated against client Level01.unity).
export const RACE_CONFIG: RaceConfig = {
  maxPlayers: 2,
  countdownSeconds: 3,
  matchSeconds: 180,
  catchPenaltySeconds: 3,
  spawn: { x: 0, y: 0.55, z: -2 },
  door: { x: 0, y: 1.1, z: 29.35, reach: 1.6, minY: 0.8 },
  water: { surfaceY: 0, minX: -15, maxX: 15, minZ: 0, maxZ: 25.5, roamMargin: 1.5, entryY: -0.05 },
  bounds: { minX: -16, maxX: 16, minY: -4, maxY: 6, minZ: -5, maxZ: 31 },
  moveLimits: { maxHorizontalSpeed: 6.5, maxVerticalSpeed: 22, tolerance: 0.5 },
  listener: {
    startX: 0,
    startZ: 12.75,
    roamSpeed: 3,
    roamPauseSeconds: 1.5,
    alertDelaySeconds: 0.6,
    huntSpeed: 8,
    speedPerSplash: 0.1,
    lingerSeconds: 2,
    catchDistance: 0.9,
    reachedDistance: 0.3,
  },
};

export interface PlayerRaceState {
  id: string;
  x: number;
  y: number;
  z: number;
  yaw: number;
  ready: boolean;
  inWater: boolean;
  splashes: number;
  penalty: number;
  lastPosAtMs: number | null;
  monsterX: number;
  monsterZ: number;
  monsterState: MonsterState;
  monsterTimer: number;
  targetX: number;
  targetZ: number;
  roamTargetX: number;
  roamTargetZ: number;
  roamPauseLeft: number;
}

export interface RaceState {
  phase: RacePhase;
  countdown: number;
  remaining: number;
  winner: string;
  reason: string;
  players: Map<string, PlayerRaceState>;
}

export interface RaceRespawnEvent {
  id: string;
  x: number;
  y: number;
  z: number;
}

export interface RaceSnapshot {
  phase: RacePhase;
  countdown: number;
  remaining: number;
  winner: string;
  reason: string;
  players: Array<{
    id: string;
    x: number;
    y: number;
    z: number;
    yaw: number;
    splashes: number;
    penalty: number;
    monsterX: number;
    monsterZ: number;
    monsterState: MonsterState;
  }>;
}

export function createRaceState(config: RaceConfig = RACE_CONFIG): RaceState {
  return {
    phase: "waiting",
    countdown: config.countdownSeconds,
    remaining: config.matchSeconds,
    winner: "",
    reason: "",
    players: new Map(),
  };
}

export function addPlayer(
  state: RaceState,
  id: string,
  config: RaceConfig = RACE_CONFIG,
  rng: () => number = Math.random,
): boolean {
  if (state.phase !== "waiting" || state.players.has(id) || state.players.size >= config.maxPlayers) {
    return false;
  }
  const roam = pickRoamTarget(config, rng);
  state.players.set(id, {
    id,
    x: config.spawn.x,
    y: config.spawn.y,
    z: config.spawn.z,
    yaw: 0,
    ready: false,
    inWater: false,
    splashes: 0,
    penalty: 0,
    lastPosAtMs: null,
    monsterX: config.listener.startX,
    monsterZ: config.listener.startZ,
    monsterState: "roaming",
    monsterTimer: 0,
    targetX: config.listener.startX,
    targetZ: config.listener.startZ,
    roamTargetX: roam.x,
    roamTargetZ: roam.z,
    roamPauseLeft: 0,
  });
  return true;
}

export function markReady(state: RaceState, id: string, config: RaceConfig = RACE_CONFIG): void {
  const player = state.players.get(id);
  if (!player || state.phase !== "waiting") return;
  player.ready = true;
  if (state.players.size === config.maxPlayers && Array.from(state.players.values()).every((p) => p.ready)) {
    state.phase = "countdown";
    state.countdown = config.countdownSeconds;
    state.remaining = config.matchSeconds;
  }
}

export function applyPosition(
  state: RaceState,
  id: string,
  input: { x: number; y: number; z: number; yaw?: number },
  nowMs: number,
  config: RaceConfig = RACE_CONFIG,
): boolean {
  if (state.phase !== "racing") return false;
  const player = state.players.get(id);
  if (!player || player.penalty > 0) return false;
  if (![input.x, input.y, input.z, nowMs].every(Number.isFinite)) return false;

  const { bounds } = config;
  if (
    input.x < bounds.minX || input.x > bounds.maxX ||
    input.y < bounds.minY || input.y > bounds.maxY ||
    input.z < bounds.minZ || input.z > bounds.maxZ
  ) {
    return false;
  }

  if (player.lastPosAtMs !== null) {
    const rawDt = nowMs - player.lastPosAtMs;
    // Allow same-millisecond bursts to pass speed check while still rejecting out-of-order timestamps.
    const dtMs = rawDt === 0 ? 20 : rawDt > 0 ? Math.max(rawDt, 20) : rawDt;
    if (!isMovePlausible(player, input, dtMs, config.moveLimits)) return false;
  }

  player.x = input.x;
  player.y = input.y;
  player.z = input.z;
  if (input.yaw !== undefined && Number.isFinite(input.yaw)) player.yaw = input.yaw;
  player.lastPosAtMs = nowMs;

  const nowInWater = player.y < config.water.entryY && player.z >= config.water.minZ && player.z <= config.water.maxZ;
  if (!player.inWater && nowInWater) {
    player.inWater = true;
    player.splashes += 1;
    player.targetX = player.x;
    player.targetZ = player.z;
    if (player.monsterState === "roaming") {
      player.monsterState = "alerted";
      player.monsterTimer = config.listener.alertDelaySeconds;
    } else if (player.monsterState !== "alerted") {
      player.monsterState = "hunting";
    }
  } else if (nowInWater) {
    player.targetX = player.x;
    player.targetZ = player.z;
  } else {
    player.inWater = false;
  }

  if (
    !player.inWater &&
    player.y >= config.door.minY &&
    Math.hypot(player.x - config.door.x, player.z - config.door.z) <= config.door.reach
  ) {
    state.phase = "finished";
    state.winner = player.id;
    state.reason = "Reached the exit door first.";
  }

  return true;
}

export function tickRace(
  state: RaceState,
  dtSeconds: number,
  config: RaceConfig = RACE_CONFIG,
  rng: () => number = Math.random,
): RaceRespawnEvent[] {
  if (dtSeconds <= 0 || !Number.isFinite(dtSeconds)) return [];

  if (state.phase === "countdown") {
    state.countdown = Math.max(0, state.countdown - dtSeconds);
    if (state.countdown === 0) state.phase = "racing";
    return [];
  }

  if (state.phase !== "racing") return [];

  const respawns: RaceRespawnEvent[] = [];
  for (const player of state.players.values()) {
    if (player.penalty > 0) {
      player.penalty = Math.max(0, player.penalty - dtSeconds);
      if (player.penalty === 0) player.lastPosAtMs = null;
    }

    const caught = tickListener(player, dtSeconds, config, rng);
    if (caught) {
      player.x = config.spawn.x;
      player.y = config.spawn.y;
      player.z = config.spawn.z;
      player.inWater = false;
      player.penalty = config.catchPenaltySeconds;
      player.lastPosAtMs = null;
      player.monsterState = "lingering";
      player.monsterTimer = config.listener.lingerSeconds;
      respawns.push({ id: player.id, ...config.spawn });
    }
  }

  state.remaining = Math.max(0, state.remaining - dtSeconds);
  if (state.remaining === 0) finishByTimeout(state, config);
  return respawns;
}

export function handleDisconnect(state: RaceState, leavingId: string): void {
  state.players.delete(leavingId);
  if (state.phase === "countdown" || state.phase === "racing") {
    const remaining = state.players.values().next().value;
    state.phase = "finished";
    state.winner = remaining?.id ?? "";
    state.reason = "Other racer disconnected.";
  }
}

export function toSnapshot(state: RaceState): RaceSnapshot {
  return {
    phase: state.phase,
    countdown: state.countdown,
    remaining: state.remaining,
    winner: state.winner,
    reason: state.reason,
    players: Array.from(state.players.values()).map((p) => ({
      id: p.id,
      x: p.x,
      y: p.y,
      z: p.z,
      yaw: p.yaw,
      splashes: p.splashes,
      penalty: p.penalty,
      monsterX: p.monsterX,
      monsterZ: p.monsterZ,
      monsterState: p.monsterState,
    })),
  };
}

function tickListener(player: PlayerRaceState, dt: number, config: RaceConfig, rng: () => number): boolean {
  const l = config.listener;
  if (player.inWater) {
    player.targetX = player.x;
    player.targetZ = player.z;
  }

  player.monsterTimer -= dt;
  const atTarget = Math.hypot(player.targetX - player.monsterX, player.targetZ - player.monsterZ) <= l.reachedDistance;

  switch (player.monsterState) {
    case "roaming":
      if (player.inWater) {
        player.monsterState = "alerted";
        player.monsterTimer = l.alertDelaySeconds;
      }
      break;
    case "alerted":
      if (player.monsterTimer <= 0) player.monsterState = "hunting";
      break;
    case "hunting":
      if (!player.inWater && atTarget) {
        player.monsterState = "lingering";
        player.monsterTimer = l.lingerSeconds;
      }
      break;
    case "lingering":
      if (player.inWater) {
        player.monsterState = "hunting";
      } else if (player.monsterTimer <= 0) {
        player.monsterState = "roaming";
        const next = pickRoamTarget(config, rng);
        player.roamTargetX = next.x;
        player.roamTargetZ = next.z;
        player.roamPauseLeft = 0;
      }
      break;
  }

  if (player.monsterState === "roaming") {
    if (player.roamPauseLeft > 0) {
      player.roamPauseLeft = Math.max(0, player.roamPauseLeft - dt);
    } else {
      moveToward(player, player.roamTargetX, player.roamTargetZ, l.roamSpeed * dt);
      if (Math.hypot(player.roamTargetX - player.monsterX, player.roamTargetZ - player.monsterZ) <= l.reachedDistance) {
        player.roamPauseLeft = l.roamPauseSeconds;
        const next = pickRoamTarget(config, rng);
        player.roamTargetX = next.x;
        player.roamTargetZ = next.z;
      }
    }
  } else if (player.monsterState === "hunting") {
    const huntSpeed = l.huntSpeed * (1 + l.speedPerSplash * Math.max(0, player.splashes - 1));
    moveToward(player, player.targetX, player.targetZ, huntSpeed * dt);
    if (player.inWater && player.penalty <= 0 && Math.hypot(player.x - player.monsterX, player.z - player.monsterZ) <= l.catchDistance) {
      return true;
    }
  }

  return false;
}

function moveToward(player: PlayerRaceState, tx: number, tz: number, maxStep: number): void {
  const dx = tx - player.monsterX;
  const dz = tz - player.monsterZ;
  const dist = Math.hypot(dx, dz);
  if (dist <= maxStep || dist === 0) {
    player.monsterX = tx;
    player.monsterZ = tz;
    return;
  }
  player.monsterX += (dx / dist) * maxStep;
  player.monsterZ += (dz / dist) * maxStep;
}

function pickRoamTarget(config: RaceConfig, rng: () => number): { x: number; z: number } {
  const { minX, maxX, minZ, maxZ, roamMargin } = config.water;
  return {
    x: minX + roamMargin + rng() * Math.max(0, maxX - minX - 2 * roamMargin),
    z: minZ + roamMargin + rng() * Math.max(0, maxZ - minZ - 2 * roamMargin),
  };
}

function finishByTimeout(state: RaceState, config: RaceConfig): void {
  const racers = Array.from(state.players.values());
  state.phase = "finished";
  if (racers.length === 0) {
    state.winner = "";
    state.reason = "Time's up — draw!";
    return;
  }
  if (racers.length === 1) {
    state.winner = racers[0].id;
    state.reason = "Time's up — closest to the exit wins.";
    return;
  }
  const d0 = Math.hypot(racers[0].x - config.door.x, racers[0].z - config.door.z);
  const d1 = Math.hypot(racers[1].x - config.door.x, racers[1].z - config.door.z);
  if (Math.abs(d0 - d1) <= 0.01) {
    state.winner = "";
    state.reason = "Time's up — draw!";
  } else {
    state.winner = d0 < d1 ? racers[0].id : racers[1].id;
    state.reason = "Time's up — closest to the exit wins.";
  }
}

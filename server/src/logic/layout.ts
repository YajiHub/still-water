// Level layouts shared with the Unity client (decision 16). Pure functions, no I/O.
// Metres, Unity axes: x/z is the floor plane, y is up.

export interface Rect {
  x: number; // centre
  z: number; // centre
  width: number; // along x
  depth: number; // along z
}

/** A ferry (Level Roster, Level 3): slides from its x/z (end A) to A + (dx, dz) and back, pausing at each end. */
export interface PlatformMove {
  dx: number;
  dz: number;
  seconds: number; // one way, A → B
  pause: number; // at each end
  offset?: number; // seconds into the cycle at level start, so ferries aren't in step
}

export interface PlatformSpot {
  id: number;
  x: number;
  z: number;
  size: number; // square, edge length
  move?: PlatformMove;
  // Level 4 (Level Roster): "crumble" = sinks if you stand on it too long, rises again; "oneway" = sinks after you
  // leave it and stays down. Every tile exists at the start, so reach and dead-end checks ignore this.
  sink?: SinkKind;
}

export type SinkKind = "crumble" | "oneway";
const SINK_KINDS: readonly string[] = ["crumble", "oneway"];

export interface LevelLayout {
  id: string;
  name: string;
  water: { y: number; minX: number; maxX: number; minZ: number; maxZ: number };
  platformTopY: number;
  exitTopY: number; // higher than the platforms: jump onto it, never climb onto it from the water
  start: Rect;
  exit: Rect;
  platforms: PlatformSpot[];
  safePath: number[]; // platform ids, start → exit: the glowing pattern
  oneDryRoute?: boolean;
  maxGap?: number; // longest jump this level may ask for (default MAX_GAP_M; never above JUMP_LIMIT_M) // no fakes: every non-path platform must be a dead end (tested per level)
}

// Longest gap a layout may ask for: the jump (4.5 m/s, 1.2 m) reaches ~3 m (Core Loop Rules).
export const MAX_GAP_M = 2.8;
// An "edge of the jump" level may go up to this (Level 3, hand-tuned by the user); nothing is fair beyond it.
export const JUMP_LIMIT_M = 3.1;

// Open water kept around a ferry's whole slide, so it never grazes another tile (or shoves a player standing there).
export const FERRY_CLEARANCE_M = 1.5;

// A ferry counts as the whole strip it slides across: you can reach it (and anything near it) from any point of its trip.
export const toRect = (p: PlatformSpot): Rect =>
  p.move
    ? { x: p.x + p.move.dx / 2, z: p.z + p.move.dz / 2, width: p.size + Math.abs(p.move.dx), depth: p.size + Math.abs(p.move.dz) }
    : { x: p.x, z: p.z, width: p.size, depth: p.size };

// Generous reach for "can a player get there at all": a strong jump (~3.1 m) plus edge forgiveness.
export const PLAYER_REACH_M = 3.4;

/** Can you get from the start to the exit by jumps of ≤ `reach` metres, without using the platforms in `skipIds`? */
export function reachesExit(layout: LevelLayout, skipIds: number[], reach: number): boolean {
  const nodes = [layout.start, ...layout.platforms.filter((p) => !skipIds.includes(p.id)).map(toRect), layout.exit];
  const reached = new Set<number>([0]);
  const queue = [0];
  while (queue.length > 0) {
    const i = queue.shift()!;
    nodes.forEach((n, j) => {
      if (!reached.has(j) && edgeGap(nodes[i], n) <= reach) {
        reached.add(j);
        queue.push(j);
      }
    });
  }
  return reached.has(nodes.length - 1);
}

/** Shortest distance of open water between two rectangles (0 if they touch or overlap). */
export function edgeGap(a: Rect, b: Rect): number {
  const dx = Math.max(0, Math.abs(a.x - b.x) - (a.width + b.width) / 2);
  const dz = Math.max(0, Math.abs(a.z - b.z) - (a.depth + b.depth) / 2);
  return Math.hypot(dx, dz);
}

/** Every problem found, as readable strings. An empty list means the layout is valid. */
export function validateLayout(layout: LevelLayout): string[] {
  const errors: string[] = [];
  const { water } = layout;
  const seen = new Set<number>();

  for (const p of layout.platforms) {
    if (seen.has(p.id)) errors.push(`duplicate platform id ${p.id}`);
    seen.add(p.id);
    if (!(p.size > 0)) errors.push(`platform ${p.id}: size must be > 0`);
    const r = toRect(p);
    if (r.x - r.width / 2 < water.minX || r.x + r.width / 2 > water.maxX || r.z - r.depth / 2 < water.minZ || r.z + r.depth / 2 > water.maxZ) {
      errors.push(`platform ${p.id}: outside the water`);
    }
    if (p.sink !== undefined && !SINK_KINDS.includes(p.sink)) errors.push(`platform ${p.id}: sink must be "crumble" or "oneway"`);
    if (p.move) {
      // On one-dry-route levels a moving decoy would break the dead-end proof; hand-tuned levels may have them.
      if (layout.oneDryRoute && !layout.safePath.includes(p.id)) errors.push(`platform ${p.id}: only route platforms may move`);
      if (!(p.move.seconds > 0)) errors.push(`platform ${p.id}: move.seconds must be > 0`);
      if (!(p.move.pause >= 0)) errors.push(`platform ${p.id}: move.pause must be ≥ 0`);
      if (!(Math.hypot(p.move.dx, p.move.dz) > 0)) errors.push(`platform ${p.id}: move must go somewhere`);
      for (const q of layout.platforms) {
        if (q.id !== p.id && edgeGap(r, toRect(q)) < FERRY_CLEARANCE_M) errors.push(`platform ${q.id} is in ferry ${p.id}'s way`);
      }
    }
  }
  if (!(layout.exitTopY > layout.platformTopY)) errors.push("exitTopY must be above platformTopY");
  for (const [name, r] of [["start", layout.start], ["exit", layout.exit]] as const) {
    if (!(r.width > 0 && r.depth > 0)) errors.push(`${name}: width and depth must be > 0`);
  }

  const maxGap = layout.maxGap ?? MAX_GAP_M;
  if (maxGap > JUMP_LIMIT_M) errors.push(`maxGap ${maxGap} is beyond the jump (max ${JUMP_LIMIT_M})`);

  // The safe path must be a chain of jumps: start → first → … → last → exit.
  if (layout.safePath.length === 0) errors.push("safePath is empty");
  const byId = new Map(layout.platforms.map((p) => [p.id, toRect(p)]));
  const missing = layout.safePath.filter((id) => !byId.has(id));
  for (const id of missing) errors.push(`safePath: no platform ${id}`);
  if (layout.safePath.length > 0 && missing.length === 0) {
    const steps: [string, Rect][] = [
      ["start", layout.start],
      ...layout.safePath.map((id): [string, Rect] => [String(id), byId.get(id)!]),
      ["exit", layout.exit],
    ];
    for (let i = 1; i < steps.length; i++) {
      const gap = edgeGap(steps[i - 1][1], steps[i][1]);
      if (gap > maxGap) errors.push(`safePath: ${steps[i - 1][0]} → ${steps[i][0]} is ${gap.toFixed(2)} m (max ${maxGap})`);
    }
  }

  // Walk from the start over every jumpable gap; the exit must be reached.
  if (!reachesExit(layout, [], maxGap)) errors.push(`exit is not reachable with jumps of ≤ ${maxGap} m`);

  return errors;
}

/**
 * The easiest way from the start to the exit: the chain of platforms whose longest jump is as short as possible
 * (fewest jumps on a tie), using jumps of at most the level's max gap. Null if there is none.
 * Used to set the route after a level was edited by hand in Unity.
 */
export function easiestRoute(layout: LevelLayout): number[] | null {
  const maxGap = layout.maxGap ?? MAX_GAP_M;
  const nodes = [layout.start, ...layout.platforms.map(toRect), layout.exit];
  const exitIndex = nodes.length - 1;
  // Dijkstra on (longest jump so far, jumps so far).
  const best = nodes.map(() => ({ longest: Infinity, jumps: Infinity }));
  const from = nodes.map(() => -1);
  const done = new Set<number>();
  best[0] = { longest: 0, jumps: 0 };
  while (done.size < nodes.length) {
    let i = -1;
    for (let j = 0; j < nodes.length; j++) {
      if (done.has(j) || best[j].longest === Infinity) continue;
      if (i < 0 || best[j].longest < best[i].longest || (best[j].longest === best[i].longest && best[j].jumps < best[i].jumps)) i = j;
    }
    if (i < 0) break;
    done.add(i);
    if (i === exitIndex) break;
    for (let j = 0; j < nodes.length; j++) {
      const gap = edgeGap(nodes[i], nodes[j]);
      if (done.has(j) || j === 0 || gap > maxGap) continue;
      const cand = { longest: Math.max(best[i].longest, gap), jumps: best[i].jumps + 1 };
      if (cand.longest < best[j].longest || (cand.longest === best[j].longest && cand.jumps < best[j].jumps)) {
        best[j] = cand;
        from[j] = i;
      }
    }
  }
  if (!done.has(exitIndex)) return null;
  const route: number[] = [];
  for (let i = from[exitIndex]; i > 0; i = from[i]) route.unshift(layout.platforms[i - 1].id);
  return route;
}

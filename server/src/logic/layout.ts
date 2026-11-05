// Level layouts shared with the Unity client (decision 16). Pure functions, no I/O.
// Metres, Unity axes: x/z is the floor plane, y is up.

export interface Rect {
  x: number; // centre
  z: number; // centre
  width: number; // along x
  depth: number; // along z
}

export interface PlatformSpot {
  id: number;
  x: number;
  z: number;
  size: number; // square, edge length
}

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
  oneDryRoute?: boolean; // no fakes: every non-path platform must be a dead end (tested per level)
}

// Longest gap a layout may ask for: the jump (4.5 m/s, 1.2 m) reaches ~3 m (Core Loop Rules).
export const MAX_GAP_M = 2.8;

const toRect = (p: PlatformSpot): Rect => ({ x: p.x, z: p.z, width: p.size, depth: p.size });

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
    const half = p.size / 2;
    if (p.x - half < water.minX || p.x + half > water.maxX || p.z - half < water.minZ || p.z + half > water.maxZ) {
      errors.push(`platform ${p.id}: outside the water`);
    }
  }
  if (!(layout.exitTopY > layout.platformTopY)) errors.push("exitTopY must be above platformTopY");
  for (const [name, r] of [["start", layout.start], ["exit", layout.exit]] as const) {
    if (!(r.width > 0 && r.depth > 0)) errors.push(`${name}: width and depth must be > 0`);
  }

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
      if (gap > MAX_GAP_M) errors.push(`safePath: ${steps[i - 1][0]} → ${steps[i][0]} is ${gap.toFixed(2)} m (max ${MAX_GAP_M})`);
    }
  }

  // Walk from the start over every jumpable gap; the exit must be reached.
  if (!reachesExit(layout, [], MAX_GAP_M)) errors.push(`exit is not reachable with jumps of ≤ ${MAX_GAP_M} m`);

  return errors;
}

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
}

// Longest gap a layout may ask for: DESIGN §3.3 says gaps grow to 3.2 m by Level 5.
export const MAX_GAP_M = 3.2;

const toRect = (p: PlatformSpot): Rect => ({ x: p.x, z: p.z, width: p.size, depth: p.size });

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

  // Walk from the start over every jumpable gap; the exit must be reached.
  const nodes = [layout.start, ...layout.platforms.map(toRect), layout.exit];
  const reached = new Set<number>([0]);
  const queue = [0];
  while (queue.length > 0) {
    const i = queue.shift()!;
    nodes.forEach((n, j) => {
      if (!reached.has(j) && edgeGap(nodes[i], n) <= MAX_GAP_M) {
        reached.add(j);
        queue.push(j);
      }
    });
  }
  if (!reached.has(nodes.length - 1)) errors.push(`exit is not reachable with jumps of ≤ ${MAX_GAP_M} m`);

  return errors;
}

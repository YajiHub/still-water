// Level layout generator (Level Roster): seeded, so the same options always give the same hall.
// Builds one dry route (start → exit) and then adds dead ends, keeping only ones that can't
// be used to skip a route platform (checked with reachesExit at the generous player reach).
//
//   npm run level:gen -- --id level02 --name "Deep End" --width 40 --length 42 --platforms 40 \
//     --route 9-11 --gaps 2.0-2.6 --sizes 2,1.6 --small 0.25 --seed 1 [--force]
//
// Writes src/levels/<id>.json (refuses to overwrite unless --force) and prints a summary.
import { existsSync, writeFileSync } from "node:fs";
import { pathToFileURL } from "node:url";

import { edgeGap, PLAYER_REACH_M, reachesExit, validateLayout, type LevelLayout, type PlatformSpot, type Rect } from "../src/logic/layout.js";

export interface GenOptions {
  id: string;
  name: string;
  width: number; // water along x, metres
  length: number; // water along z, metres (start deck before it, exit deck after it)
  platforms: number; // total, route + dead ends
  routeMin: number; // route platforms
  routeMax: number;
  gapMin: number; // edge-to-edge water between neighbours
  gapMax: number;
  sizes: number[]; // sizes[0] is the usual size, the rest are "small"
  smallChance: number; // 0..1
  seed: number;
  maxTurnDeg?: number; // how far a route step may turn from straight ahead (default 45)
  platformTopY?: number; // default 0.5 (Level 1)
  exitTopY?: number; // default 1.1 (Level 1)
}

const DECK_DEPTH = 4;
const WALL_MARGIN = 0.5; // keep platforms off the pool walls
const ROUTE_TRIES = 3000;
const STEP_TRIES = 200;
const DEAD_END_TRIES = 20000;

// mulberry32: tiny seeded PRNG, good enough for layouts.
function makeRng(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const round = (v: number) => Math.round(v * 20) / 20; // 5 cm grid, readable JSON
const rectOf = (p: PlatformSpot): Rect => ({ x: p.x, z: p.z, width: p.size, depth: p.size });

export function generateLevel(o: GenOptions): LevelLayout {
  const rand = makeRng(o.seed);
  const between = (lo: number, hi: number) => lo + rand() * (hi - lo);
  const pickSize = () => (o.sizes.length > 1 && rand() < o.smallChance ? o.sizes[1 + Math.floor(rand() * (o.sizes.length - 1))] : o.sizes[0]);
  const maxTurn = ((o.maxTurnDeg ?? 45) * Math.PI) / 180;

  const half = o.width / 2;
  const start: Rect = { x: 0, z: -DECK_DEPTH / 2, width: o.width, depth: DECK_DEPTH };
  const exit: Rect = { x: 0, z: o.length + DECK_DEPTH / 2, width: o.width, depth: DECK_DEPTH };
  const inWater = (x: number, z: number, s: number) =>
    x - s / 2 >= -half + WALL_MARGIN && x + s / 2 <= half - WALL_MARGIN && z - s / 2 >= WALL_MARGIN && z + s / 2 <= o.length - WALL_MARGIN;
  const inGapRange = (g: number) => g >= o.gapMin && g <= o.gapMax;

  // A candidate next to `from`, `gapMin..gapMax` of water away, heading `angle` (0 = +z).
  const near = (from: Rect, angle: number, size: number): PlatformSpot => {
    const d = between(o.gapMin, o.gapMax) + (from.width + size) / 2;
    return { id: 0, x: round(from.x + d * Math.sin(angle)), z: round(from.z + d * Math.cos(angle)), size };
  };

  const layoutOf = (platforms: PlatformSpot[], safePath: number[]): LevelLayout => ({
    id: o.id,
    name: o.name,
    oneDryRoute: true,
    water: { y: 0, minX: -half, maxX: half, minZ: 0, maxZ: o.length },
    platformTopY: o.platformTopY ?? 0.5,
    exitTopY: o.exitTopY ?? 1.1,
    start,
    exit,
    safePath,
    platforms,
  });

  // Every route platform is needed: with any one removed, the exit is out of reach.
  const deadEndsHold = (platforms: PlatformSpot[], route: number[]) =>
    route.every((id) => !reachesExit(layoutOf(platforms, route), [id], PLAYER_REACH_M));

  // 1. The route: a forward zig-zag walk from the start deck to the exit deck.
  let route: PlatformSpot[] | null = null;
  for (let attempt = 0; attempt < ROUTE_TRIES && !route; attempt++) {
    const path: PlatformSpot[] = [];
    while (path.length < o.routeMax) {
      const prev = path.at(-1);
      let next: PlatformSpot | null = null;
      for (let t = 0; t < STEP_TRIES && !next; t++) {
        const size = pickSize();
        const c = prev
          ? near(rectOf(prev), between(-maxTurn, maxTurn), size)
          : { id: 0, x: round(between(-half + size, half - size)), z: round(between(o.gapMin, o.gapMax) + size / 2), size };
        if (!inWater(c.x, c.z, c.size)) continue;
        if (prev && !inGapRange(edgeGap(rectOf(prev), rectOf(c)))) continue;
        // Only the next step may be in reach: anything closer would let you skip a platform.
        if (prev && edgeGap(start, rectOf(c)) <= PLAYER_REACH_M) continue;
        if (path.slice(0, -1).some((p) => edgeGap(rectOf(p), rectOf(c)) <= PLAYER_REACH_M)) continue;
        const toExit = edgeGap(rectOf(c), exit);
        if (toExit < o.gapMin || (toExit <= PLAYER_REACH_M && !inGapRange(toExit))) continue;
        next = c;
      }
      if (!next) break;
      path.push(next);
      if (inGapRange(edgeGap(rectOf(next), exit))) break; // reached the far side
    }
    const done = path.length >= o.routeMin && inGapRange(edgeGap(rectOf(path.at(-1)!), exit));
    if (done) route = path.map((p, i) => ({ ...p, id: i + 1 }));
  }
  if (!route) throw new Error(`no route for seed ${o.seed}: try another seed or loosen the options`);

  // 2. Dead ends: branch off any platform, spaced like the route, never a shortcut.
  const platforms = [...route];
  const routeIds = route.map((p) => p.id);
  for (let t = 0; t < DEAD_END_TRIES && platforms.length < o.platforms; t++) {
    const parent = platforms[Math.floor(rand() * platforms.length)];
    const c = { ...near(rectOf(parent), between(-Math.PI, Math.PI), pickSize()), id: platforms.length + 1 };
    if (!inWater(c.x, c.z, c.size)) continue;
    if (!inGapRange(edgeGap(rectOf(parent), rectOf(c)))) continue;
    if (platforms.some((p) => edgeGap(rectOf(p), rectOf(c)) < o.gapMin)) continue;
    if (edgeGap(start, rectOf(c)) < o.gapMin || edgeGap(exit, rectOf(c)) < o.gapMin) continue;
    if (!deadEndsHold([...platforms, c], routeIds)) continue;
    platforms.push(c);
  }
  if (platforms.length < o.platforms) {
    throw new Error(`seed ${o.seed}: only ${platforms.length}/${o.platforms} platforms fit: try another seed or fewer platforms`);
  }

  // 3. Number from the start side (like level01), so ids read front to back.
  const sorted = [...platforms].sort((a, b) => a.z - b.z || a.x - b.x);
  const newId = new Map(sorted.map((p, i) => [p.id, i + 1]));
  const layout = layoutOf(
    sorted.map((p) => ({ ...p, id: newId.get(p.id)! })),
    routeIds.map((id) => newId.get(id)!),
  );

  const errors = validateLayout(layout);
  if (errors.length > 0 || !deadEndsHold(layout.platforms, layout.safePath)) {
    throw new Error(`seed ${o.seed}: generated an invalid layout: ${errors.join("; ") || "dead-end rule broken"}`);
  }
  return layout;
}

/** level01-style JSON: one platform per line. */
export function formatLayout(layout: LevelLayout): string {
  return (
    JSON.stringify(layout, null, 2)
      .replace(/"safePath": \[[^\]]*\]/, `"safePath": [${layout.safePath.join(", ")}]`)
      .replace(/\{\s+"id": ([^,]+),\s+"x": ([^,]+),\s+"z": ([^,]+),\s+"size": ([^\s}]+)\s+\}/g, '{ "id": $1, "x": $2, "z": $3, "size": $4 }') + "\n"
  );
}

function parseArgs(argv: string[]): { options: GenOptions; force: boolean } {
  const args = new Map<string, string>();
  for (let i = 0; i < argv.length; i++) {
    if (!argv[i].startsWith("--")) throw new Error(`unexpected argument: ${argv[i]}`);
    const key = argv[i].slice(2);
    if (key === "force") args.set(key, "true");
    else args.set(key, argv[++i]);
  }
  const need = (k: string) => {
    const v = args.get(k);
    if (v === undefined) throw new Error(`missing --${k}`);
    return v;
  };
  const num = (k: string, fallback?: number) => {
    const v = args.has(k) ? Number(args.get(k)) : fallback;
    if (v === undefined || !Number.isFinite(v)) throw new Error(`--${k} must be a number`);
    return v;
  };
  const range = (k: string) => {
    const [lo, hi] = need(k).split("-").map(Number);
    if (!Number.isFinite(lo) || !Number.isFinite(hi) || lo > hi) throw new Error(`--${k} must look like 2.0-2.6`);
    return [lo, hi];
  };
  const [routeMin, routeMax] = range("route");
  const [gapMin, gapMax] = range("gaps");
  return {
    force: args.has("force"),
    options: {
      id: need("id"),
      name: need("name"),
      width: num("width"),
      length: num("length"),
      platforms: num("platforms"),
      routeMin,
      routeMax,
      gapMin,
      gapMax,
      sizes: (args.get("sizes") ?? "2").split(",").map(Number),
      smallChance: num("small", 0),
      seed: num("seed", 1),
    },
  };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const { options, force } = parseArgs(process.argv.slice(2));
    if (!/^[a-z0-9]+$/.test(options.id)) throw new Error("--id must be lowercase letters/digits, e.g. level02");
    const layout = generateLevel(options);
    const file = new URL(`../src/levels/${options.id}.json`, import.meta.url);
    if (existsSync(file) && !force) throw new Error(`${options.id}.json already exists (add --force to overwrite)`);
    writeFileSync(file, formatLayout(layout));

    const byId = new Map(layout.platforms.map((p) => [p.id, rectOf(p)]));
    const steps = [layout.start, ...layout.safePath.map((id) => byId.get(id)!), layout.exit];
    const gaps = steps.slice(1).map((r, i) => edgeGap(steps[i], r));
    console.log(`wrote src/levels/${options.id}.json (seed ${options.seed})`);
    console.log(`${layout.platforms.length} platforms, route ${layout.safePath.length}: ${layout.safePath.join(" → ")}`);
    console.log(`route jumps ${gaps.length}, gaps ${Math.min(...gaps).toFixed(2)}–${Math.max(...gaps).toFixed(2)} m`);
    console.log(`small platforms: ${layout.platforms.filter((p) => p.size !== options.sizes[0]).length}`);
  } catch (e) {
    console.error(`level:gen: ${(e as Error).message}`);
    process.exitCode = 1;
  }
}

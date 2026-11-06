// Level layout generator (Level Roster): seeded, so the same options always give the same hall.
// Builds one dry route (start → exit) and then adds dead ends, keeping only ones that can't
// be used to skip a route platform (checked with reachesExit at the generous player reach).
//
//   npm run level:gen -- --id level02 --name "Deep End" --width 40 --length 42 --platforms 40 \
//     --route 9-11 --gaps 2.0-2.6 --sizes 2,1.6 --small 0.25 --seed 1 [--force]
//   Level 3 extras: --decoys 3-4 --decoy-length 4-6 --ferries 2-3 --ferry-travel 4-5 --ferry-gaps 2.0-2.4
//     --ferry-seconds 3 --ferry-pause 1 --max-turn 70 (degrees a route step may turn; default 45)
//   Level 4 extras: --spread (fill left/centre/right) --crumble 0.5 (share of route tiles) --oneway-decoys
//     --meander (route turns from its own heading: S-curves instead of a straight line)
//
// Writes src/levels/<id>.json (refuses to overwrite unless --force) and prints a summary.
import { existsSync, writeFileSync } from "node:fs";
import { pathToFileURL } from "node:url";

import { edgeGap, PLAYER_REACH_M, reachesExit, toRect, validateLayout, type LevelLayout, type PlatformSpot, type Rect } from "../src/logic/layout.js";

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
  // Level 3 (Level Roster): long dead ends that head forward like the route, and ferries across channels.
  decoys?: [number, number]; // how many decoy branches
  decoyLength?: [number, number]; // jumps per decoy
  ferries?: [number, number]; // how many route platforms move
  ferryTravel?: [number, number]; // metres, end A → end B
  ferryGaps?: [number, number]; // boarding / leaving gaps (shorter: the target moves)
  ferrySeconds?: number; // one way
  ferryPause?: number; // at each end
  // Level 4 (Level Roster): fill the hall left/centre/right, and sinking tiles.
  spread?: boolean; // dead ends grow into the emptiest third; each third must end with ≥ SPREAD_MIN of the platforms
  crumble?: number; // share of route tiles (0..1) that crumble: sink if you stand too long, rise again
  onewayDecoys?: boolean; // decoy tiles sink after you leave them and stay down
  meander?: boolean; // each route step turns a little from the previous heading (S-curves), not from straight ahead
}

const DECK_DEPTH = 4;
const WALL_MARGIN = 0.5; // keep platforms off the pool walls
const ROUTE_TRIES = 3000;
const STEP_TRIES = 200;
const DEAD_END_TRIES = 20000;
const DECOY_TRIES = 300;
const SPREAD_MIN = 0.2;

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
const rectOf = toRect; // a ferry counts as the strip it slides across, so nothing may sit in its way
const atA = (p: PlatformSpot): Rect => ({ x: p.x, z: p.z, width: p.size, depth: p.size });
// Where you leave a platform: a ferry's far end (B), otherwise the platform itself.
const leaveAt = (p: PlatformSpot): Rect => (p.move ? { ...atA(p), x: p.x + p.move.dx, z: p.z + p.move.dz } : atA(p));

/** Centre distance from `from` (heading `angle`, 0 = +z) at which a square of `size` has exactly `gap` of water. */
function distanceForGap(from: Rect, size: number, angle: number, gap: number): number {
  const sx = Math.abs(Math.sin(angle)), sz = Math.abs(Math.cos(angle));
  const hx = (from.width + size) / 2, hz = (from.depth + size) / 2;
  const waterAt = (d: number) => Math.hypot(Math.max(0, d * sx - hx), Math.max(0, d * sz - hz));
  let lo = 0, hi = gap + hx + hz + 1; // waterAt grows with d: bisect
  for (let i = 0; i < 40; i++) {
    const mid = (lo + hi) / 2;
    if (waterAt(mid) < gap) lo = mid; else hi = mid;
  }
  return hi;
}

/** How many jumps the deepest dead end is from the route (through non-route platforms, jumps of ≤ `gapMax`). */
export function decoyDepth(layout: LevelLayout, gapMax: number): number {
  const route = new Set(layout.safePath);
  const others = layout.platforms.filter((p) => !route.has(p.id));
  const depth = new Map<number, number>();
  let frontier = others.filter((p) => layout.platforms.some((q) => route.has(q.id) && edgeGap(toRect(q), toRect(p)) <= gapMax));
  for (let d = 1; frontier.length > 0; d++) {
    for (const p of frontier) depth.set(p.id, d);
    frontier = others.filter((p) => !depth.has(p.id) && frontier.some((q) => edgeGap(toRect(q), toRect(p)) <= gapMax));
  }
  return Math.max(0, ...depth.values());
}

export function generateLevel(o: GenOptions): LevelLayout {
  const rand = makeRng(o.seed);
  const between = (lo: number, hi: number) => lo + rand() * (hi - lo);
  const pickSize = () => (o.sizes.length > 1 && rand() < o.smallChance ? o.sizes[1 + Math.floor(rand() * (o.sizes.length - 1))] : o.sizes[0]);
  const maxTurn = ((o.maxTurnDeg ?? 45) * Math.PI) / 180;

  const half = o.width / 2;
  const start: Rect = { x: 0, z: -DECK_DEPTH / 2, width: o.width, depth: DECK_DEPTH };
  const exit: Rect = { x: 0, z: o.length + DECK_DEPTH / 2, width: o.width, depth: DECK_DEPTH };
  const inWater = (r: Rect) =>
    r.x - r.width / 2 >= -half + WALL_MARGIN && r.x + r.width / 2 <= half - WALL_MARGIN && r.z - r.depth / 2 >= WALL_MARGIN && r.z + r.depth / 2 <= o.length - WALL_MARGIN;
  const inGapRange = (g: number, [lo, hi]: [number, number] = [o.gapMin, o.gapMax]) => g >= lo && g <= hi;
  const ferryGaps = o.ferryGaps ?? [o.gapMin, o.gapMax];

  // A candidate next to `from`, `gaps` of water away, heading `angle` (0 = +z).
  const near = (from: Rect, angle: number, size: number, gaps: [number, number] = [o.gapMin, o.gapMax]): PlatformSpot => {
    const gap = between(gaps[0], gaps[1]);
    // --meander: solve for the centre distance that leaves exactly `gap` of water in this direction (squares come
    // closer on a diagonal). Without it, the old straight-ahead estimate (kept so older seeds reproduce).
    const d = o.meander ? distanceForGap(from, size, angle, gap) : gap + (from.width + size) / 2;
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

  // 1. The route: a forward zig-zag walk from the start deck to the exit deck. A ferry step boards at its
  //    end A and the walk carries on from its end B, so the channel between is only crossable by riding it.
  let route: PlatformSpot[] | null = null;
  for (let attempt = 0; attempt < ROUTE_TRIES && !route; attempt++) {
    const path: PlatformSpot[] = [];
    let ferriesPlaced = 0;
    let sinceFerry = 0;
    let heading = 0; // --meander: the direction the route is currently going (0 = +z)
    while (path.length < o.routeMax) {
      const prev = path.at(-1);
      const makeFerry = !!o.ferries && ferriesPlaced < o.ferries[1] && path.length >= 2 && sinceFerry >= 2 && rand() < 0.4;
      const gaps: [number, number] = makeFerry || prev?.move ? ferryGaps : [o.gapMin, o.gapMax];
      let next: PlatformSpot | null = null;
      for (let t = 0; t < STEP_TRIES && !next; t++) {
        const size = makeFerry ? o.sizes[0] : pickSize(); // ferries are full-size: the target moves
        let angle = prev ? between(-maxTurn, maxTurn) : 0; // same random sequence as before ferries existed
        // Meander: reuse that draw as a turn of up to ±maxTurn/2 from the current heading, kept within ±maxTurn.
        if (o.meander && prev) angle = Math.max(-maxTurn, Math.min(maxTurn, heading + angle / 2));
        const c: PlatformSpot = prev
          ? near(leaveAt(prev), angle, size, gaps)
          : { id: 0, x: round(between(-half + size, half - size)), z: round(between(o.gapMin, o.gapMax) + size / 2), size };
        if (makeFerry) {
          const travel = between(...(o.ferryTravel ?? [4, 5]));
          const seconds = o.ferrySeconds ?? 3, pause = o.ferryPause ?? 1;
          c.move = { dx: round(travel * Math.sin(angle)), dz: round(travel * Math.cos(angle)), seconds, pause, offset: round(rand() * 2 * (seconds + pause)) };
        }
        if (!inWater(rectOf(c))) continue;
        if (prev && !inGapRange(edgeGap(leaveAt(prev), atA(c)), gaps)) continue;
        // Next to a ferry, keep that gap from its whole slide, not just its end (a diagonal ferry swings close).
        if (prev && (c.move || prev.move) && edgeGap(rectOf(prev), rectOf(c)) < gaps[0]) continue;
        // Only the next step may be in reach: anything closer would let you skip a platform.
        if (prev && edgeGap(start, rectOf(c)) <= PLAYER_REACH_M) continue;
        if (path.slice(0, -1).some((p) => edgeGap(rectOf(p), rectOf(c)) <= PLAYER_REACH_M)) continue;
        const toExit = edgeGap(rectOf(c), exit);
        if (toExit < o.gapMin || (toExit <= PLAYER_REACH_M && (c.move || !inGapRange(toExit)))) continue; // never end on a ferry
        next = c;
      }
      if (!next) break;
      path.push(next);
      if (o.meander && path.length > 1) {
        const from = leaveAt(path[path.length - 2]);
        heading = Math.atan2(next.x - from.x, next.z - from.z);
      }
      if (next.move) { ferriesPlaced++; sinceFerry = 0; } else sinceFerry++;
      if (!next.move && inGapRange(edgeGap(rectOf(next), exit))) break; // reached the far side
    }
    const last = path.at(-1);
    const done = !!last && !last.move && path.length >= o.routeMin && inGapRange(edgeGap(rectOf(last), exit)) && ferriesPlaced >= (o.ferries?.[0] ?? 0);
    if (done) route = path.map((p, i) => ({ ...p, id: i + 1 }));
  }
  if (!route) throw new Error(`no route for seed ${o.seed}: try another seed or loosen the options`);

  // Crumbling tiles: a random share of the route (never a ferry).
  if (o.crumble) {
    const pool = route.filter((p) => !p.move);
    for (let k = Math.round(o.crumble * route.length); k > 0 && pool.length > 0; k--) {
      pool.splice(Math.floor(rand() * pool.length), 1)[0].sink = "crumble";
    }
  }

  const platforms = [...route];
  const routeIds = route.map((p) => p.id);
  // Left / centre / right thirds of the hall, for --spread.
  const thirdOf = (x: number) => (x < -o.width / 6 ? 0 : x > o.width / 6 ? 2 : 1);
  const thirdCounts = () => {
    const counts = [0, 0, 0];
    for (const p of platforms) counts[thirdOf(p.x)]++;
    return counts;
  };
  const emptiestThird = () => {
    const counts = thirdCounts();
    return counts.indexOf(Math.min(...counts));
  };
  // A new platform fits if it's in the water, spaced like the route, and can't be used to skip a route platform.
  const fits = (c: PlatformSpot, others: PlatformSpot[]) =>
    inWater(rectOf(c)) &&
    !others.some((p) => edgeGap(rectOf(p), rectOf(c)) < o.gapMin) &&
    edgeGap(start, rectOf(c)) >= o.gapMin && edgeGap(exit, rectOf(c)) >= o.gapMin &&
    deadEndsHold([...others, c], routeIds);

  // 2a. Decoys: chains that peel off the route sideways, then head forward like the real route and stop in the fog.
  const decoysWanted = o.decoys ? o.decoys[0] + Math.floor(rand() * (o.decoys[1] - o.decoys[0] + 1)) : 0;
  const [decoyMin, decoyMax] = o.decoyLength ?? [4, 6];
  let decoysMade = 0;
  const anchors = route.filter((p, i) => !p.move && i < route.length - 2);
  for (let t = 0; t < DECOY_TRIES && decoysMade < decoysWanted; t++) {
    const root = anchors[Math.floor(rand() * anchors.length)];
    const length = decoyMin + Math.floor(rand() * (decoyMax - decoyMin + 1));
    const side = rand() < 0.5 ? -1 : 1;
    const chain: PlatformSpot[] = [];
    while (chain.length < length) {
      const from = chain.at(-1) ?? root;
      let placed: PlatformSpot | null = null;
      for (let s = 0; s < STEP_TRIES && !placed; s++) {
        const angle = chain.length === 0 ? side * between(maxTurn, 2 * maxTurn) : between(-maxTurn, maxTurn);
        const c = { ...near(atA(from), angle, pickSize()), id: platforms.length + chain.length + 1 };
        if (fits(c, [...platforms, ...chain])) placed = c;
      }
      if (!placed) break;
      chain.push(placed);
    }
    if (chain.length >= decoyMin) {
      if (o.onewayDecoys) for (const p of chain) p.sink = "oneway";
      platforms.push(...chain);
      decoysMade++;
    }
  }
  if (decoysMade < (o.decoys?.[0] ?? 0)) throw new Error(`seed ${o.seed}: only ${decoysMade} decoys fit: try another seed or shorter decoys`);

  // 2b. Short dead ends: branch off any platform, spaced like the route, never a shortcut.
  for (let t = 0; t < DEAD_END_TRIES && platforms.length < o.platforms; t++) {
    let parent = platforms[Math.floor(rand() * platforms.length)];
    let angle = between(-Math.PI, Math.PI);
    if (o.spread && rand() < 0.5) {
      // Half the tries grow from the platforms nearest the emptiest third, heading roughly toward it (always doing
      // it stalls once that patch is full); the rest grow anywhere.
      const target = (emptiestThird() - 1) * (o.width / 3);
      const nearest = [...platforms].sort((a, b) => Math.abs(a.x - target) - Math.abs(b.x - target)).slice(0, 8);
      parent = nearest[Math.floor(rand() * nearest.length)];
      angle = Math.sign(target - parent.x || 1) * Math.PI / 2 + between(-Math.PI / 2, Math.PI / 2);
    }
    const c = { ...near(rectOf(parent), angle, pickSize()), id: platforms.length + 1 };
    if (!inGapRange(edgeGap(rectOf(parent), rectOf(c)))) continue;
    if (!fits(c, platforms)) continue;
    platforms.push(c);
  }
  if (platforms.length < o.platforms) {
    throw new Error(`seed ${o.seed}: only ${platforms.length}/${o.platforms} platforms fit: try another seed or fewer platforms`);
  }
  if (o.spread && thirdCounts().some((c) => c < SPREAD_MIN * platforms.length)) {
    throw new Error(`seed ${o.seed}: left/centre/right = ${thirdCounts().join("/")}, not spread enough: try another seed`);
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
    if (["force", "spread", "oneway-decoys", "meander"].includes(key)) args.set(key, "true");
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
  const optionalRange = (k: string): [number, number] | undefined => {
    if (!args.has(k)) return undefined;
    const [lo, hi] = range(k);
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
      decoys: optionalRange("decoys"),
      decoyLength: optionalRange("decoy-length"),
      ferries: optionalRange("ferries"),
      ferryTravel: optionalRange("ferry-travel"),
      ferryGaps: optionalRange("ferry-gaps"),
      ferrySeconds: args.has("ferry-seconds") ? num("ferry-seconds") : undefined,
      ferryPause: args.has("ferry-pause") ? num("ferry-pause") : undefined,
      maxTurnDeg: args.has("max-turn") ? num("max-turn") : undefined,
      spread: args.has("spread"),
      crumble: args.has("crumble") ? num("crumble") : undefined,
      onewayDecoys: args.has("oneway-decoys"),
      meander: args.has("meander"),
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
    const ferries = layout.platforms.filter((p) => p.move).map((p) => p.id);
    if (ferries.length > 0) console.log(`ferries: ${ferries.join(", ")}`);
    console.log(`deepest dead end: ${decoyDepth(layout, options.gapMax)} jumps from the route`);
    const sinking = (kind: string) => layout.platforms.filter((p) => p.sink === kind).length;
    if (options.crumble || options.onewayDecoys) console.log(`crumbling: ${sinking("crumble")}, one-way: ${sinking("oneway")}`);
    const thirds = [0, 0, 0];
    for (const p of layout.platforms) thirds[p.x < -options.width / 6 ? 0 : p.x > options.width / 6 ? 2 : 1]++;
    console.log(`left / centre / right: ${thirds.join(" / ")}`);
  } catch (e) {
    console.error(`level:gen: ${(e as Error).message}`);
    process.exitCode = 1;
  }
}

// After a level was edited by hand in Unity (Still Water → Export Level to JSON): tidy the JSON and set the route
// to the easiest way through (easiestRoute), so the server tests check the level you actually play.
//
//   npm run level:route -- --id level03 [--max-gap 3.1] [--hand-tuned]
// --hand-tuned drops "oneDryRoute": the dead-end proof no longer applies (you designed the alternatives yourself).
import { readFileSync, writeFileSync } from "node:fs";

import { easiestRoute, edgeGap, toRect, validateLayout, type LevelLayout, type PlatformSpot } from "../src/logic/layout.js";
import { formatLayout } from "./generate-level.js";

const args = process.argv.slice(2);
const arg = (k: string) => {
  const i = args.indexOf(`--${k}`);
  return i >= 0 ? args[i + 1] : undefined;
};

try {
  const id = arg("id");
  if (!id || !/^[a-z0-9]+$/.test(id)) throw new Error("--id must be like level03");
  const cm = (v: number) => Math.round(v * 100) / 100;
  const file = new URL(`../src/levels/${id}.json`, import.meta.url);
  const raw = JSON.parse(readFileSync(file, "utf8")) as LevelLayout & { oneDryRoute?: boolean; maxGap?: number };

  // Unity's JSON writer fills every field: drop the empty ones so the file reads like the generated levels.
  const platforms: PlatformSpot[] = raw.platforms.map((p) => {
    const moves = p.move && Math.hypot(p.move.dx, p.move.dz) > 0 && p.move.seconds > 0;
    // Round to 1 cm: Unity saves 3.6 as 3.5999999…, which can push a jump generated at exactly 2.8 m just over.
    const spot: PlatformSpot = { id: p.id, x: cm(p.x), z: cm(p.z), size: cm(p.size) };
    if (moves) spot.move = p.move;
    if (p.sink) spot.sink = p.sink; // Unity writes "" for none
    return spot;
  });
  const maxGapArg = arg("max-gap");
  const maxGap = maxGapArg !== undefined ? Number(maxGapArg) : raw.maxGap;
  const layout: LevelLayout = {
    id: raw.id,
    name: raw.name,
    ...(raw.oneDryRoute && !args.includes("--hand-tuned") ? { oneDryRoute: true } : {}),
    ...(maxGap && maxGap > 0 ? { maxGap } : {}),
    water: raw.water,
    platformTopY: raw.platformTopY,
    exitTopY: raw.exitTopY,
    start: raw.start,
    exit: raw.exit,
    safePath: [],
    platforms: [...platforms].sort((a, b) => a.id - b.id),
  };

  const route = easiestRoute(layout);
  if (!route) throw new Error(`no way through with jumps of ≤ ${layout.maxGap ?? 2.8} m: move some platforms closer in Unity, or raise --max-gap (≤ 3.1)`);
  layout.safePath = route;
  writeFileSync(file, formatLayout(layout));

  const byId = new Map(layout.platforms.map((p) => [p.id, p]));
  const steps = [layout.start, ...route.map((r) => toRect(byId.get(r) as PlatformSpot)), layout.exit];
  const gaps = steps.slice(1).map((r, i) => edgeGap(steps[i], r));
  console.log(`wrote src/levels/${id}.json: ${layout.platforms.length} platforms`);
  console.log(`route (${route.length + 1} jumps): ${route.map((r) => (byId.get(r)?.move ? `${r}F` : r)).join(" → ")}`);
  console.log(`longest jump ${Math.max(...gaps).toFixed(2)} m`);
  const problems = validateLayout(layout);
  if (problems.length > 0) console.log(`still to fix (npm test will fail):\n  ${problems.join("\n  ")}`);
} catch (e) {
  console.error(`level:route: ${(e as Error).message}`);
  process.exitCode = 1;
}

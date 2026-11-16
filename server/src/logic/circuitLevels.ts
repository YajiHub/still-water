// The five level layouts the online circuit joins end to end. They are imported (not read from disk), so the
// compiled build/ (the Docker runtime image has nothing else) carries them.
import level01 from "../levels/level01.json" with { type: "json" };
import level02 from "../levels/level02.json" with { type: "json" };
import level03 from "../levels/level03.json" with { type: "json" };
import level04 from "../levels/level04.json" with { type: "json" };
import level05 from "../levels/level05.json" with { type: "json" };
import listenerTuning from "./listenerTuning.json" with { type: "json" };
import { CIRCUIT_CONFIG, buildCircuit, type CircuitRoom, type ListenerTuning } from "./circuit.js";
import type { LevelLayout } from "./layout.js";

export const CIRCUIT_LAYOUTS = [level01, level02, level03, level04, level05] as unknown as LevelLayout[];

// Each room's Listener as fast as in the solo level (the client's EditMode test checks they match the LevelConfigs).
export const LISTENER_TUNING = listenerTuning as Record<string, Partial<ListenerTuning>>;

export const CIRCUIT_ROOMS: CircuitRoom[] = buildCircuit(CIRCUIT_LAYOUTS, CIRCUIT_CONFIG, LISTENER_TUNING);

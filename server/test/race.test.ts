import { describe, it } from "node:test";
import assert from "node:assert/strict";

import {
  RACE_CONFIG,
  addPlayer,
  applyPosition,
  createRaceState,
  handleDisconnect,
  markReady,
  tickRace,
  toSnapshot,
} from "../src/logic/race.js";

function setupRacing() {
  const state = createRaceState();
  addPlayer(state, "p1", RACE_CONFIG, () => 0.5);
  addPlayer(state, "p2", RACE_CONFIG, () => 0.5);
  markReady(state, "p1");
  markReady(state, "p2");
  tickRace(state, RACE_CONFIG.countdownSeconds, RACE_CONFIG, () => 0.5);
  return state;
}

describe("race logic", () => {
  it("waits until both players are ready, then counts down into racing", () => {
    const state = createRaceState();
    assert.equal(addPlayer(state, "p1"), true);
    markReady(state, "p1");
    assert.equal(state.phase, "waiting");

    assert.equal(addPlayer(state, "p2"), true);
    assert.equal(addPlayer(state, "p3"), false);
    markReady(state, "p2");
    assert.equal(state.phase, "countdown");
    assert.equal(state.countdown, RACE_CONFIG.countdownSeconds);

    tickRace(state, 1);
    assert.equal(state.phase, "countdown");
    assert.equal(state.countdown, RACE_CONFIG.countdownSeconds - 1);

    tickRace(state, RACE_CONFIG.countdownSeconds);
    assert.equal(state.phase, "racing");
  });

  it("rejects out-of-bounds and teleport inputs, accepts valid steps", () => {
    const state = setupRacing();
    assert.equal(applyPosition(state, "p1", { x: 0, y: 0.55, z: -2, yaw: 15 }, 1000), true);
    assert.equal(applyPosition(state, "p1", { x: 10, y: 0.55, z: 10 }, 1100), false);
    assert.equal(applyPosition(state, "p1", { x: 99, y: 0.55, z: 0 }, 10000), false);
    assert.equal(applyPosition(state, "p1", { x: NaN, y: 0.55, z: 0 }, 1100), false);

    const p1 = state.players.get("p1")!;
    assert.equal(p1.x, 0);
    assert.equal(p1.z, -2);
    assert.equal(p1.yaw, 15);
  });

  it("counts splashes from water entry, hunts, and catches back to start with penalty", () => {
    const state = setupRacing();
    assert.equal(applyPosition(state, "p1", { x: 0, y: -0.3, z: 12.75 }, 1000), true);
    const p1 = state.players.get("p1")!;
    assert.equal(p1.inWater, true);
    assert.equal(p1.splashes, 1);
    assert.equal(p1.monsterState, "alerted");

    // Alert delay completes -> hunting; monster starts at (0, 12.75) right on player -> immediate catch.
    const respawns = tickRace(state, RACE_CONFIG.listener.alertDelaySeconds + 0.1, RACE_CONFIG, () => 0.5);
    assert.equal(respawns.length, 1);
    assert.deepEqual(respawns[0], { id: "p1", ...RACE_CONFIG.spawn });
    assert.equal(p1.penalty, RACE_CONFIG.catchPenaltySeconds);
    assert.equal(p1.inWater, false);
    assert.equal(applyPosition(state, "p1", { x: 0, y: 0.55, z: -1 }, 2000), false);

    tickRace(state, RACE_CONFIG.catchPenaltySeconds, RACE_CONFIG, () => 0.5);
    assert.equal(p1.penalty, 0);
    assert.equal(applyPosition(state, "p1", { x: 0, y: 0.55, z: -1 }, 6000), true);
  });

  it("declares winner when reaching the exit door", () => {
    const state = setupRacing();
    assert.equal(applyPosition(state, "p2", { x: 0, y: 1.1, z: 28.5 }, 1000), true);
    assert.equal(state.phase, "finished");
    assert.equal(state.winner, "p2");
  });

  it("picks closest racer on timeout or draw when tied", () => {
    const state = setupRacing();
    applyPosition(state, "p1", { x: 0, y: 0.55, z: 20 }, 1000);
    applyPosition(state, "p2", { x: 0, y: 0.55, z: 5 }, 1000);
    tickRace(state, RACE_CONFIG.matchSeconds, RACE_CONFIG, () => 0.5);
    assert.equal(state.phase, "finished");
    assert.equal(state.winner, "p1");

    const drawState = setupRacing();
    tickRace(drawState, RACE_CONFIG.matchSeconds, RACE_CONFIG, () => 0.5);
    assert.equal(drawState.phase, "finished");
    assert.equal(drawState.winner, "");
    assert.match(drawState.reason, /draw/i);
  });

  it("awards forfeit on active disconnect and cancels cleanly while waiting", () => {
    const waiting = createRaceState();
    addPlayer(waiting, "p1");
    handleDisconnect(waiting, "p1");
    assert.equal(waiting.phase, "waiting");
    assert.equal(waiting.players.size, 0);

    const active = setupRacing();
    handleDisconnect(active, "p1");
    assert.equal(active.phase, "finished");
    assert.equal(active.winner, "p2");
    assert.equal(toSnapshot(active).winner, "p2");
  });
});

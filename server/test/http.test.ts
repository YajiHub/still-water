import { describe, it, before, after } from "node:test";
import assert from "node:assert/strict";
import { ColyseusTestServer, boot } from "@colyseus/testing";

import appConfig from "../src/app.config.js";

// Lab 6: the pipeline's health check and the "change the homepage text" demo depend on these two routes.
describe("HTTP routes", () => {
  let colyseus: ColyseusTestServer<typeof appConfig>;

  before(async () => { colyseus = await boot(appConfig); });
  after(async () => { await colyseus.shutdown(); });

  it("GET /health reports ok and the version (dev when APP_VERSION is unset)", async () => {
    const res = await colyseus.http.get("/health");
    assert.equal(res.statusCode, 200);
    assert.deepEqual(res.data, { status: "ok", version: "dev" });
  });

  it("GET /health reports the deployed commit SHA from APP_VERSION", async () => {
    process.env.APP_VERSION = "abc1234";
    try {
      const res = await colyseus.http.get("/health");
      assert.deepEqual(res.data, { status: "ok", version: "abc1234" });
    } finally {
      delete process.env.APP_VERSION;
    }
  });

  it("GET / shows the status page with the version", async () => {
    const res = await colyseus.http.get("/");
    assert.equal(res.statusCode, 200);
    assert.match(String(res.data), /Still Water server/);
    assert.match(String(res.data), /version dev/);
  });
});

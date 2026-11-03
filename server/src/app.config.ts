import {
  defineServer,
  defineRoom,
  monitor,
  playground,
  createRouter,
  createEndpoint,
} from "colyseus";

/**
 * Import your Room files
 */
import { MyRoom } from "./rooms/MyRoom.js";

const server = defineServer({

  /**
   * Define your room handlers:
   */
  rooms: {
    my_room: defineRoom(MyRoom),
  },

  /**
   * Experimental: Define API routes. Built-in integration with the "playground" and SDK.
   *
   * Usage from SDK:
   *   client.http.get("/api/hello").then((response) => {})
   *
   */
  routes: createRouter({
    api_hello: createEndpoint("/api/hello", { method: "GET" }, async () => {
      return { message: "Hello World" };
    }),
  }),

  /**
   * Bind your custom express routes here:
   * Read more: https://expressjs.com/en/starter/basic-routing.html
   */
  express: (app) => {

    // APP_VERSION is the commit SHA in deploys (set by CI/CD) and "dev" locally. Read per request so tests can set it.
    const version = () => process.env.APP_VERSION ?? "dev";

    // Lab 6 Test A edits this text to prove a merge reaches the VM without touching it.
    app.get("/", (_req, res) => {
      res.type("html").send(
        `<h1>Still Water server</h1><p>Something in the water is listening.</p><p>version ${version()}</p>`,
      );
    });

    // The deploy job's health-check loop waits until this returns the new commit SHA.
    app.get("/health", (_req, res) => {
      res.json({ status: "ok", version: version() });
    });

    /**
     * Use @colyseus/monitor
     * If you expose it in production, make sure to protect it with a password:
     * https://docs.colyseus.io/tools/monitoring#password-protection
     */
    if (process.env.NODE_ENV !== "production") {
      app.use("/monitor", monitor());
    }

    /**
     * Use @colyseus/playground
     * (It is not recommended to expose this route in a production environment)
     */
    if (process.env.NODE_ENV !== "production") {
      app.use("/playground", playground());
    }
  }
});

export default server;


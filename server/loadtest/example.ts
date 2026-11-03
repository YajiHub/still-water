import { Client, Room } from "@colyseus/sdk";
import { cli, Options } from "@colyseus/loadtest";

export async function main(options: Options) {
  const client = new Client(options.endpoint);
  const room: Room = await client.joinOrCreate(options.roomName, {
    // your join options here...
  });

  console.log("joined successfully!");

  room.onMessage("message-type", () => {
    // logic
  });

  room.onStateChange((state: unknown) => {
    console.log("state change:", state);
  });

  room.onLeave(() => {
    console.log("left");
  });
}

cli(main);

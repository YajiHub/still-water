using System.Collections;
using StillWater.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StillWater.Net
{
    /// <summary>
    /// Builds the online co-op circuit inside the Level01 scene. Rooms 2–5 are loaded from their own level scenes,
    /// stripped to their "Level" geometry before any of their scripts start, and moved to the offsets the server sent.
    /// At each join, the two end walls are replaced by one partition with an open doorway: it's a race, every door is
    /// open from the start. Only the rooms around you stay active, so a phone draws at most three.
    /// </summary>
    public class CircuitWorld : MonoBehaviour
    {
        // Looks: the doorway size (m), and the wall height and thickness the level builder uses.
        private const float DoorWidth = 2.4f, DoorHeight = 2.7f, WallThickness = 0.5f, WallHeight = 10f;

        public bool Ready { get; private set; }
        public int RoomCount => rooms?.Length ?? 0;

        private CircuitRoomGeometry[] rooms;
        private GameObject[] roots;
        private int pendingRoom = -1;
        private int shownRoom = -1;

        public IEnumerator Build(CircuitGeometry geometry)
        {
            rooms = geometry.rooms;
            roots = new GameObject[rooms.Length];
            roots[0] = FindRoot(SceneManager.GetActiveScene(), "Level");
            if (roots[0] != null) roots[0].transform.position = Offset(0);

            SceneManager.sceneLoaded += Strip;
            for (int i = 1; i < rooms.Length; i++)
            {
                string scene = SceneFor(rooms[i].id);
                if (scene == null) { Debug.LogWarning($"Circuit: no level scene for {rooms[i].id}"); continue; }
                pendingRoom = i;
                yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            }
            SceneManager.sceneLoaded -= Strip;
            pendingRoom = -1;

            for (int i = 0; i + 1 < rooms.Length; i++) Join(i);
            var look = FindFirstObjectByType<Level.LookSwitch>();
            if (look != null) look.Refresh(); // the new rooms' lamps take the look too
            Show(0);
            Ready = true;
        }

        /// <summary>You're in <paramref name="room"/>: draw it and its neighbours only.</summary>
        public void Show(int room)
        {
            if (roots == null || room == shownRoom) return;
            shownRoom = room;
            for (int i = 0; i < roots.Length; i++)
                if (roots[i] != null) roots[i].SetActive(i >= room - 1 && i <= room + 1);
        }

        public CircuitRoomGeometry Room(int i) => rooms != null && i >= 0 && i < rooms.Length ? rooms[i] : null;

        private Vector3 Offset(int i) => new Vector3(rooms[i].offsetX, 0f, rooms[i].offsetZ);

        // Called for each additively loaded scene before any of its scripts Start: keep the geometry, switch off the
        // rest at once (their LevelController, player, camera and Listener must never run), then delete it.
        private void Strip(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive || pendingRoom < 0) return;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == "Level" && roots[pendingRoom] == null)
                {
                    go.transform.position = Offset(pendingRoom);
                    roots[pendingRoom] = go;
                    continue;
                }
                go.SetActive(false);
                Destroy(go);
            }
        }

        // Room i's exit end meets room i+1's start end: one partition the width of the wider room, with an open doorway
        // where room i's door was.
        private void Join(int i)
        {
            var a = roots[i];
            var b = roots[i + 1];
            if (a == null || b == null) return;
            var front = a.transform.Find("WallFront");
            var back = b.transform.Find("WallBack");
            Material wall = front != null ? front.GetComponent<Renderer>().sharedMaterial : null;
            float width = Mathf.Max(Width(front), Width(back));
            if (front != null) Destroy(front.gameObject);
            if (back != null) Destroy(back.gameObject);

            var room = rooms[i];
            float z = room.maxZ + WallThickness / 2f;
            float floorY = room.exitTopY;
            float doorX = room.door.x;
            float bottom = -4f, top = floorY + WallHeight;
            var partition = new GameObject($"Junction_{i + 1}_{i + 2}");
            partition.transform.SetParent(a.transform, true);
            float leftW = (doorX - DoorWidth / 2f) - (doorX - width / 2f);
            Wall(partition, wall, new Vector3(doorX - width / 2f + leftW / 2f, (bottom + top) / 2f, z), new Vector3(leftW, top - bottom, WallThickness));
            Wall(partition, wall, new Vector3(doorX + DoorWidth / 2f + leftW / 2f, (bottom + top) / 2f, z), new Vector3(leftW, top - bottom, WallThickness));
            Wall(partition, wall, new Vector3(doorX, (floorY + DoorHeight + top) / 2f, z), new Vector3(DoorWidth, top - floorY - DoorHeight, WallThickness));
            Wall(partition, wall, new Vector3(doorX, (bottom + floorY) / 2f, z), new Vector3(DoorWidth, floorY - bottom, WallThickness));

            // The race runs straight through: no door in this doorway (the last room keeps its exit door as the finish).
            var door = a.GetComponentInChildren<Level.ExitDoor>(true);
            if (door != null) Destroy(door.gameObject);
        }

        private static float Width(Transform wall) => wall != null ? wall.lossyScale.x : 0f;

        private static void Wall(GameObject parent, Material mat, Vector3 center, Vector3 size)
        {
            if (size.x <= 0.01f || size.y <= 0.01f) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall";
            go.transform.SetParent(parent.transform, false);
            go.transform.position = center;
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects()) if (go.name == name) return go;
            return null;
        }

        private static string SceneFor(string levelId)
        {
            foreach (var level in GameState.Catalog.levels)
                if (level != null && level.id == levelId) return level.sceneName;
            return null;
        }
    }
}

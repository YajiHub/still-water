using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StillWater.Core;
using StillWater.Level;
using StillWater.Monster;
using StillWater.Player;
using StillWater.UI;
using UnityEngine;

namespace StillWater.Net
{
    /// <summary>
    /// The online co-op race circuit (decision 07) on top of Level01's rig and art. The server decides everything that
    /// matters: where each room's Listener is, who it catches, who reached which door, the places, scores and the
    /// result. This shows it:
    /// - builds the five rooms with open doorways (CircuitWorld);
    /// - moves you and mirrors 1–3 teammates;
    /// - draws one Listener per room;
    /// - plays every catch where it happened (the catch camera too, when it's you);
    /// - relays pushes;
    /// - runs the HUD.
    /// </summary>
    public class OnlineLevelController : MonoBehaviour
    {
        // Looks: how fast remote players and the Listeners catch up with the server, how deep a Listener ducks under a
        // platform, how close counts as "on top of you" for the music, how long banners and the catch show.
        private const float Follow = 12f, DiveDepth = 2.2f, DepthSpeed = 3.5f, ThreatRange = 15f, BannerSeconds = 3f;
        private const float SendHz = 15f, CatchSeconds = 2.4f;
        // Looks: the server sends 10 updates a second; between them a Listener is drawn where it is now (its last
        // known position + its velocity), not ~0.1 s behind (at 18-20 m/s that read as "too slow"). Capped so a
        // late packet can't fling it far.
        private const float MaxPredictSeconds = 0.15f, MaxListenerSpeed = 30f, ListenerFollow = 20f;

        private class Remote
        {
            public GameObject go;
            public Animator animator;
            public string clip;
            public Vector3 last;
            public int splashes;
        }

        private class RoomListener
        {
            public Transform root;
            public Animator animator;
            public Transform chest, head;
            public string clip;
            public float depth, diveHold, lungeUntil;
            public Vector3 server, velocity; // last known position from the server, and how fast it was going
            public float serverTime;
            public bool known;
        }

        private PlayerMotor motor;
        private TouchControls touch;
        private CameraModeSwitch cameras;
        private LanRace session;
        private CircuitWorld world;
        private PoolWater water;
        private AudioClip splashClip, screamClip;
        private readonly Dictionary<string, Remote> remotes = new Dictionary<string, Remote>();
        private RoomListener[] monsters = new RoomListener[0];
        private float sendAt, bannerUntil;
        private string banner = "";
        private bool menu, resultStinger, building = true, myCatchPlaying;
        private int shownRoom = -1, lastSplashes, lastPlace;
        private Vector3? pendingRespawn;
        private CircuitSnapshot lastSnapshot; // a new object per server update

        private IEnumerator Start()
        {
            session = LanRace.Instance;
            motor = FindFirstObjectByType<PlayerMotor>();
            touch = FindFirstObjectByType<TouchControls>();
            cameras = FindFirstObjectByType<CameraModeSwitch>();
            water = FindFirstObjectByType<PoolWater>();
            splashClip = Resources.Load<AudioClip>("Audio/Splash");
            if (splashClip == null) splashClip = ProceduralAudio.Splash();
            screamClip = Resources.Load<AudioClip>("Music/sfx_listener_scream");
            motor.InputEnabled = false;
            motor.enabled = false;
            touch.enabled = false;
            touch.ShowPush = true;
            var template = FindFirstObjectByType<ListenerAI>();
            if (template != null) template.enabled = false; // the server's Listeners, not the offline AI
            yield return null; // camera Start must finish before the circuit takes control
            if (session == null) yield break;

            for (float waited = 0f; session.Geometry == null && waited < 6f; waited += Time.unscaledDeltaTime) yield return null;
            if (session.Geometry == null || session.Geometry.rooms == null || session.Geometry.rooms.Length == 0)
            {
                session.ReturnToLobby("The server sent no circuit. Is it up to date?");
                yield break;
            }
            world = gameObject.AddComponent<CircuitWorld>();
            yield return world.Build(session.Geometry);
            MakeListeners(template, world.RoomCount);
            building = false;

            touch.PausePressed += ToggleMenu;
            touch.PushPressed += OnPushPressed;
            session.Respawned += Respawn;
            session.Caught += OnCaught;
            session.Pushed += OnPushed;
            session.Shoved += OnShoved;
            var me = session.Snapshot?.Find(session.SessionId);
            var spawn = world.Room(0).spawn;
            motor.Teleport(me != null ? new Vector3(me.x, me.y, me.z) : new Vector3(spawn.x, spawn.y, spawn.z));
            MusicDirector.Instance.Play(MusicDirector.Race);
            MusicDirector.Instance.SetThreat(0f);
            session.Ready(PlayerAvatar.Choice);
        }

        // One Listener body per room: the scene's Listener for room 1, copies for the others.
        private void MakeListeners(ListenerAI template, int count)
        {
            if (template == null) return;
            monsters = new RoomListener[count];
            for (int i = 0; i < count; i++)
            {
                var go = i == 0 ? template.gameObject : Instantiate(template.gameObject);
                if (i > 0)
                {
                    go.name = $"Listener (room {i + 1})";
                    var ai = go.GetComponent<ListenerAI>();
                    if (ai != null) Destroy(ai);
                }
                var anim = go.GetComponentInChildren<Animator>(true);
                monsters[i] = new RoomListener
                {
                    root = go.transform,
                    animator = anim,
                    chest = ListenerAI.FindBone(anim, HumanBodyBones.Chest, "Chest"),
                    head = ListenerAI.FindBone(anim, HumanBodyBones.Head, "Head"),
                };
                var start = world.Room(i);
                if (start != null) go.transform.position = new Vector3((start.water.minX + start.water.maxX) / 2f, 0f, (start.water.minZ + start.water.maxZ) / 2f);
            }
        }

        private void Update()
        {
            if (session == null || motor == null) return;
            var s = session.Snapshot;
            var me = s?.Find(session.SessionId);
            bool running = !building && !myCatchPlaying && s != null && s.phase == "running" && me != null && me.penalty <= 0f && me.place == 0;
            motor.enabled = running;
            motor.InputEnabled = running && !menu;
            touch.enabled = running && !menu;
            if (s == null || building) return;

            if (me != null)
            {
                if (me.room != shownRoom)
                {
                    world.Show(me.room);
                    if (shownRoom >= 0 && me.room > shownRoom)
                    {
                        var room = world.Room(me.room);
                        Banner($"ROOM {me.room + 1} / {s.roomCount}" + (room != null && !string.IsNullOrEmpty(room.name) ? $"  ·  {room.name}" : ""));
                        AudioSource.PlayClipAtPoint(ProceduralAudio.Chime(660f, 1.2f), motor.transform.position, 0.6f);
                    }
                    shownRoom = me.room;
                }
                if (me.place > 0 && lastPlace == 0) Banner(me.place == 1 ? "FIRST THROUGH!" : $"THROUGH: {Ordinal(me.place)}");
                lastPlace = me.place;
                if (me.splashes > lastSplashes && splashClip != null) AudioSource.PlayClipAtPoint(splashClip, motor.transform.position);
                lastSplashes = me.splashes;
            }

            SyncRemotes(s);
            UpdateListeners(s, me);

            if (running && Time.unscaledTime >= sendAt)
            {
                sendAt = Time.unscaledTime + 1f / SendHz;
                session.Position(motor.transform.position, motor.transform.eulerAngles.y);
            }
            UpdateMusic(s, me);
        }

        // ---------------------------------------------------------------- teammates

        private void SyncRemotes(CircuitSnapshot s)
        {
            if (s.players == null) return;
            var seen = new HashSet<string>();
            foreach (var p in s.players)
            {
                if (p == null || p.id == session.SessionId) continue;
                seen.Add(p.id);
                if (!remotes.TryGetValue(p.id, out var r))
                {
                    r = new Remote { go = new GameObject($"Player P{p.slot + 1} (no collision)"), splashes = p.splashes };
                    r.go.transform.position = new Vector3(p.x, p.y, p.z);
                    r.last = r.go.transform.position;
                    var body = PlayerAvatar.Spawn(p.body, r.go.transform);
                    if (body != null) r.animator = body.GetComponent<Animator>();
                    remotes[p.id] = r;
                }
                float k = 1f - Mathf.Exp(-Follow * Time.deltaTime);
                var t = r.go.transform;
                var target = new Vector3(p.x, p.y, p.z);
                if ((target - t.position).sqrMagnitude > 25f) t.position = target; // a respawn: jump, don't glide across rooms
                else t.position = Vector3.Lerp(t.position, target, k);
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(0f, p.yaw, 0f), k);
                Animate(r);
                if (p.splashes > r.splashes && splashClip != null) AudioSource.PlayClipAtPoint(splashClip, t.position); // you hear the others
                r.splashes = p.splashes;
            }
            foreach (var gone in remotes.Keys.Where(id => !seen.Contains(id)).ToList())
            {
                Destroy(remotes[gone].go);
                remotes.Remove(gone);
            }
        }

        // Speed from how far the interpolated player moved; below the surface = swimming (only positions are sent).
        private void Animate(Remote r)
        {
            if (r.animator == null) return;
            Vector3 pos = r.go.transform.position;
            Vector3 d = pos - r.last;
            r.last = pos;
            float speed = new Vector2(d.x, d.z).magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
            bool swimming = water != null && pos.y < water.SurfaceY;
            string next = PlayerLook.ClipFor(swimming ? PlayerMotor.MotorState.Swimming : PlayerMotor.MotorState.Grounded, speed);
            if (next != r.clip) r.animator.CrossFadeInFixedTime(next, 0.2f);
            r.clip = next;
            r.animator.speed = PlayerLook.Playback(next, speed);
        }

        // ---------------------------------------------------------------- the Listeners

        private void UpdateListeners(CircuitSnapshot s, CircuitPlayer me)
        {
            if (s.listeners == null) return;
            float surface = water != null ? water.SurfaceY : 0f;
            bool fresh = !ReferenceEquals(s, lastSnapshot);
            lastSnapshot = s;
            foreach (var l in s.listeners)
            {
                if (l == null || l.room < 0 || l.room >= monsters.Length || monsters[l.room] == null) continue;
                var m = monsters[l.room];
                if (fresh)
                {
                    var pos = new Vector3(l.x, 0f, l.z);
                    float dt = Time.time - m.serverTime;
                    m.velocity = m.known && dt > 0.02f ? Vector3.ClampMagnitude((pos - m.server) / dt, MaxListenerSpeed) : Vector3.zero;
                    m.server = pos;
                    m.serverTime = Time.time;
                    m.known = true;
                }
                bool near = me == null || Mathf.Abs(l.room - me.room) <= 1;
                m.root.gameObject.SetActive(near); // only the rooms around you are drawn
                if (!near || Time.time < m.lungeUntil) continue; // a catch is playing: it stays on its victim

                var root = m.root;
                bool moving = l.state == "hunting" || l.state == "roaming";
                var predicted = m.server + (moving ? m.velocity * Mathf.Min(Time.time - m.serverTime, MaxPredictSeconds) : Vector3.zero);
                var target = new Vector3(predicted.x, root.position.y, predicted.z);
                var d = target - root.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f) root.rotation = Quaternion.Slerp(root.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-6f * Time.deltaTime));
                var flat = Vector3.Lerp(root.position, target, 1f - Mathf.Exp(-ListenerFollow * Time.deltaTime));
                // The server's Listeners swim straight; here they duck under a platform when right inside one.
                if (PlatformAvoidance.UnderPlatform(flat, surface)) m.diveHold = PlatformAvoidance.DiveHold;
                else m.diveHold -= Time.deltaTime;
                m.depth = Mathf.MoveTowards(m.depth, m.diveHold > 0f ? DiveDepth : 0f, DepthSpeed * Time.deltaTime);
                root.position = new Vector3(flat.x, surface - m.depth, flat.z);
                string next = l.state == "hunting" || l.state == "roaming" ? ListenerLook.Swim : ListenerLook.Tread;
                if (m.animator != null && next != m.clip)
                {
                    m.animator.CrossFadeInFixedTime(next, 0.25f);
                    m.clip = next;
                }
                if (m.animator != null) m.animator.speed = 1f;
            }
        }

        private void LateUpdate()
        {
            // Each model's root is at its feet; keep its chest at its depth so the back shows (or ducks under a platform).
            foreach (var m in monsters)
                if (m != null && m.chest != null && m.animator != null && m.root.gameObject.activeSelf && Time.time >= m.lungeUntil)
                    m.animator.transform.position += Vector3.up * (m.root.position.y - m.chest.position.y);
        }

        // ---------------------------------------------------------------- catches

        // Everyone sees every catch: that room's Listener rears up where it happened and screams. Yours also gets the
        // catch camera before you respawn.
        private void OnCaught(CircuitCaught c)
        {
            if (c.room < 0 || c.room >= monsters.Length || monsters[c.room] == null) return;
            var m = monsters[c.room];
            var spot = new Vector3(c.x, water != null ? water.SurfaceY : 0f, c.z);
            Vector3 victim = c.id == session.SessionId ? motor.transform.position
                : remotes.TryGetValue(c.id, out var r) ? r.go.transform.position : spot;
            var toVictim = victim - m.root.position;
            toVictim.y = 0f;
            m.root.position = spot - (toVictim.sqrMagnitude > 0.01f ? toVictim.normalized : m.root.forward) * 0.8f;
            if (toVictim.sqrMagnitude > 0.01f) m.root.rotation = Quaternion.LookRotation(toVictim);
            m.root.gameObject.SetActive(true);
            m.lungeUntil = Time.time + CatchSeconds;
            if (m.animator != null)
            {
                m.animator.speed = 1f;
                m.animator.CrossFadeInFixedTime(ListenerLook.Lunge, 0.1f);
                m.clip = ListenerLook.Lunge;
            }
            if (screamClip != null) AudioSource.PlayClipAtPoint(screamClip, spot + Vector3.up * 2f, c.id == session.SessionId ? 1f : 0.7f);
            if (c.id == session.SessionId) StartCoroutine(MyCatch(m));
        }

        private IEnumerator MyCatch(RoomListener m)
        {
            myCatchPlaying = true;
            MusicDirector.Instance.Stinger(MusicDirector.Catch, false);
            if (cameras != null) cameras.DisableAll();
            yield return CatchCamera.Play(Camera.main, motor.transform, m.root, m.chest, m.head, water != null ? water.SurfaceY : 0f, CatchSeconds);
            if (pendingRespawn.HasValue) motor.Teleport(pendingRespawn.Value);
            pendingRespawn = null;
            if (cameras != null) cameras.Restore();
            myCatchPlaying = false;
        }

        // The server already put you back at the room's start; wait for the catch camera before showing it.
        private void Respawn(RaceRespawn r)
        {
            var at = new Vector3(r.x, r.y, r.z);
            if (myCatchPlaying) pendingRespawn = at;
            else motor.Teleport(at);
        }

        // ---------------------------------------------------------------- pushing

        private void OnPushPressed() => session.Push();

        private void OnPushed(CircuitPushed p)
        {
            motor.ApplyPush(new Vector3(p.dx, 0f, p.dz) * p.speed);
            AudioSource.PlayClipAtPoint(ProceduralAudio.Chime(180f, 0.25f), motor.transform.position, 0.8f);
        }

        private void OnShoved(CircuitShove s)
        {
            if (s.pusher == session.SessionId) Banner("SHOVE!");
            var at = s.target == session.SessionId ? motor.transform.position
                : remotes.TryGetValue(s.target, out var r) ? r.go.transform.position : motor.transform.position;
            AudioSource.PlayClipAtPoint(ProceduralAudio.Chime(140f, 0.2f), at, 0.6f);
        }

        // ---------------------------------------------------------------- music

        private void UpdateMusic(CircuitSnapshot s, CircuitPlayer me)
        {
            var music = MusicDirector.Instance;
            if (s.phase == "finished")
            {
                if (!resultStinger)
                {
                    resultStinger = true;
                    music.Stinger(me != null && me.place > 0 ? MusicDirector.Win : MusicDirector.Lose, true);
                }
                return;
            }
            var l = me != null && s.listeners != null ? s.listeners.FirstOrDefault(x => x != null && x.room == me.room) : null;
            if (l == null || me.place > 0) { music.SetThreat(0f); return; }
            float close = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(me.x, me.z), new Vector2(l.x, l.z)) / ThreatRange);
            music.SetThreat(l.state == "hunting" ? 0.6f + 0.4f * close : l.state == "alerted" ? 0.45f : l.state == "lingering" ? 0.3f : 0f);
        }

        private void ToggleMenu() => menu = !menu; // never pause shared time

        private void Banner(string text)
        {
            banner = text;
            bannerUntil = Time.unscaledTime + BannerSeconds;
        }

        // ---------------------------------------------------------------- HUD

        private static string Ordinal(int n) => n == 1 ? "1st" : n == 2 ? "2nd" : n == 3 ? "3rd" : $"{n}th";
        private static string Tag(CircuitPlayer p, string me) => $"P{p.slot + 1}" + (p.id == me ? " (you)" : "");

        // Live race order: finished players by place, then whoever is furthest along.
        private static IEnumerable<CircuitPlayer> RaceOrder(CircuitSnapshot s) =>
            (s.players ?? new CircuitPlayer[0]).OrderBy(p => p.place > 0 ? p.place : int.MaxValue).ThenByDescending(p => p.z);

        private void OnGUI()
        {
            if (session == null) return;
            var s = session.Snapshot;
            var me = s?.Find(session.SessionId);
            if (building)
            {
                Gui.Whisper(9f, "Opening the way between the rooms...");
                return;
            }
            if (s == null || s.phase == "waiting")
            {
                int n = s?.players?.Length ?? 1;
                Gui.BigTitle(2.5f, "CO-OP RACE", 2.4f);
                Gui.Text(6f, $"Players {n} / 4");
                Gui.Whisper(7.4f, n >= 2 ? $"Starting in {Mathf.CeilToInt(s.lobby)} s (or as soon as 4 are in)" : "Waiting for at least one more player...");
                Gui.Whisper(9f, "Room ID: " + session.RoomId);
                if (Gui.Button(15f, "Leave")) session.ReturnToLobby();
                return;
            }
            if (s.phase == "countdown")
            {
                Gui.BigTitle(3f, Mathf.CeilToInt(s.countdown).ToString(), 3f);
                Gui.Text(7f, "Five rooms, every door open. Race to the end, or wait and help each other.");
                Gui.Whisper(8.4f, "Each room's Listener hears whoever swims in it. PUSH shoves whoever is in front of you.");
                return;
            }
            if (s.phase == "finished")
            {
                Gui.Dim();
                Gui.BigTitle(1.5f, s.result == "cleared" ? "EVERYONE MADE IT" : s.result == "timeout" ? "TIME'S UP" : "RUN OVER", 2.4f);
                Gui.Whisper(4f, s.reason);
                var board = (s.players ?? new CircuitPlayer[0]).OrderByDescending(p => p.score).ToArray();
                for (int i = 0; i < board.Length; i++)
                {
                    var p = board[i];
                    string finish = p.place > 0 ? $"through {Ordinal(p.place)}" : $"room {p.room + 1}";
                    Gui.Text(6f + i * 1.5f, $"#{i + 1}   {Tag(p, session.SessionId)}   {p.score} pts   ·   {finish}   ·   splashes {p.splashes}   ·   caught {p.catches}");
                }
                if (Gui.Button(15.5f, "Back to lobby")) session.ReturnToLobby();
                return;
            }

            int seconds = Mathf.CeilToInt(s.remaining);
            var order = RaceOrder(s).ToList();
            int myRank = me != null ? order.FindIndex(p => p.id == me.id) + 1 : 0;
            Gui.Text(0.3f, $"ROOM {(me?.room ?? 0) + 1}/{s.roomCount}    {seconds / 60}:{seconds % 60:00}    {Ordinal(Mathf.Max(1, myRank))}    SCORE {me?.score ?? 0}");
            Gui.Whisper(1.4f, string.Join("     ", order.Select((p, i) => $"{i + 1}. {Tag(p, session.SessionId)} " + (p.place > 0 ? "✓" : $"room {p.room + 1}"))));
            if (me != null && me.place > 0) Gui.Text(16.5f, "You're through. Watch the others finish...");
            if (me != null && me.penalty > 0f && !myCatchPlaying) Gui.Text(6f, $"Caught! Back in {Mathf.CeilToInt(me.penalty)}...");
            if (Time.unscaledTime < bannerUntil) Gui.BigTitle(4f, banner, 2f);
            DrawNameTags(s);
            if (menu)
            {
                Gui.Dim();
                Gui.Text(4f, "The race goes on while this menu is open");
                if (Gui.Button(8f, "Resume")) menu = false;
                if (Gui.Button(10f, "Leave")) session.ReturnToLobby();
            }
        }

        // "P2" over each other player's head, so you can find each other in the dark.
        private void DrawNameTags(CircuitSnapshot s)
        {
            var cam = Camera.main;
            if (cam == null || s.players == null) return;
            foreach (var p in s.players)
            {
                if (p.id == session.SessionId || !remotes.TryGetValue(p.id, out var r)) continue;
                Vector3 sp = cam.WorldToScreenPoint(r.go.transform.position + Vector3.up * 2.1f);
                if (sp.z <= 0f) continue;
                var rect = new Rect(sp.x - Gui.Unit * 2f, Screen.height - sp.y - Gui.Unit * 0.5f, Gui.Unit * 4f, Gui.Unit);
                GUI.Label(rect, $"P{p.slot + 1}" + (p.place > 0 ? " ✓" : ""), Gui.Label);
            }
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.Respawned -= Respawn;
                session.Caught -= OnCaught;
                session.Pushed -= OnPushed;
                session.Shoved -= OnShoved;
            }
            if (touch != null)
            {
                touch.PausePressed -= ToggleMenu;
                touch.PushPressed -= OnPushPressed;
                touch.ShowPush = false;
            }
            foreach (var r in remotes.Values) if (r.go != null) Destroy(r.go);
            for (int i = 1; i < monsters.Length; i++) if (monsters[i] != null) Destroy(monsters[i].root.gameObject);
        }
    }
}

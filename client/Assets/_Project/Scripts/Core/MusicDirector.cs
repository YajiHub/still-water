using UnityEngine;

namespace StillWater.Core
{
    /// <summary>
    /// The game's music (original, synthesized by art/music/compose.py into Resources/Music). One base track crossfades
    /// to the next (menu, the Reveal's music box, the pool's ambience, the online race). A hunt layer rises over it with
    /// the threat (0 = the Listener is roaming, 1 = it's on you), and the base dips under it. One-shot stingers play for
    /// the catch, a win and a loss. It survives scene loads, so Retry or Next doesn't restart a track that's already
    /// playing. The Sound toggle (AudioListener.volume) and pause (AudioListener.pause) cover it like every other sound.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        public const string Menu = "menu_theme", Ambience = "level_ambience", Reveal = "reveal_music_box",
            Race = "race_theme", Hunt = "hunt_layer", Catch = "stinger_catch", Win = "stinger_win", Lose = "stinger_lose";

        // Mix levels (0..1) and how fast things move: looks/sound, not gameplay.
        private const float BaseVolume = 0.5f, HuntVolume = 0.75f, StingerVolume = 0.9f;
        private const float FadeSeconds = 1.5f, ThreatPerSecond = 1.5f, BaseDipAtFullThreat = 0.55f;

        private static MusicDirector instance;
        private static float Music => Mathf.Clamp01(GameSettings.Current.musicVolume); // the Settings page's Music slider

        public static MusicDirector Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("Music");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<MusicDirector>();
                }
                return instance;
            }
        }

        private AudioSource a, b, hunt, stinger, current;
        private string currentName;
        private float threat, threatTarget;

        private void Awake()
        {
            a = Source(true);
            b = Source(true);
            hunt = Source(true);
            hunt.clip = Load(Hunt);
            stinger = Source(false);
            stinger.volume = 1f; // PlayOneShot scales the source volume: at 0 (Source's default) every stinger was silent
        }

        private AudioSource Source(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        private static AudioClip Load(string name) => Resources.Load<AudioClip>("Music/" + name);

        /// <summary>Crossfade to a base track (no change if it is already playing).</summary>
        public void Play(string track)
        {
            if (track == currentName) return;
            currentName = track;
            var next = current == a ? b : a;
            next.clip = Load(track);
            next.volume = 0f;
            if (next.clip != null) next.Play();
            current = next;
        }

        /// <summary>Fade the base track and the hunt layer out.</summary>
        public void Stop()
        {
            currentName = null;
            current = null;
            threatTarget = 0f;
        }

        /// <summary>How close the danger is, 0..1: drives the hunt layer.</summary>
        public void SetThreat(float value) => threatTarget = Mathf.Clamp01(value);

        public void Stinger(string name, bool endMusic)
        {
            var clip = Load(name);
            if (clip != null) stinger.PlayOneShot(clip, StingerVolume * Music);
            if (endMusic)
            {
                Stop();
                threat = 0f; // cut the hunt layer at once: the stinger takes over
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            threat = Mathf.MoveTowards(threat, threatTarget, ThreatPerSecond * dt);
            float baseTarget = BaseVolume * Music * (1f - BaseDipAtFullThreat * threat);
            foreach (var s in new[] { a, b })
            {
                float target = s == current ? baseTarget : 0f;
                s.volume = Mathf.MoveTowards(s.volume, target, BaseVolume * dt / FadeSeconds);
                if (s != current && s.volume <= 0f && s.isPlaying) s.Stop();
            }
            hunt.volume = threat * HuntVolume * Music;
            if (threat > 0.001f && !hunt.isPlaying && hunt.clip != null) hunt.Play();
            else if (threat <= 0.001f && hunt.isPlaying) hunt.Stop();
        }
    }
}

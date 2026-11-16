"""Still Water: original music, synthesized offline. Run it with Blender's Python (numpy + Blender's audio module
write OGG Vorbis):

    blender -b --factory-startup --python art/music/compose.py -- client/Assets/_Project/Resources/Music

Everything is generated from code with a fixed seed: oscillators, FFT filters, and a long "poolroom" convolution
reverb. Loops are seamless: each one is rendered with a tail, and the tail is folded back onto the start.

Tracks (all D minor / Phrygian, so the layers and stingers sit together):
  menu_theme        64 s loop   drone, slow detuned pad chords, a sparse glass melody, water drips
  level_ambience    80 s loop   liminal pool hall: fluorescent hum, room tone, beating sub drone, drips, distant metal groans
  reveal_music_box  32 s loop   a slightly warped music box for "remember the glowing path"
  hunt_layer        16 s loop   heartbeat bass pulse + tremolo dissonant strings; MusicDirector fades it in with the threat
  race_theme        48 s loop   online 1v1: a driving 120 bpm pulse
  stinger_catch     ~4.5 s      rising cluster + shriek, slam at 1.3 s (the lunge's slam frame), ringing tail
  stinger_win       ~6 s        relief: an open D major add9 bloom with bells
  stinger_lose      ~7 s        a low tolling bell and a sinking minor drone
"""
import math
import os
import sys

import numpy as np

SR = 44100
OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "."
rng = np.random.default_rng(23)

NOTE = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6, "G": 7, "G#": 8,
        "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def hz(name):
    """'D3' -> frequency."""
    return 440.0 * 2 ** ((12 * (int(name[-1]) + 1) + NOTE[name[:-1]] - 69) / 12)


def S(seconds):
    return int(round(seconds * SR))


def T(n):
    return np.arange(n) / SR


# ---------------------------------------------------------------- oscillators and shapes
def sine(f, n, phase=0.0):
    return np.sin(2 * np.pi * f * T(n) + phase)


def sweep_sine(freq):
    """Sine following a per-sample frequency array."""
    return np.sin(2 * np.pi * np.cumsum(freq) / SR)


def saw(f, n):
    return 2 * ((f * T(n) + rng.random()) % 1.0) - 1


def noise(n):
    return rng.standard_normal(n)


def spectral(x, gain):
    f = np.fft.rfftfreq(len(x), 1 / SR)
    return np.fft.irfft(np.fft.rfft(x) * gain(f), len(x))


def lowpass(x, fc, order=2):
    return spectral(x, lambda f: 1 / np.sqrt(1 + (f / fc) ** (2 * order)))


def highpass(x, fc, order=2):
    return spectral(x, lambda f: 1 / np.sqrt(1 + (fc / np.maximum(f, 1e-3)) ** (2 * order)))


def bandpass(x, fc, width_octaves):
    return spectral(x, lambda f: np.exp(-0.5 * (np.log2(np.maximum(f, 1.0) / fc) / width_octaves) ** 2))


def fade(n, attack, release):
    e = np.ones(n)
    a, r = min(n, S(attack)), min(n, S(release))
    if a:
        e[:a] = np.sin(np.linspace(0, np.pi / 2, a)) ** 2
    if r:
        e[n - r:] *= np.cos(np.linspace(0, np.pi / 2, r)) ** 2
    return e


def decay(n, tau):
    return np.exp(-T(n) / tau)


# ---------------------------------------------------------------- instruments
def pad(freq, seconds, cutoff=900.0, cents=8.0, attack=3.0, release=4.0):
    n = S(seconds)
    x = sum(saw(freq * 2 ** (c / 1200), n) for c in (-cents, 0.0, cents)) / 3
    return lowpass(x, cutoff) * fade(n, attack, release)


def bell(freq, seconds=4.0, tau=2.2, partials=((1, 1.0), (2.76, 0.45), (5.4, 0.22), (8.93, 0.1))):
    n = S(seconds)
    return sum(a * sine(freq * r, n, rng.random() * 6.28) * decay(n, tau / r ** 0.5) for r, a in partials) * fade(n, 0.003, 0.3)


def music_box(freq, seconds=2.2):
    n = S(seconds)
    tone = sum(a * sine(freq * r, n) * decay(n, 1.3 / r ** 0.4) for r, a in ((1, 1.0), (2.0, 0.18), (4.18, 0.14), (6.07, 0.05)))
    click = highpass(noise(n) * decay(n, 0.002), 3000) * 0.15
    return (tone + click) * fade(n, 0.002, 0.2)


def drip(seconds=0.25):
    n = S(seconds)
    f0 = rng.uniform(1300, 2600)
    f = f0 * (1 + 0.9 * (1 - np.exp(-T(n) / 0.018)))  # a water drop's "bloop" bends up
    return sweep_sine(f) * decay(n, 0.045) * fade(n, 0.001, 0.05)


def kick(seconds=0.5):
    n = S(seconds)
    return sweep_sine(42 + 100 * np.exp(-T(n) / 0.035)) * decay(n, 0.28) * fade(n, 0.002, 0.05)


def heartbeat_hit(seconds=0.45, f=55.0):
    n = S(seconds)
    return np.tanh(2.2 * sweep_sine(f * 0.7 + f * 1.3 * np.exp(-T(n) / 0.03)) * decay(n, 0.13))


def hat(seconds=0.08):
    n = S(seconds)
    return highpass(noise(n), 7000) * decay(n, 0.018)


def groan(seconds=7.0, f0=72.0, f1=54.0, formant=(320.0, 900.0)):
    """Distant metal under stress: a sliding harmonic series through a moving resonance."""
    n = S(seconds)
    t = T(n) / seconds
    f = f0 * (f1 / f0) ** t * (1 + 0.012 * np.sin(2 * np.pi * 5.3 * T(n)))
    phase = 2 * np.pi * np.cumsum(f) / SR
    res = formant[0] + (formant[1] - formant[0]) * np.sin(np.pi * t)
    x = np.zeros(n)
    for k in range(1, 28):
        x += np.exp(-(((k * f) - res) / 160.0) ** 2) * np.sin(k * phase) / k ** 0.3
    return x * fade(n, 1.6, 2.5)


def shriek(seconds=1.6, f0=2300.0, f1=850.0):
    n = S(seconds)
    t = T(n) / seconds
    f = f0 * (f1 / f0) ** (t ** 0.7) * (1 + 0.035 * np.sin(2 * np.pi * 7 * T(n)))
    phase = 2 * np.pi * np.cumsum(f) / SR
    x = sum(np.sin(k * phase) * np.exp(-((k * f - 2600) / 900) ** 2) for k in range(1, 7))
    rasp = bandpass(noise(n), 2400, 0.6) * 0.35
    return (x + rasp) * fade(n, 0.08, 0.6)


def whisper(seconds=3.0):
    n = S(seconds)
    syll = 0.5 + 0.5 * np.sin(2 * np.pi * rng.uniform(2.5, 4.5) * T(n)) ** 2
    return bandpass(noise(n), rng.uniform(2200, 3600), 0.35) * syll * fade(n, 0.8, 1.2)


# ---------------------------------------------------------------- mixing, reverb, loops
def impulse(seconds, rt60, bright):
    n = S(seconds)
    t = T(n)
    chans = []
    for _ in range(2):
        w = np.exp(-t / 0.35)
        ir = (w * lowpass(noise(n), bright) + (1 - w) * lowpass(noise(n), bright / 4)) * np.exp(-6.9 * t / rt60)
        for _ in range(10):  # early reflections off the tiles
            i = S(rng.uniform(0.008, 0.09))
            ir[i] += rng.uniform(-1, 1) * 3.0
        ir[: S(0.004)] = 0
        chans.append(ir / np.sqrt(np.sum(ir ** 2)))
    return np.stack(chans, 1)


def convolve(x, ir):
    size = len(x) + len(ir) - 1
    N = 1 << (size - 1).bit_length()
    return np.fft.irfft(np.fft.rfft(x, N) * np.fft.rfft(ir, N), N)[: len(x)]


class Track:
    def __init__(self, seconds, tail=8.0, loop=True):
        self.L = S(seconds)
        self.n = self.L + S(tail)
        self.loop = loop
        self.dry = np.zeros((self.n, 2))
        self.send = np.zeros((self.n, 2))

    def add(self, sig, at, gain=1.0, pan=0.0, send=0.4):
        i = S(at)
        if i >= self.n:
            return
        m = min(len(sig), self.n - i)
        a = (pan + 1) * np.pi / 4
        for ch, g in ((0, np.cos(a)), (1, np.sin(a))):
            self.dry[i:i + m, ch] += sig[:m] * gain * g * np.sqrt(2) * (1 - send)
            self.send[i:i + m, ch] += sig[:m] * gain * g * np.sqrt(2) * send

    def render(self, rt60=5.0, bright=5000.0, wet=1.0, peak=0.89):
        ir = impulse(min(rt60 * 1.2, 9.0), rt60, bright)
        out = self.dry.copy()
        for ch in range(2):
            out[:, ch] += wet * convolve(self.send[:, ch], ir[:, ch])
        if self.loop:
            out[: self.n - self.L] += out[self.L:]  # fold the tail onto the start: seamless loop
            out = out[: self.L]
        else:
            out *= fade(len(out), 0.0, 1.5)[:, None]
        out = np.tanh(out / max(1e-9, np.max(np.abs(out))) * 1.2) / np.tanh(1.2) * peak
        return out.astype(np.float32)


def write(name, data):
    import aud
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".ogg")
    snd = aud.Sound.buffer(np.ascontiguousarray(data), SR)
    snd.write(path, rate=SR, channels=aud.CHANNELS_STEREO, format=aud.FORMAT_S16,
              container=aud.CONTAINER_OGG, codec=aud.CODEC_VORBIS, bitrate=112000)
    rms = float(np.sqrt(np.mean(data ** 2)))
    print(f"WROTE {name}.ogg  {len(data) / SR:.1f}s  peak {np.max(np.abs(data)):.2f}  rms {rms:.3f}  {os.path.getsize(path) // 1024} KB")


# ---------------------------------------------------------------- pieces
def menu_theme():
    tr = Track(64, tail=9)
    n = S(64 + 9)
    drone = (sine(hz("D1"), n) + 0.5 * sine(hz("D2"), n) + 0.18 * sine(hz("A1"), n)) * (0.75 + 0.25 * np.sin(2 * np.pi * T(n) / 32))
    tr.add(np.tanh(1.5 * drone) * fade(n, 0.0, 0.0), 0, gain=0.32, send=0.25)
    chords = [("D3", "F3", "A3", "D4"), ("Bb2", "D3", "F3", "A3"), ("G2", "Bb2", "D3", "A3"), ("A2", "C#3", "G3", "Bb3")]
    for i, chord in enumerate(chords):
        for k, note in enumerate(chord):
            tr.add(pad(hz(note), 19, cutoff=800 + 150 * k), i * 16 - 1.5 if i else 0, gain=0.07, pan=(k - 1.5) * 0.35, send=0.55)
    melody = [(2, "D5"), (5, "F5"), (7, "E5"), (10, "A4"), (16.5, "C5"), (18.5, "Bb4"), (22, "A4"),
              (32, "D5"), (34, "F5"), (36, "A5"), (38.5, "G#5"), (44, "F5"), (46, "E5"), (49, "D5"), (56, "C#5"), (58.5, "D5")]
    for at, note in melody:
        tr.add(bell(hz(note) * 2 ** (rng.uniform(-9, 9) / 1200), 5, tau=2.4), at, gain=0.16, pan=rng.uniform(-0.5, 0.5), send=0.7)
    at = 1.0
    while at < 64:
        tr.add(drip(), at, gain=0.05, pan=rng.uniform(-0.9, 0.9), send=0.85)
        at += rng.uniform(2.5, 7.0)
    return tr.render(rt60=6.0, bright=4500)


def level_ambience():
    L = 80
    tr = Track(L, tail=10)
    n = S(L + 10)
    hum = sum(a * sine(60 * k, n) for k, a in ((1, 1.0), (2, 0.55), (3, 0.35), (4, 0.18), (5, 0.08)))
    tr.add(hum * (1 + 0.08 * np.sin(2 * np.pi * 0.27 * T(n))), 0, gain=0.035, send=0.3)  # fluorescent tubes
    tr.add(lowpass(noise(n), 380, 1), 0, gain=0.05, send=0.4)                                 # the room breathing
    beat = sine(hz("D1"), n) + sine(hz("D1") + 0.31, n)                                       # two subs, slowly beating
    tr.add(np.tanh(1.3 * beat) * (0.7 + 0.3 * np.sin(2 * np.pi * T(n) / 40)), 0, gain=0.22, send=0.2)
    swell = lowpass(saw(hz("D2"), n) + saw(hz("Eb2"), n), 160) * (0.5 - 0.5 * np.cos(2 * np.pi * T(n) / 20))
    tr.add(swell, 0, gain=0.10, send=0.5)
    at = 0.5
    while at < L:
        tr.add(drip(), at, gain=0.045, pan=rng.uniform(-1, 1), send=0.9)
        at += rng.uniform(1.8, 6.0)
    for at, f0, f1 in ((14, 74, 55), (47, 64, 50), (69, 82, 61)):
        tr.add(groan(7.5, f0, f1), at, gain=0.16, pan=rng.uniform(-0.7, 0.7), send=0.85)
    for at in (30, 61):
        tr.add(whisper(3.5), at, gain=0.03, pan=rng.choice([-0.8, 0.8]), send=0.8)
    return tr.render(rt60=7.0, bright=3500)


def reveal_music_box():
    L = 32
    tr = Track(L, tail=6)
    bars = [("D5", "A5", "F5", "A5"), ("C#5", "A5", "E5", "A5"), ("D5", "A5", "F5", "A5"), ("Bb4", "F5", "D5", "F5"),
            ("D5", "A5", "F5", "A5"), ("C#5", "G5", "E5", "Bb5"), ("D5", "A5", "F5", "D6"), ("Bb4", "F5", "Db5", "F5")]
    warble = lambda at: 2 ** (14 * math.sin(2 * math.pi * 0.23 * at) / 1200)  # tired spring: the pitch sags and drifts
    for b, bar in enumerate(bars):
        for i in range(8):
            at = b * 4 + i * 0.5
            tr.add(music_box(hz(bar[i % 4]) * warble(at), 2.4), at, gain=0.22, pan=0.3 * math.sin(at), send=0.45)
    for b, root in enumerate(("D3", "A2", "D3", "Bb2", "D3", "A2", "D3", "Bb2")):
        tr.add(pad(hz(root), 5.5, cutoff=500, attack=1.5, release=2.0), b * 4 - 0.5 if b else 0, gain=0.06, send=0.6)
    return tr.render(rt60=4.0, bright=6000)


def hunt_layer():
    L = 16
    tr = Track(L, tail=5)
    beat = 0.5  # 120 bpm
    for k in range(int(L / beat)):
        at = k * beat
        if k % 2 == 0:  # lub-dub on every half bar
            tr.add(heartbeat_hit(f=hz("D2")), at, gain=0.55, send=0.15)
            tr.add(heartbeat_hit(f=hz("D2") * 0.9), at + 0.17, gain=0.35, send=0.15)
        tr.add(hat(), at + 0.25, gain=0.05, pan=0.4, send=0.2)
    n = S(L + 5)
    trem = 0.55 + 0.45 * np.abs(np.sin(2 * np.pi * 3.6 * T(n)))
    cluster = sum(lowpass(saw(hz(note) * 2 ** (c / 1200), n), 2400) for note in ("D4", "Eb4", "Ab4") for c in (-6, 6))
    tr.add(cluster * trem * fade(n, 0.0, 0.0), 0, gain=0.035, send=0.45)
    shimmer = sweep_sine(2500 * (1 + 0.01 * np.sin(2 * np.pi * 5.5 * T(n))))
    tr.add(shimmer, 0, gain=0.012, send=0.6)
    return tr.render(rt60=3.0, bright=6000)


def race_theme():
    L = 48
    tr = Track(L, tail=5)
    beat = 0.5
    roots = ("D", "Bb", "C", "A")
    for k in range(int(L / beat)):
        at = k * beat
        tr.add(kick(), at, gain=0.5, send=0.1)
        tr.add(hat(), at + 0.25, gain=0.07, pan=-0.3, send=0.15)
        root = roots[int(at // 8) % 4]
        for h in (0, 0.25):  # driving 8th-note bass
            n = S(0.24)
            bass = lowpass(saw(hz(root + "1") * (2 if h else 1), n), 700) * decay(n, 0.12) * fade(n, 0.004, 0.03)
            tr.add(bass, at + h, gain=0.28, send=0.05)
    arp = {"D": ("D4", "F4", "A4", "D5"), "Bb": ("Bb3", "D4", "F4", "A4"), "C": ("C4", "E4", "G4", "Bb4"), "A": ("A3", "C#4", "E4", "G4")}
    for k in range(int(L / 0.25)):
        at = k * 0.25
        notes = arp[roots[int(at // 8) % 4]]
        n = S(0.3)
        pluck = lowpass(saw(hz(notes[k % 4]), n), 1800) * decay(n, 0.07) * fade(n, 0.003, 0.03)
        tr.add(pluck, at, gain=0.07, pan=0.5 * math.sin(k * 0.7), send=0.35)
    for i, root in enumerate(roots * 2):
        if i >= L / 8:
            break
        chord = {"D": ("D3", "A3", "F4"), "Bb": ("Bb2", "F3", "D4"), "C": ("C3", "G3", "E4"), "A": ("A2", "E3", "C#4")}[root]
        for k, note in enumerate(chord):
            tr.add(pad(hz(note), 9.5, cutoff=1100, attack=1.0, release=1.5), i * 8 - 0.5 if i else 0, gain=0.05, pan=(k - 1) * 0.5, send=0.5)
    return tr.render(rt60=2.5, bright=6500)


def stinger_catch():
    tr = Track(4.5, tail=4, loop=False)
    n = S(1.35)
    rise = np.linspace(0, 1, n) ** 2
    cluster = sum(saw(hz(note), n) for note in ("D3", "Eb3", "E3", "A3", "Bb3", "Eb4"))
    tr.add(spectral(cluster, lambda f: 1 / np.sqrt(1 + (f / 2500) ** 4)) * rise, 0, gain=0.09, send=0.5)
    tr.add(shriek(1.7), 0.35, gain=0.6, send=0.5)
    tr.add(shriek(1.2, 3200.0, 1400.0), 0.9, gain=0.35, pan=0.4, send=0.6)
    m = S(3.0)
    boom = sweep_sine(38 + 70 * np.exp(-T(m) / 0.06)) * decay(m, 0.9)
    tr.add(np.tanh(2.5 * boom), 1.3, gain=0.8, send=0.35)
    tr.add(lowpass(noise(S(0.6)), 900) * decay(S(0.6), 0.12), 1.3, gain=0.5, send=0.4)
    for note in ("D2", "Eb3", "A3"):
        tr.add(bell(hz(note), 3.2, tau=1.6), 1.3, gain=0.18, send=0.6)
    return tr.render(rt60=4.0, bright=4000)


def stinger_win():
    tr = Track(6, tail=4, loop=False)
    for k, note in enumerate(("D3", "F#3", "A3", "E4", "A4")):
        tr.add(pad(hz(note), 5.5, cutoff=1400, attack=0.8, release=3.0), 0.05 * k, gain=0.09, pan=(k - 2) * 0.3, send=0.55)
    for at, note in ((0.2, "A5"), (0.7, "D6"), (1.3, "F#5")):
        tr.add(bell(hz(note), 4, tau=2.0), at, gain=0.12, send=0.65)
    return tr.render(rt60=4.5, bright=6000)


def stinger_lose():
    tr = Track(7, tail=5, loop=False)
    for at in (0.0, 2.6):
        tr.add(bell(hz("D2"), 5.5, tau=3.0, partials=((1, 1.0), (2.0, 0.6), (2.4, 0.4), (3.0, 0.3), (4.2, 0.2), (5.4, 0.1))), at, gain=0.45, send=0.5)
    n = S(6.5)
    slide = 2 ** (-np.linspace(0, 3, n) / 12)  # sinks three semitones
    for note in ("D3", "F3", "A3"):
        f = hz(note) * slide
        x = lowpass(2 * ((np.cumsum(f) / SR) % 1.0) - 1, 600)
        tr.add(x * fade(n, 1.0, 2.5), 0.3, gain=0.07, send=0.6)
    return tr.render(rt60=6.0, bright=3000)


def sfx_listener_scream():
    """The Listener's own scream on the catch (played in 3D from its head): a throat of formant-filtered, jittering
    voices sliding from a howl into a shriek, ring-modulated and torn up, over a wet gurgle."""
    tr = Track(2.6, tail=1.5, loop=False)
    n = S(2.4)
    t = T(n) / 2.4
    jitter = np.cumsum(rng.standard_normal(n)) / SR * 40          # unstable, rasping pitch
    jitter -= np.linspace(jitter[0], jitter[-1], n)
    vowel = np.clip(t * 1.6, 0, 1)                                  # "aah" opening into "eee"
    formants = [(700 + 300 * vowel, 1.0), (1200 + 1100 * vowel, 0.7), (2600 + 400 * vowel, 0.45)]
    voice = np.zeros(n)
    for detune in (-0.03, 0.0, 0.04):
        f0 = (190 * (1 + 0.9 * np.sin(np.pi * np.clip(t * 1.3, 0, 1))) + jitter) * (1 + detune)
        phase = 2 * np.pi * np.cumsum(f0) / SR
        for k in range(1, 30):
            w = sum(a * np.exp(-((k * f0 - fc) / 160.0) ** 2) for fc, a in formants)
            voice += w * np.sin(k * phase) / k ** 0.2
    voice *= 1 + 0.9 * np.sin(2 * np.pi * 55 * T(n))               # ring modulation: not an animal throat
    voice = np.tanh(3.0 * voice / np.max(np.abs(voice)))
    gurgle = lowpass(noise(n), 500) * (0.5 + 0.5 * np.sin(2 * np.pi * 23 * T(n)))
    tr.add(voice * fade(n, 0.04, 0.7), 0, gain=0.8, send=0.25)
    tr.add(gurgle * fade(n, 0.02, 0.5), 0, gain=0.35, send=0.2)
    tr.add(shriek(1.6, 2600.0, 1200.0), 0.25, gain=0.35, send=0.3)
    return tr.render(rt60=1.8, bright=6000)


if __name__ == "__main__":
    for fn in (menu_theme, level_ambience, reveal_music_box, hunt_layer, race_theme, stinger_catch, stinger_win, stinger_lose,
               sfx_listener_scream):
        write(fn.__name__, fn())

# Title and Monster Identity

- **Status:** Monster **locked** (2026-11-06, by: me); title still open
- **Type:** grill
- **Blocks:** Phase 5 (title screen, icon, store-ready name); Phase 2 only needs a placeholder monster

## Question
What is the final game title, and what does the monster look like?

## Why it matters
Both must be original (lab rule and future store release). The inspiration is a Roblox game, so avoid its name ("Don't Splash") and its creature design.

## Title options (brainstorm; add your own)
| Title | Feel |
|---|---|
| **Still Water** (working) | Calm-sounding, ominous ("still waters run deep") |
| Don't Make a Sound | Explains the mechanic |
| Deep End | Pool pun + dread |
| The Quiet Pool | Liminal |
| Undertow | Short, punchy |

Before locking: search the Play Store and itch.io for clashes.

## Monster options
| Concept | For | Against |
|---|---|---|
| **The Listener**: pale, eyeless, long-limbed swimmer, mostly seen as a ridge and ripples | Cheap to make (little of it is visible); fits the sound mechanic | Needs at least a surfacing + catch animation |
| Drowned figure / shadow under the surface | Very cheap (a dark shape + wake) | Less memorable |
| Something mechanical (a pool drain "creature") | Unique | Weaker horror read |

## Recommendation
Keep **Still Water** + **the Listener** unless something better comes up. Show it mostly as a silhouette under the water (cheap and scary), with one full reveal on the catch.

## Decision
**Monster (locked 2026-11-06, by: me): the Listener is a pale, long-limbed, eyeless humanoid** that swims face-down.
- **Glimpse / roaming:** only the spine ridge and shoulder blades break the surface (fits the existing Fin / Wake / Hidden roam modes).
- **Catch:** it rises out of the water, faceless, too-long arms out: the one full look.
- Original design; nothing crab-like or from the Roblox inspiration.
- Being humanoid means a standard humanoid rig, so free swim / crawl animations can be retargeted (source decided under Phase 3b asset sources).

**Amendment (2026-11-16, by: me, "be creative… it doesn't matter even if it is not a humanoid"):** the Listener is now an
original Blender model (`art/listener/build_listener.py` → `Listener_Custom.fbx`). The locked parts stay: pale, eyeless,
too-long arms, face-down swimmer, only the back (spine + shoulder blades) at the surface, one full look on the catch.
The new parts:
- **Below the waist it is not human:** a long, spined eel tail (~2.4 m) with two withered legs fused along it.
- **Second pair of small mantis arms** folded under the ribs. They unfold on the catch.
- **The head is a blank egg** with eye sockets that skin has grown over, and a seam down the middle of the face. On the
  catch it **splits sideways into two halves lined with needle teeth**, with a ring of teeth down the throat.
- **Catch:** it rears out of the water, arms flung wide, head splitting open, then falls on you and closes its arms.
- **v2 (2026-11-16, after the first in-game test):** 1.5× bigger (~6.3 m nose to tail, arms ~2.3 m). A baked skin
  texture with pale mottling, a darker slate back, bruised extremities and wandering purple veins. Wet black hair
  trailing from the skull. **Glowing sound pits** (it has no eyes, it listens): a lateral line of cyan-green lights
  down each flank, and clusters in the skinned-over sockets. They breathe slowly while it roams, flicker when it hears
  you, throb while it hunts and blaze on the catch, so in the dark you see a string of lights coming. A dim ember
  glows deep in the throat. **Catch camera:** the view drops to just above the water behind you and looks up at it
  for 2.4 s before the Lost screen.
- **v3 (2026-11-16, after test 3, "not that scary, lacks texture, wideness"):** 1.8× (~7.6 m long, ~4.5 m across
  the arms), a wider, hunched upper body and skull, ragged skin flaps on the arms. Baked colour, normal and
  vein-emission maps: rot at the waterline, blood at the mouth and hands, ivory teeth. The look glow lights the veins.
  It screams on the catch.
- **Generic rig, not Humanoid.** Its own procedural clips under the same names (`Swim_Fwd_Loop`, `Swim_Idle_Loop`,
  `Zombie_Scratch`), so `ListenerLook` and the server don't change.

**Title:** still open (working title Still Water); lock in Phase 5 after a Play Store / itch.io clash search.

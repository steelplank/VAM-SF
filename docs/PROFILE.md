# Writing VAM-profile.txt

`VAM-profile.txt` is where you program **AR, Hidden, Fade-In, and Scroll Velocity** over time. It's a plain text file, read by the **VAM_Generator** effect, and it lives in your storybrew project root.

The rules are simple:

- **One keyframe per line**, written as `time:value` (with optional flags).
- **`time` is milliseconds** on the osu! editor timeline - the same number the editor shows.
- **`#` starts a comment.** Anything after it on the line is ignored.
- Write your keyframes under the **`WRITE BELOW`** line at the bottom of the file.

!!! tip "In the file itself"
    `VAM-profile.txt` keeps a short cheat sheet at the top and links back here. This page is the full reference.

---

## At a glance

```
0:9                AR 9 from the start
45000:10           ease to AR 10 by 45s
60000:10:after     snap to 10 (no ease)
60000:9:sinein     ease in with a named curve
60000:9:hd=5       add fake Hidden   (0 off | 5 normal | 10 hardest)
95000:-:hd=0       Hidden only, leave AR unchanged
60000:9:fi=5       add fake Fade-In  (invisible up top, fades in lower)
60000:9:hd=5:fi=5  Hidden + Fade-In on one line -> a middle reading window

[sv]               start the Scroll Velocity section
60000:6            x6 fall speed   (1 normal | <1 slow | 0 freeze)
61000:1            always end back at x1

loop 60000 1/2 -> 64000     repeat a pattern every 1/2 beat
hold 60000 1/16 0.1 10 -> 64000   mania hold-then-snap
```

---

## Approach Rate

The headline feature. One `time:ar` per line - values **ease** between keyframes, and **hold** before the first keyframe and after the last.

```
0:8            # AR 8 from the start
45000:10       # ease up to AR 10 by 45s
90000:9.5      # ease back down to 9.5 by 90s
```

AR can go past osu!'s normal range (try `12`), and decimals are fine (`9.5`).

### Flags

Add flags after the AR value, separated by colons, in any order: `time:ar:flag:flag`

| Flag | What it does |
|------|--------------|
| **easing name** | `linear` / `none`, `sine`, `sinein`, `sineout`, `quad` / `quadin` / `quadout`, `cubic` / `cubicin` / `cubicout`. Overrides the effect's default easing (`SineInOut`) for that transition. |
| **`after`** | Snap *into* this keyframe - hold the previous value, then jump at this time. A sudden change. |
| **`before`** | Snap *out of* this keyframe. |
| **`hd=N`** | Fake Hidden (see below). |
| **`fi=N`** | Fake Fade-In (see below). |

```
60000:10.5:after     # AR jumps straight to 10.5 at 60s, no ramp
60000:9:sinein       # ease in with a specific curve
```

!!! note "Leaving AR unchanged"
    Put `-` in place of the AR value on a line that only sets Hidden or Fade-In - `95000:-:hd=0` changes Hidden and leaves AR on its current path.

---

## Hidden

`hd=0..10` reproduces osu!catch Hidden - objects fade out on the way down. It keeps osu!'s exact fade *width* and just slides *where* the fade happens, about 8% of the fall per step.

| Value | Effect |
|-------|--------|
| `hd=0` (or `hd=false`) | Off, fully visible. |
| `hd=1-4` | Fade lower -> vanishes later (easier). |
| `hd=5` | osu!'s normal Hidden (same as a bare `hd`). |
| `hd=6-9` | Fade higher -> vanishes sooner (harder). |
| `hd=10` | Fully hidden the instant it appears. |

Decimals are fine (`hd=6.5`). Hidden ramps with AR's easing and scope, so you can fade it in and out:

```
60000:9:hd=0
65000:9:hd=5     # Hidden fades in over 60-65s
90000:9:hd=5
95000:9:hd=0     # ...and back out over 90-95s
```

!!! warning "Needs its master switch"
    Requires **`EnableHidden`** on the VAM_Generator effect (on by default). If you turn Hidden on but never set `hd` in the profile, the whole map can end up Hidden - a good habit is to start the file with `0:9:hd=0`.

---

## Fade-In

`fi=0..10` is osu!mania's Fade-In: the object is **invisible at the top** of the fall and fades **in** lower down - the mirror of Hidden. The number sets how far down it stays hidden.

| Value | Effect |
|-------|--------|
| `fi=0` (or `fi=false`) | Off, appears normally at the top. |
| `fi=1-4` | Revealed higher (easier). |
| `fi=5` | Normal Fade-In (same as a bare `fi`); revealed by ~1/4 down. |
| `fi=6-10` | Revealed lower (harder). |

### Combining with Hidden

Fade-In hides the **top**, Hidden hides the **bottom**, so together they leave a **visible band in the middle** - a fake-mania reading window.

At the defaults (`fi=5` + `hd=5`) you get a clean window visible from about 1/4 to 2/5 of the fall. Pushing `fi` higher narrows it: by `fi=10` the reveal reaches into `hd=5`'s fade and the window closes almost completely, for a very tight read.

```
60000:9:hd=5:fi=5      # a comfortable middle window
90000:9:hd=5:fi=8      # window tightens toward the drop
```

The object is always fully shown before it lands - Fade-In never hides the catch itself. Decimals are fine, and it ramps with AR's easing/scope. Requires **`EnableFadeIn`** (on by default).

---

## Scroll Velocity

A mania-style speed multiplier on how fast **every** object falls - yet each still lands on its exact beat. It has its own **`[sv]`** header, then `time:multiplier` lines.

```
[sv]
60000:6         # x6 fall speed
60500:0         # freeze
60900:0.15      # crawl
61200:1         # back to normal - always end on 1
```

- `1` normal, `>1` faster, `<1` slower, `0` frozen. Never below 0.
- **Stepped:** a value holds until the next line, then snaps (no easing).
- It's `x1` before the first line and **holds the last value forever**, so **always end an `[sv]` section with `:1`**.
- Hidden is left alone while SV is running.

!!! tip "Show the SV visually"
    Turn on **`EnableSvColor`** to tint and glow every object whose beat lands while SV is off its `1x` baseline - a clear cue for the SV section. The colour is set by `SvColor`; hyperdashes keep their red glow.

Requires **`EnableScrollVelocity`** (on by default).

---

## Loops

Repeat a pattern without typing every keyframe. A loop expands into plain `time:value` lines before anything else is read, so it works in `[sv]`, in the AR/Hidden block, and in any `[mod:*]` block - just put it under the right header.

```
loop <start> <beat-fraction> -> <end>
<value>
<value>
...
end
```

| Part | Meaning |
|------|---------|
| **start** | ms of the first keyframe (snapped onto osu!'s beat grid). |
| **beat-fraction** | Spacing as a fraction of a beat: `1/2`, `3/4`, `2/3`, `5/7`, or a plain decimal. Follows the map's red-line BPM, re-anchoring on each red line it crosses - so it lands where the editor would, even across BPM changes. |
| **end** | ms; keyframes run up to **and including** this time (an end *time*, not a count). The `->` is optional. |
| **values** | The part after `time:` - `0.5`, `1`, `9:sine`, `-:hd=3`. They cycle in order, one per keyframe (2 values over 6 beats -> A B A B A B). |

```
[sv]
loop 60000 1/2 -> 64000    # every 1/2 beat from 60s to 64s
2                          # values cycle: 2, 0.5, 2, 0.5, ...
0.5
end
64000:1
```

!!! note "Whole cycles only"
    A loop always ends on its last value - a partial cycle that would fit before `end` is dropped (A,B over 5 slots gives `A B A B`, not `A B A B A`). Pick an `end` with a little room; you don't have to hit it exactly.

---

## Hold

The classic mania "hold, then snap" scroll, in one line. Every object creeps down at `<slow>`, then **snaps** the last stretch at `<fast>`, still landing exactly on its beat. It's the smart version of a long hand-written loop - it scans the objects in the window and auto-fits each gap (a 1-beat gap gets a short hold, a 2-beat gap a longer one), re-anchoring across BPM changes. Goes under the `[sv]` header.

```
hold <start> <beat-fraction> <slow> <fast> -> <end>
```

| Part | Meaning |
|------|---------|
| **start** | ms of the first keyframe (snapped onto osu!'s beat grid). |
| **beat-fraction** | The grid the creep steps on: `1/16`, `1/8`, a decimal. Finer = smoother creep, tighter snap. |
| **slow** | Fall speed while holding (e.g. `0.1`, a crawl). |
| **fast** | Fall speed of the snap (e.g. `10`, a rapid drop). |
| **end** | ms the hold covers objects up to. `->` optional. SV returns to `x1` at `end` automatically. |

```
[sv]
hold 95801 1/16 0.1 10 -> 111747
```

!!! note "Every object lands clean"
    Every catchable object (fruit, droplet, banana) snaps on the division it lands in, so nothing ever crawls into the catcher. Dense streams have no room to hold and mostly snap; sparse rhythms give the big hold-then-drop.

---

## Mods

Custom mods read their own `[mod:name]` block from this file, so you can tune them without touching code. A block holds keyframes and settings:

```
[mod:wobble]
amount: 40
enabled: true
60000: 1
64000: 0
```

See the [full guide](GUIDE.md) for writing your own mods.

---

## Common mistakes

- **`hd` / `fi` keyframes but the master toggle is off.** `EnableHidden` / `EnableFadeIn` must be on.
- **An `[sv]` section that doesn't return to `1`.** The last value holds forever - end on `1` unless you *want* to hold a speed as a gimmick.
- **Putting `[sv]`, `loop`, or `hold` lines in the AR block.** AR keyframes are top-level; a section header ends that block. Loops go under the right header; `hold` goes under `[sv]`.
- **Expecting a partial loop cycle.** A 2-value loop over 5 slots gives `A B A B`, not `A B A B A`.

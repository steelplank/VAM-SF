# Examples

Ready-to-paste recipes for `VAM-profile.txt`. Drop any of these under the **`WRITE BELOW`** line, swap the timestamps for your own, and rebuild. Each one is a complete, self-contained block - for the full meaning of every field, see the [profile reference](PROFILE.md).

!!! tip "Times are milliseconds"
    Every `time` is the millisecond value the osu! editor shows on the timeline. Copy them straight from the editor.

---

## Gentle AR ramp

The signature move: start comfortable and tighten the read as the map builds. Values ease between keyframes, so this glides from AR 8 up to AR 10.

```
0:8            # AR 8 from the start
45000:9        # ease up to AR 9 by 45s
90000:10       # ease up to AR 10 by 90s
120000:10      # hold AR 10 to the end of the section
```

Want the last stretch to stay pinned? Repeat the final value - AR holds after the last keyframe anyway, but an explicit line makes the intent clear.

---

## Sudden AR spike

Snap the AR up for a single burst, then drop it back. `after` jumps *into* a keyframe with no ramp.

```
0:9
60000:9            # cruising at AR 9
60001:11:after     # instant jump to AR 11 for the burst
72000:11
72001:9:after      # snap back down to AR 9
```

---

## Hidden showcase

Fade Hidden in for a section, then back out. `hd=5` is osu!'s normal Hidden; higher hides sooner, lower hides later.

```
0:9:hd=0           # start fully visible
30000:9:hd=0
35000:9:hd=5       # Hidden fades in over 30-35s
80000:9:hd=5
85000:9:hd=0       # ...and back out over 80-85s
```

!!! note "Start with hd=0"
    If `EnableHidden` is on but you never set `hd`, the whole map can read as Hidden. Opening with `0:9:hd=0` is a safe habit.

---

## Fake-mania reading window

Stack Fade-In (hides the top) with Hidden (hides the bottom) to leave a visible band in the middle - the fake-mania read.

```
0:9:hd=0:fi=0
40000:9:hd=5:fi=5      # comfortable middle window fades in
90000:9:hd=5:fi=8      # window tightens toward the drop
110000:9:hd=0:fi=0     # open back up
```

Push `fi` higher to narrow the window; by `fi=10` it closes almost completely for a very tight read.

---

## Scroll Velocity rush

A mania-style speed change over the whole field. Remember the `[sv]` header, and **always end on `1`** - the last value holds forever.

```
[sv]
60000:6         # x6 fall speed - sudden rush
60500:0         # freeze
60900:0.15      # crawl
61200:1         # back to normal
```

!!! tip "Show it visually"
    Turn on **`EnableSvColor`** to tint every object whose beat lands while SV is off its `1x` baseline - a clear cue for the SV section.

---

## SV pulse with a loop

Alternate fast and slow every half-beat without typing each keyframe. A loop expands before anything else is read, so it works under `[sv]`.

```
[sv]
loop 60000 1/2 -> 68000    # every 1/2 beat from 60s to 68s
2                          # values cycle: 2, 0.5, 2, 0.5, ...
0.5
end
68000:1                    # always return to x1
```

---

## Mania hold-then-snap

The classic "hold, then drop." Every object creeps at `0.1`, then snaps the last stretch at `10`, still landing on its beat. `hold` scans the objects in the window and auto-fits each gap. Goes under `[sv]`.

```
[sv]
hold 95801 1/16 0.1 10 -> 111747
```

That's the whole thing - one line. SV returns to `x1` at the end time automatically. Finer grids (`1/16`) give a smoother creep and a tighter snap; sparse rhythms give the big dramatic hold.

---

## Putting it together

The AR/Hidden/Fade-In block and the `[sv]` section live in the same file - AR keyframes go at the top, then the `[sv]` header starts the speed section.

```
# --- AR / HD / FI ---
0:8:hd=0
45000:10           # ease up as the map builds
90000:10:hd=5      # add Hidden for the second half
150000:10:hd=5

# --- Scroll Velocity ---
[sv]
hold 120000 1/16 0.1 8 -> 135000    # a big hold-drop mid-map
135000:1
```

For every flag, easing name, and the full `hd`/`fi` scales, see the [profile reference](PROFILE.md).

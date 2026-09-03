# VAM:SF - Advanced Guide

Advanced guide going in-depth into everything past the install: what each effect setting does, how to write `VAM-profile.txt`, how to write your own mod, and how the storybrew layers fit together.
For installing and publishing, see [INSTALL.md](../package/VAM-SF/INSTALL.md); for the profile's own quick syntax, the top of `VAM-profile.txt` documents itself. This guide is the deeper reference.

> Every setting below is a field on one of the three effects. In storybrew you reach them from the **Effects** tab: click the cog next to the effect. Defaults are given as `(default: X)` - the defaults already produce a working, osu-faithful result, so change only what you need.

---

## Contents

1. [The three effects and the layer stack](#1-the-three-effects-and-the-layer-stack)
2. [VAM_Generator - the core](#2-vam_generator---the-core)
3. [VAM_Cover - hiding the real game](#3-vam_cover---hiding-the-real-game)
4. [VAM_Countdown - the intro count](#4-vam_countdown---the-intro-count)
5. [Writing VAM-profile.txt](#5-writing-vam-profiletxt)
6. [Writing a mod](#6-writing-a-mod)
7. [Extras](#7-extras)

---

## 1. The three effects and the layer stack

VAM:SF is three storybrew effects that work together:

| Effect | Job |
|--------|-----|
| **VAM_Generator** | Draws the fake catch objects (fruits, droplets, bananas) and applies AR, Hidden, Fade In, SV, miss simulation, skin support, and mods. This is where almost all settings live. |
| **VAM_Cover** | Draws the cover that hides the real gameplay, split into a top region and a bottom region so the catcher stays visible. |
| **VAM_Countdown** | Redraws osu!'s "Ready? 3 2 1 GO!" intro, because the cover would otherwise hide osu!'s own. |

The illusion only works if these render in the right order. Remember that storybrew layers are opposite of how they're actually stacked - layer at the very top is covered by everything else and layer at the very bottom will be covering any other layers.

- **VAM_Countdown** if used, should be placed at the **very bottom** of the effect list, lower than the generator. This is to ensure that the countdown will cover fruits, if such edge case will occur.
- **VAM_Generator** (the objects) is usually placed at the very bottom of the effect list **unless** you're using countdown. In that case, it should be right above the countdown.
- **VAM_Cover** produces two pieces - its top region goes on the **Overlay** OSB layer, its bottom region on the **Background** OSB layer.

In short, they should look something like this in the `Layers` tab:
| Layers | Type |
|--------|------|
| VAM_Cover (Background) | Background |
| - | Fail |
| - | Pass |
| - | Foreground |
| VAM_Cover (Overlay) | Overlay |
| VAM_Generator (VAM Objects) | Overlay |
| VAM_Countdown (VAM Countdown) | Overlay |

---

## 2. VAM_Generator - the core

### Objects and playfield geometry

These map osu!'s 512x384 playfield onto storyboard space. The defaults are correct for a standard widescreen storyboard; only touch them if objects don't line up with the real catch line.

| Setting | Default | What it does |
|---------|---------|--------------|
| `CenterX` | 320 | Storyboard x of the playfield centre. Leave at 320. |
| `PlayfieldWidth` | 512 | Storyboard width the osu x-range 0..512 is stretched across. Leave at 512. |
| `SpawnY` | -28 | Storyboard y where a fruit becomes visible (just above the screen). |
| `CatchY` | 412 | Storyboard y of the catch line (the platter). |
| `CircleSizeOverride` | -1 | Force a Circle Size for object sizing. Negative = use the map's real CS. Not recommended to change as it breaks the "1:1 to osu! generation" rule. |
| `DrawOverlays` | true | Draw the untinted `-overlay` layer on each object (the osu! two-part fruit look). |
| `RenderFruits` / `RenderDroplets` / `RenderTinyDroplets` / `RenderBananas` | all true | Turn whole object classes on/off. Handy for debugging or stylistic maps. |

**Recommended:** leave all of these alone unless something is visibly misaligned, in which case `CatchY` and `SpawnY` are the two worth nudging.

### Dynamic AR

The headline feature: change Approach Rate over time, including values osu! can't reach.

| Setting | Default | What it does |
|---------|---------|--------------|
| `FakeApproachRate` | 9.0 | Constant AR used when the profile has no AR keyframes. A quick way to set one flat AR. |
| `OverrideWithMapAr` | false | Ignore the fake AR entirely and use the map's real AR. Useful for A/B testing. |
| `EnableEasing` | true | Ease AR transitions between keyframes. Off = linear. |
| `Easing` | SineInOut | The default easing curve for transitions that don't name their own. |

AR over time is written in `VAM-profile.txt` (see [section 5](#5-writing-vam-profiletxt)). If the profile has AR keyframes they win; otherwise `FakeApproachRate` is used as a constant.

**Recommended:** keep `EnableEasing` on but feel free to play around with easing types. Turning it off and using linear easing is also worth trying out - this one is the easiest to understand and properly implement. I recommend using `after`/`before` on individual keyframes when you want a sudden snap.

### Fake Hidden

osu!catch Hidden makes objects fade out on the way down. VAM reproduces it and lets you slide *where* the fade happens.

| Setting | Default | What it does |
|---------|---------|--------------|
| `EnableHidden` | true | Master switch. Must be on if you use any `hd` keyframes. |
| `HiddenUseGameValues` | true | Use osu!catch's exact HD timing. Off = use the manual fractions below. |
| `HiddenFadeStart` | 0.40 | Manual mode only: fraction of the fall where the fade begins. |
| `HiddenFadeEnd` | 0.56 | Manual mode only: fraction of the fall where the object is fully gone. |

Strength is set per-keyframe in the profile with `hd=0..10` (0 off, 5 = osu!'s normal Hidden, 10 = hidden the instant it appears). Keep `HiddenUseGameValues` on for an authentic feel; the manual fractions are an escape hatch for custom looks.
> Worth noting: if you enable hidden and do not declare it in the VAM-profile.txt, your entire map might be using hidden. I tend to always add `0:9:hd=0` at the beginning of the file to make sure that HD won't be on by accident.

### Fade In

The mirror of Hidden, borrowed from osu!mania: objects are invisible at the **top** of the fall and fade **in** lower down.

| Setting | Default | What it does |
|---------|---------|--------------|
| `EnableFadeIn` | true | Master switch. Must be on if you use any `fi` keyframes. |

Strength is `fi=0..10` in the profile. Fade In composes with Hidden: Fade In hides the top, Hidden hides the bottom, so `fi=5` + `hd=5` leaves a readable band in the middle - a genuine fake-mania reading window. Higher `fi` reveals lower and narrows that window - by `fi=10` the reveal reaches into `hd=5`'s fade and the window closes almost entirely, for a very tight read. An object is always fully shown before it lands.

### Scroll Velocity

Mania-style SV: speed up, slow down, or freeze how fast **every** object falls, while each still lands exactly on its beat. *Optional:* A visual tell for SV sections: tint and glow every object whose beat lands while SV is off its `1x` baseline. Off by default.

| Setting | Default | What it does |
|---------|---------|--------------|
| `EnableScrollVelocity` | true | Master switch for the `[sv]` timeline in the profile. |
| `EnableSvColor` | false | Turn the SV tint on. |
| `SvColor` | `#b4f8ff` | Body tint colour for SV objects. |
| `SvGlowColor` | `#11e7ff` | Glow colour for SV objects. Only fruits and big droplets glow. |
| `SvColorWindow` | 10 | Tolerance in ms for catching objects sitting exactly on an SV boundary. Raise if edge objects still miss the tint, lower if too many normal objects get tinted. |

SV is a stepped timeline (`time:multiplier`) under an `[sv]` header. `1` = normal, `>1` faster, `<1` slower, `0` frozen.
It holds each value until the next line and holds the last forever, so **always end an SV section with `:1`**.

**Recommended:** Follow your heart when it comes to using both SV and SV color. I prefer having colors off but this was added after feedback from testers. Either way, hyperdashes always keep their red glow.

> It might look weird when you have a very long custom SV section or you end with value different than 1. This is a limitation of my engine - will work on it in the future.

### Miss simulation

An "osu-like" fake miss: when the plate wouldn't have caught a fruit, it keeps falling past the platter instead of vanishing.

| Setting | Default | What it does |
|---------|---------|--------------|
| `EnableCatchMiss` | true | Draw missed objects falling past the plate. Fruits only - droplets/bananas aren't detectable. |
| `MissFadeDuration` | 250 | Milliseconds a missed object keeps falling past the plate (osu! uses 250). |
| `CatchTriggerWindow` | 5 | Half-width (ms) of the HitSound match window, auto-clamped to half the gap to the nearest fruit. Advanced; rarely needs changing. |

This is a best-effort detection based on hitsounds, so it's approximate - accurate enough to look right, not a real judgement.

### Skin support

| Setting | Default | What it does |
|---------|---------|--------------|
| `UseSkinSprites` | false | Draw the **player's** skin objects instead of the bundled PNGs. Requires `UseSkinSprites: 1` in the `.osu` `[General]` (the installer sets it). |
| `RotateObjects` | true | Rotate objects the way osu! does, using per-object RNG. Fully supported for square skins. |
| `SpriteFolder` | `sb/vam/` | Folder holding the bundled default-skin PNGs. |

**Important:** with `UseSkinSprites` on, objects **will not render in storybrew's preview** - that's expected, they render in-game.
For a map you plan on releasing, I highly recommend on exporting it with `UseSkinSprites` set to **True**. It's more convincing and better overall for the player to use skin sprites.

### Hyperdash glow

| Setting | Default | What it does |
|---------|---------|--------------|
| `HyperDashGlow` | true | Draw osu!'s red hyperdash glow on the object before an impossible jump. |
| `HyperDashColor` | `#FF0000` | The glow colour (hex `#RRGGBB`). |

> Why would you tweak with this... oh god, a hyperdash-hyperless map is possible...

### Combo colours and bananas

| Setting | Default | What it does |
|---------|---------|--------------|
| `UseComboColors` | false | Tint objects with the map's combo colours. **Work in progress - not well tested, may be buggy.** |
| `BananaColor` | `#FFD23E` | Banana colour (hex `#RRGGBB`). |

**Recommended:** for publishing, most maps combine `UseComboColors = false` with the combo-strip / white-colours `.osu` mod so objects render on their intended colours.

> Some players use colours in their skins, some are using pure white. Unfortunately I cannot extract that information so the second best thing is to use pure white and not bother with combo colours.

### Mods

| Setting | Default | What it does |
|---------|---------|--------------|
| `EnableModifiers` | false | Enable mod discovery and the mod pipeline. Required to use any mod. |

See [section 6](#6-writing-a-mod). Off by default so the base engine stays intact.

### Advanced / housekeeping

| Setting | Default | What it does |
|---------|---------|--------------|
| `ProfilePath` | `VAM-profile.txt` | Path to the keyframe file. |
| `RefreshProfile` | false | Manual refresh - flick on then off if a profile edit doesn't pick up. |
| `StartTime` / `EndTime` | 0 / 0 | Limit generation to a time window (0,0 = whole map). Useful for testing a section. |
| `LayerName` | `VAM Objects` | Name of the storyboard layer the objects go on. Changing it has no real effect. |
| `ArKeyframes` / `HdKeyframes` | empty | Legacy inline keyframe strings. Prefer the profile file; leave these blank. |

---

## 3. VAM_Cover - hiding the real game

The cover is what makes the illusion possible: an opaque layer over the real playfield, split so the catcher area stays visible.

### The image

| Setting | Default | What it does |
|---------|---------|--------------|
| `SpritePath` | `sb/vam/black.png` | The cover image. Empty or `black` = a pure-black tile. `background` = the map's background. Anything else = that image path. |
| `BlackTilePath` | `sb/vam/black.png` | The bundled black tile used for the `black` option. |
| `Opacity` | 1.0 | Cover opacity. `1.0` fully hides what's behind. Keep at 1.0 for image covers - darken with `BackgroundDim` instead. |

### Fitting an image cover

Only relevant when `SpritePath` is an image (not the black tile).

| Setting | Default | What it does |
|---------|---------|--------------|
| `BackgroundFit` | Fill | How the image is fitted before it's split. **Fill** = cover-crop, no distortion for a matching aspect. **Fit** = letterbox (black bars, still opaque). **Stretch** = old stretch-to-fill (distorts). |
| `BackgroundDim` | 0.75 | Darken an image cover so the storyboard fruits stay readable. `0` = untouched, `1` = black. Baked into the pixels; the cover stays fully opaque. |

**Recommended:** I usually play with bg dim at around 90% so using map background with Opacity = 0.9 works the best for me. But many players don't like visible backgrounds and that can throw them off especially with variable AR. Best to keep it on default black.

### Geometry and lead-in

| Setting | Default | What it does |
|---------|---------|--------------|
| `CoverBottomY` | 412 | Where the top cover ends - match this to the Generator's `CatchY`. |
| `ScreenBottomY` | 480 | Bottom edge the bottom cover extends to. |
| `CoverWidth` | 854 | Storyboard width to span. 854 covers widescreen; use 640 for 4:3. |
| `AutoLeadIn` | true | Bring the cover up **before** the first object can appear, so maps with little audio lead-in are never seen uncovered. |
| `LeadInPreempt` | 1800 | Safety preempt (ms) subtracted from the first object's time to place that lead-in. |

**Recommended:** just don't touch these settings and you'll be fine.

> The background-branding step at publish time (see [section 7](#7-layer-setup-and-publishing-in-depth)) is separate from this. It stamps your usage card onto a copy of the *song-select* background - it does not touch the in-game cover, as long as `SpritePath` isn't set to `background`.

---

## 4. VAM_Countdown - the intro count

Redraws osu!'s pre-map countdown so it shows on top of the cover.

| Setting | Default | What it does |
|---------|---------|--------------|
| `Mode` | Auto | **Auto** = show only when the `.osu` `[General] Countdown` isn't 0, using its speed. **ForceOn** = always show (uses `SpeedWhenForced`). **ForceOff** = never show. |
| `SpeedWhenForced` | Normal | Used only in ForceOn. Normal = one count per beat, Half = one per two beats, Double = two per beat. |
| `ShowReady` | true | Show the "Ready?" element. |
| `ShowGo` | true | Show the "GO!" element on the beat before the first object. |
| `CenterX` / `CenterY` | 320 / 240 | Screen position of the countdown centre. |
| `Scale` | 1.0 | Sprite scale. The bundled art is SD (1x), so 1.0 matches osu!'s size. |
| `ExtraOffsetBeats` | 0 | Extra beats of lead on top of the `.osu` CountdownOffset. Positive = starts earlier. |
| `BeatLengthOverride` | 0 | ms per beat (60000/BPM). `<= 0` = use the map's timing at the first object. |
| `UseSkinSprites` | false | Draw the player's skin countdown instead of the bundled art.|
| `SpriteFolder` | `sb/vam/` | Folder with the bundled countdown PNGs. Ignored when `UseSkinSprites` is on. |
| `LayerName` | `VAM Countdown` | Name of the layer. Doesn't do anything special. |

**Recommended:** leave on `Auto` so it mirrors whatever the map's own countdown setting is. UseSkinSprites is really cool - if the player skin got it right. If unsure, keep it off.

---

## 5. Writing VAM-profile.txt

The file documents its own syntax at the top; this section is the "how to actually use it" companion.
The profile holds three things on one timeline: **AR/Hidden/Fade-In** keyframes (top level), a **`[sv]`** section, and any **`[mod:*]`** blocks.
Write your keyframes under the `WRITE BELOW` line at the bottom.

### AR keyframes

One `time:ar` per line. `time` is milliseconds on the osu! editor timeline. Values ease between keyframes and hold before the first and after the last.

```
0:8            # AR 8 from the start
45000:10       # ease up to AR 10 by 45s
90000:9.5      # ease back to 9.5 by 90s
```

Add flags after the AR, in any order, separated by colons:

- **Easing name:** `linear`/`none`, `sine`, `sinein`, `sineout`, `quad`/`quadin`/`quadout`, `cubic`/`cubicin`/`cubicout`. Overrides the effect's default easing for that transition.
- **`after`** - snap *into* this keyframe (a sudden change, no ease in).
- **`before`** - snap *out of* this keyframe.

```
60000:10.5:after     # AR jumps to 10.5 at 60s with no ramp
60000:9:sinein       # ease in with a specific curve
```

### Hidden and Fade In columns

`hd=N` and `fi=N` (0..10) attach to any keyframe. The AR value can be blank or `-`/`_` on a line that only changes Hidden/Fade-In:

```
60000:9:hd=5         # AR 9, normal Hidden on
60000:-:hd=0         # Hidden off, AR unchanged
60000:9:hd=5:fi=5    # AR 9, Hidden + Fade In together
```

Both ramp with AR's easing and scope, so you can fade Hidden in and out smoothly:

```
60000:9:hd=0
65000:9:hd=5         # Hidden fades in over 60-65s
90000:9:hd=5
95000:9:hd=0         # ...and back out over 90-95s
```

### The `[sv]` section

Its own header, then `time:multiplier`, stepped. Ends by holding the last value, so close with `:1`.

```
[sv]
60000:6         # x6 fall speed
60500:0         # freeze
60900:0.15      # crawl
61200:1         # back to normal - always end on 1
```

### Loops

Repeat a pattern without typing every keyframe. Don't worry, it works in any section.

```
[sv]
loop 60000 1/2 -> 64000    # every 1/2 beat from 60s up to 64s
2                          # values cycle, one per keyframe...
0.5                        # ...here: 2, 0.5, 2, 0.5, ...
end
64000:1
```

More advanced loop magic (slow down every 3/2 beat):

```
loop 88293 1/2 -> 105168   # every 1/2 beat loop the following:
1                          # unchanged SV for 1/2 of a beat
1                          # unchanged SV for 1/2 of a beat
1                          # unchanged SV for 1/2 of a beat
0.5                        # we *slow* it down for 1/2 of a beat
end
105169:1                   # to make sure we exit the loop with SV=1, I've placed that keyframe one ms after the loop
```

`start` snaps to the osu! beat grid; `beat-fraction` can be `1/2`, `3/4`, `2/3`, a decimal, etc., and uses the map's BPM at `start`.
Only **whole cycles** are emitted, so the loop always ends on the last value - pick an `end` with a little room rather than hitting it exactly.
Haven't tested this loop functionality a lot and osu! rounding makes it extra difficult so report any issues if you find any.

### Hold (mania hold-then-snap)

A one-liner for the classic mania "hold, then snap" scroll: every object creeps down slowly, then makes a rapid final drop onto its beat. It lives in `[sv]`:

```
[sv]
hold 95801 1/16 0.1 10 -> 111747
#    start  div  slow fast   end
```

`start`/`end` bound the window, `beat-fraction` (`1/16` here) is the grid the creep steps on, and the two numbers are the slow (creep) and fast (snap) speeds. The `->` is optional, and SV returns to `1x` at `end` automatically.

It replaces a long hand loop of `0.1`s ending in one fast value - and it's smarter about it. `hold` looks at where the objects actually are and auto-fits each gap: a 1-beat gap gets a short hold, a 2-beat gap a longer one, and it re-anchors across BPM changes (great for maps like deltaMAX where the rhythm keeps shifting). Every object gets its snap on the beat-division it lands in, so nothing ever crawls into the catcher - which also means dense streams have no room to hold and end up mostly snapping, while sparse rhythms give the big hold-then-drop.

### Common mistakes

- **`hd`/`fi` keyframes but the master toggle is off.** `EnableHidden` / `EnableFadeIn` must be on.
- **SV section that doesn't return to `1`.** The last value holds forever but it breaks due to engine limitations. It might look funky. Make sure to always end SV sections with 1.
- **Putting `[sv]` or `loop` lines in the AR block.** AR keyframes are top-level only; a section header ends the AR block.
- **Expecting a partial loop cycle.** A 2-value loop over 5 slots gives `A B A B`, not `A B A B A`.

---

## 6. Writing a mod

Mods are the extension surface: drop a `.cs` file into `scriptslibrary/VAM/Modifiers/`, and it's auto-discovered - no engine change needed. Turn on **`EnableModifiers`** on VAM_Generator to use any.

### The interface

Every mod implements `IVamModifier`, but it's easiest to inherit `VamModifier`, which gives you sane defaults and helpers:

```csharp
namespace StorybrewScripts.Vam
{
    public sealed class MyMod : VamModifier
    {
        public override int Order { get { return 100; } }   // lower runs earlier (default 100)

        // Which objects this mod touches.
        public override bool AppliesTo(VamObject obj, VamContext ctx)
        {
            return OnlyFruits(obj) && TimeWindow(obj, 60000, 90000);
        }

        // What it does to each matching object's plan.
        public override void Apply(VamPlan plan, VamObject obj, VamContext ctx)
        {
            plan.ShiftSpawn(150, 0);   // start 150px to the side; still lands on the plate
        }
    }
}
```

Discovery is by reflection: any `IVamModifier` with a parameterless constructor is picked up, instances are sorted by `Order` (ties broken by type name), and a mod that throws is isolated and logged rather than breaking the build.

### Helpers on the base class

- `TimeWindow(obj, startMs, endMs)` - true inside the window; `(0, 0)` means the whole map.
- `OnlyFruits(obj)` - fruit objects only.
- `EveryNth(obj, n)` - true when `obj.Index % n == 0`.

### What you can change - the VamPlan

`Apply` edits the object's `VamPlan`. The most useful handles:

| On `plan` | Effect |
|-----------|--------|
| `ShiftSpawn(dx, dy)` | Move the spawn point; the catch stays put, so a straight fall becomes diagonal. |
| `SplitFall(atFraction, secondSpeedFactor)` | Insert a midpoint at `atFraction` of the path and re-time the two halves to speeds `1 : secondSpeedFactor`, still arriving on time. |
| `Position` | The full fall path (a `Track<Vec2>`) if you want finer control. |
| `Scale`, `ScaleStart` | Object size. |
| `BodyColor`, `DrawGlow`, `GlowColor` | Tint and glow. |
| `RotStart`, `RotEnd` | Rotation. |

`ctx` is read-only map/effect state: `ctx.Beatmap`, `ctx.CircleSize`, `ctx.MapApproachRate`, `ctx.Profile`, `ctx.PreemptFor(ar)`, and `VamRng` for osu-faithful randomness.

### Reading settings from the profile

A mod can read its own `[mod:name]` block from `VAM-profile.txt` with `ctx.Mod("name")` - so users tune it without editing code. A block holds keyframes (`time:value`) and settings (`key: value`):

```
[mod:wobble]
amount: 40
enabled: true
60000: 1
64000: 0
```

```csharp
var cfg = ctx.Mod("wobble");
if (!cfg.GetBool("enabled", false)) return false;   // in AppliesTo
double amount = cfg.GetDouble("amount", 20);        // in Apply
```

`cfg.Present` tells you if the block existed; `GetDouble`/`GetBool`/`Get` read named settings with defaults; `cfg.Keyframes` gives you the `time:value` list.

### Learn from the two bundled examples

- **`Modifiers/_Template.cs`** - a do-nothing skeleton. Copy it, rename the class, fill in `AppliesTo` + `Apply`.
- **`Modifiers/SpawnOffset.cs`** - a working proof of concept: in a time window it offsets each object to the side (alternating by index) so fruit slides in diagonally, still landing on the plate. Read it as a real example of `TimeWindow` + `ShiftSpawn`.

---

## 7. Extras

### How settings interact with publishing

The publish step (`install.ps1` Quick Publish, or the separate brand/merge actions) does four things, and two of them lean on settings above:

- **AR & OD set to 0.** Intentional - it forces players onto the storyboard rather than the (now hidden) real objects. This is a `.osu` edit, independent of the effect settings.
- **Combo strip + white colours.** Pairs with keeping `UseComboColors` **off** so the beam coming from the platter is in the same color (in this case, white).
- **Background branding.** Stamps your usage card onto a copy of the background (`<bg>-vam.jpg`) and points only the published diff at it. If `VAM_Cover`'s `SpritePath` is set to `background`** then the branded card would bake into the in-game cover.
  Keep the cover on the default black tile (or point it at the *original* background filename) so the card only ever shows in song-select.
- **Storyboard inlined, `.osb` deleted.** Makes the published diff self-contained. Run publishing on the **copy you upload**, never your working project - storybrew rebuilds the `.osb` on its next save.

> **IMPORTANT:** I really recommend combining .osu and .osb together as this is the only way to make cheating impossible. osu! **does not** verify the integrity of .osb files during score submission - you can easily replace the storyboard with whatever you want and it will be submitted without any issues. Same applies to replays - they will work even if you change .osb after.

See [INSTALL.md](../package/VAM-SF/INSTALL.md) section 6 for the step-by-step publish flow.

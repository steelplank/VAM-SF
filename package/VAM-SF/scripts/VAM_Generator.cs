using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewScripts.Vam;
using System;
using System.Linq;

namespace StorybrewScripts
{
    // VAM:SF main generator. Renders a 1:1 osu!catch copy using storyboard and enables dynamic visual effects.
    // If you're thinking of adding new features or modifying how it works, I recommend creating a custom mod instead of editing this file.
    // More info in /scriptslibrary/VAM/Modifiers/_Template.cs
    public class VAM_Generator : StoryboardObjectGenerator
    {
        [Group("Profile file")]
        [Configurable] public bool ThisFileIsInTheProjectRootFolder = true;
        [Description("Path to the AR/HD keyframe file. Instructions inside.")]
        [Configurable] public string ProfilePath = "VAM-profile.txt";
        [Description("Manual refresh if something goes wrong. Just flick it on and off to refresh.")]
        [Configurable] public bool RefreshProfile = false;

        [Group("Basic settings:")]
        [Description("Use the player's SKIN objects instead of the bundled PNGs. Requires 'UseSkinSprites: 1' in the .osu [General]. Objects WILL NOT RENDER in storybrew.")]
        [Configurable] public bool UseSkinSprites = false;
        [Description("Uses map combo colors for the objects. Experimental - off by default.")]
        [Configurable] public bool UseComboColors = false;
        [Description("My best attempt at detecting misses and drawing them during gameplay. Only fruits are detectable.")]
        [Configurable] public bool EnableCatchMiss = true;
        [Description("Rotate objects like osu. Uses per-object RNG but wasn't well tested.")]
        [Configurable] public bool RotateObjects = true;
        [Description("Enable fake hidden. Using default osu! values. You have to turn it on if you have hidden keyframes.")]
        [Configurable] public bool EnableHidden = true;
        [Description("Enable fake mania Fade In (objects invisible at the top, fade in lower). Reads the profile 'fi' column. Composes with hidden - fi=5 + hd=5 leaves a visible window. Turn on if you have fi keyframes.")]
        [Configurable] public bool EnableFadeIn = true;
        [Description("Enable the mania-like sv timeline from the profile file.")]
        [Configurable] public bool EnableScrollVelocity = true;
        [Description("Tint + glow objects whose beat lands while SV is off its 1x baseline (a visual cue for SV sections). Hyperdashes keep their red glow.")]
        [Configurable] public bool EnableSvColor = false;
        [Description("Set constant AR if you're lazy. Also useful for testing.")]
        [Configurable] public double FakeApproachRate = 9.0;
        [Description("Ignore fake AR and use the map's real AR.")]
        [Configurable] public bool OverrideWithMapAr = false;
        [Description("Ease AR between keyframes (false = linear).")]
        [Configurable] public bool EnableEasing = true;
        [Configurable] public VamEasing Easing = VamEasing.SineInOut;

        [Group("Modifiers")]
        [Description("Enable mods and mod discovery. If you want to use mods, you have to keep this enabled.")]
        [Configurable] public bool EnableModifiers = false;

        [Group("Advanced settings:")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 0;
        [Description("Storyboard x of playfield centre. Set to 320 if you don't know what you're doing.")]
        [Configurable] public double CenterX = 320;
        [Description("Storyboard width the osu-x range 0..512 is stretched across. Keep it on 512 if you don't know what you're doing.")]
        [Configurable] public double PlayfieldWidth = 512;
        [Description("Storyboard y where a fruit becomes visible. Around -28 works for me but might not work for you.")]
        [Configurable] public double SpawnY = -28;
        [Description("Storyboard y of the catch line (platter). Y = 412 is right for me, tweak it around if it doesn't look right.")]
        [Configurable] public double CatchY = 412;
        [Description("Folder holding the bundled default-skin PNGs.")]
        [Configurable] public string SpriteFolder = "sb/vam/";
        [Description("Draw the untinted -overlay layer over each object.")]
        [Configurable] public bool DrawOverlays = true;
        [Description("Force a Circle Size for object sizing - not recommended to modify. Negative = use the map's real CS.")]
        [Configurable] public double CircleSizeOverride = -1;
        [Description("Banana colour as hex #RRGGBB.")]
        [Configurable] public string BananaColor = "#FFD23E";
        [Description("Draw osu!'s red hyperdash glow. Why would you want to turn it off?")]
        [Configurable] public bool HyperDashGlow = true;
        [Description("Glow colour as hex #RRGGBB.")]
        [Configurable] public string HyperDashColor = "#FF0000";
        [Description("Object body tint (hex #RRGGBB) for SV objects.")]
        [Configurable] public string SvColor = "#b4f8ff";
        [Description("Glow colour (hex #RRGGBB) for SV objects. Only fruits and big droplets glow.")]
        [Configurable] public string SvGlowColor = "#11e7ff";
        [Description("Tolerance (ms) for the SV tint. Catches objects on a boundary. Raise if edge objects still miss, lower if too many normal objects tint.")]
        [Configurable] public double SvColorWindow = 10;
        [Configurable] public bool RenderFruits = true;
        [Configurable] public bool RenderDroplets = true;
        [Configurable] public bool RenderTinyDroplets = true;
        [Configurable] public bool RenderBananas = true;
        [Description("Use osu!catch exact HD timing. Off = manual fractions.")]
        [Configurable] public bool HiddenUseGameValues = true;
        [Description("Manual mode: fraction of the fall where the HD fade begins. Default: 0.40")]
        [Configurable] public double HiddenFadeStart = 0.40;
        [Description("Manual mode: fraction of the fall where the object is fully gone. Default: 0.56")]
        [Configurable] public double HiddenFadeEnd = 0.56;
        [Description("Half-width (ms) of the HitSound match window, auto-clamped to half the gap to the nearest fruit.")]
        [Configurable] public double CatchTriggerWindow = 5;
        [Description("Milliseconds a missed object keeps falling past the plate (osu uses 250).")]
        [Configurable] public double MissFadeDuration = 250;
        [Description("Name of the layer for fake objects. As simple as that. Changing it won't really change anything lol")]
        [Configurable] public string LayerName = "VAM Objects";
        [Configurable] public string ArKeyframes = "";
        [Configurable] public string HdKeyframes = "";

        public override void Generate()
        {
            var osuPath = VamOsuPathResolver.Resolve(MapsetPath, Beatmap.Name, Beatmap.Id);
            if (string.IsNullOrEmpty(osuPath)) { Log("VAM_Generator: no .osu found in " + MapsetPath); return; }

            VamBeatmap map;
            try { map = VamLoader.Load(osuPath, computeHyperDash: HyperDashGlow); }
            catch (Exception e) { Log("VAM_Generator: load/convert failed: " + e.Message); return; }

            var geometry = new VamGeometry { CenterX = CenterX, PlayfieldWidth = PlayfieldWidth, SpawnY = SpawnY, CatchY = CatchY };
            var arProfile = new VamArProfile(ArKeyframes, FakeApproachRate, EnableEasing, Easing);
            var hdProfile = new VamHdProfile(HdKeyframes, 1.0, EnableEasing, Easing);

            VamProfile profile = null;
            string profileText = null;
            if (!string.IsNullOrWhiteSpace(ProfilePath))
            {
                try
                {
                    var full = System.IO.Path.Combine(ProjectPath, ProfilePath);
                    try { AddDependency(full); } catch { }
                    if (System.IO.File.Exists(full))
                    {
                        profileText = System.IO.File.ReadAllText(full);
                        System.Collections.Generic.List<string> loopErrors;
                        profileText = VamLoopExpander.Expand(profileText, map, out loopErrors);   // expand loop..end blocks first
                        foreach (var er in loopErrors) Log("VAM-profile loop " + er);
                        System.Collections.Generic.List<string> holdErrors;
                        profileText = VamSvHoldExpander.Expand(profileText, map, out holdErrors);  // expand [sv] `hold` lines into keyframes
                        foreach (var er in holdErrors) Log("VAM-profile " + er);
                        profile = new VamProfile(profileText, FakeApproachRate, EnableEasing, Easing);
                        foreach (var er in profile.Errors) Log("VAM-profile " + er);
                    }
                }
                catch (Exception e) { Log("VAM_Generator: could not read profile: " + e.Message); }
            }

            var layer = GetLayer(LayerName);
            double effectiveCs = CircleSizeOverride >= 0 ? CircleSizeOverride : map.CircleSize;
            double baseScale = VamGeometry.ObjectScaleFromCircleSize(effectiveCs);

            var scrollVelocity = new VamScrollVelocity(profileText);
            if (scrollVelocity.HasKeyframes && EnableScrollVelocity)
                Log("VAM_Generator: scroll velocity active ([sv] section).");

            var builder = new VamPlanBuilder
            {
                Geometry = geometry,
                ArProfile = arProfile,
                HdProfile = hdProfile,
                Profile = profile,
                OverrideWithMapAr = OverrideWithMapAr,
                MapApproachRate = map.ApproachRate,
                BaseScale = baseScale,
                ScrollVelocity = scrollVelocity,
                EnableScrollVelocity = EnableScrollVelocity,
                EnableSvColor = EnableSvColor,
                SvColor = SvColor,
                SvGlowColor = SvGlowColor,
                SvColorWindow = SvColorWindow,
                EnableHidden = EnableHidden,
                EnableFadeIn = EnableFadeIn,
                HiddenUseGameValues = HiddenUseGameValues,
                HiddenFadeStart = HiddenFadeStart,
                HiddenFadeEnd = HiddenFadeEnd,
                EnableCatchMiss = EnableCatchMiss,
                CatchTriggerWindow = CatchTriggerWindow,
                MissFadeDuration = MissFadeDuration,
                RotateObjects = RotateObjects,
                UseComboColors = UseComboColors,
                BananaColor = BananaColor,
                UseSkinSprites = UseSkinSprites,
                SpriteFolder = SpriteFolder,
                DrawOverlays = DrawOverlays,
                HyperDashGlow = HyperDashGlow,
                HyperDashColor = HyperDashColor,
            };
            builder.ComputeTriggerWindows(map.Objects);

            var ctx = new VamContext
            {
                Beatmap = map,
                Geometry = geometry,
                CircleSize = effectiveCs,
                MapApproachRate = map.ApproachRate,
                Profile = profile,
                Mods = VamModConfig.ParseAll(profileText),
            };

            var pipeline = new VamPipeline(Log);
            if (EnableModifiers)
            {
                pipeline.DiscoverModifiers();
                Log($"VAM_Generator: modifiers on ({pipeline.Modifiers.Count} discovered).");
            }

            var emitter = new VamEmitter();
            int windowEnd = EndTime > 0 ? EndTime : int.MaxValue;
            int rendered = 0;

            // Draw order in a storyboard layer is creation order. Emit tiny droplets first so they
            // sit UNDER every other object (slider heads/ends, big droplets, fruit); then the rest
            // in time order. Both passes keep the map's time ordering within themselves.
            var ordered = map.Objects.Where(o => o.Type == VamObjectType.TinyDroplet)
                .Concat(map.Objects.Where(o => o.Type != VamObjectType.TinyDroplet));

            foreach (var obj in ordered)
            {
                if (obj.Time < StartTime || obj.Time > windowEnd) continue;
                if (!ShouldRender(obj.Type)) continue;
                var plan = EnableModifiers ? pipeline.Run(builder, obj, ctx) : builder.Build(obj);
                emitter.Emit(layer, plan);
                rendered++;
            }

            Log($"VAM_Generator: rendered {rendered} of {map.Objects.Count}. CS={effectiveCs}, real AR={map.ApproachRate}, modifiers={(EnableModifiers ? "on" : "off")}.");
        }

        bool ShouldRender(VamObjectType type)
        {
            switch (type)
            {
                case VamObjectType.Fruit: return RenderFruits;
                case VamObjectType.Droplet: return RenderDroplets;
                case VamObjectType.TinyDroplet: return RenderTinyDroplets;
                case VamObjectType.Banana: return RenderBananas;
                default: return true;
            }
        }
    }
}

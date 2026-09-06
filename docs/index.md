# VAM:SF { style="display:none" }

<figure markdown="span">
  ![VAM:SF](logo-light.png#only-light){ width="600" }
  ![VAM:SF](logo-dark.png#only-dark){ width="600" }
  
  **Variable AR Modification: storyboard framework for osu!catch.**
</figure>

VAM renders a pixel-accurate storyboard copy of an osu!catch map with a dynamic Approach Rate, fake Hidden, mania-style Scroll Velocity, and more for osu! stable client.

<figure markdown="span">
  <a href="https://youtu.be/LUY3zBq9lSM">
    <img src="https://img.youtube.com/vi/LUY3zBq9lSM/maxresdefault.jpg" alt="VAM:SF showcase video" width="600">
  </a>
  <figcaption><a href="https://youtu.be/LUY3zBq9lSM">Watch the showcase on YouTube</a></figcaption>
</figure>

<div class="grid cards" markdown>

- **New here?**
  Start with the [easy install guide](EZ_INSTALL.md) designed for less technical audience.

- **In-depth install:**
  You can also read the [full install guide](INSTALL.md) with manual installation steps and advanced features.

- **Want the details?**
  The [full guide](GUIDE.md) covers every effect setting, the profile syntax, writing mods, and the layer stack.

- **How to use VAM-profile.txt:** A full [profile guide](PROFILE.md) will explain what each command does and how to utilize them.

- **Just want snippets?** The [examples page](EXAMPLES.md) has ready-to-paste profile recipes for AR ramps, Hidden, SV rushes, and mania holds.

- **Something not working?** The [FAQ](FAQ.md) covers the common setup, alignment, and publishing gotchas.

</div>

## What it does

- **Works on any osu!catch map** - it reads the beatmap and regenerates it as a storyboard.
- **Dynamic AR** - change approach rate over time, even past AR 10.
- **Scroll Velocity** - mania-style SV: rush, slow, or freeze the whole field.
- **Fake Hidden + Fade-In** - stack them for a mania-like reading window.
- **One profile file** programs it all, and an installer handles the setup for you.
- **Suite of scripts** to make your life easier.

## Get it

Download the latest release from [GitHub](https://github.com/steelplank/VAM-SF/releases/latest), extract the `VAM-SF` folder into your storybrew project, and run `Install.bat`. The [easy guide](EZ_INSTALL.md) walks through it step by step.

## Support

Found a bug or have a feature idea? Open a [GitHub issue](https://github.com/steelplank/VAM-SF/issues). For "how do I..." questions, ping **@Malai** on Discord.

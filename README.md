<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)"  srcset="docs/logo-dark.png">
    <source media="(prefers-color-scheme: light)" srcset="docs/logo-light.png">
    <img alt="VAM:SF" src="docs/logo-light.png" width="480">
  </picture>
</p>

# VAM:SF - Variable AR Modification: Storybrew Framework (osu!catch)

**Variable AR Modification - a storyboard framework for osu!catch.**

VAM renders a pixel-accurate storyboard copy of an osu!catch map with a fake dynamic Approach Rate, fake Hidden, mania-style Scroll Velocity, and more!

![status](https://img.shields.io/badge/status-alpha%20v0.30-orange)

| Dynamic AR | Scroll Velocity |
|:---:|:---:|
| ![](docs/ar.gif) | ![](docs/sv.gif) |

| Fake Hidden | Skin Support |
|:---:|:---:|
| ![](docs/hd.gif) | ![](docs/skin.gif) |

---

## What it does

VAM is the ultimate answer for mappers and players that want to have more control over a map. osu!catch is rather limited compared to other modes so I've decided to fix it. Originally made for a gimmick tournament (PWD) - turned into a fully-fledged project with a way wider scope.

## How it works

Using storyboard and overlay layer, we can generate gameplay objects identical to osu!catch. By covering majority of the screen, we can hide almost all game elements and replace their function with storyboard elements. Since beatmap is deterministic and player inputs don't affect how fruits are being generated, we can create a pretty convincing illusion for any kind of map.

## Features

- **Installator and helper** - easy install script including modifying the .osu file to make your life easier!
- **Recreation of osu! RNG model** - full recreation of osu!catch droplets and banana randomization!
- **Dynamic AR** - control and change AR during gameplay using keyframes with easing! Exceed normal osu! AR values and torment people with maps on AR 12!
- **Fake Hidden** - add hidden to the mix with adjustable HD strength!
- **Fade-In** - why bother with lane cover when you can have fade-in! Mix FI with HD together and make fake mania-like FL!
- **Scroll Velocity** - mania-style SV: rush, slow, or freeze the whole field!
- **Player Skin Support** - with this enabled, skin elements are dynamically used! Rotation for square skins is fully supported!
- **Miss Simulator** - fake "osu-like" simulation of a miss where the object will fall below the platter. (fruits-only feature)
- **Countdown** - overlay can and will cover osu! built-in countdown so we have our own implementation!
- **Cover with background support** - you should always use cover together with the generator but nobody said it has to be pure black.
- **Map Combo Colors** - an option to use map combo colors. (work in progress)
- **Publish-ready script** - included some quality-of-life scripts that will make mass-edits easy and quick.
- **Custom interpreter** - program keyframes and all efects in one file thanks to a robust interpreter. Now including loop statements!
- **Mod support** - make own mods for the engine and share them with others! Basic template and example mod included, seamless integration with profile file.

## Requirements

- latest osu! stable build
- [storybrew](https://github.com/Damnae/storybrew)

## Install

1. Download the latest **[release](../../releases/latest)** (the `.zip`).
2. Extract the `VAM-SF` folder into your storybrew project root folder.
3. Run **`Install.bat`** and choose **Install**.
4. In storybrew, add the effects and set the cover's OSB layers.

<details>
<summary>Full setup &amp; troubleshooting</summary>

See the full guide: **[INSTALL.md](package/VAM-SF/INSTALL.md)** - scripted and manual install, storybrew setup, managing an install, and publishing.

</details>

## Configuring

Program AR, Hidden, and Scroll Velocity over time in **`VAM-profile.txt`** - the file documents its own syntax.

## Credits

- Phob - provided almost entire beatmap converter <3

## License

VAM:SF is released under the **[GNU General Public License v3.0](LICENSE)**.

```
VAM:SF - Variable AR Modification: Storybrew Framework
Copyright (C) 2026 Malai

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
```

The bundled beatmap converter and the recreated osu! default sprites remain the property of their respective authors.

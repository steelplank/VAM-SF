# VAM-SF - Variable AR Modification: Storybrew Framework (osu!catch)

VAM-SF renders a pixel-accurate storyboard copy of an osu!catch map with a fake dynamic
Approach Rate, fake Hidden, mania-style Scroll Velocity, fake Flashlight, and a redrawn
countdown. The real objects are hidden behind a cover; the storyboard copy is what the
player sees.

## What's new in 2.1

- **Scroll Velocity** - a new core feature. A `[sv]` section in `VAM-profile.txt` sets one
  velocity multiplier over time that reshapes how fast every object falls (rush / freeze /
  creep), while each object still lands on its exact beat. See the `[sv]` docs inside
  `VAM-profile.txt`.
- **Smart cover lead-in** - the cover now automatically comes up before the first object can
  appear, so maps with little or no audio lead-in no longer show early objects uncovered. On
  by default (VAM_Cover -> AutoLeadIn); no setup needed.

This folder is a self-contained toolbox. It holds the framework payload (scripts + sprites),
the installer/manager, and - after installing - your `.osu` backups. Keep it inside your
storybrew project if you want easy upgrades or a clean uninstall later; you can delete the
whole folder once you no longer need those (the installed framework keeps working without it).

## Install

1. Extract this `VAM-SF` folder INSIDE your storybrew project folder (the one with the
   `.sbrew` folder in it).
2. Run the installer:
   - double-click `Install.bat`, or
   - right-click `install.ps1` -> Run with PowerShell.
3. It detects your project and mapset, shows what it found, and gives you a menu. Pick
   **Install**.

The installer copies the effects + `scriptslibrary` into the project, copies the `sb/vam`
sprites into the mapset, backs up every `.osu` into `VAM-SF\backups`, and sets the required
`[General]` flags (`WidescreenStoryboard: 1`, `UseSkinSprites: 1`).

Then, in the storybrew editor:

- Add the effects: `VAM_Generator`, `VAM_Cover`, `VAM_Flashlight`, `VAM_Countdown`.
- On `VAM_Cover`, set the OSB layers: the layer named **Overlay** -> OSB Overlay, the layer
  named **Background** -> OSB Background. Keep the object layer above the cover.
- Program AR and Hidden by editing `VAM-profile.txt` in the project root (one keyframe per
  line: `time:ar[:easing][:before|after][:hd | hd=0..10]`). The file documents its own syntax.
- Optionally add mania-style scroll velocity in the same file under an `[sv]` section (one
  `time:multiplier` per line) to rush, slow, or freeze the whole field while every object still
  lands on its beat. On by default (`VAM_Generator` -> `EnableScrollVelocity`); does nothing
  unless the profile has an `[sv]` section. Fully documented in `VAM-profile.txt`.

## Menu options (once installed)

The installer detects the current state (by scanning the project for the VAM code and the
mapset for the sprites) and offers only what applies:

- **Upgrade / reinstall** - removes the old VAM code, installs the new payload. Keeps your
  `VAM-profile.txt`, keeps your `.osu` files, keeps backups. Use this to move to a new version.
- **Remove scripts only** - removes just the VAM code from the project (leaves the profile,
  sprites and `.osu` alone). A fallback for upgrading across a version that renamed files.
- **Full uninstall** - removes the VAM code, the sprites, and `VAM-profile.txt`, and reverts
  every `.osu` to its original backup.
- **(Re)apply .osu combo mod** - see below.

## Optional .osu combo mod

An optional modification (offered during install, or from the menu afterwards) strips every
new-combo flag from the map and sets the combo colours to two pure-white entries
(`255,255,255`). This makes the underlying (hidden) real objects render uniformly white.
It is fully reversible - the originals are in `VAM-SF\backups`, restored by Full uninstall.

## Command-line use

`install.ps1` also runs non-interactively:

```
install.ps1 -Action install    [-StripCombos] [-NoWidescreenFlag] [-NoSkinFlag] [-Force]
install.ps1 -Action upgrade     [-Force]
install.ps1 -Action remove-scripts [-Force]
install.ps1 -Action uninstall   [-Force]
install.ps1 -Action osu-mod     [-Force]
```

`-MapsetPath` / `-ProjectPath` override the auto-detected folders; `-Force` skips the
confirmation prompt.

## Backups

Backups live in `VAM-SF\backups\<mapset folder name>\`, never in the mapset itself (osu!'s
editor dislikes stray files in the song folder). Each `.osu` is backed up once, on first
modification, so the backup is always the true original.

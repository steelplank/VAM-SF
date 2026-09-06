# Installing VAM:SF - EZ GUIDE FOR FOLKS WHO AREN'T TECHNICAL

I'll drop the professional act and try to make it more accessible. VAM:SF is just a bunch of scripts and installing it is super simple. Think of the whole `VAM-SF` folder like an application - don't modify anything inside and we'll be golden. Let's start from the top:

---

## What you need

- latest version of [**VAM:SF**](https://github.com/steelplank/VAM-SF/releases/latest)
- **osu!** and a mapset with at least one `.osu` difficulty.
- [**storybrew**](https://github.com/Damnae/storybrew/releases/latest), with a created project. Simply open storybrew -> `New project` -> add whatever name and point it to the mapset -> `Start`
- **PC with Windows** - the install script is designed for Windows. I won't make EZ guides for other operating systems.

---

## 1. Download and put the folder in the right place

>Have you made the project inside storybrew? Then you'll have a project folder with all scripts and such. It's in `/storybrew/projects/[name_of_your_project]` or open the project in storybrew and in bottom right corner you have two folder icons. Click on the one that says `Open project folder`.

- Download latest version of [**VAM:SF**](https://github.com/steelplank/VAM-SF/) from GitHub - it's under `Releases`. Pick the latest version and in `Assets` download the VAM-SF-vx.xx.zip file. Please **don't** download Source code if you need this guide.
- Inside the zip you'll have folder named `VAM-SF` <- **this is the important one**. Drag it to the project folder. Or extract it and copy-paste. Up to you what's easier for you, just make sure that the `VAM-SF` folder (not the .zip) is in the project folder.
> You can easily tell that it's your project folder if there's a file inside called `project.sbrew.yaml`
- After moving, open that `VAM-SF` folder and double-click on `Install.bat` to open it. Windows will make sure that you want to run this file - don't worry, just run it.
- You'll see an old-school command prompt window. If you did everything correctly, no errors should appear. Make sure that both `project` and `mapset` have a path next to it. Type **1** inside the window and press enter to install, then type **Y** and press enter again to confirm you want to proceed.
- Done without any errors? Cool, the entire thing was installed and you're ready to go!

---

## 2. Set it up in storybrew

After installing, open the project in storybrew and enter `Effects` tab, then:

- Add effects: **VAM_Generator**, **VAM_Cover** (optionally VAM_Countdown).
- Go to `Layers` tab and arrange everything like this:

| Layers | Type |
|--------|------|
| VAM_Cover (Background) | Background |
| - | Fail |
| - | Pass |
| - | Foreground |
| VAM_Cover (Overlay) | Overlay |
| VAM_Generator (VAM Objects) | Overlay |
| VAM_Countdown (VAM Countdown) | Overlay |

- Edit **`VAM-profile.txt`** (in the project folder) for your AR / Hidden / Fade In / Scroll Velocity adjustments.
- In effects tab, click on the cog next to `VAM_Generator` -> you can tune fields there to taste (constant AR, the effect toggles, etc.).
- Still in effects tab, you can also edit `VAM_Cover` if you want to have your mapset background visible in the storyboard. Simply change `Sprite Path` to `background`, and set `Background Dim` to whatever value you want. 

> Objects **will not render inside storybrew's preview** when `UseSkinSprites` is on - that's expected.
> They render in-game.

---

## 3. Export and publish

When you're done setting up keyframes and other stuff in `VAM-profile.txt` then you should export the storyboard to your mapset. In storybrew, in the bottom-right corner, press on that puzzle icon that says `Export to .osb`. Open osu! and load the set or F5 if osu! was already open. Test the map and see if you like it - you can always go back to storybrew and edit whatever you don't like. Usually I like bouncing back-and-forth like that until I'm certain that everything is exactly the way I want.

---

Done testing and you want to share your wonderful creation online? You can upload it the way it is or you can use our `Quick Publish` script to make it super simple. What this script does is:
1. Sets AR and OD of the diff to 0 (so you cannot cheat and play without storyboard enabled)
2. Removes all nc's (new combo), whitens the combo colors, and adds the VAM tags
3. Brands map background in the bottom left corner with *STORYBOARD ON, BACKGROUND DIM 0%, PLAY WITH HIDDEN*
> If you set cover's `SpritePath` to `background`, point it at the original background filename instead, so the card isn't baked into the cover.
4. Combines .osb with .osu of the selected diff - that's to prevent cheating. It will also delete .osb from the mapset folder.
> Worth mentioning that when submitting a score, osu doesn't check for .osb version and accepts whatever. If you don't bundle .osu and .osb together, then somebody else can just make their own VAM storyboard with no effects, replace it in the folder and cheat the score.

To use that `Quick Publish` script simply go to the `VAM-SF` folder in your project folder, and open `Install.bat` again. Select **Quick publish** (option 2) just like during installation. No errors appear? Cool, everything is ready. You can now upload your map to osu! as you would usually do.

---

## 4. Upgrading to the newest version

New version appeared on GitHub and you want to upgrade? Download the latest version .zip and just replace old `VAM-SF` folder with the new one. Windows will ask you if you want to replace files - simply say yes to all. After that open `Install.bat` and select **Upgrade / reinstall** (option 1). Upgrade is done - as simple as that!

---

## 5. Backups and reverting

Every operation that edits a `.osu` first copies the original into `VAM-SF\backups\<mapset>\`. If you need one file back, copy it out of `backups\` by hand. Or you can open `Install.bat` and select **Revert .osu to originals** (option 7).

---
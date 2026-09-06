# Installing VAM:SF

VAM:SF ships as a **persistent toolbox folder** (`VAM-SF/`) that also acts as its own installer and manager. You keep the folder inside your storybrew project and from it you install, upgrade, uninstall, and publish. This guide covers the scripted install, the manual install, and the publish workflow.

---

## What you need

- latest version of [**VAM:SF**](https://github.com/steelplank/VAM-SF/releases/latest)
- **osu!** and a mapset with at least one `.osu` difficulty to storyboard.
- [**storybrew**](https://github.com/Damnae/storybrew/releases/latest), with a project already pointed at that mapset (the project folder contains `.sbrew`).
- **Windows PowerShell** to run `install.ps1`. The scripted parts use the built-in `System.Drawing`, so no extra installs.

---

## 1. Put the folder in the right place

Copy the whole **`VAM-SF/`** folder *inside* your storybrew project folder - the one that contains the `.sbrew` file. The installer walks up from itself to find `.sbrew`, so it only needs to sit somewhere under the project.

The folder is a toolbox: it holds the payload (scripts + sprites), your `.osu` backups, and the manager, so you can upgrade or uninstall later. If you don't want any of that, feel free to remove the entire folder after installation.

---

## 2. Install (scripted)

Open `install.bat` or right-click `install.ps1` -> **Run with PowerShell**. With no arguments it detects your project and mapset, prints what it found, and shows a menu. Choose **Install** and confirm with **Y**.

Install does the following:

- Copies the framework code into the project (`scripts/` and `scriptslibrary/VAM/`).
- Copies the sprites (`sb/vam/`) into the mapset.
- Backs up every `.osu` into `VAM-SF\backups\`.
- Sets the `.osu` `[General]` flags **`WidescreenStoryboard: 1`** and **`UseSkinSprites: 1`**.

### Command-line (non-interactive)

```powershell
.\install.ps1 -Action install                 # install / upgrade
.\install.ps1 -Action install -WhitenColours   # also whiten combo colours + add tags
.\install.ps1 -Action install -Force           # skip the confirmation prompt
```

Useful overrides: `-MapsetPath "<song folder>"`, `-ProjectPath "<project folder>"`, `-NoWidescreenFlag`, `-NoSkinFlag`.

---

## 2b. Manual install (no script)

If you'd rather not run the script:

1. Copy `scripts/*` (the `VAM_*.cs` files and the `scriptslibrary/VAM/` folder) into your storybrew project.
2. Copy `storyboard/sb/vam/` into your mapset as `sb/vam/`.
3. In each `.osu` `[General]`, set `WidescreenStoryboard: 1` and `UseSkinSprites: 1`.
4. Copy `scripts/VAM-profile.txt` into the project root.
5. In storybrew, add the effects and set the cover's OSB layers (as in step 3 below).

---

## 3. Set it up in storybrew

After installing, open the project in storybrew and enter `Effects` tab, then:

1. Add effects: **VAM_Generator**, **VAM_Cover** (optionally VAM_Countdown).
2. Go to `Layers` tab and arrange everything correctly. **VAM_Cover (Background)** should stay in `Background` layer, **VAM_Cover (Overlay)** and **VAM_Generator (VAM Objects)** need to be in the `Overlay` layer. **Make sure that VAM_Generator is at the very bottom of the list!**
3. Edit **`VAM-profile.txt`** (in the project root) for your AR / Hidden / Fade In / Scroll Velocity keyframes. The file documents its own syntax at the top.
4. In effects tab, click on the cog next to VAM_Generator -> you can tune fields there to taste (constant AR, the effect toggles, etc.).

> Objects **will not render inside storybrew's preview** when `UseSkinSprites` is on - that's expected.
> They render in-game.

---

## 4. Managing an install

When VAM:SF is already installed, `install.bat` shows a grouped menu. The numbers are assigned to whatever options are available at the time, so pick them **by name**:

**INSTALL**

- **Upgrade / reinstall** - refresh code + sprites. Keeps `VAM-profile.txt` and `.osu`.

**PUBLISH** (see step 6)

- **Quick publish** - the one-shot release step.
- **Brand background** - stamp the usage card onto a diff's background.
- **Merge storyboard** - inline the `.osb` into a diff's `.osu`.

**TOOLS**

- **Diagnose setup (doctor)** - read-only check of the whole setup; prints what's wrong and how to fix it. Safe to run any time.
- **Whiten colours** - whiten the combo colours and add the VAM tags (`vam vamsf storyboard`). New combos are left intact (stripping them hurts performance under Hidden). Originals are backed up first. (`-Action osu-mod`)
- **Revert .osu to originals** - restore the `.osu` files from backups. Keeps the VAM code, sprites, and profile.

**REMOVE**

- **Remove scripts** - remove the VAM code only. Keeps profile, sprites, `.osu`.
- **Full uninstall** - remove code + sprites + profile, and revert every `.osu` from backup.

Every option is also available non-interactively via `-Action` (see the list under step 2).

---

## 5. Upgrading to the newest version

If a new update appears on GitHub and you want to use the latest version of the framework, you can simply replace all files inside the toolbox folder (`VAM-SF/`) with the newest build. From there, simply run the installer and select option `1 - Upgrade / reinstall`.

Otherwise, if you performed manual install, simply replace old scripts in the storybrew project folder. Storybrew should detect that the script was modified even during runtime, and it should regenerate the storyboard automatically.

---

## 6. Publishing a diff

When the map is finished, use **Quick publish** (recommended) - the one-shot release step.
On the difficulty you pick it will:

1. Whiten the combo colours and add the VAM tags.
2. Set **AR** and **OD** to **0**.
3. Brand the background: cover-crop it to 16:9 the way osu displays it, stamp the usage card, and save it as `<bg>-vam.jpg`, then point that diff at the branded background.
4. Inline the storyboard: merge the `.osb` into the `.osu`'s `[Events]` (keeps background + breaks, drops the video) so the diff is self-contained.
5. Delete the `.osb`.

Every `.osu` is backed up first. **Run this on the copy you're uploading, not your working project** - storybrew recreates the `.osb` the next time you save, and the other difficulties lose the storyboard.

Requirements and pieces:

- The cover stays clean automatically (its default is a black tile, so gameplay is unaffected and the card only shows in song-select). If you ever set `VAM_Cover`'s `SpritePath` to `background`, point it at the original background filename instead so the card isn't baked into the cover.
- The steps are also available separately: **Brand background** and **Merge storyboard**. Command-line: `-Action publish|brand-bg|merge-sb`, with `-PublishDiff`, `-BrandDiff`, `-MergeDiff` to target a difficulty by name, and `-JpegQuality <1-100>`.

---

## 7. Backups and reverting

Every operation that edits a `.osu` first copies the original into `VAM-SF\backups\<mapset>\`. **Full uninstall** restores those originals. If you only need one file back, copy it out of `backups\` by hand.

---

## Troubleshooting

- **Objects don't show in the storybrew preview** - expected with `UseSkinSprites` on; they render in-game.
- **No `.osb` to merge / publish** - save (export) your storyboard in storybrew first.
- **"System.Drawing isn't available"** - run with Windows PowerShell, not another shell.
- **The installer can't find the project** - keep `VAM-SF/` inside the storybrew project, or pass `-ProjectPath`.
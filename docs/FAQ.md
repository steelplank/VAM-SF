# FAQ

Quick answers to the questions that come up most. If something here doesn't fix it, run the installer's **Diagnose setup (doctor)** first - it catches most setup problems - then reach out (see [Support](#support-and-feedback) at the bottom).

---

## Setup and rendering

??? question "The objects are invisible in storybrew!"
    That's expected when **`UseSkinSprites`** is on - osu!'s skin sprites don't render inside storybrew's preview. Edit your effects with `UseSkinSprites` set to **off**, then flip it back **on** when you export the final storyboard. The objects render correctly in-game either way.

??? question "My storyboard isn't working in-game / the objects don't appear."
    Run the installer's **Diagnose setup (doctor)** option (`install.ps1` -> Tools -> Diagnose, or `-Action doctor`). It checks for the most common issues and prints the fix. The usual culprits are:

    - The `.osu` flags **`WidescreenStoryboard: 1`** and **`UseSkinSprites: 1`** aren't set (re-run Install / Upgrade).
    - **VAM_Generator** isn't at the **very bottom** of the effect list, so the objects draw on top of the cover.
    - The cover's OSB layers are wrong - the top region must be **Overlay**, the bottom region **Background**.

??? question "I added HD / FI / SV keyframes but nothing changes."
    Each feature needs its master toggle on in **VAM_Generator**: **`EnableHidden`** for `hd`, **`EnableFadeIn`** for `fi`, and **`EnableScrollVelocity`** for the `[sv]` section. They're on by default, so if nothing happens, check that one wasn't switched off.

??? question "The installer errors with \"System.Drawing isn't available\", or can't find my project."
    Run it with **Windows PowerShell** - `System.Drawing` isn't available in other shells. And keep the whole **`VAM-SF`** folder **inside your storybrew project** (the folder with the `.sbrew` file), or pass `-ProjectPath "<project folder>"`.

---

## Alignment and visuals

??? question "The objects drift out of line with gameplay / fruits fall past the platter on my monitor."
    Check your resolution's aspect ratio. VAM lines up on **16:9** and **4:3**, but **not on 5:4** (e.g. 1280x1024) or anything narrower than 4:3.

    If you *only* ever play or record on 5:4, you can compensate by hand: set **VAM_Generator**'s **`CenterX`** from `320` to `300` and rebuild. That realigns 5:4, but it then breaks 16:9 and 4:3, so only do this if 5:4 is all you use.

??? question "My fruits look wobbly as they fall."
    Your skin's fruit elements might not be perfectly centered. Each fruit gets a fixed tilt at spawn and never actually spins during the fall, so it's an optical illusion, not motion (osu! stable does the exact same thing). If it bothers you, turn off **Rotate Objects** in the basic settings of the VAM_Generator effect.

---

## Publishing and difficulties

??? question "Storyboard not working for multiple difficulties."
    Storybrew exports the `.osb` either for the entire set or per difficulty. If you want only one difficulty to carry the storyboard, use the **Quick publish** option in the installer to merge the `.osb` into the `.osu` of the selected difficulty.

    Recommended workflow: work on one difficulty, export the `.osb` for the whole set, merge it into that difficulty's `.osu`, then move on to the next difficulty. If you find a smoother workflow, go for it.

---

## Support and feedback

Found a bug or have a feature idea? Open a **[GitHub issue](https://github.com/steelplank/VAM-SF/issues)** - that's the place for anything actionable, and it's how VAM:SF gets better.

For "how do I..." questions, ping me as **@Malai** on Discord (I'm around the catch mapping servers) rather than the tracker. The [easy install guide](EZ_INSTALL.md), the [full guide](GUIDE.md), and the [profile reference](PROFILE.md) already answer most of the common ones.

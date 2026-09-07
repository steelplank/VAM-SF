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

## Performance

Performance is first in priority when it comes to this set of tools. Because of that, I'm always checking impact of the storyboard on performance and have gathered a good collection of best practices:

1. Most of the compute time is spent on sprites functions like move, fade, etc. Thankfully, it's not compute-heavy so an average map will be absolutely fine.
2. At first, we deleted NCs and had entire map played on one combo. This is a problem when played without Hidden in-game as the amount of fruits on platter can tank performance over time. For that reason, we adviced to enable HD and most of the testers were fine with framerate. But after further testing, it seems that even with HD enabled in-game, having one continuous combo is slightly affecting performance. So it might not be the best practice forwards - for now the script to remove NCs is disabled.
3. Turns out that EnableCatchMiss which is utilizing storyboard triggers is tanking performance over time even more than lack of NCs. Long story short, if something has a trigger, then it never "despawns" from the map. An option to have visible misses is super useful though so feel free to enable it on your maps but expect the ms to raise significantly on longer maps. I wouldn't recommend it for tournaments or sets going to ranked/loved section.

> As I've mentioned, performance is extremely important to me. If you have suggestions on how to optimize it even further, or found any performance-affecting bugs, feel free to report them as soon as possible. Even if you don't know the exact cause, simply let me know that something is happening.

---

## Support and feedback

Found a bug or have a feature idea? Open a **[GitHub issue](https://github.com/steelplank/VAM-SF/issues)** - that's the place for anything actionable, and it's how VAM:SF gets better.

For "how do I..." questions, ping me as **@Malai** on Discord (I'm around the catch mapping servers) rather than the tracker. The [easy install guide](EZ_INSTALL.md), the [full guide](GUIDE.md), and the [profile reference](PROFILE.md) already answer most of the common ones.

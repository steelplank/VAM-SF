<#
  VAM-SF - Variable AR Modification: Storybrew Framework (osu!catch) - Installer / Manager (v2.1)

  Keep this whole VAM-SF folder INSIDE your storybrew project folder. It is a PERSISTENT
  toolbox: after installing, the folder stays so you can upgrade or uninstall later. It holds
  the payload (scripts + sprites), your .osu backups, and this manager. You can delete the
  whole folder once you no longer want upgrade/uninstall support - the installed framework in
  the project keeps working.

  Run it: right-click install.ps1 -> Run with PowerShell (or run install.ps1 from a terminal).
  With no -Action it detects the current state and shows a menu.

  Actions (also selectable from the menu):
    install     copy scripts into the project + sprites into the mapset, patch .osu flags,
                optionally strip new-combo/whiten colours. If already installed it upgrades in
                place (see 'upgrade').
    upgrade     remove the old VAM code files, then install the new ones. Keeps VAM-profile.txt,
                keeps .osu files, keeps backups. Use when moving to a new version.
    remove-scripts   remove only the VAM code from the project (keep VAM-profile, sprites and
                     .osu untouched). Fallback when a version renamed files.
    uninstall   full removal: VAM code + sprites + VAM-profile, and revert every .osu to its
                original backup.
    osu-mod     (re)apply the optional .osu modification (strip new-combo + white colours + add the
                VAM tags: vam vamsf storyboard) to an existing install.
    brand-bg    bake the usage card onto a copy of a diff's background and repoint that .osu to it
                (song-select branding). Cover-crops the original to 16:9 the same way osu displays
                it, stamps assets\usage-card.png on top, saves <bg>-vam.jpg, and leaves the original
                background + a .osu backup untouched.
    merge-sb    inline the storyboard (.osb) into a diff's .osu so it's self-contained (a publish
                step). Keeps the .osu background + breaks, DROPS the video, appends the .osb's
                storyboard, and DELETES the .osb so it isn't loaded twice. Run it on the COPY you're
                shipping - storybrew re-creates the .osb on its next save.
    publish     the one-shot release step for a finished diff (RECOMMENDED). On the chosen diff it:
                strips new-combo + whitens colours + adds the VAM tags, sets AR & OD to 0, brands the
                background with the usage card, inlines the storyboard, and deletes the .osb. Every
                .osu is backed up first. Best run on the COPY you upload.

  Parameters:
    -Action <install|upgrade|remove-scripts|uninstall|osu-mod|brand-bg|merge-sb|publish>   run non-interactively
    -Force              skip the confirmation prompt
    -MapsetPath <path>  override the auto-detected mapset (song) folder
    -ProjectPath <path> override the auto-detected storybrew project folder
    -StripCombos        (install/osu-mod) apply the .osu combo strip + white colours
    -NoWidescreenFlag   do NOT set WidescreenStoryboard: 1
    -NoSkinFlag         do NOT set UseSkinSprites: 1
    -CardPath <path>    (brand-bg) the usage-card PNG; default assets\usage-card.png. Author it on a
                        full 1920x1080 transparent frame so it stamps 1:1.
    -BrandDiff <text>   (brand-bg) only brand .osu whose filename contains this (else you're asked)
    -CardX -CardY <n>   (brand-bg) card offset in the 1920x1080 space if it's NOT a full-frame export
    -JpegQuality <n>    (brand-bg) exported background JPEG quality 1-100 (default 90)
    -MergeDiff <text>   (merge-sb) only merge into .osu whose filename contains this (else you're asked)
    -PublishDiff <text> (publish) the diff to publish, by filename substring (else you're asked)
#>
[CmdletBinding()]
param(
    [ValidateSet('install','upgrade','remove-scripts','uninstall','osu-mod','brand-bg','merge-sb','publish')]
    [string]$Action,
    [switch]$Force,
    [string]$MapsetPath,
    [string]$ProjectPath,
    [switch]$StripCombos,
    [switch]$NoWidescreenFlag,
    [switch]$NoSkinFlag,
    [string]$CardPath,      # brand-bg: the usage-card PNG (full 1920x1080 transparent frame)
    [string]$BrandDiff,     # brand-bg: only .osu whose name contains this (else you're asked)
    [int]$CardX = 0,        # brand-bg: card offset if it's NOT a full-frame export
    [int]$CardY = 0,
    [ValidateRange(1,100)]
    [int]$JpegQuality = 90, # brand-bg: JPEG quality of the exported background (1-100)
    [string]$MergeDiff,     # merge-sb: only merge into .osu whose name contains this (else you're asked)
    [string]$PublishDiff    # publish: the diff to publish (name substring; else you're asked)
)

$ErrorActionPreference = 'Stop'

$EffectFiles  = @('VAM_Generator.cs','VAM_Cover.cs','VAM_Countdown.cs')
$LibFolders   = @('scriptslibrary/VAM')
$ProfileFile  = 'VAM-profile.txt'
$SpriteRel    = 'sb/vam'
$VamTags      = @('vam','vamsf','storyboard')   # added to [Metadata] Tags with the combo mod / publish

function Info($m){ Write-Host "  $m" }
function Good($m){ Write-Host "  [OK] $m" -ForegroundColor Green }
function Warn($m){ Write-Host "  [!]  $m" -ForegroundColor Yellow }
function Step($m){ Write-Host "" ; Write-Host $m -ForegroundColor Cyan }
function Die ($m){ Write-Host "`n[X] $m" -ForegroundColor Red; Write-Host "Aborted - no destructive step was completed." -ForegroundColor Red; exit 1 }

# ---------- banner ----------
function Show-Banner {
    $art = @(
        '____   _________      _____          ____________________',
        '\   \ /   /  _  \    /     \   /\   /   _____/\_   _____/',
        ' \   Y   /  /_\  \  /  \ /  \  \/   \_____  \  |    __)  ',
        '  \     /    |    \/    Y    \ /\   /        \ |     \   ',
        '   \___/\____|__  /\____|__  / \/  /_______  / \___  /   ',
        '                \/         \/              \/      \/    '
    )

    Write-Host ""
    foreach ($l in $art){ Write-Host $l -ForegroundColor Cyan }
    Write-Host ""
    Write-Host " Variable AR Modification: Storybrew Framework" -ForegroundColor DarkCyan
    Write-Host " installer / manager   v0.21" -ForegroundColor DarkGray
    Write-Host " -------------------------------------------------" -ForegroundColor DarkCyan
}

# ---------- file helpers (bracket-safe: mapset folders like '... [no video]') ----------
function Copy-Merge([string]$src, [string]$dst){
    if (-not (Test-Path -LiteralPath $src)) { return }
    $srcFull = (Resolve-Path -LiteralPath $src).Path
    [void][System.IO.Directory]::CreateDirectory($dst)
    Get-ChildItem -LiteralPath $srcFull -Recurse | ForEach-Object {
        $rel = $_.FullName.Substring($srcFull.Length).TrimStart([char]92, [char]47)
        $target = Join-Path $dst $rel
        if ($_.PSIsContainer) {
            [void][System.IO.Directory]::CreateDirectory($target)
        } else {
            [void][System.IO.Directory]::CreateDirectory((Split-Path $target -Parent))
            [System.IO.File]::Copy($_.FullName, $target, $true)
        }
    }
}

function Remove-PathSafe([string]$p){
    if (Test-Path -LiteralPath $p){ Remove-Item -LiteralPath $p -Recurse -Force -ErrorAction SilentlyContinue }
}

function Set-OsuFlag([System.Collections.Generic.List[string]]$lines, [string]$key, [string]$value){
    $genStart = -1
    for ($i = 0; $i -lt $lines.Count; $i++){ if ($lines[$i].Trim() -eq '[General]'){ $genStart = $i; break } }
    if ($genStart -lt 0){
        $lines.Insert(0, "")
        $lines.Insert(0, "${key}: ${value}")
        $lines.Insert(0, "[General]")
        return
    }
    $genEnd = $lines.Count
    for ($i = $genStart + 1; $i -lt $lines.Count; $i++){ if ($lines[$i].Trim() -match '^\[.+\]$'){ $genEnd = $i; break } }
    for ($i = $genStart + 1; $i -lt $genEnd; $i++){
        if ($lines[$i] -match ('^\s*' + [regex]::Escape($key) + '\s*:')){ $lines[$i] = "${key}: ${value}"; return }
    }
    $lines.Insert($genStart + 1, "${key}: ${value}")
}

function Get-Section([System.Collections.Generic.List[string]]$lines, [string]$name){
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++){ if ($lines[$i].Trim() -eq $name){ $start = $i; break } }
    if ($start -lt 0){ return @{ Start = -1; End = -1 } }
    $end = $lines.Count
    for ($i = $start + 1; $i -lt $lines.Count; $i++){ if ($lines[$i].Trim() -match '^\[.+\]$'){ $end = $i; break } }
    return @{ Start = $start; End = $end }
}

# Strip new-combo (bit 4) + colour-skip bits (0x70) from every hit object, and force
# [Colours] to exactly two pure-white combos. Reversible: caller backs up the original first.
function Edit-OsuStripCombos([System.Collections.Generic.List[string]]$lines){
    # 1) hit objects: keep only circle(1)/slider(2)/spinner(8) type bits.
    $ho = Get-Section $lines '[HitObjects]'
    if ($ho.Start -ge 0){
        for ($i = $ho.Start + 1; $i -lt $ho.End; $i++){
            $line = $lines[$i]
            if ($line.Trim().Length -eq 0){ continue }
            $f = $line.Split(',')
            if ($f.Count -lt 5){ continue }
            $t = 0
            if (-not [int]::TryParse($f[3], [ref]$t)){ continue }
            $f[3] = ([string]($t -band 0x0B))
            $lines[$i] = ($f -join ',')
        }
    }

    # 2) [Colours]: drop existing ComboN lines, keep any slider overrides, set two white combos.
    $white1 = 'Combo1 : 255,255,255'
    $white2 = 'Combo2 : 255,255,255'
    $col = Get-Section $lines '[Colours]'
    if ($col.Start -ge 0){
        $kept = New-Object System.Collections.Generic.List[string]
        for ($i = $col.Start + 1; $i -lt $col.End; $i++){
            if ($lines[$i] -notmatch '^\s*Combo\s*\d+\s*:'){ $kept.Add($lines[$i]) }
        }
        # remove old body
        for ($i = $col.End - 1; $i -gt $col.Start; $i--){ $lines.RemoveAt($i) }
        # insert new body after the header
        $ins = $col.Start + 1
        $lines.Insert($ins, $white1); $ins++
        $lines.Insert($ins, $white2); $ins++
        foreach ($k in $kept){ $lines.Insert($ins, $k); $ins++ }
    } else {
        # no [Colours]: add one just before [HitObjects] (or at the end).
        $ho2 = Get-Section $lines '[HitObjects]'
        $at = if ($ho2.Start -ge 0){ $ho2.Start } else { $lines.Count }
        $block = @('[Colours]', $white1, $white2, '')
        for ($k = $block.Count - 1; $k -ge 0; $k--){ $lines.Insert($at, $block[$k]) }
    }
}

# Append tags to [Metadata] Tags (space-separated), skipping any already present (case-insensitive).
function Add-OsuTags([System.Collections.Generic.List[string]]$lines, [string[]]$tags){
    $m = Get-Section $lines '[Metadata]'
    if ($m.Start -lt 0){ return }
    $tagIdx = -1
    for ($i = $m.Start + 1; $i -lt $m.End; $i++){ if ($lines[$i] -match '^\s*Tags\s*:'){ $tagIdx = $i; break } }
    if ($tagIdx -lt 0){ $lines.Insert($m.Start + 1, ('Tags:' + ($tags -join ' '))); return }
    $cur = ($lines[$tagIdx] -replace '^\s*Tags\s*:\s*', '')
    $existing = @($cur -split '\s+' | Where-Object { $_ -ne '' })
    $lower = @($existing | ForEach-Object { $_.ToLowerInvariant() })
    $add = @($tags | Where-Object { $lower -notcontains $_.ToLowerInvariant() })
    if ($add.Count -eq 0){ return }
    $lines[$tagIdx] = 'Tags:' + (@($existing + $add) -join ' ')
}

# Read the [Events] background line (0,0,"file",..). Returns @{ Name=..; Index=.. } or $null.
function Get-OsuBackground([System.Collections.Generic.List[string]]$lines){
    $ev = Get-Section $lines '[Events]'
    if ($ev.Start -lt 0){ return $null }
    for ($i = $ev.Start + 1; $i -lt $ev.End; $i++){
        if ($lines[$i].Trim() -match '^(0|Background)\s*,\s*0\s*,\s*"([^"]+)"'){ return @{ Name = $Matches[2]; Index = $i } }
    }
    return $null
}
# Swap only the quoted filename on the background line (keeps any x,y offsets).
function Set-OsuBackground([System.Collections.Generic.List[string]]$lines, [int]$index, [string]$newName){
    $lines[$index] = $lines[$index] -replace '"([^"]+)"', ('"' + $newName + '"')
}

function Read-Lines([string]$path){
    $raw = [System.IO.File]::ReadAllText($path)
    $list = [System.Collections.Generic.List[string]]::new()
    foreach ($l in ($raw -split "`r?`n")){ $list.Add($l) }
    return ,$list
}
function Write-Lines([string]$path, [System.Collections.Generic.List[string]]$lines){
    [System.IO.File]::WriteAllText($path, ($lines -join "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
}

# ============================================================================
$Here = $PSScriptRoot
if (-not $Here){ $Here = Split-Path -Parent $MyInvocation.MyCommand.Path }
$BackupRoot = Join-Path $Here 'backups'
$StateFile  = Join-Path $Here '.vam-state.json'

Show-Banner

# --- locate the storybrew project ---
if (-not $ProjectPath){
    $dir = $Here
    for ($i = 0; $i -lt 8 -and $dir; $i++){
        if (Test-Path -LiteralPath (Join-Path $dir '.sbrew')){ $ProjectPath = $dir; break }
        $parent = Split-Path -Parent $dir
        if (-not $parent -or $parent -eq $dir){ break }
        $dir = $parent
    }
}
if (-not $ProjectPath -or -not (Test-Path -LiteralPath $ProjectPath)){
    Die "Couldn't find a storybrew project (.sbrew) above this folder.`n    Keep VAM-SF INSIDE your storybrew project, or pass -ProjectPath."
}
Good "project: $ProjectPath"

# --- resolve the mapset folder ---
if (-not $MapsetPath){
    $userYaml = Join-Path $ProjectPath '.sbrew/user.yaml'
    if (Test-Path -LiteralPath $userYaml){
        $line = Get-Content -LiteralPath $userYaml | Where-Object { $_ -match '^\s*MapsetPath\s*:' } | Select-Object -First 1
        if ($line -match '^\s*MapsetPath\s*:\s*"?(.+?)"?\s*$'){ $MapsetPath = $Matches[1] }
    }
}
$mapsetOk = $MapsetPath -and (Test-Path -LiteralPath $MapsetPath)
if ($mapsetOk){ Good "mapset:  $MapsetPath" } else { Warn "mapset not resolved yet (will ask if an action needs it)" }

# --- detect current state by scanning ---
$installedEffects = @($EffectFiles | Where-Object { Test-Path -LiteralPath (Join-Path $ProjectPath $_) })
$isInstalled = $installedEffects.Count -gt 0 -or (Test-Path -LiteralPath (Join-Path $ProjectPath 'scriptslibrary/VAM'))
$sbInstalled = $mapsetOk -and (Test-Path -LiteralPath (Join-Path $MapsetPath $SpriteRel))
$profileHere = Test-Path -LiteralPath (Join-Path $ProjectPath $ProfileFile)
$backupCount = 0
if (Test-Path -LiteralPath $BackupRoot){ $backupCount = @(Get-ChildItem -LiteralPath $BackupRoot -Recurse -Filter *.osu -File -ErrorAction SilentlyContinue).Count }

Write-Host ""
Write-Host "Detected:" -ForegroundColor Cyan
Info ("- VAM code in project : " + $(if($isInstalled){"YES ($($installedEffects.Count) effect files)"}else{"no"}))
Info ("- sprites in mapset    : " + $(if($sbInstalled){"YES"}else{"no"}))
Info ("- VAM-profile.txt       : " + $(if($profileHere){"present"}else{"absent"}))
Info ("- .osu backups kept     : " + $(if($backupCount){"$backupCount file(s)"}else{"none"}))

# ---------- payload check ----------
$ScriptsSrc = Join-Path $Here 'scripts'
$SbSrc      = Join-Path $Here 'storyboard/sb'
function Assert-Payload {
    if (-not (Test-Path -LiteralPath (Join-Path $ScriptsSrc 'VAM_Generator.cs'))){ Die "Payload missing: scripts\VAM_Generator.cs next to this script." }
    if (-not (Test-Path -LiteralPath (Join-Path $SbSrc 'vam'))){ Die "Payload missing: storyboard\sb\vam next to this script." }
}
function Need-Mapset {
    if (-not $script:mapsetOk){
        if (-not $script:MapsetPath){ $script:MapsetPath = Read-Host "  Enter the full path to your mapset (song) folder" }
        $script:mapsetOk = $script:MapsetPath -and (Test-Path -LiteralPath $script:MapsetPath)
    }
    if (-not $script:mapsetOk){ Die "Mapset folder not found: $script:MapsetPath" }
}

# ---------- actions ----------
function Backup-Osus([array]$osuFiles){
    $dst = Join-Path $BackupRoot ([System.IO.Path]::GetFileName($MapsetPath))
    [void][System.IO.Directory]::CreateDirectory($dst)
    foreach ($osu in $osuFiles){
        $bak = Join-Path $dst $osu.Name
        if (-not [System.IO.File]::Exists($bak)){ [System.IO.File]::Copy($osu.FullName, $bak, $false) }
    }
    return $dst
}

function Patch-Osus([array]$osuFiles, [bool]$doFlags, [bool]$doCombos){
    foreach ($osu in $osuFiles){
        $lines = Read-Lines $osu.FullName
        if ($doFlags){
            if (-not $NoWidescreenFlag){ Set-OsuFlag $lines 'WidescreenStoryboard' '1' }
            if (-not $NoSkinFlag){ Set-OsuFlag $lines 'UseSkinSprites' '1' }
        }
        if ($doCombos){ Edit-OsuStripCombos $lines; Add-OsuTags $lines $VamTags }
        Write-Lines $osu.FullName $lines
    }
}

function Remove-VamCode {
    foreach ($f in $EffectFiles){ Remove-PathSafe (Join-Path $ProjectPath $f) }
    foreach ($d in $LibFolders){ Remove-PathSafe (Join-Path $ProjectPath $d) }
    # tidy an empty scriptslibrary if we emptied it
    $sl = Join-Path $ProjectPath 'scriptslibrary'
    if ((Test-Path -LiteralPath $sl) -and -not (Get-ChildItem -LiteralPath $sl -Force -ErrorAction SilentlyContinue)){ Remove-PathSafe $sl }
}

function Do-InstallCore([bool]$isUpgrade){
    Assert-Payload
    Need-Mapset
    $osuFiles = @(Get-ChildItem -LiteralPath $MapsetPath -Filter *.osu -File)
    if ($osuFiles.Count -eq 0){ Die "No .osu files in the mapset:`n    $MapsetPath" }

    if ($isUpgrade){ Step "Upgrading (removing old VAM code first)..."; Remove-VamCode }

    Step "Installing framework code -> project"
    $keepProfile = Test-Path -LiteralPath (Join-Path $ProjectPath $ProfileFile)
    Get-ChildItem -LiteralPath $ScriptsSrc -Force | ForEach-Object {
        if ($_.Name -eq $ProfileFile -and $keepProfile){ return }
        if ($_.PSIsContainer){ Copy-Merge $_.FullName (Join-Path $ProjectPath $_.Name) }
        else { [System.IO.File]::Copy($_.FullName, (Join-Path $ProjectPath $_.Name), $true) }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectPath 'VAM_Generator.cs'))){ Die "Copy check failed: VAM_Generator.cs not in project." }
    if (-not (Test-Path -LiteralPath (Join-Path $ProjectPath 'scriptslibrary/VAM/CtbLoader/OsuV14BeatmapDeserializer.cs'))){ Die "Copy check failed: map loader not copied." }
    Good "code installed$(if($keepProfile){' (kept your existing VAM-profile.txt)'})"

    Step "Installing sprites -> mapset"
    Copy-Merge $SbSrc (Join-Path $MapsetPath 'sb')
    if (-not (Test-Path -LiteralPath (Join-Path $MapsetPath 'sb/vam/fruit-apple.png'))){ Die "Copy check failed: sb\vam sprites not in the mapset." }
    Good "sprites installed"

    Step "Backing up .osu files (into this folder\backups)"
    $bdst = Backup-Osus $osuFiles
    Good "$($osuFiles.Count) original .osu saved -> $bdst"

    Step "Patching .osu files"
    Patch-Osus $osuFiles $true $StripCombos.IsPresent
    Good ("flags set" + $(if($StripCombos){"; new-combo stripped + colours whitened"}else{""}))

    # state file
    $state = @{
        version   = '0.21'
        project   = $ProjectPath
        mapset    = $MapsetPath
        osu       = @($osuFiles | ForEach-Object { $_.Name })
        combosMod = $StripCombos.IsPresent
    }
    ($state | ConvertTo-Json) | Set-Content -LiteralPath $StateFile -Encoding UTF8

    Step "Done."
    Info "In storybrew: add effects VAM_Generator, VAM_Cover, VAM_Countdown;"
    Info "set the cover's OSB layers (Overlay -> Overlay, Background -> Background);"
    Info "edit VAM-profile.txt for AR/HD keyframes."
    Info "This VAM-SF folder can stay for future upgrades/uninstall, or be deleted."
}

function Do-RemoveScripts {
    Step "Removing VAM code (keeping VAM-profile, sprites and .osu)..."
    Remove-VamCode
    Good "VAM code removed. VAM-profile.txt, sprites and .osu were left untouched."
}

function Do-Uninstall {
    Step "Full uninstall"
    Remove-VamCode
    Remove-PathSafe (Join-Path $ProjectPath $ProfileFile)
    Good "removed VAM code + VAM-profile.txt"

    if ($mapsetOk){
        Remove-PathSafe (Join-Path $MapsetPath $SpriteRel)
        Good "removed sprites (sb\vam) from the mapset"
    }

    if (Test-Path -LiteralPath $BackupRoot){
        $reverted = 0; $missing = 0
        Get-ChildItem -LiteralPath $BackupRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            $mapDir = $null
            if ($mapsetOk -and ([System.IO.Path]::GetFileName($MapsetPath) -eq $_.Name)){ $mapDir = $MapsetPath }
            if (-not $mapDir){ return }
            Get-ChildItem -LiteralPath $_.FullName -Filter *.osu -File | ForEach-Object {
                $dest = Join-Path $mapDir $_.Name
                if (Test-Path -LiteralPath $dest){ [System.IO.File]::Copy($_.FullName, $dest, $true); $reverted++ }
                else { $missing++ }
            }
        }
        Good "reverted $reverted .osu file(s) to their originals"
        if ($missing){ Warn "$missing backup(s) had no matching .osu in the mapset (skipped)" }
    } else {
        Warn "No backups found - .osu files were NOT reverted (WidescreenStoryboard/UseSkinSprites/colour edits remain)."
    }
    Remove-PathSafe $StateFile
    Step "Uninstalled. You can delete this VAM-SF folder now."
}

function Do-OsuMod {
    Need-Mapset
    $osuFiles = @(Get-ChildItem -LiteralPath $MapsetPath -Filter *.osu -File)
    if ($osuFiles.Count -eq 0){ Die "No .osu files in the mapset." }
    Step "Applying .osu combo mod (strip new-combo + white colours + tags)"
    Backup-Osus $osuFiles | Out-Null
    Patch-Osus $osuFiles $false $true
    Good "$($osuFiles.Count) .osu modified: new-combo stripped, colours whitened, tags added ($($VamTags -join ', ')). Originals safe in backups."
}

# ---- shared publish primitives ----

# Pick target .osu(s): -filter matches by name substring; else prompt. $allowAll adds an [A] option.
function Select-Diffs($osuFiles, $filter, $prompt, [bool]$allowAll){
    if ($filter){
        $m = @($osuFiles | Where-Object { $_.Name -like "*$filter*" })
        if ($m.Count -eq 0){ Die "No .osu name contained '$filter'." }
        return ,$m
    }
    Write-Host ""
    Write-Host "  $prompt" -ForegroundColor Cyan
    for ($i = 0; $i -lt $osuFiles.Count; $i++){ Info ("[{0}] {1}" -f ($i + 1), $osuFiles[$i].Name) }
    if ($allowAll){ Info "[A] all of them" }
    $sel = Read-Host "  >"
    if ($allowAll -and $sel -match '^(a|all)$'){ return ,$osuFiles }
    if ($sel -match '^\d+$' -and [int]$sel -ge 1 -and [int]$sel -le $osuFiles.Count){ return ,@($osuFiles[[int]$sel - 1]) }
    Die "Nothing selected."
}

# Set a key in [Difficulty] (osu writes these as 'Key:value', no space).
function Set-OsuDifficulty([System.Collections.Generic.List[string]]$lines, [string]$key, [string]$value){
    $d = Get-Section $lines '[Difficulty]'
    if ($d.Start -lt 0){ return }
    for ($i = $d.Start + 1; $i -lt $d.End; $i++){
        if ($lines[$i] -match ('^\s*' + [regex]::Escape($key) + '\s*:')){ $lines[$i] = "${key}:${value}"; return }
    }
    $lines.Insert($d.Start + 1, "${key}:${value}")
}

# Read the .osb storyboard body: everything after '[Events]' (+ the '//Background and Video events'
# comment); stray bg/video/break lines are dropped (those belong to the .osu).
function Get-OsbBody($osbFile){
    $osbLines = Read-Lines $osbFile.FullName
    $startBody = 0
    for ($i = 0; $i -lt $osbLines.Count; $i++){ if ($osbLines[$i].Trim() -eq '[Events]'){ $startBody = $i + 1; break } }
    if ($startBody -lt $osbLines.Count -and $osbLines[$startBody].Trim() -eq '//Background and Video events'){ $startBody++ }
    $body = New-Object System.Collections.Generic.List[string]
    for ($i = $startBody; $i -lt $osbLines.Count; $i++){
        $t = $osbLines[$i].Trim()
        if ($t.Length -gt 0 -and -not $t.StartsWith('//')){
            $f0 = ($t -split ',')[0].Trim()
            if ($f0 -match '^(0|1|2|Background|Video|Break)$'){ continue }
        }
        $body.Add($osbLines[$i])
    }
    while ($body.Count -gt 0 -and $body[$body.Count - 1].Trim() -eq ''){ $body.RemoveAt($body.Count - 1) }
    return ,$body
}

# Bake the branded background for ONE .osu: cover-crop original -> 16:9, stamp the card, repoint.
function Invoke-BrandOsu($osu, $card, $jpegCodec, $encParams){
    $lines = Read-Lines $osu.FullName
    $bg = Get-OsuBackground $lines
    if (-not $bg){ Warn "$($osu.Name): no background line - skipped."; return $false }
    if ($bg.Name -match '-vam\.(png|jpg)$'){ Warn "$($osu.Name): already branded ('$($bg.Name)') - skipped."; return $false }
    $origBg = Join-Path $MapsetPath $bg.Name
    if (-not (Test-Path -LiteralPath $origBg)){ Warn "$($osu.Name): background '$($bg.Name)' not found - skipped."; return $false }

    $outName = [System.IO.Path]::GetFileNameWithoutExtension($bg.Name) + '-vam.jpg'
    $outPath = Join-Path $MapsetPath $outName
    $cw = 1920; $ch = 1080
    $src = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $origBg).Path)
    try {
        $scale = [Math]::Max($cw / [double]$src.Width, $ch / [double]$src.Height)
        $dw = [int][Math]::Ceiling($src.Width * $scale)
        $dh = [int][Math]::Ceiling($src.Height * $scale)
        $ox = [int](($cw - $dw) / 2); $oy = [int](($ch - $dh) / 2)
        $canvas = New-Object System.Drawing.Bitmap($cw, $ch)
        $g = [System.Drawing.Graphics]::FromImage($canvas)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.DrawImage($src, $ox, $oy, $dw, $dh)
        $g.DrawImage($card, [int]$CardX, [int]$CardY, $card.Width, $card.Height)
        $g.Dispose()
        $canvas.Save($outPath, $jpegCodec, $encParams)
        $canvas.Dispose()
    } finally { $src.Dispose() }
    Set-OsuBackground $lines $bg.Index $outName
    Write-Lines $osu.FullName $lines
    Good "$($osu.Name): background -> $outName  (original '$($bg.Name)' kept)"
    return $true
}

# Inline a storyboard body into ONE .osu: keep bg + breaks, DROP video, append the storyboard.
function Invoke-MergeOsu($osu, $osbBody){
    $lines = Read-Lines $osu.FullName
    $ev = Get-Section $lines '[Events]'
    if ($ev.Start -lt 0){ Warn "$($osu.Name): no [Events] section - skipped."; return $false }
    $bg = $null
    $breaks = New-Object System.Collections.Generic.List[string]
    for ($i = $ev.Start + 1; $i -lt $ev.End; $i++){
        $t = $lines[$i].Trim()
        if ($t.Length -eq 0 -or $t.StartsWith('//')){ continue }
        $first = ($t -split ',')[0].Trim()
        if ($first -match '^(0|Background)$'){ if (-not $bg){ $bg = $t } }
        elseif ($first -match '^(2|Break)$'){ $breaks.Add($t) }
    }
    $body = New-Object System.Collections.Generic.List[string]
    $body.Add('//Background and Video events')
    if ($bg){ $body.Add($bg) }
    if ($breaks.Count -gt 0){ $body.Add('//Break Periods'); foreach ($b in $breaks){ $body.Add($b) } }
    foreach ($l in $osbBody){ $body.Add($l) }
    for ($i = $ev.End - 1; $i -gt $ev.Start; $i--){ $lines.RemoveAt($i) }
    $ins = $ev.Start + 1
    foreach ($l in $body){ $lines.Insert($ins, $l); $ins++ }
    Write-Lines $osu.FullName $lines
    Good "$($osu.Name): storyboard inlined ($($osbBody.Count) line(s)); video dropped, bg + $($breaks.Count) break(s) kept"
    return $true
}

function Do-BrandBackground {
    Need-Mapset

    if (-not $CardPath){ $CardPath = Join-Path $Here 'assets/usage-card.png' }
    if (-not (Test-Path -LiteralPath $CardPath)){
        Die ("Usage-card image not found:`n    {0}`n    Export your card as a full 1920x1080 transparent PNG and drop it there, or pass -CardPath." -f $CardPath)
    }
    try { Add-Type -AssemblyName System.Drawing -ErrorAction Stop }
    catch { Die "System.Drawing isn't available. Run this with Windows PowerShell (right-click install.ps1 -> Run with PowerShell)." }

    $osuFiles = @(Get-ChildItem -LiteralPath $MapsetPath -Filter *.osu -File)
    if ($osuFiles.Count -eq 0){ Die "No .osu files in the mapset." }

    $targets = Select-Diffs $osuFiles $BrandDiff "Which difficulty's background should carry the usage card?" $true

    Step "Backing up .osu files (into this folder\backups)"
    Backup-Osus $osuFiles | Out-Null

    Step "Branding background(s)"
    $jpegCodec  = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.FormatID -eq [System.Drawing.Imaging.ImageFormat]::Jpeg.Guid } | Select-Object -First 1
    $encParams  = New-Object System.Drawing.Imaging.EncoderParameters(1)
    $encParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter([System.Drawing.Imaging.Encoder]::Quality, [int64]$JpegQuality)
    $card = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $CardPath).Path)
    try { foreach ($osu in $targets){ [void](Invoke-BrandOsu $osu $card $jpegCodec $encParams) } } finally { $card.Dispose() }

    Step "Done."
    Info "The branded background shows in SONG SELECT (and briefly on load)."
    Warn "Cover note: the default cover is a black tile, so GAMEPLAY is unaffected. Only if your"
    Info "VAM_Cover 'SpritePath' is set to 'background' should you change it to the ORIGINAL background"
    Info "filename (kept in the mapset), so the usage card never bakes into the cover."
    Info "Originals are safe in this folder\backups; 'uninstall' reverts every .osu."
}

function Do-MergeStoryboard {
    Need-Mapset
    $osuFiles = @(Get-ChildItem -LiteralPath $MapsetPath -Filter *.osu -File)
    if ($osuFiles.Count -eq 0){ Die "No .osu files in the mapset." }
    $osbFiles = @(Get-ChildItem -LiteralPath $MapsetPath -Filter *.osb -File)
    if ($osbFiles.Count -eq 0){ Die "No .osb in the mapset - nothing to merge. Save/export your storyboard first." }
    $osbFile = $osbFiles[0]
    if ($osbFiles.Count -gt 1){ Warn "Multiple .osb found; using '$($osbFile.Name)'." }

    $targets = Select-Diffs $osuFiles $MergeDiff "Which difficulty should carry the inline storyboard?" $true

    Step "Backing up .osu files (into this folder\backups)"
    Backup-Osus $osuFiles | Out-Null

    $osbBody = Get-OsbBody $osbFile
    if ($osbBody.Count -eq 0){ Die "The .osb had no storyboard content after its header." }

    Step "Inlining storyboard -> .osu"
    foreach ($osu in $targets){ [void](Invoke-MergeOsu $osu $osbBody) }

    Step "Deleting the .osb"
    Remove-Item -LiteralPath $osbFile.FullName -Force
    Good "deleted '$($osbFile.Name)' (storybrew recreates it in seconds on the next save)"

    Step "Done."
    Warn "PUBLISH step: the .osb is gone, so OTHER diffs lose the storyboard. Run this on the COPY you're"
    Info "shipping - your working storybrew project still has everything and rebuilds the .osb on save."
    Info "The .osu originals are safe in this folder\backups; 'uninstall' reverts them."
}

function Do-QuickPublish {
    Need-Mapset
    if (-not $CardPath){ $CardPath = Join-Path $Here 'assets/usage-card.png' }
    if (-not (Test-Path -LiteralPath $CardPath)){ Die ("Usage-card image not found:`n    {0}`n    Export it as a full 1920x1080 transparent PNG, or pass -CardPath." -f $CardPath) }
    try { Add-Type -AssemblyName System.Drawing -ErrorAction Stop } catch { Die "System.Drawing isn't available. Run with Windows PowerShell (right-click -> Run with PowerShell)." }
    $osuFiles = @(Get-ChildItem -LiteralPath $MapsetPath -Filter *.osu -File)
    if ($osuFiles.Count -eq 0){ Die "No .osu files in the mapset." }
    $osbFiles = @(Get-ChildItem -LiteralPath $MapsetPath -Filter *.osb -File)
    if ($osbFiles.Count -eq 0){ Die "No .osb in the mapset - save/export your storyboard first." }
    $osbFile = $osbFiles[0]
    if ($osbFiles.Count -gt 1){ Warn "Multiple .osb found; using '$($osbFile.Name)'." }

    $filter = if ($PublishDiff){ $PublishDiff } else { $BrandDiff }
    $target = (Select-Diffs $osuFiles $filter "Which difficulty are you publishing?" $false)[0]

    Step "Backing up .osu files (into this folder\backups)"
    Backup-Osus $osuFiles | Out-Null

    Step "1/3  Combos + colours + difficulty + tags"
    $lines = Read-Lines $target.FullName
    Edit-OsuStripCombos $lines
    Add-OsuTags $lines $VamTags
    Set-OsuDifficulty $lines 'ApproachRate' '0'
    Set-OsuDifficulty $lines 'OverallDifficulty' '0'
    Write-Lines $target.FullName $lines
    Good "$($target.Name): new-combo stripped, colours whitened, AR & OD set to 0, tags added ($($VamTags -join ', '))"

    Step "2/3  Brand background"
    $jpegCodec  = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.FormatID -eq [System.Drawing.Imaging.ImageFormat]::Jpeg.Guid } | Select-Object -First 1
    $encParams  = New-Object System.Drawing.Imaging.EncoderParameters(1)
    $encParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter([System.Drawing.Imaging.Encoder]::Quality, [int64]$JpegQuality)
    $card = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $CardPath).Path)
    try { [void](Invoke-BrandOsu $target $card $jpegCodec $encParams) } finally { $card.Dispose() }

    Step "3/3  Inline storyboard + delete .osb"
    $osbBody = Get-OsbBody $osbFile
    if ($osbBody.Count -eq 0){ Die "The .osb had no storyboard content after its header." }
    [void](Invoke-MergeOsu $target $osbBody)
    Remove-Item -LiteralPath $osbFile.FullName -Force
    Good "storyboard inlined; '$($osbFile.Name)' deleted (storybrew recreates it on save)"

    Step "Published '$($target.Name)'."
    Warn "This edited your working mapset and deleted the .osb (storybrew rebuilds it on the next save)."
    Info "Ideally run this on the COPY you upload. Originals are in this folder\backups; 'uninstall' reverts the .osu."
}

function Confirm-Or-Exit($summary){
    if ($Force){ return }
    Write-Host ""
    Write-Host $summary -ForegroundColor Yellow
    $ans = Read-Host "Proceed? [Y/N]"
    if ($ans -notmatch '^(y|yes)$'){ Write-Host "Cancelled - nothing was changed." -ForegroundColor Yellow; exit 0 }
}

if (-not $Action){
    Write-Host ""
    Write-Host "Choose an action:" -ForegroundColor Cyan
    if (-not $isInstalled){
        Info "[1] Install VAM-SF"
    } else {
        Info "[1] Upgrade / reinstall (refresh code + sprites; keep VAM-profile and .osu)"
        Info "[2] Remove scripts only (keep VAM-profile, sprites, .osu) - upgrade fallback"
        Info "[3] Full uninstall (remove everything + revert .osu to originals)"
        Info "[4] (Re)apply .osu combo mod (strip new-combo + white colours + tags)"
        Info "[5] Quick publish  (combo strip + AR/OD 0 + brand background + inline .osb) - recommended"
        Info "[6] Brand a diff's background with the usage card only"
        Info "[7] Merge storyboard (.osb) into a diff's .osu only"
    }
    Info "[0] Exit"
    $sel = Read-Host "  >"
    switch ($sel){
        '1' { $Action = if ($isInstalled){ 'upgrade' } else { 'install' } }
        '2' { if ($isInstalled){ $Action = 'remove-scripts' } }
        '3' { if ($isInstalled){ $Action = 'uninstall' } }
        '4' { if ($isInstalled){ $Action = 'osu-mod'; $StripCombos = [switch]$true } }
        '5' { if ($isInstalled){ $Action = 'publish' } }
        '6' { if ($isInstalled){ $Action = 'brand-bg' } }
        '7' { if ($isInstalled){ $Action = 'merge-sb' } }
        default { Write-Host "Bye." ; exit 0 }
    }
    if (-not $Action){ Write-Host "Bye." ; exit 0 }
}

switch ($Action){
    'install'        { if ($isInstalled){ Confirm-Or-Exit "About to UPGRADE the existing install (code refreshed; profile + .osu kept)."; Do-InstallCore $true } else { Confirm-Or-Exit "About to INSTALL VAM-SF into the project and mapset."; Do-InstallCore $false } }
    'upgrade'        { Confirm-Or-Exit "About to UPGRADE: remove old VAM code, install the new payload. VAM-profile and .osu are kept."; Do-InstallCore $true }
    'remove-scripts' { Confirm-Or-Exit "About to REMOVE the VAM code only. VAM-profile, sprites and .osu are kept."; Do-RemoveScripts }
    'uninstall'      { Confirm-Or-Exit "FULL UNINSTALL: removes VAM code + sprites + VAM-profile and REVERTS every .osu to its backup."; Do-Uninstall }
    'osu-mod'        { Confirm-Or-Exit "About to modify .osu files (strip new-combo + white colours + add tags: $($VamTags -join ', ')). Originals are backed up."; Do-OsuMod }
    'brand-bg'       { Confirm-Or-Exit "About to bake the usage card onto a copy of the diff's background and repoint that .osu. Original background + .osu backup are kept."; Do-BrandBackground }
    'merge-sb'       { Confirm-Or-Exit "PUBLISH: inline the .osb storyboard into the chosen .osu (drops video, keeps bg+breaks) and DELETE the .osb. Do this on a shipping COPY - storybrew recreates the .osb on save."; Do-MergeStoryboard }
    'publish'        { Confirm-Or-Exit "QUICK PUBLISH one diff: strip new-combo + white colours, set AR & OD to 0, brand the background with the usage card, inline the storyboard and DELETE the .osb. Backs up every .osu first; best run on the COPY you upload."; Do-QuickPublish }
}

Write-Host ""

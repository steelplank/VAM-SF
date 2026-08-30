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
    osu-mod     (re)apply the optional .osu modification (strip new-combo + white colours) to an
                existing install.

  Parameters:
    -Action <install|upgrade|remove-scripts|uninstall|osu-mod>   run non-interactively
    -Force              skip the confirmation prompt
    -MapsetPath <path>  override the auto-detected mapset (song) folder
    -ProjectPath <path> override the auto-detected storybrew project folder
    -StripCombos        (install/osu-mod) apply the .osu combo strip + white colours
    -NoWidescreenFlag   do NOT set WidescreenStoryboard: 1
    -NoSkinFlag         do NOT set UseSkinSprites: 1
#>
[CmdletBinding()]
param(
    [ValidateSet('install','upgrade','remove-scripts','uninstall','osu-mod')]
    [string]$Action,
    [switch]$Force,
    [string]$MapsetPath,
    [string]$ProjectPath,
    [switch]$StripCombos,
    [switch]$NoWidescreenFlag,
    [switch]$NoSkinFlag
)

$ErrorActionPreference = 'Stop'

$EffectFiles  = @('VAM_Generator.cs','VAM_Cover.cs','VAM_Countdown.cs')
$LibFolders   = @('scriptslibrary/VAM')
$ProfileFile  = 'VAM-profile.txt'
$SpriteRel    = 'sb/vam'

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
        if ($doCombos){ Edit-OsuStripCombos $lines }
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
    Step "Applying .osu combo mod (strip new-combo + white colours)"
    Backup-Osus $osuFiles | Out-Null
    Patch-Osus $osuFiles $false $true
    Good "$($osuFiles.Count) .osu modified (originals safe in backups)."
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
        Info "[4] (Re)apply .osu combo mod (strip new-combo + white colours)"
    }
    Info "[0] Exit"
    $sel = Read-Host "  >"
    switch ($sel){
        '1' { $Action = if ($isInstalled){ 'upgrade' } else { 'install' } }
        '2' { if ($isInstalled){ $Action = 'remove-scripts' } }
        '3' { if ($isInstalled){ $Action = 'uninstall' } }
        '4' { if ($isInstalled){ $Action = 'osu-mod'; $StripCombos = [switch]$true } }
        default { Write-Host "Bye." ; exit 0 }
    }
    if (-not $Action){ Write-Host "Bye." ; exit 0 }
}

switch ($Action){
    'install'        { if ($isInstalled){ Confirm-Or-Exit "About to UPGRADE the existing install (code refreshed; profile + .osu kept)."; Do-InstallCore $true } else { Confirm-Or-Exit "About to INSTALL VAM-SF into the project and mapset."; Do-InstallCore $false } }
    'upgrade'        { Confirm-Or-Exit "About to UPGRADE: remove old VAM code, install the new payload. VAM-profile and .osu are kept."; Do-InstallCore $true }
    'remove-scripts' { Confirm-Or-Exit "About to REMOVE the VAM code only. VAM-profile, sprites and .osu are kept."; Do-RemoveScripts }
    'uninstall'      { Confirm-Or-Exit "FULL UNINSTALL: removes VAM code + sprites + VAM-profile and REVERTS every .osu to its backup."; Do-Uninstall }
    'osu-mod'        { Confirm-Or-Exit "About to modify .osu files (strip new-combo + white colours). Originals are backed up."; Do-OsuMod }
}

Write-Host ""

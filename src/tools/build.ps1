param(
  [ValidateSet('emu','lan')][string]$Mode = 'emu',
  [string]$LanIp = '192.168.0.104',
  [ValidateSet('on','off')][string]$Kikyo = 'on',
  [ValidateSet('on','off')][string]$GrantKikyo = 'off',
  [ValidateSet('on','off')][string]$Install = 'on'
)
# One command: switch mode -> build server dll -> (optional) client patches -> repack+sign -> install
# NOTE: keep this file ASCII-only. Windows PowerShell 5.1 misreads UTF-8 with no BOM and
#       full-width punctuation breaks parsing (missing string terminator).
$ErrorActionPreference = 'Stop'
$WORK   = 'F:\BaiduNetdiskDownload\_inuyasha_work'
$CS     = "$WORK\srv_build\InuyashaLocalServer\C.cs"
$DOTNET = "$WORK\tools\dotnet\dotnet.exe"
$PY     = 'C:\Users\Administrator\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\python\python.exe'
$ADB    = 'D:\leidian\LDPlayer64\adb.exe'
$SEVEN  = "$WORK\tools\7zr.exe"

$env:DOTNET_ROOT    = "$WORK\tools\dotnet"
$env:DOTNET_CLI_HOME= "$WORK\tools\dotnethome"
$env:NUGET_PACKAGES = "$WORK\tools\dotnethome\.nuget\packages"
$env:DOTNET_NOLOGO  = '1'
$env:PYTHONUTF8     = '1'

# ---- 1) switch LAN/emu mode + Kikyo grant flag
$text = [System.IO.File]::ReadAllText($CS, [System.Text.Encoding]::UTF8)
$flag = if ($Mode -eq 'lan') { 'true' } else { 'false' }
$kf   = if ($GrantKikyo -eq 'on') { 'true' } else { 'false' }
$text = [regex]::Replace($text, 'public const string LanIp = "[^"]*";', ('public const string LanIp = "' + $LanIp + '";'))
$text = [regex]::Replace($text, 'public const bool LanMode = (true|false);', ('public const bool LanMode = ' + $flag + ';'))
$text = [regex]::Replace($text, 'public const bool GrantKikyoByDefault = (true|false);', ('public const bool GrantKikyoByDefault = ' + $kf + ';'))
[System.IO.File]::WriteAllText($CS, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "[1/6] mode=$Mode LanIp=$LanIp LanMode=$flag Kikyo=$Kikyo GrantKikyo=$GrantKikyo"

# ---- 2) server dll
Write-Host '[2/6] building InuyashaLocalServer ...'
$dll = "$WORK\srv_build\out\Release\net35\InuyashaLocalServer.dll"
if (Test-Path $dll) { Remove-Item $dll -Force }
$out = & $DOTNET build "$WORK\srv_build\InuyashaLocalServer.csproj" -c Release -p:BaseOutputPath="$WORK\srv_build\out\\" -v:m 2>&1
$errs = $out | Select-String -Pattern 'error CS|error MSB'
if ($errs) { $errs | ForEach-Object { '      ' + $_.Line.Trim() }; throw 'BUILD FAILED - not packaging a stale dll' }
if (-not (Test-Path $dll)) { throw 'build produced no dll' }
Write-Host ('      ok: ' + [math]::Round((Get-Item $dll).Length/1KB) + ' KB')

$env:WITH_KIKYO = if ($Kikyo -eq 'on') { '1' } else { '0' }

if ($Kikyo -eq 'on') {
  # ---- 3) client patch: drop the "must have network" guard
  Write-Host '[3/6] patching client dll (remove network requirement) ...'
  $pc = "$WORK\patch_client"
  $xsrc = "$WORK\verify_patched\device_managed\XEngineBase.dll"
  $xdst = "$pc\XEngineBase.patched.dll"
  if (Test-Path $xdst) { Remove-Item $xdst -Force }
  $pcOut = & $DOTNET "$pc\out\Release\net8.0\PatchClient.dll" $xsrc $xdst 2>&1
  $pcOut | ForEach-Object { '      ' + $_ }
  if (-not (Test-Path $xdst)) { throw 'client dll patch failed' }

  # ---- 4) client patch: add Kikyo (hero 105) to the bin tables, then repack Bin.7z
  Write-Host '[4/6] patching client bins (add Kikyo hero 105) ...'
  $bw = "$WORK\binwork"
  if (-not (Test-Path "$bw\Bin\Bin\heroAttr.bin")) {
    Remove-Item "$bw\Bin" -Recurse -Force -ErrorAction SilentlyContinue
    & $SEVEN x "$bw\Bin.orig.7z" "-o$bw\Bin" -y | Out-Null
  }
  & $PY "$WORK\patch_bins_kikyo.py" "$bw\Bin\Bin" | ForEach-Object { '      ' + $_ }
  & $PY "$WORK\fix_kikyo_intro.py" "$bw\Bin\Bin" | ForEach-Object { '      ' + $_ }
  if (Test-Path "$bw\Bin.patched.7z") { Remove-Item "$bw\Bin.patched.7z" -Force }
  Push-Location "$bw\Bin"
  & $SEVEN a -t7z "$bw\Bin.patched.7z" "Bin" -mx=9 -y | Out-Null
  Pop-Location
  if (-not (Test-Path "$bw\Bin.patched.7z")) { throw 'Bin.7z repack failed' }
  Write-Host ('      Bin.patched.7z ' + [math]::Round((Get-Item "$bw\Bin.patched.7z").Length/1KB) + ' KB')
} else {
  Write-Host '[3/6] Kikyo off - skipping client patches'
  Write-Host '[4/6] (skipped)'
}

# ---- 5) repack + sign
Write-Host '[5/6] repacking + signing APK ...'
$tag = if ($Kikyo -eq 'on') { $Mode + '_kikyo' } else { $Mode }
$env:APK_TAG = $tag
& $PY "$WORK\repack_patched.py" 2>&1 | Select-Object -Last 8 | ForEach-Object { '      ' + $_ }

# ---- 6) install
if ($Install -eq 'on' -and $Mode -eq 'emu') {
  Write-Host '[6/6] installing to emulator ...'
  $apk = Get-ChildItem "$WORK\full_apk" -Filter ("*_" + $tag + ".apk") | Select-Object -First 1
  & $ADB -s emulator-5554 install -r $apk.FullName
  & $ADB -s emulator-5554 shell "am force-stop com.heitao.qyc.hta"

  # The client unpacks Bin.7z / *.7z into /data/data/<pkg>/files/AssetBundles ONCE (first run)
  # and never re-extracts afterwards; with no network it will not re-extract either, so an
  # APK-only overwrite leaves the OLD config there (Kikyo missing, or HeroData NRE at load).
  # Reliable approach: restore a known-good extracted tree, then overlay the patched files.
  # (Overlaying preserves the app's ownership; do NOT use tar-extract without fixing the uid.)
  Write-Host '[6/6] updating the extracted asset tree ...'
  $baseline = "$WORK\save_backup\good_state.tgz"
  if (Test-Path $baseline) {
    & $ADB -s emulator-5554 push $baseline /data/local/tmp/good_state.tgz | Out-Null
    & $ADB -s emulator-5554 shell "cd /data/data/com.heitao.qyc.hta && tar xzf /data/local/tmp/good_state.tgz"
    & $ADB -s emulator-5554 shell "u=`$(stat -c %u /data/data/com.heitao.qyc.hta); chown -R `$u /data/data/com.heitao.qyc.hta/files; restorecon -R /data/data/com.heitao.qyc.hta 2>/dev/null" 2>$null
  } else {
    Write-Host '      WARN: no baseline extract snapshot; game may need to re-extract assets.'
  }
  if ($Kikyo -eq 'on') {
    $D = '/data/data/com.heitao.qyc.hta/files/AssetBundles/Android'
    foreach ($f in 'heroAttr.bin','getHero.bin','CharmLevel.bin') {
      & $ADB -s emulator-5554 push "$WORK\binwork\chk\Bin\$f" "$D/Bin/$f" | Out-Null
    }
    & $ADB -s emulator-5554 push "$pc\XEngineBase.patched.dll" "$D/dll/XEngineBase.dll" | Out-Null
    & $ADB -s emulator-5554 shell "u=`$(stat -c %u /data/data/com.heitao.qyc.hta); chown -R `$u /data/data/com.heitao.qyc.hta/files; restorecon -R /data/data/com.heitao.qyc.hta 2>/dev/null" 2>$null
    Write-Host '      overlaid patched heroAttr/getHero/CharmLevel + XEngineBase'
  }
  Write-Host '      installed.'
} else {
  Write-Host '[6/6] apk ready'
}
Write-Host 'DONE.'

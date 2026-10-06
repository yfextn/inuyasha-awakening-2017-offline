# -*- coding: utf-8 -*-
"""把重新编译好的 InuyashaLocalServer.dll 塞回设备上的那个 APK，并重新签名。

只用 F: 盘做中间文件（C: 盘只剩 2GB，不能拿来当临时目录）。
"""
import os
import subprocess
import sys
import zipfile

WORK = r'F:\BaiduNetdiskDownload\_inuyasha_work'
SRC_APK = os.path.join(WORK, 'verify_patched', 'device_base.apk')
NEW_DLL = os.path.join(WORK, 'srv_build', 'out', 'Release', 'net35', 'InuyashaLocalServer.dll')
OUT_DIR = os.path.join(WORK, 'patched_apk')
UNSIGNED = os.path.join(OUT_DIR, 'patched_unsigned.apk')
ALIGNED = os.path.join(OUT_DIR, 'patched_aligned.apk')
SIGNED = os.path.join(WORK, 'full_apk',
                      '犬夜叉_单机版_LAN测试_' + (os.environ.get('APK_TAG') or 'lan') + '.apk')

TARGET = 'assets/bin/Data/Managed/InuyashaLocalServer.dll'
TARGET_XENGINE = 'assets/bin/Data/Managed/XEngineBase.dll'
TARGET_BIN7Z = 'assets/Zip/Android/Bin.7z'
PATCHED_XENGINE = os.path.join(WORK, 'patch_client', 'XEngineBase.patched.dll')
PATCHED_BIN7Z = os.path.join(WORK, 'binwork', 'Bin.patched.7z')

JAVA = r'D:\java\bin\java.exe'
APKSIGNER = r'D:\APKtool\tool\apksigner.jar'
ZIPALIGN = r'D:\APKtool\tool\zipalign.exe'
KEYSTORE = r'F:\BaiduNetdiskDownload\_inuyasha_work\inu_stable.keystore'
KEY_ALIAS = 'inu'

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # apk_repack.py 就在同目录
from apk_repack import repack  # noqa: E402

os.makedirs(OUT_DIR, exist_ok=True)


def run(cmd):
    p = subprocess.run(cmd, capture_output=True, text=True)
    print('rc=%d' % p.returncode)
    if p.stdout.strip():
        print(p.stdout.strip()[:2000])
    if p.stderr.strip():
        print('STDERR:', p.stderr.strip()[:2000])
    return p.returncode


def main():
    with_kikyo = os.environ.get('WITH_KIKYO', '1') == '1'

    print('src apk  :', SRC_APK, os.path.getsize(SRC_APK))
    print('new dll  :', NEW_DLL, os.path.getsize(NEW_DLL))

    reps = {TARGET: NEW_DLL}

    # 客户端补丁1：去掉「没网就不让进」的判断（XEngineBase.UpgradeManager.VersionCheck）
    if with_kikyo:
        if not os.path.isfile(PATCHED_XENGINE):
            raise SystemExit('缺少 %s，先跑 patch_client' % PATCHED_XENGINE)
        reps[TARGET_XENGINE] = PATCHED_XENGINE
        print('client dll:', PATCHED_XENGINE, os.path.getsize(PATCHED_XENGINE))

    # 客户端补丁2：把桔梗（英雄 105）写进配置表 heroAttr/getHero/CharmLevel
    if with_kikyo:
        if not os.path.isfile(PATCHED_BIN7Z):
            raise SystemExit('缺少 %s，先跑 patch_bins_kikyo.py + 7zr' % PATCHED_BIN7Z)
        reps[TARGET_BIN7Z] = PATCHED_BIN7Z
        print('bin7z     :', PATCHED_BIN7Z, os.path.getsize(PATCHED_BIN7Z))

    total, replaced, missing = repack(SRC_APK, UNSIGNED, reps)
    print('repack entries=%d replaced=%s missing=%s' % (total, replaced, missing))
    if missing:
        raise SystemExit('目标条目没找到，放弃')

    print('zipalign ...')
    if run([ZIPALIGN, '-f', '-p', '4', UNSIGNED, ALIGNED]) != 0:
        raise SystemExit('zipalign 失败')

    print('sign ...')
    rc = run([JAVA, '-jar', APKSIGNER, 'sign',
              '--ks', KEYSTORE, '--ks-pass', 'pass:android',
              '--key-pass', 'pass:android', '--ks-key-alias', KEY_ALIAS,
              '--v1-signing-enabled', 'true', '--v2-signing-enabled', 'true',
              '--min-sdk-version', '8', '--out', SIGNED, ALIGNED])
    if rc != 0:
        raise SystemExit('签名失败')

    print('verify ...')
    run([JAVA, '-jar', APKSIGNER, 'verify', '--min-sdk-version', '8', SIGNED])

    z = zipfile.ZipFile(SIGNED)
    info = z.getinfo(TARGET)
    print('signed apk :', SIGNED, os.path.getsize(SIGNED))
    print('dll in apk :', info.file_size)
    assert info.file_size == os.path.getsize(NEW_DLL), 'APK 里的 dll 大小不对'
    if with_kikyo:
        print('xengine   :', z.getinfo(TARGET_XENGINE).file_size)
        print('bin7z     :', z.getinfo(TARGET_BIN7Z).file_size)
    print('OK')


if __name__ == '__main__':
    main()

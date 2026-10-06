# -*- coding: utf-8 -*-
"""pack.py <tag>  — 把最新编译的 InuyashaLocalServer.dll 打回 APK 并重新签名。

  python pack.py v6   ->  full_apk/patched_v6.apk

基包 = verify_patched/device_base.apk（与 v3/v4/v5 同源，只替换
InuyashaLocalServer.dll / XEngineBase.dll / Bin.7z 三个条目）。
"""
import hashlib
import os
import subprocess
import sys
import zipfile

WORK = r'F:\BaiduNetdiskDownload\_inuyasha_work'
SRC_APK = os.path.join(WORK, 'verify_patched', 'device_base.apk')
NEW_DLL = os.path.join(WORK, 'srv_build', 'out', 'Release', 'net35', 'InuyashaLocalServer.dll')
TARGET = 'assets/bin/Data/Managed/InuyashaLocalServer.dll'
TARGET_XENGINE = 'assets/bin/Data/Managed/XEngineBase.dll'
TARGET_BIN7Z = 'assets/Zip/Android/Bin.7z'
PATCHED_XENGINE = os.path.join(WORK, 'patch_client', 'XEngineBase.patched.dll')
PATCHED_BIN7Z = os.path.join(WORK, 'binwork', 'Bin.patched.7z')

JAVA = r'D:\java\bin\java.exe'
APKSIGNER = r'D:\APKtool\tool\apksigner.jar'
ZIPALIGN = r'D:\APKtool\tool\zipalign.exe'
KEYSTORE = os.path.join(WORK, 'inu_stable.keystore')
KEY_ALIAS = 'inu'

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # apk_repack.py 就在同目录
from apk_repack import repack  # noqa: E402


def run(cmd):
    p = subprocess.run(cmd, capture_output=True, text=True)
    print('rc=%d' % p.returncode)
    if p.stdout.strip():
        print(p.stdout.strip()[:3000])
    if p.stderr.strip():
        print('STDERR:', p.stderr.strip()[:3000])
    return p.returncode


def main():
    tag = sys.argv[1] if len(sys.argv) > 1 else 'v6'
    out_dir = os.path.join(WORK, 'patched_apk')
    unsigned = os.path.join(out_dir, 'patched_%s_unsigned.apk' % tag)
    aligned = os.path.join(out_dir, 'patched_%s_aligned.apk' % tag)
    signed = os.path.join(WORK, 'full_apk', 'patched_%s.apk' % tag)
    os.makedirs(out_dir, exist_ok=True)

    reps = {TARGET: NEW_DLL, TARGET_XENGINE: PATCHED_XENGINE, TARGET_BIN7Z: PATCHED_BIN7Z}
    need = hashlib.md5(open(NEW_DLL, 'rb').read()).hexdigest()
    print('tag=%s  dll=%d md5=%s' % (tag, os.path.getsize(NEW_DLL), need[:12]))

    for f in (unsigned, aligned, signed):
        if os.path.exists(f):
            os.remove(f)

    total, replaced, missing = repack(SRC_APK, unsigned, reps)
    print('repack entries=%d replaced=%s missing=%s' % (total, replaced, missing))
    if missing:
        raise SystemExit('target entries not found, abort')

    if run([ZIPALIGN, '-f', '-p', '4', unsigned, aligned]) != 0:
        raise SystemExit('zipalign failed')

    rc = run([JAVA, '-jar', APKSIGNER, 'sign',
              '--ks', KEYSTORE, '--ks-pass', 'pass:android',
              '--key-pass', 'pass:android', '--ks-key-alias', KEY_ALIAS,
              '--v1-signing-enabled', 'true', '--v2-signing-enabled', 'true',
              '--min-sdk-version', '8', '--out', signed, aligned])
    if rc != 0:
        raise SystemExit('sign failed')
    run([JAVA, '-jar', APKSIGNER, 'verify', '--min-sdk-version', '8', signed])

    z = zipfile.ZipFile(signed)
    got = hashlib.md5(z.read(TARGET)).hexdigest()
    print('signed apk :', signed, os.path.getsize(signed))
    print('dll in apk : size=%d md5=%s' % (z.getinfo(TARGET).file_size, got[:12]))
    assert got == need, 'dll in apk does not match build output'
    print('entries    :', len(z.namelist()))
    print('OK')


if __name__ == '__main__':
    main()

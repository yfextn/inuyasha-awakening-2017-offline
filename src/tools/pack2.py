# -*- coding: utf-8 -*-
"""pack2.py <tag> —— 在 pack.py 的基础上支持**新增** APK 条目。

桔梗(105)缺的这些资源包里根本没有对应文件，只能拿别人的贴图/语音顶上去：
    image/skill/10501..10505.tex        ← 日暮篱 image/skill/10201..10205.tex
    image/fate/icon10501..10505.tex     ← 日暮篱 icon10201..10205.tex
    image/fate/name10501..10505.tex     ← 日暮篱 name10201..10205.tex
    sounds/fighting/jiegeng.audio       ← 日暮篱 gewei.audio
（角色语音文件名 = 小写的 heroAttr.actorName，桔梗是 Jiegeng，所以要 jiegeng.audio）

用法: python pack2.py v18
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

BASE = 'assets/AssetBundles/Android/'
STORED_EXT = ('.so', '.arsc')


def build_additions():
    """返回 {apk内路径: 要从基包哪条目复制}。"""
    src_of = {}
    for i in range(1, 6):
        src_of[BASE + 'image/skill/105%02d.tex' % i] = BASE + 'image/skill/102%02d.tex' % i
        src_of[BASE + 'image/fate/icon105%02d.tex' % i] = BASE + 'image/fate/icon102%02d.tex' % i
        src_of[BASE + 'image/fate/name105%02d.tex' % i] = BASE + 'image/fate/name102%02d.tex' % i
    src_of[BASE + 'sounds/fighting/jiegeng.audio'] = BASE + 'sounds/fighting/gewei.audio'
    return src_of


def repack(src, dst, replacements, additions):
    zin = zipfile.ZipFile(src, 'r')
    names = set(zin.namelist())
    missing = [s for s in additions.values() if s not in names]
    if missing:
        raise SystemExit('基包里没有这些源条目: %s' % missing)
    zout = zipfile.ZipFile(dst, 'w', zipfile.ZIP_DEFLATED, allowZip64=True)
    replaced = set()
    n = 0
    for info in zin.infolist():
        if info.filename in replacements:
            data = open(replacements[info.filename], 'rb').read()
            replaced.add(info.filename)
        else:
            data = zin.read(info.filename)
        zi = zipfile.ZipInfo(info.filename, date_time=info.date_time)
        zi.external_attr = info.external_attr
        zi.internal_attr = info.internal_attr
        zi.create_system = info.create_system
        zi.compress_type = (zipfile.ZIP_STORED if info.filename.lower().endswith(STORED_EXT)
                            else zipfile.ZIP_DEFLATED)
        zout.writestr(zi, data)
        n += 1
    for name, srcname in additions.items():
        zi = zipfile.ZipInfo(name, date_time=(2017, 6, 20, 0, 0, 0))
        zi.compress_type = zipfile.ZIP_DEFLATED
        zi.external_attr = 0o100644 << 16
        zout.writestr(zi, zin.read(srcname))
        n += 1
        print('  + %s  <=  %s' % (name.replace(BASE, ''), srcname.replace(BASE, '')))
    zout.close()
    zin.close()
    return n, sorted(replaced), sorted(set(replacements) - replaced)


def run(cmd):
    p = subprocess.run(cmd, capture_output=True, text=True)
    print('rc=%d' % p.returncode)
    if p.stdout.strip():
        print(p.stdout.strip()[:2000])
    if p.stderr.strip():
        print('STDERR:', p.stderr.strip()[:2000])
    return p.returncode


def main():
    tag = sys.argv[1] if len(sys.argv) > 1 else 'v18'
    out_dir = os.path.join(WORK, 'patched_apk')
    unsigned = os.path.join(out_dir, 'patched_%s_unsigned.apk' % tag)
    aligned = os.path.join(out_dir, 'patched_%s_aligned.apk' % tag)
    signed = os.path.join(WORK, 'full_apk', 'patched_%s.apk' % tag)
    os.makedirs(out_dir, exist_ok=True)

    reps = {TARGET: NEW_DLL, TARGET_XENGINE: PATCHED_XENGINE, TARGET_BIN7Z: PATCHED_BIN7Z}
    add = build_additions()
    need = hashlib.md5(open(NEW_DLL, 'rb').read()).hexdigest()
    print('tag=%s dll=%d md5=%s' % (tag, os.path.getsize(NEW_DLL), need[:12]))

    for f in (unsigned, aligned, signed):
        if os.path.exists(f):
            os.remove(f)

    total, replaced, missing = repack(SRC_APK, unsigned, reps, add)
    print('repack entries=%d replaced=%s missing=%s' % (total, replaced, missing))
    if missing:
        raise SystemExit('目标条目没找到，放弃')

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
    assert got == need
    for name in add:
        assert name in z.namelist(), name
    print('entries    :', len(z.namelist()))
    print('OK')


if __name__ == '__main__':
    main()

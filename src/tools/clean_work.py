# -*- coding: utf-8 -*-
"""精简工作目录：删掉可再生的中间产物，保留能重新编译/打包的最小集合。

保留清单（少一个就没法重新打包 APK）：
    verify_patched/device_base.apk        基包
    patch_client/XEngineBase.patched.dll  客户端补丁
    binwork/Bin.patched.7z                打好的 Bin 配置包
    binwork/Bin/Bin, binwork/chk/Bin      配置表源 + 推设备用的副本
    srv_build/                            服务端源码与嵌入表
    tools/                                dotnet 编译工具链
    _bin_probe/Bin                       补丁脚本要读的原始 bin
    decompiled/                          客户端反编译参考（8MB）
用法: python clean_work.py [--apply]
"""
import os
import shutil
import sys

W = r'F:\BaiduNetdiskDownload\_inuyasha_work'

# 整个目录删
DEL_DIRS = [
    r'ref_apk', r'apk_extracted', r'compilers', r'save_backup', r'shots2',
    r'emu_bins', r'image_unpacked', r'ref_managed', r'ref_decomp', r'il_tools',
    r'cecil_patch', r'srv_probe', r'pkcs7test', r'dex_decode', r'ref_dll_check',
    r'ref_decomp_acs', r'extracted7z', r'bin7z_check', r'patched_apk',
    r'apkpatcher', r'__pycache__',
]
# 大文件删
DEL_FILES = [
    r'hta_data_backup.tgz', r'compilers.zip', r'apktool.jar',
    r'classes.dex.patched_sdk', r'classes.dex.patched_sdk2', r'classes.dex.patched_sdk3',
    r'check_dll.dll', r'_v3_acs.dll', r'Inuyasha.dll.patched',
    r'Assembly-CSharp.dll.patched', r'rebuild_out.txt',
]
DEL_GLOBS = [
    (r'dsh-session-session-', '.zip'),
]


def dirsize(p):
    t = 0
    for root, _, files in os.walk(p):
        for f in files:
            try:
                t += os.path.getsize(os.path.join(root, f))
            except OSError:
                pass
    return t


def main():
    apply = '--apply' in sys.argv
    total = 0
    plans = []

    for d in DEL_DIRS:
        p = os.path.join(W, d)
        if os.path.isdir(p):
            s = dirsize(p)
            plans.append(('DIR ', p, s))
            total += s
    for f in DEL_FILES:
        p = os.path.join(W, f)
        if os.path.isfile(p):
            s = os.path.getsize(p)
            plans.append(('FILE', p, s))
            total += s
    for pref, ext in DEL_GLOBS:
        for n in os.listdir(W):
            if n.startswith(pref) and n.endswith(ext):
                p = os.path.join(W, n)
                if os.path.isfile(p):
                    s = os.path.getsize(p)
                    plans.append(('FILE', p, s))
                    total += s

    # full_apk：除最终 v22 之外的 APK 全删；测试截图/解码/pylibs 也删
    fa = os.path.join(W, 'full_apk')
    for n in sorted(os.listdir(fa)):
        p = os.path.join(fa, n)
        if os.path.isfile(p) and n.lower().endswith('.apk') and n != 'patched_v22.apk':
            s = os.path.getsize(p)
            plans.append(('FILE', p, s))
            total += s
    for d in ('burst', 'burst2', 'combo', 'decoded', 'pylibs'):
        p = os.path.join(fa, d)
        if os.path.isdir(p):
            s = dirsize(p)
            plans.append(('DIR ', p, s))
            total += s
    for n in os.listdir(fa):
        if n.lower().endswith('.png'):
            p = os.path.join(fa, n)
            s = os.path.getsize(p)
            plans.append(('FILE', p, s))
            total += s

    vp = os.path.join(W, 'verify_patched', 'device_now.apk')
    if os.path.isfile(vp):
        s = os.path.getsize(vp)
        plans.append(('FILE', vp, s))
        total += s

    plans.sort(key=lambda x: -x[2])
    for kind, p, s in plans:
        print('%s %9.1f MB  %s' % (kind, s / 1048576.0, p))
    print('---')
    print('合计可释放 %.2f GB（%d 项）' % (total / 1073741824.0, len(plans)))
    if not apply:
        print('这是预演，加 --apply 才真删')
        return
    for kind, p, s in plans:
        try:
            if kind.strip() == 'DIR':
                shutil.rmtree(p, ignore_errors=True)
            else:
                os.remove(p)
        except OSError as e:
            print('删除失败', p, e)
    print('已删除')


if __name__ == '__main__':
    main()

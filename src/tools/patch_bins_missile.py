# -*- coding: utf-8 -*-
"""把桔梗(105)的导弹发射点(ThrowPoints)对齐到日暮篱(102)的同一索引上。

背景：MissileBase 每行有个 ThrowPoints（"x,y" 发射偏移）。实测
   桔梗 10500 = 5,17.7 / 10501 = 5,17.6 / 10502 = 5,18.2
   日暮篱 102xx = -13,12.2 / -6.1,13 / -11.3,10.4 / -1.7,8.7 …（Y 最大 13）
桔梗那三个明显偏高（17.6~18.2），而她的动作名和日暮篱完全同一套
（action_01 / SuperSkill_1..5），所以直接把「同一索引」的日暮篱发射点抄过来，
让她的箭和日暮篱一个高度。

用法: python patch_bins_missile.py <Bin目录> [--dry]
"""
import os
import re
import sys
import struct

COORD = re.compile(r'^-?[\d.]+,-?[\d.]+$')


def row_bounds(b):
    """返回 [(missileId, start, end)]；start 指向 key 的长度字节。"""
    keys = []
    for m in re.finditer(rb'(?<![0-9])(10[1-8]\d\d)(?![0-9])', b):
        i = m.start()
        if i >= 1 and b[i - 1] == len(m.group(1)):
            keys.append((int(m.group(1)), i - 1))
    out = []
    for idx, (mid, pos) in enumerate(keys):
        end = keys[idx + 1][1] if idx + 1 < len(keys) else len(b)
        out.append((mid, pos, end))
    return out


def last_coord(seg):
    """段内最后一个 "x,y" 长度前缀字符串 → (起始偏移, 长度, 文本)。"""
    p = 0
    best = None
    while p < len(seg):
        ln = seg[p]
        p += 1
        if ln == 0 or ln > 60:
            continue
        if p + ln > len(seg):
            break
        raw = seg[p:p + ln]
        p += ln
        try:
            t = raw.decode('ascii')
        except Exception:
            continue
        if COORD.match(t):
            best = (p - ln - 1, ln, t)
    return best


def main():
    bindir = sys.argv[1] if len(sys.argv) > 1 else r'binwork\Bin\Bin'
    dry = '--dry' in sys.argv
    path = os.path.join(bindir, 'MissileBase.bin')
    b = open(path, 'rb').read()
    bounds = row_bounds(b)
    print('MissileBase 行数', len(bounds), '（文件头声明 %d）' % struct.unpack_from('<i', b, 0)[0])

    gewei = {}
    for mid, s, e in bounds:
        if 10200 <= mid < 10300:
            c = last_coord(b[s:e])
            if c:
                gewei[mid - 10200] = c[2]
    print('日暮篱可用索引 %d 个' % len(gewei))

    # 从后往前改，避免偏移错位
    edits = []
    for mid, s, e in bounds:
        if not (10500 <= mid < 10600):
            continue
        idx = mid - 10500
        if idx not in gewei:
            print('  索引 %d 在日暮篱那边没有对应，跳过 %d' % (idx, mid))
            continue
        seg = b[s:e]
        c = last_coord(seg)
        if not c:
            continue
        off, ln, old = c
        new = gewei[idx]
        if old == new:
            continue
        edits.append((s + off, ln, old, new, mid))
        print('  %d: %s -> %s' % (mid, old, new))

    if dry or not edits:
        print('dry-run，未写文件' if dry else '无需修改')
        return

    edits.sort(key=lambda x: x[0], reverse=True)
    data = bytearray(b)
    for off, ln, old, new, mid in edits:
        raw = new.encode('ascii')
        data[off:off + 1 + ln] = bytes([len(raw)]) + raw
    open(path, 'wb').write(bytes(data))
    print('已写回 %s（%d -> %d 字节）' % (path, len(b), len(data)))


if __name__ == '__main__':
    main()

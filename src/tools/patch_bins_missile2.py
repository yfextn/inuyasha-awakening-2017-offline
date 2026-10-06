# -*- coding: utf-8 -*-
"""修正桔梗(105)导弹的发射高度：把 ThrowPoints 的 Y 对齐到日暮篱(102)同一索引。

MissileBase 每行有 ThrowPoints（"x,y" 发射偏移）。实测：
    桔梗   10500 = 5,17.7   10501 = 5,17.6   10502 = 5,18.2
    日暮篱 102xx 的 Y 都在 0~13（-13,12.2 / -6.1,13 / -11.3,10.4 / -1.7,8.7 …）
桔梗那三个 Y 明显高出别人一截，所以箭从很高的地方飞出去、打不到地上的敌人。
她的动作名和日暮篱是同一套（action_01 / SuperSkill_1..5），所以直接取
「同一索引」的日暮篱 Y 值来替换，只改 Y、保留她自己的 X。

用法: python patch_bins_missile2.py <Bin目录> [--dry]
"""
import os
import struct
import sys


def parse(b):
    p = 4
    n = struct.unpack_from('<i', b, 0)[0]
    rows = []

    def i32():
        nonlocal p
        v = struct.unpack_from('<i', b, p)[0]
        p += 4
        return v

    def f32():
        nonlocal p
        v = struct.unpack_from('<f', b, p)[0]
        p += 4
        return v

    def lstr():
        nonlocal p
        shift = 0
        ln = 0
        while True:
            by = b[p]
            p += 1
            ln |= (by & 0x7F) << shift
            if not (by & 0x80):
                break
            shift += 7
        s = b[p:p + ln].decode('utf-8')
        p += ln
        return s

    for _ in range(n):
        r = {}
        r['key'] = lstr()
        r['id'] = i32()
        r['type'] = i32()
        r['name'] = lstr()
        r['monsterIndex'] = i32()
        r['passiveMove'] = lstr()
        r['lifespan'] = f32()
        r['refresh'] = f32()
        r['energy'] = i32()
        r['useGravity'] = b[p]; p += 1
        r['onlyFlipX'] = b[p]; p += 1
        r['boomNameId'] = i32()
        r['mask'] = lstr()
        r['triggerMode'] = i32()
        r['childrenId'] = lstr()
        r['throwPoints'] = lstr()
        r['step'] = i32()
        rows.append(r)
    assert p == len(b), 'MissileBase 解析未对齐 %d/%d' % (p, len(b))
    return rows


def wr_str(s):
    d = s.encode('utf-8')
    ln = len(d)
    out = bytearray()
    while True:
        by = ln & 0x7F
        ln >>= 7
        if ln:
            out.append(by | 0x80)
        else:
            out.append(by)
            break
    out += d
    return bytes(out)


def dump(path, rows):
    out = bytearray()
    out += struct.pack('<i', len(rows))
    for r in rows:
        out += wr_str(r['key'])
        out += struct.pack('<i', r['id'])
        out += struct.pack('<i', r['type'])
        out += wr_str(r['name'])
        out += struct.pack('<i', r['monsterIndex'])
        out += wr_str(r['passiveMove'])
        out += struct.pack('<f', r['lifespan'])
        out += struct.pack('<f', r['refresh'])
        out += struct.pack('<i', r['energy'])
        out += bytes([r['useGravity'], r['onlyFlipX']])
        out += struct.pack('<i', r['boomNameId'])
        out += wr_str(r['mask'])
        out += struct.pack('<i', r['triggerMode'])
        out += wr_str(r['childrenId'])
        out += wr_str(r['throwPoints'])
        out += struct.pack('<i', r['step'])
    open(path, 'wb').write(bytes(out))


def main():
    bindir = sys.argv[1] if len(sys.argv) > 1 else r'binwork\Bin\Bin'
    dry = '--dry' in sys.argv
    scale = 0.75         # 平A 用（索引 0~11）：上一版 0.6 修好了命中但视觉偏低，抬回 0.75
    path = os.path.join(bindir, 'MissileBase.bin')
    b = open(path, 'rb').read()
    rows = parse(b)

    # 日暮篱同索引的 ThrowPoints（她俩动作名是同一套）
    gewei = {}
    for r in rows:
        if 10200 <= r['id'] < 10300 and ',' in r['throwPoints']:
            gewei[r['id'] - 10200] = r['throwPoints']
    gvals = sorted(float(v.split(',')[1]) for v in gewei.values())
    print('MissileBase 行数 %d，日暮篱 Y 范围 %.1f ~ %.1f' % (len(rows), gvals[0], gvals[-1]))

    changed = 0
    for r in rows:
        if not (10500 <= r['id'] < 10600):
            continue
        if ',' not in r['throwPoints']:
            continue
        idx = r['id'] - 10500
        xs, ys = r['throwPoints'].split(',')
        try:
            y = float(ys)
        except ValueError:
            continue
        if idx >= 50:
            # ★ 奥义用的这几颗（索引 50~58）桔梗这边全是 0,0 —— 明显是没填过的占位值，
            #   导弹从脚下/地面生成就打得没有伤害。直接抄日暮篱同索引的发射点。
            if idx not in gewei:
                print('  索引 %d 无对应，跳过 %d' % (idx, r['id']))
                continue
            new = gewei[idx]
            if new == r['throwPoints']:
                continue
            print('  %d(奥义): %s -> %s' % (r['id'], r['throwPoints'], new))
        else:
            if y <= 1.0:
                continue
            new = '%s,%s' % (xs, ('%g' % round(y * scale, 1)))
            if new == r['throwPoints']:
                continue
            print('  %d(平A): %s -> %s' % (r['id'], r['throwPoints'], new))
        r['throwPoints'] = new
        changed += 1

    if dry or not changed:
        print('dry-run，未写文件' if dry else '无需修改')
        return
    dump(path, rows)
    print('已写回 %s（%d -> %d 字节，改 %d 行）' % (path, len(b), os.path.getsize(path), changed))


if __name__ == '__main__':
    main()

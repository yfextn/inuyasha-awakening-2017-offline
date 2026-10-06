# -*- coding: utf-8 -*-
"""修正桔梗（英雄 105）的 MissileIndex —— 她射箭打不到人的根因。

heroAttr 的 MissileIndex 是「索引 → 导弹 id」映射，写成
    0:10100,1:10101,...
客户端按动作里写的索引去查导弹，查不到就用兜底值（表现在游戏里就是
箭从头顶很高的地方飞出去、打不到地上的敌人）。

对比同一套弓箭动作的日暮篱（102）：
    102 有 74 个导弹 10200..10280，MissileIndex 是完整的一大串；
而千问当初给 105 补的只有 5 条（0:10500..4:10504）。

MissileBase.bin 里桔梗自己的导弹其实有 21 个：
    10500..10511 和 10550..10558
编号规律和 102 一样（id = 英雄基数 + 索引，102 的基数是 10200，105 的基数就是 10500），
所以正确映射应该是 0:10500 … 11:10511、50:10550 … 58:10558。

用法: python patch_bins_kikyo2.py <Bin目录>
"""
import os
import re
import struct
import sys


def rd_i32(b, p):
    return struct.unpack_from('<i', b, p)[0], p + 4


def rd_str(b, p):
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
    return s, p


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


def parse(path):
    b = open(path, 'rb').read()
    n, p = rd_i32(b, 0)
    rows = []
    for _ in range(n):
        r = {}
        r['key'], p = rd_str(b, p)
        r['id'], p = rd_i32(b, p)
        r['name'], p = rd_str(b, p)
        for f in ('pic', 'level', 'qly', 'healthBar'):
            r[f], p = rd_i32(b, p)
        r['initialAttrs'], p = rd_str(b, p)
        for f in ('teleportedLimit', 'weapon', 'helmet', 'armour', 'shoes', 'ring', 'necklace',
                  'initialPower', 'chapterObjId', 'heroPanelObjId'):
            r[f], p = rd_i32(b, p)
        r['Introduce'], p = rd_str(b, p)
        r['Open'], p = rd_i32(b, p)
        r['rate'], p = rd_str(b, p)
        r['abilityPic'], p = rd_str(b, p)
        r['skillShowIndexes'], p = rd_i32(b, p)
        r['smallicon'], p = rd_str(b, p)
        for f in ('SuMingObjId', 'RoleChangeCd'):
            r[f], p = rd_i32(b, p)
        r['actorName'], p = rd_str(b, p)
        r['shadowHead'], p = rd_str(b, p)
        r['shadowPose'], p = rd_str(b, p)
        r['MissileIndex'], p = rd_str(b, p)
        r['avataId'], p = rd_i32(b, p)
        rows.append(r)
    assert p == len(b), 'heroAttr.bin 解析未对齐 %d/%d' % (p, len(b))
    return rows


def dump(path, rows):
    out = bytearray()
    out += struct.pack('<i', len(rows))
    for r in rows:
        out += wr_str(r['key'])
        out += struct.pack('<i', r['id'])
        out += wr_str(r['name'])
        for f in ('pic', 'level', 'qly', 'healthBar'):
            out += struct.pack('<i', r[f])
        out += wr_str(r['initialAttrs'])
        for f in ('teleportedLimit', 'weapon', 'helmet', 'armour', 'shoes', 'ring', 'necklace',
                  'initialPower', 'chapterObjId', 'heroPanelObjId'):
            out += struct.pack('<i', r[f])
        out += wr_str(r['Introduce'])
        out += struct.pack('<i', r['Open'])
        out += wr_str(r['rate'])
        out += wr_str(r['abilityPic'])
        out += struct.pack('<i', r['skillShowIndexes'])
        out += wr_str(r['smallicon'])
        for f in ('SuMingObjId', 'RoleChangeCd'):
            out += struct.pack('<i', r[f])
        out += wr_str(r['actorName'])
        out += wr_str(r['shadowHead'])
        out += wr_str(r['shadowPose'])
        out += wr_str(r['MissileIndex'])
        out += struct.pack('<i', r['avataId'])
    open(path, 'wb').write(bytes(out))


def main():
    bindir = sys.argv[1] if len(sys.argv) > 1 else r'binwork\Bin\Bin'
    best = '--best' in sys.argv
    path = os.path.join(bindir, 'heroAttr.bin')
    rows = parse(path)

    # 日暮篱(102)的完整 MissileIndex：桔梗的动作和她同一套，
    # 但桔梗自己的导弹只有 21 个（10500~10511、10550~10558），
    # 动作里引用的其它索引就查不到导弹 —— 表现就是"有动作、没射出箭"（第 3 段平A 就是这种）。
    gewei = None
    for r in rows:
        if r['id'] == 102:
            gewei = r['MissileIndex']
    if best and gewei:
        mapping = gewei
        note = '整套抄日暮篱（%d 项）' % (gewei.count(',') + 1)
    else:
        missile = open(os.path.join(bindir, 'MissileBase.bin'), 'rb').read()
        ids = sorted(set(int(m.group(1)) for m in
                         re.finditer(rb'(?<![0-9])(105\d\d)(?![0-9])', missile)))
        if not ids:
            print('MissileBase.bin 里找不到 105xx 导弹，放弃')
            return
        base = 10500
        mapping = ','.join('%d:%d' % (i - base, i) for i in ids)
        note = '只列她自己有的 %d 项' % len(ids)

    hit = 0
    for r in rows:
        if r['id'] == 105:
            print('原 MissileIndex:', r['MissileIndex'])
            r['MissileIndex'] = mapping
            hit += 1
            print('新 MissileIndex(%s): %s' % (note, mapping))
    if not hit:
        print('heroAttr.bin 里没有 105 行')
        return
    dump(path, rows)
    print('已写回 %s（%d 字节）' % (path, os.path.getsize(path)))


if __name__ == '__main__':
    main()

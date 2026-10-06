# -*- coding: utf-8 -*-
"""给桔梗补足 0~58 全部索引的导弹，并把 MissileIndex 恢复成她自己的 105xx 系列。

背景（实测结论）：
  * v13 把她 MissileIndex 补成 0:10500..4:10504 后，平A 前两段能打到人了；
  * v18 把 MissileIndex 整套换成日暮篱的 102xx（53 项）后，**三段普攻全空** ——
    说明导弹必须是她自己 105xx 那一族（日暮篱的导弹被她调起来不会生成）；
  * 她自己只有 10500~10511 和 10550~10558，动作里引用的 5~49 这些索引查不到 →
    表现就是"有动作、没射箭"（第三段普攻就是这种）。

做法：
  1. MissileBase.bin 里以 10501 为模板，克隆出 10512~10549（共 38 行，
     名字/发射点/移动数据全照抄，只改 MissileId），让 105xx 连续覆盖到 10558；
  2. heroAttr.bin 里把 105 的 MissileIndex 写成 0:10500 … 58:10558。

用法: python patch_bins_missile3.py <Bin目录> [--dry]
"""
import os
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


INT_FIELDS = ['id', 'type', 'monsterIndex', 'energy', 'boomNameId', 'triggerMode', 'step']
STR_FIELDS = ['key', 'name', 'passiveMove', 'mask', 'childrenId', 'throwPoints']
FLOAT_FIELDS = ['lifespan', 'refresh']
BYTE_FIELDS = ['useGravity', 'onlyFlipX']
ORDER = ['key', 'id', 'type', 'name', 'monsterIndex', 'passiveMove', 'lifespan', 'refresh',
         'energy', 'useGravity', 'onlyFlipX', 'boomNameId', 'mask', 'triggerMode',
         'childrenId', 'throwPoints', 'step']


def parse_missile(path):
    b = open(path, 'rb').read()
    n, p = rd_i32(b, 0)
    rows = []
    for _ in range(n):
        r = {}
        r['key'], p = rd_str(b, p)
        for f in ('id', 'type'):
            r[f], p = rd_i32(b, p)
        r['name'], p = rd_str(b, p)
        r['monsterIndex'], p = rd_i32(b, p)
        r['passiveMove'], p = rd_str(b, p)
        r['lifespan'] = struct.unpack_from('<f', b, p)[0]; p += 4
        r['refresh'] = struct.unpack_from('<f', b, p)[0]; p += 4
        r['energy'], p = rd_i32(b, p)
        r['useGravity'] = b[p]; p += 1
        r['onlyFlipX'] = b[p]; p += 1
        r['boomNameId'], p = rd_i32(b, p)
        r['mask'], p = rd_str(b, p)
        r['triggerMode'], p = rd_i32(b, p)
        r['childrenId'], p = rd_str(b, p)
        r['throwPoints'], p = rd_str(b, p)
        r['step'], p = rd_i32(b, p)
        rows.append(r)
    assert p == len(b), 'MissileBase 解析未对齐 %d/%d' % (p, len(b))
    return rows


def dump_missile(path, rows):
    out = bytearray()
    out += struct.pack('<i', len(rows))
    for r in rows:
        for f in ORDER:
            if f in INT_FIELDS:
                out += struct.pack('<i', r[f])
            elif f in STR_FIELDS:
                out += wr_str(r[f])
            elif f in FLOAT_FIELDS:
                out += struct.pack('<f', r[f])
            else:
                out += bytes([r[f]])
    open(path, 'wb').write(bytes(out))


# ---------- heroAttr ----------

def parse_hero(path):
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
    assert p == len(b), 'heroAttr 解析未对齐'
    return rows


HERO_ORDER = ['key', 'id', 'name', 'pic', 'level', 'qly', 'healthBar', 'initialAttrs',
              'teleportedLimit', 'weapon', 'helmet', 'armour', 'shoes', 'ring', 'necklace',
              'initialPower', 'chapterObjId', 'heroPanelObjId', 'Introduce', 'Open', 'rate',
              'abilityPic', 'skillShowIndexes', 'smallicon', 'SuMingObjId', 'RoleChangeCd',
              'actorName', 'shadowHead', 'shadowPose', 'MissileIndex', 'avataId']
HERO_STR = {'name', 'initialAttrs', 'Introduce', 'rate', 'abilityPic', 'smallicon',
            'actorName', 'shadowHead', 'shadowPose', 'MissileIndex'}
HERO_I32 = {'id', 'pic', 'level', 'qly', 'healthBar', 'teleportedLimit', 'weapon', 'helmet',
            'armour', 'shoes', 'ring', 'necklace', 'initialPower', 'chapterObjId',
            'heroPanelObjId', 'Open', 'skillShowIndexes', 'SuMingObjId', 'RoleChangeCd', 'avataId'}


def dump_hero(path, rows):
    out = bytearray()
    out += struct.pack('<i', len(rows))
    for r in rows:
        for f in HERO_ORDER:
            if f in HERO_STR:
                out += wr_str(r[f])
            elif f in HERO_I32:
                out += struct.pack('<i', r[f])
            else:
                out += wr_str(r[f])       # key
    open(path, 'wb').write(bytes(out))


def main():
    bindir = sys.argv[1] if len(sys.argv) > 1 else r'binwork\Bin\Bin'
    dry = '--dry' in sys.argv

    mpath = os.path.join(bindir, 'MissileBase.bin')
    rows = parse_missile(mpath)
    have = set(r['id'] for r in rows)
    tmpl = None
    for r in rows:
        if r['id'] == 10501:
            tmpl = r
            break
    if tmpl is None:
        print('MissileBase 里找不到 10501 模板，放弃')
        return

    added = 0
    for i in range(12, 50):
        mid = 10500 + i
        if mid in have:
            continue
        nr = dict(tmpl)
        nr['key'] = str(mid)
        nr['id'] = mid
        rows.append(nr)
        added += 1
    rows.sort(key=lambda r: r['id'])
    print('MissileBase 克隆新增 %d 行（10512~10549），总行数 %d' % (added, len(rows)))

    mapping = ','.join('%d:%d' % (i, 10500 + i) for i in range(0, 59))

    hpath = os.path.join(bindir, 'heroAttr.bin')
    hrows = parse_hero(hpath)
    hit = 0
    for r in hrows:
        if r['id'] == 105:
            print('原 MissileIndex:', r['MissileIndex'][:60], '...')
            r['MissileIndex'] = mapping
            hit += 1
    if not hit:
        print('heroAttr 里没有 105 行')
        return
    print('新 MissileIndex: 0:10500 … 58:10558（59 项，全部 105xx 自身系列）')

    if dry:
        print('dry-run，未写文件')
        return
    dump_missile(mpath, rows)
    dump_hero(hpath, hrows)
    print('已写回 MissileBase.bin(%d 字节) heroAttr.bin(%d 字节)' %
          (os.path.getsize(mpath), os.path.getsize(hpath)))


if __name__ == '__main__':
    main()

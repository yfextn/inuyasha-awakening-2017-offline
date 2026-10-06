# -*- coding: utf-8 -*-
"""把 NPC 角色「桔梗」作为可选角色加进客户端配置表。

背景：桔梗 = 英雄 id 105。客户端 `heroAttr.bin` 里根本没有这一行，而
`HeroData` 的构造函数是按 `heroAttr` 建角色列表的，所以不管服务端怎么发，
角色面板里都不会出现她。

本脚本改写 Bin.7z 里的三张表（就地把解出来的 Bin 目录改好，随后重新压回 7z）：
  heroAttr.bin   += 105（从表格里最接近的 104 杀生丸/106 飞天 抄一份骨架）
  getHero.bin    += 105（用 recruit=1 通关解锁，避免还要新增「桔梗碎片」道具）
  CharmLevel.bin += 105001..105020（客户端 HeroBase.clothesAddPoint 用裸索引器
                   取 "英雄id+3位等级"，缺了会 KeyNotFoundException，时装面板会崩）

写文件格式（与 CsvBinReader.ReadConfigFromBinaryStream1 一致）：
  int32 记录数 ; 每条 = 7bit 长度前缀的 UTF-8 key + 30 个字段（小端）
  List<KVP<int,int>> / List<string> 都是「先写成一个字符串」，解析在客户端做。
"""

import struct
import sys

BIN_DIR = sys.argv[1] if len(sys.argv) > 1 else r'F:\BaiduNetdiskDownload\_inuyasha_work\binwork\Bin\Bin'

# ---------------------------------------------------------------- 编解码


class R:
    def __init__(self, buf):
        self.b = buf
        self.p = 0

    def i32(self):
        v = struct.unpack_from('<i', self.b, self.p)[0]
        self.p += 4
        return v

    def s(self):
        n = 0
        sh = 0
        while True:
            c = self.b[self.p]
            self.p += 1
            n |= (c & 0x7F) << sh
            if not (c & 0x80):
                break
            sh += 7
        v = self.b[self.p:self.p + n].decode('utf-8', 'replace')
        self.p += n
        return v


def wstr(s):
    b = s.encode('utf-8')
    n = len(b)
    pre = bytearray()
    while True:
        c = n & 0x7F
        n >>= 7
        pre.append(c | 0x80 if n else c)
        if not n:
            break
    return bytes(pre) + b


def wi32(v):
    return struct.pack('<i', v)


# heroAttr 的 30 个字段：(名字, 类型)  类型 i=int32  s=string
HEROATTR_FIELDS = [
    ('id', 'i'), ('name', 's'), ('pic', 'i'), ('level', 'i'), ('qly', 'i'), ('healthBar', 'i'),
    ('initialAttrs', 's'), ('teleportedLimit', 'i'), ('weapon', 'i'), ('helmet', 'i'), ('armour', 'i'),
    ('shoes', 'i'), ('ring', 'i'), ('necklace', 'i'), ('initialPower', 'i'), ('chapterObjId', 'i'),
    ('heroPanelObjId', 'i'), ('Introduce', 's'), ('Open', 'i'), ('rate', 's'), ('abilityPic', 's'),
    ('skillShowIndexes', 'i'), ('smallicon', 's'), ('SuMingObjId', 'i'), ('RoleChangeCd', 'i'),
    ('actorName', 's'), ('shadowHead', 's'), ('shadowPose', 's'), ('MissileIndex', 's'), ('avataId', 'i'),
]

# getHero 的 5 个字段
GET_HERO_FIELDS = [('heroId', 'i'), ('recruit', 'i'), ('recruitValue', 's'), ('text', 's'), ('isSchedule', 'i')]

# CharmLevel 的 7 个字段
CHARM_FIELDS = [('id', 'i'), ('heroId', 'i'), ('levelId', 'i'), ('nextLevel', 'i'),
                ('charmDemand', 'i'), ('promote', 'i'), ('itemConsume', 's')]


def load_table(path, fields):
    """返回 [(key, {field: value})]，顺序保持文件里的顺序。"""
    raw = open(path, 'rb').read()
    r = R(raw)
    total = r.i32()
    rows = []
    for _ in range(total):
        key = r.s()
        rec = {}
        for name, ty in fields:
            rec[name] = r.i32() if ty == 'i' else r.s()
        rows.append((key, rec))
    assert r.p == len(raw), '%s: 解析后还剩 %d 字节（字段表不对）' % (path, len(raw) - r.p)
    return rows


def save_table(path, fields, rows):
    out = bytearray()
    out += wi32(len(rows))
    for key, rec in rows:
        out += wstr(str(key))
        for name, ty in fields:
            v = rec.get(name)
            if ty == 'i':
                out += wi32(int(v or 0))
            else:
                out += wstr(v if isinstance(v, str) else ('' if v is None else str(v)))
    open(path, 'wb').write(bytes(out))


# ---------------------------------------------------------------- 三张表


def add_hero_attr(bin_dir):
    p = bin_dir + r'\heroAttr.bin'
    rows = load_table(p, HEROATTR_FIELDS)
    keys = [k for k, _ in rows]
    print('  heroAttr.bin 原有 %d 条: %s' % (len(rows), ','.join(keys)))
    if '105' in keys:
        print('  已存在 105，跳过')
        return
    tmpl = dict(rows[[k for k, _ in rows].index('104')][1])   # 抄杀生丸的骨架
    row = dict(tmpl)
    row.update({
        'id': 105,
        'name': '桔梗',
        'pic': 105,
        'initialAttrs': '3*0,1*0,2*0,4*0,5*0,6*0,7*0,9*0,8*0,10*1000,11*6,23*0,24*0,12*300,13*10,46*1000,30*1',
        'teleportedLimit': 1,
        'weapon': 10120100, 'helmet': 10120200, 'armour': 10120300,
        'shoes': 10120400, 'ring': 10120500, 'necklace': 10120600,
        'initialPower': 200,
        'chapterObjId': 0,
        'heroPanelObjId': 0,               # 0 = 最保险（100 号就是 0，游戏只拿它做 AddTag）
        'Introduce': '守护四魂之玉的巫女，五十年前因奈落的设计与犬夜叉反目，'
                     '带着对他的思念封印了犬夜叉后死去。五百年后以陶土之身复活，'
                     '依旧以破魔之矢净化世间妖邪。',
        'Open': 1,
        'rate': 'juesedengjinew5',         # 只是 UI 图集里的 sprite 名，用已有的最保险
        'abilityPic': 'js_shuxing_ali',
        'skillShowIndexes': 19,            # TrainConfig 的合法键（101/102 也用 19）
        'smallicon': 'Jiegeng',            # image/head/actorhero/jiegeng.tex 确实存在
        'SuMingObjId': 0,
        'RoleChangeCd': 15,
        'actorName': 'Jiegeng',            # Prefabs/Character/jiegeng.prefab 确实存在
        'shadowHead': '',                  # 不确定的 sprite 名一律留空（100 号也是空）
        'shadowPose': '',
        # 只保留 5 个动作位（与 101/102/104/106/108 一致：action_01/02/03/04 + air）。
        # 之前一口气写了 0..11 十二个位是照 MissileBase 里有 10500..10511 猜的，
        # 而 index 是 spine 事件 throw_missile 的 e.Int（每个英雄骨架最多 5 个动作位），
        # 多出来的位没有对应动作，反而容易把箭挂在错误的高度/时机上。
        'MissileIndex': '0:10500,1:10501,2:10502,3:10503,4:10504',
        'avataId': 105001,                 # AvatarList 105001 = jiegeng_bat / Jiegeng
    })
    rows.append(('105', row))
    save_table(p, HEROATTR_FIELDS, rows)
    print('  heroAttr.bin -> 写入 105 桔梗，现共 %d 条' % len(rows))


def add_get_hero(bin_dir):
    p = bin_dir + r'\getHero.bin'
    rows = load_table(p, GET_HERO_FIELDS)
    keys = [k for k, _ in rows]
    print('  getHero.bin 原有 %d 条: %s' % (len(rows), ','.join(keys)))
    if '105' in keys:
        print('  已存在 105，跳过')
        return
    # recruit=1 (Pass 通关解锁)。这样走 HeroSystem 的 Pass 分支，
    # 不会去 m_Itemstype 里索引「桔梗碎片」，因此不必新增道具行。
    rows.append(('105', {'heroId': 105, 'recruit': 1, 'recruitValue': '100101',
                         'text': '完成第{0}章第{1}关', 'isSchedule': 0}))
    save_table(p, GET_HERO_FIELDS, rows)
    print('  getHero.bin -> 写入 105')


def add_charm_level(bin_dir):
    p = bin_dir + r'\CharmLevel.bin'
    rows = load_table(p, CHARM_FIELDS)
    keys = set(k for k, _ in rows)
    print('  CharmLevel.bin 原有 %d 条' % len(rows))
    if '105001' in keys:
        print('  已存在 105xxx，跳过')
        return
    tmpl = {}
    for k, rec in rows:
        if rec.get('heroId') == 101:
            tmpl[rec['levelId']] = rec
    added = 0
    for lv in range(1, 21):
        src = tmpl.get(lv)
        if not src:
            continue
        rec = dict(src)
        rec.update({'id': 105000 + lv, 'heroId': 105, 'levelId': lv})
        rows.append((str(105000 + lv), rec))
        added += 1
    save_table(p, CHARM_FIELDS, rows)
    print('  CharmLevel.bin -> 写入 105001..1050%02d（共 %d 条），现共 %d 条' % (added, added, len(rows)))


def main():
    print('Bin 目录:', BIN_DIR)
    add_hero_attr(BIN_DIR)
    add_get_hero(BIN_DIR)
    add_charm_level(BIN_DIR)
    print('完成。')


if __name__ == '__main__':
    main()

# -*- coding: utf-8 -*-
"""给 SkillGet.bin 补齐每个英雄的 5 个奥义（skillType == 400）。

为什么只差这一张表：
  * SkillBaseConfig_T / SkillUpgradeData_D / SkillDemageData_D / SkillHitData_D
    里 101400100..101400500 这 5 个 id 全都齐全（已逐字节确认）；
  * image/fate/ 里 icon10101..icon10105、name10101..name10105 五套图也都在；
  * EXskillData.bin 直接给出了 5 个奥义的名字（风之伤/爆流破/金刚枪破/妖穴斩流破/冥道残月破 …）；
  * 但 SkillGet.bin 每个英雄只写了 2 行 skillType==400，
    而客户端 HeroBase.bigSkillList 就是按“SkillGet 里 heroId 相同且 skillType==400”建的，
    所以奥义选择界面（FateChoose 有 5 个按钮）永远只显示 2 个。

本脚本按 EXskillData 的顺序把缺的 3 行补上，其它字段照抄现有奥义行的写法
（Combinations='0*1'、ActionId=''，图标/名字取自 EXskillData）。

用法: python patch_bins_ult.py <Bin目录>
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


def wr_i32(v):
    return struct.pack('<i', v)


def parse_skillget(path):
    b = open(path, 'rb').read()
    n, p = rd_i32(b, 0)
    rows = []
    for _ in range(n):
        r = {}
        r['key'], p = rd_str(b, p)
        r['id'], p = rd_i32(b, p)
        r['heroId'], p = rd_i32(b, p)
        r['limitLv'], p = rd_i32(b, p)
        r['skillType'], p = rd_i32(b, p)
        r['Combinations'], p = rd_str(b, p)
        r['ActionId'], p = rd_str(b, p)
        r['iconId'], p = rd_str(b, p)
        r['NameIconId'], p = rd_str(b, p)
        r['SkillName'], p = rd_str(b, p)
        r['SkillDes'], p = rd_str(b, p)
        rows.append(r)
    assert p == len(b), 'SkillGet.bin 解析未对齐 %d/%d' % (p, len(b))
    return rows


def parse_exskill(path):
    b = open(path, 'rb').read()
    n, p = rd_i32(b, 0)
    rows = []
    for _ in range(n):
        _key, p = rd_str(b, p)
        sid, p = rd_i32(b, p)
        icon, p = rd_i32(b, p)
        nicon, p = rd_i32(b, p)
        name, p = rd_str(b, p)
        _des, p = rd_str(b, p)
        rows.append((sid, icon, nicon, name))
    assert p == len(b), 'EXskillData.bin 解析未对齐'
    return rows


def dump_skillget(path, rows):
    out = bytearray()
    out += wr_i32(len(rows))
    for r in rows:
        out += wr_str(r['key'])
        out += wr_i32(r['id'])
        out += wr_i32(r['heroId'])
        out += wr_i32(r['limitLv'])
        out += wr_i32(r['skillType'])
        out += wr_str(r['Combinations'])
        out += wr_str(r['ActionId'])
        out += wr_str(r['iconId'])
        out += wr_str(r['NameIconId'])
        out += wr_str(r['SkillName'])
        out += wr_str(r['SkillDes'])
    open(path, 'wb').write(bytes(out))


def main():
    bindir = sys.argv[1] if len(sys.argv) > 1 else r'binwork\Bin\Bin'
    ex = parse_exskill(r'_bin_probe\Bin\EXskillData.bin')
    path = os.path.join(bindir, 'SkillGet.bin')
    rows = parse_skillget(path)
    have = set(r['id'] for r in rows)

    # 每个英雄现有奥义里最小的 limitLv，用来当新增奥义的解锁等级（不设门槛，方便直接试）
    min_limit = {}
    for r in rows:
        if r['skillType'] == 400:
            h = r['heroId']
            min_limit[h] = min(min_limit.get(h, 999), r['limitLv'])

    added = []
    for sid, icon, nicon, name in ex:
        if sid in have:
            continue
        hero = sid // 1000000
        rows.append({
            'key': str(sid),
            'id': sid,
            'heroId': hero,
            'limitLv': min_limit.get(hero, 1),
            'skillType': 400,
            'Combinations': '0*1',
            'ActionId': '',
            'iconId': str(icon),
            'NameIconId': str(nicon),
            'SkillName': name,
            'SkillDes': name + '：当前奥义额外增加{1}攻击力',
        })
        added.append((hero, sid, name, icon))

    rows.sort(key=lambda r: r['id'])
    dump_skillget(path, rows)
    print('SkillGet.bin 补入 %d 条奥义' % len(added))
    for h, sid, name, icon in added:
        print('   hero %d  %d  %s  icon=%d' % (h, sid, name, icon))
    total = {}
    for r in rows:
        if r['skillType'] == 400:
            total[r['heroId']] = total.get(r['heroId'], 0) + 1
    print('每个英雄奥义数:', dict(sorted(total.items())))
    print('rows=%d  size=%d' % (len(rows), os.path.getsize(path)))


if __name__ == '__main__':
    main()

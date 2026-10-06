# -*- coding: utf-8 -*-
"""给 HeadOrBg.bin 补一行桔梗（英雄 105），让她能出现在「头像」列表里。

客户端换头像/换主城形象都是走 HeadOrBgListView：
    SureUsing(id) → Request_ChangePic(level1Icon, type)
                  → Request_TownRoleChange(id, 0)   // id 就是 HeadOrBg.id = 英雄号
列表内容 = HeadOrBg 表里 type==1 且该英雄已拥有 的行。
表里原本只有 101/102/103/104/106/108（没有 105），所以桔梗进不了头像列表、
也就没法被选成主城形象。

图标取 level1Icon=8100050：image/head/player/8100050.tex 在包里是存在的
（8100010~8100090 对应英雄 101..109 的头像贴图）。
角色模型方面 heroAttr 里 105 的 actorName=Jiegeng，
character/ai/jiegeng.prefab 以及 jiegeng_1..10.prefab 也都在包里。

用法: python patch_bins_headorbg.py <Bin目录>
"""
import os
import struct
import sys

FIELDS_INT_AFTER_NAME = ('type', 'time', 'activate', 'level1Icon')


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
        r['type'], p = rd_i32(b, p)
        r['time'], p = rd_i32(b, p)
        r['activate'], p = rd_i32(b, p)
        r['level1Icon'], p = rd_i32(b, p)
        r['level2Icon'], p = rd_str(b, p)
        r['desc'], p = rd_str(b, p)
        r['level2Coordinate'], p = rd_str(b, p)
        r['level3Icon'], p = rd_str(b, p)
        r['level3Coordinate'], p = rd_str(b, p)
        r['level4Effect'], p = rd_str(b, p)
        rows.append(r)
    assert p == len(b), 'HeadOrBg.bin 解析未对齐 %d/%d' % (p, len(b))
    return rows


def dump(path, rows):
    out = bytearray()
    out += struct.pack('<i', len(rows))
    for r in rows:
        out += wr_str(r['key'])
        out += struct.pack('<i', r['id'])
        out += wr_str(r['name'])
        out += struct.pack('<i', r['type'])
        out += struct.pack('<i', r['time'])
        out += struct.pack('<i', r['activate'])
        out += struct.pack('<i', r['level1Icon'])
        out += wr_str(r['level2Icon'])
        out += wr_str(r['desc'])
        out += wr_str(r['level2Coordinate'])
        out += wr_str(r['level3Icon'])
        out += wr_str(r['level3Coordinate'])
        out += wr_str(r['level4Effect'])
    open(path, 'wb').write(bytes(out))


def main():
    bindir = sys.argv[1] if len(sys.argv) > 1 else r'binwork\Bin\Bin'
    path = os.path.join(bindir, 'HeadOrBg.bin')
    rows = parse(path)
    if any(r['id'] == 105 for r in rows):
        print('HeadOrBg.bin 里已经有 105 了，跳过')
        return
    row = {
        'key': '105',
        'id': 105,
        'name': '桔梗',
        'type': 1,
        'time': 0,
        'activate': 1,
        'level1Icon': 8100050,
        'level2Icon': '',
        'desc': '',
        'level2Coordinate': '',
        'level3Icon': '',
        'level3Coordinate': '',
        'level4Effect': '',
    }
    # 插在 104 后面，头像列表里的顺序才自然（101,102,103,104,105,106,108）
    idx = None
    for i, r in enumerate(rows):
        if r['type'] == 1 and r['id'] == 104:
            idx = i
            break
    if idx is None:
        idx = max([i for i, r in enumerate(rows) if r['type'] == 1] or [-1])
    rows.insert(idx + 1, row)
    dump(path, rows)
    print('HeadOrBg.bin 补入桔梗 id=105 level1Icon=8100050（共 %d 行, %d 字节）' % (len(rows), os.path.getsize(path)))
    for r in rows:
        if r['type'] == 1:
            print('   头像行 id=%d name=%s icon=%d' % (r['id'], r['name'], r['level1Icon']))


if __name__ == '__main__':
    main()

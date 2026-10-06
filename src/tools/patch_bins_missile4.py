# -*- coding: utf-8 -*-
"""桔梗第 3 段普攻不出箭 —— 补两处可能性。

实测（自动连点 + 抓帧统计箭道亮像素）：
    第 1 段 箭道亮像素峰值 1089   第 2 段 481   第 3 段 0
即前两段有箭、第三段完全没有。

已知她自己的导弹只有 10500~10511、10550~10558（v19 把 10512~10549 克隆补齐了）。
第 3 段连"生成物"都没有，只可能是它引用的索引落在 MissileIndex 之外，
或者它引用的那颗导弹发射点是 (0,0)（10553~10558 就全是 0,0，生成即消失）。

本脚本：
  1. MissileBase 里把 10553~10558 的 ThrowPoints 从 0,0 改成 -5.2,12（照抄 10551 的）；
  2. MissileBase 里继续克隆到 10699（索引 12~199 全覆盖）；
  3. heroAttr 的 MissileIndex 写成 0:10500 … 199:10699。

用法: python patch_bins_missile4.py <Bin目录>
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import patch_bins_missile3 as m3  # noqa: E402

MAXIDX = 199


def main():
    bindir = sys.argv[1] if len(sys.argv) > 1 else r'binwork\Bin\Bin'
    mpath = os.path.join(bindir, 'MissileBase.bin')
    rows = m3.parse_missile(mpath)
    by_id = {r['id']: r for r in rows}

    # 1) 10553~10558 的发射点 0,0 → 抄 10551 的
    src = by_id.get(10551)
    fixed = 0
    if src is not None:
        for i in range(53, 59):
            r = by_id.get(10500 + i)
            if r is not None and r['throwPoints'] in ('0,0', ''):
                r['throwPoints'] = src['throwPoints']
                fixed += 1
    print('10553~10558 发射点修正 %d 个（0,0 → %s）' %
          (fixed, src['throwPoints'] if src else 'n/a'))

    # 2) 克隆补齐到 10699
    tmpl = by_id.get(10501)
    added = 0
    for i in range(12, MAXIDX + 1):
        mid = 10500 + i
        if mid in by_id:
            continue
        nr = dict(tmpl)
        nr['key'] = str(mid)
        nr['id'] = mid
        rows.append(nr)
        by_id[mid] = nr
        added += 1
    rows.sort(key=lambda r: r['id'])
    print('MissileBase 新增 %d 行，总行数 %d' % (added, len(rows)))

    # 3) MissileIndex 覆盖 0..199
    mapping = ','.join('%d:%d' % (i, 10500 + i) for i in range(0, MAXIDX + 1))
    hpath = os.path.join(bindir, 'heroAttr.bin')
    hrows = m3.parse_hero(hpath)
    for r in hrows:
        if r['id'] == 105:
            r['MissileIndex'] = mapping
    print('MissileIndex 项数 %d（0:10500 … %d:%d）' % (MAXIDX + 1, MAXIDX, 10500 + MAXIDX))

    m3.dump_missile(mpath, rows)
    m3.dump_hero(hpath, hrows)
    print('已写回 MissileBase.bin(%d) heroAttr.bin(%d)' %
          (os.path.getsize(mpath), os.path.getsize(hpath)))


if __name__ == '__main__':
    main()

# -*- coding: utf-8 -*-
"""就地修正桔梗（105）的「角色简介」文案。

patch_bins_kikyo.py 只在缺失时新增记录，所以已经写进去的旧简介需要单独改。
"""
import sys

sys.path.insert(0, r'F:\BaiduNetdiskDownload\_inuyasha_work')
import importlib.util

spec = importlib.util.spec_from_file_location(
    'pk', r'F:\BaiduNetdiskDownload\_inuyasha_work\patch_bins_kikyo.py')
pk = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pk)

BIN_DIR = sys.argv[1] if len(sys.argv) > 1 else r'F:\BaiduNetdiskDownload\_inuyasha_work\binwork\Bin\Bin'
NEW_TEXT = ('守护四魂之玉的巫女，五十年前因奈落的设计与犬夜叉反目，'
            '带着对他的思念封印了犬夜叉后死去。五百年后以陶土之身复活，'
            '依旧以破魔之矢净化世间妖邪。')
# 只保留 5 个动作位的 missile 映射（详见 patch_bins_kikyo.py 的注释）
NEW_MISSILE = '0:10500,1:10501,2:10502,3:10503,4:10504'

path = BIN_DIR + r'\heroAttr.bin'
rows = pk.load_table(path, pk.HEROATTR_FIELDS)
changed = 0
for key, rec in rows:
    if key == '105':
        if rec.get('Introduce') != NEW_TEXT:
            rec['Introduce'] = NEW_TEXT
            changed += 1
        if rec.get('MissileIndex') != NEW_MISSILE:
            print('105 MissileIndex: %r -> %r' % (rec.get('MissileIndex'), NEW_MISSILE))
            rec['MissileIndex'] = NEW_MISSILE
            changed += 1
        print('105 name=%s actor=%s avataId=%s' % (rec['name'], rec['actorName'], rec['avataId']))
pk.save_table(path, pk.HEROATTR_FIELDS, rows)
print('updated rows:', changed, '/ total rows:', len(rows))

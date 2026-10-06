# -*- coding: utf-8 -*-
"""桔梗第 3 段普攻不出箭 —— 把她的第 3 段攻击动作改成第 1 段那套。

SkillBaseConfig_T.bin 里每个技能一行，行内带 actionName：
    105101100 → action_01    （第 1 段，出箭）
    105101200 → action_02    （第 2 段，出箭）
    105101300 → action_03    （第 3 段，只有动作、没有箭）
她的英雄预制体里 action_03 没有配套的发射数据，前两段正常。

"action_03" 和 "action_01" 长度一样，所以直接在原字节上等长替换，
不动文件大小、不动行结构，风险最小。

用法: python patch_bins_combo3.py <Bin目录> [--dry]
"""
import os
import sys


def main():
    bindir = sys.argv[1] if len(sys.argv) > 1 else r'binwork\Bin\Bin'
    dry = '--dry' in sys.argv
    path = os.path.join(bindir, 'SkillBaseConfig_T.bin')
    b = bytearray(open(path, 'rb').read())

    key = b'\x09' + b'105101300'
    old = b'\x09' + b'action_03'
    new = b'\x09' + b'action_01'

    idx = b.find(key)
    if idx < 0:
        print('找不到 105101300 行')
        return
    # 这一行的结束位置 = 下一行 key（105102100）出现的地方
    nxt = b.find(b'\x09' + b'105102100', idx + 1)
    end = nxt if nxt > 0 else idx + 120
    print('105101300 行 @%d ~ %d' % (idx, end))
    print('  行内容:', bytes(b[idx:end]))

    hits = []
    p = idx
    while True:
        j = b.find(old, p, end)
        if j < 0:
            break
        hits.append(j)
        p = j + 1
    if not hits:
        print('这一行里找不到 action_03')
        return
    print('找到 %d 处 action_03: %s' % (len(hits), hits))
    for j in hits:
        print('  @%d 替换前 %s → %s' % (j, bytes(b[j:j + 10]), bytes(new)))
        b[j:j + len(old)] = new
    print('  行内容现在是:', bytes(b[idx:end]))

    if dry:
        print('dry-run，未写文件')
        return
    open(path, 'wb').write(bytes(b))
    print('已写回 %s（%d 字节，大小不变）' % (path, len(b)))


if __name__ == '__main__':
    main()

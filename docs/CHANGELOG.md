# 修复记录 / Changelog

本文件记录把《犬夜叉·觉醒》改成离线单机版过程中修掉的问题（按发现顺序）。

## 服务端（`src/server`）

| 问题 | 原因 | 处理 |
|---|---|---|
| 竞技场打完卡死、之后全部"连接中" | 竞技场 `stage_id` 发了 `1` / `100302`，客户端 `Substring` 越界、找不到 `arena0302` 场景 | 改发真实存在的 `240101/240102/240103` |
| 同上（AI 英雄） | AI 用了 `109/110/111` 等客户端 `heroAttr` 里没有的英雄 → `CreateHero` NRE | 只用 101/102/103/104/106/108 |
| 结算面板打不开 | `PvpAsyncFinishResponse.rate` 发 0，客户端 `rewards/rate` 除零 | 改发 1 |
| 头像列表整排带锁、点不了 | `PicInfo.expireTime` 发 0，客户端 `TimeValidity(0)` 恒为未解锁 | 改发 `-1`（永久） |
| 角色信息面板头像框空白 | `RoleBaseInfo.pic/picBg` 写死 `1001/1`，`image/head/player/1.tex` 不存在 | 改发玩家真实头像（`pic`/`picbg`） |
| 宝石镶嵌没反应 | `GemUid` 是 `long`（背包 uid = uid×1e8+itemId），旧代码用 `Int()` 转换溢出返回 0 | 按 `% 1e8` 解析，落盘 + 推送背包 |
| 宝石升级（合成）无效 | 旧实现只回空包，不扣料不产出 | 按 `gemUpdateConfig`：扣 4 颗低级产 1 颗高级，并推 3002 背包 |
| 宝石图标刷屏报错 | `image/icon/3021001.tex` 不存在 | 默认背包不再发 3021001 |
| 装备只能强化第一个角色 | 4010 只下发一个英雄的装备，其余角色 `equipUid` 恒为 0 | 一次性下发**所有已拥有英雄**的装备 |
| 只能强化第 1 个角色（续） | 客户端 `EquipData.Request()` 恒用 `HeroUid=0` 请求 | 同上 |
| 觉醒点几次后卡死、切不了角色 | `HHeroQly` 无条件品质 +1，客户端 `m_heroAwakeConfig[英雄*100+品质]` 只有 0..9 行 → NRE 刷屏 | 新增 `HeroAwakeTable`，品质按表封顶，区分普通铸灵/特殊铸灵/觉醒 |
| 扫荡显示 560/次却只给 150 | `HStageSweep` 写死 `150 × 次数` | 改读 `StageConfig.rewards` |
| 精英关打完不显示通关、后续不解锁 | `HPveBegin` 把关卡号强制改写成 100101；`HStageList` 只发普通关 | 关卡号按表校验；一次下发普通 8 关 + 精英 5 关，精英关全部 `opened=true` |
| 时空炼狱永远打第 1 层 | 服务端恒发 `curFight=1 / maxScore=0`，不存残血、`awards` 发空 | 层数推进 + 残血/SP 落盘 + 奖励下发 |
| 竞技场对手太脆（几秒结束） | AI 属性直接沿用玩家自身 | 血量统一归一（我方阵容均值 ×2）、攻击 ×30%、防御均值 ×50% |
| 竞技场某个角色只打 1 点伤害 | 对手防御 ×1.4 后超过弱角色攻击，减法公式下变负 | 防御改为我方阵容均防 ×50% |
| 竞技场上阵不实时 | 读了关卡阵容 `FormationType 1`，竞技场用的是 `3` | 改用 `FormationTypePvpAsync(3)` |
| 残影无法切换/装备无效 | 残影 id 当成 `英雄号×100+i` 发，与客户端 `shadowConfig_D` 的组号 1..4 完全对不上 | 改用真实组号；登录补发 17000001 残影碎片；服务端补残影属性（客户端战斗里没算） |
| 奥义选了不生效 | `ChosenSuperSkill` 用硬编码白名单卡，新奥义被判非法回落 | 直接采用玩家选择；`skill_level` 补全 5 个奥义 |
| 桔梗不可用 | 默认不入队、时装/头像表没有她 | 默认入队、`HeadOrBg` 补 105 行、解锁 105001/105002 |
| 桔梗主城形象锁死第一套 | 曾为规避 NRE 强制把 `worn_clothes_105` 改回 105001，且每次读存档都跑 | 删除该强制逻辑 |
| 桔梗第 3 段普攻只有动作没箭 | 她的 `105101300` 用的是 `action_03`，预制体里没有配套发射数据 | 把该行动作改成 `action_01`（等长替换） |
| 桔梗平A 打不到人 / 箭飞太高 | `MissileIndex` 只补了 5 项；`MissileBase` 里 `10553~10558` 发射点是 `0,0` | 索引覆盖 0..199、克隆补齐 `10512~10699`、发射点照抄 102 调整高度 |
| 清档重开后七宝/日暮篱/杀生丸又变回锁定 | 原版这些英雄靠"招募"条件解锁，新存档只有一个初始英雄 | `Store.Progress` 里默认补齐 101/102/103/104/106/108（+105 桔梗） |
| 清档重开后桔梗成了默认首发 | 新存档 `Heroes=[101,105]`，默认阵容取前 3 个已拥有 → 变成 [101,105] | 阵容不足 3 人时用已拥有英雄补齐（顺序按英雄号，默认回到 101/102/103） |

## 客户端配置补丁（`src/tools`）

- `patch_bins_kikyo.py` —— 给 `heroAttr.bin` 补桔梗 105 行
- `patch_bins_kikyo2.py` —— 修正她的 `MissileIndex`
- `patch_bins_ult.py` —— `SkillGet.bin` 每个英雄补齐 **5 个奥义**
  （数据本来都在 `SkillBaseConfig_T`/`SkillUpgradeData_D` 等表里，只是 `SkillGet` 只列了 2 个）
- `patch_bins_headorbg.py` —— `HeadOrBg.bin` 补 105 行（头像 + 主城形象）
- `patch_bins_missile2/3/4.py` —— 桔梗导弹发射点与索引修正
- `patch_bins_combo3.py` —— 第 3 段普攻借用第 1 段动作
- `pack2.py` —— 在重打包基础上支持**新增** APK 条目（桔梗缺的图标/语音用别人资源顶上）

## 仍然无解（需要 Unity 工程 / 美术资源）

- 桔梗英雄预制体缺 skin → `AvaterUtil.CreateSkin NullReferenceException`
- 桔梗的 `image/skill/105xx`、`image/fate/icon105xx`、`sounds/fighting/jiegeng.audio`
  原包内不存在，只能用 102 的资源顶替
- AI 配音需要重新打 Unity `.audio` AssetBundle，无法靠改配置完成

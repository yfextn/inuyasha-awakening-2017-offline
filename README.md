# 犬夜叉·觉醒 2017 · 单机离线服务端
# Inuyasha Awakening 2017 · Offline Local Server

> 把 2017 年的《犬夜叉·觉醒》手游变成**完全离线单机版**：游戏内内置一个本地服务端，
> 不需要官网、不需要联网、不需要额外服务器进程，装上就能玩。
>
> Turn the 2017 mobile game *Inuyasha Awakening* into a **fully offline single-player game**.
> A local server is embedded inside the APK itself — no official servers, no internet,
> no separate server process required.

---

## 一、这是什么 / What is this

原版客户端启动后会去连官方服务器（HTTP 登录 + TCP 游戏网关）。本项目做了两件事：

1. **服务端**：`src/server` 是一套 C#（net35）写的**本地服务端**，编译成
   `InuyashaLocalServer.dll` 后替换进 APK 的 `assets/bin/Data/Managed/`。
   它在游戏进程内起两个监听：
   * `127.0.0.1:8089` —— HTTP：登录、区服列表、版本校验、SDK 回调
   * `127.0.0.1:9000` —— TCP：游戏网关（协议为 protobuf，消息号 1xxx–1000xx）
2. **客户端补丁**：`src/tools` 里是一批 Python 脚本，用来改客户端配置表
   （`Bin.7z` 内的 `heroAttr.bin` / `SkillGet.bin` / `MissileBase.bin` /
   `heroAwakeConfig.bin` / `HeadOrBg.bin` 等），以及替换
   `XEngineBase.dll` 里"没网就不让进"的判断。

> 本仓库**只包含自己写的代码**（服务端源码 + 补丁脚本 + 打包脚本），
> **不包含任何游戏资源**（贴图、音频、配置表、DLL、APK 一律不含）。
> 你需要自备正版 APK 与其中的托管程序集。
>
> This repository contains **only original code** (the local server, patch scripts and
> packaging scripts). **No game assets are included** — you must supply your own APK.

---

## 二、特性 / Features

- 完全离线：登录绕过、区服列表、版本校验全部本地伪造
- 剧情关卡 1–12 章（普通 8 关 + 精英 5 关，精英关无条件开放）
- 角色：上阵、编队、升级、觉醒/铸灵（普通 / 特殊 / 觉醒三档，按 `heroAwakeConfig` 封顶）
- 装备：强化、升阶（**每个角色独立**，不再只作用于第一个角色）
- 宝石：镶嵌 / 取下 / 升级（合成与槽位升级都按 `gemUpdateConfig` 真实扣料）
- 竞技场：异步 3v3（AI 血量按我方阵容均值归一，攻击 30%、防御均值 50%）
- 时空炼狱、地狱闯关、犬王、任务、商店、邮件、VIP、签到、排行榜等
- 头像 / 主城形象 / 时装可自由更换；头像解锁时间恒为"永久"
- 桔梗（105）可用（借用 102 日暮篱的部分美术/语音资源）

---

## 三、目录结构 / Layout

```
src/server/                     C# 本地服务端（net35）
  InuyashaLocalServer.csproj
  InuyashaLocalServer/*.cs      游戏逻辑：Game.cs / Game2.cs / TcpServer.cs / Store.cs …
  *.bin                         内嵌配置表（StageConfig / heroAwakeConfig / shadowConfig_D …）
src/tools/                      构建与补丁脚本
  pack.py / pack2.py            重打包 + 对齐 + 签名（pack2 支持新增 APK 条目）
  patch_bins_kikyo.py           给客户端补桔梗(105)行
  patch_bins_kikyo2.py          MissileIndex 修正
  patch_bins_ult.py             补齐每个英雄的 5 个奥义（SkillGet.bin）
  patch_bins_headorbg.py        头像/主城形象表补 105
  patch_bins_missile*.py        桔梗导弹发射点/发射索引修正
  patch_bins_combo3.py          桔梗第 3 段普攻借用第 1 段动作
  build.ps1                     一键：编译服务端 → 打客户端补丁 → 重打包 → 安装
  clean_work.py                 清理可再生的中间产物
docs/                           分析笔记
```

---

## 四、构建 / Build

依赖 / Requirements：

| 用途 | 工具 |
|---|---|
| 编译服务端 | .NET SDK（目标框架 `net35`）|
| 打配置表补丁 / 打包 | Python 3 |
| 解包/重打 `Bin.7z` | 7-Zip（`7zr.exe`）|
| 对齐与签名 APK | JDK（`apksigner`）、Android build-tools（`zipalign`）|

还需要（**自备，仓库不含**）：

- 正版 APK，以及从设备/APK 里取出的托管程序集：
  `UnityEngine.dll`、`Inuyasha.dll`、`XEngine*.dll`、`mscorlib.dll`
  （放到 `src/server` 引用的路径，或改 `InuyashaLocalServer.csproj` 里的 `HintPath`）
- 自己的签名用 keystore

流程：

```bash
# 1) 编译服务端
dotnet build src/server/InuyashaLocalServer.csproj -c Release

# 2) 按需打客户端配置表补丁（在解包出来的 Bin 目录上跑）
python src/tools/patch_bins_kikyo.py      <Bin目录>
python src/tools/patch_bins_ult.py        <Bin目录>
python src/tools/patch_bins_headorbg.py   <Bin目录>
python src/tools/patch_bins_combo3.py     <Bin目录>
7zr a -t7z Bin.patched.7z Bin -mx=9

# 3) 重打包 + 签名（把编译好的 dll、Bin.7z、XEngineBase 补丁塞回 APK）
python src/tools/pack2.py v1
```

成品 APK 请放在 **Releases**（仓库不存二进制）。

---

## 五、已知限制 / Known limitations

- **桔梗（英雄 105）是原版的未完成测试角色**：她的英雄预制体里缺技能图标、
  奥义名字图、战斗语音和 `action_03` 的命中/特效数据。仓库里的补丁用
  "借日暮篱（102）资源"的方式让她能正常游玩（图标 / 语音 / 奥义动作），
  但**换肤仍会报 `AvaterUtil.CreateSkin NullReferenceException`**，这需要
  用 Unity 工程补美术资源才能根除。
- 服务端是**按需实现**的：只覆盖客户端实际会发的协议；未实现的请求返回空包。
- 单机存档在 `/sdcard/Android/data/<包名>/files/inu_local_progress.txt`。

---

## 六、法律声明 / Legal

- 《犬夜叉·觉醒》及其全部美术、音频、配置、代码的著作权归**原权利人**所有。
- 本仓库**不包含任何游戏资源**，只包含独立编写的服务端程序与补丁脚本，
  目的是**学习、研究与个人离线游玩**。
- **严禁任何形式的商业化使用**：不得售卖、不得用于盈利性服务端、
  不得内购/广告变现、不得二次分发收费。
- 请在下载后 **24 小时内**自行删除；如果你不拥有该游戏，请不要使用本项目。
- 因使用本项目产生的一切后果由使用者自行承担。

- All rights to *Inuyasha Awakening* and its assets belong to the original rights holder.
- This repository contains **no game assets**, only independently written server code
  and patch scripts, for **study, research and personal offline play**.
- **Commercial use of any kind is strictly prohibited.**
- Delete within 24 hours; do not use if you do not own the game.

---

## 七、许可 / License

见 [LICENSE](LICENSE)：**允许学习、修改、非商业分享；禁止一切商业用途。**
See [LICENSE](LICENSE): study, modify and non-commercial sharing allowed;
**all commercial use prohibited**.

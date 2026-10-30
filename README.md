# 深空当铺模组（Probably Stolen Modding）

《Probably Stolen》（深空当铺，游戏内版本 **0.46D**）的模组开发工作区。按 [dragonpan2/probably-stolen-modding](https://github.com/dragonpan2/probably-stolen-modding) 的教程搭好，全部工具都在本文件夹内。

## 目录

```
E:\深空当铺模组\
├── build.bat                    ← 双击即编译（用本体带的便携 SDK），并自动装进游戏
├── src\PawnshopMod\             ← 模组工程（改这里）
│   ├── PawnshopMod.csproj       ← 已配置好 4 个游戏 DLL 引用 + 构建后自动安装
│   ├── manifest.xml             ← 模组元数据（ID: pawnshop_mod）
│   ├── ModEntry.cs              ← IMod 入口类，Init / OnEnable / OnDisable
│   ├── Notes\                   ← 仰慕者纸条（10 档 × 10 条 + 开场信，Harmony 补丁）
│   ├── News\                    ← 报纸纯新闻 60 条（追加进 COSMETIC 事件池，零补丁）
│   ├── Localization\zh.csv      ← 全部文案（221 个键，由 content/ 生成）
│   └── Textures\Items, Sounds, LootTables, Books\
├── content\                     ← 文案原稿（审核用，改这里再重新生成 CSV）
│   ├── notes_zh.md              ← 纸条 100 条 + 开场信 + 礼物阶梯
│   └── news_zh.md               ← 新闻 60 条
├── docs\probably-stolen-modding\ ← 教程仓库快照（中英双语）
│   ├── README.zh-CN.md          ← 入门必读
│   ├── ModdingGuide.zh-CN.md    ← 主指南（钩子点、掉落表、贴图、能力声明…）
│   └── create-mod.bat           ← 官方建项目脚本（想换 ID 重开一个工程时用）
├── tools\
│   ├── dotnet-sdk\              ← .NET SDK 8.0.427 便携版（dotnet.exe 在这里）
│   └── ILSpy\app\ILSpy.exe      ← 反编译器，读 Assembly-CSharp 用
├── assets\preview.png           ← 创意工坊预览图（1280×720）
├── 上传创意工坊\                 ← 手动上传用的成品（详见下文）
└── .build\                      ← 构建缓存（dotnet-home / nuget / temp），可随便删
```

游戏本体：`E:\SteamLibrary\steamapps\common\Probably Stolen`

## 怎么编译

- **双击 `build.bat`**（推荐），或
- 命令行：`tools\dotnet-sdk\dotnet.exe build -c Release src\PawnshopMod\PawnshopMod.csproj`

编译产物：`src\PawnshopMod\bin\Release\netstandard2.1\PawnshopMod.dll`，同时 `csproj` 里的 `InstallMod` 目标会把它和 `manifest.xml`、资源目录一起复制进：

```
E:\SteamLibrary\steamapps\common\Probably Stolen\Mods\PawnshopMod\
```

游戏目录不是这个路径时，用 `-p:GameDir="D:\你的路径\Probably Stolen"` 覆盖，或直接改 `PawnshopMod.csproj` 里的 `<GameDir>`。

## 模组功能（v0.2.3）

### 1. 神秘仰慕者的纸条

替换原版固定的 7 条文案，改为 **10 档 × 10 条 = 100 条**，按下层区声望（`FACTION_LOWER_LEVEL`）分档：

| 声望 | 档位 |
|---|---|
| 0–9 | T00 |
| 10–19 | T01 |
| 20–29 | T02 |
| 30–39 | T03 |
| 40–49 | T04 |
| 50–59 | T05 |
| 60–69 | T06 |
| 70–79 | T07 |
| 80–99 | T08 |
| 100–200 | T09 |

- **声望为负不出纸条**（原版礼物照常送，只是没有信）。
- **同档不重复**：抽过的编号记进本局存档，抽满 10 条才重置。
- **礼物纸条**：每档第 10 条附赠一件固定物品，价值随档位递增（`unitValue` 20 → 325）：

  `cup_noodle` → `galaxy_blend` → `beis_icecream` → `salve` → `scav_token` → `black_lamp` → `toolbox` → `aug_scanner` → `blue_blood_bag` → `bottled_water_premium`

- **开场信**：带这个特质的存档，第 2 天开店时固定夹一封（只送一次）。
- 所有纸条都**无标题、无署名**（只有正文）。

### 2. 报纸纯新闻

往游戏的 COSMETIC 事件池里追加 **60 条**纯风闻（上层生活 15 / 下层民生 15 / 安保行政 8 / 黑市帮派 7 / 工会教会 8 / 科技奇闻 7）。每条权重 4，原版 13 条权重 10，所以原版新闻仍占约 35% 曝光。**没有 `negociationDatas`，不影响任何物价**。

## 怎么测试

1. 启动游戏 → **模组（Mods）** 菜单 → 未启用列表里选中「深空当铺模组」→ **启用** → **重启游戏**。
2. 按 **F8** 打开控制台，应看到：
   - `[PawnshopMod] registered 60 cosmetic news entries.`
   - `[深空当铺模组]: 深空当铺模组 v0.2.3 loaded.` / `[深空当铺模组]: Enabled.`
3. 想快速看纸条：**F9 → Spawn → Spawn Admirer Note**，按当前下层区声望抽一条（礼物纸条会连带物品放进店面前台）。
4. 想看报纸：等第二天开店，或 F9 里触发事件，然后拿报纸（`isNewspaperUnlocked` 解锁后每天会送）。
5. 重新编译后 DLL 哈希会变；没开**开发者模式**时可能要求「审核并启用」，在模组详情里打开一次即可永久信任。



## 发布到 Steam 创意工坊（手动上传）

创意工坊物品就是**一个和 `Mods\PawnshopMod\` 一模一样的模组文件夹**，外加一张预览图：

```
上传创意工坊\
├── PawnshopMod\          ← 上传时选这个文件夹（manifest.xml 在根目录）
│   ├── manifest.xml
│   ├── PawnshopMod.dll
│   ├── Localization\zh.csv
│   └── preview.png       ← < 1 MB
├── preview.png           ← 同一个文件，方便在客户端里直接选图
└── 工坊信息.txt          ← 标题 / 简介 / 更新说明 / 标签 / 可见性，复制粘贴用
```

流程：

1. 改完代码或文案 → `build.bat` → 把 `src\PawnshopMod\` 里的 `manifest.xml`、`bin\Release\netstandard2.1\PawnshopMod.dll`、`Localization\zh.csv` 重新复制进 `上传创意工坊\PawnshopMod\`（预览图不用动）。
2. 在 Steam 客户端的创意工坊上传界面：内容目录选 `上传创意工坊\PawnshopMod`，预览图选 `上传创意工坊\preview.png`，标题和简介照 `工坊信息.txt` 粘贴。
3. 首次上传会创建新物品；之后对同一物品更新即可，订阅者自动收到新版本。
4. 自测阶段可见性选**隐藏**，确认没问题再改**公开**。

> 首次发布前须在 Steam 网页上接受一次创意工坊法律协议，账号也需要拥有这款游戏。

**已发布**：物品 ID `3829961729`，状态**隐藏**（只有作者可见）。

- 页面：<https://steamcommunity.com/sharedfiles/filedetails/?id=3829961729>
- 上传方式：`上传创意工坊\开始上传.bat`（手动，需要输 Steam 账号密码；steamcmd 不保存密码，每次上传都要输）
- `pawnshop_mod.vdf` 里的 `publishedfileid` 已是 `3829961729`，保持不动就是更新而不是新建
- 想公开：在 Steam 页面改可见性，或把 vdf 的 `"visibility" "2"` 改成 `"0"` 再跑一次上传

## 已验证（不是“应该能行”）

- 便携 SDK 编译 `netstandard2.1` 类库、并引用游戏里 4 个 DLL → 成功，0 警告 0 错误。
- `Mods\PawnshopMod\` 已就位：`manifest.xml`（v0.2.3）+ `PawnshopMod.dll`（11 KB）+ `Localization\zh.csv`（19 KB）。
- 文案键完整性：**221 个键**（100 条纸条 + 开场信 1 + 新闻 60×2）全部存在，无重复、无缺失；改文案请改 `content/*.md` 再跑 `python content/build_localization.py`。
- 反编译出的程序集里确认有 `ModEntry` / `AdmirerNotes` / `Patch_CreateSecretAdmirerNote` / `Patch_SecretAdmirerIntro` / `CosmeticNews`。
- **游戏已经能识别这个模组**（实机启动一次，`Player.log`）：

  ```
  [ModLoader] Found 1 mod folder(s)
  [ModLoader]   Registered (disabled, Local): 深空当铺模组 v0.2.0
  [ModLoader] audit: pawnshop_mod v0.2.0 source=Local ... caps=declared none hash=-
  ```

  注意是 **disabled**：还需要在模组菜单里点一次「启用」，那一步我代替不了。
- ILSpy 能启动（它要 .NET 10 运行时，本机 10.0.1 满足）。
- `E:\steamcmd` 自更新正常。

## 现在这套工程里已经确定的点

| 项 | 值 |
|---|---|
| 模组 ID | `pawnshop_mod`（小写+下划线，玩家磁盘上的文件夹名） |
| 程序集 | `PawnshopMod.dll` |
| 目标框架 | `netstandard2.1`（与教程一致，Unity 2021.3.45f2 可加载） |
| 引用 | `Assembly-CSharp`、`UnityEngine`、`UnityEngine.CoreModule`、`0Harmony`（均 `Private=false`，不打进产物） |
| 游戏版本写法 | 游戏日志里是 `0.46D`，manifest 按官方示例写 `0.46` |
| 能力声明 | `<Capabilities />` 空。用了文件/网络/进程/反射等要在 manifest 里声明，否则启用会被拒；自查命令：F8 控制台 `mod-scan pawnshop_mod` |

## 注意事项（都是实测出来的）

1. **构建请用 `build.bat`**：它已封装便携版 `tools\dotnet-sdk\dotnet.exe`，不要直接敲 `dotnet build`（系统 PATH 里的 `dotnet` 很可能只有运行时、没有 SDK）。
2. **steamcmd 不要放在含中文的目录里**：steamcmd 会拒绝从含非英文字符的路径运行（`cannot run from a folder path that includes non-English characters`），发布创意工坊前请自行安装到纯英文路径。
3. **开发者工具默认关着**：游戏会检查 `Probably Stolen.exe` 旁边有没有 `dev.txt`，没有就禁用开发者工具。需要时手动建一个空的 `dev.txt`。
4. 工程路径含中文对 **dotnet/MSBuild 没有影响**（已实测：工程路径含中文照样编译），唯一受影响的是 steamcmd，所以没必要为了开发把整个目录改名；真要改，改名后记得用 `-p:GameDir=` 或重跑 `build.bat`。
5. 在受限/沙箱环境里跑构建时，编译能过但「复制进游戏目录」那步可能被环境拒绝，用 `build.bat -p:SkipInstall=true` 再手动复制即可；正常双击 `build.bat` 不受影响。

## 下一步

- 想加物品/改掉落表/挂 Harmony 补丁，直接看 [ModdingGuide.zh-CN.md](docs/probably-stolen-modding/ModdingGuide.zh-CN.md) 的对应章节，代码写在 `src\PawnshopMod\` 下新增的 `.cs` 文件即可（同程序集会被自动 `[HarmonyPatch]` 应用）。
- 想换模组 ID/显示名/作者：改 `manifest.xml`，并同步改 `PawnshopMod.csproj` 里的 `<AssemblyName>`（如果你也想换程序集名）。

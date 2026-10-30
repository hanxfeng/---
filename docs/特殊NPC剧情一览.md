# 特殊 NPC 与剧情事件一览（游戏 0.46D）

> 数据来源：反编译的 `Assembly-CSharp.dll`（本仓库 `.ref/game/`）。
> 目的：给模组开发做参考——知道每个人物**哪天来、为什么来、卡在什么条件上**。

## 0. 它们是怎么被调度的

- 每天开店时，`StoreClientManager` 会跑一串 `Handle*` / `CheckStoryLine*`，决定今天队列里塞哪些剧情人物。
- **固定日程**基本都是硬编码 `dayCounter == N`；**条件型**则读存档字段或声望。
- 入口（按执行顺序大致如下）：
  - [`HandleCharacterLine`](.ref/game/StoreClientManager.cs#L865)：乔治奥 / 卢警官 / 埃莉诺 / 塞尔皮科
  - [`HandleClientFromStoryLine`](.ref/game/StoreClientManager.cs#L1094)：教会线（牧师、Abby、工程师）
  - [`HandleWantedClient`](.ref/game/StoreClientManager.cs#L1574)：通缉/悬赏随机池
  - `HandleImportantScriptedClient` / `HandleBlackmailClient` / `HandleScalperClient` / `HandleCaseEnjoyerClient` / `HandleJacksonStorage` …
- 剧情人物走 `AddClient` 直接入队，**不参与**随机客池；随机池（`StoreClientList.factionPools`）只负责黑市、上层区、革命军的普通客人。

---

## 1. 固定天数的长线剧情

| 角色 | 首次 | 后续节点（天数） | 关键条件 / 状态 |
|---|---|---|---|
| **乔治奥 Georgio** | **D6** | D9、D12、D15、D18、D22、D27、D32、D37、D43 | D15 那次需要你没选「Maimed」特质；D43 是借款 |
| **卢警官 Officer Lun** | **D11** | D12、D13、D16、D20、D24 | D20 需要 `lunAddictionLevel > 0`；D24 还需要他没被捕 |
| **埃莉诺 Eleanor**（革命军线） | **D23** | D25、D27，之后按条件分支；D28 她叔叔 | 分支用 `StoreClientData.CanEleanor*`：规划访问 / 拾荒者 / 开车 / 威胁 / 抉择 / 结局；D28 在无尽模式不触发 |
| **内利斯 Nellis** | **D21** | D25、D29、D33 | 无额外条件 |
| **塞尔皮科 Serpico** | **D40** | 之后 +2、+1、+2、+1、+2 天；然后是信 → 3 晚食物 → 警官 → 文件 → 新闻 → 结局 | 天数全部由 `SerpicoStoryline` 计算；食物阶段每晚要看店里有 2000 kcal |
| **教会线**（牧师 / Abby / 工程师） | **D3** | D5、D10、D12、D19、D26、D33、D40、D43、D47 | D3 牧师、D5 Abby#1、D10 牧师#2、D12 Abby#2、D19 Abby#3、D26 Abby#4、D33 Abby#5、D40 工程师、D43 Abby 结局、D47 工程师#2 |
| **房东（批发）** | **D18** | — | `LandlordWholesale` |
| **清洁工（地下交易所）** | **D25** | — | 仅当交易所尚未解锁 |
| **导师 Mentor** | **D12 / D24** | — | D12 霰弹枪（未介绍义体时）、D24 营业执照（还没证时） |
| **营业执照推销** | **D38 起** | 之后每 7 天（`day%7==3`） | 仍然没有营业执照时，会来加价行贿 |

### 乔治奥的额外两支
- **核废料**：`day ≥ 3` 且你往店里丢过核废料（`nuclearWasteDumped > 0`）且还没被访问过 → 一次。
- **催债**：借过他钱（`loanOfferedToGeorgio != 0`）且天数超过借款日 +25 天 → 上门的 `CreateGeorgioLoanPayback`。

### 埃莉诺的额外两支
- **早期**（[`HandleEleanorEarlyClient`](.ref/game/StoreClientManager.cs#L1859)）：`CanEleanorWarnedVisit`（叔叔被举报后的警告）、第 4 次访问日、第 5 次访问日（需要帮过她 ≥1 次且她没被捕）。
- **叔叔**（`HandleEleanorUncleClient`）：D28，仅非无尽模式。

---

## 2. 按条件触发（没有固定天数）

| 角色 / 事件 | 触发条件 | 位置 |
|---|---|---|
| **娄 Lou**（通缉 wanted5） | 拿到营业执照后，从 `louEligibleDay` 起；当天"缺钱/付不起房租"（`IsLouTemptingMorning`）或等满 14 天 | `ScheduleLouFirstVisit` |
| 娄 · 投放炸弹 / 玩具盒 / 伪装 / 收账 | 前一步做完后按各自 `CanLou*` 判断 | `CanLouDropBox` 等 |
| **屠夫 Butcher**（wanted2） | 被通缉状态 `wanted2`；之后每 7 天回访一次（若未被抓） | `CanButcherReturn` |
| **贝利 Bailey**（wanted1） | `CanBaileyReturn` | `StoreClientData` |
| **约翰·史密斯**（wanted8） | 被举报后 +2 天回来；之后每 7 天一访 | `CanJohnSmithWeekly` |
| **李北文 Li**（wanted6） | `liNextVisitDay` 到期；第 2 次是终章 | `HandleImportantScriptedClient` |
| **胡安 Juan**（wanted7） | `CanRecurrentJuanAppear()` 且 `day % 8 == 1` | 同上 |
| **贷款鲨** | 现金 ≥ 房贷首付 且 店还没买下 | 同上 |
| **吊销枪证** | 持 I 证且安保声望 < 0 / II 证 < 30 / III 证 < 60 | 同上 |
| **发明家 Jackson** | 有机器 + 存储舱数量与现金达标（D12 起 850、D30 起 1200，逐级提高） | `HandleJacksonStorage` |
| **勒索者** | 带 `Blackmail` 特质：D1 初访，之后每 7 天 | `HandleBlackmailClient` |
| **黄牛 Scalper** | 带 `Sentinel` 特质，`day % 7 == 6` | `HandleScalperClient` |
| **案件爱好者 CaseEnjoyer** | 店里同时有 ≥2 件案件物品，D>1；7 天后回访一次 | `HandleCaseEnjoyerClient` |
| **抽奖代表** | 抽奖解锁后 `day % 7 == 4` | `HandleServiceClient` |

---

## 3. 革命军（反抗军）专题

**结论：革命军没有"第一次来"的固定日子，它是一张按声望每天掷的随机池。**

- 每天生成普通客人时会依次掷三个阵营池（黑市 / 上层 / 革命军）：
  [`StoreClientList.factionPools`](.ref/game/StoreClientList.cs#L33)。
- 革命军今天的出场概率由 [`GetRevolutionClientChance`](.ref/game/StoreClientManager.cs#L1963) 决定：

| 革命军声望 | 每天出现概率 |
|---|---|
| ≥ 50 | 9% |
| 25 – 49 | 6% |
| 0 – 24 | 3% |
| < 0 | 2% |
| 解锁 `REV_BLACKLISTED`（声望 ≤ -40） | **0%（不再来）** |

- 池里目前只有 `revolutionaryBuyer`（`StoreClientListRev.CreateRevolutionaryBuyer`），权重 50。
- 红色军需官 `revQuartermaster`：需要 **革命军声望 ≥ 40** 解锁 `REV_QUARTERMASTER`，且 `day % 7 == 0` 才来。
- 掠夺者 `revRaider`：需要 **声望 ≥ 80** 解锁 `REV_RAID_LOOT`，且 `day % 7 == 5` 才来。
- 革命军请求（StoreRequest）：声望 ≥ 20 解锁 `REV_REQUEST` 后开始出现。
- 革命军行动（Operations）：由 `revPower` 决定——≥15 突袭上层、≥25 秘密动员、≥50 公开动员；安保侧有对应的镇压行动。
- 电话商人：革命军有个电话联系人（号码 `51189`），某些任务线推进后才接单。
- 相关特质：`REV_FRIEND`（60）、`REV_HIT`（-100，被盯上）。

---

## 4. 悬赏 / 通缉随机池

- 从 **D14** 起（新手引导跳过则更早），每天掷 [4% 起步、每天 +2%、连续 16 天没出必出](.ref/game/StoreClientManager.cs#L1597)。
- 池子：`wanted1 … wanted9`，其中
  - `wanted1` 贝利（Bailey）、`wanted2` 屠夫、`wanted4` 奥丁、`wanted5` 娄、`wanted6` 李北文、`wanted7` 胡安、`wanted8` 约翰·史密斯。
- 每次只会有一个通缉客入队（`isTodayWantedQueued` 标记）。

---

## 5. 调试与验证

- 只有 `dev.txt` 时，F9 里能看到基础的调试菜单；
  **把 `dev.txt` 改名为 `1dev.txt`**（完整开发者模式）才会出现 "Butcher / Lou / Eleanor …" 这些剧情线的单独按钮，以及 `AddNextClient(...)` 这类直接插队的工具。
- 剧情队列是 `AddNextClient`（插到下一个客人）vs `AddClient`（塞进今天队列）——调试时前者更可控。

## 6. 给模组作者的可挂钩点

- 想**改某人的日程**：Harmony 补丁 `StoreClientManager.CheckStoryLineGeorgio` / `CheckStoryLineOfficerLun` / `CheckStoryLineEleanor` / `CheckStoryLineNellis` / `SerpicoStoryline.HandleDay`（这些都是 public static/实例方法，补丁很轻）。
- 想**插自己的角色**：`PlayerStore.Instance.storeClientManager.AddNextClient(<StoreClient>)`；`StoreClient` 是普通类，用 `new StoreClient { ... }` 就能拼（参考 `StoreClientListRev` 的写法，最省事）。
- 想**加阵营权重**：`StoreClientList.factionPools` 里的 `RandomClientPool` 是公开静态对象，`Add(create, weight)` 就能加新客人。
- 想**加革命军行动**：`StoreOperationManager` 里的 blueprint 列表（`rev_mobilization_secret` 等）是公开静态的，可以照原版格式追加。

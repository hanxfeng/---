using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PawnshopMod
{
    /// <summary>
    /// 特殊 NPC 纸条：第一次遇到某个 NPC 的第二天早上，在柜台放一张关于他的纸条。
    /// 用哪一档取决于"遇见那一刻"的下层区声望（1 档 0-29，2 档 30-99，3 档 100+）。
    /// 每人只会收到一次，纸条本身不带物品。
    /// </summary>
    internal static class NpcNotes
    {
        private sealed class NpcDef
        {
            public string Id;
            public string[] Prefixes;
        }

        /// <summary>id 要和 Localization 里的 psm_npc_{id}_tN 对齐。</summary>
        private static readonly NpcDef[] Npcs =
        {
            new NpcDef { Id = "georgio", Prefixes = new[] { "georgio" } },
            new NpcDef { Id = "eleanor", Prefixes = new[] { "eleanor" } },
            new NpcDef { Id = "serpico", Prefixes = new[] { "serpico" } },
            new NpcDef { Id = "lou",     Prefixes = new[] { "wanted5", "lou" } },
            new NpcDef { Id = "joe",     Prefixes = new[] { "insurance" } },
            new NpcDef { Id = "sawyer",  Prefixes = new[] { "janitor" } },
            new NpcDef { Id = "jackson", Prefixes = new[] { "inventor" } },
            new NpcDef { Id = "butcher", Prefixes = new[] { "wanted2" } },
            new NpcDef { Id = "bailey",  Prefixes = new[] { "wanted1" } },
            new NpcDef { Id = "arthur",  Prefixes = new[] { "wanted3" } },
            new NpcDef { Id = "li",      Prefixes = new[] { "wanted6" } },
            new NpcDef { Id = "juan",    Prefixes = new[] { "wanted7" } },
            new NpcDef { Id = "john",    Prefixes = new[] { "wanted8" } }
        };

        private const string SeenPrefix = "npc_seen_";
        private const string RepPrefix = "npc_rep_";
        private const string DonePrefix = "npc_done_";

        private static string Key(string prefix, string id) => prefix + id;

        public static int TierOf(int reputation)
        {
            if (reputation < 30) return 1;
            if (reputation < 100) return 2;
            return 3;
        }

        /// <summary>某个剧情客人被排进今天的队列时调用，记录"第一次见到"和当时的声望。</summary>
        public static void OnClientAdded(StoreClient client)
        {
            try
            {
                if (client == null || string.IsNullOrEmpty(client.identifier))
                {
                    return;
                }
                if (PlayerStore.Instance == null || StoreStation.Instance == null)
                {
                    return;
                }
                // 只有带「神秘仰慕者」特质的存档才有人写这些纸条
                if (!StartingPerk.IsSecretAdmirer())
                {
                    return;
                }

                string identifier = client.identifier;

                foreach (NpcDef npc in Npcs)
                {
                    if (!Matches(npc, identifier))
                    {
                        continue;
                    }
                    if (ModHelper.HasModData(ModIds.Id, Key(SeenPrefix, npc.Id)))
                    {
                        return; // 已经见过（纸条是否已发由 Done 标记管）
                    }

                    int day = StoreStation.Instance.dayCounter;
                    int reputation = StoreReputation.GetLLReputation();
                    ModHelper.SetModDataInt(ModIds.Id, Key(SeenPrefix, npc.Id), day);
                    ModHelper.SetModDataInt(ModIds.Id, Key(RepPrefix, npc.Id), reputation);
                    Debug.Log($"[PawnshopMod] npc met: {npc.Id} day={day} rep={reputation} (tier {TierOf(reputation)})");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[PawnshopMod] OnClientAdded failed: " + e);
            }
        }

        private static bool Matches(NpcDef npc, string identifier)
        {
            // 「埃莉诺的叔叔」不是埃莉诺本人（是黑市打手），别算成见过她
            if (identifier == "eleanorUncle")
            {
                return false;
            }

            for (int i = 0; i < npc.Prefixes.Length; i++)
            {
                if (identifier.StartsWith(npc.Prefixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>每天开店门时调用：把"昨天以前见过、还没发过纸条"的人发出去。</summary>
        public static void DeliverPending()
        {
            try
            {
                if (PlayerStore.Instance == null || StoreStation.Instance == null)
                {
                    return;
                }
                if (!StartingPerk.IsSecretAdmirer())
                {
                    return;
                }

                int today = StoreStation.Instance.dayCounter;

                foreach (NpcDef npc in Npcs)
                {
                    if (!ModHelper.HasModData(ModIds.Id, Key(SeenPrefix, npc.Id)))
                    {
                        continue;
                    }
                    if (ModHelper.GetModDataBool(ModIds.Id, Key(DonePrefix, npc.Id)))
                    {
                        continue;
                    }

                    int seenDay = ModHelper.GetModDataInt(ModIds.Id, Key(SeenPrefix, npc.Id), -1);
                    if (seenDay < 0 || today <= seenDay)
                    {
                        continue; // 当天遇见，明天才送
                    }

                    int reputation = ModHelper.GetModDataInt(ModIds.Id, Key(RepPrefix, npc.Id), 0);
                    int tier = TierOf(reputation);
                    string body = ModHelper.GetLocalized(ModIds.Id, $"psm_npc_{npc.Id}_t{tier}");
                    GameItem note = PreBuiltItemHelper.CreateHandnote(body, "", "");

                    PlayerStore.Instance.AddDirectSellingItemToTable(note, true);
                    ModHelper.SetModDataBool(ModIds.Id, Key(DonePrefix, npc.Id), true);

                    Debug.Log($"[PawnshopMod] npc note: {npc.Id} tier={tier} rep={reputation} (met day {seenDay}, today {today})");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[PawnshopMod] DeliverPending failed: " + e);
            }
        }

        /// <summary>调试用：打印每个人物的状态。</summary>
        public static string Describe()
        {
            int today = StoreStation.Instance != null ? StoreStation.Instance.dayCounter : -1;
            List<string> parts = new List<string>();
            foreach (NpcDef npc in Npcs)
            {
                int seen = ModHelper.GetModDataInt(ModIds.Id, Key(SeenPrefix, npc.Id), -1);
                int rep = ModHelper.GetModDataInt(ModIds.Id, Key(RepPrefix, npc.Id), 0);
                bool done = ModHelper.GetModDataBool(ModIds.Id, Key(DonePrefix, npc.Id));
                parts.Add($"{npc.Id}: seen={seen} rep={rep} tier={(seen >= 0 ? TierOf(rep) : 0)} sent={done}");
            }
            return $"today={today} llrep={StoreReputation.GetLLReputation()} | " + string.Join("; ", parts);
        }
    }

    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.AddClient))]
    internal static class Patch_AddClient
    {
        private static void Prefix(StoreClient __0)
        {
            NpcNotes.OnClientAdded(__0);
        }
    }

    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.AddNextClient))]
    internal static class Patch_AddNextClient
    {
        private static void Prefix(StoreClient __0)
        {
            NpcNotes.OnClientAdded(__0);
        }
    }

    [HarmonyPatch(typeof(StoreShutterButton), nameof(StoreShutterButton.OnShutterDoorOpenMorning))]
    internal static class Patch_ShutterMorning
    {
        private static void Postfix()
        {
            NpcNotes.DeliverPending();
        }
    }
}

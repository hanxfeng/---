using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace PawnshopMod
{
    internal static class ModIds
    {
        public const string Id = "pawnshop_mod";
    }

    /// <summary>
    /// 仰慕者纸条：替换原版 7 条固定文案。
    /// 按下层区声望分 10 档（0-9 … 100-200），每档 10 条，第 10 条是"礼物纸条"，附赠一件固定物品。
    /// 同档内抽完一轮才允许重复；声望为负时不产生纸条。
    /// </summary>
    internal static class AdmirerNotes
    {
        private const int TierCount = 10;
        private const int NotesPerTier = 10;

        /// <summary>每档附赠的物品，价值随档位递增（unitValue 20 → 325）。</summary>
        private static readonly string[] GiftItems =
        {
            "cup_noodle",           // 20
            "galaxy_blend",         // 25
            "beis_icecream",        // 40
            "salve",                // 80
            "scav_token",           // 100
            "black_lamp",           // 120
            "toolbox",              // 150
            "aug_scanner",          // 200
            "blue_blood_bag",       // 250
            "bottled_water_premium" // 325
        };

        public static int TierOf(int reputation)
        {
            return Mathf.Clamp(reputation / 10, 0, TierCount - 1);
        }

        /// <summary>造一张纸条；声望为负时返回 null（游戏侧会安全跳过）。</summary>
        public static GameItem Create()
        {
            int reputation = StoreReputation.GetLLReputation();
            if (reputation < 0)
            {
                return null;
            }

            int tier = TierOf(reputation);
            int index = PickIndex(tier);
            string body = ModHelper.GetLocalized(ModIds.Id, $"psm_note_t{tier:00}_{index:00}");

            // 标题、署名都留空：只有正文
            GameItem note = PreBuiltItemHelper.CreateHandnote(body, "", "");

            if (index == NotesPerTier)
            {
                GiveGift(GiftItems[tier]);
            }

            return note;
        }

        /// <summary>同档内不重复抽取，抽满一轮后重置。</summary>
        private static int PickIndex(int tier)
        {
            string dataKey = $"notes_t{tier:00}_used";
            HashSet<int> used = new HashSet<int>();
            string raw = ModHelper.GetModData(ModIds.Id, dataKey);
            if (!string.IsNullOrEmpty(raw))
            {
                foreach (string part in raw.Split(','))
                {
                    if (int.TryParse(part, out int value))
                    {
                        used.Add(value);
                    }
                }
            }

            if (used.Count >= NotesPerTier)
            {
                used.Clear();
            }

            List<int> pool = new List<int>();
            for (int i = 1; i <= NotesPerTier; i++)
            {
                if (!used.Contains(i))
                {
                    pool.Add(i);
                }
            }

            int pick = pool[UnityEngine.Random.Range(0, pool.Count)];
            used.Add(pick);
            ModHelper.SetModData(ModIds.Id, dataKey, string.Join(",", used.OrderBy(v => v)));
            return pick;
        }

        private static void GiveGift(string itemId)
        {
            try
            {
                PlayerStore.Instance.AddDirectSellingItemToTable(itemId, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PawnshopMod] gift item '" + itemId + "' failed: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(PreBuiltItemHelper), nameof(PreBuiltItemHelper.CreateSecretAdmirerNote))]
    internal static class Patch_CreateSecretAdmirerNote
    {
        private static bool Prefix(ref GameItem __result)
        {
            try
            {
                __result = AdmirerNotes.Create();
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("[PawnshopMod] admirer note failed, falling back to vanilla: " + e);
                return true;
            }
        }
    }
}

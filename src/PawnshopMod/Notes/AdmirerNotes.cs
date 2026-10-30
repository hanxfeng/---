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

        /// <summary>有礼物的那天：纸条必出（原版是 50%）。</summary>
        public const int GiftDayNoteChance = 100;

        /// <summary>没礼物的那天：也给一个"只写字条"的机会（只从非礼物条目里抽）。</summary>
        public const int PlainDayNoteChance = 30;

        /// <summary>本次开店流程里是否已经产出过纸条（原版 50% 掷中时置位）。</summary>
        public static bool NoteCreated { get; set; }

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

        /// <summary>
        /// 只在"有礼物"的日子才会抽到的条目：T00 的 1 号
        /// 「东西放在门口了。不值钱，也不占地方。」——这句默认门口有东西。
        /// </summary>
        private static bool IsGiftOnly(int tier, int index)
        {
            return tier == 0 && index == 1;
        }

        /// <summary>
        /// 造一张纸条；声望为负时返回 null（游戏侧会安全跳过）。
        /// allowGift=false 时只从 1..9 号里抽（不带物品的那种）。
        /// </summary>
        public static GameItem Create(bool allowGift = true)
        {
            int reputation = StoreReputation.GetLLReputation();
            if (reputation < 0)
            {
                Debug.Log($"[PawnshopMod] note skipped: lower-level reputation is {reputation} (< 0).");
                return null;
            }

            int tier = TierOf(reputation);
            int index = PickIndex(tier, allowGift);
            string body = ModHelper.GetLocalized(ModIds.Id, $"psm_note_t{tier:00}_{index:00}");

            // 标题、署名都留空：只有正文
            GameItem note = PreBuiltItemHelper.CreateHandnote(body, "", "");

            if (allowGift && index == NotesPerTier)
            {
                GiveGift(GiftItems[tier]);
            }

            Debug.Log($"[PawnshopMod] note created: tier={tier:00} index={index:00} gift={allowGift} rep={reputation}");
            NoteCreated = true;
            return note;
        }

        /// <summary>同档内不重复抽取，抽满一轮后重置。</summary>
        private static int PickIndex(int tier, bool allowGift)
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

            int maxIndex = allowGift ? NotesPerTier : NotesPerTier - 1;

            // 只在"这一轮还能抽的条目"里判断是否抽完，避免不带礼物的日子把礼物条卡死
            int availableCount = 0;
            for (int i = 1; i <= maxIndex; i++)
            {
                if (used.Contains(i) || (!allowGift && IsGiftOnly(tier, i)))
                {
                    continue;
                }
                availableCount++;
            }
            if (availableCount == 0)
            {
                foreach (int i in Enumerable.Range(1, maxIndex).ToList())
                {
                    used.Remove(i);
                }
            }

            List<int> pool = new List<int>();
            for (int i = 1; i <= maxIndex; i++)
            {
                if (used.Contains(i) || (!allowGift && IsGiftOnly(tier, i)))
                {
                    continue;
                }
                pool.Add(i);
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

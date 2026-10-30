using System;
using HarmonyLib;
using UnityEngine;

namespace PawnshopMod
{
    /// <summary>
    /// 提高纸条的出现率（原版只在"有礼物 + 掷中 50%"时才有纸条）：
    ///   · 有礼物那天：纸条必出（100%）
    ///   · 没礼物那天：30% 概率收到一张不带物品的便条（只从非礼物条目里抽）
    /// 第 1、2 天不补：第 2 天走开场信，之前不抢戏。
    /// </summary>
    [HarmonyPatch(typeof(StartingPerkContent), nameof(StartingPerkContent.HandleSecretAdmirerOnOpenShutter))]
    internal static class Patch_SecretAdmirerChance
    {
        private static void Prefix()
        {
            AdmirerNotes.NoteCreated = false;
        }

        private static void Postfix()
        {
            try
            {
                if (!StartingPerk.IsSecretAdmirer())
                {
                    return;
                }
                if (AdmirerNotes.NoteCreated)
                {
                    // 原版那 50% 已经掷中，纸条已经产出
                    return;
                }

                int day = StoreStation.Instance.dayCounter;
                if (day <= 2)
                {
                    return;
                }

                StoreClientManager manager = PlayerStore.Instance.storeClientManager;
                bool giftToday = manager != null && manager.secretAdmirerDaysWithoutGift == 0;

                int chance = giftToday ? AdmirerNotes.GiftDayNoteChance : AdmirerNotes.PlainDayNoteChance;
                if (chance <= 0 || UnityEngine.Random.Range(0, 100) >= chance)
                {
                    return;
                }

                GameItem note = AdmirerNotes.Create(giftToday);
                if (note == null)
                {
                    return;
                }
                PlayerStore.Instance.AddDirectSellingItemToTable(note, true);
            }
            catch (Exception e)
            {
                Debug.LogError("[PawnshopMod] note top-up failed: " + e);
            }
        }
    }
}

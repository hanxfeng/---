using System;
using HarmonyLib;
using UnityEngine;

namespace PawnshopMod
{
    /// <summary>
    /// 第 2 天的开场信。原版那天固定送一件 galaxy_blend 且不掷纸条，
    /// 这里补一封一次性的信，作为整条线的开场。
    /// </summary>
    [HarmonyPatch(typeof(StartingPerkContent), nameof(StartingPerkContent.HandleSecretAdmirerOnOpenShutter))]
    internal static class Patch_SecretAdmirerIntro
    {
        private const string DataKey = "intro_letter_given";

        private static void Postfix()
        {
            try
            {
                if (NoteGate.Paused())
                {
                    return;
                }
                if (!StartingPerk.IsSecretAdmirer())
                {
                    return;
                }
                if (StoreStation.Instance.dayCounter != 2)
                {
                    return;
                }
                if (ModHelper.GetModDataBool(ModIds.Id, DataKey))
                {
                    return;
                }

                ModHelper.SetModDataBool(ModIds.Id, DataKey, true);

                string body = ModHelper.GetLocalized(ModIds.Id, "psm_note_intro_body");

                // 标题、署名都留空：和普通纸条保持一致
                PlayerStore.Instance.AddDirectSellingItemToTable(
                    PreBuiltItemHelper.CreateHandnote(body, "", ""), true);
            }
            catch (Exception e)
            {
                Debug.LogError("[PawnshopMod] intro letter failed: " + e);
            }
        }
    }
}

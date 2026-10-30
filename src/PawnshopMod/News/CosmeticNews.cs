using System;
using UnityEngine;

namespace PawnshopMod
{
    /// <summary>
    /// 报纸纯新闻：往游戏的 COSMETIC 事件池里追加若干条无物价影响的事件。
    /// 游戏每天开店时会从池子里抽，抽中就出现在当天的报纸上。
    /// </summary>
    internal static class CosmeticNews
    {
        private const int NewsCount = 60;

        /// <summary>单条新闻的权重。原版 13 条 × 10 = 130，我们 60 条 × 4 = 240。</summary>
        private const int NewsOdd = 4;

        private static bool registered;

        public static void Register()
        {
            if (registered)
            {
                return;
            }
            registered = true;

            try
            {
                var blueprints = StoreEventManager.cosmeticEventBlueprints;
                if (blueprints == null)
                {
                    Debug.LogWarning("[PawnshopMod] cosmeticEventBlueprints is null, news not registered.");
                    return;
                }

                for (int i = 1; i <= NewsCount; i++)
                {
                    string identifier = $"psm_news_{i:00}";
                    blueprints.Add(new StoreEventBlueprint(() => Build(identifier), NewsOdd, identifier));
                }

                Debug.Log($"[PawnshopMod] registered {NewsCount} cosmetic news entries.");
            }
            catch (Exception e)
            {
                Debug.LogError("[PawnshopMod] failed to register news: " + e);
            }
        }

        private static StoreEvent Build(string identifier)
        {
            return new StoreEvent
            {
                identifier = identifier,
                newsName = ModHelper.GetLocalized(ModIds.Id, identifier + "_title"),
                newsDescription = ModHelper.GetLocalized(ModIds.Id, identifier + "_desc"),
                duration = 1,
                eventType = StoreEvent.EventType.COSMETIC,
                eventArea = StoreEvent.EventArea.ALL,
                importance = 1,
                cooldown = 0
            };
        }
    }
}

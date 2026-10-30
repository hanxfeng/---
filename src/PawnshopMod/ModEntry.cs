using System;
using UnityEngine;
using HarmonyLib;

namespace PawnshopMod
{
    /// <summary>
    /// Entry point of the mod. The loader finds the one class implementing IMod, creates it,
    /// applies the [HarmonyPatch] classes of this assembly, then calls Init and OnEnable.
    /// All of that happens in the main menu: no save is loaded yet, so do not touch game
    /// state here. Subscribe to ModHook events instead (see ModdingGuide.zh-CN.md, "钩子点").
    /// </summary>
    public class ModEntry : IMod
    {
        public static ModEntry Instance { get; private set; }
        public ModManifest Manifest { get; private set; }
        public ModLog Log { get; private set; }

        public void Init(ModManifest manifest)
        {
            Instance = this;
            Manifest = manifest;
            Log = new ModLog(manifest);
            Log.Log($"{manifest.Name} v{manifest.ModVersion} loaded.");
        }

        public void OnEnable()
        {
            // 报纸新闻：往原版 COSMETIC 事件池里追加 60 条纯风闻（无物价影响）。
            CosmeticNews.Register();

            // 调试用：F9 的 Spawn 页里一键生成一张仰慕者纸条，方便核对文案与档位。
            ModHelper.AddDebugFunction(Manifest.ID, ModHelper.DEBUG_CATEGORY_SPAWN, "Spawn Admirer Note", SpawnAdmirerNote);

            // 调试用：把"纸条为什么不出现"相关的状态打到 F8 控制台。
            ModHelper.AddDebugFunction(Manifest.ID, ModHelper.DEBUG_CATEGORY_COMMAND, "PawnshopMod: Note State", LogNoteState);

            Log.Log("Enabled.");
        }

        public void OnDisable()
        {
            ModHelper.RemoveDebugFunctions(Manifest.ID);
            Log.Log("Disabled.");
        }

        private void SpawnAdmirerNote()
        {
            try
            {
                GameItem note = AdmirerNotes.Create();
                if (note == null)
                {
                    Log.Log("下层区声望为负，今晚不会有纸条。");
                    return;
                }
                ModHelper.MoveItemToInventory(note);
                Log.Log($"已生成纸条（下层区声望 {StoreReputation.GetLLReputation()}，档位 T{AdmirerNotes.TierOf(StoreReputation.GetLLReputation()):00}）。");
            }
            catch (Exception e)
            {
                Log.Error("生成纸条失败: " + e.Message);
            }
        }

        /// <summary>排查用：纸条依赖的几个状态。</summary>
        private void LogNoteState()
        {
            try
            {
                StoreClientManager manager = PlayerStore.Instance != null ? PlayerStore.Instance.storeClientManager : null;
                int rep = StoreReputation.GetLLReputation();
                int tier = AdmirerNotes.TierOf(rep);
                string used = ModHelper.GetModData(Manifest.ID, $"notes_t{tier:00}_used");
                Log.Log($"perk={StartingPerk.IsSecretAdmirer()} rep={rep} tier=T{tier:00} " +
                        $"noGiftDays={(manager != null ? manager.secretAdmirerDaysWithoutGift : -1)} " +
                        $"day={StoreStation.Instance.dayCounter} used=[{used}]");
            }
            catch (Exception e)
            {
                Log.Error("读取状态失败: " + e.Message);
            }
        }
    }
}

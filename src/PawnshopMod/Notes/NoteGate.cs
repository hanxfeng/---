using UnityEngine;

namespace PawnshopMod
{
    internal static class NoteGate
    {
        public const string PauseUntilRepKey = "notes_pause_until_rep";
        public const string PauseDayKey = "notes_pause_day";

        public static bool Paused()
        {
            if (PlayerStore.Instance == null || StoreStation.Instance == null)
            {
                return false;
            }

            int day = StoreStation.Instance.dayCounter;
            if (ModHelper.GetModDataInt(ModIds.Id, PauseDayKey, -1) == day)
            {
                return true;   // 只停这一天
            }

            int until = ModHelper.GetModDataInt(ModIds.Id, PauseUntilRepKey, int.MinValue);
            if (until == int.MinValue)
            {
                return false;  // 没设置过，正常发
            }
            return StoreReputation.GetLLReputation() < until;
        }
    }
}

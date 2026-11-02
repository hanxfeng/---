using HarmonyLib;

namespace PawnshopMod
{
    [HarmonyPatch(typeof(StartingPerkContent), nameof(StartingPerkContent.HandleSecretAdmirerOnOpenShutter))]
    internal static class Patch_SecretAdmirerGate
    {
        private static bool Prefix()
        {
            return !NoteGate.Paused();
        }
    }
}

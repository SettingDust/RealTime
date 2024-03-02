// HotelAdvertisementAIPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects for the hotel world info panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class HotelAdvertisementAIPatch
    {
        [HarmonyPatch]
        private sealed class HotelAdvertisementAI_GetEndFrame
        {
            [HarmonyPatch(typeof(HotelAdvertisementAI), "GetEndFrame")]
            [HarmonyPrefix]
            private static bool GetEndFrame(HotelAdvertisementAI __instance, ushort eventID, ref EventData data, ref uint __result)
            {
                uint num = (uint)Mathf.RoundToInt(__instance.m_eventDuration * SimulationManager.DAYTIME_HOUR_TO_FRAME);
                __result = data.m_startFrame + num;
                return false;
            }

        }
    }
}

// HotelAdvertisementAIPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects for the hotel advertisement AI game methods.
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

            [HarmonyPatch(typeof(HotelAdvertisementAI), "GetDaysLeft")]
            [HarmonyPrefix]
            public static bool GetDaysLeft(HotelAdvertisementAI __instance, ushort eventID, ref EventData data, ref int __result)
            {
                int num = 0;
                if (data.m_flags == EventData.Flags.Created)
                {
                    num += (int)__instance.m_prepareDuration;
                    num += (int)__instance.m_eventDuration;
                    num += (int)__instance.m_disorganizeDuration;
                }
                else if ((data.m_flags & EventData.Flags.Preparing) != 0)
                {
                    num += Mathf.CeilToInt((data.m_startFrame - Singleton<SimulationManager>.instance.m_currentFrameIndex) / SimulationManager.DAYTIME_HOUR_TO_FRAME);
                    num += (int)__instance.m_eventDuration;
                    num += (int)__instance.m_disorganizeDuration;
                }
                else if ((data.m_flags & EventData.Flags.Active) != 0)
                {
                    num += Mathf.CeilToInt((data.m_expireFrame - Singleton<SimulationManager>.instance.m_currentFrameIndex) / SimulationManager.DAYTIME_HOUR_TO_FRAME);
                    num += (int)__instance.m_disorganizeDuration;
                }
                else if ((data.m_flags & EventData.Flags.Disorganizing) != 0)
                {
                    num += Mathf.CeilToInt((GetDisorganizingEndFrame(__instance, eventID, ref data) - Singleton<SimulationManager>.instance.m_currentFrameIndex) / SimulationManager.DAYTIME_HOUR_TO_FRAME);
                }
                __result = Mathf.Clamp(num, 0, (int)(__instance.m_prepareDuration + __instance.m_eventDuration + __instance.m_disorganizeDuration));
                return false;
            }


            private static uint GetDisorganizingEndFrame(HotelAdvertisementAI __instance, ushort eventID, ref EventData data)
            {
                uint num = (uint)Mathf.RoundToInt(__instance.m_eventDuration * SimulationManager.DAYTIME_HOUR_TO_FRAME);
                uint num2 = (uint)Mathf.RoundToInt(__instance.m_disorganizeDuration * SimulationManager.DAYTIME_HOUR_TO_FRAME);
                return data.m_startFrame + num + num2;
            }

        }
    }
}

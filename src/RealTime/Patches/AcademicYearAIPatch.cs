namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using UnityEngine;

    [HarmonyPatch]
    public static class AcademicYearAIPatch
    {
        public static bool year_ended = false;

        [HarmonyPatch(typeof(AcademicYearAI), "CreateEvent")]
        [HarmonyPrefix]
        public static bool CreateEvent(ref EventData data)
        {           
            float hours_since_last_year_ended = CalculateHoursSinceLastYearEnded(ref data);
            if (hours_since_last_year_ended >= 24f)
            {
                year_ended = false;
                return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(AcademicYearAI), "GetYearProgress")]
        [HarmonyPrefix]
        public static bool GetYearProgress(ref float __result)
        {
            if (year_ended)
            {
                __result = 100f;
                return false;
            }
            return true;
        }

        public static float CalculateHoursSinceLastYearEnded(ref EventData data)
        {
            var lastAcademicYearAI = Singleton<EventManager>.instance.m_events.m_buffer[data.m_nextBuildingEvent].Info.m_eventAI as AcademicYearAI;
            uint num = (uint)Mathf.RoundToInt(lastAcademicYearAI.m_eventDuration * SimulationManager.DAYTIME_HOUR_TO_FRAME);
            uint endFrame = Singleton<EventManager>.instance.m_events.m_buffer[data.m_nextBuildingEvent].m_startFrame + num; // last year end frame
            year_ended = SimulationManager.instance.m_currentFrameIndex >= endFrame;
            return (SimulationManager.instance.m_currentFrameIndex - endFrame) * SimulationManager.DAYTIME_FRAME_TO_HOUR;
        }
    }
}

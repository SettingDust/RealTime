namespace RealTime.Patches
{
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using SkyTools.Tools;

    [HarmonyPatch]
    internal static class AcademicYearAIPatch
    {
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        public static TimeInfo TimeInfo { get; set; }

        public static uint actualEndFrame;

        [HarmonyPatch(typeof(AcademicYearAI), "GetYearProgress")]
        [HarmonyPrefix]
        public static bool GetYearProgress(ref float __result)
        {
            if (EventManagerPatch.didLastYearEnd)
            {
                __result = 100f;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(AcademicYearAI), "EndEvent")]
        [HarmonyPrefix]
        public static bool EndEvent(ref EventData data)
        {
            if (TimeInfo.IsNightTime || TimeInfo.Now.IsWeekend() || !RealTimeBuildingAI.IsBuildingWorking(data.m_building))
            {
                return false;
            }
            if (TimeInfo.CurrentHour < 9f || TimeInfo.CurrentHour > 10f)
            {
                return false;
            }
            actualEndFrame = SimulationManager.instance.m_currentFrameIndex;
            return true;
        }
    }
}

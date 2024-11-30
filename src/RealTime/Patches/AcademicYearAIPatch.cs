namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using SkyTools.Tools;

    [HarmonyPatch]
    internal static class AcademicYearAIPatch
    {
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        public static TimeInfo TimeInfo { get; set; }

        [HarmonyPatch(typeof(AcademicYearAI), "GetYearProgress")]
        [HarmonyPrefix]
        public static bool GetYearProgress(ref EventData data, ref float __result)
        {
            uint didLastYearEnd = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_building].m_garbageTrafficRate;

            if (didLastYearEnd == 1)
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
            if (!CanAcademicYearEndorBegin(data.m_building))
            {
                return false;
            }
            Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_building].m_cargoTrafficRate = SimulationManager.instance.m_currentFrameIndex;
            return true;
        }

        [HarmonyPatch(typeof(AcademicYearAI), "StartTogaParty")]
        [HarmonyPrefix]
        public static bool StartTogaParty()
        {
            if (TimeInfo.Now.IsWeekend() || TimeInfo.CurrentHour >= 22f && TimeInfo.CurrentHour <= 6f)
            {
                return true;
            }
            return false;
        }

        // dont start or end academic year if night time or weekend or building is closed or the hour is not between 9 am and 10 am
        public static bool CanAcademicYearEndorBegin(ushort buildingId)
        {
            if (TimeInfo.IsNightTime || TimeInfo.Now.IsWeekend() || !RealTimeBuildingAI.IsBuildingWorking(buildingId))
            {
                return false;
            }
            if (TimeInfo.CurrentHour < 9f || TimeInfo.CurrentHour > 10f)
            {
                return false;
            }
            return true;
        }
    }
}

namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using SkyTools.Tools;

    [HarmonyPatch]
    internal static class EventManagerPatch
    {
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        public static TimeInfo TimeInfo { get; set; }

        public static bool didLastYearEnd = false;

        [HarmonyPatch(typeof(EventManager), "CreateEvent")]
        [HarmonyPrefix]
        public static bool CreateEvent(EventManager __instance, out ushort eventIndex, ushort building, EventInfo info, ref bool __result)
        {
            if(info.GetAI() is AcademicYearAI)
            {
                var buildingManager = Singleton<BuildingManager>.instance;
                var eventData = default(EventData);
                eventData.m_flags = EventData.Flags.Created;
                eventData.m_startFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                eventData.m_ticketPrice = (ushort)info.m_eventAI.m_ticketPrice;
                eventData.m_securityBudget = (ushort)info.m_eventAI.m_securityBudget;
                if (building != 0)
                {
                    eventData.m_building = building;
                    eventData.m_nextBuildingEvent = buildingManager.m_buildings.m_buffer[building].m_eventIndex;
                }
                eventData.Info = info;
                if (__instance.m_events.m_size < 256)
                {
                    eventIndex = (ushort)__instance.m_events.m_size;
                    // dont start year if night time or weekend or building is closed
                    if (TimeInfo.IsNightTime || TimeInfo.Now.IsWeekend() || !RealTimeBuildingAI.IsBuildingWorking(building))
                    {
                        __result = false;
                        return false;
                    }

                    // if not between 9 and 10 dont start year
                    if (TimeInfo.CurrentHour < 9f || TimeInfo.CurrentHour > 10f)
                    {
                        __result = false;
                        return false;
                    }

                    bool can_start_new_year = false;
                    // first year or not first year and 24 hours have passed since the last year ended
                    if (eventIndex != 0)
                    {
                        float hours_until_next_year_starts = CalculateHoursSinceLastYearEnded();
                        
                        if (hours_until_next_year_starts >= 24f)
                        {
                            can_start_new_year = true;
                            didLastYearEnd = false;
                        }
                        else
                        {
                            didLastYearEnd = true;
                        }
                    }
                    else
                    {
                        can_start_new_year = true;
                    }
                    
                    if (can_start_new_year)
                    {
                        __instance.m_events.Add(eventData);
                        __instance.m_eventCount++;
                        info.m_eventAI.CreateEvent(eventIndex, ref __instance.m_events.m_buffer[eventIndex]);
                        buildingManager.m_buildings.m_buffer[building].m_eventIndex = eventIndex;
                        __result = true;
                        return false;
                    }
                }
                eventIndex = 0;
                __result = false;
                return false;
            }
            eventIndex = 0;
            return true;
        }

        public static float CalculateHoursSinceLastYearEnded() => (SimulationManager.instance.m_currentFrameIndex - AcademicYearAIPatch.actualEndFrame) * SimulationManager.DAYTIME_FRAME_TO_HOUR;

    }
}

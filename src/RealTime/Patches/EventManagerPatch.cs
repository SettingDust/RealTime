namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using SkyTools.Tools;
    using UnityEngine;

    [HarmonyPatch]
    internal static class EventManagerPatch
    {
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        public static TimeInfo TimeInfo { get; set; }

        public static bool last_year_ended = false;

        [HarmonyPatch(typeof(EventManager), "BuildingDeactivated")]
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
                        float hours_since_last_year_ended = CalculateHoursSinceLastYearEnded(ref eventData);
                        if(hours_since_last_year_ended >= 24f)
                        {
                            can_start_new_year = true;
                            last_year_ended = false;
                        }
                        else
                        {
                            last_year_ended = true;
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

        public static float CalculateHoursSinceLastYearEnded(ref EventData data)
        {
            var lastAcademicYearAI = Singleton<EventManager>.instance.m_events.m_buffer[data.m_nextBuildingEvent].Info.m_eventAI as AcademicYearAI;
            uint num = (uint)Mathf.RoundToInt(lastAcademicYearAI.m_eventDuration * SimulationManager.DAYTIME_HOUR_TO_FRAME);
            uint endFrame = Singleton<EventManager>.instance.m_events.m_buffer[data.m_nextBuildingEvent].m_startFrame + num; // last year end frame
            return (SimulationManager.instance.m_currentFrameIndex - endFrame) * SimulationManager.DAYTIME_FRAME_TO_HOUR;
        }

    }
}

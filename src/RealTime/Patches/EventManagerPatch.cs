namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;

    [HarmonyPatch]
    internal static class EventManagerPatch
    {
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
                    if (!AcademicYearAIPatch.CanAcademicYearEndorBegin(building))
                    {
                        __result = false;
                        return false;
                    }

                    bool can_start_new_year = false;
                    // first year or not first year and 24 hours have passed since the last year ended
                    if (eventIndex != 0)
                    {
                        float hours_since_last_year_ended = CalculateHoursSinceLastYearEnded(eventData.m_building);
                        
                        if (hours_since_last_year_ended >= 24f)
                        {
                            can_start_new_year = true;
                            buildingManager.m_buildings.m_buffer[building].m_garbageTrafficRate = 0;
                        }
                        else
                        {
                            buildingManager.m_buildings.m_buffer[building].m_garbageTrafficRate = 1;
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

        public static float CalculateHoursSinceLastYearEnded(ushort buildingId)
        {
            uint actualEndFrame = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].m_cargoTrafficRate;
            return (SimulationManager.instance.m_currentFrameIndex - actualEndFrame) * SimulationManager.DAYTIME_FRAME_TO_HOUR;
        }

    }
}

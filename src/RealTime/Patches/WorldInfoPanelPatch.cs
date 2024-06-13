// WorldInfoPanelPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework.UI;
    using ColossalFramework;
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.UI;
    using RealTime.Events;
    using RealTime.GameConnection;

    /// <summary>
    /// A static class that provides the patch objects for the world info panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class WorldInfoPanelPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the customized citizen information panel.</summary>
        public static CustomCitizenInfoPanel CitizenInfoPanel { get; set; }

        /// <summary>Gets or sets the customized vehicle information panel.</summary>
        public static CustomVehicleInfoPanel VehicleInfoPanel { get; set; }

        /// <summary>Gets or sets the customized campus information panel.</summary>
        public static CustomCampusWorldInfoPanel CampusWorldInfoPanel { get; set; }

        /// <summary>Gets or sets the timeInfo.</summary>
        public static TimeInfo TimeInfo { get; set; }

        /// <summary>Gets or sets the game events data.</summary>
        public static RealTimeEventManager RealTimeEventManager { get; set; }

        [HarmonyPatch]
        private sealed class WorldInfoPanel_UpdateBindings
        {
            [HarmonyPatch(typeof(WorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void Postfix(WorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
            {
                switch (__instance)
                {
                    case CitizenWorldInfoPanel _:
                        CitizenInfoPanel?.UpdateCustomInfo(ref ___m_InstanceID);
                        break;

                    case VehicleWorldInfoPanel _:
                        VehicleInfoPanel?.UpdateCustomInfo(ref ___m_InstanceID);
                        break;

                    case CampusWorldInfoPanel _:
                        CampusWorldInfoPanel?.UpdateCustomInfo(ref ___m_InstanceID);
                        break;
                }
            }
        }

        [HarmonyPatch]
        private sealed class FootballPanel_RefreshMatchInfo
        {
            [HarmonyPatch(typeof(FootballPanel), "RefreshMatchInfo")]
            [HarmonyPostfix]
            private static void Postfix(FootballPanel __instance, ref InstanceID ___m_InstanceID, ref UILabel ___m_nextMatchDate, ref UIPanel ___m_panelPastMatches)
            {
                int eventIndex = Singleton<BuildingManager>.instance.m_buildings.m_buffer[___m_InstanceID.Building].m_eventIndex;
                var eventData = Singleton<EventManager>.instance.m_events.m_buffer[eventIndex];
                var data = eventData;

                var footbal_event = RealTimeEventManager.GetCityEvent(___m_InstanceID.Building);

                if (footbal_event != null)
                {
                    ___m_nextMatchDate.text = footbal_event.StartTime.ToString("dd/MM/yyyy HH:mm");
                }

                for (int i = 1; i <= 6; i++)
                {
                    var uISlicedSprite = ___m_panelPastMatches.Find<UISlicedSprite>("PastMatch " + i);
                    var uILabel2 = uISlicedSprite.Find<UILabel>("PastMatchDate");
                    ushort num4 = data.m_nextBuildingEvent;
                    if (i == 1 && (eventData.m_flags & EventData.Flags.Cancelled) != 0)
                    {
                        num4 = (ushort)eventIndex;
                    }
                    if (num4 != 0)
                    {
                        data = Singleton<EventManager>.instance.m_events.m_buffer[num4];
                        var past_footbal_event = RealTimeEventManager.GetCityEvent(data.m_building);
                        if (past_footbal_event != null)
                        {
                            uILabel2.text = past_footbal_event.StartTime.ToString("dd/MM/yyyy HH:mm");
                        }
                        else
                        {
                            uILabel2.text = data.StartTime.ToString("dd/MM/yyyy");
                        }
                    }
                }
            }
        }

        [HarmonyPatch]
        internal static class VarsitySportsArenaPanelPatch
        {
            [HarmonyPatch(typeof(VarsitySportsArenaPanel), "RefreshPastMatches")]
            [HarmonyPostfix]
            private static void RefreshPastMatches(int eventIndex, EventData upcomingEvent, EventData currentEvent, ref UIPanel ___m_panelPastMatches)
            {
                var originalTime = new DateTime(currentEvent.m_startFrame * SimulationManager.instance.m_timePerFrame.Ticks + SimulationManager.instance.m_timeOffsetTicks);
                currentEvent.m_startFrame = SimulationManager.instance.TimeToFrame(originalTime);

                for (int i = 1; i <= 6; i++)
                {
                    var uISlicedSprite = ___m_panelPastMatches.Find<UISlicedSprite>("PastMatch " + i);
                    var uILabel2 = uISlicedSprite.Find<UILabel>("PastMatchDate");
                    ushort num4 = currentEvent.m_nextBuildingEvent;
                    if (i == 1 && (upcomingEvent.m_flags & EventData.Flags.Cancelled) != 0)
                    {
                        num4 = (ushort)eventIndex;
                    }
                    if (num4 != 0)
                    {
                        currentEvent = Singleton<EventManager>.instance.m_events.m_buffer[num4];
                        var past_sports_event = RealTimeEventManager.GetCityEvent(currentEvent.m_building);
                        if (past_sports_event != null)
                        {
                            uILabel2.text = past_sports_event.StartTime.ToString("dd/MM/yyyy HH:mm");
                        }
                        else
                        {
                            uILabel2.text = currentEvent.StartTime.ToString("dd/MM/yyyy");
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(VarsitySportsArenaPanel), "RefreshNextMatchDates")]
            [HarmonyPostfix]
            private static void RefreshNextMatchDates(EventData upcomingEvent, EventData currentEvent, ref UILabel ___m_nextMatchDate)
            {
                var varsity_sports_event = RealTimeEventManager.GetCityEvent(currentEvent.m_building);
                if (varsity_sports_event != null)
                {
                    ___m_nextMatchDate.text = varsity_sports_event.StartTime.ToString("dd/MM/yyyy HH:mm");
                }
            }
        }

        [HarmonyPatch]
        internal static class HotelWorldInfoPanelPatch
        {
            [HarmonyPatch(typeof(HotelWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void Postfix(HotelWorldInfoPanel __instance, ref InstanceID ___m_InstanceID, ref UILabel ___m_labelEventTimeLeft, ref UIPanel ___m_panelEventInactive)
            {
                ushort building = ___m_InstanceID.Building;
                var hotel_event = RealTimeEventManager.GetCityEvent(building);
                var event_state = RealTimeEventManager.GetEventState(building, DateTime.MaxValue);

                if(hotel_event != null)
                {
                    if (event_state == CityEventState.Upcoming)
                    {
                        if (hotel_event.StartTime.Date < TimeInfo.Now.Date)
                        {
                            string event_start = hotel_event.StartTime.ToString("dd/MM/yyyy HH:mm");
                            ___m_labelEventTimeLeft.text = "Event starts at " + event_start;
                        }
                        else
                        {
                            string event_start = hotel_event.StartTime.ToString("HH:mm");
                            ___m_labelEventTimeLeft.text = "Event starts at " + event_start;
                        }
                    }
                    else if (event_state == CityEventState.Ongoing)
                    {
                        if (TimeInfo.Now.Date < hotel_event.EndTime.Date)
                        {
                            string event_end = hotel_event.EndTime.ToString("dd/MM/yyyy HH:mm");
                            ___m_labelEventTimeLeft.text = "Event ends at " + event_end;
                        }
                        else
                        {
                            string event_end = hotel_event.EndTime.ToString("HH:mm");
                            ___m_labelEventTimeLeft.text = "Event ends at " + event_end;
                        }
                    }
                    else if (event_state == CityEventState.Finished)
                    {
                        ___m_labelEventTimeLeft.text = "Event ended";
                    }
                }
                var buttonStartEvent = ___m_panelEventInactive.Find<UIButton>("ButtonStartEvent");
                if (buttonStartEvent != null)
                {
                    buttonStartEvent.text = "Schedule";
                }
            }

            [HarmonyPatch(typeof(HotelWorldInfoPanel), "SelectEvent")]
            [HarmonyPostfix]
            private static void SelectEvent(HotelWorldInfoPanel __instance, int index, ref UILabel ___m_labelEventDuration)
            {
                if (___m_labelEventDuration && ___m_labelEventDuration.text.Contains("days"))
                {
                    ___m_labelEventDuration.text = ___m_labelEventDuration.text.Replace("days", "hours");
                }
            }

            private static bool IsHotelEventActiveOrUpcoming(ushort buildingID, ref Building buildingData) => buildingData.m_eventIndex != 0 || RealTimeEventManager.GetCityEvent(buildingID) != null;
        }

        [HarmonyPatch]
        internal static class FestivalPanelPatch
        {
            [HarmonyPatch(typeof(FestivalPanel), "RefreshCurrentConcert")]
            [HarmonyPostfix]
            private static void RefreshCurrentConcert(UIPanel panel, EventData concert)
            {
                var current_concert = RealTimeEventManager.GetCityEvent(concert.m_building);
                if (current_concert != null)
                {
                    panel.Find<UILabel>("Date").text = current_concert.StartTime.ToString("dd/MM/yyyy HH:mm");
                }
            }

            [HarmonyPatch(typeof(FestivalPanel), "RefreshFutureConcert")]
            [HarmonyPostfix]
            private static void RefreshFutureConcert(UIPanel panel, EventManager.FutureEvent concert) => panel.Find<UILabel>("Date").text = concert.m_startTime.ToString("dd/MM/yyyy HH:mm");
        }


        [HarmonyPatch]
        private sealed class ZonedBuildingWorldInfoPanel_OnSetTarget
        {
            [HarmonyPatch(typeof(ZonedBuildingWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void Postfix()
            {
                if(ZonedBuildingOperationHoursUIPanel.m_uiMainPanel == null)
                {
                    ZonedBuildingOperationHoursUIPanel.Init();
                }
                ZonedBuildingOperationHoursUIPanel.RefreshData();

            }
        }

        [HarmonyPatch]
        private sealed class CityServiceWorldInfoPanel_OnSetTarget
        {
            [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void Postfix()
            {
                if (CityServiceOperationHoursUIPanel.m_uiMainPanel == null)
                {
                    CityServiceOperationHoursUIPanel.Init();
                }
                CityServiceOperationHoursUIPanel.RefreshData();
            }
        }
    }
}

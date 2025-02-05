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
    using UnityEngine;
    using System.Linq;
    using SkyTools.Localization;

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

        public static ILocalizationProvider localizationProvider { get; set; }


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
            private static BuildingOperationHoursUIPanel zonedBuildingOperationHoursUIPanel;

            private static UILabel s_hotelLabel;

            [HarmonyPatch(typeof(ZonedBuildingWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget()
            {
                if (zonedBuildingOperationHoursUIPanel == null)
                {
                    ZonedCreateUI();
                }
                zonedBuildingOperationHoursUIPanel.RefreshData();
            }

            [HarmonyPatch(typeof(ZonedBuildingWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void UpdateBindings()
            {
                // Currently selected building.
                ushort building = WorldInfoPanel.GetCurrentInstanceID().Building;

                // Create hotel label if it isn't already set up.
                if (s_hotelLabel == null)
                {
                    // Get info panel.
                    var infoPanel = UIView.library.Get<ZonedBuildingWorldInfoPanel>(typeof(ZonedBuildingWorldInfoPanel).Name);

                    // Add current visitor count label.
                    s_hotelLabel = UiUtils.CreateLabel(infoPanel.component, 65f, 280f, "Rooms Ocuppied", textScale: 0.75f);
                    s_hotelLabel.textColor = new Color32(185, 221, 254, 255);
                    s_hotelLabel.font = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault((UIFont f) => f.name == "OpenSans-Regular");

                    // Position under existing Highly Educated workers count row in line with total workplace count label.
                    var situationLabel = infoPanel.Find("WorkSituation");
                    var workerLabel = infoPanel.Find("HighlyEducatedWorkers");
                    if (situationLabel != null && workerLabel != null)
                    {
                        s_hotelLabel.absolutePosition = new Vector2(situationLabel.absolutePosition.x + 200f, workerLabel.absolutePosition.y + 25f);
                    }
                    else
                    {
                        Debug.Log("couldn't find ZonedBuildingWorldInfoPanel components");
                    }
                }

                // Local references.
                var buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                var buildingData = buildingBuffer[building];
                var buildingInfo = buildingData.Info;

                // Is this a hotel building?
                if (buildingInfo.GetAI() is CommercialBuildingAI && BuildingManagerConnection.IsHotel(building))
                {
                    // Hotel show the label
                    s_hotelLabel.Show();

                    // Display hotel rooms ocuppied count out of max hotel rooms.
                    s_hotelLabel.text = buildingData.m_roomUsed + " / " + buildingData.m_roomMax + " Rooms";

                }
                else
                {
                    // Not a hotel hide the label
                    s_hotelLabel.Hide();
                }
            }

            private static void ZonedCreateUI()
            {
                var m_zonedBuildingWorldInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();
                var makeHistoricalPanel = m_zonedBuildingWorldInfoPanel.Find("MakeHistoricalPanel").GetComponent<UIPanel>();
                if (makeHistoricalPanel == null)
                {
                    return;
                }
                zonedBuildingOperationHoursUIPanel = new BuildingOperationHoursUIPanel(m_zonedBuildingWorldInfoPanel, makeHistoricalPanel, localizationProvider);
            }
        }

        [HarmonyPatch]
        private sealed class CityServiceWorldInfoPanel_OnSetTarget
        {
            private static BuildingOperationHoursUIPanel cityServiceOperationHoursUIPanel;

            [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget()
            {
                if (cityServiceOperationHoursUIPanel == null)
                {
                    CityServiceCreateUI();
                }
                cityServiceOperationHoursUIPanel.RefreshData();
            }

            private static void CityServiceCreateUI()
            {
                var m_cityServiceWorldInfoPanel = GameObject.Find("(Library) CityServiceWorldInfoPanel").GetComponent<CityServiceWorldInfoPanel>();
                var wrapper = m_cityServiceWorldInfoPanel?.Find("Wrapper");
                var mainSectionPanel = wrapper?.Find("MainSectionPanel");
                var mainBottom = mainSectionPanel?.Find("MainBottom");
                var buttonPanels = mainBottom?.Find("ButtonPanels").GetComponent<UIPanel>();
                if (buttonPanels == null)
                {
                    return;
                }
                cityServiceOperationHoursUIPanel = new BuildingOperationHoursUIPanel(m_cityServiceWorldInfoPanel, buttonPanels, localizationProvider);
            }

        }
    }
}

// HotelWorldInfoPanelPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using ColossalFramework.UI;
    using RealTime.GameConnection;
    using System;
    using RealTime.Events;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects for the hotel world info panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class HotelWorldInfoPanelPatch
    {
        public static TimeInfo TimeInfo { get; set; }

        public static RealTimeEventManager RealTimeEventManager { get; set; }

        [HarmonyPatch(typeof(HotelWorldInfoPanel), "UpdateBindings")]
        [HarmonyPostfix]
        private static void Postfix(HotelWorldInfoPanel __instance, ref InstanceID ___m_InstanceID, ref UILabel ___m_labelEventTimeLeft, ref UIPanel ___m_panelEventInactive)
        {
            ushort building = ___m_InstanceID.Building;
            var hotel_event = RealTimeEventManager.GetCityEvent(building);
            var event_state = RealTimeEventManager.GetEventState(building, DateTime.MaxValue);

            if(event_state == CityEventState.Upcoming)
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
            if (___m_labelEventDuration.text.Contains("days"))
            {
                ___m_labelEventDuration.text = ___m_labelEventDuration.text.Replace("days", "hours");
            }
        }

        private static bool IsHotelEventActiveOrUpcoming(ushort buildingID, ref Building buildingData) => buildingData.m_eventIndex != 0 || RealTimeEventManager.GetCityEvent(buildingID) != null;
    }
}

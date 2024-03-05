// HotelWorldInfoPanelPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using ColossalFramework.UI;
    using ColossalFramework;
    using RealTime.GameConnection;
    using System;
    using RealTime.Events;

    /// <summary>
    /// A static class that provides the patch objects for the hotel world info panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class HotelWorldInfoPanelPatch
    {
        public static TimeInfo TimeInfo { get; set; }

        public static RealTimeEventManager RealTimeEventManager { get; set; }

        [HarmonyPatch]
        private sealed class HotelWorldInfoPanel_UpdateBindings
        {
            [HarmonyPatch(typeof(HotelWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void Postfix(HotelWorldInfoPanel __instance, ref InstanceID ___m_InstanceID, ref UILabel ___m_labelEventTimeLeft, ref UIPanel ___m_panelEventActive, ref UIPanel ___m_panelEventInactive, ref UILabel ___m_labelEventStatus, ref UISprite ___m_overlay)
            {
                ushort building = ___m_InstanceID.Building;
                var instance = Singleton<BuildingManager>.instance;
                var buffer = instance.m_buildings.m_buffer;
                var info = buffer[building].Info;
                var hotelAI = (HotelAI)info.m_buildingAI;
                if (hotelAI.hasEvents)
                {
                    bool flag = IsHotelEventActiveOrUpcoming(building, ref buffer[building]);
                    ___m_panelEventActive.isVisible = flag;
                    ___m_panelEventInactive.isVisible = !flag;
                    if (flag)
                    {
                        var hotel_event = RealTimeEventManager.GetCityEvent(building);
                        var hotel_state = RealTimeEventManager.GetEventState(building, DateTime.MaxValue);

                        if (hotel_state == CityEventState.Upcoming)
                        {
                            string event_start = hotel_event.StartTime.ToString("HH:mm");
                            if (hotel_event.StartTime.Date < TimeInfo.Now.Date)
                            {
                                event_start = hotel_event.StartTime.ToString("dd/MM/yyyy HH:mm");
                                ___m_labelEventTimeLeft.text = "Event starts at " + event_start;
                            }
                            ___m_labelEventTimeLeft.text = "Event starts at " + event_start;
                        }
                        else if (hotel_state == CityEventState.Ongoing)
                        {
                            string event_end = hotel_event.EndTime.ToString("dd/MM/yyyy HH:mm");
                            ___m_labelEventTimeLeft.text = "Event ends at " + event_end;
                        }
                        else if (hotel_state == CityEventState.Finished)
                        {
                            ___m_labelEventTimeLeft.text = "Event ended";
                        }
                        ___m_labelEventStatus.text = hotel_state.ToString();
                        var event_data = Singleton<EventManager>.instance.m_events.m_buffer[buffer[building].m_eventIndex];
                        var hotelAdvertisementAI = event_data.Info.m_eventAI as HotelAdvertisementAI;
                        if (hotelAdvertisementAI != null)
                        {
                            ___m_overlay.isVisible = true;
                            ___m_overlay.spriteName = hotelAdvertisementAI.m_overlaySprite;
                        }
                    }
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
}

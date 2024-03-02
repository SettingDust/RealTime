// HotelWorldInfoPanelPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using ColossalFramework.UI;
    using ColossalFramework;
    using RealTime.GameConnection;

    /// <summary>
    /// A static class that provides the patch objects for the hotel world info panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class HotelWorldInfoPanelPatch
    {
        public static TimeInfo TimeInfo { get; set; }


        [HarmonyPatch]
        private sealed class HotelWorldInfoPanel_UpdateBindings
        {
            [HarmonyPatch(typeof(HotelWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void Postfix(HotelWorldInfoPanel __instance, ref InstanceID ___m_InstanceID, ref UILabel ___m_labelEventTimeLeft)
            {
                ushort building = ___m_InstanceID.Building;
                var instance = Singleton<BuildingManager>.instance;
                var buffer = instance.m_buildings.m_buffer;
                var event_buffer = Singleton<EventManager>.instance.m_events.m_buffer;
                var event_data = event_buffer[buffer[building].m_eventIndex];

                if(event_data.StartTime.Date < TimeInfo.Now.Date)
                {
                    string event_start = event_data.StartTime.ToString("dd/MM/yyyy HH:mm");
                    ___m_labelEventTimeLeft.text = "Event starts at " + event_start;
                }
                else
                {
                    if ((event_data.m_flags & EventData.Flags.Preparing) != 0)
                    {
                        string event_start = event_data.StartTime.ToString("HH:mm");
                        ___m_labelEventTimeLeft.text = "Event starts at " + event_start;
                    }
                    else if ((event_data.m_flags & EventData.Flags.Active) != 0)
                    {
                        var event_end_time = Singleton<SimulationManager>.instance.FrameToTime(event_data.m_expireFrame);

                        if(TimeInfo.Now.Date < event_end_time.Date)
                        {
                            string event_end = Singleton<SimulationManager>.instance.FrameToTime(event_data.m_expireFrame).ToString("dd/MM/yyyy HH:mm");
                            ___m_labelEventTimeLeft.text = "Event ends at " + event_end;
                        }
                        else
                        {
                            string event_end = Singleton<SimulationManager>.instance.FrameToTime(event_data.m_expireFrame).ToString("HH:mm");
                            ___m_labelEventTimeLeft.text = "Event ends at " + event_end;
                        }
                    }
                    else
                    {
                        ___m_labelEventTimeLeft.text = "Event ended";
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
        }
    }
}

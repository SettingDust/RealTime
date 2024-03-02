// VarsitySportsArenaPanelPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using ColossalFramework.UI;
    using ColossalFramework;

    /// <summary>
    /// A static class that provides the patch objects for the varsity sports arena panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class VarsitySportsArenaPanelPatch
    {
        [HarmonyPatch(typeof(VarsitySportsArenaPanel), "RefreshPastMatches")]
        [HarmonyPostfix]
        private static void RefreshPastMatches(int eventIndex, EventData upcomingEvent, EventData currentEvent, ref UIPanel ___m_panelPastMatches)
        {
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
                    uILabel2.text = currentEvent.StartTime.ToString("dd/MM/yyyy HH:mm");
                }
            }
        }

        [HarmonyPatch(typeof(VarsitySportsArenaPanel), "RefreshNextMatchDates")]
        [HarmonyPostfix]
        private static void RefreshNextMatchDates(EventData upcomingEvent, EventData currentEvent, ref UILabel ___m_nextMatchDate) => ___m_nextMatchDate.text = currentEvent.StartTime.ToString("dd/MM/yyyy HH:mm");
    }
}

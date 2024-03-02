// FootballPanelPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using ColossalFramework.UI;
    using ColossalFramework;

    /// <summary>
    /// A static class that provides the patch objects for the football panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class FootballPanelPatch
    {
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
                ___m_nextMatchDate.text = data.StartTime.ToString("dd/MM/yyyy HH:mm");
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
                        uILabel2.text = data.StartTime.ToString("dd/MM/yyyy HH:mm");
                    }

                }
            }

        }
    }
}

// HotelWorldInfoPanelPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using ColossalFramework.UI;

    /// <summary>
    /// A static class that provides the patch objects for the hotel world info panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class HotelWorldInfoPanelPatch
    {
        [HarmonyPatch]
        private sealed class HotelWorldInfoPanel_UpdateBindings
        {
            [HarmonyPatch(typeof(HotelWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void Postfix(HotelWorldInfoPanel __instance, ref UILabel ___m_labelEventTimeLeft)
            {
                if(___m_labelEventTimeLeft.text.Contains("days"))
                {
                    ___m_labelEventTimeLeft.text = ___m_labelEventTimeLeft.text.Replace("days", "hours");
                }
                else if (___m_labelEventTimeLeft.text.Contains("day"))
                {
                    ___m_labelEventTimeLeft.text = ___m_labelEventTimeLeft.text.Replace("day", "hour");
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

namespace RealTime.Patches
{
    using System.Reflection;
    using HarmonyLib;
    using RealTime.Config;
    using RealTime.CustomAI;

    [HarmonyPatch]
    internal class EventAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the mod configuration.</summary>
        public static RealTimeConfig RealTimeConfig { get; set; }

        private delegate void CancelDelegate(EventAI __instance, ushort eventID, ref EventData data);
        private static readonly CancelDelegate Cancel = AccessTools.MethodDelegate<CancelDelegate>(typeof(EventAI).GetMethod("Cancel", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

        [HarmonyPatch(typeof(EventAI), "BuildingDeactivated")]
        [HarmonyPrefix]
        public static bool BuildingDeactivated(EventAI __instance, ushort eventID, ref EventData data)
        {
            if ((data.m_flags & (EventData.Flags.Completed | EventData.Flags.Cancelled)) == 0 && __instance.m_info.m_type != EventManager.EventType.AcademicYear && RealTimeBuildingAI != null && !RealTimeBuildingAI.IsEventWithinOperationHours(ref data))
            {
                Cancel(__instance, eventID, ref data);
            }
            return false;
        }

        [HarmonyPatch(typeof(EventAI), "CalculateExpireFrame")]
        [HarmonyPrefix]
        private static void CalculateExpireFrame(EventAI __instance, uint startFrame) => UpdateAcademicYear(__instance);

        [HarmonyPatch(typeof(AcademicYearAI), "GetYearProgress")]
        [HarmonyPrefix]
        private static void GetYearProgress(EventAI __instance, ushort eventID, ref EventData data) => UpdateAcademicYear(__instance);

        [HarmonyPatch(typeof(EventAI), "GetDebugString")]
        [HarmonyPrefix]
        private static void GetDebugString(EventAI __instance, ushort eventID, ref EventData data) => UpdateAcademicYear(__instance);

        [HarmonyPatch(typeof(EventAI), "GetDaysLeft")]
        [HarmonyPrefix]
        private static void GetDaysLeft(EventAI __instance, ushort eventID, ref EventData data) => UpdateAcademicYear(__instance);

        [HarmonyPatch(typeof(EventAI), "GetDisorganizingEndFrame")]
        [HarmonyPrefix]
        private static void GetDisorganizingEndFrame(EventAI __instance, ushort eventID, ref EventData data) => UpdateAcademicYear(__instance);

        [HarmonyPatch(typeof(EventAI), "GetEndFrame")]
        [HarmonyPrefix]
        private static void GetEndFrame(EventAI __instance, ushort eventID, ref EventData data) => UpdateAcademicYear(__instance);

        private static void UpdateAcademicYear(EventAI instance)
        {
            if(instance.m_info.GetAI() is AcademicYearAI)
            {
                instance.m_prepareDuration = 0;
                instance.m_disorganizeDuration = 0;
                instance.m_eventDuration = RealTimeConfig.AcademicYearLength * 24f;
            }
        }
    }
}

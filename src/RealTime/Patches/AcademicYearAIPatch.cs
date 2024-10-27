namespace RealTime.Patches
{
    using HarmonyLib;

    [HarmonyPatch]
    public static class AcademicYearAIPatch
    {
        [HarmonyPatch(typeof(AcademicYearAI), "GetYearProgress")]
        [HarmonyPrefix]
        public static bool GetYearProgress(ref float __result)
        {
            if (EventManagerPatch.last_year_ended)
            {
                __result = 100f;
                return false;
            }
            return true;
        }
    }
}

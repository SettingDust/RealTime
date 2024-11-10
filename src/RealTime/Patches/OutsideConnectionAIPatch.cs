// OutsideConnectionAIPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using RealTime.CustomAI;

    /// <summary>
    /// A static class that provides the patch objects for the outside connections AI.
    /// </summary>
    [HarmonyPatch]
    internal static class OutsideConnectionAIPatch
    {
        /// <summary>Gets or sets the spare time behavior simulation.</summary>
        public static ISpareTimeBehavior SpareTimeBehavior { get; set; }

        [HarmonyPatch]
        private sealed class OutsideConnectionAI_DummyTrafficProbability
        {
            [HarmonyPatch(typeof(OutsideConnectionAI), "DummyTrafficProbability")]
            [HarmonyPostfix]
            private static void Postfix(ref int __result)
            {
                if(SpareTimeBehavior != null)
                {
                    __result = SpareTimeBehavior.SetDummyTrafficProbability(__result);
                }
            }
        }

    }
}

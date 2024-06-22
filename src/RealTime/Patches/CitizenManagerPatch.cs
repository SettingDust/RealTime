// CitizenManagerPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework.Math;
    using HarmonyLib;
    using RealTime.Core;
    using RealTime.CustomAI;

    /// <summary>
    /// A static class that provides the patch objects for the game's citizen manager.
    /// </summary>
    [HarmonyPatch]
    internal static class CitizenManagerPatch
    {
        /// <summary>
        /// Gets or sets the implementation of the <see cref="INewCitizenBehavior"/> interface.
        /// </summary>
        public static INewCitizenBehavior NewCitizenBehavior { get; set; }

        private static void UpdateCitizenAge(uint citizenId)
        {
            ref var citizen = ref CitizenManager.instance.m_citizens.m_buffer[citizenId];
            citizen.Age = NewCitizenBehavior.AdjustCitizenAge(citizen.Age);
        }

        private static void UpdateCitizenEducation(uint citizenId)
        {
            ref var citizen = ref CitizenManager.instance.m_citizens.m_buffer[citizenId];
            var newEducation = NewCitizenBehavior.GetEducation(citizen.Age, citizen.EducationLevel);
            citizen.Education3 = newEducation == Citizen.Education.ThreeSchools;
            citizen.Education2 = newEducation == Citizen.Education.TwoSchools || newEducation == Citizen.Education.ThreeSchools;
            citizen.Education1 = newEducation != Citizen.Education.Uneducated;
        }

        [HarmonyPatch]
        private sealed class CitizenManager_CreateCitizen1
        {
            [HarmonyPatch(typeof(CitizenManager), "CreateCitizen",
                [typeof(uint), typeof(int), typeof(int), typeof(Randomizer)],
                [ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref]
            )]
            [HarmonyPostfix]
            private static void Postfix(ref uint citizen, bool __result)
            {
                if(!RealTimeCore.ApplyCitizenPatch)
                {
                    return;
                }

                if (__result && NewCitizenBehavior != null)
                {
                    // This method is called by the game in two cases only: a new child is born or a citizen joins the city.
                    // So we tailor the age here.
                    UpdateCitizenAge(citizen);
                    UpdateCitizenEducation(citizen);
                }
            }
        }

        [HarmonyPatch]
        private sealed class CitizenManager_CreateCitizen2
        {
            [HarmonyPatch(typeof(CitizenManager), "CreateCitizen",
                [typeof(uint), typeof(int), typeof(int), typeof(Randomizer), typeof(Citizen.Gender)],
                [ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal]
            )]
            [HarmonyPostfix]
            private static void Postfix(ref uint citizen, bool __result)
            {
                if(!RealTimeCore.ApplyCitizenPatch)
                {
                    return;
                }

                if (__result && NewCitizenBehavior != null)
                {
                    UpdateCitizenEducation(citizen);
                }
            }
        }
    }
}

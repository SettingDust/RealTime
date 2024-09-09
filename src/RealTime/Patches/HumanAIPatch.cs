// HumanAIPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using SkyTools.Tools;

    /// <summary>
    /// A static class that provides the patch objects for the Human AI.
    /// </summary>
    [HarmonyPatch]
    internal static class HumanAIPatch
    {
        /// <summary>Gets or sets the custom AI object for resident citizens.</summary>
        public static RealTimeResidentAI<ResidentAI, Citizen> RealTimeResidentAI { get; set; }

        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch]
        private sealed class HumanAI_StartMoving
        {
            [HarmonyPatch(typeof(HumanAI), "StartMoving",
                [typeof(uint), typeof(Citizen), typeof(ushort), typeof(ushort)],
                [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
            [HarmonyPrefix]
            private static bool Prefix(HumanAI __instance, uint citizenID, ref Citizen data, ushort sourceBuilding, ushort targetBuilding, ref bool __result)
            {
                if (RealTimeResidentAI == null)
                {
                    return true;
                }
                if (__instance is ResidentAI)
                {
                    var instance = Singleton<CitizenManager>.instance;
                    ref var schedule = ref RealTimeResidentAI.GetCitizenSchedule(citizenID);
                    schedule.FindVisitPlaceAttempts = 0;
                    if (targetBuilding != 0 && targetBuilding != sourceBuilding && schedule.WorkBuilding == targetBuilding && schedule.WorkStatus == WorkStatus.Working)
                    {
                        if (sourceBuilding == 0)
                        {
                            sourceBuilding = data.GetBuildingByLocation();
                        }
                        if (sourceBuilding != 0)
                        {
                            Log.Debug(LogCategory.Movement, $"{citizenID} is going from {sourceBuilding} to work {targetBuilding}");

                            if (instance.CreateCitizenInstance(out ushort instance2, ref Singleton<SimulationManager>.instance.m_randomizer, __instance.m_info, citizenID))
                            {
                                __instance.m_info.m_citizenAI.SetSource(instance2, ref instance.m_instances.m_buffer[instance2], sourceBuilding);
                                __instance.m_info.m_citizenAI.SetTarget(instance2, ref instance.m_instances.m_buffer[instance2], targetBuilding);
                                data.CurrentLocation = Citizen.Location.Moving;
                                __result = true;
                                return false;
                            }
                        }
                    }
                }    
                __result = false;
                return true;
            }

            [HarmonyPatch(typeof(HumanAI), "StartMoving",
                [typeof(uint), typeof(Citizen), typeof(ushort), typeof(ushort)],
                [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
            [HarmonyPostfix]
            private static void Postfix(HumanAI __instance, uint citizenID, bool __result)
            {
                if (__result && __instance is ResidentAI && citizenID != 0 && RealTimeResidentAI != null)
                {
                    RealTimeResidentAI.RegisterCitizenDeparture(citizenID);
                }
            }
        }

        [HarmonyPatch]
        private sealed class HumanAI_ArriveAtTarget
        {
            [HarmonyPatch(typeof(HumanAI), "ArriveAtTarget")]
            [HarmonyPostfix]
            private static void Postfix(HumanAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool __result)
            {
                if (__result && citizenData.m_citizen != 0 && RealTimeResidentAI != null && __instance is ResidentAI)
                {
                    RealTimeResidentAI.RegisterCitizenArrival(citizenData.m_citizen);
                }
            }
        }
    }
}

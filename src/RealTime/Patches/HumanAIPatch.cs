// <copyright file="HumanAIPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Patches
{
    using System;
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

        [HarmonyPatch]
        private sealed class HumanAI_StartMoving
        {
            [HarmonyPatch(typeof(HumanAI), "StartMoving",
                new Type[] { typeof(uint), typeof(Citizen), typeof(ushort), typeof(ushort) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
            [HarmonyPrefix]
            private static bool Prefix(HumanAI __instance, uint citizenID, ref Citizen data, ushort sourceBuilding, ushort targetBuilding, ref bool __result)
            {
                var instance = Singleton<CitizenManager>.instance;
                var schedule = RealTimeResidentAI.GetCitizenSchedule(citizenID);
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
                            Log.Debug(LogCategory.Movement, $"for {citizenID} __result is : {__result} ");
                            return false;
                        }
                    }
                }
                Log.Debug(LogCategory.Movement, $"for {citizenID} __result is : {__result} ");
                __result = false;
                return true;
            }
        }
    }
}

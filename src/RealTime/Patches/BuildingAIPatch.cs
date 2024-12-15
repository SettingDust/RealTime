// BuildingAIPatch.cs

namespace RealTime.Patches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using ColossalFramework;
    using ColossalFramework.Math;
    using HarmonyLib;
    using ICities;
    using RealTime.Config;
    using RealTime.Core;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Simulation;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects for the building AI game methods.
    /// </summary>
    ///
    [HarmonyPatch]
    internal static class BuildingAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the weather information service.</summary>
        public static IWeatherInfo WeatherInfo { get; set; }

        /// <summary>Gets or sets the custom AI object for resident citizens.</summary>
        public static RealTimeResidentAI<ResidentAI, Citizen> RealTimeResidentAI { get; set; }

        [HarmonyPatch]
        private sealed class CommercialBuildingAI_SimulationStepActive
        {
            [HarmonyPatch(typeof(CommercialBuildingAI), "SimulationStepActive")]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> TranspileSimulationStepActive(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var IsBuildingWorkingInstance = AccessTools.PropertyGetter(typeof(BuildingAIPatch), nameof(RealTimeBuildingAI));
                var IsBuildingWorking = typeof(RealTimeBuildingAI).GetMethod("IsBuildingWorking", BindingFlags.Public | BindingFlags.Instance);
                var inst = new List<CodeInstruction>(instructions);

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].opcode == OpCodes.Ldfld &&
                        inst[i].operand == typeof(Building).GetField("m_flags") &&
                        inst[i + 1].opcode == OpCodes.Ldc_I4 &&
                        inst[i + 1].operand is int s &&
                        s == 32768 &&
                        inst[i - 1].opcode == OpCodes.Ldarg_2)
                    {
                        inst.InsertRange(i - 1, [
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brtrue, inst[i + 3].operand),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Stloc_S, 10)
                        ]);
                        break;
                    }
                }

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].opcode == OpCodes.Ldfld && inst[i].operand == typeof(Building).GetField("m_fireIntensity"))
                    {
                        inst.InsertRange(i + 2, [
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brfalse, inst[i + 1].operand)
                        ]);
                    }
                }

                return inst;
            }

            [HarmonyPatch(typeof(CommercialBuildingAI), "SimulationStepActive")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_outgoingProblemTimer;
                if (buildingData.m_customBuffer2 > 0)
                {
                    // Simulate some goods become spoiled; additionally, this will cause the buildings to never reach the 'stock full' state.
                    // In that state, no visits are possible anymore, so the building gets stuck
                    --buildingData.m_customBuffer2;
                }

                return true;
            }

            [HarmonyPatch(typeof(CommercialBuildingAI), "SimulationStepActive")]
            [HarmonyPostfix]
            private static void Postfix(CommercialBuildingAI __instance, ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_outgoingProblemTimer && RealTimeBuildingAI != null)
                {
                    RealTimeBuildingAI.ProcessBuildingProblems(buildingID, __state);
                }
                if (BuildingManagerConnection.IsHotel(buildingID))
                {
                    int aliveCount = 0;
                    int hotelTotalCount = 0;
                    Citizen.BehaviourData behaviour = default;
                    GetHotelBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveCount, ref hotelTotalCount);
                    buildingData.m_roomUsed = (ushort)hotelTotalCount;
                    Singleton<DistrictManager>.instance.m_districts.m_buffer[0].m_hotelData.m_tempHotelVisitors += (uint)hotelTotalCount;
                }
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID) && Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Hotels))
                {
                    float radius = Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_commertialBuilding.m_radius + (float)(buildingData.m_width + buildingData.m_length) * 0.25f;
                    int rate = Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_commertialBuilding.m_attraction * buildingData.m_width * buildingData.m_length;
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Shopping, rate, buildingData.m_position, radius);
                }
            }

            private static void GetHotelBehaviour(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
            {
                var instance = Singleton<CitizenManager>.instance;
                uint num = buildingData.m_citizenUnits;
                int num2 = 0;
                while (num != 0)
                {
                    if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Hotel) != 0)
                    {
                        instance.m_units.m_buffer[num].GetCitizenHotelBehaviour(ref behaviour, ref aliveCount, ref totalCount);
                    }
                    num = instance.m_units.m_buffer[num].m_nextUnit;
                    if (++num2 > 524288)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
            }
        }

        [HarmonyPatch]
        private sealed class IndustrialBuildingAI_SimulationStepActive
        {
            [HarmonyPatch(typeof(IndustrialBuildingAI), "SimulationStepActive")]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> TranspileSimulationStepActive(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var IsBuildingWorkingInstance = AccessTools.PropertyGetter(typeof(BuildingAIPatch), nameof(RealTimeBuildingAI));
                var IsBuildingWorking = typeof(RealTimeBuildingAI).GetMethod("IsBuildingWorking", BindingFlags.Public | BindingFlags.Instance);
                var inst = new List<CodeInstruction>(instructions);

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].opcode == OpCodes.Ldfld &&
                        inst[i].operand == typeof(Building).GetField("m_flags") &&
                        inst[i + 1].opcode == OpCodes.Ldc_I4 &&
                        inst[i + 1].operand is int s &&
                        s == 32768 &&
                        inst[i - 1].opcode == OpCodes.Ldarg_2)
                    {
                        inst.InsertRange(i - 1, [
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brtrue, inst[i + 3].operand),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Stloc_S, 9)
                        ]);
                        break;
                    }
                }

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].opcode == OpCodes.Ldfld && inst[i].operand == typeof(Building).GetField("m_fireIntensity"))
                    {
                        inst.InsertRange(i + 2, [
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brfalse, inst[i + 1].operand)
                        ]);
                    }
                }

                return inst;
            }

            [HarmonyPatch(typeof(IndustrialBuildingAI), "SimulationStepActive")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_outgoingProblemTimer;
                return true;
            }

            [HarmonyPatch(typeof(IndustrialBuildingAI), "SimulationStepActive")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_outgoingProblemTimer && RealTimeBuildingAI != null)
                {
                    RealTimeBuildingAI.ProcessBuildingProblems(buildingID, __state);
                }
            }
        }

        [HarmonyPatch]
        private sealed class IndustrialExtractorAI_SimulationStepActive
        {
            [HarmonyPatch(typeof(IndustrialExtractorAI), "SimulationStepActive")]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> TranspileSimulationStepActive(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var IsBuildingWorkingInstance = AccessTools.PropertyGetter(typeof(BuildingAIPatch), nameof(RealTimeBuildingAI));
                var IsBuildingWorking = typeof(RealTimeBuildingAI).GetMethod("IsBuildingWorking", BindingFlags.Public | BindingFlags.Instance);
                var inst = new List<CodeInstruction>(instructions);

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].opcode == OpCodes.Ldfld &&
                        inst[i].operand == typeof(Building).GetField("m_flags") &&
                        inst[i + 1].opcode == OpCodes.Ldc_I4 &&
                        inst[i + 1].operand is int s &&
                        s == 32768 &&
                        inst[i - 1].opcode == OpCodes.Ldarg_2)
                    {
                        inst.InsertRange(i - 1, [
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brtrue, inst[i + 3].operand),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Stloc_S, 9)
                        ]);
                        break;
                    }
                }

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].opcode == OpCodes.Ldfld && inst[i].operand == typeof(Building).GetField("m_fireIntensity"))
                    {
                        inst.InsertRange(i + 2, [
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brfalse, inst[i + 1].operand)
                        ]);
                    }
                }

                return inst;
            }

            [HarmonyPatch(typeof(IndustrialExtractorAI), "SimulationStepActive")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_outgoingProblemTimer;
                return true;
            }

            [HarmonyPatch(typeof(IndustrialExtractorAI), "SimulationStepActive")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_outgoingProblemTimer && RealTimeBuildingAI != null)
                {
                    RealTimeBuildingAI.ProcessBuildingProblems(buildingID, __state);
                }
            }
        }

        [HarmonyPatch]
        private sealed class OfficeBuildingAI_SimulationStepActive
        {
            [HarmonyPatch(typeof(OfficeBuildingAI), "SimulationStepActive")]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> TranspileSimulationStepActive(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var IsBuildingWorkingInstance = AccessTools.PropertyGetter(typeof(BuildingAIPatch), nameof(RealTimeBuildingAI));
                var IsBuildingWorking = typeof(RealTimeBuildingAI).GetMethod("IsBuildingWorking", BindingFlags.Public | BindingFlags.Instance);
                var inst = new List<CodeInstruction>(instructions);

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].opcode == OpCodes.Ldfld &&
                        inst[i].operand == typeof(Building).GetField("m_flags") &&
                        inst[i + 1].opcode == OpCodes.Ldc_I4 &&
                        inst[i + 1].operand is int s &&
                        s == 32768 &&
                        inst[i - 1].opcode == OpCodes.Ldarg_2)
                    {
                        inst.InsertRange(i - 1, [
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brtrue, inst[i + 3].operand),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Stloc_S, 11)
                        ]);
                        break;
                    }
                }

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].opcode == OpCodes.Ldfld && inst[i].operand == typeof(Building).GetField("m_fireIntensity"))
                    {
                        inst.InsertRange(i + 2, [
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brfalse, inst[i + 1].operand)
                        ]);
                    }
                }

                return inst;
            }

            [HarmonyPatch(typeof(OfficeBuildingAI), "SimulationStepActive")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_outgoingProblemTimer;
                return true;
            }

            [HarmonyPatch(typeof(OfficeBuildingAI), "SimulationStepActive")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_outgoingProblemTimer && RealTimeBuildingAI != null)
                {
                    RealTimeBuildingAI.ProcessBuildingProblems(buildingID, __state);
                }
                if(!RealTimeBuildingAI.IsBuildingWorking(buildingID) && Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Hotels))
                {
                    float radius = Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_officeBuilding.m_radius + (float)(buildingData.m_width + buildingData.m_length) * 0.25f;
                    int rate = Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_officeBuilding.m_attraction * buildingData.m_width * buildingData.m_length;
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Business, rate, buildingData.m_position, radius);
                }
            }
        }

        [HarmonyPatch]
        private sealed class PlayerBuildingAI_SimulationStepActive
        {
            [HarmonyPatch(typeof(PlayerBuildingAI), "SimulationStepActive")]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> TranspileSimulationStepActive(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var IsBuildingWorkingInstance = AccessTools.PropertyGetter(typeof(BuildingAIPatch), nameof(RealTimeBuildingAI));
                var IsBuildingWorking = typeof(RealTimeBuildingAI).GetMethod("IsBuildingWorking", BindingFlags.Public | BindingFlags.Instance);
                var IsSchoolBuilding = typeof(RealTimeBuildingAI).GetMethod("IsSchoolBuilding", BindingFlags.Public | BindingFlags.Instance);
                var inst = new List<CodeInstruction>(instructions);

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].opcode == OpCodes.Ldfld &&
                        inst[i].operand == typeof(Building).GetField("m_flags") &&
                        inst[i + 1].opcode == OpCodes.Ldc_I4 &&
                        inst[i + 1].operand is int s &&
                        s == 32768 &&
                        inst[i - 1].opcode == OpCodes.Ldarg_2)
                    {
                        inst.InsertRange(i - 1, [
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ceq),
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Call, IsSchoolBuilding),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ceq),
                            new(OpCodes.And),
                            new(OpCodes.Brfalse, inst[i + 3].operand),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Stloc_1)
                        ]);
                        break;
                    }
                }

                return inst;
            }
        }

        [HarmonyPatch]
        private sealed class MarketAI_SimulationStep
        {
            [HarmonyPatch(typeof(MarketAI), "SimulationStep")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_outgoingProblemTimer;
                if (buildingData.m_customBuffer2 > 0)
                {
                    // Simulate some goods become spoiled; additionally, this will cause the buildings to never reach the 'stock full' state.
                    // In that state, no visits are possible anymore, so the building gets stuck
                    --buildingData.m_customBuffer2;
                }

                return true;
            }

            [HarmonyPatch(typeof(MarketAI), "SimulationStep")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_outgoingProblemTimer && RealTimeBuildingAI != null)
                {
                    RealTimeBuildingAI.ProcessBuildingProblems(buildingID, __state);
                }
            }
        }

        [HarmonyPatch]
        private sealed class CommonBuildingAI_SimulationStepActive
        {
            private delegate bool CanEvacuateDelegate(CommonBuildingAI __instance);
            private static readonly CanEvacuateDelegate CanEvacuate = AccessTools.MethodDelegate<CanEvacuateDelegate>(typeof(CommonBuildingAI).GetMethod("CanEvacuate", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

            private delegate void SetEvacuatingDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building data, bool evacuating);
            private static readonly SetEvacuatingDelegate SetEvacuating = AccessTools.MethodDelegate<SetEvacuatingDelegate>(typeof(CommonBuildingAI).GetMethod("SetEvacuating", BindingFlags.Instance | BindingFlags.NonPublic), null, true);


            [HarmonyPatch(typeof(CommonBuildingAI), "SimulationStepActive")]
            [HarmonyPrefix]
            private static bool Prefix(CommonBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
            {
                if(RealTimeBuildingAI != null && !RealTimeBuildingAI.WeeklyPickupsOnly())
                {
                    return true;
                }
                var problemStruct = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.Garbage);
                if (buildingData.m_garbageBuffer >= 40000)
                {
                    int num = buildingData.m_garbageBuffer / 20000;
                    if (Singleton<SimulationManager>.instance.m_randomizer.Int32(5u) == 0)
                    {
                        num = UniqueFacultyAI.DecreaseByBonus(UniqueFacultyAI.FacultyBonus.Science, num);
                        Singleton<NaturalResourceManager>.instance.TryDumpResource(NaturalResourceManager.Resource.Pollution, num, num, buildingData.m_position, 0f);
                    }
                    int num2 = (!(__instance is MainCampusBuildingAI) && !(__instance is ParkGateAI) && !(__instance is MainIndustryBuildingAI)) ? 3 : 4;
                    if (num >= num2)
                    {
                        if (Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Garbage))
                        {
                            int num3 = (__instance is not MainCampusBuildingAI && __instance is not ParkGateAI && __instance is not MainIndustryBuildingAI) ? 6 : 8;
                            problemStruct = (num < num3) ? Notification.AddProblems(problemStruct, Notification.Problem1.Garbage) : Notification.AddProblems(problemStruct, Notification.Problem1.Garbage | Notification.Problem1.MajorProblem);
                            var properties = Singleton<GuideManager>.instance.m_properties;
                            if (properties is not null)
                            {
                                int publicServiceIndex = ItemClass.GetPublicServiceIndex(ItemClass.Service.Garbage);
                                Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex].Activate(properties.m_serviceNeeded, ItemClass.Service.Garbage);
                            }
                        }
                        else
                        {
                            buildingData.m_garbageBuffer = 4000;
                        }
                    }
                }
                buildingData.m_problems = problemStruct;
                float radius = (buildingData.Width + buildingData.Length) * 2.5f;
                if (buildingData.m_crimeBuffer != 0)
                {
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.CrimeRate, buildingData.m_crimeBuffer, buildingData.m_position, radius);
                }
                if (__instance.GetFireParameters(buildingID, ref buildingData, out int fireHazard, out int fireSize, out int fireTolerance))
                {
                    var instance = Singleton<DistrictManager>.instance;
                    byte district = instance.GetDistrict(buildingData.m_position);
                    var servicePolicies = instance.m_districts.m_buffer[district].m_servicePolicies;
                    var cityPlanningPolicies = instance.m_districts.m_buffer[district].m_cityPlanningPolicies;
                    if ((servicePolicies & DistrictPolicies.Services.SmokeDetectors) != 0)
                    {
                        fireHazard = fireHazard * 75 / 100;
                    }
                    if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.LightningRods) != 0)
                    {
                        Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 10, __instance.m_info.m_class);
                    }
                }
                fireHazard = 100 - (10 + fireTolerance) * 50000 / ((100 + fireHazard) * (100 + fireSize));
                if (fireHazard > 0)
                {
                    fireHazard = fireHazard * buildingData.Width * buildingData.Length;
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.FireHazard, fireHazard, buildingData.m_position, radius);
                }
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.FirewatchCoverage, 50, buildingData.m_position, 100f);
                if (Singleton<DisasterManager>.instance.IsEvacuating(buildingData.m_position))
                {
                    if ((buildingData.m_flags & Building.Flags.Evacuating) == 0 && CanEvacuate(__instance))
                    {
                        Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.RadioCoverage, buildingData.m_position, out int local);
                        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(100u) < local + 10)
                        {
                            SetEvacuating(__instance, buildingID, ref buildingData, evacuating: true);
                        }
                    }
                }
                else if ((buildingData.m_flags & Building.Flags.Evacuating) != 0)
                {
                    SetEvacuating(__instance, buildingID, ref buildingData, evacuating: false);
                }
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_HandleWorkers
        {
            [HarmonyPatch(typeof(PrivateBuildingAI), "HandleWorkers")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_workerProblemTimer;
                return true;
            }

            [HarmonyPatch(typeof(PrivateBuildingAI), "HandleWorkers")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_workerProblemTimer && RealTimeBuildingAI != null)
                {
                    RealTimeBuildingAI.ProcessWorkerProblems(buildingID, __state);
                }
            }
        }

        [HarmonyPatch]
        public sealed class CommonBuildingAI_RenderGarbageBins
        {
            [HarmonyPatch(typeof(CommonBuildingAI), "RenderGarbageBins")]
            [HarmonyPrefix]
            public static bool RenderGarbageBins(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
            {
                int renderBins = 10000;
                if(RealTimeBuildingAI != null && !RealTimeBuildingAI.WeeklyPickupsOnly())
                {
                    renderBins = 1000;
                }
                if (data.m_garbageBuffer < renderBins)
                {
                    return false;
                }
                var r = new Randomizer(buildingID);
                var randomPropInfo = Singleton<PropManager>.instance.GetRandomPropInfo(ref r, ItemClass.Service.Garbage);
                if (randomPropInfo is null || (layerMask & (1 << randomPropInfo.m_prefabDataLayer)) == 0 || randomPropInfo.m_requireHeightMap)
                {
                    return false;
                }
                int num = Mathf.Min(8, data.m_garbageBuffer / renderBins);
                int width = data.Width;
                int length = data.Length;
                Vector3 vector = default;
                for (int i = 0; i < num; i++)
                {
                    var variation = randomPropInfo.GetVariation(ref r);
                    float scale = variation.m_minScale + r.Int32(10000u) * (variation.m_maxScale - variation.m_minScale) * 0.0001f;
                    float angle = r.Int32(10000u) * 0.0006283185f;
                    var color = variation.GetColor(ref r);
                    vector.x = (r.Int32(10000u) * 0.0001f - 0.5f) * width * 4f;
                    vector.y = 0f;
                    vector.z = r.Int32(10000u) * 0.0001f - 0.5f + length * 4f;
                    vector = instance.m_dataMatrix0.MultiplyPoint(vector);
                    vector.y = instance.m_extraData.GetUShort(64 + i) * (1f / 64f);
                    if (cameraInfo.CheckRenderDistance(vector, variation.m_maxRenderDistance))
                    {
                        var objectIndex = new Vector4(0.001953125f, 0.0026041667f, 0f, 0f);
                        InstanceID id = default;
                        id.Building = buildingID;
                        PropInstance.RenderInstance(cameraInfo, variation, id, vector, scale, angle, color, objectIndex, (data.m_flags & Building.Flags.Active) != 0);
                    }
                }
                return false;
            }

        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_GetConstructionTime
        {
            [HarmonyPatch(typeof(PrivateBuildingAI), "GetConstructionTime")]
            [HarmonyPrefix]
            private static bool Prefix(ref int __result)
            {
                if(RealTimeBuildingAI != null)
                {
                    __result = RealTimeBuildingAI.GetConstructionTime();
                }
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class BuildingAI_CalculateUnspawnPosition
        {
            [HarmonyPatch(typeof(BuildingAI), "CalculateUnspawnPosition",
                [typeof(ushort), typeof(Building), typeof(Randomizer), typeof(CitizenInfo), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(Vector2), typeof(CitizenInstance.Flags)],
                [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out])]
            [HarmonyPostfix]
            private static void Postfix(BuildingAI __instance, ushort buildingID, ref Building data, ref Randomizer randomizer, CitizenInfo info, ref Vector3 position, ref Vector3 target, ref CitizenInstance.Flags specialFlags)
            {
                if (WeatherInfo != null && !WeatherInfo.IsBadWeather || data.Info == null || data.Info.m_enterDoors == null)
                {
                    return;
                }

                var enterDoors = data.Info.m_enterDoors;
                bool doorFound = false;
                for (int i = 0; i < enterDoors.Length; ++i)
                {
                    var prop = enterDoors[i].m_finalProp;
                    if (prop == null)
                    {
                        continue;
                    }

                    if (prop.m_doorType == PropInfo.DoorType.Enter || prop.m_doorType == PropInfo.DoorType.Both)
                    {
                        doorFound = true;
                        break;
                    }
                }

                if (!doorFound)
                {
                    return;
                }

                __instance.CalculateSpawnPosition(buildingID, ref data, ref randomizer, info, out var spawnPosition, out var spawnTarget);

                position = spawnPosition;
                target = spawnTarget;
                specialFlags &= ~(CitizenInstance.Flags.HangAround | CitizenInstance.Flags.SittingDown);
            }
        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_GetUpgradeInfo
        {
            [HarmonyPatch(typeof(PrivateBuildingAI), "GetUpgradeInfo")]
            [HarmonyPrefix]
            private static bool Prefix(ushort buildingID, ref Building data, ref BuildingInfo __result)
            {
                if (!RealTimeCore.ApplyBuildingPatch)
                {
                    return true;
                }

                if ((data.m_flags & Building.Flags.Upgrading) != 0)
                {
                    return true;
                }

                if (RealTimeBuildingAI != null && !RealTimeBuildingAI.CanBuildOrUpgrade(data.Info.GetService(), buildingID))
                {
                    __result = null;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch]
        private sealed class BuildingManager_CreateBuilding
        {
            [HarmonyPatch(typeof(BuildingManager), "CreateBuilding")]
            [HarmonyPrefix]
            private static bool Prefix(BuildingInfo info, ref bool __result)
            {
                if (!RealTimeCore.ApplyBuildingPatch)
                {
                    return true;
                }

                if (RealTimeBuildingAI != null && !RealTimeBuildingAI.CanBuildOrUpgrade(info.GetService()))
                {
                    __result = false;
                    return false;
                }

                return true;
            }

            [HarmonyPatch(typeof(BuildingManager), "CreateBuilding")]
            [HarmonyPostfix]
            private static void Postfix(ushort building, BuildingInfo info, ref bool __result)
            {
                if (!RealTimeCore.ApplyBuildingPatch)
                {
                    return;
                }

                if (__result && RealTimeBuildingAI != null)
                {
                    RealTimeBuildingAI.RegisterConstructingBuilding(building, info.GetService());
                }
            }
        }

        [HarmonyPatch]
        private sealed class WaterFacilityAI_ProduceGoods
        {
            [HarmonyPatch(typeof(WaterFacilityAI), "ProduceGoods")]
            [HarmonyPrefix]
            private static void Prefix(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
            {
                if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    buildingData.m_productionRate = 0;
                }
            }
        }

        [HarmonyPatch]
        private sealed class PowerPlantAI_ProduceGoods
        {
            [HarmonyPatch(typeof(PowerPlantAI), "ProduceGoods")]
            [HarmonyPrefix]
            private static bool Prefix(ushort buildingID, ref Building buildingData)
            {
                if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class CommonBuildingAI_GetColor
        {
            [HarmonyPatch(typeof(CommonBuildingAI), "GetColor")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
            {
                var negativeColor = InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.TrafficRoutes].m_negativeColor;
                var targetColor = InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.TrafficRoutes].m_targetColor;
                switch (infoMode)
                {
                    case InfoManager.InfoMode.Garbage:
                        __result = Color.Lerp(Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_targetColor, Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_negativeColor, Mathf.Min(100, data.m_garbageBuffer / 500) * 0.01f);
                        return;

                    case InfoManager.InfoMode.TrafficRoutes:
                        float f = 0;
                        if(RealTimeBuildingAI != null)
                        {
                            f = RealTimeBuildingAI.GetBuildingReachingTroubleFactor(buildingID);
                        }
                        __result = Color.Lerp(negativeColor, targetColor, f);
                        return;

                    case InfoManager.InfoMode.Wind:
                        if (RealTimeBuildingAI != null)
                        {
                            __result = RealTimeBuildingAI.IsBuildingWorking(buildingID) ? Color.green : Color.red;
                        }
                        return;

                    case InfoManager.InfoMode.NaturalResources:
                        if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
                        {
                            var instance = Singleton<CitizenManager>.instance;
                            var instance1 = Singleton<BuildingManager>.instance;
                            uint units = instance1.m_buildings.m_buffer[buildingID].m_citizenUnits;
                            int num = 0;
                            while (units != 0)
                            {
                                uint nextUnit = instance.m_units.m_buffer[units].m_nextUnit;
                                for (int i = 0; i < 5; i++)
                                {
                                    uint citizenId = instance.m_units.m_buffer[units].GetCitizen(i);
                                    var citizen = instance.m_citizens.m_buffer[citizenId];
                                    if (citizenId != 0U && citizen.CurrentLocation != Citizen.Location.Moving && citizen.GetBuildingByLocation() == buildingID)
                                    {
                                        __result = Color.blue;
                                        return;
                                    }
                                }
                                units = nextUnit;
                                if (++num > 524288)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                    break;
                                }
                            }
                            __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                        }
                        return;

                    case InfoManager.InfoMode.None:
                        if (RealTimeBuildingAI != null && RealTimeBuildingAI.ShouldSwitchBuildingLightsOff(buildingID))
                        {
                            __result.a = 0;
                        }
                        return;
                }
            }
        }

        [HarmonyPatch]
        private sealed class SchoolAI_GetCurrentRange
        {
            [HarmonyPatch(typeof(SchoolAI), "GetCurrentRange")]
            [HarmonyPrefix]
            private static bool Prefix(SchoolAI __instance, ushort buildingID, ref Building data, ref float __result)
            {
                if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    int num = data.m_productionRate;
                    if ((data.m_flags & (Building.Flags.Evacuating)) != 0)
                    {
                        num = 0;
                    }
                    else if ((data.m_flags & Building.Flags.RateReduced) != 0)
                    {
                        num = Mathf.Min(num, 50);
                    }
                    int budget = Singleton<EconomyManager>.instance.GetBudget(__instance.m_info.m_class);
                    num = PlayerBuildingAI.GetProductionRate(num, budget);
                    __result = num * __instance.m_educationRadius * 0.01f;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class LibraryAI_GetCurrentRange
        {
            [HarmonyPatch(typeof(LibraryAI), "GetCurrentRange",
                [typeof(ushort), typeof(Building), typeof(float)],
                [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
            [HarmonyPrefix]
            private static bool Prefix(LibraryAI __instance, ushort buildingID, ref Building data, float radius, ref float __result)
            {
                if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    int num = data.m_productionRate;
                    if ((data.m_flags & (Building.Flags.Evacuating)) != 0)
                    {
                        num = 0;
                    }
                    else if ((data.m_flags & Building.Flags.RateReduced) != 0)
                    {
                        num = Mathf.Min(num, 50);
                    }
                    int budget = Singleton<EconomyManager>.instance.GetBudget(__instance.m_info.m_class);
                    num = PlayerBuildingAI.GetProductionRate(num, budget);
                    __result = (float)num * radius * 0.01f;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class FishingHarborAI_TrySpawnBoat
        {
            [HarmonyPatch(typeof(FishingHarborAI), "TrySpawnBoat")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData) => (buildingData.m_flags & Building.Flags.Active) != 0;
        }

        [HarmonyPatch]
        private sealed class CommonBuildingAI_HandleCommonConsumption
        {
            private delegate bool CanStockpileElectricityDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building data, out int stockpileAmount, out int stockpileRate);
            private static readonly CanStockpileElectricityDelegate CanStockpileElectricity = AccessTools.MethodDelegate<CanStockpileElectricityDelegate>(typeof(CommonBuildingAI).GetMethod("CanStockpileElectricity", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

            private delegate bool CanStockpileWaterDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building data, out int stockpileAmount, out int stockpileRate);
            private static readonly CanStockpileWaterDelegate CanStockpileWater = AccessTools.MethodDelegate<CanStockpileWaterDelegate>(typeof(CommonBuildingAI).GetMethod("CanStockpileWater", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

            private delegate bool CanSufferFromFloodDelegate(CommonBuildingAI __instance, out bool onlyCollapse);
            private static readonly CanSufferFromFloodDelegate CanSufferFromFlood = AccessTools.MethodDelegate<CanSufferFromFloodDelegate>(typeof(CommonBuildingAI).GetMethod("CanSufferFromFlood", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

            private delegate int GetCollapseTimeDelegate(CommonBuildingAI __instance);
            private static readonly GetCollapseTimeDelegate GetCollapseTime = AccessTools.MethodDelegate<GetCollapseTimeDelegate>(typeof(CommonBuildingAI).GetMethod("GetCollapseTime", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

            private delegate void RemovePeopleDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building data, int killPercentage);
            private static readonly RemovePeopleDelegate RemovePeople = AccessTools.MethodDelegate<RemovePeopleDelegate>(typeof(CommonBuildingAI).GetMethod("RemovePeople", BindingFlags.Instance | BindingFlags.NonPublic), null, false);



            [HarmonyPatch(typeof(CommonBuildingAI), "HandleCommonConsumption")]
            [HarmonyPrefix]
            public static bool HandleCommonConsumption(CommonBuildingAI __instance, ushort buildingID, ref Building data, ref Building.Frame frameData, ref int electricityConsumption, ref int heatingConsumption, ref int waterConsumption, ref int sewageAccumulation, ref int garbageAccumulation, ref int mailAccumulation, int maxMail, DistrictPolicies.Services policies, ref int __result)
            {
                if (RealTimeBuildingAI != null && !RealTimeBuildingAI.WeeklyPickupsOnly())
                {
                    return true;
                }
                if ((data.m_flags & Building.Flags.Active) == 0)
                {
                    garbageAccumulation = 0;
                    mailAccumulation = 0;
                }
                int num = 100;
                var instance = Singleton<DistrictManager>.instance;
                var problemStruct = Notification.RemoveProblems(data.m_problems, Notification.Problem1.Electricity | Notification.Problem1.Water | Notification.Problem1.Sewage | Notification.Problem1.Flood | Notification.Problem1.Heating);
                bool flag = data.m_electricityProblemTimer != 0;
                bool flag2 = false;
                bool flag3 = false;
                int electricityUsage = 0;
                int heatingUsage = 0;
                int waterUsage = 0;
                int sewageUsage = 0;
                if (electricityConsumption != 0)
                {
                    electricityConsumption = UniqueFacultyAI.DecreaseByBonus(UniqueFacultyAI.FacultyBonus.Science, electricityConsumption);
                    int value = Mathf.RoundToInt((20f - Singleton<WeatherManager>.instance.SampleTemperature(data.m_position, ignoreWeather: false)) * 8f);
                    value = Mathf.Clamp(value, 0, 400);
                    int num2 = heatingConsumption;
                    heatingConsumption = (num2 * value + Singleton<SimulationManager>.instance.m_randomizer.Int32(100u)) / 100;
                    if ((policies & DistrictPolicies.Services.PowerSaving) != 0)
                    {
                        electricityConsumption = Mathf.Max(1, electricityConsumption * 90 / 100);
                        Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 32, __instance.m_info.m_class);
                    }
                    bool connected = false;
                    int num3 = heatingConsumption * 2 - data.m_heatingBuffer;
                    if (num3 > 0 && (policies & DistrictPolicies.Services.OnlyElectricity) == 0)
                    {
                        int num4 = Singleton<WaterManager>.instance.TryFetchHeating(data.m_position, heatingConsumption, num3, out connected);
                        data.m_heatingBuffer += (ushort)num4;
                    }
                    if (data.m_heatingBuffer < heatingConsumption)
                    {
                        if ((policies & DistrictPolicies.Services.NoElectricity) != 0)
                        {
                            flag3 = true;
                            data.m_heatingProblemTimer = (byte)Mathf.Min(255, data.m_heatingProblemTimer + 1);
                            if (data.m_heatingProblemTimer >= 65)
                            {
                                num = 0;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Heating | Notification.Problem1.MajorProblem);
                            }
                            else if (data.m_heatingProblemTimer >= 3)
                            {
                                num /= 2;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Heating);
                            }
                        }
                        else
                        {
                            value = ((value + 50) * (heatingConsumption - data.m_heatingBuffer) + heatingConsumption - 1) / heatingConsumption;
                            electricityConsumption += (num2 * value + Singleton<SimulationManager>.instance.m_randomizer.Int32(100u)) / 100;
                            if (connected)
                            {
                                flag3 = true;
                                data.m_heatingProblemTimer = (byte)Mathf.Min(255, data.m_heatingProblemTimer + 1);
                                if (data.m_heatingProblemTimer >= 3)
                                {
                                    problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Heating);
                                }
                            }
                        }
                        heatingUsage = data.m_heatingBuffer;
                        data.m_heatingBuffer = 0;
                    }
                    else
                    {
                        heatingUsage = heatingConsumption;
                        data.m_heatingBuffer -= (ushort)heatingConsumption;
                    }
                    if (CanStockpileElectricity(__instance, buildingID, ref data, out int stockpileAmount, out int stockpileRate))
                    {
                        num3 = stockpileAmount + electricityConsumption * 2 - data.m_electricityBuffer;
                        if (num3 > 0)
                        {
                            int num5 = electricityConsumption;
                            if (data.m_electricityBuffer < stockpileAmount)
                            {
                                num5 += Mathf.Min(stockpileRate, stockpileAmount - data.m_electricityBuffer);
                            }
                            int num6 = Singleton<ElectricityManager>.instance.TryFetchElectricity(data.m_position, num5, num3);
                            data.m_electricityBuffer += (ushort)num6;
                            if (num6 < num3 && num6 < num5)
                            {
                                flag2 = true;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Electricity);
                                if (data.m_electricityProblemTimer < 64)
                                {
                                    data.m_electricityProblemTimer = 64;
                                }
                            }
                        }
                    }
                    else
                    {
                        num3 = electricityConsumption * 2 - data.m_electricityBuffer;
                        if (num3 > 0)
                        {
                            int num7 = Singleton<ElectricityManager>.instance.TryFetchElectricity(data.m_position, electricityConsumption, num3);
                            data.m_electricityBuffer += (ushort)num7;
                        }
                    }
                    if (data.m_electricityBuffer < electricityConsumption)
                    {
                        flag2 = true;
                        data.m_electricityProblemTimer = (byte)Mathf.Min(255, data.m_electricityProblemTimer + 1);
                        if (data.m_electricityProblemTimer >= 65)
                        {
                            num = 0;
                            problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Electricity | Notification.Problem1.MajorProblem);
                        }
                        else if (data.m_electricityProblemTimer >= 3)
                        {
                            num /= 2;
                            problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Electricity);
                        }
                        electricityUsage = data.m_electricityBuffer;
                        data.m_electricityBuffer = 0;
                        if (Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Electricity))
                        {
                            var properties = Singleton<GuideManager>.instance.m_properties;
                            if (properties != null)
                            {
                                int publicServiceIndex = ItemClass.GetPublicServiceIndex(ItemClass.Service.Electricity);
                                int electricityCapacity = instance.m_districts.m_buffer[0].GetElectricityCapacity();
                                int electricityConsumption2 = instance.m_districts.m_buffer[0].GetElectricityConsumption();
                                if (electricityCapacity >= electricityConsumption2)
                                {
                                    Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex].Activate(properties.m_serviceNeeded2, ItemClass.Service.Electricity);
                                }
                                else
                                {
                                    Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex].Activate(properties.m_serviceNeeded, ItemClass.Service.Electricity);
                                }
                            }
                        }
                    }
                    else
                    {
                        electricityUsage = electricityConsumption;
                        data.m_electricityBuffer -= (ushort)electricityConsumption;
                    }
                }
                else
                {
                    heatingConsumption = 0;
                }
                if (!flag2)
                {
                    data.m_electricityProblemTimer = 0;
                }
                if (flag != flag2)
                {
                    Singleton<BuildingManager>.instance.UpdateBuildingColors(buildingID);
                }
                if (!flag3)
                {
                    data.m_heatingProblemTimer = 0;
                }
                bool flag4 = false;
                sewageAccumulation = UniqueFacultyAI.DecreaseByBonus(UniqueFacultyAI.FacultyBonus.Engineering, sewageAccumulation);
                int num8 = sewageAccumulation;
                if (waterConsumption != 0)
                {
                    waterConsumption = UniqueFacultyAI.DecreaseByBonus(UniqueFacultyAI.FacultyBonus.Engineering, waterConsumption);
                    if ((policies & DistrictPolicies.Services.WaterSaving) != 0)
                    {
                        waterConsumption = Mathf.Max(1, waterConsumption * 85 / 100);
                        if (sewageAccumulation != 0)
                        {
                            sewageAccumulation = Mathf.Max(1, sewageAccumulation * 85 / 100);
                        }
                        Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 32, __instance.m_info.m_class);
                    }
                    if (CanStockpileWater(__instance, buildingID, ref data, out int stockpileAmount2, out int stockpileRate2))
                    {
                        int num9 = stockpileAmount2 + waterConsumption * 2 - data.m_waterBuffer;
                        if (num9 > 0)
                        {
                            int num10 = waterConsumption;
                            if (data.m_waterBuffer < stockpileAmount2)
                            {
                                num10 += Mathf.Min(stockpileRate2, stockpileAmount2 - data.m_waterBuffer);
                            }
                            int num11 = Singleton<WaterManager>.instance.TryFetchWater(data.m_position, num10, num9, ref data.m_waterPollution);
                            data.m_waterBuffer += (ushort)num11;
                            if (num11 < num9 && num11 < num10)
                            {
                                flag4 = true;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Water);
                                if (data.m_waterProblemTimer < 64)
                                {
                                    data.m_waterProblemTimer = 64;
                                }
                            }
                        }
                    }
                    else
                    {
                        int num12 = waterConsumption * 2 - data.m_waterBuffer;
                        if (num12 > 0)
                        {
                            int num13 = Singleton<WaterManager>.instance.TryFetchWater(data.m_position, waterConsumption, num12, ref data.m_waterPollution);
                            data.m_waterBuffer += (ushort)num13;
                        }
                    }
                    if (data.m_waterBuffer < waterConsumption)
                    {
                        flag4 = true;
                        data.m_waterProblemTimer = (byte)Mathf.Min(255, data.m_waterProblemTimer + 1);
                        if (data.m_waterProblemTimer >= 65)
                        {
                            num = 0;
                            problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Water | Notification.Problem1.MajorProblem);
                        }
                        else if (data.m_waterProblemTimer >= 3)
                        {
                            num /= 2;
                            problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Water);
                        }
                        num8 = sewageAccumulation * (waterConsumption + data.m_waterBuffer) / (waterConsumption << 1);
                        waterUsage = data.m_waterBuffer;
                        data.m_waterBuffer = 0;
                        if (Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Water))
                        {
                            var properties2 = Singleton<GuideManager>.instance.m_properties;
                            if (properties2 != null)
                            {
                                int publicServiceIndex2 = ItemClass.GetPublicServiceIndex(ItemClass.Service.Water);
                                int waterCapacity = instance.m_districts.m_buffer[0].GetWaterCapacity();
                                int waterConsumption2 = instance.m_districts.m_buffer[0].GetWaterConsumption();
                                if (waterCapacity >= waterConsumption2)
                                {
                                    Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex2].Activate(properties2.m_serviceNeeded2, ItemClass.Service.Water);
                                }
                                else
                                {
                                    Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex2].Activate(properties2.m_serviceNeeded, ItemClass.Service.Water);
                                }
                            }
                        }
                    }
                    else
                    {
                        num8 = sewageAccumulation;
                        waterUsage = waterConsumption;
                        data.m_waterBuffer -= (ushort)waterConsumption;
                    }
                }
                if (CanStockpileWater(__instance, buildingID, ref data, out int stockpileAmount3, out int stockpileRate3))
                {
                    int num14 = Mathf.Max(0, stockpileAmount3 + num8 * 2 - data.m_sewageBuffer);
                    if (num14 < num8)
                    {
                        if (!flag4 && (data.m_problems & Notification.Problem1.Water).IsNone)
                        {
                            flag4 = true;
                            data.m_waterProblemTimer = (byte)Mathf.Min(255, data.m_waterProblemTimer + 1);
                            if (data.m_waterProblemTimer >= 65)
                            {
                                num = 0;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Sewage | Notification.Problem1.MajorProblem);
                            }
                            else if (data.m_waterProblemTimer >= 3)
                            {
                                num /= 2;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Sewage);
                            }
                        }
                        sewageUsage = num14;
                        data.m_sewageBuffer = (ushort)(stockpileAmount3 + num8 * 2);
                    }
                    else
                    {
                        sewageUsage = num8;
                        data.m_sewageBuffer += (ushort)num8;
                    }
                    int num15 = num8 + Mathf.Max(num8, stockpileRate3);
                    num14 = Mathf.Min(num15, data.m_sewageBuffer);
                    if (num14 > 0)
                    {
                        int num16 = Singleton<WaterManager>.instance.TryDumpSewage(data.m_position, num15, num14);
                        data.m_sewageBuffer -= (ushort)num16;
                        if (num16 < num15 && num16 < num14 && !flag4 && (data.m_problems & Notification.Problem1.Water).IsNone)
                        {
                            flag4 = true;
                            problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Sewage);
                            if (data.m_waterProblemTimer < 64)
                            {
                                data.m_waterProblemTimer = 64;
                            }
                        }
                    }
                }
                else if (num8 != 0)
                {
                    int num17 = Mathf.Max(0, num8 * 2 - data.m_sewageBuffer);
                    if (num17 < num8)
                    {
                        if (!flag4 && (data.m_problems & Notification.Problem1.Water).IsNone)
                        {
                            flag4 = true;
                            data.m_waterProblemTimer = (byte)Mathf.Min(255, data.m_waterProblemTimer + 1);
                            if (data.m_waterProblemTimer >= 65)
                            {
                                num = 0;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Sewage | Notification.Problem1.MajorProblem);
                            }
                            else if (data.m_waterProblemTimer >= 3)
                            {
                                num /= 2;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Sewage);
                            }
                        }
                        sewageUsage = num17;
                        data.m_sewageBuffer = (ushort)(num8 * 2);
                    }
                    else
                    {
                        sewageUsage = num8;
                        data.m_sewageBuffer += (ushort)num8;
                    }
                    num17 = Mathf.Min(num8 * 2, data.m_sewageBuffer);
                    if (num17 > 0)
                    {
                        int num18 = Singleton<WaterManager>.instance.TryDumpSewage(data.m_position, num8 * 2, num17);
                        data.m_sewageBuffer -= (ushort)num18;
                    }
                }
                if (!flag4)
                {
                    data.m_waterProblemTimer = 0;
                }
                garbageAccumulation = UniqueFacultyAI.DecreaseByBonus(UniqueFacultyAI.FacultyBonus.Environment, garbageAccumulation);
                if (garbageAccumulation != 0)
                {
                    int num19 = 65535 - data.m_garbageBuffer;
                    if (num19 <= garbageAccumulation)
                    {
                        num /= 2;
                        data.m_garbageBuffer = ushort.MaxValue;
                    }
                    else
                    {
                        data.m_garbageBuffer += (ushort)garbageAccumulation;
                    }
                }
                int garbageBuffer = data.m_garbageBuffer;
                if (garbageBuffer >= 20000 && Singleton<SimulationManager>.instance.m_randomizer.Int32(5u) == 0 && Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Garbage))
                {
                    int count = 0;
                    int cargo = 0;
                    int capacity = 0;
                    int outside = 0;
                    __instance.CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.Garbage, ref count, ref cargo, ref capacity, ref outside);
                    garbageBuffer -= capacity - cargo;
                    if (garbageBuffer >= 200)
                    {
                        TransferManager.TransferOffer offer = default;
                        offer.Priority = garbageBuffer / 1000;
                        offer.Building = buildingID;
                        offer.Position = data.m_position;
                        offer.Amount = 1;
                        Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Garbage, offer);
                    }
                }
                if (mailAccumulation != 0)
                {
                    if ((policies & DistrictPolicies.Services.FreeWifi) != 0)
                    {
                        mailAccumulation = (mailAccumulation * 17 + Singleton<SimulationManager>.instance.m_randomizer.Int32(80u)) / 80;
                        if ((buildingID & (Singleton<SimulationManager>.instance.m_currentFrameIndex >> 8) & (true ? 1u : 0u)) != 0)
                        {
                            Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 13, __instance.m_info.m_class);
                        }
                        else
                        {
                            Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 12, __instance.m_info.m_class);
                        }
                    }
                    else
                    {
                        mailAccumulation = mailAccumulation + Singleton<SimulationManager>.instance.m_randomizer.Int32(4u) >> 2;
                    }
                }
                if (mailAccumulation != 0)
                {
                    int num20 = Mathf.Min(maxMail, 65535) - data.m_mailBuffer;
                    if (num20 <= mailAccumulation)
                    {
                        data.m_mailBuffer = (ushort)Mathf.Min(maxMail, 65535);
                    }
                    else
                    {
                        data.m_mailBuffer += (ushort)mailAccumulation;
                    }
                }
                if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Industry) && Singleton<UnlockManager>.instance.Unlocked(ItemClass.SubService.PublicTransportPost) && maxMail != 0)
                {
                    int mailBuffer = data.m_mailBuffer;
                    if (mailBuffer >= maxMail / 4 && Singleton<SimulationManager>.instance.m_randomizer.Int32(5u) == 0)
                    {
                        int count2 = 0;
                        int cargo2 = 0;
                        int capacity2 = 0;
                        int outside2 = 0;
                        __instance.CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.Mail, ref count2, ref cargo2, ref capacity2, ref outside2);
                        mailBuffer -= capacity2 - cargo2;
                        if (mailBuffer >= maxMail / 4)
                        {
                            TransferManager.TransferOffer offer2 = default;
                            offer2.Priority = mailBuffer * 4 / maxMail;
                            offer2.Building = buildingID;
                            offer2.Position = data.m_position;
                            offer2.Amount = 1;
                            Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Mail, offer2);
                        }
                    }
                }
                if (CanSufferFromFlood(__instance, out bool onlyCollapse))
                {
                    float num21 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(data.m_position));
                    if (num21 > data.m_position.y)
                    {
                        bool flag5 = num21 > data.m_position.y + Mathf.Max(4f, __instance.m_info.m_collisionHeight) && (data.m_flags & Building.Flags.Untouchable) == 0;
                        if ((!onlyCollapse || flag5) && (data.m_flags & Building.Flags.Flooded) == 0 && data.m_fireIntensity == 0)
                        {
                            var instance2 = Singleton<DisasterManager>.instance;
                            ushort disasterIndex = instance2.FindDisaster<FloodBaseAI>(data.m_position);
                            if (disasterIndex == 0)
                            {
                                var disasterInfo = DisasterManager.FindDisasterInfo<GenericFloodAI>();
                                if (disasterInfo != null && instance2.CreateDisaster(out disasterIndex, disasterInfo))
                                {
                                    instance2.m_disasters.m_buffer[disasterIndex].m_intensity = 10;
                                    instance2.m_disasters.m_buffer[disasterIndex].m_targetPosition = data.m_position;
                                    disasterInfo.m_disasterAI.StartNow(disasterIndex, ref instance2.m_disasters.m_buffer[disasterIndex]);
                                }
                            }
                            if (disasterIndex != 0)
                            {
                                InstanceID srcID = default;
                                InstanceID dstID = default;
                                srcID.Disaster = disasterIndex;
                                dstID.Building = buildingID;
                                Singleton<InstanceManager>.instance.CopyGroup(srcID, dstID);
                                var info = instance2.m_disasters.m_buffer[disasterIndex].Info;
                                info.m_disasterAI.ActivateNow(disasterIndex, ref instance2.m_disasters.m_buffer[disasterIndex]);
                                if ((instance2.m_disasters.m_buffer[disasterIndex].m_flags & DisasterData.Flags.Significant) != 0)
                                {
                                    instance2.DetectDisaster(disasterIndex, located: false);
                                    instance2.FollowDisaster(disasterIndex);
                                }
                            }
                            data.m_flags |= Building.Flags.Flooded;
                        }
                        if (flag5)
                        {
                            frameData.m_constructState = (byte)Mathf.Max(0, frameData.m_constructState - 1088 / GetCollapseTime(__instance));
                            data.SetFrameData(Singleton<SimulationManager>.instance.m_currentFrameIndex, frameData);
                            InstanceID id = default;
                            id.Building = buildingID;
                            var group = Singleton<InstanceManager>.instance.GetGroup(id);
                            if (group != null)
                            {
                                ushort disaster = group.m_ownerInstance.Disaster;
                                if (disaster != 0)
                                {
                                    Singleton<DisasterManager>.instance.m_disasters.m_buffer[disaster].m_collapsedCount++;
                                }
                            }
                            if (frameData.m_constructState == 0)
                            {
                                Singleton<InstanceManager>.instance.SetGroup(id, null);
                            }
                            data.m_levelUpProgress = 0;
                            data.m_fireIntensity = 0;
                            data.m_garbageBuffer = 0;
                            data.m_flags = (data.m_flags & (Building.Flags.ContentMask | Building.Flags.IncomingOutgoing | Building.Flags.CapacityFull | Building.Flags.Created | Building.Flags.Deleted | Building.Flags.Original | Building.Flags.CustomName | Building.Flags.Untouchable | Building.Flags.FixedHeight | Building.Flags.RateReduced | Building.Flags.HighDensity | Building.Flags.RoadAccessFailed | Building.Flags.Evacuating | Building.Flags.Completed | Building.Flags.Active | Building.Flags.Abandoned | Building.Flags.Demolishing | Building.Flags.ZonesUpdated | Building.Flags.Downgrading | Building.Flags.Collapsed | Building.Flags.Upgrading | Building.Flags.SecondaryLoading | Building.Flags.Hidden | Building.Flags.EventActive | Building.Flags.Flooded | Building.Flags.Filling)) | Building.Flags.Collapsed;
                            num = 0;
                            RemovePeople(__instance, buildingID, ref data, 90);
                            __instance.BuildingDeactivated(buildingID, ref data);
                            if (__instance.m_info.m_hasParkingSpaces != 0)
                            {
                                Singleton<BuildingManager>.instance.UpdateParkingSpaces(buildingID, ref data);
                            }
                            Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, updateGroup: true);
                            Singleton<BuildingManager>.instance.UpdateBuildingColors(buildingID);
                            var properties3 = Singleton<GuideManager>.instance.m_properties;
                            if (properties3 != null)
                            {
                                Singleton<BuildingManager>.instance.m_buildingFlooded.Deactivate(buildingID, soft: false);
                                Singleton<BuildingManager>.instance.m_buildingFlooded2.Deactivate(buildingID, soft: false);
                            }
                            if (data.m_subBuilding != 0 && data.m_parentBuilding == 0)
                            {
                                int num22 = 0;
                                ushort subBuilding = data.m_subBuilding;
                                while (subBuilding != 0)
                                {
                                    var info2 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].Info;
                                    info2.m_buildingAI.CollapseBuilding(subBuilding, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding], group, testOnly: false, demolish: false, 0);
                                    subBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].m_subBuilding;
                                    if (++num22 > 49152)
                                    {
                                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                        break;
                                    }
                                }
                            }
                        }
                        else if (!onlyCollapse)
                        {
                            if ((data.m_flags & Building.Flags.RoadAccessFailed) == 0)
                            {
                                int count3 = 0;
                                int cargo3 = 0;
                                int capacity3 = 0;
                                int outside3 = 0;
                                __instance.CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.FloodWater, ref count3, ref cargo3, ref capacity3, ref outside3);
                                if (count3 == 0)
                                {
                                    TransferManager.TransferOffer offer3 = default;
                                    offer3.Priority = 5;
                                    offer3.Building = buildingID;
                                    offer3.Position = data.m_position;
                                    offer3.Amount = 1;
                                    Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.FloodWater, offer3);
                                }
                            }
                            if (num21 > data.m_position.y + 1f)
                            {
                                num = 0;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Flood | Notification.Problem1.MajorProblem);
                            }
                            else
                            {
                                num /= 2;
                                problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.Flood);
                            }
                            var properties4 = Singleton<GuideManager>.instance.m_properties;
                            if (properties4 != null)
                            {
                                if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.NaturalDisasters) && Singleton<UnlockManager>.instance.Unlocked(UnlockManager.Feature.WaterPumping))
                                {
                                    Singleton<BuildingManager>.instance.m_buildingFlooded2.Activate(properties4.m_buildingFlooded2, buildingID);
                                }
                                else
                                {
                                    Singleton<BuildingManager>.instance.m_buildingFlooded.Activate(properties4.m_buildingFlooded, buildingID);
                                }
                            }
                        }
                    }
                    else if ((data.m_flags & Building.Flags.Flooded) != 0)
                    {
                        InstanceID id2 = default;
                        id2.Building = buildingID;
                        Singleton<InstanceManager>.instance.SetGroup(id2, null);
                        data.m_flags &= ~Building.Flags.Flooded;
                    }
                }
                byte district = instance.GetDistrict(data.m_position);
                instance.m_districts.m_buffer[district].AddUsageData(electricityUsage, heatingUsage, waterUsage, sewageUsage);
                data.m_problems = problemStruct;
                __result = num;
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class FishFarmAI_GetColor
        {
            [HarmonyPatch(typeof(FishFarmAI), "GetColor")]
            [HarmonyPrefix]
            public static bool GetColor(FishFarmAI __instance, ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
            {
                if(infoMode == InfoManager.InfoMode.Fishing)
                {
                    if(data.m_productionRate > 0)
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor;
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class FishingHarborAI_GetColor
        {
            [HarmonyPatch(typeof(FishingHarborAI), "GetColor")]
            [HarmonyPrefix]
            public static bool GetColor(FishingHarborAI __instance, ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
            {
                if (infoMode == InfoManager.InfoMode.Fishing)
                {
                    if (data.m_productionRate > 0)
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor;
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class SchoolAI_GetColor
        {
            [HarmonyPatch(typeof(SchoolAI), "GetColor")]
            [HarmonyPrefix]
            public static bool GetColor(SchoolAI __instance, ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
            {
                if (infoMode == InfoManager.InfoMode.Education)
                {
                    var level = ItemClass.Level.None;
                    switch (subInfoMode)
                    {
                        case InfoManager.SubInfoMode.Default:
                            level = ItemClass.Level.Level1;
                            break;
                        case InfoManager.SubInfoMode.WaterPower:
                            level = ItemClass.Level.Level2;
                            break;
                        case InfoManager.SubInfoMode.WindPower:
                            level = ItemClass.Level.Level3;
                            break;
                    }
                    if (level == __instance.m_info.m_class.m_level && __instance.m_info.m_class.m_service == ItemClass.Service.Education)
                    {
                        if (data.m_productionRate > 0)
                        {
                            __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor;
                        }
                        else
                        {
                            __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                        }
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class LibraryAI_GetColor
        {
            [HarmonyPatch(typeof(LibraryAI), "GetColor")]
            [HarmonyPrefix]
            public static bool GetColor(LibraryAI __instance, ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
            {
                if (infoMode == InfoManager.InfoMode.Education && subInfoMode == InfoManager.SubInfoMode.LibraryEducation)
                {
                    if (data.m_productionRate > 0)
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor;
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                    }
                    return false;
                }
                if (infoMode == InfoManager.InfoMode.Entertainment && subInfoMode == InfoManager.SubInfoMode.PipeWater)
                {
                    if (data.m_productionRate > 0)
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor;
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class ParkAI_GetColor
        {
            [HarmonyPatch(typeof(ParkAI), "GetColor")]
            [HarmonyPrefix]
            public static bool GetColor(ParkAI __instance, ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
            {
                if (infoMode == InfoManager.InfoMode.Entertainment)
                {
                    if(subInfoMode == InfoManager.SubInfoMode.WaterPower)
                    {
                        if (data.m_productionRate > 0)
                        {
                            __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor;
                        }
                        else
                        {
                            __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                        }
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_CreateBuilding
        {
            private delegate void CommonBuildingAICreateBuildingDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building data);
            private static readonly CommonBuildingAICreateBuildingDelegate BaseCreateBuilding = AccessTools.MethodDelegate<CommonBuildingAICreateBuildingDelegate>(typeof(CommonBuildingAI).GetMethod("CreateBuilding", BindingFlags.Instance | BindingFlags.Public), null, false);

            [HarmonyPatch(typeof(PrivateBuildingAI), "CreateBuilding")]
            [HarmonyPrefix]
            public static bool Prefix(PrivateBuildingAI __instance, ushort buildingID, ref Building data)
            {
                var buildingInfo = data.Info;
                if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
                {
                    BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);

                    if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
                    {
                        var buildignPrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                        UpdateBuildingSettings.SetBuildingToPrefab(buildingID, buildignPrefab);
                    }
                    else if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo))
                    {
                        var buildignGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);
                        UpdateBuildingSettings.SetBuildingToGlobal(buildingID, buildignGlobal);
                    }
                }
                if (buildingInfo.GetAI() is CommercialBuildingAI && BuildingManagerConnection.IsHotel(buildingID))
                {
                    BaseCreateBuilding(__instance, buildingID, ref data);
                    data.m_level = (byte)__instance.m_info.m_class.m_level;
                    __instance.CalculateWorkplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length, out int level, out int level2, out int level3, out int level4);
                    __instance.AdjustWorkplaceCount(buildingID, ref data, ref level, ref level2, ref level3, ref level4);
                    int workCount = level + level2 + level3 + level4;
                    int visitCount = __instance.CalculateVisitplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length);
                    int hotelRoomCount = visitCount;
                    if (BuildingWorkTimeManager.HotelNamesList.ContainsKey(buildingInfo.name))
                    {
                        hotelRoomCount = BuildingWorkTimeManager.HotelNamesList[buildingInfo.name];
                    }
                    visitCount = hotelRoomCount * 20 / 100;
                    data.m_roomMax = (ushort)hotelRoomCount;
                    Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, 0, workCount, visitCount, 0, 0, hotelRoomCount);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_BuildingLoaded
        {
            [HarmonyPatch(typeof(PrivateBuildingAI), "BuildingLoaded")]
            [HarmonyPrefix]
            public static bool Prefix(PrivateBuildingAI __instance, ushort buildingID, ref Building data, uint version)
            {
                var buildingInfo = data.Info;
                if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
                {
                    BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);

                    if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
                    {
                        var buildignPrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                        UpdateBuildingSettings.SetBuildingToPrefab(buildingID, buildignPrefab);
                    }
                    else if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo))
                    {
                        var buildignGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);
                        UpdateBuildingSettings.SetBuildingToGlobal(buildingID, buildignGlobal);
                    }
                }
                if (buildingInfo.GetAI() is CommercialBuildingAI && BuildingManagerConnection.IsHotel(buildingID))
                {
                    data.m_level = (byte)Mathf.Max(data.m_level, (int)__instance.m_info.m_class.m_level);
                    __instance.CalculateWorkplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length, out int level, out int level2, out int level3, out int level4);
                    __instance.AdjustWorkplaceCount(buildingID, ref data, ref level, ref level2, ref level3, ref level4);
                    int workCount = level + level2 + level3 + level4;
                    int visitCount = __instance.CalculateVisitplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length);
                    int hotelRoomCount = visitCount;
                    if (BuildingWorkTimeManager.HotelNamesList.ContainsKey(buildingInfo.name))
                    {
                        hotelRoomCount = BuildingWorkTimeManager.HotelNamesList[buildingInfo.name];
                    }
                    visitCount = hotelRoomCount * 20 / 100;
                    EnsureCitizenUnits(buildingID, ref data, 0, workCount, visitCount, 0, hotelRoomCount);
                    data.m_roomMax = (ushort)hotelRoomCount;
                    return false;
                }
                else
                {
                    return true;
                }                
            }

            private static void EnsureCitizenUnits(ushort buildingID, ref Building data, int homeCount = 0, int workCount = 0, int visitCount = 0, int studentCount = 0, int hotelCount = 0)
            {
                if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.Collapsed)) != 0)
                {
                    return;
                }
                var wealthLevel = Citizen.GetWealthLevel((ItemClass.Level)data.m_level);
                var instance = Singleton<CitizenManager>.instance;
                uint num = 0u;
                uint num2 = data.m_citizenUnits;
                int num3 = 0;
                while (num2 != 0)
                {
                    var flags = instance.m_units.m_buffer[num2].m_flags;
                    if ((flags & CitizenUnit.Flags.Home) != 0)
                    {
                        instance.m_units.m_buffer[num2].SetWealthLevel(wealthLevel);
                        homeCount--;
                    }
                    if ((flags & CitizenUnit.Flags.Work) != 0)
                    {
                        workCount -= 5;
                    }
                    if ((flags & CitizenUnit.Flags.Visit) != 0)
                    {
                        visitCount -= 5;
                    }
                    if ((flags & CitizenUnit.Flags.Student) != 0)
                    {
                        studentCount -= 5;
                    }
                    if ((flags & CitizenUnit.Flags.Hotel) != 0)
                    {
                        hotelCount -= 5;
                    }
                    num = num2;
                    num2 = instance.m_units.m_buffer[num2].m_nextUnit;
                    if (++num3 > 524288)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
                homeCount = Mathf.Max(0, homeCount);
                workCount = Mathf.Max(0, workCount);
                visitCount = Mathf.Max(0, visitCount);
                studentCount = Mathf.Max(0, studentCount);
                hotelCount = Mathf.Max(0, hotelCount);
                if (homeCount == 0 && workCount == 0 && visitCount == 0 && studentCount == 0 && hotelCount == 0)
                {
                    return;
                }
                if (instance.CreateUnits(out uint firstUnit, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, homeCount, workCount, visitCount, 0, studentCount, hotelCount))
                {
                    if (num != 0)
                    {
                        instance.m_units.m_buffer[num].m_nextUnit = firstUnit;
                    }
                    else
                    {
                        data.m_citizenUnits = firstUnit;
                    }
                }
            }
        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_ReleaseBuilding
        {
            [HarmonyPatch(typeof(PrivateBuildingAI), "ReleaseBuilding")]
            [HarmonyPrefix]
            public static void Prefix(PrivateBuildingAI __instance, ushort buildingID, ref Building data) => BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingID);
        }

        [HarmonyPatch]
        private sealed class PlayerBuildingAI_CreateBuilding
        {
            [HarmonyPatch(typeof(PlayerBuildingAI), "CreateBuilding")]
            [HarmonyPrefix]
            public static void Prefix(PlayerBuildingAI __instance, ushort buildingID, ref Building data)
            {
                if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
                {
                    var buildingInfo = data.Info;
                    BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);

                    if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
                    {
                        var buildignPrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                        UpdateBuildingSettings.SetBuildingToPrefab(buildingID, buildignPrefab);
                    }
                    else if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo))
                    {
                        var buildignGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);
                        UpdateBuildingSettings.SetBuildingToGlobal(buildingID, buildignGlobal);
                    }
                }
            } 
        }

        [HarmonyPatch]
        private sealed class PlayerBuildingAI_BuildingLoaded
        {
            [HarmonyPatch(typeof(PlayerBuildingAI), "BuildingLoaded")]
            [HarmonyPrefix]
            public static void Prefix(PlayerBuildingAI __instance, ushort buildingID, ref Building data)
            {
                if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
                {
                    var buildingInfo = data.Info;
                    BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);

                    if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
                    {
                        var buildignPrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                        UpdateBuildingSettings.SetBuildingToPrefab(buildingID, buildignPrefab);
                    }
                    else if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo))
                    {
                        var buildignGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);
                        UpdateBuildingSettings.SetBuildingToGlobal(buildingID, buildignGlobal);
                    }
                }
            }
        }

        [HarmonyPatch]
        private sealed class PlayerBuildingAI_ReleaseBuilding
        {
            [HarmonyPatch(typeof(PlayerBuildingAI), "ReleaseBuilding")]
            [HarmonyPrefix]
            public static void Prefix(PlayerBuildingAI __instance, ushort buildingID, ref Building data)
            {
                if (BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID))
                {
                    BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingID);
                }   
            }
        }

        [HarmonyPatch]
        private sealed class CommonBuildingAI_HandleFire
        {
            private delegate void HandleFireSpreadDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building buildingData, int fireDamage);
            private static readonly HandleFireSpreadDelegate HandleFireSpread = AccessTools.MethodDelegate<HandleFireSpreadDelegate>(typeof(CommonBuildingAI).GetMethod("HandleFireSpread", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

            private delegate int GetCollapseTimeDelegate(CommonBuildingAI __instance);
            private static readonly GetCollapseTimeDelegate GetCollapseTime = AccessTools.MethodDelegate<GetCollapseTimeDelegate>(typeof(CommonBuildingAI).GetMethod("GetCollapseTime", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

            private delegate void RemovePeopleDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building data, int killPercentage);
            private static readonly RemovePeopleDelegate RemovePeople = AccessTools.MethodDelegate<RemovePeopleDelegate>(typeof(CommonBuildingAI).GetMethod("RemovePeople", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

            [HarmonyPatch(typeof(CommonBuildingAI), "HandleFire")]
            [HarmonyPrefix]
            public static bool HandleFire(CommonBuildingAI __instance, ushort buildingID, ref Building data, ref Building.Frame frameData, DistrictPolicies.Services policies)
            {
                if (__instance.GetFireParameters(buildingID, ref data, out int fireHazard, out int fireSize, out int fireTolerance) && (policies & DistrictPolicies.Services.SmokeDetectors) != 0)
                {
                    fireHazard = fireHazard * 75 / 100;
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 32, __instance.m_info.m_class);
                }
                if (fireHazard != 0 && data.m_fireIntensity == 0 && frameData.m_fireDamage == 0 && Singleton<SimulationManager>.instance.m_randomizer.Int32(8388608u) < fireHazard && Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.FireDepartment) && !Singleton<BuildingManager>.instance.m_firesDisabled)
                {
                    float num = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(data.m_position));
                    if (num <= data.m_position.y)
                    {
                        if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.NaturalDisasters))
                        {
                            var disasterInfo = DisasterManager.FindDisasterInfo<StructureFireAI>();
                            if (disasterInfo is object)
                            {
                                var instance = Singleton<DisasterManager>.instance;
                                if (instance.CreateDisaster(out ushort disasterIndex, disasterInfo))
                                {
                                    int num2 = Singleton<SimulationManager>.instance.m_randomizer.Int32(100u);
                                    num2 = 10 + num2 * num2 * num2 * num2 / 1055699;
                                    instance.m_disasters.m_buffer[disasterIndex].m_intensity = (byte)num2;
                                    instance.m_disasters.m_buffer[disasterIndex].m_targetPosition = data.m_position;
                                    disasterInfo.m_disasterAI.StartNow(disasterIndex, ref instance.m_disasters.m_buffer[disasterIndex]);
                                    disasterInfo.m_disasterAI.ActivateNow(disasterIndex, ref instance.m_disasters.m_buffer[disasterIndex]);
                                    InstanceID srcID = default;
                                    InstanceID dstID = default;
                                    srcID.Disaster = disasterIndex;
                                    dstID.Building = buildingID;
                                    Singleton<InstanceManager>.instance.CopyGroup(srcID, dstID);
                                    data.m_flags &= ~Building.Flags.Flooded;
                                    data.m_fireIntensity = (byte)fireSize;
                                    frameData.m_fireDamage = 133;
                                    __instance.BuildingDeactivated(buildingID, ref data);
                                    Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, updateGroup: true);
                                    Singleton<BuildingManager>.instance.UpdateBuildingColors(buildingID);
                                    Singleton<DisasterManager>.instance.m_disasters.m_buffer[disasterIndex].m_buildingFireCount++;
                                    if (data.m_subBuilding != 0 && data.m_parentBuilding == 0)
                                    {
                                        int num3 = 0;
                                        ushort subBuilding = data.m_subBuilding;
                                        while (subBuilding != 0)
                                        {
                                            var info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].Info;
                                            if (info.m_buildingAI.GetFireParameters(subBuilding, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding], out _, out _, out _))
                                            {
                                                dstID.Building = subBuilding;
                                                Singleton<InstanceManager>.instance.CopyGroup(srcID, dstID);
                                                Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].m_flags &= ~Building.Flags.Flooded;
                                                Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].m_fireIntensity = (byte)fireSize;
                                                var lastFrameData = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].GetLastFrameData();
                                                lastFrameData.m_fireDamage = 133;
                                                Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].SetLastFrameData(lastFrameData);
                                                info.m_buildingAI.BuildingDeactivated(subBuilding, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding]);
                                                Singleton<BuildingManager>.instance.UpdateBuildingRenderer(subBuilding, updateGroup: true);
                                                Singleton<BuildingManager>.instance.UpdateBuildingColors(subBuilding);
                                            }
                                            subBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].m_subBuilding;
                                            if (++num3 > 49152)
                                            {
                                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            data.m_flags &= ~Building.Flags.Flooded;
                            data.m_fireIntensity = (byte)fireSize;
                            frameData.m_fireDamage = 133;
                            __instance.BuildingDeactivated(buildingID, ref data);
                            Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, updateGroup: true);
                            Singleton<BuildingManager>.instance.UpdateBuildingColors(buildingID);
                            if (data.m_subBuilding != 0 && data.m_parentBuilding == 0)
                            {
                                int num4 = 0;
                                ushort subBuilding2 = data.m_subBuilding;
                                while (subBuilding2 != 0)
                                {
                                    var info2 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding2].Info;
                                    if (info2.m_buildingAI.GetFireParameters(subBuilding2, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding2], out _, out _, out _))
                                    {
                                        Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding2].m_flags &= ~Building.Flags.Flooded;
                                        Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding2].m_fireIntensity = (byte)fireSize;
                                        var lastFrameData2 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding2].GetLastFrameData();
                                        lastFrameData2.m_fireDamage = 133;
                                        Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding2].SetLastFrameData(lastFrameData2);
                                        info2.m_buildingAI.BuildingDeactivated(subBuilding2, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding2]);
                                        Singleton<BuildingManager>.instance.UpdateBuildingRenderer(subBuilding2, updateGroup: true);
                                        Singleton<BuildingManager>.instance.UpdateBuildingColors(subBuilding2);
                                    }
                                    subBuilding2 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding2].m_subBuilding;
                                    if (++num4 > 49152)
                                    {
                                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                if (data.m_fireIntensity != 0)
                {
                    int num5 = (fireTolerance == 0) ? 255 : ((data.m_fireIntensity + fireTolerance) / fireTolerance + 3 >> 2);
                    if (num5 != 0)
                    {
                        num5 = Singleton<SimulationManager>.instance.m_randomizer.Int32(1, num5);
                        frameData.m_fireDamage = (byte)Mathf.Min(frameData.m_fireDamage + num5, 255);
                        HandleFireSpread(__instance, buildingID, ref data, frameData.m_fireDamage);
                        if (data.m_subBuilding != 0 && data.m_parentBuilding == 0)
                        {
                            int num6 = 0;
                            ushort subBuilding3 = data.m_subBuilding;
                            while (subBuilding3 != 0)
                            {
                                var info3 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding3].Info;
                                if (info3.m_buildingAI.GetFireParameters(subBuilding3, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding3], out _, out _, out _))
                                {
                                    var lastFrameData3 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding3].GetLastFrameData();
                                    lastFrameData3.m_fireDamage = frameData.m_fireDamage;
                                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding3].SetLastFrameData(lastFrameData3);
                                }
                                subBuilding3 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding3].m_subBuilding;
                                if (++num6 > 49152)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                    break;
                                }
                            }
                        }
                        if(frameData.m_fireDamage >= 210 && RealTimeBuildingAI != null && !RealTimeBuildingAI.ShouldExtinguishFire(buildingID))
                        {
                            frameData.m_fireDamage = 150;
                        }
                        if (frameData.m_fireDamage == byte.MaxValue)
                        {
                            frameData.m_constructState = (byte)Mathf.Max(0, frameData.m_constructState - 1088 / GetCollapseTime(__instance));
                            data.SetFrameData(Singleton<SimulationManager>.instance.m_currentFrameIndex, frameData);
                            InstanceID id = default;
                            id.Building = buildingID;
                            var group = Singleton<InstanceManager>.instance.GetGroup(id);
                            if (group != null && (data.m_flags & Building.Flags.Collapsed) == 0)
                            {
                                ushort disaster = group.m_ownerInstance.Disaster;
                                if (disaster != 0)
                                {
                                    Singleton<DisasterManager>.instance.m_disasters.m_buffer[disaster].m_collapsedCount++;
                                }
                            }
                            if (frameData.m_constructState == 0)
                            {
                                Singleton<InstanceManager>.instance.SetGroup(id, null);
                            }
                            data.m_levelUpProgress = 0;
                            data.m_fireIntensity = 0;
                            data.m_garbageBuffer = 0;
                            data.m_flags = (data.m_flags & (Building.Flags.ContentMask | Building.Flags.IncomingOutgoing | Building.Flags.CapacityFull | Building.Flags.Created | Building.Flags.Deleted | Building.Flags.Original | Building.Flags.CustomName | Building.Flags.Untouchable | Building.Flags.FixedHeight | Building.Flags.RateReduced | Building.Flags.HighDensity | Building.Flags.RoadAccessFailed | Building.Flags.Evacuating | Building.Flags.Completed | Building.Flags.Active | Building.Flags.Abandoned | Building.Flags.Demolishing | Building.Flags.ZonesUpdated | Building.Flags.Downgrading | Building.Flags.Collapsed | Building.Flags.Upgrading | Building.Flags.SecondaryLoading | Building.Flags.Hidden | Building.Flags.EventActive | Building.Flags.Flooded | Building.Flags.Filling)) | Building.Flags.Collapsed;
                            RemovePeople(__instance, buildingID, ref data, 90);
                            __instance.BuildingDeactivated(buildingID, ref data);
                            if (__instance is CampusBuildingAI campusBuildingAI)
                            {
                                var instance2 = Singleton<DistrictManager>.instance;
                                byte area = campusBuildingAI.GetArea(buildingID, ref data);
                                instance2.m_parks.m_buffer[area].DeactivateCampusBuilding(campusBuildingAI.m_campusAttractiveness);
                            }
                            if (__instance.m_info.m_hasParkingSpaces != 0)
                            {
                                Singleton<BuildingManager>.instance.UpdateParkingSpaces(buildingID, ref data);
                            }
                            Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, updateGroup: true);
                            Singleton<BuildingManager>.instance.UpdateBuildingColors(buildingID);
                            var properties = Singleton<GuideManager>.instance.m_properties;
                            if (properties is object)
                            {
                                Singleton<BuildingManager>.instance.m_buildingOnFire.Deactivate(buildingID, soft: false);
                            }
                            if (data.m_subBuilding != 0 && data.m_parentBuilding == 0)
                            {
                                int num7 = 0;
                                ushort subBuilding4 = data.m_subBuilding;
                                while (subBuilding4 != 0)
                                {
                                    var info4 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4].Info;
                                    if (frameData.m_constructState == 0)
                                    {
                                        id.Building = subBuilding4;
                                        Singleton<InstanceManager>.instance.SetGroup(id, null);
                                    }
                                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4].m_levelUpProgress = 0;
                                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4].m_fireIntensity = 0;
                                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4].m_garbageBuffer = 0;
                                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4].m_flags |= Building.Flags.Collapsed;
                                    var lastFrameData4 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4].GetLastFrameData();
                                    lastFrameData4.m_constructState = frameData.m_constructState;
                                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4].SetLastFrameData(lastFrameData4);
                                    info4.m_buildingAI.BuildingDeactivated(subBuilding4, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4]);
                                    if (info4.m_hasParkingSpaces != 0)
                                    {
                                        Singleton<BuildingManager>.instance.UpdateParkingSpaces(subBuilding4, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4]);
                                    }
                                    Singleton<BuildingManager>.instance.UpdateBuildingRenderer(subBuilding4, updateGroup: true);
                                    Singleton<BuildingManager>.instance.UpdateBuildingColors(subBuilding4);
                                    Singleton<BuildingManager>.instance.UpdateFlags(subBuilding4, Building.Flags.Collapsed);
                                    subBuilding4 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding4].m_subBuilding;
                                    if (++num7 > 49152)
                                    {
                                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            float num8 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(data.m_position));
                            if (num8 > data.m_position.y + 1f)
                            {
                                InstanceID id2 = default;
                                id2.Building = buildingID;
                                Singleton<InstanceManager>.instance.SetGroup(id2, null);
                                data.m_fireIntensity = 0;
                                var flags = data.m_flags;
                                if (data.m_productionRate != 0 && (data.m_flags & Building.Flags.Evacuating) == 0)
                                {
                                    data.m_flags |= Building.Flags.Active;
                                }
                                var flags2 = data.m_flags;
                                Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, frameData.m_fireDamage == 0 || (data.m_flags & (Building.Flags.Abandoned | Building.Flags.Collapsed)) != 0);
                                Singleton<BuildingManager>.instance.UpdateBuildingColors(buildingID);
                                if (flags2 != flags)
                                {
                                    Singleton<BuildingManager>.instance.UpdateFlags(buildingID, flags2 ^ flags);
                                }
                                var properties2 = Singleton<GuideManager>.instance.m_properties;
                                if (properties2 is object)
                                {
                                    Singleton<BuildingManager>.instance.m_buildingOnFire.Deactivate(buildingID, soft: false);
                                }
                                if (data.m_subBuilding != 0 && data.m_parentBuilding == 0)
                                {
                                    int num9 = 0;
                                    ushort subBuilding5 = data.m_subBuilding;
                                    while (subBuilding5 != 0)
                                    {
                                        id2.Building = subBuilding5;
                                        Singleton<InstanceManager>.instance.SetGroup(id2, null);
                                        Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding5].m_fireIntensity = 0;
                                        Singleton<BuildingManager>.instance.UpdateBuildingRenderer(subBuilding5, updateGroup: true);
                                        Singleton<BuildingManager>.instance.UpdateBuildingColors(subBuilding5);
                                        subBuilding5 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding5].m_subBuilding;
                                        if (++num9 > 49152)
                                        {
                                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // fireSize = Mathf.Min(5000, data.m_fireIntensity * data.Width * data.Length);
                                int count = 0;
                                int cargo = 0;
                                int capacity = 0;
                                int outside = 0;
                                int truckCount = GetFireTruckCount(ref data);
                                int helicopterCount = GetFireHelicopterCount(ref data);
                                __instance.CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.Fire, ref count, ref cargo, ref capacity, ref outside);
                                __instance.CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.Fire2, ref count, ref cargo, ref capacity, ref outside);
                                if (count < helicopterCount)
                                {
                                    TransferManager.TransferOffer offer = default;
                                    offer.Priority = Mathf.Max(8 - count - 1, 4);
                                    offer.Building = buildingID;
                                    offer.Position = data.m_position;
                                    offer.Amount = 1;
                                    if ((policies & DistrictPolicies.Services.HelicopterPriority) != 0)
                                    {
                                        var instance3 = Singleton<DistrictManager>.instance;
                                        byte district = instance3.GetDistrict(data.m_position);
                                        instance3.m_districts.m_buffer[district].m_servicePoliciesEffect |= DistrictPolicies.Services.HelicopterPriority;
                                        offer.Amount = 2;
                                        Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Fire2, offer);
                                    }
                                    else if ((data.m_flags & Building.Flags.RoadAccessFailed) != 0)
                                    {
                                        offer.Amount = 2;
                                        Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Fire2, offer);
                                    }
                                    else
                                    {
                                        Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Fire2, offer);
                                    }
                                }
                                if((policies & DistrictPolicies.Services.HelicopterPriority) == 0 && (data.m_flags & Building.Flags.RoadAccessFailed) == 0 && count < truckCount)
                                {
                                    TransferManager.TransferOffer offer = default;
                                    offer.Priority = Mathf.Max(8 - count - 1, 4);
                                    offer.Building = buildingID;
                                    offer.Position = data.m_position;
                                    offer.Amount = 1;
                                    Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Fire, offer);
                                }
                            }
                        }
                    }
                    if (data.m_fireIntensity != 0)
                    {
                        if (frameData.m_fireDamage >= 192)
                        {
                            data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem1.Fire | Notification.Problem1.MajorProblem);
                        }
                        else
                        {
                            data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem1.Fire);
                        }
                        var position = data.CalculateSidewalkPosition();
                        if (PathManager.FindPathPosition(position, ItemClass.Service.Road, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, VehicleInfo.VehicleType.Car, VehicleInfo.VehicleCategory.All, allowUnderground: false, requireConnect: false, 32f, excludeLaneWidth: false, checkPedestrianStreet: false, out var pathPos))
                        {
                            Singleton<NetManager>.instance.m_segments.m_buffer[pathPos.m_segment].AddTraffic(65535, 0);
                            BlockSegmentsOnBothSides(pathPos);
                        }
                        float num10 = VectorUtils.LengthXZ(__instance.m_info.m_size) * 0.5f;
                        int num11 = Mathf.Max(10, Mathf.RoundToInt((float)(int)data.m_fireIntensity * Mathf.Min(1f, num10 / 33.75f)));
                        Singleton<NaturalResourceManager>.instance.TryDumpResource(NaturalResourceManager.Resource.Burned, num11, num11, data.m_position, num10, refresh: true);
                    }
                    return false;
                }
                if (frameData.m_fireDamage != 0 && (data.m_flags & Building.Flags.Collapsed) == 0)
                {
                    frameData.m_fireDamage = (byte)Mathf.Max(frameData.m_fireDamage - 1, 0);
                    if (data.m_subBuilding != 0 && data.m_parentBuilding == 0)
                    {
                        int num12 = 0;
                        ushort subBuilding6 = data.m_subBuilding;
                        while (subBuilding6 != 0)
                        {
                            var lastFrameData5 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding6].GetLastFrameData();
                            lastFrameData5.m_fireDamage = (byte)Mathf.Min(frameData.m_fireDamage, lastFrameData5.m_fireDamage);
                            Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding6].SetLastFrameData(lastFrameData5);
                            subBuilding6 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding6].m_subBuilding;
                            if (++num12 > 49152)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                    }
                    if (frameData.m_fireDamage == 0)
                    {
                        data.SetFrameData(Singleton<SimulationManager>.instance.m_currentFrameIndex, frameData);
                        Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, updateGroup: true);
                        if (data.m_subBuilding != 0 && data.m_parentBuilding == 0)
                        {
                            int num13 = 0;
                            ushort subBuilding7 = data.m_subBuilding;
                            while (subBuilding7 != 0)
                            {
                                Singleton<BuildingManager>.instance.UpdateBuildingRenderer(subBuilding7, updateGroup: true);
                                subBuilding7 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding7].m_subBuilding;
                                if (++num13 > 49152)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                    break;
                                }
                            }
                        }
                    }
                }
                data.m_problems = Notification.RemoveProblems(data.m_problems, Notification.Problem1.Fire);
                return false;
            }

            private static int GetFireTruckCount(ref Building data)
            {
                int buildingVolume = GetBuildingVolume(data.Info.m_generatedInfo);
                int fireTruckCount = buildingVolume < 10000 ? 1 : buildingVolume / 10000 + 1;
                return fireTruckCount;
            }

            private static int GetFireHelicopterCount(ref Building data)
            {
                int buildingVolume = GetBuildingVolume(data.Info.m_generatedInfo);
                int fireHelicopterCount = buildingVolume < 10000 ? 1 : buildingVolume / 20000;
                return fireHelicopterCount;
            }

            private static int GetBuildingVolume(BuildingInfoGen buildingInfoGen)
            {
                float gridSizeX = (buildingInfoGen.m_max.x - buildingInfoGen.m_min.x) / 16f;
                float gridSizeY = (buildingInfoGen.m_max.z - buildingInfoGen.m_min.z) / 16f;
                float gridArea = gridSizeX * gridSizeY;

                float volume = 0f;
                float[] heights = buildingInfoGen.m_heights;
                for (int i = 0; i < heights.Length; i++)
                {
                    volume += gridArea * heights[i];
                }
                return (int)volume;
            }

            private static void BlockSegmentsOnBothSides(PathUnit.Position pathPos)
            {
                ushort segment = pathPos.m_segment;

                ushort end_node = Singleton<NetManager>.instance.m_segments.m_buffer[pathPos.m_segment].m_endNode;

                ushort start_node = Singleton<NetManager>.instance.m_segments.m_buffer[pathPos.m_segment].m_startNode;


                Singleton<NetManager>.instance.m_segments.m_buffer[segment].GetLeftAndRightSegments(end_node, out ushort endLeftSegment, out ushort endRightSegment);

                Singleton<NetManager>.instance.m_segments.m_buffer[endLeftSegment].AddTraffic(65535, 0);

                Singleton<NetManager>.instance.m_segments.m_buffer[endRightSegment].AddTraffic(65535, 0);


                Singleton<NetManager>.instance.m_segments.m_buffer[segment].GetLeftAndRightSegments(start_node, out ushort startLeftSegment, out ushort startRightSegment);

                Singleton<NetManager>.instance.m_segments.m_buffer[startLeftSegment].AddTraffic(65535, 0);

                Singleton<NetManager>.instance.m_segments.m_buffer[startRightSegment].AddTraffic(65535, 0);

            }
        }

        [HarmonyPatch]
        private sealed class HotelAI_ProduceGoods
        {
            [HarmonyPatch(typeof(HotelAI), "ProduceGoods")]
            [HarmonyPrefix]
            public static void Prefix(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData guestBehaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveGuestCount, int totalGuestCount, int guestPlaceCount)
            {
                // Remove tourist with no hotel or bad location from hotel building
                var instance = Singleton<CitizenManager>.instance;
                uint num = buildingData.m_citizenUnits;
                int num2 = 0;
                while (num != 0)
                {
                    if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Hotel) != 0)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            uint citizen = instance.m_units.m_buffer[num].GetCitizen(i);
                            if (Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].m_hotelBuilding == 0)
                            {
                                instance.m_citizens.m_buffer[citizen].RemoveFromUnit(citizen, ref instance.m_units.m_buffer[num]);
                            }
                            else if (Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].CurrentLocation == Citizen.Location.Home)
                            {
                                instance.m_citizens.m_buffer[citizen].RemoveFromUnit(citizen, ref instance.m_units.m_buffer[num]);
                            }
                        }
                    }
                    num = instance.m_units.m_buffer[num].m_nextUnit;
                    if (++num2 > 524288)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
            }

            [HarmonyPatch(typeof(HotelAI), "ProduceGoods")]
            [HarmonyPostfix]
            public static void Postfix(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData guestBehaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveGuestCount, int totalGuestCount, int guestPlaceCount)
            {
                int aliveCount = 0;
                int hotelTotalCount = 0;
                Citizen.BehaviourData behaviour = default;
                GetHotelBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveCount, ref hotelTotalCount);
                buildingData.m_roomUsed = (ushort)hotelTotalCount;
            }

            private static void GetHotelBehaviour(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
            {
                var instance = Singleton<CitizenManager>.instance;
                uint num = buildingData.m_citizenUnits;
                int num2 = 0;
                while (num != 0)
                {
                    if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Hotel) != 0)
                    {
                        instance.m_units.m_buffer[num].GetCitizenHotelBehaviour(ref behaviour, ref aliveCount, ref totalCount);
                    }
                    num = instance.m_units.m_buffer[num].m_nextUnit;
                    if (++num2 > 524288)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
            }

        }

        [HarmonyPatch]
        private sealed class ShelterAI_CreateBuilding
        {
            [HarmonyPatch(typeof(ShelterAI), "CreateBuilding")]
            [HarmonyPrefix]
            public static void CreateBuilding(ShelterAI __instance, ushort buildingID, ref Building data)
            {
                __instance.m_goodsStockpileAmount = ushort.MaxValue;
                data.m_customBuffer1 = ushort.MaxValue;
            }
        }

        [HarmonyPatch]
        private sealed class ShelterAI_SimulationStep
        {
            [HarmonyPatch(typeof(ShelterAI), "SimulationStep")]
            [HarmonyPrefix]
            public static void SimulationStep(ShelterAI __instance, ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
            {
                if(buildingData.m_productionRate == 0)
                {
                    __instance.m_goodsConsumptionRate = 1;
                }
            }
        }

        [HarmonyPatch]
        private sealed class ShelterAI_ProduceGoods
        {
            [HarmonyPatch(typeof(ShelterAI), "ProduceGoods")]
            [HarmonyPrefix]
            public static void ProduceGoods(ShelterAI __instance, ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount) => __instance.m_goodsConsumptionRate = 1;
        }

        [HarmonyPatch]
        private sealed class CommercialBuildingAI_SetGoodsAmount
        {
            [HarmonyPatch(typeof(CommercialBuildingAI), "SetGoodsAmount")]
            [HarmonyPrefix]
            public static bool SetGoodsAmount(CommercialBuildingAI __instance, ref Building data, ushort amount)
            {
                if(RealTimeBuildingAI != null && !RealTimeBuildingAI.WeeklyCommericalDeliveries())
                {
                    return true;
                }
                if(data.m_customBuffer1 - amount > 0)
                {
                    var rnd = new System.Random();
                    int custom_amount = rnd.Next(1, 5);
                    data.m_customBuffer1 -= (ushort)custom_amount;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class PlayerBuildingAI_GetLocalizedStatus
        {
            [HarmonyPatch(typeof(PlayerBuildingAI), "GetLocalizedStatus")]
            [HarmonyPostfix]
            private static void postfix(ushort buildingID, ref Building data, ref string __result)
            {
                if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    __result = "Closed";
                }
            }
        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_GetLocalizedStatus
        {
            [HarmonyPatch(typeof(PrivateBuildingAI), "GetLocalizedStatus")]
            [HarmonyPostfix]
            private static void postfix(ushort buildingID, ref Building data, ref string __result)
            {
                if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    __result = "Closed";
                }
            }
        }

        [HarmonyPatch(typeof(HelicopterDepotAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool HelicopterDepotAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HospitalAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool HospitalAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PoliceStationAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool PoliceStationAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(FireStationAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool FireStationAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(DepotAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool DepotAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(MaintenanceDepotAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool MaintenanceDepotAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(BankOfficeAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool BankOfficeAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PostOfficeAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool PostOfficeAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(DisasterResponseBuildingAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool DisasterResponseBuildingAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CemeteryAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool CemeteryAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(LandfillSiteAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool LandfillSiteAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(FishFarmAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool FishFarmAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(FishingHarborAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool FishingHarborAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(SnowDumpAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool SnowDumpAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(WaterFacilityAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool WaterFacilityAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CableCarStationAI), "ProduceGoods")]
        [HarmonyPrefix]
        public static bool CableCarStationAIProduceGoods(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                return false;
            }
            return true;
        }

    }
}


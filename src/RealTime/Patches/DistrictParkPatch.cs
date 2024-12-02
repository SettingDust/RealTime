// ParkPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework;
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using ColossalFramework.Threading;
    using UnityEngine;
    using ColossalFramework.Math;
    using SkyTools.Tools;
    using RealTime.Config;

    /// <summary>
    /// A static class that provides the patch objects for the Park Life DLC related methods.
    /// </summary>
    [HarmonyPatch]
    internal static class DistrictParkPatch
    {
        /// <summary>Gets or sets the city spare time behavior.</summary>
        public static ISpareTimeBehavior SpareTimeBehavior { get; set; }

        /// <summary>Gets or sets the city time behavior.</summary>
        public static TimeInfo TimeInfo { get; set; }

        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the mod configuration.</summary>
        public static RealTimeConfig RealTimeConfig { get; set; }

        [HarmonyPatch]
        private sealed class DistrictPark_SimulationStep
        {
            [HarmonyPatch(typeof(DistrictPark), "SimulationStep")]
            [HarmonyPostfix]
            private static void Postfix(byte parkID)
            {
                if(parkID != 0)
                {
                    ref var park = ref DistrictManager.instance.m_parks.m_buffer[parkID];

                    if (SpareTimeBehavior!= null && !SpareTimeBehavior.AreFireworksAllowed)
                    {
                        park.m_flags &= ~DistrictPark.Flags.SpecialMode;
                        return;
                    }

                    if (park.m_dayNightCount == 6 || (park.m_parkPolicies & DistrictPolicies.Park.FireworksBoost) != 0)
                    {
                        park.m_flags |= DistrictPark.Flags.SpecialMode;
                    }
                }
            }
        }

        [HarmonyPatch]
        private sealed class DistrictPark_CampusSimulationStep
        {
            [HarmonyPatch(typeof(DistrictPark), "CampusSimulationStep")]
            [HarmonyPrefix]
            [HarmonyAfter(["t1a2l.CombinedAIS"])]
            private static bool CampusSimulationStepPrefix(DistrictPark __instance, byte parkID)
            {
                var instance = Singleton<SimulationManager>.instance;
                var instance2 = Singleton<DistrictManager>.instance;
                __instance.m_finalGateCount = __instance.m_tempGateCount;
                __instance.m_finalVisitorCapacity = __instance.m_tempVisitorCapacity;
                __instance.m_finalMainCapacity = __instance.m_tempMainCapacity;
                __instance.m_tempEntertainmentAccumulation = 0;
                __instance.m_tempAttractivenessAccumulation = 0;
                __instance.m_tempGateCount = 0;
                __instance.m_tempVisitorCapacity = 0;
                __instance.m_tempMainCapacity = 0;
                __instance.m_studentCount = 0u;
                __instance.m_studentCapacity = 0u;
                if (parkID == 0 || __instance.m_parkType == DistrictPark.ParkType.GenericCampus)
                {
                    return false;
                }
                var instance3 = Singleton<BuildingManager>.instance;
                var serviceBuildings = instance3.GetServiceBuildings(ItemClass.Service.PlayerEducation);
                for (int i = 0; i < serviceBuildings.m_size; i++)
                {
                    ushort num = serviceBuildings.m_buffer[i];
                    byte park = Singleton<DistrictManager>.instance.GetPark(instance3.m_buildings.m_buffer[num].m_position);
                    if (park == parkID)
                    {
                        int count = 0;
                        int capacity = 0;
                        var campusBuildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]].Info.m_buildingAI as CampusBuildingAI;
                        var uniqueFacultyAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]].Info.m_buildingAI as UniqueFacultyAI;
                        campusBuildingAI?.GetStudentCount(serviceBuildings[i], ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]], out count, out capacity, out int global);
                        uniqueFacultyAI?.GetStudentCount(serviceBuildings[i], ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]], out count, out capacity, out global);
                        __instance.m_studentCount += (uint)count;
                        __instance.m_studentCapacity += (uint)capacity;
                    }
                }
                int num2 = 0;
                if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[__instance.m_mainGate].m_productionRate > 0 && (Singleton<BuildingManager>.instance.m_buildings.m_buffer[__instance.m_mainGate].m_flags & Building.Flags.Collapsed) == 0 && (__instance.m_parkPolicies & DistrictPolicies.Park.UniversalEducation) == 0)
                {
                    __instance.m_finalTicketIncome = __instance.m_studentCount * Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusLevelInfo[(uint)__instance.m_parkLevel].m_tuitionMoneyPerStudent;
                    int amount = (int)(__instance.m_finalTicketIncome / 16 * 100);
                    Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, amount, Singleton<BuildingManager>.instance.m_buildings.m_buffer[__instance.m_mainGate].Info.m_class);
                }
                else
                {
                    __instance.m_finalTicketIncome = 0u;
                }
                num2 += (int)__instance.m_finalTicketIncome;
                if (__instance.m_mainGate != 0)
                {
                    float academicYearProgress = __instance.GetAcademicYearProgress();
                    if (__instance.m_previousYearProgress > academicYearProgress)
                    {
                        __instance.m_previousYearProgress = academicYearProgress;
                    }
                    float num3 = academicYearProgress - __instance.m_previousYearProgress;
                    __instance.m_previousYearProgress = academicYearProgress;
                    __instance.m_academicStaffCount = (byte)Mathf.Clamp(__instance.m_academicStaffCount, Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMin, Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMax);
                    float num4 = (__instance.m_academicStaffCount - Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMin) / (float)(Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMax - Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMin);
                    __instance.m_academicStaffAccumulation += num3 * num4;
                    __instance.m_academicStaffAccumulation = Mathf.Clamp(__instance.m_academicStaffAccumulation, 0f, 1f);
                    int num5 = (int)(__instance.CalculateAcademicStaffWages() / 0.16f) / 100;
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, num5, ItemClass.Service.PlayerEducation, __instance.CampusTypeToSubservice(), ItemClass.Level.None);
                    num2 -= num5;
                    if (__instance.m_awayMatchesDone == null || __instance.m_awayMatchesDone.Length != 5)
                    {
                        __instance.m_awayMatchesDone = new bool[5];
                    }
                    for (int j = 0; j < 5; j++)
                    {
                        if (!__instance.m_awayMatchesDone[j] && academicYearProgress > 1f / 6f * (float)(j + 1))
                        {
                            SimulateVarsityAwayGame(__instance, parkID);
                            __instance.m_awayMatchesDone[j] = true;
                        }
                    }
                    uint didLastYearEnd = Singleton<BuildingManager>.instance.m_buildings.m_buffer[__instance.m_mainGate].m_garbageTrafficRate;

                    ref var campus = ref instance2.m_parks.m_buffer[parkID];

                    // m_hasTerminal - graduation happend
                    // m_terrainHeight - hour of graduation start
                    if (didLastYearEnd == 1 && (campus.m_flags & DistrictPark.Flags.Graduation) == 0 && !campus.m_hasTerminal)
                    {
                        bool shouldStartGraduation = true;
                        if (TimeInfo.IsNightTime || TimeInfo.Now.IsWeekend())
                        {
                            shouldStartGraduation = false;
                        }
                        if (TimeInfo.CurrentHour < 9f || TimeInfo.CurrentHour > 12f)
                        {
                            shouldStartGraduation = false;
                        }
                        if(shouldStartGraduation)
                        {
                            campus.m_flags |= DistrictPark.Flags.Graduation;
                            campus.m_terrainHeight = TimeInfo.CurrentHour;
                            campus.m_hasTerminal = true;
                        }
                    }
                    if (didLastYearEnd == 1 && (campus.m_flags & DistrictPark.Flags.Graduation) != 0
                        && campus.m_hasTerminal && TimeInfo.CurrentHour - campus.m_terrainHeight > 3f)
                    {
                        campus.m_terrainHeight = 0;
                        campus.m_flags &= ~DistrictPark.Flags.Graduation;
                    }

                    if(didLastYearEnd == 0 && campus.m_hasTerminal)
                    {
                        campus.m_hasTerminal = false;
                    }
                }
                if (__instance.m_coachCount != 0)
                {
                    int num6 = (int)((float)__instance.CalculateCoachingStaffCost() / 0.16f) / 100;
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, num6, ItemClass.Service.PlayerEducation, __instance.CampusTypeToSubservice(), ItemClass.Level.None);
                    num2 -= num6;
                }
                int activeArenasCount = __instance.GetActiveArenasCount();
                if (__instance.m_cheerleadingBudget != 0)
                {
                    int num7 = (int)((float)(__instance.m_cheerleadingBudget * activeArenasCount) / 0.16f);
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, num7, ItemClass.Service.PlayerEducation, __instance.CampusTypeToSubservice(), ItemClass.Level.None);
                    num2 -= num7;
                }
                if ((__instance.m_parkPolicies & DistrictPolicies.Park.UniversalEducation) != 0)
                {
                    __instance.m_parkPoliciesEffect |= DistrictPolicies.Park.UniversalEducation;
                }
                if ((__instance.m_parkPolicies & DistrictPolicies.Park.StudentHealthcare) != 0)
                {
                    __instance.m_parkPoliciesEffect |= DistrictPolicies.Park.StudentHealthcare;
                    int num8 = (int)(__instance.m_studentCount * 3125) / 100;
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, num8, ItemClass.Service.PlayerEducation, __instance.CampusTypeToSubservice(), ItemClass.Level.None);
                    num2 -= num8;
                }
                if ((__instance.m_parkPolicies & DistrictPolicies.Park.FreeLunch) != 0)
                {
                    __instance.m_parkPoliciesEffect |= DistrictPolicies.Park.FreeLunch;
                    int num9 = (int)(__instance.m_studentCount * 625) / 100;
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, num9, ItemClass.Service.PlayerEducation, __instance.CampusTypeToSubservice(), ItemClass.Level.None);
                    num2 -= num9;
                }
                if ((__instance.m_parkPolicies & DistrictPolicies.Park.VarsitySportsAds) != 0)
                {
                    __instance.m_parkPoliciesEffect |= DistrictPolicies.Park.VarsitySportsAds;
                    int num10 = 0;
                    int num11 = 1250;
                    num10 = num11 * activeArenasCount;
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, num10, ItemClass.Service.PlayerEducation, __instance.CampusTypeToSubservice(), ItemClass.Level.None);
                    num2 -= num10;
                }
                if ((__instance.m_parkPolicies & DistrictPolicies.Park.FreeFanMerchandise) != 0)
                {
                    __instance.m_parkPoliciesEffect |= DistrictPolicies.Park.FreeFanMerchandise;
                    int num12 = 0;
                    int num13 = 1125;
                    num12 = num13 * activeArenasCount;
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, num12, ItemClass.Service.PlayerEducation, __instance.CampusTypeToSubservice(), ItemClass.Level.None);
                    num2 -= num12;
                }
                if (__instance.CanAddExchangeStudentAttractiveness())
                {
                    int num14 = __instance.CalculateExchangeStudentAttractiveness();
                    if (num14 != 0)
                    {
                        num14 = num14 / 4 * 10;
                        float num15 = Singleton<ImmaterialResourceManager>.instance.CheckActualTourismResource();
                        num15 = (num15 * (float)num14 + 99f) / 100f;
                        Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Attractiveness, Mathf.RoundToInt(num15));
                    }
                }
                long num16 = __instance.m_ledger.ReadCurrentTogaPartySeed();
                if (num16 != 0 && (TimeInfo.Now - new DateTime(num16)).Hours > RealTimeConfig.TogaPartyLength)
                {
                    EndTogaParty(__instance, parkID);
                }
                if (__instance.m_mainGate != 0 && __instance.m_ledger.ReadYearData(DistrictPark.AcademicYear.Last).m_reputationLevel != 0)
                {
                    var properties = Singleton<GuideManager>.instance.m_properties;
                    if (properties is not null)
                    {
                        Singleton<DistrictManager>.instance.m_academicYearReportClosed.Activate(properties.m_academicYearReportClosed, __instance.m_mainGate);
                    }
                }
                if (num2 >= 0 && __instance.m_studentCount >= 5000 && Singleton<SimulationManager>.instance.m_metaData.m_disableAchievements != SimulationMetaData.MetaBool.True)
                {
                    ThreadHelper.dispatcher.Dispatch(delegate
                    {
                        SteamHelper.UnlockAchievement("ForForProfitEducation");
                    });
                }
                return false;
            }

            private static void EndTogaParty(DistrictPark __instance, byte parkID)
            {
                __instance.m_flags &= ~DistrictPark.Flags.TogaParty;
                var instance = Singleton<BuildingManager>.instance;
                if (instance.m_buildings.m_buffer[__instance.m_partyVenue].Info.m_buildingAI is CampusBuildingAI)
                {
                    for (int i = 0; i < __instance.m_arrivedAtParty; i++)
                    {
                        CreatePartyReturner(__instance.m_partyVenue, ref instance.m_buildings.m_buffer[__instance.m_partyVenue], parkID);
                    }
                }
                __instance.m_partyVenue = 0;
                __instance.m_arrivedAtParty = 0;
                __instance.m_goingToParty = 0;
            }

            private static void SimulateVarsityAwayGame(DistrictPark __instance, byte parkID)
            {
                for (int i = 0; i < 5; i++)
                {
                    var sport = (DistrictPark.SportIndex)i;
                    if (__instance.GetArenas(sport).m_size != 0)
                    {
                        if (GetMatchResult(__instance, ref Singleton<SimulationManager>.instance.m_randomizer))
                        {
                            __instance.m_ledger.WriteMatchWon(parkID, sport);
                        }
                        else
                        {
                            __instance.m_ledger.WriteMatchLost(parkID, sport);
                        }
                    }
                }
            }

            private static bool GetMatchResult(DistrictPark __instance, ref Randomizer randomizer)
            {
                int randomChanceModifier = randomizer.Int32(0, Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_randomChanceModifier);
                float winProbability = __instance.GetWinProbability(randomChanceModifier);
                int num = randomizer.Int32(10000u);
                if ((float)num < winProbability * 100f)
                {
                    return true;
                }
                return false;
            }

            private static void CreatePartyReturner(ushort buildingID, ref Building data, byte campus)
            {
                var instance = Singleton<CitizenManager>.instance;
                var gender = (Citizen.Gender)Singleton<SimulationManager>.instance.m_randomizer.Int32(2u);
                var groupCitizenInfo = instance.GetGroupCitizenInfo(ref Singleton<SimulationManager>.instance.m_randomizer, ItemClass.Service.PlayerEducation, gender, Citizen.SubCulture.Hippie, Citizen.AgePhase.Adult0);
                if (groupCitizenInfo is not null && instance.CreateCitizenInstance(out ushort instance2, ref Singleton<SimulationManager>.instance.m_randomizer, groupCitizenInfo, 0u))
                {
                    ushort randomCampusBuilding = Singleton<DistrictManager>.instance.m_parks.m_buffer[campus].GetRandomCampusBuilding(campus, instance2);
                    if (randomCampusBuilding == 0)
                    {
                        instance.ReleaseCitizenInstance(instance2);
                        return;
                    }
                    groupCitizenInfo.m_citizenAI.SetSource(instance2, ref instance.m_instances.m_buffer[instance2], buildingID);
                    groupCitizenInfo.m_citizenAI.SetTarget(instance2, ref instance.m_instances.m_buffer[instance2], randomCampusBuilding);
                }
            }
        }
    }
}

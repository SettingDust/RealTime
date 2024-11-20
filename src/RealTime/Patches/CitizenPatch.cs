// CitizenPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.GameConnection;
    using UnityEngine;
    using static Citizen;

    [HarmonyPatch]
    internal class CitizenPatch
    {
        [HarmonyPatch]
        private sealed class Citizen_GetGarbageAccumulation
        {
            [HarmonyPatch(typeof(Citizen), "GetGarbageAccumulation")]
            [HarmonyPrefix]
            public static bool GetGarbageAccumulation(Education educationLevel, ref int __result)
            {
                switch(educationLevel)
                {
                    case Education.Uneducated:
                        __result = 10;
                        break;
                    case Education.OneSchool:
                        __result = 9;
                        break;
                    case Education.TwoSchools:
                        __result = 8;
                        break;
                    case Education.ThreeSchools:
                        __result = 7;
                        break;
                    default:
                        __result = 0;
                        break;
                };
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class Citizen_GetMailAccumulation
        {
            [HarmonyPatch(typeof(Citizen), "GetMailAccumulation")]
            [HarmonyPrefix]
            public static bool GetMailAccumulation(Education educationLevel, ref int __result)
            {
                switch (educationLevel)
                {
                    case Education.Uneducated:
                        __result = 7;
                        break;
                    case Education.OneSchool:
                        __result = 8;
                        break;
                    case Education.TwoSchools:
                        __result = 9;
                        break;
                    case Education.ThreeSchools:
                        __result = 10;
                        break;
                    default:
                        __result = 0;
                        break;
                };
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class Citizen_ResetHotel
        {
            [HarmonyPatch(typeof(Citizen), "ResetHotel")]
            [HarmonyPrefix]
            public static bool ResetHotel(Citizen __instance, uint citizenID, uint unitID)
            {
                var instance = Singleton<CitizenManager>.instance;
                ushort hotelBuilding = instance.m_citizens.m_buffer[citizenID].m_hotelBuilding;
                if (hotelBuilding != 0)
                {
                    var buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                    if (unitID == 0)
                    {
                        unitID = buffer[hotelBuilding].m_citizenUnits;
                    }
                    __instance.RemoveFromUnits(citizenID, unitID, CitizenUnit.Flags.Hotel);
                    var buildingInfo = buffer[hotelBuilding].Info;
                    if (buildingInfo.m_buildingAI is HotelAI hotelAI)
                    {
                        hotelAI.RemoveGuest(hotelBuilding, ref buffer[hotelBuilding]);
                    }
                    else if (BuildingManagerConnection.IsHotel(hotelBuilding))
                    {
                        buffer[hotelBuilding].m_roomUsed = (ushort)Mathf.Max(buffer[hotelBuilding].m_roomUsed - 1, 0);
                        buffer[hotelBuilding].m_roomBecomeVacant++;
                    }
                    instance.m_citizens.m_buffer[citizenID].m_hotelBuilding = 0;
                }
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class Citizen_SetHotel
        {
            [HarmonyPatch(typeof(Citizen), "SetHotel")]
            [HarmonyPrefix]
            public static bool SetHotel(Citizen __instance, uint citizenID, ushort buildingID, uint unitID)
            {
                __instance.ResetHotel(citizenID, unitID);
                if (unitID != 0)
                {
                    var buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                    var instance = Singleton<CitizenManager>.instance;
                    if (__instance.AddToUnit(citizenID, ref instance.m_units.m_buffer[unitID]))
                    {
                        ushort hotelBuilding = instance.m_units.m_buffer[unitID].m_building;
                        instance.m_citizens.m_buffer[citizenID].m_hotelBuilding = hotelBuilding;
                        instance.m_citizens.m_buffer[citizenID].WealthLevel = GetWealthLevel(buffer[hotelBuilding].Info.m_class.m_level);
                        var buildingInfo = buffer[hotelBuilding].Info;
                        if (buildingInfo.m_buildingAI is HotelAI hotelAI)
                        {
                            hotelAI.AddGuest(hotelBuilding, ref buffer[hotelBuilding]);
                        }
                        else if (BuildingManagerConnection.IsHotel(hotelBuilding))
                        {
                            buffer[hotelBuilding].m_roomUsed = (ushort)Mathf.Min(buffer[hotelBuilding].m_roomUsed + 1, buffer[hotelBuilding].m_roomMax);
                            buffer[hotelBuilding].m_roomLeftToUse--;
                            buffer[hotelBuilding].m_roomBecomeUsed++;
                        }
                    }
                }
                else
                {
                    if (buildingID == 0)
                    {
                        return false;
                    }
                    var buffer2 = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                    var instance = Singleton<CitizenManager>.instance;
                    if (__instance.AddToUnits(citizenID, buffer2[buildingID].m_citizenUnits, CitizenUnit.Flags.Hotel))
                    {
                        instance.m_citizens.m_buffer[citizenID].m_hotelBuilding = buildingID;
                        instance.m_citizens.m_buffer[citizenID].WealthLevel = GetWealthLevel(buffer2[buildingID].Info.m_class.m_level);
                        var buildingInfo = buffer2[buildingID].Info;
                        if (buildingInfo.m_buildingAI is HotelAI hotelAI)
                        {
                            hotelAI.AddGuest(buildingID, ref buffer2[buildingID]);
                        }
                        else if (BuildingManagerConnection.IsHotel(buildingID))
                        {
                            buffer2[buildingID].m_roomUsed = (ushort)Mathf.Min(buffer2[buildingID].m_roomUsed + 1, buffer2[buildingID].m_roomMax);
                            buffer2[buildingID].m_roomLeftToUse--;
                            buffer2[buildingID].m_roomBecomeUsed++;
                        }
                    }
                }
                return false;
            }
        }
    }
}

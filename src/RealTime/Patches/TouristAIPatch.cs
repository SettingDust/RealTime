// TouristAIPatch.cs

namespace RealTime.Patches
{
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using SkyTools.Tools;
    using RealTime.GameConnection;
    using static RealTime.GameConnection.HumanAIConnectionBase<TouristAI, Citizen>;
    using static RealTime.GameConnection.TouristAIConnection<TouristAI, Citizen>;
    using ColossalFramework;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects and the game connection objects for the tourist AI .
    /// </summary>
    [HarmonyPatch]
    internal static class TouristAIPatch
    {
        /// <summary>Gets or sets the custom AI object for tourists.</summary>
        public static RealTimeTouristAI<TouristAI, Citizen> RealTimeTouristAI { get; set; }

        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        public static TimeInfo TimeInfo { get; set; }


        public static ushort Chosen_Building = 0;

        /// <summary>Creates a game connection object for the tourist AI class.</summary>
        /// <returns>A new <see cref="TouristAIConnection{TouristAI, Citizen}"/> object.</returns>
        public static TouristAIConnection<TouristAI, Citizen> GetTouristAIConnection()
        {
            try
            {
                var getRandomTargetType = AccessTools.MethodDelegate<GetRandomTargetTypeDelegate>(AccessTools.Method(typeof(TouristAI), "GetRandomTargetType"));

                var getLeavingReason = AccessTools.MethodDelegate<GetLeavingReasonDelegate>(AccessTools.Method(typeof(HumanAI), "GetLeavingReason"));

                var addTouristVisit = AccessTools.MethodDelegate<AddTouristVisitDelegate>(AccessTools.Method(typeof(TouristAI), "AddTouristVisit", new Type[] { typeof(uint), typeof(ushort) })); //

                var getBusinessReason = AccessTools.MethodDelegate<GetBusinessReasonDelegate>(AccessTools.Method(typeof(TouristAI), "GetBusinessReason"));

                var getNatureReason = AccessTools.MethodDelegate<GetNatureReasonDelegate>(AccessTools.Method(typeof(TouristAI), "GetNatureReason"));

                var doRandomMove = AccessTools.MethodDelegate<DoRandomMoveDelegate>(AccessTools.Method(typeof(TouristAI), "DoRandomMove"));

                var findEvacuationPlace = AccessTools.MethodDelegate<FindEvacuationPlaceDelegate>(AccessTools.Method(typeof(HumanAI), "FindEvacuationPlace"));

                var findVisitPlace = AccessTools.MethodDelegate<FindVisitPlaceDelegate>(AccessTools.Method(typeof(HumanAI), "FindVisitPlace"));

                var getEntertainmentReason = AccessTools.MethodDelegate<GetEntertainmentReasonDelegate>(AccessTools.Method(typeof(TouristAI), "GetEntertainmentReason"));

                var getEvacuationReason = AccessTools.MethodDelegate<GetEvacuationReasonDelegate>(AccessTools.Method(typeof(TouristAI), "GetEvacuationReason"));

                var getShoppingReason = AccessTools.MethodDelegate<GetShoppingReasonDelegate>(AccessTools.Method(typeof(TouristAI), "GetShoppingReason"));

                var startMoving = AccessTools.MethodDelegate<StartMovingDelegate>(AccessTools.Method(typeof(HumanAI), "StartMoving", new Type[] { typeof(uint), typeof(Citizen).MakeByRefType(), typeof(ushort), typeof(ushort) }));

                return new TouristAIConnection<TouristAI, Citizen>(
                    getRandomTargetType,
                    getLeavingReason,
                    addTouristVisit,
                    getBusinessReason,
                    getNatureReason,
                    doRandomMove,
                    findEvacuationPlace,
                    findVisitPlace,
                    getEntertainmentReason,
                    getEvacuationReason,
                    getShoppingReason,
                    startMoving);
            }
            catch (Exception e)
            {
                Log.Error("The 'Real Time' mod failed to create a delegate for type 'TouristAI', no method patching for the class: " + e);
                return null;
            }
        }

        [HarmonyPatch]
        private sealed class TouristAI_UpdateLocation
        {
            [HarmonyPatch(typeof(TouristAI), "UpdateLocation")]
            [HarmonyPrefix]
            private static bool Prefix(TouristAI __instance, uint citizenID, ref Citizen data)
            {
                if(RealTimeTouristAI != null)
                {
                    RealTimeTouristAI.UpdateLocation(__instance, citizenID, ref data);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class TouristAI_SetTarget
        {
            [HarmonyPatch(typeof(TouristAI), "SetTarget")]
            [HarmonyPrefix]
            private static bool SetTarget(ushort instanceID, ref CitizenInstance data, ushort targetIndex, bool targetIsNode)
            {
                if (data.Info.GetAI() is TouristAI && data.m_targetBuilding != 0)
                {
                    var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                    if (building.Info && building.Info.GetAI() is CampusBuildingAI && (building.Info.name.Contains("Cafeteria") || building.Info.name.Contains("Gymnasium")))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class TouristAI_StartTransfer
        {
            [HarmonyPatch(typeof(TouristAI), "StartTransfer")]
            [HarmonyPrefix]
            private static bool Prefix(TouristAI __instance, uint citizenID, ref Citizen data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (data.m_flags == Citizen.Flags.None || data.Dead || data.Sick)
                {
                    return true;
                }
                switch (material)
                {
                    case TransferManager.TransferReason.Shopping:
                    case TransferManager.TransferReason.ShoppingB:
                    case TransferManager.TransferReason.ShoppingC:
                    case TransferManager.TransferReason.ShoppingD:
                    case TransferManager.TransferReason.ShoppingE:
                    case TransferManager.TransferReason.ShoppingF:
                    case TransferManager.TransferReason.ShoppingG:
                    case TransferManager.TransferReason.ShoppingH:
                    case TransferManager.TransferReason.Entertainment:
                    case TransferManager.TransferReason.EntertainmentB:
                    case TransferManager.TransferReason.EntertainmentC:
                    case TransferManager.TransferReason.EntertainmentD:
                    case TransferManager.TransferReason.TouristA:
                    case TransferManager.TransferReason.TouristB:
                    case TransferManager.TransferReason.TouristC:
                    case TransferManager.TransferReason.TouristD:
                    case TransferManager.TransferReason.BusinessA:
                    case TransferManager.TransferReason.BusinessB:
                    case TransferManager.TransferReason.BusinessC:
                    case TransferManager.TransferReason.BusinessD:
                    case TransferManager.TransferReason.NatureA:
                    case TransferManager.TransferReason.NatureB:
                    case TransferManager.TransferReason.NatureC:
                    case TransferManager.TransferReason.NatureD:
                        var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building];
                        // if target is hotel building
                        if (BuildingManagerConnection.IsHotel(offer.Building))
                        {
                            // if no event
                            // and no visit places
                            // and not tourist current hotel
                            // dont visit hotel
                            if((building.m_flags & Building.Flags.EventActive) == 0 && !RealTimeBuildingAI.HaveUnits(offer.Building, CitizenUnit.Flags.Visit) && data.m_hotelBuilding != offer.Building)
                            {
                                return false;
                            }
                        }
                        // dont go to closed buildings
                        if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(offer.Building))
                        {
                            return false;
                        }
                        
                        // tourist will not go to campus cafeteria or gym buildings
                        if (building.Info && building.Info.GetAI() is CampusBuildingAI && (building.Info.name.Contains("Cafeteria") || building.Info.name.Contains("Gymnasium")))
                        {
                            return false;
                        }
                        return true;

                    default:
                        return true;
                }
            }
        }

        [HarmonyPatch]
        private sealed class TouristAI_GetColor
        {
            [HarmonyPatch(typeof(TouristAI), "GetColor")]
            [HarmonyPrefix]
            private static bool Prefix(TouristAI __instance, ushort instanceID, ref CitizenInstance data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
            {
                if (instanceID == 0)
                {
                    return true;
                }

                if (infoMode == InfoManager.InfoMode.Density)
                {
                    if (Chosen_Building == 0 && WorldInfoPanel.GetCurrentInstanceID().Building == 0)
                    {
                        return true;
                    }

                    if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                    {
                        Chosen_Building = WorldInfoPanel.GetCurrentInstanceID().Building;
                    }

                    ushort hotel_building = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen].m_hotelBuilding;
                    ushort visit_building = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen].m_visitBuilding;

                    if (Chosen_Building == hotel_building)
                    {
                        __result = Color.green;
                    }
                    else
                    {
                        __result = Chosen_Building == visit_building ? Color.magenta : Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                    return false;
                }

                return true;
            }
        }

    }
}

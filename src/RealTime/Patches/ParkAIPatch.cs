namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using ICities;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    [HarmonyPatch]
    public static class ParkAIPatch
    {
        private static readonly string[] BannedEntertainmentBuildings = ["parking", "garage", "car park", "Parking", "Car Port", "Garage", "Car Park"];

        private delegate void PlayerBuildingAIProduceGoodsDelegate(PlayerBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount);
        private static readonly PlayerBuildingAIProduceGoodsDelegate BaseProduceGoods = AccessTools.MethodDelegate<PlayerBuildingAIProduceGoodsDelegate>(typeof(PlayerBuildingAI).GetMethod("ProduceGoods", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        private delegate void ParkAIGetMaintenanceLevelDelegate(PlayerBuildingAI __instance, ushort buildingID, ref Building data, out int current, out int max);
        private static readonly ParkAIGetMaintenanceLevelDelegate GetMaintenanceLevel = AccessTools.MethodDelegate<ParkAIGetMaintenanceLevelDelegate>(typeof(ParkAI).GetMethod("GetMaintenanceLevel", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        private delegate void CommonBuildingAIHandleDeadDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, int citizenCount);
        private static readonly CommonBuildingAIHandleDeadDelegate HandleDead = AccessTools.MethodDelegate<CommonBuildingAIHandleDeadDelegate>(typeof(CommonBuildingAI).GetMethod("HandleDead", BindingFlags.Instance | BindingFlags.NonPublic), null, false);


        [HarmonyPatch(typeof(ParkAI), "ProduceGoods")]
        [HarmonyPrefix]
        public static bool ProduceGoods(ParkAI __instance, ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount)
        {
            if (BannedEntertainmentBuildings.Any(s => __instance.name.Equals(s)))
            {
                BaseProduceGoods(__instance, buildingID, ref buildingData, ref frameData, productionRate, finalProductionRate, ref behaviour, aliveWorkerCount, totalWorkerCount, workPlaceCount, aliveVisitorCount, totalVisitorCount, visitPlaceCount);
                GetMaintenanceLevel(__instance, buildingID, ref buildingData, out var current, out var max);
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Entertainment, 0, buildingData.m_position, 0);
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Attractiveness, 0, buildingData.m_position, 0);

                if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Hotels))
                {
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Sightseeing, 0, buildingData.m_position, 0);
                }
                if (finalProductionRate == 0)
                {
                    return false;
                }
                HandleDead(__instance, buildingID, ref buildingData, ref behaviour, totalVisitorCount);
                if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Parks) && (buildingData.m_accessSegment == 0 || (Singleton<NetManager>.instance.m_segments.m_buffer[buildingData.m_accessSegment].Info.m_vehicleCategories & VehicleInfo.VehicleCategory.ParkTruck) != 0) && current < max * 9 / 10)
                {
                    int count = 0;
                    int cargo = 0;
                    int capacity = 0;
                    int outside = 0;
                    __instance.CalculateGuestVehicles(buildingID, ref buildingData, TransferManager.TransferReason.ParkMaintenance, ref count, ref cargo, ref capacity, ref outside);
                    current = Mathf.Min(max, current + capacity - cargo);
                    if (current < max * 9 / 10)
                    {
                        TransferManager.TransferOffer offer2 = default;
                        offer2.Priority = (max - current) * 8 / max;
                        offer2.Building = buildingID;
                        offer2.Position = buildingData.m_position;
                        offer2.Amount = 1;
                        Singleton<TransferManager>.instance.AddIncomingOffer(TransferManager.TransferReason.ParkMaintenance, offer2);
                    }
                }
                return false;
            }
            return true;
        }
    }
}

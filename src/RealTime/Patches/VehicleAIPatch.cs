namespace RealTime.Patches
{
    using System;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using UnityEngine;

    [HarmonyPatch]
    internal static class VehicleAIPatch
    {
        // <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch]
        private sealed class FireTruckAI_ExtinguishFire
        {

            [HarmonyPatch(typeof(FireTruckAI), "ExtinguishFire")]
            [HarmonyPrefix]
            private static bool Prefix(FireTruckAI __instance, ushort vehicleID, ref Vehicle data, ushort buildingID, ref Building buildingData, ref bool __result)
            {
                byte fireIntensity = buildingData.m_fireIntensity;
                if(fireIntensity > 0)
                {
                    RealTimeBuildingAI.CreateBuildingFire(data.m_targetBuilding);
                    return RealTimeBuildingAI.ShouldExtinguishFire(buildingID);
                }
                if (fireIntensity == 0)
                {
                    RealTimeBuildingAI.RemoveBuildingFire(data.m_targetBuilding);
                }
                return true;               
            }

            [HarmonyPatch(typeof(FireTruckAI), "SetTarget")]
            [HarmonyPrefix]
            private static void SetTarget(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
            {
                if (targetBuilding == 0)
                {
                    RealTimeBuildingAI.RemoveBuildingFire(data.m_targetBuilding);
                }
                else
                {
                    ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding];
                    if (building.m_fireIntensity == 0)
                    {
                        RealTimeBuildingAI.RemoveBuildingFire(data.m_targetBuilding);
                    }
                }

            }

        }

        [HarmonyPatch]
        private sealed class FireCopterAI_ExtinguishFire
        {
            [HarmonyPatch(typeof(FireCopterAI), "ExtinguishFire",
                new Type[] { typeof(ushort), typeof(Vehicle), typeof(ushort), typeof(Building) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref })]
            [HarmonyPrefix]
            private static bool Prefix(FireCopterAI __instance, ushort vehicleID, ref Vehicle data, ushort buildingID, ref Building buildingData, ref bool __result)
            {
                byte fireIntensity = buildingData.m_fireIntensity;
                if (fireIntensity > 0)
                {
                    RealTimeBuildingAI.CreateBuildingFire(data.m_targetBuilding);
                    if (RealTimeBuildingAI.ShouldExtinguishFire(buildingID))
                    {
                        return true;
                    }
                    else
                    {
                        int num2 = Mathf.Min(__instance.m_fireFightingRate, data.m_transferSize);
                        data.m_transferSize = (ushort)(data.m_transferSize - num2);
                    }
                    return false;
                }
                if (fireIntensity == 0)
                {
                    RealTimeBuildingAI.RemoveBuildingFire(data.m_targetBuilding);
                }
                return true;
            }

            [HarmonyPatch(typeof(FireCopterAI), "SetTarget")]
            [HarmonyPrefix]
            private static void SetTarget(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
            {
                if (targetBuilding == 0)
                {
                    RealTimeBuildingAI.RemoveBuildingFire(data.m_targetBuilding);
                }
                else
                {
                    ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding];
                    if (building.m_fireIntensity == 0)
                    {
                        RealTimeBuildingAI.RemoveBuildingFire(data.m_targetBuilding);
                    }
                }
            }



            [HarmonyPatch(typeof(HelicopterDepotAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool HelicopterDepotAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if(!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(HospitalAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool HospitalAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(PoliceStationAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool PoliceStationAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(FireStationAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool FireStationAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(DepotAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool DepotAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }



            [HarmonyPatch(typeof(MaintenanceDepotAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool MaintenanceDepotAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(BankOfficeAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool BankOfficeAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(PostOfficeAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool PostOfficeAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(DisasterResponseBuildingAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool DisasterResponseBuildingAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(CemeteryAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool CemeteryAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(LandfillSiteAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool LandfillSiteAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(FishFarmAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool FishFarmAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(FishingHarborAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool FishingHarborAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }


            [HarmonyPatch(typeof(SnowDumpAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool SnowDumpAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }


            [HarmonyPatch(typeof(WaterFacilityAI), "StartTransfer")]
            [HarmonyPrefix]
            public static bool WaterFacilityAIStartTransfer(ushort buildingID, ref Building data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
            {
                if (!RealTimeBuildingAI.IsBuildingWorking(buildingID))
                {
                    return false;
                }
                return true;
            }

        }
    }
}

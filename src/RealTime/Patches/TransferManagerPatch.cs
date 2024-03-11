// TransferManagerPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using RealTime.CustomAI;
    using SkyTools.Tools;

    /// <summary>
    /// A static class that provides the patch objects for the game's transfer manager.
    /// </summary>
    [HarmonyPatch]
    internal static class TransferManagerPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch]
        private sealed class TransferManager_AddOutgoingOffer
        {
            [HarmonyPatch(typeof(TransferManager), "AddOutgoingOffer")]
            [HarmonyPrefix]
            private static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
            {
                if(RealTimeBuildingAI == null)
                {
                    Log.Info("TransferManager_AddOutgoingOffer RealTimeBuildingAI is null");
                    return true;
                }

                switch (material)
                {
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
                        return RealTimeBuildingAI.IsEntertainmentTarget(offer.Building);

                    case TransferManager.TransferReason.Shopping:
                    case TransferManager.TransferReason.ShoppingB:
                    case TransferManager.TransferReason.ShoppingC:
                    case TransferManager.TransferReason.ShoppingD:
                    case TransferManager.TransferReason.ShoppingE:
                    case TransferManager.TransferReason.ShoppingF:
                    case TransferManager.TransferReason.ShoppingG:
                    case TransferManager.TransferReason.ShoppingH:
                        return RealTimeBuildingAI.IsShoppingTarget(offer.Building);

                    case TransferManager.TransferReason.Mail: // buildings request to send or recieve mail
                    case TransferManager.TransferReason.UnsortedMail: // post offices request to pick up unsorted mail
                        return RealTimeBuildingAI.IsMailHours(offer.Building);

                    case TransferManager.TransferReason.Garbage: // buildings sends outgoing offers for garbage
                        return RealTimeBuildingAI.IsGarbageHours(offer.Building);

                    default:
                        return true;
                }
            }
        }

        [HarmonyPatch]
        private sealed class TransferManager_AddIncomingOffer
        {
            [HarmonyPatch(typeof(TransferManager), "AddIncomingOffer")]
            [HarmonyPrefix]
            private static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
            {
                if (RealTimeBuildingAI == null)
                {
                    Log.Info("TransferManager_AddIncomingOffer RealTimeBuildingAI is null");
                    return true;
                }

                switch (material)
                {
                    case TransferManager.TransferReason.SortedMail: // post offices request to send then sorted mail
                        return RealTimeBuildingAI.IsMailHours(offer.Building);

                    case TransferManager.TransferReason.RoadMaintenance: // road segments request snow amd road maintenance
                    case TransferManager.TransferReason.Snow:
                        return RealTimeBuildingAI.IsMaintenanceSnowRoadServiceHours(offer.NetSegment);

                    case TransferManager.TransferReason.ParkMaintenance: // park buildings request maintenance
                        return RealTimeBuildingAI.IsParkMaintenanceHours(offer.Building);

                    default:
                        return true;
                }
            }
        }
    }
}

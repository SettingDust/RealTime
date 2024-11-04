// OutsideConnectionAIPatch.cs

namespace RealTime.Patches
{
    using System.Reflection;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;

    /// <summary>
    /// A static class that provides the patch objects for the outside connections AI.
    /// </summary>
    [HarmonyPatch]
    internal static class OutsideConnectionAIPatch
    {
        /// <summary>Gets or sets the spare time behavior simulation.</summary>
        public static ISpareTimeBehavior SpareTimeBehavior { get; set; }

        [HarmonyPatch]
        private sealed class OutsideConnectionAI_DummyTrafficProbability
        {
            [HarmonyPatch(typeof(OutsideConnectionAI), "DummyTrafficProbability")]
            [HarmonyPostfix]
            private static void Postfix(ref int __result)
            {
                if(SpareTimeBehavior != null)
                {
                    __result = SpareTimeBehavior.SetDummyTrafficProbability(__result);
                }
            }
        }

        [HarmonyPatch]
        private sealed class OutsideConnectionAI_SimulationStep
        {
            [HarmonyPatch(typeof(OutsideConnectionAI), "SimulationStep")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building data)
            {
                if ((Singleton<ToolManager>.instance.m_properties.m_mode & ItemClass.Availability.Game) != 0)
                {
                    var instance = Singleton<SimulationManager>.instance;
                    var instance2 = Singleton<TransferManager>.instance;
                    var offer = default(TransferManager.TransferOffer);
                    offer.Building = buildingID;
                    offer.Unlimited = true;
                    offer.Position = data.m_position * (instance.m_randomizer.Int32(100, 400) * 0.01f);
                    offer.Active = true;
                    offer.Amount = 1;
                    offer.Priority = instance.m_randomizer.Int32(8u);
                    int[] m_incomingAmount = (int[])typeof(TransferManager).GetField("m_incomingAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance2);
                    int[] m_outgoingAmount = (int[])typeof(TransferManager).GetField("m_outgoingAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance2);
                    int leave_city0_in = m_incomingAmount[(int)TransferManager.TransferReason.LeaveCity0];
                    int leave_city1_in = m_incomingAmount[(int)TransferManager.TransferReason.LeaveCity1];
                    int leave_city2_in = m_incomingAmount[(int)TransferManager.TransferReason.LeaveCity2];
                    int leave_city0_out = m_outgoingAmount[(int)TransferManager.TransferReason.LeaveCity0];
                    int leave_city1_out = m_outgoingAmount[(int)TransferManager.TransferReason.LeaveCity1];
                    int leave_city2_out = m_outgoingAmount[(int)TransferManager.TransferReason.LeaveCity2];
                    if (leave_city0_out < leave_city0_in)
                    {
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.LeaveCity0, offer);
                    }
                    if (leave_city1_out < leave_city1_in)
                    {
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.LeaveCity1, offer);
                    }
                    if (leave_city2_out < leave_city2_in)
                    {
                        instance2.AddOutgoingOffer(TransferManager.TransferReason.LeaveCity2, offer);
                    }
                }
            }
        }
    }
}

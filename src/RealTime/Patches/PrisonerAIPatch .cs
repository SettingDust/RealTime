namespace RealTime.Patches
{
    using System;
    using ColossalFramework;
    using HarmonyLib;

    [HarmonyPatch(typeof(PrisonerAI))]
    public static class PrisonerAIPatch
    {

        [HarmonyPatch(typeof(PrisonerAI), "GetLocalizedStatus",
            new Type[] { typeof(ushort), typeof(CitizenInstance), typeof(InstanceID) },
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Out })]
        [HarmonyPostfix]
        public static void Postfix1(PrisonerAI __instance, ushort instanceID, ref CitizenInstance data, out InstanceID target, ref string __result)
        {
            ushort targetBuilding = data.m_targetBuilding;
            if (targetBuilding != 0)
            {
                target = InstanceID.Empty;
                target.Building = targetBuilding;
                __result = "Serving time at ";
            }
            else
            {
                target = InstanceID.Empty;
            }
        }

        [HarmonyPatch(typeof(PrisonerAI), "GetLocalizedStatus",
            new Type[] { typeof(uint), typeof(Citizen), typeof(InstanceID) },
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Out })]
        [HarmonyPostfix]
        public static void Postfix2(PrisonerAI __instance, uint citizenID, ref Citizen data, out InstanceID target, ref string __result)
        {
            var instance = Singleton<CitizenManager>.instance;
            ushort instance2 = data.m_instance;
            if (instance2 != 0)
            {
                Postfix1(__instance, instance2, ref instance.m_instances.m_buffer[instance2], out target, ref __result);
            }
            else if (data.m_visitBuilding != 0)
            {
                target = InstanceID.Empty;
                target.Building = data.m_visitBuilding;
                __result = "Serving time at ";
            }
            else
            {
                target = InstanceID.Empty;
                __result = ColossalFramework.Globalization.Locale.Get("CITIZEN_STATUS_CONFUSED");
            }
        }
    }
}

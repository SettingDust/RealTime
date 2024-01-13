namespace RealTime.CustomAI
{
    using System.Collections.Generic;
    using RealTime.Core;

    internal static class BuildingWorkTimeManager
    {
        public static Dictionary<ushort, WorkTime> BuildingsWorkTime;

        public struct WorkTime
        {
            public bool WorkAtNight;
            public bool WorkAtWeekands;
            public bool HasExtendedWorkShift;
            public bool HasContinuousWorkShift;
            public int WorkShifts;
        }

        public static void Init()
        {
            if (BuildingsWorkTime == null)
            {
                BuildingsWorkTime = new Dictionary<ushort, WorkTime>();
            }
        }

        public static void Deinit() => BuildingsWorkTime = new Dictionary<ushort, WorkTime>();

        internal static WorkTime GetBuildingWorkTime(ushort buildingID) => !BuildingsWorkTime.TryGetValue(buildingID, out var workTime) ? default : workTime;

        internal static void CreateBuildingWorkTime(ushort buildingID, BuildingInfo buildingInfo)
        {
            if (!BuildingsWorkTime.TryGetValue(buildingID, out _))
            {
                var service = buildingInfo.m_class.m_service;
                var sub_service = buildingInfo.m_class.m_subService;

                bool OpenAtNight = IsBuildingActiveAtNight(service, sub_service);
                bool OpenOnWeekends = IsBuildingActiveOnWeekend(service, sub_service);

                if(!OpenAtNight && service == ItemClass.Service.Commercial && sub_service == ItemClass.SubService.CommercialLow)
                {
                    OpenAtNight = ShouldOccur(RealTimeMod.configProvider.Configuration.OpenCommercialAtNightQuota);
                }

                if (!OpenOnWeekends && service == ItemClass.Service.Commercial && sub_service == ItemClass.SubService.CommercialLow)
                {
                    OpenOnWeekends = ShouldOccur(RealTimeMod.configProvider.Configuration.OpenCommercialAtWeekendsQuota);
                }

                bool ExtendedWorkShift = HasExtendedFirstWorkShift(service, sub_service);
                bool ContinuousWorkShift = HasContinuousWorkShift(service, sub_service);

                if (!ExtendedWorkShift && service == ItemClass.Service.Commercial)
                {
                    ExtendedWorkShift = ShouldOccur(50);
                    if(ExtendedWorkShift)
                    {
                        ContinuousWorkShift = false;
                    }
                }

                if (!ExtendedWorkShift && !ContinuousWorkShift && service == ItemClass.Service.Commercial)
                {
                    ContinuousWorkShift = ShouldOccur(50);
                }

                int WorkShifts = 2;

                if (ContinuousWorkShift && !OpenAtNight)
                {
                    WorkShifts = 1;
                }

                if (OpenAtNight)
                {
                    WorkShifts = ContinuousWorkShift ? 2 : 3;
                }

                if(buildingInfo.m_class.m_service == ItemClass.Service.Education)
                {
                    if(buildingInfo.m_class.m_level == ItemClass.Level.Level1 || buildingInfo.m_class.m_level == ItemClass.Level.Level2)
                    {
                        WorkShifts = 1;
                    }
                    else
                    {
                        WorkShifts = 2;
                    }
                }

                var workTime = new WorkTime()
                {
                    WorkAtNight = OpenAtNight,
                    WorkAtWeekands = OpenOnWeekends,
                    HasExtendedWorkShift = ExtendedWorkShift,
                    HasContinuousWorkShift = ContinuousWorkShift,
                    WorkShifts = WorkShifts
                };
                BuildingsWorkTime.Add(buildingID, workTime);
            }
        }

        public static void SetBuildingWorkTime(ushort buildingID, WorkTime workTime) => BuildingsWorkTime[buildingID] = workTime;

        public static void RemoveBuildingWorkTime(ushort buildingID) => BuildingsWorkTime.Remove(buildingID);

        private static bool ShouldOccur(uint probability) => SimulationManager.instance.m_randomizer.Int32(100u) < probability;

        private static bool IsBuildingActiveAtNight(ItemClass.Service service, ItemClass.SubService subService)
        {
            switch (subService)
            {
                case ItemClass.SubService.IndustrialOil:
                case ItemClass.SubService.IndustrialOre:
                case ItemClass.SubService.PlayerIndustryOre:
                case ItemClass.SubService.PlayerIndustryOil:
                    return true;
            }

            switch (service)
            {
                case ItemClass.Service.Commercial when subService == ItemClass.SubService.CommercialTourist:
                case ItemClass.Service.Commercial when subService == ItemClass.SubService.CommercialLeisure:
                case ItemClass.Service.Tourism:
                case ItemClass.Service.Hotel:
                case ItemClass.Service.PoliceDepartment:
                case ItemClass.Service.FireDepartment:
                case ItemClass.Service.PublicTransport:
                case ItemClass.Service.Disaster:
                case ItemClass.Service.Electricity:
                case ItemClass.Service.Water:
                case ItemClass.Service.HealthCare:
                case ItemClass.Service.Garbage:
                case ItemClass.Service.Road:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsBuildingActiveOnWeekend(ItemClass.Service service, ItemClass.SubService subService)
        {
            switch (service)
            {
                case ItemClass.Service.Commercial when subService == ItemClass.SubService.CommercialTourist:
                case ItemClass.Service.Commercial when subService == ItemClass.SubService.CommercialLeisure:
                case ItemClass.Service.Industrial when subService != ItemClass.SubService.IndustrialGeneric:
                case ItemClass.Service.PlayerIndustry:
                case ItemClass.Service.Tourism:
                case ItemClass.Service.Electricity:
                case ItemClass.Service.Water:
                case ItemClass.Service.Beautification:
                case ItemClass.Service.HealthCare:
                case ItemClass.Service.PoliceDepartment:
                case ItemClass.Service.FireDepartment:
                case ItemClass.Service.PublicTransport:
                case ItemClass.Service.Disaster:
                case ItemClass.Service.Monument:
                case ItemClass.Service.Garbage:
                case ItemClass.Service.Road:
                case ItemClass.Service.Museums:
                case ItemClass.Service.VarsitySports:
                case ItemClass.Service.Fishing:
                    return true;

                default:
                    return false;
            }
        }

        private static bool HasExtendedFirstWorkShift(ItemClass.Service service, ItemClass.SubService subService)
        {
            switch (service)
            {
                case ItemClass.Service.Beautification:
                case ItemClass.Service.Education:
                case ItemClass.Service.PlayerIndustry:
                case ItemClass.Service.PlayerEducation:
                case ItemClass.Service.Fishing:
                case ItemClass.Service.Industrial
                    when subService == ItemClass.SubService.IndustrialFarming || subService == ItemClass.SubService.IndustrialForestry:
                    return true;

                default:
                    return false;
            }
        }

        private static bool HasContinuousWorkShift(ItemClass.Service service, ItemClass.SubService subService)
        {
            switch (service)
            {
                case ItemClass.Service.HealthCare:
                case ItemClass.Service.PoliceDepartment:
                case ItemClass.Service.FireDepartment:
                case ItemClass.Service.Disaster:
                    return true;

                default:
                    return false;
            }
        }

    }

}

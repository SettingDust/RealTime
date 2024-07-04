// BuildingWorkTimeManager.cs

namespace RealTime.CustomAI
{
    using System.Collections.Generic;
    using System.Linq;
    using RealTime.Core;
    using RealTime.GameConnection;

    internal static class BuildingWorkTimeManager
    {
        public static Dictionary<ushort, WorkTime> BuildingsWorkTime;

        internal static List<WorkTimePrefab> BuildingsWorkTimePrefabs;

        private static readonly string[] CarParkingBuildings = ["parking", "garage", "car park", "Parking", "Car Port", "Garage", "Car Park"];

        public struct WorkTime
        {
            public bool WorkAtNight;
            public bool WorkAtWeekands;
            public bool HasExtendedWorkShift;
            public bool HasContinuousWorkShift;
            public int WorkShifts;
        }

        internal struct WorkTimePrefab
        {
            public string InfoName;
            public string BuildingAI;
            public bool WorkAtNight;
            public bool WorkAtWeekands;
            public bool HasExtendedWorkShift;
            public bool HasContinuousWorkShift;
            public int WorkShifts;
        }

        public static void Init()
        {
            BuildingsWorkTime = [];
            BuildingsWorkTimePrefabs = [];
        }

        public static void Deinit()
        {
            BuildingsWorkTime = [];
            BuildingsWorkTimePrefabs = [];
        }

        internal static WorkTime GetBuildingWorkTime(ushort buildingID)
        {
            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingID].Info;
            if (!BuildingsWorkTime.TryGetValue(buildingID, out var workTime) && buildingInfo.m_class.m_service != ItemClass.Service.Residential && buildingInfo.GetAI() is not ResidentialBuildingAI && !RealTimeBuildingAI.IsAreaResidentalBuilding(buildingID))
            {
                workTime = CreateBuildingWorkTime(buildingID, buildingInfo);
            }

            return workTime;
        }

        public static WorkTimePrefab GetPrefab(ushort buildingID, BuildingInfo buildingInfo)
        {
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;
            int index = BuildingsWorkTimePrefabs.FindIndex(item => item.InfoName == buildingInfo.name && item.BuildingAI == BuildingAIstr);
            if (index != -1)
            {
                return BuildingsWorkTimePrefabs[index];
            }
            else
            {
                var buildingWorkTime = CreateBuildingWorkTime(buildingID, buildingInfo);

                WorkTimePrefab newPrefabRecord = new()
                {
                    InfoName = buildingInfo.name,
                    BuildingAI = BuildingAIstr,
                    WorkAtNight = buildingWorkTime.WorkAtNight,
                    WorkAtWeekands = buildingWorkTime.WorkAtWeekands,
                    HasExtendedWorkShift = buildingWorkTime.HasExtendedWorkShift,
                    HasContinuousWorkShift = buildingWorkTime.HasContinuousWorkShift,
                    WorkShifts = buildingWorkTime.WorkShifts
                };
                BuildingsWorkTimePrefabs.Add(newPrefabRecord);
                return newPrefabRecord;
            }
        }

        public static void SetPrefab(WorkTimePrefab workTimePrefab)
        {
            int index = BuildingsWorkTimePrefabs.FindIndex(item => item.InfoName == workTimePrefab.InfoName && item.BuildingAI == workTimePrefab.BuildingAI);
            if (index != -1)
            {
                BuildingsWorkTimePrefabs[index] = workTimePrefab;
            }            
        }

        internal static WorkTime CreateBuildingWorkTime(ushort buildingID, BuildingInfo buildingInfo)
        {
            if (BuildingsWorkTime.TryGetValue(buildingID, out var oldWorkTime))
            {
                return oldWorkTime;
            }
            var service = buildingInfo.m_class.m_service;
            var sub_service = buildingInfo.m_class.m_subService;
            var level = buildingInfo.m_class.m_level;

            bool ExtendedWorkShift = HasExtendedFirstWorkShift(service, sub_service, level);
            bool ContinuousWorkShift = HasContinuousWorkShift(service, sub_service, level, ExtendedWorkShift);

            bool OpenAtNight = IsBuildingActiveAtNight(service, sub_service, level);
            bool OpenOnWeekends = IsBuildingActiveOnWeekend(service, sub_service, level);

            if(BuildingManagerConnection.IsHotel(buildingID) || RealTimeBuildingAI.IsAreaMainBuilding(buildingID) || RealTimeBuildingAI.IsWarehouseBuilding(buildingID))
            {
                OpenAtNight = true;
                OpenOnWeekends = true;
            }
            else if(service == ItemClass.Service.Beautification && sub_service == ItemClass.SubService.BeautificationParks)
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[buildingID].m_position;
                byte parkId = DistrictManager.instance.GetPark(position);
                if (parkId != 0 && (DistrictManager.instance.m_parks.m_buffer[parkId].m_parkPolicies & DistrictPolicies.Park.NightTours) != 0)
                {
                    OpenAtNight = true;
                }
            }
            else if (RealTimeBuildingAI.IsEssentialIndustryBuilding(buildingID) && (sub_service == ItemClass.SubService.PlayerIndustryFarming || sub_service == ItemClass.SubService.PlayerIndustryForestry))
            {
                OpenAtNight = true;
            }
            else if (RealTimeBuildingAI.IsRecreationalCareBuilding(buildingID))
            {
                OpenAtNight = false;
                OpenOnWeekends = true;
                ExtendedWorkShift = false;
                ContinuousWorkShift = false;
            }

            if(CarParkingBuildings.Any(s => buildingInfo.name.Contains(s)))
            {
                OpenAtNight = true;
                OpenOnWeekends = true;
                ExtendedWorkShift = false;
                ContinuousWorkShift = false;
            }

            int WorkShifts = GetBuildingWorkShiftCount(service, sub_service, buildingInfo, OpenAtNight, ContinuousWorkShift);

            var workTime = new WorkTime()
            {
                WorkAtNight = OpenAtNight,
                WorkAtWeekands = OpenOnWeekends,
                HasExtendedWorkShift = ExtendedWorkShift,
                HasContinuousWorkShift = ContinuousWorkShift,
                WorkShifts = WorkShifts
            };

            BuildingsWorkTime.Add(buildingID, workTime);

            return workTime;
        }

        public static void SetBuildingWorkTime(ushort buildingID, WorkTime workTime) => BuildingsWorkTime[buildingID] = workTime;

        public static void RemoveBuildingWorkTime(ushort buildingID) => BuildingsWorkTime.Remove(buildingID);

        private static bool ShouldOccur(uint probability) => SimulationManager.instance.m_randomizer.Int32(100u) < probability;

        // has 3 normal shifts or 2 continous shifts
        private static bool IsBuildingActiveAtNight(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
        {
            switch (subService)
            {
                case ItemClass.SubService.CommercialTourist:
                case ItemClass.SubService.CommercialLeisure:
                case ItemClass.SubService.CommercialLow when ShouldOccur(RealTimeMod.configProvider.Configuration.OpenLowCommercialAtNightQuota):
                case ItemClass.SubService.IndustrialOil:
                case ItemClass.SubService.IndustrialOre:
                case ItemClass.SubService.PlayerIndustryOre:
                case ItemClass.SubService.PlayerIndustryOil:
                    return true;
            }

            switch (service)
            {
                case ItemClass.Service.Industrial:
                case ItemClass.Service.Tourism:
                case ItemClass.Service.Electricity:
                case ItemClass.Service.Water:
                case ItemClass.Service.HealthCare when level <= ItemClass.Level.Level3:
                case ItemClass.Service.PoliceDepartment:
                case ItemClass.Service.FireDepartment:
                case ItemClass.Service.PublicTransport when subService != ItemClass.SubService.PublicTransportPost:
                case ItemClass.Service.Disaster:
                case ItemClass.Service.Natural:
                case ItemClass.Service.Garbage:
                case ItemClass.Service.Road:
                case ItemClass.Service.Hotel:
                case ItemClass.Service.ServicePoint:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsBuildingActiveOnWeekend(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
        {
            switch (subService)
            {
                case ItemClass.SubService.CommercialTourist:
                case ItemClass.SubService.CommercialLeisure:
                    return true;
            }

            switch (service)
            {
                case ItemClass.Service.PlayerIndustry:
                case ItemClass.Service.Industrial:
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
                case ItemClass.Service.ServicePoint:
                case ItemClass.Service.Hotel:
                case ItemClass.Service.Commercial when ShouldOccur(RealTimeMod.configProvider.Configuration.OpenCommercialAtWeekendsQuota):
                    return true;

                default:
                    return false;
            }
        }

        private static bool HasExtendedFirstWorkShift(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
        {
            switch (service)
            {
                case ItemClass.Service.Commercial when ShouldOccur(50):
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

        private static bool HasContinuousWorkShift(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, bool extendedWorkShift)
        {
            switch (subService)
            {
                case ItemClass.SubService.CommercialLow when !extendedWorkShift && ShouldOccur(50):
                    return true;
            }

            switch (service)
            {
                case ItemClass.Service.HealthCare when level <= ItemClass.Level.Level3:
                case ItemClass.Service.PoliceDepartment when subService != ItemClass.SubService.PoliceDepartmentBank:
                case ItemClass.Service.FireDepartment:
                case ItemClass.Service.Disaster:
                    return true;

                default:
                    return false;
            }
        }

        private static int GetBuildingWorkShiftCount(ItemClass.Service service, ItemClass.SubService subService, BuildingInfo buildingInfo, bool activeAtNight, bool continuousWorkShift)
        {
            if(activeAtNight)
            {
                if(continuousWorkShift)
                {
                    return 2;
                }
                return 3;
            }

            switch (service)
            {
                case ItemClass.Service.Office:
                case ItemClass.Service.Education when buildingInfo.m_class.m_level == ItemClass.Level.Level1 || buildingInfo.m_class.m_level == ItemClass.Level.Level2:
                case ItemClass.Service.PlayerIndustry
                    when subService == ItemClass.SubService.PlayerIndustryForestry || subService == ItemClass.SubService.PlayerIndustryFarming:
                case ItemClass.Service.Industrial
                    when subService == ItemClass.SubService.IndustrialForestry || subService == ItemClass.SubService.IndustrialFarming:
                case ItemClass.Service.Fishing:
                case ItemClass.Service.PoliceDepartment when subService == ItemClass.SubService.PoliceDepartmentBank:
                case ItemClass.Service.PublicTransport when subService == ItemClass.SubService.PublicTransportPost:
                    return 1;

                case ItemClass.Service.Beautification:
                case ItemClass.Service.Monument:
                case ItemClass.Service.Citizen:
                case ItemClass.Service.VarsitySports:
                case ItemClass.Service.PlayerEducation:
                case ItemClass.Service.Education when buildingInfo.m_class.m_level == ItemClass.Level.Level3:
                case ItemClass.Service.Commercial when ShouldOccur(RealTimeMod.configProvider.Configuration.OpenCommercialSecondShiftQuota):
                case ItemClass.Service.HealthCare when buildingInfo.GetAI() is SaunaAI:
                    return 2;

                default:
                    return 1;
            }
        }

    }

}

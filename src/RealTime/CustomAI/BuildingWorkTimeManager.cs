// BuildingWorkTimeManager.cs

namespace RealTime.CustomAI
{
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using RealTime.Core;
    using RealTime.GameConnection;

    internal static class BuildingWorkTimeManager
    {
        public static Dictionary<ushort, WorkTime> BuildingsWorkTime;

        public static List<WorkTimePrefab> BuildingsWorkTimePrefabs;

        private static readonly string[] CarParkingBuildings = ["parking", "garage", "car park", "Parking", "Car Port", "Garage", "Car Park"];

        public static Dictionary<string, int> HotelNamesList;

        public struct WorkTime
        {
            public bool WorkAtNight;
            public bool WorkAtWeekands;
            public bool HasExtendedWorkShift;
            public bool HasContinuousWorkShift;
            public int WorkShifts;
            public bool IsDefault;
            public bool IsPrefab;
            public bool IsGlobal;
            public bool IsLocked;
        }

        public struct WorkTimePrefab
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
            HotelNamesList = GetHotelNames();
        }

        public static void Deinit()
        {
            BuildingsWorkTime = [];
            BuildingsWorkTimePrefabs = [];
            HotelNamesList = GetHotelNames();
        }

        public static Dictionary<string, int> GetHotelNames() => new()
        {
            { "4x4_Tourist Hotel", 108 },
            { "4x4_Tourist Hotel2", 112 },
            { "4x4_Tourist Hotel3", 75 },
            { "4x3_Beach Hotel", 112 },
            { "4x3_Beach Hotel2", 80 },
            { "4x3_Beach Hotel3", 75 },
            { "2x2_Hotel01", 60 },
            { "3x2_Hotel01", 224 },
            { "1x1_Hotel01", 10 },
            { "3x2_Hotel02", 144 },
            { "4x3_winter_hotel01", 80 },
            { "4x3_winter_hotel02", 80 },
            { "PDX11_Hotel_kikyo", 320 },
            { "925095879.Paradise Beach Rental 2x2_Data", 4 },
            { "925091428.Paradise Beach Rental 1x2_Data", 4 },
            { "925090029.Paradise Beach Rental 2x3_Data", 2 },
            { "548728769.Breakwater Hotel_Data", 46 },
            { "542485582.Century Hotel_Data", 32 },
            { "3228401893.Winter 4x4L1CT C-Hotel Cinema_Data", 75 },
            { "3223997164.Winter 3x4 L2CT Swedavia Hotel_Data", 80 },
            { "3224669875.Winter 3x4 L2CT MarriottGameStop_Data", 80 },
            { "3224669875.Winter 3x4 L2CT Clarion Hotel_Data", 80 },
            { "622253956.Apex Hotel_Data", 324 },
            { "878187357.Blue Skies Holiday Inn_Data", 160 },
            { "2071136289.Monaco Beach Hotel L2 4x4_Data", 112 },
            { "2905863202.Aparthotel Ortop, Cuba_Data", 130 },
            { "879681170.Skyline Hotel_Data", 80 },
            { "554199404.Yggdrasil_Data", 300 },
            { "974500802.Holden Hotel 5 Star_Data", 132 },
            { "974501653.Holden Capital Hotel_Data", 132 },
            { "3273240208.The Earl Hotel_Data", 190 },
            { "646955796.Obsidian_Data", 190 },
            { "548489294.Crescent_Data", 290 },
            { "1563919076.Tokyo-INN_Data", 80 },
            { "2810293251.Sanford Hotel Lowrise_Data", 42 },
            { "2908925316.Executive Hotel CDE Paraguay_Data", 24 },
            { "3043923218.Hotel Europe (After Dark DLC)_Data", 48 },
            { "3047106252.Black Eagle Hotel (After Dark)_Data", 30 },
            { "3044294296.The Court Hotel (After Dark DLC)_Data", 16 },
            { "3045979344.The Ship Inn (After Dark DLC)_Data", 16 }
        };

        public static int GetIndex(string infoName, string buildingAIstr)
        {
            string defaultBuildingAIstr = "";
            if (buildingAIstr == "ExtendedBankOfficeAI")
            {
                defaultBuildingAIstr = "BankOfficeAI";
            }
            else if (buildingAIstr == "BankOfficeAI")
            {
                defaultBuildingAIstr = "ExtendedBankOfficeAI";
            }
            else if (buildingAIstr == "ExtendedPostOfficeAI")
            {
                defaultBuildingAIstr = "PostOfficeAI";
            }
            else if (buildingAIstr == "PostOfficeAI")
            {
                defaultBuildingAIstr = "ExtendedPostOfficeAI";
            }
            int index = BuildingsWorkTimePrefabs.FindIndex(item => item.InfoName == infoName &&
            defaultBuildingAIstr != "" ? (item.BuildingAI == buildingAIstr || item.BuildingAI == defaultBuildingAIstr) : item.BuildingAI == buildingAIstr);
            return index;
        }

        public static bool PrefabExist(BuildingInfo buildingInfo)
        {
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;
            int index = GetIndex(buildingInfo.name, BuildingAIstr);
            return index != -1;
        }

        public static WorkTimePrefab GetPrefab(BuildingInfo buildingInfo)
        {
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;
            int index = GetIndex(buildingInfo.name, BuildingAIstr);
            return index != -1 ? BuildingsWorkTimePrefabs[index] : default;
        }

        public static void SetPrefab(WorkTimePrefab workTimePrefab)
        {
            int index = GetIndex(workTimePrefab.InfoName, workTimePrefab.BuildingAI);
            if (index != -1)
            {
                BuildingsWorkTimePrefabs[index] = workTimePrefab;
            }            
        }

        public static void CreatePrefab(WorkTimePrefab workTimePrefab)
        {
            int index = GetIndex(workTimePrefab.InfoName, workTimePrefab.BuildingAI);
            if (index == -1)
            {
                BuildingsWorkTimePrefabs.Add(workTimePrefab);
            }
        }

        public static void RemovePrefab(WorkTimePrefab workTimePrefab)
        {
            int index = BuildingsWorkTimePrefabs.FindIndex(item => item.InfoName == workTimePrefab.InfoName && item.BuildingAI == workTimePrefab.BuildingAI);
            if (index != -1)
            {
                BuildingsWorkTimePrefabs.RemoveAt(index);
            }
        }

        public static WorkTime CreateBuildingWorkTime(ushort buildingID, BuildingInfo buildingInfo)
        {
            var workTime = CreateDefaultBuildingWorkTime(buildingID, buildingInfo);

            BuildingsWorkTime.Add(buildingID, workTime);

            return workTime;
        }

        public static bool ShouldHaveBuildingWorkTime(ushort buildingID)
        {
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            if(building.Info.GetAI() is OutsideConnectionAI)
            {
                return false;
            }

            var service = building.Info.m_class.m_service;
            var level = building.Info.m_class.m_level;

            bool IsCarPark = CarParkingBuildings.Any(s => building.Info.name.Contains(s));

            if (IsCarPark)
            {
                return false;
            }

            switch (service)
            {
                case ItemClass.Service.Residential:
                case ItemClass.Service.HealthCare when level >= ItemClass.Level.Level4 && BuildingManagerConnection.IsCimCareBuilding(buildingID):
                case ItemClass.Service.PlayerEducation when BuildingManagerConnection.IsAreaResidentalBuilding(buildingID):
                case ItemClass.Service.PlayerIndustry when BuildingManagerConnection.IsAreaResidentalBuilding(buildingID):
                    return false;
            }

            return true;
        }

        public static WorkTime CreateDefaultBuildingWorkTime(ushort buildingID, BuildingInfo buildingInfo)
        {
            var service = buildingInfo.m_class.m_service;
            var sub_service = buildingInfo.m_class.m_subService;
            var level = buildingInfo.m_class.m_level;
            var ai = buildingInfo.m_buildingAI;

            bool ExtendedWorkShift = HasExtendedFirstWorkShift(service, sub_service, level);
            bool ContinuousWorkShift = HasContinuousWorkShift(service, sub_service, level, ExtendedWorkShift);

            bool OpenAtNight = IsBuildingActiveAtNight(service, sub_service, level);
            bool OpenOnWeekends = IsBuildingActiveOnWeekend(service, sub_service, level);

            if (BuildingManagerConnection.IsHotel(buildingID) || BuildingManagerConnection.IsAreaMainBuilding(buildingID) || BuildingManagerConnection.IsWarehouseBuilding(buildingID))
            {
                OpenAtNight = true;
                OpenOnWeekends = true;
            }
            else if (service == ItemClass.Service.Beautification && sub_service == ItemClass.SubService.BeautificationParks)
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[buildingID].m_position;
                byte parkId = DistrictManager.instance.GetPark(position);
                if (parkId != 0 && (DistrictManager.instance.m_parks.m_buffer[parkId].m_parkPolicies & DistrictPolicies.Park.NightTours) != 0)
                {
                    OpenAtNight = true;
                }
            }
            else if (BuildingManagerConnection.IsEssentialIndustryBuilding(buildingID) && (sub_service == ItemClass.SubService.PlayerIndustryFarming || sub_service == ItemClass.SubService.PlayerIndustryForestry))
            {
                OpenAtNight = true;
            }
            else if (BuildingManagerConnection.IsRecreationalCareBuilding(buildingID))
            {
                OpenAtNight = false;
                OpenOnWeekends = true;
                ExtendedWorkShift = false;
                ContinuousWorkShift = false;
            }
            else if (BuildingManagerConnection.IsBuildingNoiseRestricted(buildingID))
            {
                OpenAtNight = false;
            }

            if (CarParkingBuildings.Any(s => buildingInfo.name.Contains(s)))
            {
                OpenAtNight = true;
                OpenOnWeekends = true;
                ExtendedWorkShift = false;
                ContinuousWorkShift = false;
            }

            int WorkShifts = GetBuildingWorkShiftCount(service, sub_service, level, ai, OpenAtNight, ContinuousWorkShift);

            var workTime = new WorkTime()
            {
                WorkAtNight = OpenAtNight,
                WorkAtWeekands = OpenOnWeekends,
                HasExtendedWorkShift = ExtendedWorkShift,
                HasContinuousWorkShift = ContinuousWorkShift,
                WorkShifts = WorkShifts,
                IsDefault = true,
                IsPrefab = false,
                IsGlobal = false,
                IsLocked = false
            };

            return workTime;
        }

        public static bool BuildingWorkTimeExist(ushort buildingID) => BuildingsWorkTime.ContainsKey(buildingID);

        public static WorkTime GetBuildingWorkTime(ushort buildingID) => BuildingsWorkTime.TryGetValue(buildingID, out var workTime) ? workTime : default;

        public static void SetBuildingWorkTime(ushort buildingID, WorkTime workTime)
        {
            if (BuildingsWorkTime.TryGetValue(buildingID, out var _))
            {
                BuildingsWorkTime[buildingID] = workTime;
            }    
        }

        public static void RemoveBuildingWorkTime(ushort buildingID)
        {
            if(BuildingsWorkTime.TryGetValue(buildingID, out var _))
            {
                BuildingsWorkTime.Remove(buildingID);
            }
        }

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
                case ItemClass.Service.PoliceDepartment when subService != ItemClass.SubService.PoliceDepartmentBank:
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
                case ItemClass.Service.PoliceDepartment when subService != ItemClass.SubService.PoliceDepartmentBank:
                case ItemClass.Service.FireDepartment:
                case ItemClass.Service.PublicTransport when subService != ItemClass.SubService.PublicTransportPost:
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

        private static int GetBuildingWorkShiftCount(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, BuildingAI ai, bool activeAtNight, bool continuousWorkShift)
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
                case ItemClass.Service.Education when level == ItemClass.Level.Level1 || level == ItemClass.Level.Level2:
                case ItemClass.Service.PlayerIndustry
                    when subService == ItemClass.SubService.PlayerIndustryForestry || subService == ItemClass.SubService.PlayerIndustryFarming:
                case ItemClass.Service.Industrial
                    when subService == ItemClass.SubService.IndustrialForestry || subService == ItemClass.SubService.IndustrialFarming:
                case ItemClass.Service.PoliceDepartment when subService == ItemClass.SubService.PoliceDepartmentBank:
                case ItemClass.Service.PublicTransport when subService == ItemClass.SubService.PublicTransportPost:
                    return 1;

                case ItemClass.Service.Beautification:
                case ItemClass.Service.Monument:
                case ItemClass.Service.Citizen:
                case ItemClass.Service.VarsitySports:
                case ItemClass.Service.PlayerEducation:
                case ItemClass.Service.Education when level == ItemClass.Level.Level3:
                case ItemClass.Service.Commercial when ShouldOccur(RealTimeMod.configProvider.Configuration.OpenCommercialSecondShiftQuota):
                case ItemClass.Service.HealthCare when ai is SaunaAI:
                case ItemClass.Service.Fishing when level == ItemClass.Level.Level1 && ai is MarketAI:
                    return 2;

                default:
                    return 1;
            }
        }

    }

}

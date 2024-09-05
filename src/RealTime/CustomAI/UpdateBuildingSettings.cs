namespace RealTime.CustomAI
{
    using System.Collections.Generic;
    using ColossalFramework;
    using RealTime.Config;

    internal static class UpdateBuildingSettings
    {
        internal static void ChangeBuildingLockStatus(ushort buildingID, bool LockStatus)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.IsLocked = LockStatus;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void SaveNewSettings(ushort buildingID, BuildingWorkTimeManager.WorkTime workTime)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkAtNight = workTime.WorkAtNight;
            buildingWorkTime.WorkAtWeekands = workTime.WorkAtWeekands;
            buildingWorkTime.HasExtendedWorkShift = workTime.HasExtendedWorkShift;
            buildingWorkTime.HasContinuousWorkShift = workTime.HasContinuousWorkShift;
            buildingWorkTime.WorkShifts = workTime.WorkShifts;
            buildingWorkTime.IsDefault = false;
            buildingWorkTime.IsPrefab = false;
            buildingWorkTime.IsGlobal = false;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void SetBuildingToPrefab(ushort buildingID, BuildingWorkTimeManager.WorkTimePrefab workTimePrefab)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkAtNight = workTimePrefab.WorkAtNight;
            buildingWorkTime.WorkAtWeekands = workTimePrefab.WorkAtWeekands;
            buildingWorkTime.HasExtendedWorkShift = workTimePrefab.HasExtendedWorkShift;
            buildingWorkTime.HasContinuousWorkShift = workTimePrefab.HasContinuousWorkShift;
            buildingWorkTime.WorkShifts = workTimePrefab.WorkShifts;
            buildingWorkTime.IsDefault = false;
            buildingWorkTime.IsPrefab = true;
            buildingWorkTime.IsGlobal = false;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void SetBuildingToGlobal(ushort buildingID, BuildingWorkTimeGlobal buildingWorkTimeGlobalConfig)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkAtNight = buildingWorkTimeGlobalConfig.WorkAtNight;
            buildingWorkTime.WorkAtWeekands = buildingWorkTimeGlobalConfig.WorkAtWeekands;
            buildingWorkTime.HasExtendedWorkShift = buildingWorkTimeGlobalConfig.HasExtendedWorkShift;
            buildingWorkTime.HasContinuousWorkShift = buildingWorkTimeGlobalConfig.HasContinuousWorkShift;
            buildingWorkTime.WorkShifts = buildingWorkTimeGlobalConfig.WorkShifts;
            buildingWorkTime.IsDefault = false;
            buildingWorkTime.IsPrefab = false;
            buildingWorkTime.IsGlobal = true;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void UpdateBuildingToDefaultSettings(ushort buildingID, BuildingWorkTimeManager.WorkTime newDefaultWorkTime)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkAtNight = newDefaultWorkTime.WorkAtNight;
            buildingWorkTime.WorkAtWeekands = newDefaultWorkTime.WorkAtWeekands;
            buildingWorkTime.HasExtendedWorkShift = newDefaultWorkTime.HasExtendedWorkShift;
            buildingWorkTime.HasContinuousWorkShift = newDefaultWorkTime.HasContinuousWorkShift;
            buildingWorkTime.WorkShifts = newDefaultWorkTime.WorkShifts;
            buildingWorkTime.IsDefault = true;
            buildingWorkTime.IsPrefab = false;
            buildingWorkTime.IsGlobal = false;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void CreatePrefabSettings(ushort buildingID, BuildingWorkTimeManager.WorkTime newWorkTime)
        {
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;
            string defaultBuildingAIstr = "";
            if (BuildingAIstr == "ExtendedBankOfficeAI")
            {
                defaultBuildingAIstr = "BankOfficeAI";
            }
            else if (BuildingAIstr == "BankOfficeAI")
            {
                defaultBuildingAIstr = "ExtendedBankOfficeAI";
            }
            else if (BuildingAIstr == "ExtendedPostOfficeAI")
            {
                defaultBuildingAIstr = "PostOfficeAI";
            }
            else if (BuildingAIstr == "PostOfficeAI")
            {
                defaultBuildingAIstr = "ExtendedPostOfficeAI";
            }
            var buildingsIdsList = new List<ushort>();

            foreach (var item in BuildingWorkTimeManager.BuildingsWorkTime)
            {
                var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                string buildingAIName = Info.GetAI().GetType().Name;
                if(Info.name == buildingInfo.name && !item.Value.IsLocked)
                {
                    if(defaultBuildingAIstr != "")
                    {
                        if(defaultBuildingAIstr == buildingAIName || BuildingAIstr == buildingAIName)
                        {
                            buildingsIdsList.Add(item.Key);
                        }
                    }
                    else if(BuildingAIstr == buildingAIName)
                    {
                        buildingsIdsList.Add(item.Key);
                    }
                }
            }

            // set new prefab settings according to the building current settings
            var buildingWorkTimePrefab = new BuildingWorkTimeManager.WorkTimePrefab
            {
                InfoName = buildingInfo.name,
                BuildingAI = BuildingAIstr,
                WorkAtNight = newWorkTime.WorkAtNight,
                WorkAtWeekands = newWorkTime.WorkAtWeekands,
                HasExtendedWorkShift = newWorkTime.HasExtendedWorkShift,
                HasContinuousWorkShift = newWorkTime.HasContinuousWorkShift,
                WorkShifts = newWorkTime.WorkShifts
            };

            foreach (ushort buildingId in buildingsIdsList)
            {
                var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);
                workTime.WorkAtNight = buildingWorkTimePrefab.WorkAtNight;
                workTime.WorkAtWeekands = buildingWorkTimePrefab.WorkAtWeekands;
                workTime.HasExtendedWorkShift = buildingWorkTimePrefab.HasExtendedWorkShift;
                workTime.HasContinuousWorkShift = buildingWorkTimePrefab.HasContinuousWorkShift;
                workTime.WorkShifts = buildingWorkTimePrefab.WorkShifts;
                workTime.IsDefault = false;
                workTime.IsPrefab = true;
                workTime.IsGlobal = false;
                BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
            }

            if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
            {
                // update the prefab
                var prefabRecord = BuildingWorkTimeManager.GetPrefab(buildingInfo);

                prefabRecord.WorkAtNight = buildingWorkTimePrefab.WorkAtNight;
                prefabRecord.WorkAtWeekands = buildingWorkTimePrefab.WorkAtWeekands;
                prefabRecord.HasExtendedWorkShift = buildingWorkTimePrefab.HasExtendedWorkShift;
                prefabRecord.HasContinuousWorkShift = buildingWorkTimePrefab.HasContinuousWorkShift;
                prefabRecord.WorkShifts = buildingWorkTimePrefab.WorkShifts;

                BuildingWorkTimeManager.SetPrefab(prefabRecord);
            }
            else
            {
                // create new prefab
                BuildingWorkTimeManager.CreatePrefab(buildingWorkTimePrefab);
            }
        }

        internal static void CreateGlobalSettings(ushort buildingID, BuildingWorkTimeManager.WorkTime newWorkTime)
        {
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;
            string defaultBuildingAIstr = "";
            if (BuildingAIstr == "ExtendedBankOfficeAI")
            {
                defaultBuildingAIstr = "BankOfficeAI";
            }
            else if (BuildingAIstr == "BankOfficeAI")
            {
                defaultBuildingAIstr = "ExtendedBankOfficeAI";
            }
            else if (BuildingAIstr == "ExtendedPostOfficeAI")
            {
                defaultBuildingAIstr = "PostOfficeAI";
            }
            else if (BuildingAIstr == "PostOfficeAI")
            {
                defaultBuildingAIstr = "ExtendedPostOfficeAI";
            }
            var buildingsIdsList = new List<ushort>();

            foreach (var item in BuildingWorkTimeManager.BuildingsWorkTime)
            {
                var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                string buildingAIName = Info.GetAI().GetType().Name;
                if (Info.name == buildingInfo.name && !item.Value.IsLocked)
                {
                    if (defaultBuildingAIstr != "")
                    {
                        if (defaultBuildingAIstr == buildingAIName || BuildingAIstr == buildingAIName)
                        {
                            buildingsIdsList.Add(item.Key);
                        }
                    }
                    else if (BuildingAIstr == buildingAIName)
                    {
                        buildingsIdsList.Add(item.Key);
                    }
                }
            }

            // set new global settings according to the building current settings
            var buildingWorkTimeGlobal = new BuildingWorkTimeGlobal
            {
                InfoName = buildingInfo.name,
                BuildingAI = BuildingAIstr,
                WorkAtNight = newWorkTime.WorkAtNight,
                WorkAtWeekands = newWorkTime.WorkAtWeekands,
                HasExtendedWorkShift = newWorkTime.HasExtendedWorkShift,
                HasContinuousWorkShift = newWorkTime.HasContinuousWorkShift,
                WorkShifts = newWorkTime.WorkShifts
            };

            foreach (ushort buildingId in buildingsIdsList)
            {
                var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);
                workTime.WorkAtNight = buildingWorkTimeGlobal.WorkAtNight;
                workTime.WorkAtWeekands = buildingWorkTimeGlobal.WorkAtWeekands;
                workTime.HasExtendedWorkShift = buildingWorkTimeGlobal.HasExtendedWorkShift;
                workTime.HasContinuousWorkShift = buildingWorkTimeGlobal.HasContinuousWorkShift;
                workTime.WorkShifts = buildingWorkTimeGlobal.WorkShifts;
                workTime.IsDefault = false;
                workTime.IsPrefab = false;
                workTime.IsGlobal = true;
                BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
            }

            // try get global settings and update them or create new global settings for this building type
            // if not exist and apply the settings to all the individual buildings
            var globalRecord = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);

            if (globalRecord != null)
            {
                globalRecord.WorkAtNight = buildingWorkTimeGlobal.WorkAtNight;
                globalRecord.WorkAtWeekands = buildingWorkTimeGlobal.WorkAtWeekands;
                globalRecord.HasExtendedWorkShift = buildingWorkTimeGlobal.HasExtendedWorkShift;
                globalRecord.HasContinuousWorkShift = buildingWorkTimeGlobal.HasContinuousWorkShift;
                globalRecord.WorkShifts = buildingWorkTimeGlobal.WorkShifts;

                BuildingWorkTimeGlobalConfig.Config.SetGlobalSettings(globalRecord);
            }
            else
            {
                BuildingWorkTimeGlobalConfig.Config.CreateGlobalSettings(buildingWorkTimeGlobal);
            }
        }

    }
}

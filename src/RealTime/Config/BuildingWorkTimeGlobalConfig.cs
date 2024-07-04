namespace RealTime.Config
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using UnityEngine;

    public class BuildingWorkTimeGlobalConfig
    {
        public List<BuildingWorkTimeGlobal> BuildingWorkTimeGlobalSettings = [];

        private const string optionsFileName = "RealTimeOperationHoursGlobal.xml";

        public bool ShowPanel { get; set; } = false;

        private static XmlSerializer Ser_ => new(typeof(BuildingWorkTimeGlobalConfig));

        private static BuildingWorkTimeGlobalConfig config_;

        public static BuildingWorkTimeGlobalConfig Config => config_ ??= Deserialize() ?? new BuildingWorkTimeGlobalConfig();

        public static void Reset() => config_ = new BuildingWorkTimeGlobalConfig();


        public BuildingWorkTimeGlobal GetGlobalSettings(string name, string buildingAI)
        {
            int index = BuildingWorkTimeGlobalSettings.FindIndex(item => item.InfoName == name && item.BuildingAI == buildingAI);
            if (index != -1)
            {
                return BuildingWorkTimeGlobalSettings[index];
            }
            else
            {
                BuildingWorkTimeGlobal newBuildingWorkTimeGlobal = new()
                {
                    InfoName = name,
                    BuildingAI = buildingAI,
                    WorkAtNight = false,
                    WorkAtWeekands = false,
                    HasExtendedWorkShift = false,
                    HasContinuousWorkShift = false,
                    WorkShifts = 1
                };
                BuildingWorkTimeGlobalSettings.Add(newBuildingWorkTimeGlobal);
                return newBuildingWorkTimeGlobal;
            }
        }

        public void SetGlobalSettings(BuildingWorkTimeGlobal buildingWorkTimeGlobal)
        {
            int index = BuildingWorkTimeGlobalSettings.FindIndex(item => item.InfoName == buildingWorkTimeGlobal.InfoName && item.BuildingAI == buildingWorkTimeGlobal.BuildingAI);
            BuildingWorkTimeGlobalSettings[index] = buildingWorkTimeGlobal;
        }

        public void ClearGlobalSettings() => BuildingWorkTimeGlobalSettings.Clear();

        public static BuildingWorkTimeGlobalConfig Deserialize()
        {
            try
            {
                if (File.Exists(GetXMLPath()))
                {
                    using FileStream stream = new(GetXMLPath(), FileMode.Open, FileAccess.Read);
                    return Ser_.Deserialize(stream) as BuildingWorkTimeGlobalConfig;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("RealTimeMod: " + ex.Message);
            }
            return null;
        }

        public void Serialize()
        {
            try
            {
                using var stream = new FileStream(GetXMLPath(), FileMode.Create, FileAccess.Write);
                Ser_.Serialize(stream, this);
            }
            catch (Exception ex)
            {
                Debug.LogError("RealTimeMod: " + ex.Message);
            }
        }

        public static string GetXMLPath()
        {
            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string CO_path = Path.Combine(fileName, "Colossal Order");
            string CS_path = Path.Combine(CO_path, "Cities_Skylines");
            string file_path = Path.Combine(CS_path, optionsFileName);
            return file_path;
        }

    }

    [XmlRoot("BuildingWorkTimeGlobal")]
    public class BuildingWorkTimeGlobal
    {
        [XmlAttribute("InfoName")]
        public string InfoName { get; set; }

        [XmlAttribute("BuildingAI")]
        public string BuildingAI { get; set; }

        [XmlAttribute("numOfApartments")]
        public bool WorkAtNight { get; set; }

        [XmlAttribute("numOfApartments")]
        public bool WorkAtWeekands { get; set; }

        [XmlAttribute("numOfApartments")]
        public bool HasExtendedWorkShift { get; set; }

        [XmlAttribute("numOfApartments")]
        public bool HasContinuousWorkShift { get; set; }

        [XmlAttribute("numOfApartments")]
        public int WorkShifts { get; set; }
    }
}

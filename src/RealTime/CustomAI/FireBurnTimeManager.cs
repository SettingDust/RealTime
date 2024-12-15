// FireBurnTimeManager.cs

namespace RealTime.CustomAI
{
    using System;
    using System.Collections.Generic;
    using RealTime.Simulation;

    public static class FireBurnTimeManager
    {
        public static Dictionary<ushort, BurnTime> FireBurnTime;

        public struct BurnTime
        {
            public DateTime StartDate;
            public float StartTime;
            public float Duration;
        }

        public static void Init() => FireBurnTime ??= [];

        public static void Deinit() => FireBurnTime = [];

        public static bool BuildingBurnTimeExist(ushort buildingID) => FireBurnTime.ContainsKey(buildingID);

        public static BurnTime GetBuildingBurnTime(ushort buildingID) => !FireBurnTime.TryGetValue(buildingID, out var burnTime) ? default : burnTime;

        internal static void CreateBuildingBurnTime(ushort buildingID, ITimeInfo timeInfo)
        {
            if (!FireBurnTime.TryGetValue(buildingID, out _))
            {
                float burnDuration = 0.5f; // UnityEngine.Random.Range(0.5f, 4f);
                var burnTime = new BurnTime()
                {
                    StartDate = timeInfo.Now.Date,
                    StartTime = timeInfo.CurrentHour,
                    Duration = burnDuration
                };
                FireBurnTime.Add(buildingID, burnTime);
            }
        }

        public static void SetBuildingBurnTime(ushort buildingID, BurnTime burnTime) => FireBurnTime[buildingID] = burnTime;


        public static void RemoveBuildingBurnTime(ushort buildingID) => FireBurnTime.Remove(buildingID);
    }

}

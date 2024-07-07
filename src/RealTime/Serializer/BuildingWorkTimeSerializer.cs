// BuildingWorkTimeSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using RealTime.CustomAI;
    using UnityEngine;

    public class BuildingWorkTimeSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iBUILDING_WORK_TIME_DATA_VERSION = 2;

        public static void SaveData(FastList<byte> Data)
        {
            // Write out metadata
            StorageData.WriteUInt16(iBUILDING_WORK_TIME_DATA_VERSION, Data);
            StorageData.WriteInt32(BuildingWorkTimeManager.BuildingsWorkTime.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in BuildingWorkTimeManager.BuildingsWorkTime)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);
                StorageData.WriteBool(kvp.Value.WorkAtNight, Data);
                StorageData.WriteBool(kvp.Value.WorkAtWeekands, Data);
                StorageData.WriteBool(kvp.Value.HasExtendedWorkShift, Data);
                StorageData.WriteBool(kvp.Value.HasContinuousWorkShift, Data);
                StorageData.WriteInt32(kvp.Value.WorkShifts, Data);
                StorageData.WriteBool(kvp.Value.IsDefault, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iBuildingWorkTimeVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("Global: " + iGlobalVersion + " BufferVersion: " + iBuildingWorkTimeVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                BuildingWorkTimeManager.BuildingsWorkTime ??= [];
                int BuildingWorkTime_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < BuildingWorkTime_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", BuildingWorkTime_Count, Data, ref iIndex);

                    ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    bool WorkAtNight = StorageData.ReadBool(Data, ref iIndex);
                    bool WorkAtWeekands = StorageData.ReadBool(Data, ref iIndex);
                    bool HasExtendedWorkShift = StorageData.ReadBool(Data, ref iIndex);
                    bool HasContinuousWorkShift = StorageData.ReadBool(Data, ref iIndex);
                    int WorkShifts = StorageData.ReadInt32(Data, ref iIndex);

                    var workTime = new BuildingWorkTimeManager.WorkTime()
                    {
                        WorkAtNight = WorkAtNight,
                        WorkAtWeekands = WorkAtWeekands,
                        HasExtendedWorkShift = HasExtendedWorkShift,
                        HasContinuousWorkShift = HasContinuousWorkShift,
                        WorkShifts = WorkShifts,
                        IsDefault = true
                    };

                    if (iBuildingWorkTimeVersion == 2)
                    {
                        workTime.IsDefault = StorageData.ReadBool(Data, ref iIndex);
                    }

                    BuildingWorkTimeManager.BuildingsWorkTime.Add(BuildingId, workTime);
                    CheckEndTuple($"Buffer({i})", iBuildingWorkTimeVersion, Data, ref iIndex);
                }
            }
        }

        private static void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"BuildingWorkTime Buffer start tuple not found at: {sTupleLocation}");
                }
            }
        }

        private static void CheckEndTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleEnd = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleEnd != uiTUPLE_END)
                {
                    throw new Exception($"BuildingWorkTime Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}

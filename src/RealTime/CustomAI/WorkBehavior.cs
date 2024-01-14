// <copyright file="WorkBehavior.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.CustomAI
{
    using System;
    using ColossalFramework;
    using RealTime.Config;
    using RealTime.GameConnection;
    using RealTime.Simulation;
    using SkyTools.Tools;
    using static ColossalFramework.DataBinding.BindPropertyByKey;
    using static Constants;

    /// <summary>
    /// A class containing methods for managing the citizens' work behavior.
    /// </summary>
    internal sealed class WorkBehavior : IWorkBehavior
    {
        private readonly RealTimeConfig config;
        private readonly IRandomizer randomizer;
        private readonly IBuildingManagerConnection buildingManager;
        private readonly ITimeInfo timeInfo;
        private readonly ITravelBehavior travelBehavior;

        private DateTime lunchBegin;
        private DateTime lunchEnd;

        /// <summary>Initializes a new instance of the <see cref="WorkBehavior"/> class.</summary>
        /// <param name="config">The configuration to run with.</param>
        /// <param name="randomizer">The randomizer implementation.</param>
        /// <param name="buildingManager">The building manager implementation.</param>
        /// <param name="timeInfo">The time information source.</param>
        /// <param name="travelBehavior">A behavior that provides simulation info for the citizens traveling.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public WorkBehavior(
            RealTimeConfig config,
            IRandomizer randomizer,
            IBuildingManagerConnection buildingManager,
            ITimeInfo timeInfo,
            ITravelBehavior travelBehavior)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
            this.buildingManager = buildingManager ?? throw new ArgumentNullException(nameof(buildingManager));
            this.timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
            this.travelBehavior = travelBehavior ?? throw new ArgumentNullException(nameof(travelBehavior));
        }

        /// <summary>
        /// Determines whether a building of specified id/>
        /// currently has working hours. Note that this method always returns <c>true</c> for residential buildings.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if a building of specified id/>
        /// currently has working hours; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBuildingWorking(ushort buildingId)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);

            if (!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)) && building.Info.m_class.m_service == ItemClass.Service.Residential)
            {
                BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingId);
            }

            // no one at work but
            if(GetWorkersInBuilding(buildingId) == 0)
            {
                return false;
            }


            if (config.IsWeekendEnabled && timeInfo.Now.IsWeekend())
            {
                return workTime.WorkAtWeekands;
            }
            if(timeInfo.IsNightTime)
            {
                return workTime.WorkAtNight;
            }

            float currentHour = timeInfo.CurrentHour;
            if (workTime.HasExtendedWorkShift)
            {
                float startHour = Math.Min(config.WakeUpHour, EarliestWakeUp);
                if (building.Info.m_class.m_service == ItemClass.Service.Education)
                {
                    if(building.Info.m_class.m_level == ItemClass.Level.Level1 || building.Info.m_class.m_level == ItemClass.Level.Level2)
                    {
                        if(workTime.WorkShifts == 2)
                        {
                            workTime.WorkShifts = 1;
                            BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                        }
                    }
                    if (workTime.WorkShifts == 1)
                    {
                        return currentHour >= startHour && currentHour < config.SchoolEnd;
                    }
                    return currentHour >= startHour && currentHour < 20;
                }
                if (workTime.WorkShifts == 1)
                {
                    return currentHour >= startHour && currentHour < config.WorkEnd;
                }
                return currentHour >= startHour && currentHour < 24;
            }
            else if (workTime.HasContinuousWorkShift)
            {
                if (workTime.WorkShifts == 1)
                {
                    return currentHour >= 8 && currentHour < 20;
                }
                return true; // two work shifts
            }
            else
            {
                if (workTime.WorkShifts == 1)
                {
                    float startHour = Math.Min(config.WakeUpHour, EarliestWakeUp);
                    return currentHour >= startHour && currentHour < config.WorkEnd;
                }
                else if (workTime.WorkShifts == 2)
                {
                    float startHour = Math.Min(config.WakeUpHour, EarliestWakeUp);
                    return currentHour >= startHour && currentHour < 24;
                }
                else
                {
                    return true; // three work shifts
                }
            }
        }

        /// <summary>Notifies this object that a new game day starts.</summary>
        public void BeginNewDay()
        {
            var today = timeInfo.Now.Date;
            lunchBegin = today.AddHours(config.LunchBegin);
            lunchEnd = today.AddHours(config.LunchEnd);
        }

        /// <summary>Updates the citizen's work shift parameters in the specified citizen's <paramref name="schedule"/>.</summary>
        /// <param name="schedule">The citizen's schedule to update the work shift in.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        public void UpdateWorkShift(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(schedule.WorkBuilding);
            if (schedule.WorkBuilding == 0 || citizenAge == Citizen.AgeGroup.Senior)
            {
                schedule.UpdateWorkShift(WorkShift.Unemployed, 0, 0, worksOnWeekends: false);
                return;
            }

            float workBegin, workEnd;
            var workShift = schedule.WorkShift;

            switch (citizenAge)
            {
                case Citizen.AgeGroup.Young:
                case Citizen.AgeGroup.Adult:
                    if (workShift == WorkShift.Unemployed)
                    {
                        workShift = GetWorkShift(workTime);
                    }
                    workBegin = config.WorkBegin;
                    workEnd = config.WorkEnd;
                    break;

                default:
                    return;
            }
            var service = buildingManager.GetBuildingService(schedule.WorkBuilding);
            switch (workShift)
            {
                case WorkShift.First when workTime.HasExtendedWorkShift:
                    float extendedShiftBegin = Math.Min(config.SchoolBegin, config.WakeUpHour);
                    if (service == ItemClass.Service.Education) // teachers
                    {
                        workBegin = Math.Min(EarliestWakeUp, extendedShiftBegin);
                    }
                    else
                    {
                        extendedShiftBegin = config.WakeUpHour;
                        workBegin = Math.Min(EarliestWakeUp, extendedShiftBegin);
                    }
                    workEnd = config.SchoolEnd;
                    break;

                case WorkShift.First:
                    workBegin = config.WorkBegin;
                    workEnd = config.WorkEnd;
                    break;

                case WorkShift.Second:
                    if (service == ItemClass.Service.Education) // night class at university
                    {
                        workBegin = config.SchoolEnd;
                        workEnd = 20;
                    }
                    else
                    {
                        workBegin = config.WorkEnd;
                        workEnd = 0;
                    }
                    break;

                case WorkShift.Night:
                    workEnd = config.WorkBegin;
                    workBegin = 0;
                    break;

                case WorkShift.ContinuousDay:
                    workBegin = 8;
                    workEnd = 20;
                    break;

                case WorkShift.ContinuousNight:
                    workBegin = 20;
                    workEnd = 8;
                    break;
            }

            schedule.UpdateWorkShift(workShift, workBegin, workEnd, workTime.WorkAtWeekands);
        }

        /// <summary>Updates the citizen's work schedule by determining the time for going to work.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="currentBuilding">The ID of the building where the citizen is currently located.</param>
        /// <param name="simulationCycle">The duration (in hours) of a full citizens simulation cycle.</param>
        /// <returns><c>true</c> if work was scheduled; otherwise, <c>false</c>.</returns>
        public bool ScheduleGoToWork(ref CitizenSchedule schedule, ushort currentBuilding, float simulationCycle)
        {
            if (schedule.CurrentState == ResidentState.AtSchoolOrWork || schedule.CurrentState == ResidentState.AtWork)
            {
                return false;
            }

            var now = timeInfo.Now;
            if (config.IsWeekendEnabled && now.IsWeekend() && !schedule.WorksOnWeekends)
            {
                return false;
            }

            float travelTime = GetTravelTimeToWork(ref schedule, currentBuilding);

            var workEndTime = now.FutureHour(schedule.WorkShiftEndHour);
            var departureTime = now.FutureHour(schedule.WorkShiftStartHour - travelTime - simulationCycle);
            if (departureTime > workEndTime && now.AddHours(travelTime + simulationCycle) < workEndTime)
            {
                departureTime = now;
            }

            schedule.Schedule(ResidentState.AtWork, departureTime);
            return true;
        }

        /// <summary>Updates the citizen's work schedule by determining the lunch time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The citizen's age.</param>
        /// <returns><c>true</c> if a lunch time was scheduled; otherwise, <c>false</c>.</returns>
        public bool ScheduleLunch(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            if (timeInfo.Now < lunchBegin
                && schedule.WorkStatus == WorkStatus.Working
                && (schedule.WorkShift == WorkShift.First || schedule.WorkShift == WorkShift.ContinuousDay)
                && WillGoToLunch(citizenAge))
            {
                schedule.Schedule(ResidentState.Shopping, lunchBegin);
                return true;
            }

            return false;
        }

        /// <summary>Updates the citizen's work schedule by determining the returning from lunch time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        public void ScheduleReturnFromLunch(ref CitizenSchedule schedule)
        {
            if (schedule.WorkStatus == WorkStatus.Working)
            {
                schedule.Schedule(ResidentState.AtWork, lunchEnd);
            }
        }

        /// <summary>Updates the citizen's work schedule by determining the time for returning from work.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        public void ScheduleReturnFromWork(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            if (schedule.WorkStatus != WorkStatus.Working)
            {
                return;
            }

            float departureHour = schedule.WorkShiftEndHour + GetOvertime(citizenAge);
            schedule.Schedule(ResidentState.Unknown, timeInfo.Now.FutureHour(departureHour));
        }

        private WorkShift GetWorkShift(BuildingWorkTimeManager.WorkTime workTime)
        {
            if (workTime.HasContinuousWorkShift)
            {
                if (workTime.WorkShifts == 2)
                {
                    return randomizer.ShouldOccur(config.ContinuousNightShiftQuota) ? WorkShift.ContinuousNight : WorkShift.ContinuousDay;
                }
                else
                {
                    return WorkShift.ContinuousDay;
                }
            }
            else
            {
                switch (workTime.WorkShifts)
                {
                    case 1:
                        return WorkShift.First;

                    case 2:
                        return randomizer.ShouldOccur(config.SecondShiftQuota) ? WorkShift.Second : WorkShift.First;

                    case 3:
                        int random = randomizer.GetRandomValue(100u);
                        if (random < config.NightShiftQuota)
                        {
                            return WorkShift.Night;
                        }
                        else if (random < config.SecondShiftQuota + config.NightShiftQuota)
                        {
                            return WorkShift.Second;
                        }

                        return WorkShift.First;

                    default:
                        return WorkShift.Unemployed;
                }
            }
        }

        private float GetTravelTimeToWork(ref CitizenSchedule schedule, ushort buildingId)
        {
            float result = schedule.CurrentState == ResidentState.AtHome
                ? schedule.TravelTimeToWork
                : 0;

            if (result <= 0)
            {
                result = travelBehavior.GetEstimatedTravelTime(buildingId, schedule.WorkBuilding);
            }

            return result;
        }

        private bool WillGoToLunch(Citizen.AgeGroup citizenAge)
        {
            if (!config.IsLunchtimeEnabled)
            {
                return false;
            }

            switch (citizenAge)
            {
                case Citizen.AgeGroup.Child:
                case Citizen.AgeGroup.Teen:
                case Citizen.AgeGroup.Senior:
                    return false;
            }

            return randomizer.ShouldOccur(config.LunchQuota);
        }

        private float GetOvertime(Citizen.AgeGroup citizenAge)
        {
            switch (citizenAge)
            {
                case Citizen.AgeGroup.Young:
                case Citizen.AgeGroup.Adult:
                    return randomizer.ShouldOccur(config.OnTimeQuota)
                        ? 0
                        : config.MaxOvertime * randomizer.GetRandomValue(100u) / 100f;

                default:
                    return 0;
            }
        }

        public int GetWorkersInBuilding(ushort buildingId)
        {
            int count = 0;
            var buildingData = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            var instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Work) != 0)
                {
                    if(instance.m_units.m_buffer[num].m_citizen0 != 0 )
                    {
                        count++;
                    }
                    if (instance.m_units.m_buffer[num].m_citizen1 != 0)
                    {
                        count++;
                    }
                    if (instance.m_units.m_buffer[num].m_citizen2 != 0)
                    {
                        count++;
                    }
                    if (instance.m_units.m_buffer[num].m_citizen3 != 0)
                    {
                        count++;
                    }
                    if (instance.m_units.m_buffer[num].m_citizen4 != 0)
                    {
                        count++;
                    }
                }
                num = instance.m_units.m_buffer[num].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            return count;
        }
    }
}

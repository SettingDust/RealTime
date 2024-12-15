// WorkBehavior.cs

namespace RealTime.CustomAI
{
    using System;
    using RealTime.Config;
    using RealTime.Events;
    using RealTime.GameConnection;
    using RealTime.Simulation;
    using SkyTools.Tools;
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
        private readonly IRealTimeEventManager eventManager;

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
            ITravelBehavior travelBehavior,
            IRealTimeEventManager eventManager)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
            this.buildingManager = buildingManager ?? throw new ArgumentNullException(nameof(buildingManager));
            this.timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
            this.travelBehavior = travelBehavior ?? throw new ArgumentNullException(nameof(travelBehavior));
            this.eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
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
        public void UpdateWorkShift(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, WorkShift chosenWorkShift)
        {
            if (schedule.WorkBuilding == 0 || citizenAge == Citizen.AgeGroup.Senior)
            {
                schedule.UpdateWorkShift(WorkShift.Unemployed, 0, 0, worksOnWeekends: false);
                return;
            }

            BuildingWorkTimeManager.WorkTime workTime;

            var workBuilding = BuildingManager.instance.m_buildings.m_buffer[schedule.WorkBuilding];

            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(schedule.WorkBuilding))
            {
                if(!BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(schedule.WorkBuilding))
                {
                    return;
                }
                workTime = BuildingWorkTimeManager.CreateBuildingWorkTime(schedule.WorkBuilding, workBuilding.Info);
            }
            else
            {
                workTime = BuildingWorkTimeManager.GetBuildingWorkTime(schedule.WorkBuilding);
            }


            float workBegin, workEnd;
            var workShift = chosenWorkShift;

            switch (citizenAge)
            {
                case Citizen.AgeGroup.Young:
                case Citizen.AgeGroup.Adult:
                    if (workShift == WorkShift.Unemployed)
                    {
                        // if the building has an upcoming event, assign new workers to the event
                        var buildingEvent = eventManager.GetCityEvent(schedule.WorkBuilding);
                        workShift = buildingEvent != null && timeInfo.Now.TimeOfDay.TotalHours < buildingEvent.StartTime.TimeOfDay.TotalHours - 1 ? WorkShift.Event : GetWorkShift(workTime);
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
                    if (service == ItemClass.Service.Education || service == ItemClass.Service.PlayerEducation) // teachers or lectures
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
                    if (service == ItemClass.Service.Education || service == ItemClass.Service.PlayerEducation) // night class at university (lectures)
                    {
                        workBegin = config.SchoolEnd;
                        workEnd = 22f;
                    }
                    else
                    {
                        workBegin = config.WorkEnd;
                        workEnd = config.GoToSleepHour;
                    }
                    break;

                case WorkShift.Night:
                    workBegin = config.GoToSleepHour;
                    workEnd = config.WorkBegin;
                    break;

                case WorkShift.ContinuousDay:
                    workBegin = 8f;
                    workEnd = 20f;
                    break;

                case WorkShift.ContinuousNight:
                    workBegin = 20f;
                    workEnd = 8f;
                    break;

                case WorkShift.Event:
                    var buildingEvent = eventManager.GetCityEvent(schedule.WorkBuilding);
                    workBegin = (float)(buildingEvent.StartTime.TimeOfDay.TotalHours - 0.5);
                    workEnd = (float)(buildingEvent.EndTime.TimeOfDay.TotalHours + 0.5);
                    break;
            }

            schedule.UpdateWorkShift(workShift, workBegin, workEnd, workTime.WorkAtWeekands);
        }

        /// <summary>Check if the citizen should go to work</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <returns><c>true</c> if the citizen should go to work; otherwise, <c>false</c>.</returns>
        public bool ShouldScheduleGoToWork(ref CitizenSchedule schedule)
        {
            if (schedule.CurrentState == ResidentState.AtWork)
            {
                return false;
            }

            var now = timeInfo.Now;
            if (config.IsWeekendEnabled && now.IsWeekend() && !schedule.WorksOnWeekends)
            {
                return false;
            }

            float halfShiftLength = (schedule.WorkShiftEndHour - schedule.WorkShiftStartHour) / 2;

            return now.TimeOfDay.TotalHours + halfShiftLength < schedule.WorkShiftEndHour;
        }


        /// <summary>Updates the citizen's work schedule by determining the time for going to work.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="currentBuilding">The ID of the building where the citizen is currently located.</param>
        /// <param name="simulationCycle">The duration (in hours) of a full citizens simulation cycle.</param>
        /// <returns>The time when going to work</returns>
        public DateTime ScheduleGoToWorkTime(ref CitizenSchedule schedule, ushort currentBuilding, float simulationCycle)
        {
            var now = timeInfo.Now;

            float travelTime = GetTravelTimeToWork(ref schedule, currentBuilding);

            var workEndTime = now.FutureHour(schedule.WorkShiftEndHour);  
            var departureTime = now.FutureHour(schedule.WorkShiftStartHour - travelTime - simulationCycle);

            Log.Debug(LogCategory.Schedule, $"  - works shift start hour is {schedule.WorkShiftStartHour}, works shift end hour is {schedule.WorkShiftEndHour}");
            Log.Debug(LogCategory.Schedule, $"  - travel time is {travelTime}, workEndTime is {workEndTime}, simulationCycle is {simulationCycle}, departureTime is {departureTime}");

            if (departureTime > workEndTime && now.AddHours(travelTime + simulationCycle) < workEndTime)
            {
                departureTime = now;
            }

            Log.Debug(LogCategory.Schedule, $"  - new departureTime is {departureTime}");

            return departureTime;
        }

        /// <summary>Updates the citizen's before work schedule by checking if he will or will not eat breakfast.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The citizen's age.</param>
        /// <returns><c>true</c> if a breakfast was scheduled; otherwise, <c>false</c>.</returns>
        public bool ScheduleBreakfast(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            float minGoToBreakfastHour = config.WakeUpHour;
            float maxGoToBreakfastHour = schedule.WorkShiftStartHour;

            Log.Debug(LogCategory.Schedule, $"  - Work status is {schedule.WorkStatus}, working in shift {schedule.WorkShift}");
            if (schedule.WorkStatus == WorkStatus.None
                && (schedule.WorkShift == WorkShift.First || schedule.WorkShift == WorkShift.ContinuousDay)
                && timeInfo.CurrentHour >= minGoToBreakfastHour && timeInfo.CurrentHour <= maxGoToBreakfastHour
                && WillGoToBreakfast(citizenAge))
            {
                schedule.Schedule(ResidentState.GoToBreakfast);
                return true;
            }

            return false;
        }

        /// <summary>Updates the citizen's work schedule by determining the lunch time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The citizen's age.</param>
        /// <returns><c>true</c> if a lunch time was scheduled; otherwise, <c>false</c>.</returns>
        public bool ScheduleLunch(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            int hours = (int)(lunchBegin - timeInfo.Now).TotalHours;

            if (hours >= 2 && schedule.WorkStatus == WorkStatus.Working
                && (schedule.WorkShift == WorkShift.First || schedule.WorkShift == WorkShift.ContinuousDay)
                && WillGoToLunch(citizenAge))
            {
                schedule.Schedule(ResidentState.GoToLunch, lunchBegin);
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
                schedule.Schedule(ResidentState.GoToWork, lunchEnd);
            }
        }

        /// <summary>Updates the citizen's work schedule by determining the time for returning from work.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        public void ScheduleReturnFromWork(uint citizenId, ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            if (schedule.WorkStatus != WorkStatus.Working)
            {
                return;
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} end work hour is {schedule.WorkShiftEndHour} and current hour is {timeInfo.CurrentHour}");
            
            float time = 0;
            if (timeInfo.CurrentHour - schedule.WorkShiftEndHour > 0)
            {
                time = timeInfo.CurrentHour - (schedule.WorkShiftEndHour + GetOvertime(citizenAge));
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} time is {time}");

            float departureHour = schedule.WorkShiftEndHour + GetOvertime(citizenAge) + time + 0.1f;

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} departureHour is {departureHour} and future hour is {timeInfo.Now.FutureHour(departureHour):dd.MM.yy HH:mm}");

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

        private bool WillGoToBreakfast(Citizen.AgeGroup citizenAge)
        {
            Log.Debug(LogCategory.Schedule, $"  - citizen age is {citizenAge}, BreakfastQuota is {config.BreakfastQuota}");
            if (!config.IsBreakfastTimeEnabled)
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

            return randomizer.ShouldOccur(config.BreakfastQuota);
        }

        private bool WillGoToLunch(Citizen.AgeGroup citizenAge)
        {
            if (!config.IsLunchTimeEnabled)
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

    }
}

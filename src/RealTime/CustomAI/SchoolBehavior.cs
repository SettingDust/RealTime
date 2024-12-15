// SchoolBehavior.cs

namespace RealTime.CustomAI
{
    using System;
    using ColossalFramework;
    using RealTime.Config;
    using RealTime.Simulation;
    using SkyTools.Tools;

    /// <summary>
    /// A class containing methods for managing the citizens' work behavior.
    /// </summary>
    internal sealed class SchoolBehavior : ISchoolBehavior
    {
        private readonly RealTimeConfig config;
        private readonly IRandomizer randomizer;
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
        public SchoolBehavior(
            RealTimeConfig config,
            IRandomizer randomizer,
            ITimeInfo timeInfo,
            ITravelBehavior travelBehavior)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
            this.timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
            this.travelBehavior = travelBehavior ?? throw new ArgumentNullException(nameof(travelBehavior));
        }

        public void BeginNewDay()
        {
            var today = timeInfo.Now.Date;
            lunchBegin = today.AddHours(config.LunchBegin);
            lunchEnd = today.AddHours(config.LunchEnd);
        }

        /// <summary>Updates the citizen's school class parameters in the specified citizen's <paramref name="schedule"/>.</summary>
        /// <param name="schedule">The citizen's schedule to update the work shift in.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        public void UpdateSchoolClass(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            if (schedule.SchoolBuilding == 0 || citizenAge == Citizen.AgeGroup.Senior)
            {
                schedule.UpdateSchoolClass(SchoolClass.NoSchool, 0, 0);
                return;
            }

            var schoolBuilding = BuildingManager.instance.m_buildings.m_buffer[schedule.SchoolBuilding];

            var level = schoolBuilding.Info.m_class.m_level;

            float schoolBegin = config.SchoolBegin;
            float schoolEnd = config.SchoolEnd;

            SchoolClass schoolClass;

            switch (level)
            {
                case ItemClass.Level.Level1:
                case ItemClass.Level.Level2:
                    schoolClass = SchoolClass.DayClass;
                    break;

                case ItemClass.Level.Level3:
                    schoolClass = randomizer.ShouldOccur(config.NightClassQuota) ? SchoolClass.NightClass : SchoolClass.DayClass;
                    break;

                default:
                    return;
            }

            switch (schoolClass)
            {
                case SchoolClass.DayClass:
                    break;

                case SchoolClass.NightClass:
                    schoolBegin = config.SchoolEnd;
                    schoolEnd = 20;
                    break;
            }

            schedule.UpdateSchoolClass(schoolClass, schoolBegin, schoolEnd);
        }

        /// <summary>Check if the citizen should go to school</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <returns><c>true</c> if the citizen should go to school; otherwise, <c>false</c>.</returns>
        public bool ShouldScheduleGoToSchool(ref CitizenSchedule schedule)
        {
            if (schedule.CurrentState == ResidentState.AtSchool)
            {
                return false;
            }

            var now = timeInfo.Now;
            if (config.IsWeekendEnabled && now.IsWeekend())
            {
                return false;
            }

            return true;
        }

        /// <summary>Updates the citizen's school schedule by determining the time for going to school.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="currentBuilding">The ID of the building where the citizen is currently located.</param>
        /// <param name="simulationCycle">The duration (in hours) of a full citizens simulation cycle.</param>
        /// <returns>The time when going to school</returns>
        public DateTime ScheduleGoToSchoolTime(ref CitizenSchedule schedule, ushort currentBuilding, float simulationCycle)
        {
            var now = timeInfo.Now;

            float travelTime = GetTravelTimeToSchool(ref schedule, currentBuilding);

            var schoolEndTime = now.FutureHour(schedule.SchoolClassEndHour);
            var departureTime = now.FutureHour(schedule.SchoolClassStartHour - travelTime - simulationCycle);

            Log.Debug(LogCategory.Schedule, $"  - school start hour is {schedule.SchoolClassStartHour}, school end hour is {schedule.SchoolClassEndHour}");
            Log.Debug(LogCategory.Schedule, $"  - travel time is {travelTime}, schoolEndTime is {schoolEndTime}, simulationCycle is {simulationCycle}, departureTime is {departureTime}");

            if (departureTime > schoolEndTime && now.AddHours(travelTime + simulationCycle) < schoolEndTime)
            {
                departureTime = now;
            }

            Log.Debug(LogCategory.Schedule, $"  - new departureTime is {departureTime}");

            return departureTime;
        }

        /// <summary>Updates the citizen's school schedule by determining the lunch time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="schoolBuilding">The citizen's school building.</param>
        /// <returns><c>true</c> if a lunch time was scheduled; otherwise, <c>false</c>.</returns>
        public bool ScheduleLunch(ref CitizenSchedule schedule, ushort schoolBuilding)
        {
            int hours = (int)(lunchBegin - timeInfo.Now).TotalHours;

            if (hours >= 2 && schedule.SchoolStatus == SchoolStatus.Studying
                && schedule.SchoolClass == SchoolClass.DayClass
                && WillGoToLunch(schoolBuilding))
            {
                schedule.Schedule(ResidentState.GoToLunch, lunchBegin);
                return true;
            }

            return false;
        }

        /// <summary>Updates the citizen's school schedule by determining the returning from lunch time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        public void ScheduleReturnFromLunch(ref CitizenSchedule schedule)
        {
            if (schedule.SchoolStatus == SchoolStatus.Studying)
            {
                schedule.Schedule(ResidentState.GoToSchool, lunchEnd);
            }
        }

        /// <summary>Updates the citizen's school schedule by determining the time for returning from school.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        public void ScheduleReturnFromSchool(uint citizenId, ref CitizenSchedule schedule)
        {
            if (schedule.SchoolStatus != SchoolStatus.Studying)
            {
                return;
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} end school hour is {schedule.SchoolClassEndHour} and current hour is {timeInfo.CurrentHour}");

            float time = 0;
            if (timeInfo.CurrentHour - schedule.SchoolClassEndHour > 0)
            {
                time = timeInfo.CurrentHour - schedule.WorkShiftEndHour;
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} time is {time}");

            float departureHour = schedule.SchoolClassEndHour + time + 0.1f;

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} departureHour is {departureHour} and future hour is {timeInfo.Now.FutureHour(departureHour):dd.MM.yy HH:mm}");

            schedule.Schedule(ResidentState.Unknown, timeInfo.Now.FutureHour(departureHour));
        }

        private float GetTravelTimeToSchool(ref CitizenSchedule schedule, ushort buildingId)
        {
            float result = schedule.CurrentState == ResidentState.AtHome ? schedule.TravelTimeToWork : 0;

            if (result <= 0)
            {
                result = travelBehavior.GetEstimatedTravelTime(buildingId, schedule.SchoolBuilding);
            }

            return result;
        }

        private bool WillGoToLunch(ushort schoolBuildingId)
        {
            if (!config.IsLunchTimeEnabled)
            {
                return false;
            }

            var schoolBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[schoolBuildingId];
            if(schoolBuilding.Info.GetAI() is not CampusBuildingAI)
            {
                return false;
            }

            return randomizer.ShouldOccur(config.LunchQuota);
        }

    }
}

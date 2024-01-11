// <copyright file="WorkBehavior.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.CustomAI
{
    using System;
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

        /// <summary>Updates the citizen's school class parameters in the specified citizen's <paramref name="schedule"/>.</summary>
        /// <param name="schedule">The citizen's schedule to update the work shift in.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        public void UpdateSchoolClass(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            if (schedule.SchoolBuilding == 0 || citizenAge == Citizen.AgeGroup.Senior)
            {
                schedule.UpdateSchoolClass(SchoolClass.NoClass, 0, 0);
                return;
            }

            float schoolBegin, schoolEnd;

            SchoolClass schoolClass;

            switch (citizenAge)
            {
                case Citizen.AgeGroup.Child:
                case Citizen.AgeGroup.Teen:
                    schoolClass = SchoolClass.DayClass;
                    schoolBegin = config.SchoolBegin;
                    schoolEnd = config.SchoolEnd;
                    break;

                case Citizen.AgeGroup.Young:
                case Citizen.AgeGroup.Adult:
                    schoolClass = randomizer.ShouldOccur(config.NightClassQuota) ? SchoolClass.DayClass : SchoolClass.NightClass;
                    schoolBegin = config.SchoolBegin;
                    schoolEnd = config.SchoolEnd;
                    break;

                default:
                    return;
            }

            switch (schoolClass)
            {
                case SchoolClass.DayClass:
                    schoolBegin = config.SchoolBegin;
                    schoolEnd = config.SchoolEnd; 
                    break;

                case SchoolClass.NightClass:
                    schoolBegin = config.SchoolEnd;
                    schoolEnd = 20;
                    break;
            }

            schedule.UpdateSchoolClass(schoolClass, schoolBegin, schoolEnd);
        }

        /// <summary>Updates the citizen's school schedule by determining the time for going to school.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="currentBuilding">The ID of the building where the citizen is currently located.</param>
        /// <param name="simulationCycle">The duration (in hours) of a full citizens simulation cycle.</param>
        /// <returns><c>true</c> if school was scheduled; otherwise, <c>false</c>.</returns>
        public bool ScheduleGoToSchool(ref CitizenSchedule schedule, ushort currentBuilding, float simulationCycle)
        {
            if (schedule.CurrentState == ResidentState.AtSchoolOrWork || schedule.CurrentState == ResidentState.AtSchool)
            {
                return false;
            }

            var now = timeInfo.Now;
            if (config.IsWeekendEnabled && now.IsWeekend())
            {
                return false;
            }

            float travelTime = GetTravelTimeToSchool(ref schedule, currentBuilding);

            var schoolEndTime = now.FutureHour(schedule.SchoolClassEndHour);
            var departureTime = now.FutureHour(schedule.SchoolClassStartHour - travelTime - simulationCycle);
            if (departureTime > schoolEndTime && now.AddHours(travelTime + simulationCycle) < schoolEndTime)
            {
                departureTime = now;
            }

            schedule.Schedule(ResidentState.AtSchool, departureTime);
            return true;
        }

        /// <summary>Updates the citizen's school schedule by determining the time for returning from school.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        public void ScheduleReturnFromSchool(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            if (schedule.SchoolStatus != SchoolStatus.Studying)
            {
                return;
            }

            float departureHour = schedule.SchoolClassEndHour;
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

    }
}

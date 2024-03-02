// ISchoolBehavior.cs

namespace RealTime.CustomAI
{
    /// <summary>
    /// An interface for the citizens work behavior.
    /// </summary>
    internal interface ISchoolBehavior
    {
        /// <summary>Updates the citizen's school schedule by determining the time for going to school.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="currentBuilding">The ID of the building where the citizen is currently located.</param>
        /// <param name="simulationCycle">The duration (in hours) of a full citizens simulation cycle.</param>
        /// <returns><c>true</c> if school was scheduled; otherwise, <c>false</c>.</returns>
        bool ScheduleGoToSchool(ref CitizenSchedule schedule, ushort currentBuilding, float simulationCycle);

        /// <summary>Updates the citizen's school schedule by determining the time for returning from school.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        void ScheduleReturnFromSchool(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge);

        /// <summary>Updates the citizen's school class parameters in the specified citizen's <paramref name="schedule"/>.</summary>
        /// <param name="schedule">The citizen's schedule to update the school class time in.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        void UpdateSchoolClass(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge);
    }
}

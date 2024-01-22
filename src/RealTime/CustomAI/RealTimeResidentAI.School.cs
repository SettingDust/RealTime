// <copyright file="RealTimeResidentAI.SchoolWork.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.CustomAI
{
    using SkyTools.Tools;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        private bool ScheduleSchool(ref CitizenSchedule schedule, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            if (!schoolBehavior.ScheduleGoToSchool(ref schedule, currentBuilding, simulationCycle))
            {
                return false;
            }

            Log.Debug(LogCategory.Schedule, $"  - Schedule school at {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");

            float timeLeft = (float)(schedule.ScheduledStateTime - TimeInfo.Now).TotalHours;
            if (timeLeft <= PrepareToSchoolHours)
            {
                // Just sit at home if the school time will come soon
                Log.Debug(LogCategory.Schedule, $"  - School time in {timeLeft} hours, preparing for departure");
                return true;
            }

            if (timeLeft <= MaxTravelTime)
            {
                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    Log.Debug(LogCategory.Schedule, $"  - School time in {timeLeft} hours, returning home");
                    schedule.Schedule(ResidentState.AtHome);
                    return true;
                }

                var age = CitizenProxy.GetAge(ref citizen);
                if(age == Citizen.AgeGroup.Young || age == Citizen.AgeGroup.Adult)
                {
                    // If we have some time, try to shop locally.
                    if (ScheduleShopping(ref schedule, ref citizen, localOnly: true))
                    {
                        Log.Debug(LogCategory.Schedule, $"  - University time in {timeLeft} hours, trying local shop");
                    }
                    else
                    {
                        Log.Debug(LogCategory.Schedule, $"  - University time in {timeLeft} hours, doing nothing");
                    }
                }
                return true;
            }

            return false;
        }

        private void DoScheduledSchool(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            schedule.SchoolStatus = SchoolStatus.Studying;

            if (currentBuilding == schedule.SchoolBuilding && schedule.CurrentState != ResidentState.AtSchoolOrWork && schedule.CurrentState != ResidentState.AtSchool)
            {
                CitizenProxy.SetVisitPlace(ref citizen, citizenId, 0);
                CitizenProxy.SetLocation(ref citizen, Citizen.Location.Work);
                return;
            }

#if DEBUG
            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);
#else
            const string citizenDesc = null;
#endif

            if (residentAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, schedule.SchoolBuilding))
            {
                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    // The start moving method will register a departure from any building to school,
                    // but we are only interested in the 'home->school' route.
                    schedule.DepartureTime = default;
                }

                var citizenAge = CitizenProxy.GetAge(ref citizen);
                schoolBehavior.ScheduleReturnFromSchool(ref schedule, citizenAge);
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to school {schedule.SchoolBuilding} and will leave school at {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
            }
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to go to school from {currentBuilding} but can't, will try once again next time");
                schedule.Schedule(ResidentState.Unknown);
            }
        }

    }
}

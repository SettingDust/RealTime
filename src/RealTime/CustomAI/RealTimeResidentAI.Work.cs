// <copyright file="RealTimeResidentAI.SchoolWork.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.CustomAI
{
    using SkyTools.Tools;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        private bool ScheduleWork(ref CitizenSchedule schedule, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            if (!workBehavior.ScheduleGoToWork(ref schedule, currentBuilding, simulationCycle))
            {
                return false;
            }

            Log.Debug(LogCategory.Schedule, $"  - Schedule work at {schedule.ScheduledStateTime}");

            float timeLeft = (float)(schedule.ScheduledStateTime - TimeInfo.Now).TotalHours;
            if (timeLeft <= PrepareToWorkHours)
            {
                // Just sit at home if the work time will come soon
                Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, preparing for departure");
                return true;
            }

            if (timeLeft <= MaxTravelTime)
            {
                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, returning home");
                    schedule.Schedule(ResidentState.AtHome);
                    return true;
                }

                // If we have some time, try to shop locally.
                if (ScheduleShopping(ref schedule, ref citizen, localOnly: true))
                {
                    Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, trying local shop");
                }
                else
                {
                    Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, doing nothing");
                }

                return true;
            }

            return false;
        }

        private void DoScheduledWork(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            schedule.WorkStatus = WorkStatus.Working;

            if (currentBuilding == schedule.WorkBuilding && schedule.CurrentState != ResidentState.AtSchoolOrWork && schedule.CurrentState != ResidentState.AtWork)
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

            if (residentAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, schedule.WorkBuilding))
            {
                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    // The start moving method will register a departure from any building to work,
                    // but we are only interested in the 'home->work' route.
                    schedule.DepartureTime = default;
                }

                var citizenAge = CitizenProxy.GetAge(ref citizen);
                if (workBehavior.ScheduleLunch(ref schedule, citizenAge))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to work {schedule.WorkBuilding} and will go to lunch at {schedule.ScheduledStateTime}");
                }
                else
                {
                    workBehavior.ScheduleReturnFromWork(ref schedule, citizenAge);
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to work {schedule.WorkBuilding} and will leave work at {schedule.ScheduledStateTime}");
                }
            }
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to go to work from {currentBuilding} but can't, will try once again next time");
                schedule.Schedule(ResidentState.Unknown);
            }
        }

        private void DoScheduledLunch(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
#if DEBUG
            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);
#endif
            // no one it worked besides me
            if(workBehavior.GetWorkersInBuilding(currentBuilding) <= 1)
            {
#if DEBUG
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go for lunch from {currentBuilding}, but there is no one at work to cover his shift");
#endif
                workBehavior.ScheduleReturnFromWork(ref schedule, CitizenProxy.GetAge(ref citizen));
            }
            else
            {
                ushort lunchPlace = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance);
                if (lunchPlace != 0)
                {
#if DEBUG
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going for lunch from {currentBuilding} to {lunchPlace}");
#endif
                    workBehavior.ScheduleReturnFromLunch(ref schedule);
                }
                else
                {
#if DEBUG
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go for lunch from {currentBuilding}, but there were no buildings close enough");
#endif
                    workBehavior.ScheduleReturnFromWork(ref schedule, CitizenProxy.GetAge(ref citizen));
                }
            }
        }

    }
}

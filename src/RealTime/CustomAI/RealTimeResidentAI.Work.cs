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
            if(buildingAI.GetWorkersInBuilding(currentBuilding) <= 1)
            {
#if DEBUG
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go for lunch from {currentBuilding}, but there is no one at work to cover his shift");
#endif
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
                }
            }
        }

        private bool ProcessCitizenWork(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            return RescheduleReturnFromWork(ref schedule, citizenId, ref citizen, currentBuilding);
        }

        private bool RescheduleReturnFromWork(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, ushort currentBuilding)
        {
            if (ShouldReturnFromWork(ref schedule, citizenId, ref citizen, currentBuilding))
            {
                workBehavior.ScheduleReturnFromWork(ref schedule, CitizenProxy.GetAge(ref citizen));
            }

            return false;
        }

        public bool IsEssentialService(ushort buildingId)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            var service = building.Info.m_class.m_service;
            switch (service)
            {
                case ItemClass.Service.Electricity:
                case ItemClass.Service.Water:
                case ItemClass.Service.HealthCare:
                case ItemClass.Service.PoliceDepartment:
                case ItemClass.Service.FireDepartment:
                case ItemClass.Service.PublicTransport:
                case ItemClass.Service.Disaster:
                case ItemClass.Service.Natural:
                case ItemClass.Service.Garbage:
                case ItemClass.Service.Road:
                case ItemClass.Service.Hotel:
                case ItemClass.Service.ServicePoint:
                    return true;

                default:
                    return false;
            }
        }

        private bool ShouldReturnFromWork(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, ushort currentBuilding)
        {
            // work place data
            var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(currentBuilding);

            // building that are required for city operations - must wait for the next shift to arrive
            if (!IsEssentialService(currentBuilding))
            {
                return true;
            }

            // find the next work shift of the work place
            WorkShift workShiftToFind;

            switch (schedule.WorkShift)
            {
                case WorkShift.First when workTime.WorkShifts == 2 && !workTime.HasContinuousWorkShift:
                    workShiftToFind = WorkShift.Second;
                    break;

                case WorkShift.Second when workTime.WorkShifts == 3:
                    workShiftToFind = WorkShift.Night;
                    break;

                case WorkShift.Night:
                    workShiftToFind = WorkShift.First;
                    break;

                case WorkShift.ContinuousDay when workTime.WorkShifts == 2 && workTime.HasContinuousWorkShift:
                    workShiftToFind = WorkShift.ContinuousNight;
                    break;

                case WorkShift.ContinuousNight:
                    workShiftToFind = WorkShift.ContinuousDay;
                    break;

                default:
                    return true;
            }

            
            // get the building work force 
            uint[] workforce = buildingAI.GetBuildingWorkForce(currentBuilding);

            for (int i = 0; i < workforce.Length; i++)
            {
                // check if all people from the next shift that are not on vacation has arrived
                var citizen_schedule = GetCitizenSchedule(workforce[i]);
                if(citizen_schedule.WorkShift == workShiftToFind && citizen_schedule.WorkStatus != WorkStatus.OnVacation)
                {
                    if(CitizenProxy.GetLocation(ref citizen) != Citizen.Location.Work)
                    {
                        // do not leave work until next shift has arrived
                        return false;
                    }
                }
            }

            return true;
        }

    }
}

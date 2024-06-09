namespace RealTime.Events
{
    using ColossalFramework;
    using RealTime.Events.Storage;
    using RealTime.Simulation;
    using SkyTools.Tools;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    internal class XmlEvent : CityEventBase
    {
        protected int m_capacity = 0;
        protected double m_eventLength = 0;
        protected string m_userEventName = "";
        protected CityEventAttendees m_eventChances = null;
        protected CityEventCosts m_eventCosts = null;
        protected CityEventIncentive[] m_eventIncentives = null;

        public XmlEvent(CityEventTemplate xmlContainer)
        {
            m_eventData.m_eventName = "XMLEvent-" + xmlContainer.EventName;
            m_eventData.m_canBeWatchedOnTV = xmlContainer.CanBeWatchedOnTV;
            m_eventData.m_entryCost = xmlContainer.Costs._entry;

            m_capacity = xmlContainer.Capacity;
            m_eventLength = xmlContainer.Duration;
            m_eventChances = xmlContainer.Attendees;
            m_eventCosts = xmlContainer.Costs;
            m_eventIncentives = xmlContainer.Incentives;
            m_userEventName = xmlContainer.UserEventName;

            SetUpIncentives(m_eventIncentives);
        }

        protected void SetUpIncentives(CityEventIncentive[] incentives)
        {
            if (incentives != null)
            {
                m_eventData.m_incentives = new CityEventDataIncentives[incentives.Length];

                for (int index = 0; index < incentives.Length; ++index)
                {
                    CityEventXmlIncentive incentive = incentives[index];
                    CityEventDataIncentives dataIncentive = new CityEventDataIncentives()
                    {
                        itemCount = 0,
                        name = incentive._name,
                        returnCost = incentive._returnCost
                    };

                    m_eventData.m_incentives[index] = dataIncentive;
                }
            }
        }

        public string GetReadableName() => m_userEventName;

        public CityEventCosts GetCosts() => m_eventCosts;

        public List<CityEventIncentive> GetIncentives() => m_eventIncentives.ToList();

        public void SetEntryCost(int cost) => m_eventData.m_entryCost = cost;

        public override bool TryAcceptAttendee(Citizen.AgeGroup age, Citizen.Gender gender, Citizen.Education education, Citizen.Wealth wealth, Citizen.Wellbeing wellbeing, Citizen.Happiness happiness, IRandomizer randomizer)
        {
            bool canGo = false;

            if (m_eventChances != null && m_eventData.m_registeredCitizens < GetCapacity())
            {
                canGo = true;

                var _citizenWealth = wealth;
                var _citizenEducation = education;
                var _citizenGender = gender;
                var _citizenHappiness = happiness;
                var _citizenWellbeing = wellbeing;
                var _citizenAgeGroup = age;

                float percentageChange = GetAdjustedChancePercentage();
                int randomPercentage = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U);

                switch (_citizenWealth)
                {
                    case Citizen.Wealth.Low:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.LowWealth, percentageChange);
                        break;
                    case Citizen.Wealth.Medium:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.MediumWealth, percentageChange);
                        break;
                    case Citizen.Wealth.High:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.HighWealth, percentageChange);
                        break;
                }

                randomPercentage = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U);

                switch (_citizenEducation)
                {
                    case Citizen.Education.Uneducated:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.Uneducated, percentageChange);
                        break;
                    case Citizen.Education.OneSchool:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.OneSchool, percentageChange);
                        break;
                    case Citizen.Education.TwoSchools:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.TwoSchools, percentageChange);
                        break;
                    case Citizen.Education.ThreeSchools:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.ThreeSchools, percentageChange);
                        break;
                }

                randomPercentage = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U);

                switch (_citizenGender)
                {
                    case Citizen.Gender.Female:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.Females, percentageChange);
                        break;
                    case Citizen.Gender.Male:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.Males, percentageChange);
                        break;
                }

                randomPercentage = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U);

                switch (_citizenHappiness)
                {
                    case Citizen.Happiness.Bad:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.BadHappiness, percentageChange);
                        break;
                    case Citizen.Happiness.Poor:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.PoorHappiness, percentageChange);
                        break;
                    case Citizen.Happiness.Good:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.GoodHappiness, percentageChange);
                        break;
                    case Citizen.Happiness.Excellent:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.ExcellentHappiness, percentageChange);
                        break;
                    case Citizen.Happiness.Suberb:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.SuperbHappiness, percentageChange);
                        break;
                }

                randomPercentage = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U);

                switch (_citizenWellbeing)
                {
                    case Citizen.Wellbeing.VeryUnhappy:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.VeryUnhappyWellbeing, percentageChange);
                        break;
                    case Citizen.Wellbeing.Unhappy:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.UnhappyWellbeing, percentageChange);
                        break;
                    case Citizen.Wellbeing.Satisfied:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.SatisfiedWellbeing, percentageChange);
                        break;
                    case Citizen.Wellbeing.Happy:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.HappyWellbeing, percentageChange);
                        break;
                    case Citizen.Wellbeing.VeryHappy:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.VeryHappyWellbeing, percentageChange);
                        break;
                }

                randomPercentage = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U);

                switch (_citizenAgeGroup)
                {
                    case Citizen.AgeGroup.Child:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.Children, percentageChange);
                        break;
                    case Citizen.AgeGroup.Teen:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.Teens, percentageChange);
                        break;
                    case Citizen.AgeGroup.Young:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.YoungAdults, percentageChange);
                        break;
                    case Citizen.AgeGroup.Adult:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.Adults, percentageChange);
                        break;
                    case Citizen.AgeGroup.Senior:
                        canGo = canGo && randomPercentage < Adjust(m_eventChances.Seniors, percentageChange);
                        break;
                }

                Debug.Log(
                    (canGo ? "[Can Go]" : "[Ignoring]") +
                    " Citizen " + citizenID + " for " + m_eventData.m_eventName + "\n\t" +
                    _citizenWealth.ToString() + ", " + _citizenEducation.ToString() + ", " + _citizenGender.ToString() + ", " +
                    _citizenHappiness.ToString() + ", " + _citizenWellbeing.ToString() + ", " + _citizenAgeGroup.ToString());
            }

            return canGo;
        }

        protected float GetAdjustedChancePercentage()
        {
            float additionalAmount = 0;

            if (m_eventData.m_incentives != null && m_eventIncentives != null)
            {
                var xmlIncentives = m_eventIncentives.ToList();

                foreach (var dataIncentive in m_eventData.m_incentives)
                {
                    var foundIncentive = xmlIncentives.Find(incentive => incentive._name == dataIncentive.name);

                    if (foundIncentive != null)
                    {
                        if (dataIncentive.boughtItems < dataIncentive.itemCount || !m_eventData.m_userEvent && foundIncentive._activeWhenRandomEvent)
                        {
                            additionalAmount += foundIncentive._positiveEffect;
                        }
                        else
                        {
                            additionalAmount -= foundIncentive._negativeEffect;
                        }
                    }
                }
            }

            Log.Info("Adjusting percentage for event. Adjusting by " + additionalAmount.ToString());

            return additionalAmount;
        }

        protected int Adjust(int value, float percentage)
        {
            float decimalValue = value;
            return Mathf.RoundToInt(decimalValue + (decimalValue == 0f || percentage == 0f ? 0 : ((decimalValue * percentage) / 100f)));
        }

        public override float GetCost()
        {
            float finalCost = 0f;

            if (m_eventData != null && m_eventData.m_userEvent)
            {
                finalCost += m_eventCosts._creation;
                finalCost += m_eventCosts._perHead * m_eventData.m_userTickets;

                if (m_eventData.m_incentives != null && m_eventIncentives != null)
                {
                    var incentiveList = m_eventIncentives.ToList();

                    foreach (CityEventDataIncentives incentive in m_eventData.m_incentives)
                    {
                        var foundIncentive = incentiveList.Find(thisIncentive => thisIncentive._name == incentive.name);

                        if (foundIncentive != null)
                        {
                            finalCost += incentive.itemCount * foundIncentive._cost;
                        }
                        else
                        {
                            Log.Error("Failed to match event data incentive to XML data incentive.");
                        }
                    }
                }
                else
                {
                    Log.Error("Tried to get the cost of an event that has no incentives!");
                }
            }
            else
            {
                Log.Error("Tried to get the cost of an event that has no data!");
            }

            return finalCost;
        }

        public override float GetExpectedReturn()
        {
            float expectedReturn = 0f;

            if (m_eventData != null && m_eventData.m_userEvent)
            {
                expectedReturn += m_eventData.m_entryCost * m_eventData.m_userTickets;

                if (m_eventData.m_incentives != null && m_eventIncentives != null)
                {
                    var incentiveList = m_eventIncentives.ToList();

                    foreach (CityEventDataIncentives incentive in m_eventData.m_incentives)
                    {
                        expectedReturn += incentive.itemCount * incentive.returnCost;
                    }
                }
                else
                {
                    Log.Error("Tried to get the return cost of an event that has no incentives!");
                }
            }

            return expectedReturn;
        }

        public override int GetCapacity()
        {
            if (m_eventData.m_userEvent)
            {
                return Math.Min(m_eventData.m_userTickets, Math.Min(m_capacity, 9000));
            }
            else
            {
                return Math.Min(m_capacity, 9000);
            }
        }

        public override double GetEventLength() => m_eventLength;

        protected override bool CitizenRegistered(uint citizenID, ref Citizen person)
        {
            bool canAttend = true;
            float maxSpend = 0f;

            if (m_eventData.m_userEvent)
            {
                var simulationManager = Singleton<SimulationManager>.instance;
                Citizen.Wealth wealth = person.WealthLevel;

                switch (wealth)
                {
                    case Citizen.Wealth.Low:
                        maxSpend = 30f + simulationManager.m_randomizer.Int32(60);
                        break;
                    case Citizen.Wealth.Medium:
                        maxSpend = 80f + simulationManager.m_randomizer.Int32(80);
                        break;
                    case Citizen.Wealth.High:
                        maxSpend = 120f + simulationManager.m_randomizer.Int32(320);
                        break;
                }

                if (m_eventCosts != null)
                {
                    maxSpend -= m_eventCosts._entry;

                    canAttend = maxSpend > 0;

                    if (m_eventData.m_incentives != null && m_eventData.m_incentives.Length > 0)
                    {
                        int startFrom = simulationManager.m_randomizer.Int32(0, m_eventData.m_incentives.Length - 1);
                        int index = startFrom;
                        string buying = m_eventData.m_eventName + " ";

                        do
                        {
                            var incentive = m_eventData.m_incentives[index];

                            if (incentive.boughtItems < incentive.itemCount && maxSpend - incentive.returnCost >= 0)
                            {
                                maxSpend -= incentive.returnCost;
                                ++incentive.boughtItems;

                                buying += "[" + incentive.name + " (" + incentive.boughtItems + "/" + incentive.itemCount + ")] ";
                            }

                            if (++index >= m_eventData.m_incentives.Length)
                            {
                                index = 0;
                            }
                        } while (index != startFrom);

                        Log.Info(buying);
                    }
                }
            }

            if (!canAttend)
            {
                Log.Info("Cim is too poor to attend the event :(");
            }

            return canAttend;
        }


        protected void CheckAndAdd<T>(ref List<T> list, ref int highestChance, T itemToAdd, int value)
        {
            if (value > highestChance)
            {
                highestChance = value;
                list.Clear();
                list.Add(itemToAdd);
            }
            else if (value == highestChance)
            {
                list.Add(itemToAdd);
            }
        }

        public List<Citizen.AgeGroup> GetHighestPercentageAgeGroup()
        {
            var returnGroups = new List<Citizen.AgeGroup>();
            int highestChance = 0;

            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.AgeGroup.Adult, m_eventChances.Adults);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.AgeGroup.Child, m_eventChances.Children);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.AgeGroup.Senior, m_eventChances.Seniors);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.AgeGroup.Teen, m_eventChances.Teens);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.AgeGroup.Young, m_eventChances.YoungAdults);

            return returnGroups;
        }

        public List<Citizen.Wealth> GetHighestPercentageWealth()
        {
            var returnGroups = new List<Citizen.Wealth>();
            int highestChance = 0;

            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.Wealth.High, m_eventChances.HighWealth);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.Wealth.Medium, m_eventChances.MediumWealth);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.Wealth.Low, m_eventChances.LowWealth);

            return returnGroups;
        }

        public List<Citizen.Education> GetHighestPercentageEducation()
        {
            var returnGroups = new List<Citizen.Education>();
            int highestChance = 0;

            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.Education.Uneducated, m_eventChances.Uneducated);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.Education.OneSchool, m_eventChances.OneSchool);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.Education.TwoSchools, m_eventChances.TwoSchools);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.Education.ThreeSchools, m_eventChances.ThreeSchools);

            return returnGroups;
        }

        public List<Citizen.Gender> GetHighestPercentageGender()
        {
            var returnGroups = new List<Citizen.Gender>();
            int highestChance = 0;

            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.Gender.Female, m_eventChances.Females);
            CheckAndAdd(ref returnGroups, ref highestChance, Citizen.Gender.Male, m_eventChances.Males);

            return returnGroups;
        }
    }
}

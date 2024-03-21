// RealTimeBuildingAI.cs

namespace RealTime.CustomAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using RealTime.Config;
    using RealTime.GameConnection;
    using RealTime.Simulation;
    using SkyTools.Tools;
    using static Constants;

    /// <summary>
    /// A class that incorporates the custom logic for the private buildings.
    /// </summary>
    internal sealed class RealTimeBuildingAI : IRealTimeBuildingAI
    {
        private const int ConstructionSpeedPaused = 10880;
        private const int ConstructionSpeedMinimum = 1088;
        private const int StepMask = 0xFF;
        private const int BuildingStepSize = 192;
        private const int ConstructionRestrictionThreshold1 = 100;
        private const int ConstructionRestrictionThreshold2 = 1_000;
        private const int ConstructionRestrictionThreshold3 = 10_000;
        private const int ConstructionRestrictionStep1 = MaximumBuildingsInConstruction / 10;
        private const int ConstructionRestrictionStep2 = MaximumBuildingsInConstruction / 5;
        private const int ConstructionRestrictionScale2 = ConstructionRestrictionThreshold2 / (ConstructionRestrictionStep2 - ConstructionRestrictionStep1);
        private const int ConstructionRestrictionScale3 = ConstructionRestrictionThreshold3 / (MaximumBuildingsInConstruction - ConstructionRestrictionStep2);

        private static readonly string[] BannedEntertainmentBuildings = ["parking", "garage", "car park", "Parking", "Car Port", "Garage", "Car Park"];
        private readonly TimeSpan lightStateCheckInterval = TimeSpan.FromSeconds(15);

        private readonly RealTimeConfig config;
        private readonly ITimeInfo timeInfo;
        private readonly IBuildingManagerConnection buildingManager;
        private readonly IToolManagerConnection toolManager;
        private readonly ITravelBehavior travelBehavior;
        private readonly IRandomizer randomizer;

        private readonly bool[] lightStates;
        private readonly byte[] reachingTroubles;
        private readonly HashSet<ushort>[] buildingsInConstruction;

        private int lastProcessedMinute = -1;
        private bool freezeProblemTimers;

        private uint lastConfigConstructionSpeedValue;
        private double constructionSpeedValue;

        private int lightStateCheckFramesInterval;
        private int lightStateCheckCounter;
        private ushort lightCheckStep;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeBuildingAI"/> class.
        /// </summary>
        ///
        /// <param name="config">The configuration to run with.</param>
        /// <param name="timeInfo">The time information source.</param>
        /// <param name="buildingManager">A proxy object that provides a way to call the game-specific methods of the <see cref="BuildingManager"/> class.</param>
        /// <param name="toolManager">A proxy object that provides a way to call the game-specific methods of the <see cref="ToolManager"/> class.</param>
        /// <param name="travelBehavior">A behavior that provides simulation info for the citizens' traveling.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public RealTimeBuildingAI(
            RealTimeConfig config,
            ITimeInfo timeInfo,
            IBuildingManagerConnection buildingManager,
            IToolManagerConnection toolManager,
            ITravelBehavior travelBehavior,
            IRandomizer randomizer)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
            this.buildingManager = buildingManager ?? throw new ArgumentNullException(nameof(buildingManager));
            this.toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            this.travelBehavior = travelBehavior ?? throw new ArgumentNullException(nameof(travelBehavior));
            this.randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));

            lightStates = new bool[buildingManager.GetMaxBuildingsCount()];
            for (int i = 0; i < lightStates.Length; ++i)
            {
                lightStates[i] = true;
            }

            reachingTroubles = new byte[lightStates.Length];

            // This is to preallocate the hash sets to a large capacity, .NET 3.5 doesn't provide a proper way.
            var preallocated = Enumerable.Range(0, MaximumBuildingsInConstruction * 2).Select(v => (ushort)v).ToList();
            buildingsInConstruction = new[]
            {
                new HashSet<ushort>(preallocated),
                new HashSet<ushort>(preallocated),
                new HashSet<ushort>(preallocated),
                new HashSet<ushort>(preallocated),
            };

            for (int i = 0; i < buildingsInConstruction.Length; ++i)
            {
                // Calling Clear() doesn't trim the capacity, we're using this trick for preallocating the hash sets
                buildingsInConstruction[i].Clear();
            }
        }

        /// <summary>
        /// Gets the building construction time taking into account the current day time.
        /// </summary>
        ///
        /// <returns>The building construction time in game-specific units (0..10880).</returns>
        public int GetConstructionTime()
        {
            if ((toolManager.GetCurrentMode() & ItemClass.Availability.AssetEditor) != 0)
            {
                return 0;
            }

            if (config.ConstructionSpeed != lastConfigConstructionSpeedValue)
            {
                lastConfigConstructionSpeedValue = config.ConstructionSpeed;
                double inverted = 101d - lastConfigConstructionSpeedValue;
                constructionSpeedValue = inverted * inverted * inverted / 1_000_000d;
            }

            // This causes the construction to not advance in the night time
            return timeInfo.IsNightTime && config.StopConstructionAtNight
                ? ConstructionSpeedPaused
                : (int)(ConstructionSpeedMinimum * constructionSpeedValue);
        }

        /// <summary>
        /// Determines whether a building can be constructed or upgraded in the specified building zone.
        /// </summary>
        /// <param name="buildingZone">The building zone to check.</param>
        /// <param name="buildingId">The building ID. Can be 0 if we're about to construct a new building.</param>
        /// <returns>
        ///   <c>true</c> if a building can be constructed or upgraded; otherwise, <c>false</c>.
        /// </returns>
        public bool CanBuildOrUpgrade(ItemClass.Service buildingZone, ushort buildingId = 0)
        {
            int index;
            switch (buildingZone)
            {
                case ItemClass.Service.Residential:
                    index = 0;
                    break;

                case ItemClass.Service.Commercial:
                    index = 1;
                    break;

                case ItemClass.Service.Industrial:
                    index = 2;
                    break;

                case ItemClass.Service.Office:
                    index = 3;
                    break;

                default:
                    return true;
            }

            var buildings = buildingsInConstruction[index];
            buildings.RemoveWhere(IsBuildingCompletedOrMissing);

            int allowedCount = GetAllowedConstructingUpradingCount(buildingManager.GeBuildingsCount());
            bool result = buildings.Count < allowedCount;
            if (result && buildingId != 0)
            {
                buildings.Add(buildingId);
            }

            return result;
        }

        /// <summary>Registers the building with specified <paramref name="buildingId"/> as being constructed or
        /// upgraded.</summary>
        /// <param name="buildingId">The building ID to register.</param>
        /// <param name="buildingZone">The building zone.</param>
        public void RegisterConstructingBuilding(ushort buildingId, ItemClass.Service buildingZone)
        {
            switch (buildingZone)
            {
                case ItemClass.Service.Residential:
                    buildingsInConstruction[0].Add(buildingId);
                    return;

                case ItemClass.Service.Commercial:
                    buildingsInConstruction[1].Add(buildingId);
                    return;

                case ItemClass.Service.Industrial:
                    buildingsInConstruction[2].Add(buildingId);
                    return;

                case ItemClass.Service.Office:
                    buildingsInConstruction[3].Add(buildingId);
                    return;
            }
        }

        /// <summary>
        /// Performs the custom processing of the outgoing problem timer.
        /// </summary>
        /// <param name="buildingId">The ID of the building to process.</param>
        /// <param name="outgoingProblemTimer">The previous value of the outgoing problem timer.</param>
        public void ProcessBuildingProblems(ushort buildingId, byte outgoingProblemTimer)
        {
            // We have only few customers at night - that's an intended behavior.
            // To avoid commercial buildings from collapsing due to lack of customers,
            // we force the problem timer to pause at night time.
            // In the daytime, the timer is running slower.
            if (timeInfo.IsNightTime || timeInfo.Now.Minute % ProblemTimersInterval != 0 || freezeProblemTimers)
            {
                buildingManager.SetOutgoingProblemTimer(buildingId, outgoingProblemTimer);
            }
        }

        /// <summary>
        /// Performs the custom processing of the worker problem timer.
        /// </summary>
        /// <param name="buildingId">The ID of the building to process.</param>
        /// <param name="oldValue">The old value of the worker problem timer.</param>
        public void ProcessWorkerProblems(ushort buildingId, byte oldValue)
        {
            // We force the problem timer to pause at night time.
            // In the daytime, the timer is running slower.
            if (timeInfo.IsNightTime || timeInfo.Now.Minute % ProblemTimersInterval != 0 || freezeProblemTimers)
            {
                buildingManager.SetWorkersProblemTimer(buildingId, oldValue);
            }
        }

        /// <summary>Initializes the state of the all building lights.</summary>
        public void InitializeLightState()
        {
            for (ushort i = 0; i <= StepMask; i++)
            {
                UpdateLightState(i, updateBuilding: false);
            }
        }

        /// <summary>Re-calculates the duration of a simulation frame.</summary>
        public void UpdateFrameDuration()
        {
            lightStateCheckFramesInterval = (int)(lightStateCheckInterval.TotalHours / timeInfo.HoursPerFrame);
            if (lightStateCheckFramesInterval == 0)
            {
                ++lightStateCheckFramesInterval;
            }
        }

        /// <summary>Notifies this simulation object that a new simulation frame is started.
        /// The buildings will be processed again from the beginning of the list.</summary>
        /// <param name="frameIndex">The simulation frame index to process.</param>
        public void ProcessFrame(uint frameIndex)
        {
            UpdateReachingTroubles(frameIndex & StepMask);
            UpdateLightState();

            if ((frameIndex & StepMask) != 0)
            {
                return;
            }

            int currentMinute = timeInfo.Now.Minute;
            if (lastProcessedMinute != currentMinute)
            {
                lastProcessedMinute = currentMinute;
                freezeProblemTimers = false;
            }
            else
            {
                freezeProblemTimers = true;
            }
        }

        /// <summary>
        /// Determines whether the lights should be switched off in the specified building.
        /// </summary>
        /// <param name="buildingId">The ID of the building to check.</param>
        /// <returns>
        ///   <c>true</c> if the lights should be switched off in the specified building; otherwise, <c>false</c>.
        /// </returns>
        public bool ShouldSwitchBuildingLightsOff(ushort buildingId)
        {
            if(config != null && config.SwitchOffLightsAtNight)
            {
                if(lightStates != null && lightStates.Length > 0)
                {
                    return !lightStates[buildingId];
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the building with the specified ID is an entertainment target.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building is an entertainment target; otherwise, <c>false</c>.
        /// </returns>
        public bool IsEntertainmentTarget(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            // ignore closed buildings
            if (!IsBuildingWorking(buildingId) || IsNoiseRestricted(buildingId))
            {
                return false;
            }

            var buildingService = buildingManager.GetBuildingService(buildingId);
            if (buildingService == ItemClass.Service.VarsitySports)
            {
                // Do not visit varsity sport arenas for entertainment when no active events
                return false;
            }
            else if (buildingService == ItemClass.Service.Monument)
            {
                return buildingManager.IsRealUniqueBuilding(buildingId);
            }

            string className = buildingManager.GetBuildingClassName(buildingId);
            if (string.IsNullOrEmpty(className))
            {
                return true;
            }

            for (int i = 0; i < BannedEntertainmentBuildings.Length; ++i)
            {
                if (className.IndexOf(BannedEntertainmentBuildings[i], 0, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the building with the specified ID is a shopping target.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building is a shopping target; otherwise, <c>false</c>.
        /// </returns>
        public bool IsShoppingTarget(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            // ignore closed buildings
            if (!IsBuildingWorking(buildingId) || IsNoiseRestricted(buildingId))
            {
                return false;
            }

            var buildingService = buildingManager.GetBuildingService(buildingId);
            if (buildingService == ItemClass.Service.VarsitySports)
            {
                return false;
            }
            else if (buildingService == ItemClass.Service.Monument)
            {
                return buildingManager.IsRealUniqueBuilding(buildingId);
            }
            
            return true;
        }

        /// <summary>
        /// Determines whether the building with the specified ID is allowed to accept garbage services in this time of day.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building is allowed to accept garbage services in this time of day; otherwise, <c>false</c>.
        /// </returns>
        public bool IsGarbageHours(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // ignore garbage facilities
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            if (building.Info.GetAI() is LandfillSiteAI)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            float currentHour = timeInfo.CurrentHour;

            switch (buildingManager.GetBuildingService(buildingId))
            {
                case ItemClass.Service.Residential:
                    if (config.GarbageResidentialStartHour == config.GarbageResidentialEndHour)
                    {
                        return true;
                    }
                    if(config.GarbageResidentialStartHour < config.GarbageResidentialEndHour)
                    {
                        if(currentHour >= config.GarbageResidentialStartHour && currentHour <= config.GarbageResidentialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if(config.GarbageResidentialStartHour <= currentHour || currentHour <= config.GarbageResidentialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Commercial:
                    if (config.GarbageCommercialStartHour == config.GarbageCommercialEndHour)
                    {
                        return true;
                    }
                    if (config.GarbageCommercialStartHour < config.GarbageCommercialEndHour)
                    {
                        if (currentHour >= config.GarbageCommercialStartHour && currentHour <= config.GarbageCommercialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.GarbageCommercialStartHour <= currentHour || currentHour <= config.GarbageCommercialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Industrial:
                case ItemClass.Service.PlayerIndustry:
                    if (config.GarbageIndustrialStartHour == config.GarbageIndustrialEndHour)
                    {
                        return true;
                    }
                    if (config.GarbageIndustrialStartHour < config.GarbageIndustrialEndHour)
                    {
                        if (currentHour >= config.GarbageIndustrialStartHour && currentHour <= config.GarbageIndustrialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.GarbageIndustrialStartHour <= currentHour || currentHour <= config.GarbageIndustrialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Office:
                    if (config.GarbageOfficeStartHour == config.GarbageOfficeEndHour)
                    {
                        return true;
                    }
                    if (config.GarbageOfficeStartHour < config.GarbageOfficeEndHour)
                    {
                        if (currentHour >= config.GarbageOfficeStartHour && currentHour <= config.GarbageOfficeEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.GarbageOfficeStartHour <= currentHour || currentHour <= config.GarbageOfficeEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                default:
                    if (config.GarbageOtherStartHour == config.GarbageOtherEndHour)
                    {
                        return true;
                    }
                    if (config.GarbageOtherStartHour < config.GarbageOtherEndHour)
                    {
                        if (currentHour >= config.GarbageOtherStartHour && currentHour <= config.GarbageOtherEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.GarbageOtherStartHour <= currentHour || currentHour <= config.GarbageOtherEndHour)
                        {
                            return true;
                        }
                    }
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the building with the specified ID is allowed to accept mail services in this time of day.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building is allowed to accept mail services in this time of day; otherwise, <c>false</c>.
        /// </returns>
        public bool IsMailHours(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            // ignore post sorting facility 
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            if(building.Info.GetAI() is PostOfficeAI postOfficeAI)
            {
                if(postOfficeAI.m_postVanCount == 0 && postOfficeAI.m_postTruckCount > 0)
                {
                    return true;
                }
            }

            float currentHour = timeInfo.CurrentHour;

            switch (buildingManager.GetBuildingService(buildingId))
            {
                case ItemClass.Service.Residential:
                    if (config.MailResidentialStartHour == config.MailResidentialEndHour)
                    {
                        return true;
                    }
                    if (config.MailResidentialStartHour < config.MailResidentialEndHour)
                    {
                        if (currentHour >= config.MailResidentialStartHour && currentHour <= config.MailResidentialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailResidentialStartHour <= currentHour || currentHour <= config.MailResidentialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Commercial:
                    if (config.MailCommercialStartHour == config.MailCommercialEndHour)
                    {
                        return true;
                    }
                    if (config.MailCommercialStartHour < config.MailCommercialEndHour)
                    {
                        if (currentHour >= config.MailCommercialStartHour && currentHour <= config.MailCommercialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailCommercialStartHour <= currentHour || currentHour <= config.MailCommercialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Industrial:
                case ItemClass.Service.PlayerIndustry:
                    if (config.MailIndustrialStartHour == config.MailIndustrialEndHour)
                    {
                        return true;
                    }
                    if (config.MailIndustrialStartHour < config.MailIndustrialEndHour)
                    {
                        if (currentHour >= config.MailIndustrialStartHour && currentHour <= config.MailIndustrialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailIndustrialStartHour <= currentHour || currentHour <= config.MailIndustrialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Office:
                    if (config.MailOfficeStartHour == config.MailOfficeEndHour)
                    {
                        return true;
                    }
                    if (config.MailOfficeStartHour < config.MailOfficeEndHour)
                    {
                        if (currentHour >= config.MailOfficeStartHour && currentHour <= config.MailOfficeEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailOfficeStartHour <= currentHour || currentHour <= config.MailOfficeEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                default:
                    if (config.MailOtherStartHour == config.MailOtherEndHour)
                    {
                        return true;
                    }
                    if (config.MailOtherStartHour < config.MailOtherEndHour)
                    {
                        if (currentHour >= config.MailOtherStartHour && currentHour <= config.MailOtherEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailOtherStartHour <= currentHour || currentHour <= config.MailOtherEndHour)
                        {
                            return true;
                        }
                    }
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the park with the specified ID is allowed to accept park maintenance services in this time of day.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the park is allowed to accept park maintenance services in this time of day; otherwise, <c>false</c>.
        /// </returns>
        public bool IsParkMaintenanceHours(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            float currentHour = timeInfo.CurrentHour;

            switch (buildingManager.GetBuildingService(buildingId))
            {
                case ItemClass.Service.Beautification:
                default:
                    if (config.ParkMaintenanceStartHour == config.ParkMaintenanceEndHour)
                    {
                        return true;
                    }
                    if (config.ParkMaintenanceStartHour < config.ParkMaintenanceEndHour)
                    {
                        if (currentHour >= config.ParkMaintenanceStartHour && currentHour <= config.ParkMaintenanceEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.ParkMaintenanceStartHour <= currentHour || currentHour <= config.ParkMaintenanceEndHour)
                        {
                            return true;
                        }
                    }
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the segment with the specified ID is allowed to accept maintenance and snow services in this time of day.
        /// </summary>
        /// <param name="segmentId">The segment ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the segment is allowed to accept maintenance and snow services in this time of day; otherwise, <c>false</c>.
        /// </returns>
        public bool IsMaintenanceSnowRoadServiceHours(ushort segmentId)
        {
            if (segmentId == 0)
            {
                return true;
            }

            float currentHour = timeInfo.CurrentHour;

            var road_info = Singleton<NetManager>.instance.m_segments.m_buffer[segmentId].Info;

            switch (road_info.category)
            {
                case "RoadsSmall":
                    if (config.MaintenanceSnowRoadsSmallStartHour == config.MaintenanceSnowRoadsSmallEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsSmallStartHour < config.MaintenanceSnowRoadsSmallEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsSmallStartHour && currentHour <= config.MaintenanceSnowRoadsSmallEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsSmallStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsSmallEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case "RoadsMedium":
                    if (config.MaintenanceSnowRoadsMediumStartHour == config.MaintenanceSnowRoadsMediumEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsMediumStartHour < config.MaintenanceSnowRoadsMediumEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsMediumStartHour && currentHour <= config.MaintenanceSnowRoadsMediumEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsMediumStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsMediumEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case "RoadsLarge":
                    if (config.MaintenanceSnowRoadsLargeStartHour == config.MaintenanceSnowRoadsLargeEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsLargeStartHour < config.MaintenanceSnowRoadsLargeEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsLargeStartHour && currentHour <= config.MaintenanceSnowRoadsLargeEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsLargeStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsLargeEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case "RoadsHighway":
                    if (config.MaintenanceSnowRoadsHighwayStartHour == config.MaintenanceSnowRoadsHighwayEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsHighwayStartHour < config.MaintenanceSnowRoadsHighwayEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsHighwayStartHour && currentHour <= config.MaintenanceSnowRoadsHighwayEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsHighwayStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsHighwayEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                default:
                    if (config.MaintenanceSnowRoadsOtherStartHour == config.MaintenanceSnowRoadsOtherEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsOtherStartHour < config.MaintenanceSnowRoadsOtherEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsOtherStartHour && currentHour <= config.MaintenanceSnowRoadsOtherEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsOtherStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsOtherEndHour)
                        {
                            return true;
                        }
                    }
                    return false;
            }
        }

        /// <summary>Determines whether a building with specified ID is currently active.</summary>
        /// <param name="buildingId">The ID of the building to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with specified ID is currently active; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBuildingActive(ushort buildingId) => buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active);

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> is noise restricted
        /// (has NIMBY policy that is active on current time).
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <param name="currentBuildingId">The ID of a building where the citizen starts their journey.
        /// Specify 0 if there is no journey in schedule.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified <paramref name="buildingId"/> has NIMBY policy
        ///   that is active on current time; otherwise, <c>false</c>.
        /// </returns>
        public bool IsNoiseRestricted(ushort buildingId, ushort currentBuildingId = 0)
        {
            if (buildingManager.GetBuildingSubService(buildingId) != ItemClass.SubService.CommercialLeisure)
            {
                return false;
            }

            float currentHour = timeInfo.CurrentHour;
            if (currentHour >= config.GoToSleepHour || currentHour <= config.WakeUpHour)
            {
                return buildingManager.IsBuildingNoiseRestricted(buildingId);
            }

            if (currentBuildingId == 0)
            {
                return false;
            }

            float travelTime = travelBehavior.GetEstimatedTravelTime(currentBuildingId, buildingId);
            if (travelTime == 0)
            {
                return false;
            }

            float arriveHour = (float)timeInfo.Now.AddHours(travelTime).TimeOfDay.TotalHours;
            if (arriveHour >= config.GoToSleepHour || arriveHour <= config.WakeUpHour)
            {
                return buildingManager.IsBuildingNoiseRestricted(buildingId);
            }

            return false;
        }

        /// <summary>Registers a trouble reaching the building with the specified ID.</summary>
        /// <param name="buildingId">The ID of the building where the citizen will not arrive as planned.</param>
        public void RegisterReachingTrouble(ushort buildingId)
        {
            ref byte trouble = ref reachingTroubles[buildingId];
            if (trouble < 255)
            {
                trouble = (byte)Math.Min(255, trouble + 10);
                buildingManager.UpdateBuildingColors(buildingId);
            }
        }

        /// <summary>Gets the reaching trouble factor for a building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the reaching trouble factor of.</param>
        /// <returns>A value in range 0 to 1 that describes how many troubles have citizens while trying to reach
        /// the building.</returns>
        public float GetBuildingReachingTroubleFactor(ushort buildingId) => reachingTroubles[buildingId] / 255f;

        /// <summary>
        /// Creates a building burning time for the specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to created a burning time for.</param>
        public void CreateBuildingFire(ushort buildingID)
        {
            var burnTime = FireBurnTimeManager.GetBuildingBurnTime(buildingID);
            if (burnTime.Equals(default(FireBurnTimeManager.BurnTime)))
            {
                FireBurnTimeManager.CreateBuildingBurnTime(buildingID, timeInfo);
            }
        }

        /// <summary>
        /// remove burning time from the building with the specified <paramref name="buildingId"/>
        /// <param name="buildingId">The building ID to remove burning time from.</param>
        public void RemoveBuildingFire(ushort buildingID) => FireBurnTimeManager.RemoveBuildingBurnTime(buildingID);

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> has burned
        /// enough time for the fire to be put out
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified <paramref name="buildingId"/> has been burned
        ///   enough time for the fire to be put out; otherwise, <c>false</c>.
        /// </returns>
        public bool ShouldExtinguishFire(ushort buildingID)
        {
            if (!config.RealisticFires)
            {
                return true;
            }
            var burnTime = FireBurnTimeManager.GetBuildingBurnTime(buildingID);
            if (burnTime.Equals(default(FireBurnTimeManager.BurnTime)))
            {
                return false;
            }
            if (burnTime.StartDate == timeInfo.Now.Date)
            {
                return timeInfo.CurrentHour >= burnTime.StartTime + burnTime.Duration;
            }
            else if (burnTime.StartDate < timeInfo.Now.Date)
            {
                if (burnTime.StartTime + burnTime.Duration >= 24f)
                {
                    float nextDayTime = burnTime.StartTime + burnTime.Duration - 24f;
                    return timeInfo.CurrentHour >= nextDayTime;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether an event is within operation hours
        /// </summary>
        /// <param name="data">The EventData to check.</param>
        /// <returns>
        ///   <c>true</c> if the event with the specified <paramref name="data"/> is currently within operation hours otherwise, <c>false</c>.
        /// </returns>
        public bool IsEventWithinOperationHours(ref EventData data)
        {
            var event_start_time = Singleton<SimulationManager>.instance.FrameToTime(data.m_startFrame);
            var event_end_time = data.StartTime.AddHours(data.Info.m_eventAI.m_eventDuration);
            if (event_start_time.Hour >= config.WorkBegin && event_end_time.Hour <= config.GoToSleepHour)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the building with specified ID is the main building of an Industrial or a Campus area.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified ID is the main building of an Industrial or a Campus area;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAreaMainBuilding(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            var buildinAI = buildingInfo?.m_buildingAI;
            return buildinAI is MainCampusBuildingAI || buildinAI is MainIndustryBuildingAI;
        }

        /// <summary>
        /// Determines whether the building with specified ID is a warehouse or not.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified ID is a warehouse;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWarehouseBuilding(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            var buildingInfo = building.Info;
            var buildinAI = buildingInfo?.m_buildingAI;
            if(buildinAI is WarehouseAI warehouseAI)
            {
                bool is_special = warehouseAI.m_storageType == TransferManager.TransferReason.Logs || warehouseAI.m_storageType == TransferManager.TransferReason.Ore ||
                    warehouseAI.m_storageType == TransferManager.TransferReason.Oil || warehouseAI.m_storageType == TransferManager.TransferReason.Grain;
                return !is_special;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether the building with specified ID is a residental building of an Industrial or a Campus area.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified ID is a residental building of an Industrial or a Campus area;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAreaResidentalBuilding(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            // Here we need to check if the mod is active
            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            var buildinAI = buildingInfo?.m_buildingAI;
            if (buildinAI is AuxiliaryBuildingAI && buildinAI.GetType().Name.Equals("BarracksAI") || buildinAI is CampusBuildingAI && buildinAI.GetType().Name.Equals("DormsAI"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the building with specified ID is a residental building of senior citizens or orphans.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified ID is a residental building of senior citizens or orphans;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public static bool IsCimCareBuilding(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            // Here we need to check if the mod is active
            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            var buildinAI = buildingInfo?.m_buildingAI;
            if (buildinAI.GetType().Name.Equals("NursingHomeAI") || buildinAI.GetType().Name.Equals("OrphanageAI"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the building with specified ID is essential to the supply chain
        /// when advanced automation policy is on.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified ID is essential to the supply chain when advanced automation policy is on;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEssentialIndustryBuilding(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            var buildingInfo = building.Info;
            var buildinAI = buildingInfo?.m_buildingAI;

            var instance = Singleton<DistrictManager>.instance;
            byte b = instance.GetPark(building.m_position);
            if (b != 0)
            {
                if (instance.m_parks.m_buffer[b].IsIndustry)
                {
                    var parkPolicies = instance.m_parks.m_buffer[b].m_parkPolicies;
                    if ((parkPolicies & DistrictPolicies.Park.AdvancedAutomation) != 0)
                    {
                        if (buildinAI is ProcessingFacilityAI || buildinAI is WarehouseAI || buildinAI is WarehouseStationAI)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> is currently working
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified <paramref name="buildingId"/> is currently working otherwise, <c>false</c>.
        /// </returns>
        public bool IsBuildingWorking(ushort buildingId)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);

            var service = building.Info.m_class.m_service;
            var subService = building.Info.m_class.m_subService;
            var level = building.Info.m_class.m_level;

            switch (service)
            {
                // ignore residential buildings of any kind
                case ItemClass.Service.Residential:
                    if (!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)))
                    {
                        BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingId);
                    }
                    return true;

                // ignore nursing homes and orphanages, create worke time for child care and elder care normal buildings or edit existing building to not work at nights
                case ItemClass.Service.HealthCare when level >= ItemClass.Level.Level4:
                    if (IsCimCareBuilding(buildingId))
                    {
                        if(!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)))
                        {
                            BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingId);
                        }
                        return true;
                    }
                    else if(workTime.Equals(default(BuildingWorkTimeManager.WorkTime)))
                    {
                        BuildingWorkTimeManager.CreateBuildingWorkTime(buildingId, building.Info);
                    }
                    else if (!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)) && workTime.WorkAtNight == true)
                    {
                        workTime.WorkShifts = 2;
                        workTime.WorkAtNight = false;
                        workTime.WorkAtWeekands = true;
                        workTime.HasExtendedWorkShift = false;
                        workTime.HasContinuousWorkShift = false;
                        BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                    }
                    break;

                // ignore resident homes of industry and campus, set main area buildings and warehouses to work all the time
                case ItemClass.Service.PlayerEducation:
                case ItemClass.Service.PlayerIndustry:
                    if (IsAreaResidentalBuilding(buildingId))
                    {
                        if (!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)))
                        {
                            BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingId);
                        }
                        return true;
                    }
                    else if (IsAreaMainBuilding(buildingId))
                    {
                        if (!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)) && workTime.WorkShifts != 3)
                        {
                            workTime.WorkShifts = 3;
                            workTime.WorkAtNight = true;
                            workTime.WorkAtWeekands = true;
                            BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                        }
                    }
                    else if (IsWarehouseBuilding(buildingId))
                    {
                        if (!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)) && workTime.WorkShifts != 3)
                        {
                            workTime.WorkShifts = 3;
                            workTime.WorkAtNight = true;
                            workTime.WorkAtWeekands = true;
                            BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                        }
                    }
                    break;
            }

            switch (service)
            {
                // update universities and campuses to have 2 shifts for night school
                case ItemClass.Service.PlayerEducation:
                case ItemClass.Service.Education when building.Info.m_class.m_level == ItemClass.Level.Level3:
                    if (!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)) && workTime.WorkShifts != 2 && !IsAreaMainBuilding(buildingId))
                    {
                        workTime.WorkShifts = 2;
                        BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                    }
                    break;

                // open or close park according to night tours check
                case ItemClass.Service.Beautification when subService == ItemClass.SubService.BeautificationParks:
                    if (!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)))
                    {
                        var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
                        byte parkId = DistrictManager.instance.GetPark(position);
                        if (parkId != 0 && (DistrictManager.instance.m_parks.m_buffer[parkId].m_parkPolicies & DistrictPolicies.Park.NightTours) != 0)
                        {
                            workTime.WorkShifts = 3;
                            workTime.WorkAtNight = true;
                        }
                        else
                        {
                            workTime.WorkShifts = 2;
                            workTime.WorkAtNight = false;
                        }
                        BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                    }
                    break;

                // open or close farming or forestry buildings according to the advanced automation policy
                case ItemClass.Service.PlayerIndustry when subService == ItemClass.SubService.PlayerIndustryFarming || subService == ItemClass.SubService.PlayerIndustryForestry:
                    if (!workTime.Equals(default(BuildingWorkTimeManager.WorkTime)) && !IsAreaMainBuilding(buildingId))
                    {
                        if (IsEssentialIndustryBuilding(buildingId) && workTime.WorkShifts != 3)
                        {
                            workTime.WorkShifts = 3;
                            workTime.WorkAtNight = true;
                            BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                        }
                        else if (!IsEssentialIndustryBuilding(buildingId) && workTime.WorkShifts != 2)
                        {
                            workTime.WorkShifts = 2;
                            workTime.WorkAtNight = false;
                            BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                        }
                    }
                    break;
            }

            // WorkForceMatters setting is enabled and no one at work - building will not work
            if (config.WorkForceMatters && GetWorkersInBuilding(buildingId) == 0)
            {
                return false;
            }

            float currentHour = timeInfo.CurrentHour;
            float startHour = Math.Min(config.WakeUpHour, EarliestWakeUp);

            if (timeInfo.IsNightTime)
            {
                if (workTime.WorkShifts == 2 && !workTime.HasContinuousWorkShift)
                {
                    return currentHour >= startHour && currentHour < config.GoToSleepHour;
                }
                return workTime.WorkAtNight;
            }

            if (config.IsWeekendEnabled && timeInfo.Now.IsWeekend())
            {
                return workTime.WorkAtWeekands;
            }


            if (workTime.HasExtendedWorkShift)
            {
                if (building.Info.m_class.m_service == ItemClass.Service.Education || building.Info.m_class.m_service == ItemClass.Service.PlayerEducation)
                {
                    // set old schools to support new shift count
                    if (building.Info.m_class.m_service == ItemClass.Service.Education && (building.Info.m_class.m_level == ItemClass.Level.Level1 || building.Info.m_class.m_level == ItemClass.Level.Level2))
                    {
                        if (workTime.WorkShifts == 2)
                        {
                            workTime.WorkShifts = 1;
                            BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                        }
                    }
                    // new school 1 shift
                    if (workTime.WorkShifts == 1)
                    {
                        return currentHour >= startHour && currentHour < config.SchoolEnd;
                    }
                    // universities - might have night classes closes at 10 pm
                    return currentHour >= startHour && currentHour < 22f;
                }
                if (workTime.WorkShifts == 1)
                {
                    return currentHour >= startHour && currentHour < config.WorkEnd;
                }
                return currentHour >= startHour && currentHour < config.GoToSleepHour;
            }
            else if (workTime.HasContinuousWorkShift)
            {
                if (workTime.WorkShifts == 1)
                {
                    return currentHour >= 8f && currentHour < 20f;
                }
                return true; // two work shifts
            }
            else
            {
                if (workTime.WorkShifts == 1)
                {
                    return currentHour >= startHour && currentHour < config.WorkEnd;
                }
                else if (workTime.WorkShifts == 2)
                {
                    return currentHour >= startHour && currentHour < config.GoToSleepHour;
                }
                else
                {
                    return true; // three work shifts
                }
            }
        }

        /// <summary>
        /// Get the number of workers currently working in the specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>the number of workers in the specified building</returns>
        public int GetWorkersInBuilding(ushort buildingId)
        {
            int count = 0;
            uint[] workforce = GetBuildingWorkForce(buildingId);
            for (int i = 0; i < workforce.Length; i++)
            {
                var citizen = CitizenManager.instance.m_citizens.m_buffer[workforce[i]];

                // check if student
                bool isStudent = (citizen.m_flags & Citizen.Flags.Student) != 0 || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Teen;

                // if at work and not a student and current building is the work building
                if (citizen.CurrentLocation == Citizen.Location.Work && citizen.m_workBuilding == buildingId && !isStudent)
                {
                    count++;
                }
            }
            // support buildings that does not have workers at all
            if(workforce.Length == 0)
            {
                return 1;
            }
            return count;
        }

        /// <summary>
        /// Get an array of workers that belong to specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>an array of workers that belong to the specified building</returns>
        public uint[] GetBuildingWorkForce(ushort buildingId)
        {
            var workforce = new List<uint>();
            var buildingData = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            var instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Work) != 0)
                {
                    if (instance.m_units.m_buffer[num].m_citizen0 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen0);
                    }
                    if (instance.m_units.m_buffer[num].m_citizen1 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen1);
                    }
                    if (instance.m_units.m_buffer[num].m_citizen2 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen2);
                    }
                    if (instance.m_units.m_buffer[num].m_citizen3 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen3);
                    }
                    if (instance.m_units.m_buffer[num].m_citizen4 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen4);
                    }
                }
                num = instance.m_units.m_buffer[num].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            return workforce.ToArray();
        }

        private static int GetAllowedConstructingUpradingCount(int currentBuildingCount)
        {
            if (currentBuildingCount < ConstructionRestrictionThreshold1)
            {
                return ConstructionRestrictionStep1;
            }

            if (currentBuildingCount < ConstructionRestrictionThreshold2)
            {
                return ConstructionRestrictionStep1 + currentBuildingCount / ConstructionRestrictionScale2;
            }

            if (currentBuildingCount < ConstructionRestrictionThreshold3)
            {
                return ConstructionRestrictionStep2 + currentBuildingCount / ConstructionRestrictionScale3;
            }

            return MaximumBuildingsInConstruction;
        }

        private bool IsBuildingCompletedOrMissing(ushort buildingId) => buildingManager.BuildingHasFlags(buildingId, Building.Flags.Completed | Building.Flags.Deleted, includeZero: true);

        private void UpdateLightState()
        {
            if (lightStateCheckCounter > 0)
            {
                --lightStateCheckCounter;
                return;
            }

            ushort step = lightCheckStep;
            lightCheckStep = (ushort)((step + 1) & StepMask);
            lightStateCheckCounter = lightStateCheckFramesInterval;

            UpdateLightState(step, updateBuilding: true);
        }

        private void UpdateReachingTroubles(uint step)
        {
            ushort first = (ushort)(step * BuildingStepSize);
            ushort last = (ushort)((step + 1) * BuildingStepSize - 1);

            for (ushort i = first; i <= last; ++i)
            {
                ref byte trouble = ref reachingTroubles[i];
                if (trouble > 0)
                {
                    --trouble;
                    buildingManager.UpdateBuildingColors(i);
                }
            }
        }

        private void UpdateLightState(ushort step, bool updateBuilding)
        {
            ushort first = (ushort)(step * BuildingStepSize);
            ushort last = (ushort)((step + 1) * BuildingStepSize - 1);

            for (ushort i = first; i <= last; ++i)
            {
                if (!buildingManager.BuildingHasFlags(i, Building.Flags.Created))
                {
                    continue;
                }

                buildingManager.GetBuildingService(i, out var service, out var subService);
                bool lightsOn = !ShouldSwitchBuildingLightsOff(i, service, subService);
                if (lightsOn == lightStates[i])
                {
                    continue;
                }

                lightStates[i] = lightsOn;
                if (updateBuilding)
                {
                    buildingManager.UpdateBuildingColors(i);
                    if (!lightsOn && service != ItemClass.Service.Residential)
                    {
                        buildingManager.DeactivateVisually(i);
                    }
                }
            }
        }

        private bool ShouldSwitchBuildingLightsOff(ushort buildingId, ItemClass.Service service, ItemClass.SubService subService)
        {
            switch (service)
            {
                case ItemClass.Service.None:
                    return false;

                case ItemClass.Service.Residential:
                case ItemClass.Service.HealthCare when IsCimCareBuilding(buildingId):
                    if (buildingManager.GetBuildingHeight(buildingId) > config.SwitchOffLightsMaxHeight)
                    {
                        return false;
                    }
                    float currentHour = timeInfo.CurrentHour;
                    return currentHour < Math.Min(config.WakeUpHour, EarliestWakeUp) || currentHour >= config.GoToSleepHour;

                case ItemClass.Service.Commercial when subService == ItemClass.SubService.CommercialLeisure:
                    return IsNoiseRestricted(buildingId);

                case ItemClass.Service.Office:
                case ItemClass.Service.Commercial:
                case ItemClass.Service.Monument:
                    if (buildingManager.GetBuildingHeight(buildingId) > config.SwitchOffLightsMaxHeight)
                    {
                        return false;
                    }

                    goto default;

                case ItemClass.Service.ServicePoint:
                    return false;

                default:
                    return !IsBuildingWorking(buildingId);
            }
        }

        /// <summary>
        /// Determines whether the building will have weekly pickups of mail and garabge or not
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the building will have weekly pickups;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public bool WeeklyPickupsOnly() => config.WeeklyPickupsOnly;


        /// <summary>
        /// Determines whether a commerical building will receive goods delivery once a week or not
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the building will receive goods delivery once a week;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public bool WeeklyCommericalDeliveries() => config.WeeklyCommericalDeliveries;


    }
}

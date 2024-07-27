// RealTimeMod.cs

namespace RealTime.Core
{
    using System;
    using System.IO;
    using System.Linq;
    using CitiesHarmony.API;
    using ColossalFramework;
    using ColossalFramework.Globalization;
    using ColossalFramework.IO;
    using ColossalFramework.Plugins;
    using ICities;
    using RealTime.Config;
    using RealTime.CustomAI;
    using RealTime.Localization;
    using RealTime.UI;
    using SkyTools.Configuration;
    using SkyTools.Localization;
    using SkyTools.Tools;
    using SkyTools.UI;
    using UnityEngine;

    /// <summary>The main class of the Real Time mod.</summary>
    public class RealTimeMod : LoadingExtensionBase, IUserMod
    {
        private const long WorkshopId = 3059406297;
        private const string NoWorkshopMessage = "Real Time can only run when subscribed to in Steam Workshop";

        private readonly string modVersion = GitVersion.GetAssemblyVersion(typeof(RealTimeMod).Assembly);
        private readonly string modPath = GetModPath();

        public static ConfigurationProvider<RealTimeConfig> configProvider;
        private RealTimeCore core;
        private ConfigUI configUI;
        private LocalizationProvider localizationProvider;

        /// <summary>Gets the name of this mod.</summary>
        public string Name => "Real Time";

        /// <summary>Gets the description string of this mod.</summary>
        public string Description => "Adjusts the time flow and the Cims behavior to make them more real. Version: " + modVersion;

        /// <summary>Called when this mod is enabled.</summary>
        public void OnEnabled()
        {
            Log.SetupDebug(Name, LogCategory.Generic);

            Log.Info("The 'Real Time' mod has been enabled, version: " + modVersion);
            configProvider = new ConfigurationProvider<RealTimeConfig>(RealTimeConfig.StorageId, Name, () => new RealTimeConfig(latestVersion: true));
            configProvider.LoadDefaultConfiguration();
            localizationProvider = new LocalizationProvider(Name, modPath);
            HarmonyHelper.DoOnHarmonyReady(() => PatchUtil.PatchAll());
            AtlasUtils.CreateAtlas();
        }

        /// <summary>Called when this mod is disabled.</summary>
        public void OnDisabled()
        {
            CloseConfigUI();
            if (configProvider?.IsDefault == true)
            {
                configProvider.SaveDefaultConfiguration();
            }

            if (HarmonyHelper.IsHarmonyInstalled)
            {
                PatchUtil.UnpatchAll();
            }

            Log.Info("The 'Real Time' mod has been disabled.");
        }

        /// <summary>Called when this mod's settings page needs to be created.</summary>
        /// <param name="helper">
        /// An <see cref="UIHelperBase"/> reference that can be used to construct the mod's settings page.
        /// </param>
        public void OnSettingsUI(UIHelperBase helper)
        {
            if (helper == null || configProvider == null)
            {
                return;
            }

            if (configProvider.Configuration == null)
            {
                Log.Warning("The 'Real Time' mod wants to display the configuration page, but the configuration is unexpectedly missing.");
                configProvider.LoadDefaultConfiguration();
            }

            IViewItemFactory itemFactory = new CitiesViewItemFactory(helper);
            CloseConfigUI();
            configUI = ConfigUI.Create(configProvider, itemFactory);
            ApplyLanguage();
        }


        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            try
            {
                FireBurnTimeManager.Init();
                BuildingWorkTimeManager.Init();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                FireBurnTimeManager.Deinit();
                BuildingWorkTimeManager.Deinit();
            }
        }

        /// <summary>
        /// Called when a game level is loaded. If applicable, activates the Real Time mod for the loaded level.
        /// </summary>
        /// <param name="mode">The <see cref="LoadMode"/> a game level is loaded in.</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.LoadGame:
                case LoadMode.NewGame:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                    break;

                default:
                    return;
            }

            Log.Info($"The 'Real Time' mod starts, game mode {mode}.");

            var compatibility = Compatibility.Create(localizationProvider);

            bool isNewGame = mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario;
            core = RealTimeCore.Run(configProvider, modPath, localizationProvider, isNewGame, compatibility);
            if (core == null)
            {
                Log.Warning("Showing a warning message to user because the mod isn't working");
                MessageBox.Show(
                    localizationProvider.Translate(TranslationKeys.Warning),
                    localizationProvider.Translate(TranslationKeys.ModNotWorkingMessage));
            }
            else
            {
                CheckCompatibility(compatibility);
            }

            var buildings = Singleton<BuildingManager>.instance.m_buildings;

            string[] CarParkingBuildings = ["parking", "garage", "car park", "Parking", "Car Port", "Garage", "Car Park"];

            for (ushort buildingId = 0; buildingId < buildings.m_size; buildingId++)
            {
                var building = buildings.m_buffer[buildingId];
                if ((building.m_flags & Building.Flags.Created) != 0)
                {
                    if(BuildingWorkTimeManager.BuildingWorkTimeExist(buildingId))
                    {
                        var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);

                        var service = building.Info.m_class.m_service;
                        var subService = building.Info.m_class.m_subService;
                        var level = building.Info.m_class.m_level;
                        // update buildings 
                        switch (service)
                        {
                            // ignore residential buildings of any kind
                            case ItemClass.Service.Residential:
                                BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingId);
                                break;

                            // ignore nursing homes and orphanages, set child care and elder care to close at night
                            case ItemClass.Service.HealthCare when level >= ItemClass.Level.Level4:
                                if (RealTimeBuildingAI.IsCimCareBuilding(buildingId))
                                {
                                    BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingId);
                                }
                                if (workTime.WorkAtNight == true)
                                {
                                    workTime.WorkShifts = 2;
                                    workTime.WorkAtNight = false;
                                    workTime.WorkAtWeekands = true;
                                    workTime.HasExtendedWorkShift = false;
                                    workTime.HasContinuousWorkShift = false;
                                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                                }
                                break;

                            // area main building works 24/7, universities work 2 shifts for night school support
                            case ItemClass.Service.PlayerEducation:
                            case ItemClass.Service.Education when building.Info.m_class.m_level == ItemClass.Level.Level3:
                                if (RealTimeBuildingAI.IsAreaMainBuilding(buildingId) && workTime.WorkShifts != 3)
                                {
                                    workTime.WorkShifts = 3;
                                    workTime.WorkAtNight = true;
                                    workTime.WorkAtWeekands = true;
                                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                                }
                                if (workTime.WorkShifts != 2)
                                {
                                    workTime.WorkShifts = 2;
                                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                                }
                                if (RealTimeBuildingAI.IsAreaResidentalBuilding(buildingId))
                                {
                                    BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingId);
                                }
                                break;

                            // old elementary school and high school - update to 1 shift
                            case ItemClass.Service.Education when building.Info.m_class.m_level == ItemClass.Level.Level1 || building.Info.m_class.m_level == ItemClass.Level.Level2:
                                if (workTime.WorkShifts == 2)
                                {
                                    workTime.WorkShifts = 1;
                                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                                }
                                break;

                            // open or close farming or forestry buildings according to the advanced automation policy, set 24/7 for general warehouses and main buildings
                            case ItemClass.Service.PlayerIndustry:
                                if (RealTimeBuildingAI.IsAreaResidentalBuilding(buildingId))
                                {
                                    BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingId);
                                }
                                else if ((RealTimeBuildingAI.IsAreaMainBuilding(buildingId) || RealTimeBuildingAI.IsWarehouseBuilding(buildingId)) && workTime.WorkShifts != 3)
                                {
                                    workTime.WorkShifts = 3;
                                    workTime.WorkAtNight = true;
                                    workTime.WorkAtWeekands = true;
                                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                                }
                                else if (subService == ItemClass.SubService.PlayerIndustryFarming || subService == ItemClass.SubService.PlayerIndustryForestry)
                                {
                                    if (RealTimeBuildingAI.IsEssentialIndustryBuilding(buildingId) && workTime.WorkShifts != 3)
                                    {
                                        workTime.WorkShifts = 3;
                                        workTime.WorkAtNight = true;
                                        BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                                    }
                                    else if (!RealTimeBuildingAI.IsEssentialIndustryBuilding(buildingId) && workTime.WorkShifts != 2)
                                    {
                                        workTime.WorkShifts = 2;
                                        workTime.WorkAtNight = false;
                                        BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                                    }
                                }
                                break;

                            // open or close park according to night tours check
                            case ItemClass.Service.Beautification when subService == ItemClass.SubService.BeautificationParks:
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
                                break;

                            // car parking buildings are always open
                            case ItemClass.Service.Beautification:

                                if (CarParkingBuildings.Any(s => building.Info.name.Contains(s)))
                                {
                                    workTime.WorkAtNight = true;
                                    workTime.WorkAtWeekands = true;
                                    workTime.HasExtendedWorkShift = false;
                                    workTime.HasContinuousWorkShift = false;
                                    workTime.WorkShifts = 3;
                                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                                }
                                break;

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when a game level is about to be unloaded. If the Real Time mod was activated for this level,
        /// deactivates the mod for this level.
        /// </summary>
        public override void OnLevelUnloading()
        {
            if (core != null)
            {
                Log.Info("The 'Real Time' mod stops.");
                core = null;
            }

            configProvider.LoadDefaultConfiguration();
        }

        private static string GetModPath()
        {
            string addonsPath = Path.Combine(DataLocation.localApplicationData, "Addons");
            string localModsPath = Path.Combine(addonsPath, "Mods");
            string localModPath = Path.Combine(localModsPath, "RealTime");

            if(Directory.Exists(localModPath))
            {
                return localModPath;
            }

            var pluginInfo = PluginManager.instance.GetPluginsInfo()
                .FirstOrDefault(pi => pi.publishedFileID.AsUInt64 == WorkshopId);

            return pluginInfo?.modPath;
        }

        private void CheckCompatibility(Compatibility compatibility)
        {
            if (core == null)
            {
                return;
            }

            string message = null;
            bool incompatibilitiesDetected = configProvider.Configuration.ShowIncompatibilityNotifications
                && compatibility.AreAnyIncompatibleModsActive(out message);

            if (core.IsRestrictedMode)
            {
                message += localizationProvider.Translate(TranslationKeys.RestrictedMode);
            }

            if (incompatibilitiesDetected || core.IsRestrictedMode)
            {
                Notification.Notify(Name + " - " + localizationProvider.Translate(TranslationKeys.Warning), message);
            }
        }

        private void ApplyLanguage()
        {
            if (!SingletonLite<LocaleManager>.exists || localizationProvider == null)
            {
                return;
            }

            if (localizationProvider.LoadTranslation(LocaleManager.instance.language))
            {
                localizationProvider.SetEnglishUSFormatsState(configProvider.Configuration.UseEnglishUSFormats);
                core?.Translate(localizationProvider);
            }

            configUI?.Translate(localizationProvider);
        }

        private void CloseConfigUI()
        {
            if (configUI != null)
            {
                configUI.Close();
                configUI = null;
            }
        }
    }
}

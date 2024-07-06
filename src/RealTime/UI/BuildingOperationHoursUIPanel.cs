namespace RealTime.UI
{
    using System.Linq;
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.Config;
    using RealTime.CustomAI;
    using RealTime.Localization;
    using SkyTools.Localization;
    using UnityEngine;
    using static ColossalFramework.DataBinding.BindPropertyByKey;

    internal class BuildingOperationHoursUIPanel
    {
        public readonly UIPanel m_uiMainPanel;

        private readonly UIPanel m_InnerPanel;
        private readonly UILabel m_settingsTitle;
        private readonly UILabel m_settingsStatus;
        private readonly UICheckBox m_operationHoursSettingsCheckBox;

        private readonly UICheckBox m_workAtNight;
        private readonly UICheckBox m_workAtWeekands;
        private readonly UICheckBox m_hasExtendedWorkShift;
        private readonly UICheckBox m_hasContinuousWorkShift;

        private readonly UILabel m_workShiftsLabel;
        private readonly UISlider m_workShifts;
        private readonly UILabel m_workShiftsCount;

        private readonly UIButton SaveBuildingSettingsBtn;
        private readonly UIButton ApplyPrefabSettingsBtn;
        private readonly UIButton ApplyGlobalSettingsBtn;

        private readonly UIButton SetPrefabSettingsBtn;
        private readonly UIButton SetGlobalSettingsBtn;

        private readonly UIButton UnlockSettingsBtn;

        private readonly string t_defaultSettingsStatus;
        private readonly string t_buildingSettingsStatus;
        private readonly string t_prefabSettingsStatus;
        private readonly string t_globalSettingsStatus;

        private readonly string[] CarParkingBuildings = ["parking", "garage", "car park", "Parking", "Car Port", "Garage", "Car Park"];

        public BuildingOperationHoursUIPanel(BuildingWorldInfoPanel buildingWorldInfoPanel, UIPanel uIPanel, ILocalizationProvider localizationProvider)
        {
            string t_operationHoursSettingsCheckBox = localizationProvider.Translate(TranslationKeys.OperationHoursSettingsCheckBox);
            string t_operationHoursSettingsCheckBoxTooltip = localizationProvider.Translate(TranslationKeys.OperationHoursSettingsCheckBoxTooltip);
            string t_settingsTitle = localizationProvider.Translate(TranslationKeys.SettingsTitle);
            string t_workAtNight = localizationProvider.Translate(TranslationKeys.WorkAtNight);
            string t_workAtNightTooltip = localizationProvider.Translate(TranslationKeys.WorkAtNightTooltip);
            string t_workAtWeekands = localizationProvider.Translate(TranslationKeys.WorkAtWeekands);
            string t_workAtWeekandsTooltip = localizationProvider.Translate(TranslationKeys.WorkAtWeekandsTooltip);
            string t_hasExtendedWorkShift = localizationProvider.Translate(TranslationKeys.HasExtendedWorkShift);
            string t_hasExtendedWorkShiftTooltip = localizationProvider.Translate(TranslationKeys.HasExtendedWorkShiftTooltip);
            string t_hasContinuousWorkShift = localizationProvider.Translate(TranslationKeys.HasContinuousWorkShift);
            string t_hasContinuousWorkShiftTooltip = localizationProvider.Translate(TranslationKeys.HasContinuousWorkShiftTooltip);
            string t_shiftCountTitle = localizationProvider.Translate(TranslationKeys.ShiftCountTitle);
            string t_shiftCountTooltip = localizationProvider.Translate(TranslationKeys.ShiftCountTooltip);

            string t_SaveBuildingSettings = localizationProvider.Translate(TranslationKeys.SaveBuildingSettings);
            string t_SaveBuildingSettingsTooltip = localizationProvider.Translate(TranslationKeys.SaveBuildingSettingsTooltip);
            string t_applyPrefabSettings = localizationProvider.Translate(TranslationKeys.ApplyPrefabSettings);
            string t_applyPrefabSettingsTooltip = localizationProvider.Translate(TranslationKeys.ApplyPrefabSettingsTooltip);
            string t_applyGlobalSettings = localizationProvider.Translate(TranslationKeys.ApplyGlobalSettings);
            string t_applyGlobalSettingsTooltip = localizationProvider.Translate(TranslationKeys.ApplyGlobalSettingsTooltip);
            string t_setPrefabSettings = localizationProvider.Translate(TranslationKeys.SetPrefabSettings);
            string t_setPrefabSettingsTooltip = localizationProvider.Translate(TranslationKeys.SetPrefabSettingsTooltip);
            string t_setGlobalSettings = localizationProvider.Translate(TranslationKeys.SetGlobalSettings);
            string t_setGlobalSettingsTooltip = localizationProvider.Translate(TranslationKeys.SetGlobalSettingsTooltip);
            string t_unlockSettings = localizationProvider.Translate(TranslationKeys.UnlockSettings);
            string t_unlockSettingsTooltip = localizationProvider.Translate(TranslationKeys.UnlockSettingsTooltip);

            t_defaultSettingsStatus = localizationProvider.Translate(TranslationKeys.DefaultSettingsStatus);
            t_buildingSettingsStatus = localizationProvider.Translate(TranslationKeys.BuildingSettingsStatus);
            t_prefabSettingsStatus = localizationProvider.Translate(TranslationKeys.PrefabSettingsStatus);
            t_globalSettingsStatus = localizationProvider.Translate(TranslationKeys.GlobalSettingsStatus);


            m_uiMainPanel = buildingWorldInfoPanel.component.AddUIComponent<UIPanel>();
            m_uiMainPanel.name = "OperationHoursUIPanel";
            m_uiMainPanel.backgroundSprite = "SubcategoriesPanel";
            m_uiMainPanel.opacity = 0.90f;
            m_uiMainPanel.isVisible = false;
            m_uiMainPanel.relativePosition = new Vector3(m_uiMainPanel.parent.width + 1, 0f);
            m_uiMainPanel.height = 370f;
            m_uiMainPanel.width = 510f;

            m_operationHoursSettingsCheckBox = UiUtils.CreateCheckBox(uIPanel, "OperationHoursSettingsCheckBox", t_operationHoursSettingsCheckBox, t_operationHoursSettingsCheckBoxTooltip, false);
            m_operationHoursSettingsCheckBox.width = 110f;
            m_operationHoursSettingsCheckBox.label.textColor = new Color32(185, 221, 254, 255);
            m_operationHoursSettingsCheckBox.label.textScale = 0.8125f;
            m_operationHoursSettingsCheckBox.AlignTo(buildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
            m_operationHoursSettingsCheckBox.relativePosition = new Vector3(350f, 6f);
            m_operationHoursSettingsCheckBox.eventCheckChanged += (component, value) =>
            {
                m_uiMainPanel.isVisible = value;
                if (m_uiMainPanel.isVisible)
                {
                    m_uiMainPanel.height = 370f;
                }
                else
                {
                    m_workAtNight.Disable();
                    m_workAtWeekands.Disable();
                    m_hasExtendedWorkShift.Disable();
                    m_hasContinuousWorkShift.Disable();
                    m_workShifts.Disable();

                    SaveBuildingSettingsBtn.Disable();
                    ApplyPrefabSettingsBtn.Disable();
                    ApplyGlobalSettingsBtn.Disable();
                    SetPrefabSettingsBtn.Disable();
                    SetGlobalSettingsBtn.Disable();
                    UnlockSettingsBtn.Show();
                }
            };
            uIPanel.AttachUIComponent(m_operationHoursSettingsCheckBox.gameObject);

            m_settingsTitle = UiUtils.CreateLabel(m_uiMainPanel, "SettingsTitle", t_settingsTitle, "");
            m_settingsTitle.font = UiUtils.GetUIFont("OpenSans-Regular");
            m_settingsTitle.textAlignment = UIHorizontalAlignment.Center;
            m_settingsTitle.textColor = new Color32(78, 184, 126, 255);
            m_settingsTitle.relativePosition = new Vector3(130f, 20f);
            m_settingsTitle.textScale = 1.2f;

            m_settingsStatus = UiUtils.CreateLabel(m_uiMainPanel, "SettingsStatus", "", "");
            m_settingsStatus.font = UiUtils.GetUIFont("OpenSans-Regular");
            m_settingsStatus.textAlignment = UIHorizontalAlignment.Center;
            m_settingsStatus.textColor = new Color32(215, 51, 58, 255);
            m_settingsStatus.relativePosition = new Vector3(10f, 90f);
            m_settingsStatus.textScale = 1.2f;

            m_workAtNight = UiUtils.CreateCheckBox(m_uiMainPanel, "WorkAtNight", t_workAtNight, t_workAtNightTooltip, false);
            m_workAtNight.width = 110f;
            m_workAtNight.label.textColor = new Color32(185, 221, 254, 255);
            m_workAtNight.label.textScale = 0.8125f;
            m_workAtNight.AlignTo(buildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
            m_workAtNight.relativePosition = new Vector3(30f, 130f);
            m_workAtNight.eventCheckChanged += (component, value) =>
            {
                m_workAtNight.isChecked = value;
                UpdateSlider();
            };
            m_uiMainPanel.AttachUIComponent(m_workAtNight.gameObject);

            m_workAtWeekands = UiUtils.CreateCheckBox(m_uiMainPanel, "WorkAtWeekands", t_workAtWeekands, t_workAtWeekandsTooltip, false);
            m_workAtWeekands.width = 110f;
            m_workAtWeekands.label.textColor = new Color32(185, 221, 254, 255);
            m_workAtWeekands.label.textScale = 0.8125f;
            m_workAtWeekands.AlignTo(buildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
            m_workAtWeekands.relativePosition = new Vector3(30f, 170f);
            m_workAtWeekands.eventCheckChanged += (component, value) => m_workAtWeekands.isChecked = value;
            m_uiMainPanel.AttachUIComponent(m_workAtWeekands.gameObject);

            m_hasExtendedWorkShift = UiUtils.CreateCheckBox(m_uiMainPanel, "HasExtendedWorkShift", t_hasExtendedWorkShift, t_hasExtendedWorkShiftTooltip, false);
            m_hasExtendedWorkShift.width = 110f;
            m_hasExtendedWorkShift.label.textColor = new Color32(185, 221, 254, 255);
            m_hasExtendedWorkShift.label.textScale = 0.8125f;
            m_hasExtendedWorkShift.AlignTo(buildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
            m_hasExtendedWorkShift.relativePosition = new Vector3(30f, 210f);
            m_hasExtendedWorkShift.eventCheckChanged += (component, value) =>
            {
                m_hasExtendedWorkShift.isChecked = value;
                if (m_hasExtendedWorkShift.isChecked)
                {
                    m_hasContinuousWorkShift.isChecked = false;
                }
                UpdateSlider();
            };
            m_uiMainPanel.AttachUIComponent(m_hasExtendedWorkShift.gameObject);

            m_hasContinuousWorkShift = UiUtils.CreateCheckBox(m_uiMainPanel, "HasContinuousWorkShift", t_hasContinuousWorkShift, t_hasContinuousWorkShiftTooltip, false);
            m_hasContinuousWorkShift.width = 110f;
            m_hasContinuousWorkShift.label.textColor = new Color32(185, 221, 254, 255);
            m_hasContinuousWorkShift.label.textScale = 0.8125f;
            m_hasContinuousWorkShift.AlignTo(buildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
            m_hasContinuousWorkShift.relativePosition = new Vector3(30f, 250f);
            m_hasContinuousWorkShift.eventCheckChanged += (component, value) =>
            {
                m_hasContinuousWorkShift.isChecked = value;
                if (m_hasContinuousWorkShift.isChecked)
                {
                    m_hasExtendedWorkShift.isChecked = false;
                }
                UpdateSlider();
            };
            m_uiMainPanel.AttachUIComponent(m_hasContinuousWorkShift.gameObject);

            m_InnerPanel = UiUtils.CreatePanel(m_uiMainPanel, "OperationHoursInnerPanel");
            m_InnerPanel.backgroundSprite = "GenericPanelLight";
            m_InnerPanel.color = new Color32(206, 206, 206, 255);
            m_InnerPanel.size = new Vector2(235f, 66f);
            m_InnerPanel.relativePosition = new Vector3(15f, 282f);

            m_workShiftsLabel = UiUtils.CreateLabel(m_uiMainPanel, "WorkShiftsTitle", t_shiftCountTitle, "");
            m_workShiftsLabel.font = UiUtils.GetUIFont("OpenSans-Regular");
            m_workShiftsLabel.textAlignment = UIHorizontalAlignment.Center;
            m_workShiftsLabel.relativePosition = new Vector3(10f, 10f);
            m_InnerPanel.AttachUIComponent(m_workShiftsLabel.gameObject);

            m_workShifts = UiUtils.CreateSlider(m_InnerPanel, "ShiftCount", t_shiftCountTooltip, 1, 3, 1, 1);
            m_workShifts.size = new Vector2(130f, 8f);
            m_workShifts.relativePosition = new Vector3(25f, 48f);
            m_workShifts.disabledColor = Color.black;
            m_workShifts.eventValueChanged += (component, value) =>
            {
                if (m_workShiftsCount != null)
                {
                    if (value == -1)
                    {
                        value = 1;
                    }
                    m_workShiftsCount.text = value.ToString();
                }
            };
            m_InnerPanel.AttachUIComponent(m_workShifts.gameObject);

            m_workShiftsCount = UiUtils.CreateLabel(m_InnerPanel, "OperationHoursInnerCount", "", "");
            m_workShiftsCount.textAlignment = UIHorizontalAlignment.Right;
            m_workShiftsCount.verticalAlignment = UIVerticalAlignment.Top;
            m_workShiftsCount.textColor = new Color32(185, 221, 254, 255);
            m_workShiftsCount.textScale = 1f;
            m_workShiftsCount.autoSize = false;
            m_workShiftsCount.size = new Vector2(30f, 16f);
            m_workShiftsCount.relativePosition = new Vector3(150f, 44f);
            m_InnerPanel.AttachUIComponent(m_workShiftsCount.gameObject);

            SaveBuildingSettingsBtn = UiUtils.AddButton(m_uiMainPanel, 260f, 120f, "SaveBuildingSettings", t_SaveBuildingSettings, t_SaveBuildingSettingsTooltip);
            SaveBuildingSettingsBtn.eventClicked += SaveBuildingSettings;

            ApplyPrefabSettingsBtn = UiUtils.AddButton(m_uiMainPanel, 260f, 170f, "ApplyPrefabSettings", t_applyPrefabSettings, t_applyPrefabSettingsTooltip);
            ApplyPrefabSettingsBtn.eventClicked += ApplyPrefabSettings;

            ApplyGlobalSettingsBtn = UiUtils.AddButton(m_uiMainPanel, 260f, 220f, "ApplyGlobalSettings", t_applyGlobalSettings, t_applyGlobalSettingsTooltip);
            ApplyGlobalSettingsBtn.eventClicked += ApplyGlobalSettings;

            SetPrefabSettingsBtn = UiUtils.AddButton(m_uiMainPanel, 260f, 270f, "SetPrefabSettings", t_setPrefabSettings, t_setPrefabSettingsTooltip);
            SetPrefabSettingsBtn.eventClicked += SetPrefabSettings;

            SetGlobalSettingsBtn = UiUtils.AddButton(m_uiMainPanel, 260f, 320f, "SetGlobalSettings", t_setGlobalSettings, t_setGlobalSettingsTooltip);
            SetGlobalSettingsBtn.eventClicked += SetGlobalSettings;

            UnlockSettingsBtn = UiUtils.AddButton(m_uiMainPanel, 130f, 55f, "UnlockSettings", t_unlockSettings, t_unlockSettingsTooltip);
            UnlockSettingsBtn.eventClicked += UnlockSettings;

            m_workAtNight.Disable();
            m_workAtWeekands.Disable();
            m_hasExtendedWorkShift.Disable();
            m_hasContinuousWorkShift.Disable();
            m_workShifts.Disable();

            SaveBuildingSettingsBtn.Disable();
            ApplyPrefabSettingsBtn.Disable();
            ApplyGlobalSettingsBtn.Disable();
            SetPrefabSettingsBtn.Disable();
            SetGlobalSettingsBtn.Disable();
        }

        public void UnlockSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            m_workAtNight.Enable();
            m_workAtWeekands.Enable();
            m_hasExtendedWorkShift.Enable();
            m_hasContinuousWorkShift.Enable();
            m_workShifts.Enable();

            SaveBuildingSettingsBtn.Enable();
            SetPrefabSettingsBtn.Enable();
            SetGlobalSettingsBtn.Enable();

            UnlockSettingsBtn.Hide();
        }

        private void UpdateSlider()
        {
            if (m_hasContinuousWorkShift.isChecked)
            {
                if (m_workAtNight.isChecked)
                {
                    m_workShifts.maxValue = 2;
                    m_workShifts.minValue = 2;
                    m_workShifts.value = 2;
                    m_workShiftsCount.text = "2";
                    m_workShifts.Disable();
                }
                else
                {
                    m_workShifts.maxValue = 1;
                    m_workShifts.minValue = 1;
                    m_workShiftsCount.text = "1";
                    m_workShifts.value = 1;
                    m_workShifts.Disable();
                }
            }
            else
            {
                if (m_workAtNight.isChecked)
                {
                    m_workShifts.maxValue = 3;
                    m_workShifts.minValue = 3;
                    m_workShifts.value = 3;
                    m_workShiftsCount.text = "3";
                    m_workShifts.Disable();
                }
                else
                {
                    m_workShifts.maxValue = 2;
                    m_workShifts.minValue = 1;
                    if(!UnlockSettingsBtn.isVisible)
                    {
                        m_workShifts.Enable();
                    }
                }
            }
        }

        public void RefreshData()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            var buildingAI = building.Info.GetAI();
            var instance = Singleton<DistrictManager>.instance;
            bool IsAllowedZoned = buildingAI is CommercialBuildingAI || buildingAI is IndustrialBuildingAI || buildingAI is IndustrialExtractorAI || buildingAI is OfficeBuildingAI;
            bool isAllowedCityService = buildingAI is BankOfficeAI || buildingAI is PostOfficeAI || buildingAI is SaunaAI || buildingAI is TourBuildingAI || buildingAI is MonumentAI;
            bool isAllowedParkBuilding = buildingAI is ParkBuildingAI && instance.GetPark(building.m_position) == 0 && !CarParkingBuildings.Any(s => building.Info.name.Contains(s));
            bool isPark = buildingAI is ParkAI && !CarParkingBuildings.Any(s => building.Info.name.Contains(s));

            if (IsAllowedZoned || isAllowedCityService || isAllowedParkBuilding || isPark)
            {
                if (BuildingWorkTimeManager.BuildingsWorkTime.TryGetValue(buildingID, out var buildingWorkTime))
                {
                    m_settingsStatus.text = t_buildingSettingsStatus;
                    m_workAtNight.isChecked = buildingWorkTime.WorkAtNight;
                    m_workAtWeekands.isChecked = buildingWorkTime.WorkAtWeekands;
                    m_hasExtendedWorkShift.isChecked = buildingWorkTime.HasExtendedWorkShift;
                    m_hasContinuousWorkShift.isChecked = buildingWorkTime.HasContinuousWorkShift;
                    m_workShifts.value = buildingWorkTime.WorkShifts;
                    m_workShiftsCount.text = buildingWorkTime.WorkShifts.ToString();
                    UpdateSlider();
                }
                else
                {
                    string buildingAIstr = buildingAI.GetType().Name;
                    int prefab_index = BuildingWorkTimeManager.BuildingsWorkTimePrefabs.FindIndex(item => item.InfoName == building.Info.name && item.BuildingAI == buildingAIstr);
                    if (prefab_index != -1)
                    {
                        m_settingsStatus.text = t_prefabSettingsStatus;
                        var buildingWorkTimePrefab = BuildingWorkTimeManager.BuildingsWorkTimePrefabs[prefab_index];
                        m_workAtNight.isChecked = buildingWorkTimePrefab.WorkAtNight;
                        m_workAtWeekands.isChecked = buildingWorkTimePrefab.WorkAtWeekands;
                        m_hasExtendedWorkShift.isChecked = buildingWorkTimePrefab.HasExtendedWorkShift;
                        m_hasContinuousWorkShift.isChecked = buildingWorkTimePrefab.HasContinuousWorkShift;
                        m_workShifts.value = buildingWorkTimePrefab.WorkShifts;
                        m_workShiftsCount.text = buildingWorkTimePrefab.WorkShifts.ToString();
                        UpdateSlider();
                        ApplyPrefabSettingsBtn.Enable();
                    }
                    else
                    {
                        ApplyPrefabSettingsBtn.Disable();
                        int global_index = BuildingWorkTimeGlobalConfig.Config.BuildingWorkTimeGlobalSettings.FindIndex(item => item.InfoName == building.Info.name && item.BuildingAI == buildingAIstr);
                        if (global_index != -1)
                        {
                            m_settingsStatus.text = t_globalSettingsStatus;
                            var saved_config = BuildingWorkTimeGlobalConfig.Config.BuildingWorkTimeGlobalSettings[global_index];
                            m_workAtNight.isChecked = saved_config.WorkAtNight;
                            m_workAtWeekands.isChecked = saved_config.WorkAtWeekands;
                            m_hasExtendedWorkShift.isChecked = saved_config.HasExtendedWorkShift;
                            m_hasContinuousWorkShift.isChecked = saved_config.HasContinuousWorkShift;
                            m_workShifts.value = saved_config.WorkShifts;
                            m_workShiftsCount.text = saved_config.WorkShifts.ToString();
                            ApplyGlobalSettingsBtn.Enable();
                        }
                        else
                        {
                            m_settingsStatus.text = t_defaultSettingsStatus;
                            ApplyGlobalSettingsBtn.Disable();
                        }
                    }
                }
                m_operationHoursSettingsCheckBox.Show();
                m_operationHoursSettingsCheckBox.relativePosition = new Vector3(350f, 6f);

                m_workAtNight.relativePosition = new Vector3(30f, 130f);
                m_workAtWeekands.relativePosition = new Vector3(30f, 170f);
                m_hasExtendedWorkShift.relativePosition = new Vector3(30f, 210f);
                m_hasContinuousWorkShift.relativePosition = new Vector3(30f, 250f);

                m_workShiftsLabel.relativePosition = new Vector3(10f, 10f);
                m_workShifts.relativePosition = new Vector3(25f, 48f);
                m_workShiftsCount.relativePosition = new Vector3(150f, 44f);

                if (m_operationHoursSettingsCheckBox.isChecked)
                {
                    m_uiMainPanel.height = 370f;
                    m_uiMainPanel.Show();
                }
            }
            else
            {
                m_operationHoursSettingsCheckBox.Hide();
                m_uiMainPanel.Hide();
            }
        }

        public void SaveBuildingSettings(UIComponent c, UIMouseEventParameter eventParameter) => ApplySettings(true, false, false);

        public void ApplyPrefabSettings(UIComponent c, UIMouseEventParameter eventParameter) => ApplySettings(false, true, false);

        public void ApplyGlobalSettings(UIComponent c, UIMouseEventParameter eventParameter) => ApplySettings(false, false, true);

        private void ApplySettings(bool isBuilding, bool isPrefab, bool isGlobal)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;

            if (isBuilding)
            {
                var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

                buildingWorkTime.WorkAtNight = m_workAtNight.isChecked;
                buildingWorkTime.WorkAtWeekands = m_workAtWeekands.isChecked;
                buildingWorkTime.HasExtendedWorkShift = m_hasExtendedWorkShift.isChecked;
                buildingWorkTime.HasContinuousWorkShift = m_hasContinuousWorkShift.isChecked;
                buildingWorkTime.WorkShifts = (int)m_workShifts.value;

                BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
            }
            else if (isPrefab)
            {
                var prefabRecord = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                if(!prefabRecord.Equals(default(BuildingWorkTimeManager.WorkTimePrefab)))
                {
                    m_workAtNight.isChecked = prefabRecord.WorkAtNight;
                    m_workAtWeekands.isChecked = prefabRecord.WorkAtWeekands;
                    m_hasExtendedWorkShift.isChecked = prefabRecord.HasExtendedWorkShift;
                    m_hasContinuousWorkShift.isChecked = prefabRecord.HasContinuousWorkShift;
                    m_workShifts.value = prefabRecord.WorkShifts;
                }
            }
            else if (isGlobal)
            {
                var buildingWorkTimeGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo.name, BuildingAIstr);

                if (!buildingWorkTimeGlobal.Equals(default(BuildingWorkTimeGlobal)))
                {
                    m_workAtNight.isChecked = buildingWorkTimeGlobal.WorkAtNight;
                    m_workAtWeekands.isChecked = buildingWorkTimeGlobal.WorkAtWeekands;
                    m_hasExtendedWorkShift.isChecked = buildingWorkTimeGlobal.HasExtendedWorkShift;
                    m_hasContinuousWorkShift.isChecked = buildingWorkTimeGlobal.HasContinuousWorkShift;
                    m_workShifts.value = buildingWorkTimeGlobal.WorkShifts;
                }
            }
        }

        public void SetPrefabSettings(UIComponent c, UIMouseEventParameter eventParameter) => ConfirmPanel.ShowModal("Set Prefab Settings", "This will update all building records of this type to the current number of apartments in this save!", (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }
            SetPrefabGlobalSettings(false);
        });

        public void SetGlobalSettings(UIComponent c, UIMouseEventParameter eventParameter) => ConfirmPanel.ShowModal("Set Global Settings", "This will update all building records of this type to the current number of apartments across all saves!", (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }
            SetPrefabGlobalSettings(true);
        });

        private void SetPrefabGlobalSettings(bool isGlobal)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;

            if (!isGlobal)
            {
                // set new prefab settings according to the building current settings
                var buildingWorkTimePrefab = new BuildingWorkTimeManager.WorkTimePrefab
                {
                    InfoName = buildingInfo.name,
                    BuildingAI = BuildingAIstr,
                    WorkAtNight = m_workAtNight.isChecked,
                    WorkAtWeekands = m_workAtWeekands.isChecked,
                    HasExtendedWorkShift = m_hasExtendedWorkShift.isChecked,
                    HasContinuousWorkShift = m_hasContinuousWorkShift.isChecked,
                    WorkShifts = (int)m_workShifts.value
                };

                // clear all individual building settings of this type
                var buildingsList = BuildingWorkTimeManager.BuildingsWorkTime.Where(item =>
                {
                    var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                    return Info.name == buildingInfo.name && Info.GetAI().GetType().Name == BuildingAIstr;
                }).ToList();

                foreach (var item in buildingsList)
                {
                    BuildingWorkTimeManager.RemoveBuildingWorkTime(item.Key);
                }

                // try get prefab settings and update them or create new prefab settings for this building type
                // if not exist and apply the settings to all the individual buildings
                var prefabRecord = BuildingWorkTimeManager.GetPrefab(buildingInfo);

                if (!prefabRecord.Equals(default(BuildingWorkTimeManager.WorkTimePrefab)))
                {
                    prefabRecord.WorkAtNight = buildingWorkTimePrefab.WorkAtNight;
                    prefabRecord.WorkAtWeekands = buildingWorkTimePrefab.WorkAtWeekands;
                    prefabRecord.HasExtendedWorkShift = buildingWorkTimePrefab.HasExtendedWorkShift;
                    prefabRecord.HasContinuousWorkShift = buildingWorkTimePrefab.HasContinuousWorkShift;
                    prefabRecord.WorkShifts = buildingWorkTimePrefab.WorkShifts;

                    BuildingWorkTimeManager.SetPrefab(prefabRecord);
                }
                else
                {
                    BuildingWorkTimeManager.CreatePrefab(buildingWorkTimePrefab);
                }
            }
            else
            {
                // set global settings

                // clear all individual building settings of this type
                var buildingsList = BuildingWorkTimeManager.BuildingsWorkTime.Where(item =>
                {
                    var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                    return Info.name == buildingInfo.name && Info.GetAI().GetType().Name == BuildingAIstr;
                }).ToList();

                foreach (var item in buildingsList)
                {
                    BuildingWorkTimeManager.RemoveBuildingWorkTime(item.Key);
                }

                // clear all prefab building settings of this type
                var buildingsPrefabList = BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Where(item => item.InfoName == buildingInfo.name && item.BuildingAI == BuildingAIstr).ToList();

                foreach (var item in buildingsPrefabList)
                {
                    BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Remove(item);
                }

                // set new global settings according to the building current settings
                var buildingWorkTimeGlobal = new BuildingWorkTimeGlobal
                {
                    InfoName = buildingInfo.name,
                    BuildingAI = BuildingAIstr,
                    WorkAtNight = m_workAtNight.isChecked,
                    WorkAtWeekands = m_workAtWeekands.isChecked,
                    HasExtendedWorkShift = m_hasExtendedWorkShift.isChecked,
                    HasContinuousWorkShift = m_hasContinuousWorkShift.isChecked,
                    WorkShifts = (int)m_workShifts.value
                };

                // try get global settings and update them or create new global settings for this building type
                // if not exist and apply the settings to all the individual buildings
                var globalRecord = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo.name, BuildingAIstr);

                if (!globalRecord.Equals(default(BuildingWorkTimeGlobal)))
                {
                    m_workAtNight.isChecked = buildingWorkTimeGlobal.WorkAtNight;
                    m_workAtWeekands.isChecked = buildingWorkTimeGlobal.WorkAtWeekands;
                    m_hasExtendedWorkShift.isChecked = buildingWorkTimeGlobal.HasExtendedWorkShift;
                    m_hasContinuousWorkShift.isChecked = buildingWorkTimeGlobal.HasContinuousWorkShift;
                    m_workShifts.value = buildingWorkTimeGlobal.WorkShifts;

                    BuildingWorkTimeGlobalConfig.Config.SetGlobalSettings(buildingWorkTimeGlobal);
                }
                else
                {
                    BuildingWorkTimeGlobalConfig.Config.CreateGlobalSettings(buildingWorkTimeGlobal);
                }
            }
        }
    }
}

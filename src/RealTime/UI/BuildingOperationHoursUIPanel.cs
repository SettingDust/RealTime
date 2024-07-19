namespace RealTime.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.Config;
    using RealTime.Core;
    using RealTime.CustomAI;
    using RealTime.Localization;
    using SkyTools.Localization;
    using UnityEngine;

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
        private readonly UIButton ReturnToDefaultBtn;
        private readonly UIButton ApplyPrefabSettingsBtn;
        private readonly UIButton ApplyGlobalSettingsBtn;

        private readonly UIButton SetPrefabSettingsBtn;
        private readonly UIButton SetGlobalSettingsBtn;

        private readonly UIButton UnlockSettingsBtn;
        private readonly UIButton LockUnlockChangesBtn;

        private readonly string t_defaultSettingsStatus;
        private readonly string t_buildingSettingsStatus;
        private readonly string t_prefabSettingsStatus;
        private readonly string t_globalSettingsStatus;

        private readonly float CheckBoxXposition;
        private readonly float CheckBoxYposition;

        private readonly string[] CarParkingBuildings = ["parking", "garage", "car park", "Parking", "Car Port", "Garage", "Car Park"];

        public BuildingOperationHoursUIPanel(BuildingWorldInfoPanel buildingWorldInfoPanel, UIPanel uIPanel, float checkBoxXposition, float checkBoxYposition, ILocalizationProvider localizationProvider)
        {
            CheckBoxXposition = checkBoxXposition;
            CheckBoxYposition = checkBoxYposition;

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
            string t_ReturnToDefault = localizationProvider.Translate(TranslationKeys.ReturnToDefault);
            string t_ReturnToDefaultTooltip = localizationProvider.Translate(TranslationKeys.ReturnToDefaultTooltip);
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
            string t_lockUnlockChangesTooltip = localizationProvider.Translate(TranslationKeys.LockUnlockChangesTooltip);

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
            m_uiMainPanel.height = 410f;
            m_uiMainPanel.width = 510f;

            m_operationHoursSettingsCheckBox = UiUtils.CreateCheckBox(uIPanel, "OperationHoursSettingsCheckBox", t_operationHoursSettingsCheckBox, t_operationHoursSettingsCheckBoxTooltip, false);
            m_operationHoursSettingsCheckBox.width = 80f;
            m_operationHoursSettingsCheckBox.label.textColor = new Color32(185, 221, 254, 255);
            m_operationHoursSettingsCheckBox.label.textScale = 0.8125f;
            m_operationHoursSettingsCheckBox.AlignTo(buildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
            m_operationHoursSettingsCheckBox.relativePosition = new Vector3(checkBoxXposition, checkBoxYposition);
            m_operationHoursSettingsCheckBox.eventCheckChanged += (component, value) =>
            {
                m_uiMainPanel.isVisible = value;
                if (m_uiMainPanel.isVisible)
                {
                    m_uiMainPanel.height = 410f;
                }
                else
                {
                    m_workAtNight.Disable();
                    m_workAtWeekands.Disable();
                    m_hasExtendedWorkShift.Disable();
                    m_hasContinuousWorkShift.Disable();
                    m_workShifts.Disable();

                    SaveBuildingSettingsBtn.Disable();
                    ReturnToDefaultBtn.Disable();
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
            m_settingsStatus.textColor = new Color32(240, 190, 199, 255);
            m_settingsStatus.relativePosition = new Vector3(110f, 95f);
            m_settingsStatus.textScale = 0.9f;

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

            SaveBuildingSettingsBtn = UiUtils.CreateButton(m_uiMainPanel, 260f, 120f, "SaveBuildingSettings", t_SaveBuildingSettings, t_SaveBuildingSettingsTooltip);
            SaveBuildingSettingsBtn.eventClicked += SaveBuildingSettings;

            ReturnToDefaultBtn = UiUtils.CreateButton(m_uiMainPanel, 260f, 170f, "ReturnToDefault", t_ReturnToDefault, t_ReturnToDefaultTooltip);
            ReturnToDefaultBtn.eventClicked += ReturnToDefault;

            ApplyPrefabSettingsBtn = UiUtils.CreateButton(m_uiMainPanel, 260f, 220f, "ApplyPrefabSettings", t_applyPrefabSettings, t_applyPrefabSettingsTooltip);
            ApplyPrefabSettingsBtn.eventClicked += ApplyPrefabSettings;

            ApplyGlobalSettingsBtn = UiUtils.CreateButton(m_uiMainPanel, 260f, 270f, "ApplyGlobalSettings", t_applyGlobalSettings, t_applyGlobalSettingsTooltip);
            ApplyGlobalSettingsBtn.eventClicked += ApplyGlobalSettings;

            SetPrefabSettingsBtn = UiUtils.CreateButton(m_uiMainPanel, 260f, 320f, "SetPrefabSettings", t_setPrefabSettings, t_setPrefabSettingsTooltip);
            SetPrefabSettingsBtn.eventClicked += SetPrefabSettings;

            SetGlobalSettingsBtn = UiUtils.CreateButton(m_uiMainPanel, 260f, 370f, "SetGlobalSettings", t_setGlobalSettings, t_setGlobalSettingsTooltip);
            SetGlobalSettingsBtn.eventClicked += SetGlobalSettings;

            UnlockSettingsBtn = UiUtils.CreateButton(m_uiMainPanel, 130f, 55f, "UnlockSettings", t_unlockSettings, t_unlockSettingsTooltip);
            UnlockSettingsBtn.eventClicked += UnlockSettings;

            LockUnlockChangesBtn = UiUtils.CreateButton(m_uiMainPanel, 10f, 55f, "LockUnLockChanges", "", t_lockUnlockChangesTooltip, 32, 32);

            LockUnlockChangesBtn.atlas = TextureUtils.GetAtlas("LockButtonAtlas");
            LockUnlockChangesBtn.normalFgSprite = "UnLock";
            LockUnlockChangesBtn.disabledFgSprite = "UnLock";
            LockUnlockChangesBtn.focusedFgSprite = "UnLock";
            LockUnlockChangesBtn.hoveredFgSprite = "UnLock";
            LockUnlockChangesBtn.pressedFgSprite = "UnLock";

            LockUnlockChangesBtn.eventClicked += LockUnlockChanges;

            m_workAtNight.Disable();
            m_workAtWeekands.Disable();
            m_hasExtendedWorkShift.Disable();
            m_hasContinuousWorkShift.Disable();
            m_workShifts.Disable();

            SaveBuildingSettingsBtn.Disable();
            ReturnToDefaultBtn.Disable();
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
            ReturnToDefaultBtn.Enable();
            SetPrefabSettingsBtn.Enable();
            SetGlobalSettingsBtn.Enable();

            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            if (BuildingWorkTimeManager.PrefabExist(building.Info))
            {
                ApplyPrefabSettingsBtn.Enable();
            }
            if (BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(building.Info) != null)
            {
                ApplyGlobalSettingsBtn.Enable();
            }

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
                string buildingAIstr = buildingAI.GetType().Name;

                var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

                BuildingWorkTimeManager.WorkTime buildingWorkTime;

                if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID))
                {
                    buildingWorkTime = BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);
                }
                else
                {
                    buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);
                }

                var buildingWorkTimeGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(building.Info);

                if (!buildingWorkTime.IsPrefab && !buildingWorkTime.IsGlobal)
                {
                    m_settingsStatus.text = buildingWorkTime.IsDefault ? t_defaultSettingsStatus : t_buildingSettingsStatus;
                    m_workAtNight.isChecked = buildingWorkTime.WorkAtNight;
                    m_workAtWeekands.isChecked = buildingWorkTime.WorkAtWeekands;
                    m_hasExtendedWorkShift.isChecked = buildingWorkTime.HasExtendedWorkShift;
                    m_hasContinuousWorkShift.isChecked = buildingWorkTime.HasContinuousWorkShift;
                    m_workShifts.value = buildingWorkTime.WorkShifts;
                    m_workShiftsCount.text = buildingWorkTime.WorkShifts.ToString();
                }
                
                else if (BuildingWorkTimeManager.PrefabExist(building.Info) && buildingWorkTime.IsPrefab && !buildingWorkTime.IsLocked)
                {
                    var buildingWorkTimePrefab = BuildingWorkTimeManager.GetPrefab(building.Info);

                    m_settingsStatus.text = t_prefabSettingsStatus;
                    m_workAtNight.isChecked = buildingWorkTimePrefab.WorkAtNight;
                    m_workAtWeekands.isChecked = buildingWorkTimePrefab.WorkAtWeekands;
                    m_hasExtendedWorkShift.isChecked = buildingWorkTimePrefab.HasExtendedWorkShift;
                    m_hasContinuousWorkShift.isChecked = buildingWorkTimePrefab.HasContinuousWorkShift;
                    m_workShifts.value = buildingWorkTimePrefab.WorkShifts;
                    m_workShiftsCount.text = buildingWorkTimePrefab.WorkShifts.ToString();
                }
                else if(buildingWorkTimeGlobal != null && buildingWorkTime.IsGlobal && !buildingWorkTime.IsLocked)
                {
                    m_settingsStatus.text = t_globalSettingsStatus;
                    m_workAtNight.isChecked = buildingWorkTimeGlobal.WorkAtNight;
                    m_workAtWeekands.isChecked = buildingWorkTimeGlobal.WorkAtWeekands;
                    m_hasExtendedWorkShift.isChecked = buildingWorkTimeGlobal.HasExtendedWorkShift;
                    m_hasContinuousWorkShift.isChecked = buildingWorkTimeGlobal.HasContinuousWorkShift;
                    m_workShifts.value = buildingWorkTimeGlobal.WorkShifts;
                    m_workShiftsCount.text = buildingWorkTimeGlobal.WorkShifts.ToString();
                }

                UpdateSlider();

                m_operationHoursSettingsCheckBox.Show();
                m_operationHoursSettingsCheckBox.relativePosition = new Vector3(CheckBoxXposition, CheckBoxYposition);

                m_workAtNight.relativePosition = new Vector3(30f, 130f);
                m_workAtWeekands.relativePosition = new Vector3(30f, 170f);
                m_hasExtendedWorkShift.relativePosition = new Vector3(30f, 210f);
                m_hasContinuousWorkShift.relativePosition = new Vector3(30f, 250f);

                m_workShiftsLabel.relativePosition = new Vector3(10f, 10f);
                m_workShifts.relativePosition = new Vector3(25f, 48f);
                m_workShiftsCount.relativePosition = new Vector3(150f, 44f);

                string spriteName = buildingWorkTime.IsLocked ? "Lock" : "UnLock";

                LockUnlockChangesBtn.normalFgSprite = spriteName;
                LockUnlockChangesBtn.disabledFgSprite = spriteName;
                LockUnlockChangesBtn.focusedFgSprite = spriteName;
                LockUnlockChangesBtn.hoveredFgSprite = spriteName;
                LockUnlockChangesBtn.pressedFgSprite = spriteName;

                if (m_operationHoursSettingsCheckBox.isChecked)
                {
                    m_uiMainPanel.height = 410f;
                    m_uiMainPanel.Show();
                }
            }
            else
            {
                m_operationHoursSettingsCheckBox.Hide();
                m_uiMainPanel.Hide();
            }
        }

        public void LockUnlockChanges(UIComponent c, UIMouseEventParameter eventParameter)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            BuildingWorkTimeManager.WorkTime buildingWorkTime;

            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID))
            {
                buildingWorkTime = BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);
            }
            else
            {
                buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);
            }

            string spriteName = "UnLock";

            if (buildingWorkTime.IsLocked)
            {
                buildingWorkTime.IsLocked = false;
                spriteName = "UnLock";
            }
            else
            {
                buildingWorkTime.IsLocked = true;
                spriteName = "Lock";
            }

            LockUnlockChangesBtn.normalFgSprite = spriteName;
            LockUnlockChangesBtn.disabledFgSprite = spriteName;
            LockUnlockChangesBtn.focusedFgSprite = spriteName;
            LockUnlockChangesBtn.hoveredFgSprite = spriteName;
            LockUnlockChangesBtn.pressedFgSprite = spriteName;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        public void ReturnToDefault(UIComponent c, UIMouseEventParameter eventParameter) => ApplySettings(false, false, false);

        public void SaveBuildingSettings(UIComponent c, UIMouseEventParameter eventParameter) => ApplySettings(true, false, false);

        public void ApplyPrefabSettings(UIComponent c, UIMouseEventParameter eventParameter) => ApplySettings(false, true, false);

        public void ApplyGlobalSettings(UIComponent c, UIMouseEventParameter eventParameter) => ApplySettings(false, false, true);

        private void ApplySettings(bool isBuilding, bool isPrefab, bool isGlobal)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            BuildingWorkTimeManager.WorkTime buildingWorkTime;

            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID))
            {
                buildingWorkTime = BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);
            }
            else
            {
                buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);
            }

            if (isBuilding && !buildingWorkTime.IsLocked)
            {
                buildingWorkTime.WorkAtNight = m_workAtNight.isChecked;
                buildingWorkTime.WorkAtWeekands = m_workAtWeekands.isChecked;
                buildingWorkTime.HasExtendedWorkShift = m_hasExtendedWorkShift.isChecked;
                buildingWorkTime.HasContinuousWorkShift = m_hasContinuousWorkShift.isChecked;
                buildingWorkTime.WorkShifts = (int)m_workShifts.value;
                buildingWorkTime.IsDefault = false;
                buildingWorkTime.IsPrefab = isPrefab;
                buildingWorkTime.IsGlobal = isGlobal;
                buildingWorkTime.IsLocked = false;
            }
            else if (isPrefab && BuildingWorkTimeManager.PrefabExist(buildingInfo) && !buildingWorkTime.IsLocked)
            {
                var prefabRecord = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                buildingWorkTime.WorkAtNight = prefabRecord.WorkAtNight;
                buildingWorkTime.WorkAtWeekands = prefabRecord.WorkAtWeekands;
                buildingWorkTime.HasExtendedWorkShift = prefabRecord.HasExtendedWorkShift;
                buildingWorkTime.HasContinuousWorkShift = prefabRecord.HasContinuousWorkShift;
                buildingWorkTime.WorkShifts = prefabRecord.WorkShifts;
                buildingWorkTime.IsDefault = false;
                buildingWorkTime.IsPrefab = isPrefab;
                buildingWorkTime.IsGlobal = isGlobal; 
            }
            else if (isGlobal)
            {
                var buildingWorkTimeGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);

                if (buildingWorkTimeGlobal != null && !buildingWorkTime.IsLocked)
                {
                    buildingWorkTime.WorkAtNight = buildingWorkTimeGlobal.WorkAtNight;
                    buildingWorkTime.WorkAtWeekands = buildingWorkTimeGlobal.WorkAtWeekands;
                    buildingWorkTime.HasExtendedWorkShift = buildingWorkTimeGlobal.HasExtendedWorkShift;
                    buildingWorkTime.HasContinuousWorkShift = buildingWorkTimeGlobal.HasContinuousWorkShift;
                    buildingWorkTime.WorkShifts = buildingWorkTimeGlobal.WorkShifts;
                    buildingWorkTime.IsDefault = false;
                    buildingWorkTime.IsPrefab = isPrefab;
                    buildingWorkTime.IsGlobal = isGlobal;
                }
            }
            else
            {
                buildingWorkTime = BuildingWorkTimeManager.CreateDefaultBuildingWorkTime(buildingID, buildingInfo);
            }

            m_workAtNight.isChecked = buildingWorkTime.WorkAtNight;
            m_workAtWeekands.isChecked = buildingWorkTime.WorkAtWeekands;
            m_hasExtendedWorkShift.isChecked = buildingWorkTime.HasExtendedWorkShift;
            m_hasContinuousWorkShift.isChecked = buildingWorkTime.HasContinuousWorkShift;
            m_workShifts.value = buildingWorkTime.WorkShifts;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);

            RefreshData();
        }

        public void SetPrefabSettings(UIComponent c, UIMouseEventParameter eventParameter) => ConfirmPanel.ShowModal("Set Type Settings", "This will update all the buildings work hours of this type to the current work time settings in this save!", (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }
            SetPrefabGlobalSettings(false);
        });

        public void SetGlobalSettings(UIComponent c, UIMouseEventParameter eventParameter) => ConfirmPanel.ShowModal("Set Global Settings", "This will update all the buildings work hours of this type to the current work time settings across all saves!", (comp, ret) =>
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

                // set all individual building settings of this type to the new settings
                var buildingsList = BuildingWorkTimeManager.BuildingsWorkTime.Where(item =>
                {
                    var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                    return Info.name == buildingInfo.name && Info.GetAI().GetType().Name == BuildingAIstr && !item.Value.IsLocked;
                }).ToList();

                var buildingsIdsList = new List<ushort>();

                foreach (var item in buildingsList)
                {
                    buildingsIdsList.Add(item.Key);
                }

                foreach (ushort buildingId in buildingsIdsList)
                {
                    var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);
                    workTime.WorkAtNight = buildingWorkTimePrefab.WorkAtNight;
                    workTime.WorkAtWeekands = buildingWorkTimePrefab.WorkAtWeekands;
                    workTime.HasExtendedWorkShift = buildingWorkTimePrefab.HasExtendedWorkShift;
                    workTime.HasContinuousWorkShift = buildingWorkTimePrefab.HasContinuousWorkShift;
                    workTime.WorkShifts = buildingWorkTimePrefab.WorkShifts;
                    workTime.IsDefault = false;
                    workTime.IsPrefab = true;
                    workTime.IsGlobal = false;
                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                }

                if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
                {
                    // update the prefab
                    var prefabRecord = BuildingWorkTimeManager.GetPrefab(buildingInfo);

                    prefabRecord.WorkAtNight = buildingWorkTimePrefab.WorkAtNight;
                    prefabRecord.WorkAtWeekands = buildingWorkTimePrefab.WorkAtWeekands;
                    prefabRecord.HasExtendedWorkShift = buildingWorkTimePrefab.HasExtendedWorkShift;
                    prefabRecord.HasContinuousWorkShift = buildingWorkTimePrefab.HasContinuousWorkShift;
                    prefabRecord.WorkShifts = buildingWorkTimePrefab.WorkShifts;

                    BuildingWorkTimeManager.SetPrefab(prefabRecord);
                }
                else
                {
                    // create new prefab
                    BuildingWorkTimeManager.CreatePrefab(buildingWorkTimePrefab);
                }
            }
            else
            {
                // set global settings

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

                // set all individual building settings of this type to the new settings
                var buildingsList = BuildingWorkTimeManager.BuildingsWorkTime.Where(item =>
                {
                    var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                    return Info.name == buildingInfo.name && Info.GetAI().GetType().Name == BuildingAIstr && !item.Value.IsLocked;
                }).ToList();

                var buildingsIdsList = new List<ushort>();

                foreach (var item in buildingsList)
                {
                    buildingsIdsList.Add(item.Key);
                }

                foreach (ushort buildingId in buildingsIdsList)
                {
                    var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);
                    workTime.WorkAtNight = buildingWorkTimeGlobal.WorkAtNight;
                    workTime.WorkAtWeekands = buildingWorkTimeGlobal.WorkAtWeekands;
                    workTime.HasExtendedWorkShift = buildingWorkTimeGlobal.HasExtendedWorkShift;
                    workTime.HasContinuousWorkShift = buildingWorkTimeGlobal.HasContinuousWorkShift;
                    workTime.WorkShifts = buildingWorkTimeGlobal.WorkShifts;
                    workTime.IsDefault = false;
                    workTime.IsPrefab = false;
                    workTime.IsGlobal = true;
                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                }

                // try get global settings and update them or create new global settings for this building type
                // if not exist and apply the settings to all the individual buildings
                var globalRecord = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);

                if (globalRecord != null)
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
            RefreshData();
        }
    }
}

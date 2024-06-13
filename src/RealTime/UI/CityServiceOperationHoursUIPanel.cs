namespace RealTime.UI
{
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.CustomAI;
    using UnityEngine;

    public static class CityServiceOperationHoursUIPanel
    {
        public static UIPanel m_uiMainPanelCityService;

        private static CityServiceWorldInfoPanel m_cityServiceWorldInfoPanel;

        private static UIPanel m_InnerPanelCityService;

        private static UILabel m_settingsTitleCityService;

        public static UICheckBox m_settingsCheckBoxCityService;

        private static UICheckBox m_workAtNightCityService;
        private static UICheckBox m_workAtWeekandsCityService;
        private static UICheckBox m_hasExtendedWorkShiftCityService;
        private static UICheckBox m_hasContinuousWorkShiftCityService;

        private static UILabel m_workShiftsLabelCityService;
        private static UISlider m_workShiftsCityService;
        private static UILabel m_workShiftsCountCityService;

        private static UIButton SaveOperationHoursBtnCityService;

        public static void InitCityServiceUI() => CretaeCityServiceUI();

        private static void CretaeCityServiceUI()
        {
            m_cityServiceWorldInfoPanel = GameObject.Find("(Library) CityServiceWorldInfoPanel").GetComponent<CityServiceWorldInfoPanel>();
            var ParkButtons = m_cityServiceWorldInfoPanel.Find("ParkButtons").GetComponent<UIPanel>();
            if (ParkButtons != null)
            {
                m_uiMainPanelCityService = m_cityServiceWorldInfoPanel.component.AddUIComponent<UIPanel>();
                m_uiMainPanelCityService.name = "OperationHoursUIPanel";
                m_uiMainPanelCityService.backgroundSprite = "SubcategoriesPanel";
                m_uiMainPanelCityService.opacity = 0.90f;
                m_uiMainPanelCityService.isVisible = false;
                m_uiMainPanelCityService.height = 370f;
                m_uiMainPanelCityService.width = 310f;
                m_uiMainPanelCityService.relativePosition = new Vector3(m_cityServiceWorldInfoPanel.component.width + 1, 40f);

                m_settingsCheckBoxCityService = UiUtils.CreateCheckBox(ParkButtons, "SettingsCheckBox", "settings", false);
                m_settingsCheckBoxCityService.width = 110f;
                m_settingsCheckBoxCityService.label.textColor = new Color32(185, 221, 254, 255);
                m_settingsCheckBoxCityService.label.textScale = 0.8125f;
                m_settingsCheckBoxCityService.tooltip = "change building operation hours.";
                m_settingsCheckBoxCityService.AlignTo(m_cityServiceWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_settingsCheckBoxCityService.relativePosition = new Vector3(350f, 6f);
                m_settingsCheckBoxCityService.eventCheckChanged += (component, value) =>
                {
                    m_uiMainPanelCityService.isVisible = value;
                    if (m_uiMainPanelCityService.isVisible)
                    {
                        m_uiMainPanelCityService.height = m_cityServiceWorldInfoPanel.component.height - 7f;
                    }
                };
                ParkButtons.AttachUIComponent(m_settingsCheckBoxCityService.gameObject);

                m_settingsTitleCityService = UiUtils.CreateLabel(m_uiMainPanelCityService, "SettingsTitle", "Adjust Operation Hours", "");
                m_settingsTitleCityService.font = UiUtils.GetUIFont("OpenSans-Regular");
                m_settingsTitleCityService.textAlignment = UIHorizontalAlignment.Center;
                m_settingsTitleCityService.textColor = new Color32(78, 184, 126, 255);
                m_settingsTitleCityService.relativePosition = new Vector3(45f, 20f);
                m_settingsTitleCityService.textScale = 1.2f;

                m_workAtNightCityService = UiUtils.CreateCheckBox(m_uiMainPanelCityService, "WorkAtNight", "Work At Night", false);
                m_workAtNightCityService.width = 110f;
                m_workAtNightCityService.label.textColor = new Color32(185, 221, 254, 255);
                m_workAtNightCityService.label.textScale = 0.8125f;
                m_workAtNightCityService.tooltip = "choose if the building will work at night.";
                m_workAtNightCityService.relativePosition = new Vector3(30f, 60f);
                m_workAtNightCityService.eventCheckChanged += (component, value) =>
                {
                    m_workAtNightCityService.isChecked = value;
                    UpdateSliderCityService();
                };
                m_uiMainPanelCityService.AttachUIComponent(m_workAtNightCityService.gameObject);

                m_workAtWeekandsCityService = UiUtils.CreateCheckBox(m_uiMainPanelCityService, "WorkAtWeekands", "Work At Weekands", false);
                m_workAtWeekandsCityService.width = 110f;
                m_workAtWeekandsCityService.label.textColor = new Color32(185, 221, 254, 255);
                m_workAtWeekandsCityService.label.textScale = 0.8125f;
                m_workAtWeekandsCityService.tooltip = "choose if the building will work at weekends.";
                m_workAtWeekandsCityService.relativePosition = new Vector3(30f, 100f);
                m_workAtWeekandsCityService.eventCheckChanged += (component, value) => m_workAtWeekandsCityService.isChecked = value;
                m_uiMainPanelCityService.AttachUIComponent(m_uiMainPanelCityService.gameObject);

                m_hasExtendedWorkShiftCityService = UiUtils.CreateCheckBox(m_uiMainPanelCityService, "HasExtendedWorkShift", "Has Extended Work Shift", false);
                m_hasExtendedWorkShiftCityService.width = 110f;
                m_hasExtendedWorkShiftCityService.label.textColor = new Color32(185, 221, 254, 255);
                m_hasExtendedWorkShiftCityService.label.textScale = 0.8125f;
                m_hasExtendedWorkShiftCityService.tooltip = "choose if the building will have an extended work shift.";
                m_hasExtendedWorkShiftCityService.relativePosition = new Vector3(30f, 140f);
                m_hasExtendedWorkShiftCityService.eventCheckChanged += (component, value) =>
                {
                    m_hasExtendedWorkShiftCityService.isChecked = value;
                    if (m_hasExtendedWorkShiftCityService.isChecked)
                    {
                        m_hasContinuousWorkShiftCityService.isChecked = false;
                    }
                    UpdateSliderCityService();
                };
                m_uiMainPanelCityService.AttachUIComponent(m_hasExtendedWorkShiftCityService.gameObject);

                m_hasContinuousWorkShiftCityService = UiUtils.CreateCheckBox(m_uiMainPanelCityService, "HasContinuousWorkShift", "Has Continuous Work Shift", false);
                m_hasContinuousWorkShiftCityService.width = 110f;
                m_hasContinuousWorkShiftCityService.label.textColor = new Color32(185, 221, 254, 255);
                m_hasContinuousWorkShiftCityService.label.textScale = 0.8125f;
                m_hasContinuousWorkShiftCityService.tooltip = "choose if the building will have a continuous work shift.";

                m_hasContinuousWorkShiftCityService.relativePosition = new Vector3(30f, 180f);
                m_hasContinuousWorkShiftCityService.eventCheckChanged += (component, value) =>
                {
                    m_hasContinuousWorkShiftCityService.isChecked = value;
                    if (m_hasContinuousWorkShiftCityService.isChecked)
                    {
                        m_hasExtendedWorkShiftCityService.isChecked = false;
                    }
                    UpdateSliderCityService();
                };
                m_uiMainPanelCityService.AttachUIComponent(m_hasContinuousWorkShiftCityService.gameObject);

                m_InnerPanelCityService = UiUtils.CreatePanel(m_uiMainPanelCityService, "OperationHoursInnerPanel");
                m_InnerPanelCityService.backgroundSprite = "GenericPanelLight";
                m_InnerPanelCityService.color = new Color32(206, 206, 206, 255);
                m_InnerPanelCityService.size = new Vector2(220f, 66f);
                m_InnerPanelCityService.relativePosition = new Vector3(30f, 210f);

                m_workShiftsLabelCityService = UiUtils.CreateLabel(m_uiMainPanelCityService, "OperationHoursInnerTitle", "Select number of shifts", "");
                m_workShiftsLabelCityService.font = UiUtils.GetUIFont("OpenSans-Regular");
                m_workShiftsLabelCityService.textAlignment = UIHorizontalAlignment.Center;
                m_workShiftsLabelCityService.relativePosition = new Vector3(10f, 10f);
                m_InnerPanelCityService.AttachUIComponent(m_workShiftsLabelCityService.gameObject);

                m_workShiftsCityService = UiUtils.CreateSlider(m_InnerPanelCityService, "ShiftCount", 1, 3, 1, 1);
                m_workShiftsCityService.tooltip = "Select how many work shifts the building should have";
                m_workShiftsCityService.size = new Vector2(130f, 8f);
                m_workShiftsCityService.relativePosition = new Vector3(25f, 48f);
                m_workShiftsCityService.eventValueChanged += (component, value) =>
                {
                    if (m_workShiftsCountCityService != null)
                    {
                        if (value == -1)
                        {
                            value = 1;
                        }
                        m_workShiftsCountCityService.text = value.ToString();
                    }
                };
                m_InnerPanelCityService.AttachUIComponent(m_workShiftsCityService.gameObject);

                m_workShiftsCountCityService = UiUtils.CreateLabel(m_InnerPanelCityService, "OperationHoursInnerCount", "", "");
                m_workShiftsCountCityService.textAlignment = UIHorizontalAlignment.Right;
                m_workShiftsCountCityService.verticalAlignment = UIVerticalAlignment.Top;
                m_workShiftsCountCityService.textColor = new Color32(185, 221, 254, 255);
                m_workShiftsCountCityService.textScale = 1f;
                m_workShiftsCountCityService.autoSize = false;
                m_workShiftsCountCityService.size = new Vector2(30f, 16f);
                m_workShiftsCountCityService.relativePosition = new Vector3(150f, 44f);
                m_InnerPanelCityService.AttachUIComponent(m_workShiftsCountCityService.gameObject);

                SaveOperationHoursBtnCityService = UiUtils.AddButton(m_uiMainPanelCityService, 25f, 290f, "SaveOperationHours", "Save Operation Hours", "save building working hours");
                SaveOperationHoursBtnCityService.eventClicked += SaveOperationHours;

                m_workAtNightCityService.AlignTo(m_cityServiceWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_workAtWeekandsCityService.AlignTo(m_cityServiceWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_hasExtendedWorkShiftCityService.AlignTo(m_cityServiceWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_hasContinuousWorkShiftCityService.AlignTo(m_cityServiceWorldInfoPanel.component, UIAlignAnchor.TopLeft);
            }
        }

        private static void UpdateSliderCityService()
        {
            if (m_hasContinuousWorkShiftCityService.isChecked)
            {
                m_workShiftsCityService.maxValue = m_workAtNightCityService.isChecked ? 2 : 1;
                m_workShiftsCityService.value = 1;
            }
            else
            {
                m_workShiftsCityService.maxValue = m_workAtNightCityService.isChecked ? 3 : 2;
                m_workShiftsCityService.value = 1;
            }
        }

        public static void RefreshDataCityService()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            var buildingAI = building.Info.GetAI();
            if (buildingAI is BankOfficeAI || buildingAI is ParkAI || buildingAI is SaunaAI || buildingAI is TourBuildingAI || buildingAI is MonumentAI)
            {
                if (BuildingWorkTimeManager.BuildingsWorkTime.TryGetValue(buildingID, out var buildingWorkTime))
                {
                    m_workAtNightCityService.isChecked = buildingWorkTime.WorkAtNight;
                    m_workAtWeekandsCityService.isChecked = buildingWorkTime.WorkAtWeekands;
                    m_hasExtendedWorkShiftCityService.isChecked = buildingWorkTime.HasExtendedWorkShift;
                    m_hasContinuousWorkShiftCityService.isChecked = buildingWorkTime.HasContinuousWorkShift;
                    m_workShiftsCityService.value = buildingWorkTime.WorkShifts;
                }
                m_settingsCheckBoxCityService.Show();
                m_settingsCheckBoxCityService.relativePosition = new Vector3(350f, 6f);
                m_workShiftsCityService.relativePosition = new Vector3(25f, 48f);
                m_workShiftsCountCityService.relativePosition = new Vector3(150f, 44f);
                if (m_settingsCheckBoxCityService.isChecked)
                {
                    m_uiMainPanelCityService.Show();
                }
            }
            else
            {
                m_settingsCheckBoxCityService.Hide();
                m_uiMainPanelCityService.Hide();
            }
        }

        private static void SaveOperationHours(UIComponent c, UIMouseEventParameter eventParameter) => SaveSettings();

        private static void SaveSettings()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkAtNight = m_workAtNightCityService.isChecked;
            buildingWorkTime.WorkAtWeekands = m_workAtWeekandsCityService.isChecked;
            buildingWorkTime.HasExtendedWorkShift = m_hasExtendedWorkShiftCityService.isChecked;
            buildingWorkTime.HasContinuousWorkShift = m_hasContinuousWorkShiftCityService.isChecked;
            buildingWorkTime.WorkShifts = (int)m_workShiftsCityService.value;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);

            RefreshDataCityService();
        }

        private static void LoadSettings(ushort buildingID)
        {

            if (BuildingWorkTimeManager.BuildingsWorkTime.TryGetValue(buildingID, out var buildingWorkTime))
            {
                m_workAtNightCityService.isChecked = buildingWorkTime.WorkAtNight;
                m_workAtWeekandsCityService.isChecked = buildingWorkTime.WorkAtWeekands;
                m_hasExtendedWorkShiftCityService.isChecked = buildingWorkTime.HasExtendedWorkShift;
                m_hasContinuousWorkShiftCityService.isChecked = buildingWorkTime.HasContinuousWorkShift;
                m_workShiftsCityService.value = buildingWorkTime.WorkShifts;
            }

        }
    }

}

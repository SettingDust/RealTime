namespace RealTime.UI
{
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.CustomAI;
    using UnityEngine;

    public static class ZonedBuildingOperationHoursUIPanel
    {
        public static UIPanel m_uiMainPanelZonedBuilding;
        
        private static ZonedBuildingWorldInfoPanel m_zonedBuildingWorldInfoPanel;
        
        private static UIPanel m_InnerPanelZonedBuilding;
        
        private static UILabel m_settingsTitleZonedBuilding;

        public static UICheckBox m_settingsCheckBoxZonedBuilding;

        private static UICheckBox m_workAtNightZonedBuilding;
        private static UICheckBox m_workAtWeekandsZonedBuilding;
        private static UICheckBox m_hasExtendedWorkShiftZonedBuilding;
        private static UICheckBox m_hasContinuousWorkShiftZonedBuilding;

        private static UILabel m_workShiftsLabelZonedBuilding;
        private static UISlider m_workShiftsZonedBuilding;
        private static UILabel m_workShiftsCountZonedBuilding;

        private static UIButton SaveOperationHoursBtnZonedBuilding;

        public static void InitZonedUI() => CretaeZonedUI();

        private static void CretaeZonedUI()
        {
            m_zonedBuildingWorldInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();
            var makeHistoricalPanel = m_zonedBuildingWorldInfoPanel.Find("MakeHistoricalPanel").GetComponent<UIPanel>();
            if (makeHistoricalPanel != null)
            {
                m_uiMainPanelZonedBuilding = m_zonedBuildingWorldInfoPanel.component.AddUIComponent<UIPanel>();
                m_uiMainPanelZonedBuilding.name = "OperationHoursUIPanel";
                m_uiMainPanelZonedBuilding.backgroundSprite = "SubcategoriesPanel";
                m_uiMainPanelZonedBuilding.opacity = 0.90f;
                m_uiMainPanelZonedBuilding.isVisible = false;
                m_uiMainPanelZonedBuilding.height = 370f;
                m_uiMainPanelZonedBuilding.width = 310f;
                m_uiMainPanelZonedBuilding.relativePosition = new Vector3(m_zonedBuildingWorldInfoPanel.component.width + 1, 40f);

                m_settingsCheckBoxZonedBuilding = UiUtils.CreateCheckBox(makeHistoricalPanel, "SettingsCheckBox", "settings", false);
                m_settingsCheckBoxZonedBuilding.width = 110f;
                m_settingsCheckBoxZonedBuilding.label.textColor = new Color32(185, 221, 254, 255);
                m_settingsCheckBoxZonedBuilding.label.textScale = 0.8125f;
                m_settingsCheckBoxZonedBuilding.tooltip = "change building operation hours.";
                m_settingsCheckBoxZonedBuilding.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_settingsCheckBoxZonedBuilding.relativePosition = new Vector3(350f, 6f);
                m_settingsCheckBoxZonedBuilding.eventCheckChanged += (component, value) =>
                {
                    m_uiMainPanelZonedBuilding.isVisible = value;
                    if (m_uiMainPanelZonedBuilding.isVisible)
                    {
                        m_uiMainPanelZonedBuilding.height = m_zonedBuildingWorldInfoPanel.component.height - 7f;
                    }
                };
                makeHistoricalPanel.AttachUIComponent(m_settingsCheckBoxZonedBuilding.gameObject);

                m_settingsTitleZonedBuilding = UiUtils.CreateLabel(m_uiMainPanelZonedBuilding, "SettingsTitle", "Adjust Operation Hours", "");
                m_settingsTitleZonedBuilding.font = UiUtils.GetUIFont("OpenSans-Regular");
                m_settingsTitleZonedBuilding.textAlignment = UIHorizontalAlignment.Center;
                m_settingsTitleZonedBuilding.textColor = new Color32(78, 184, 126, 255);
                m_settingsTitleZonedBuilding.relativePosition = new Vector3(45f, 20f);
                m_settingsTitleZonedBuilding.textScale = 1.2f;

                m_workAtNightZonedBuilding = UiUtils.CreateCheckBox(m_uiMainPanelZonedBuilding, "WorkAtNight", "Work At Night", false);
                m_workAtNightZonedBuilding.width = 110f;
                m_workAtNightZonedBuilding.label.textColor = new Color32(185, 221, 254, 255);
                m_workAtNightZonedBuilding.label.textScale = 0.8125f;
                m_workAtNightZonedBuilding.tooltip = "choose if the building will work at night.";
                m_workAtNightZonedBuilding.relativePosition = new Vector3(30f, 60f);
                m_workAtNightZonedBuilding.eventCheckChanged += (component, value) =>
                {
                    m_workAtNightZonedBuilding.isChecked = value;
                    UpdateSliderZonedBuilding();
                };
                m_uiMainPanelZonedBuilding.AttachUIComponent(m_workAtNightZonedBuilding.gameObject);

                m_workAtWeekandsZonedBuilding = UiUtils.CreateCheckBox(m_uiMainPanelZonedBuilding, "WorkAtWeekands", "Work At Weekands", false);
                m_workAtWeekandsZonedBuilding.width = 110f;
                m_workAtWeekandsZonedBuilding.label.textColor = new Color32(185, 221, 254, 255);
                m_workAtWeekandsZonedBuilding.label.textScale = 0.8125f;
                m_workAtWeekandsZonedBuilding.tooltip = "choose if the building will work at weekends.";
                m_workAtWeekandsZonedBuilding.relativePosition = new Vector3(30f, 100f);
                m_workAtWeekandsZonedBuilding.eventCheckChanged += (component, value) => m_workAtWeekandsZonedBuilding.isChecked = value;
                m_uiMainPanelZonedBuilding.AttachUIComponent(m_uiMainPanelZonedBuilding.gameObject);

                m_hasExtendedWorkShiftZonedBuilding = UiUtils.CreateCheckBox(m_uiMainPanelZonedBuilding, "HasExtendedWorkShift", "Has Extended Work Shift", false);
                m_hasExtendedWorkShiftZonedBuilding.width = 110f;
                m_hasExtendedWorkShiftZonedBuilding.label.textColor = new Color32(185, 221, 254, 255);
                m_hasExtendedWorkShiftZonedBuilding.label.textScale = 0.8125f;
                m_hasExtendedWorkShiftZonedBuilding.tooltip = "choose if the building will have an extended work shift.";
                m_hasExtendedWorkShiftZonedBuilding.relativePosition = new Vector3(30f, 140f);
                m_hasExtendedWorkShiftZonedBuilding.eventCheckChanged += (component, value) =>
                {
                    m_hasExtendedWorkShiftZonedBuilding.isChecked = value;
                    if (m_hasExtendedWorkShiftZonedBuilding.isChecked)
                    {
                        m_hasContinuousWorkShiftZonedBuilding.isChecked = false;
                    }
                    UpdateSliderZonedBuilding();
                };
                m_uiMainPanelZonedBuilding.AttachUIComponent(m_hasExtendedWorkShiftZonedBuilding.gameObject);

                m_hasContinuousWorkShiftZonedBuilding = UiUtils.CreateCheckBox(m_uiMainPanelZonedBuilding, "HasContinuousWorkShift", "Has Continuous Work Shift", false);
                m_hasContinuousWorkShiftZonedBuilding.width = 110f;
                m_hasContinuousWorkShiftZonedBuilding.label.textColor = new Color32(185, 221, 254, 255);
                m_hasContinuousWorkShiftZonedBuilding.label.textScale = 0.8125f;
                m_hasContinuousWorkShiftZonedBuilding.tooltip = "choose if the building will have a continuous work shift.";

                m_hasContinuousWorkShiftZonedBuilding.relativePosition = new Vector3(30f, 180f);
                m_hasContinuousWorkShiftZonedBuilding.eventCheckChanged += (component, value) =>
                {
                    m_hasContinuousWorkShiftZonedBuilding.isChecked = value;
                    if (m_hasContinuousWorkShiftZonedBuilding.isChecked)
                    {
                        m_hasExtendedWorkShiftZonedBuilding.isChecked = false;
                    }
                    UpdateSliderZonedBuilding();
                };
                m_uiMainPanelZonedBuilding.AttachUIComponent(m_hasContinuousWorkShiftZonedBuilding.gameObject);

                m_InnerPanelZonedBuilding = UiUtils.CreatePanel(m_uiMainPanelZonedBuilding, "OperationHoursInnerPanel");
                m_InnerPanelZonedBuilding.backgroundSprite = "GenericPanelLight";
                m_InnerPanelZonedBuilding.color = new Color32(206, 206, 206, 255);
                m_InnerPanelZonedBuilding.size = new Vector2(220f, 66f);
                m_InnerPanelZonedBuilding.relativePosition = new Vector3(30f, 210f);

                m_workShiftsLabelZonedBuilding = UiUtils.CreateLabel(m_uiMainPanelZonedBuilding, "OperationHoursInnerTitle", "Select number of shifts", "");
                m_workShiftsLabelZonedBuilding.font = UiUtils.GetUIFont("OpenSans-Regular");
                m_workShiftsLabelZonedBuilding.textAlignment = UIHorizontalAlignment.Center;
                m_workShiftsLabelZonedBuilding.relativePosition = new Vector3(10f, 10f);
                m_InnerPanelZonedBuilding.AttachUIComponent(m_workShiftsLabelZonedBuilding.gameObject);

                m_workShiftsZonedBuilding = UiUtils.CreateSlider(m_InnerPanelZonedBuilding, "ShiftCount", 1, 3, 1, 1);
                m_workShiftsZonedBuilding.tooltip = "Select how many work shifts the building should have";
                m_workShiftsZonedBuilding.size = new Vector2(130f, 8f);
                m_workShiftsZonedBuilding.relativePosition = new Vector3(25f, 48f);
                m_workShiftsZonedBuilding.eventValueChanged += (component, value) =>
                {
                    if (m_workShiftsCountZonedBuilding != null)
                    {
                        if (value == -1)
                        {
                            value = 1;
                        }
                        m_workShiftsCountZonedBuilding.text = value.ToString();
                    }
                };
                m_InnerPanelZonedBuilding.AttachUIComponent(m_workShiftsZonedBuilding.gameObject);

                m_workShiftsCountZonedBuilding = UiUtils.CreateLabel(m_InnerPanelZonedBuilding, "OperationHoursInnerCount", "", "");
                m_workShiftsCountZonedBuilding.textAlignment = UIHorizontalAlignment.Right;
                m_workShiftsCountZonedBuilding.verticalAlignment = UIVerticalAlignment.Top;
                m_workShiftsCountZonedBuilding.textColor = new Color32(185, 221, 254, 255);
                m_workShiftsCountZonedBuilding.textScale = 1f;
                m_workShiftsCountZonedBuilding.autoSize = false;
                m_workShiftsCountZonedBuilding.size = new Vector2(30f, 16f);
                m_workShiftsCountZonedBuilding.relativePosition = new Vector3(150f, 44f);
                m_InnerPanelZonedBuilding.AttachUIComponent(m_workShiftsCountZonedBuilding.gameObject);

                SaveOperationHoursBtnZonedBuilding = UiUtils.AddButton(m_uiMainPanelZonedBuilding, 25f, 290f, "SaveOperationHours", "Save Operation Hours", "save building working hours");
                SaveOperationHoursBtnZonedBuilding.eventClicked += SaveOperationHours;

                m_workAtNightZonedBuilding.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_workAtWeekandsZonedBuilding.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_hasExtendedWorkShiftZonedBuilding.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_hasContinuousWorkShiftZonedBuilding.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
            }
        }
        private static void UpdateSliderZonedBuilding()
        {
            if (m_hasContinuousWorkShiftZonedBuilding.isChecked)
            {
                m_workShiftsZonedBuilding.maxValue = m_workAtNightZonedBuilding.isChecked ? 2 : 1;
                m_workShiftsZonedBuilding.value = 1;
            }
            else
            {
                m_workShiftsZonedBuilding.maxValue = m_workAtNightZonedBuilding.isChecked ? 3 : 2;
                m_workShiftsZonedBuilding.value = 1;
            }
        }

        public static void RefreshDataZonedBuilding()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            var buildingAI = building.Info.GetAI();
            if(buildingAI is CommercialBuildingAI || buildingAI is IndustrialBuildingAI)
            {
                if (BuildingWorkTimeManager.BuildingsWorkTime.TryGetValue(buildingID, out var buildingWorkTime))
                {
                    m_workAtNightZonedBuilding.isChecked = buildingWorkTime.WorkAtNight;
                    m_workAtWeekandsZonedBuilding.isChecked = buildingWorkTime.WorkAtWeekands;
                    m_hasExtendedWorkShiftZonedBuilding.isChecked = buildingWorkTime.HasExtendedWorkShift;
                    m_hasContinuousWorkShiftZonedBuilding.isChecked = buildingWorkTime.HasContinuousWorkShift;
                    m_workShiftsZonedBuilding.value = buildingWorkTime.WorkShifts;
                }
                m_settingsCheckBoxZonedBuilding.Show();
                m_settingsCheckBoxZonedBuilding.relativePosition = new Vector3(350f, 6f);
                m_workShiftsZonedBuilding.relativePosition = new Vector3(25f, 48f);
                m_workShiftsCountZonedBuilding.relativePosition = new Vector3(150f, 44f);

                if (m_settingsCheckBoxZonedBuilding.isChecked)
                {
                    m_uiMainPanelZonedBuilding.Show();
                }
            }
            else
            {
                m_settingsCheckBoxZonedBuilding.Hide();
                m_uiMainPanelZonedBuilding.Hide();
            }
        }

        private static void SaveOperationHours(UIComponent c, UIMouseEventParameter eventParameter) => SaveSettings();

        private static void SaveSettings()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkAtNight = m_workAtNightZonedBuilding.isChecked;
            buildingWorkTime.WorkAtWeekands = m_workAtWeekandsZonedBuilding.isChecked;
            buildingWorkTime.HasExtendedWorkShift = m_hasExtendedWorkShiftZonedBuilding.isChecked;
            buildingWorkTime.HasContinuousWorkShift = m_hasContinuousWorkShiftZonedBuilding.isChecked;
            buildingWorkTime.WorkShifts = (int)m_workShiftsZonedBuilding.value;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);

            RefreshDataZonedBuilding();
        }

        private static void LoadSettings(ushort buildingID)
        {
            if (BuildingWorkTimeManager.BuildingsWorkTime.TryGetValue(buildingID, out var buildingWorkTime))
            {
                m_workAtNightZonedBuilding.isChecked = buildingWorkTime.WorkAtNight;
                m_workAtWeekandsZonedBuilding.isChecked = buildingWorkTime.WorkAtWeekands;
                m_hasExtendedWorkShiftZonedBuilding.isChecked = buildingWorkTime.HasExtendedWorkShift;
                m_hasContinuousWorkShiftZonedBuilding.isChecked = buildingWorkTime.HasContinuousWorkShift;
                m_workShiftsZonedBuilding.value = buildingWorkTime.WorkShifts;
            }

        }
    }

}

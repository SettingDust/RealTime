namespace RealTime.UI
{
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.CustomAI;
    using UnityEngine;

    public static class ZonedBuildingOperationHoursUIPanel
    {
        public static UIPanel m_uiMainPanel;

        private static UIPanel m_InnerPanel;
        private static ZonedBuildingWorldInfoPanel m_zonedBuildingWorldInfoPanel;

        private static UILabel m_settingsTitle;
        private static UICheckBox m_operationHoursSettingsCheckBox;

        private static UICheckBox m_workAtNight;
        private static UICheckBox m_workAtWeekands;
        private static UICheckBox m_hasExtendedWorkShift;
        private static UICheckBox m_hasContinuousWorkShift;

        private static UILabel m_workShiftsLabel;
        private static UISlider m_workShifts;
        private static UILabel m_workShiftsCount;

        private static UIButton SaveOperationHoursBtn;

        public static void Init() => CreateUI();

        private static void CreateUI()
        {
            m_zonedBuildingWorldInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();
            var makeHistoricalPanel = m_zonedBuildingWorldInfoPanel.Find("MakeHistoricalPanel").GetComponent<UIPanel>();
            if (makeHistoricalPanel != null)
            {
                m_uiMainPanel = m_zonedBuildingWorldInfoPanel.component.AddUIComponent<UIPanel>();
                m_uiMainPanel.name = "OperationHoursUIPanel";
                m_uiMainPanel.backgroundSprite = "SubcategoriesPanel";
                m_uiMainPanel.opacity = 0.90f;
                m_uiMainPanel.isVisible = false;
                m_uiMainPanel.relativePosition = new Vector3(m_uiMainPanel.parent.width + 1, 40f);
                m_uiMainPanel.height = 370f;
                m_uiMainPanel.width = 310f;

                m_operationHoursSettingsCheckBox = UiUtils.CreateCheckBox(makeHistoricalPanel, "OperationHoursSettingsCheckBox", "settings", false);
                m_operationHoursSettingsCheckBox.width = 110f;
                m_operationHoursSettingsCheckBox.label.textColor = new Color32(185, 221, 254, 255);
                m_operationHoursSettingsCheckBox.label.textScale = 0.8125f;
                m_operationHoursSettingsCheckBox.tooltip = "change building operation hours.";
                m_operationHoursSettingsCheckBox.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_operationHoursSettingsCheckBox.relativePosition = new Vector3(350f, 6f);
                m_operationHoursSettingsCheckBox.eventCheckChanged += (component, value) =>
                {
                    m_uiMainPanel.isVisible = value;
                    if (m_uiMainPanel.isVisible)
                    {
                        m_uiMainPanel.height = m_uiMainPanel.parent.height - 7f;
                    }
                };
                makeHistoricalPanel.AttachUIComponent(m_operationHoursSettingsCheckBox.gameObject);

                m_settingsTitle = UiUtils.CreateLabel(m_uiMainPanel, "SettingsTitle", "Adjust Operation Hours", "");
                m_settingsTitle.font = UiUtils.GetUIFont("OpenSans-Regular");
                m_settingsTitle.textAlignment = UIHorizontalAlignment.Center;
                m_settingsTitle.textColor = new Color32(78, 184, 126, 255);
                m_settingsTitle.relativePosition = new Vector3(45f, 20f);
                m_settingsTitle.textScale = 1.2f;

                m_workAtNight = UiUtils.CreateCheckBox(m_uiMainPanel, "WorkAtNight", "Work At Night", false);
                m_workAtNight.width = 110f;
                m_workAtNight.label.textColor = new Color32(185, 221, 254, 255);
                m_workAtNight.label.textScale = 0.8125f;
                m_workAtNight.tooltip = "choose if the building will work at night.";
                m_workAtNight.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_workAtNight.relativePosition = new Vector3(30f, 60f);
                m_workAtNight.eventCheckChanged += (component, value) =>
                {
                    m_workAtNight.isChecked = value;
                    UpdateSlider();
                };
                m_uiMainPanel.AttachUIComponent(m_workAtNight.gameObject);

                m_workAtWeekands = UiUtils.CreateCheckBox(m_uiMainPanel, "WorkAtWeekands", "Work At Weekands", false);
                m_workAtWeekands.width = 110f;
                m_workAtWeekands.label.textColor = new Color32(185, 221, 254, 255);
                m_workAtWeekands.label.textScale = 0.8125f;
                m_workAtWeekands.tooltip = "choose if the building will work at weekends.";
                m_workAtWeekands.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_workAtWeekands.relativePosition = new Vector3(30f, 100f);
                m_workAtWeekands.eventCheckChanged += (component, value) => m_workAtWeekands.isChecked = value;
                m_uiMainPanel.AttachUIComponent(m_workAtWeekands.gameObject);

                m_hasExtendedWorkShift = UiUtils.CreateCheckBox(m_uiMainPanel, "HasExtendedWorkShift", "Has Extended Work Shift", false);
                m_hasExtendedWorkShift.width = 110f;
                m_hasExtendedWorkShift.label.textColor = new Color32(185, 221, 254, 255);
                m_hasExtendedWorkShift.label.textScale = 0.8125f;
                m_hasExtendedWorkShift.tooltip = "choose if the building will have an extended work shift.";
                m_hasExtendedWorkShift.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_hasExtendedWorkShift.relativePosition = new Vector3(30f, 140f);
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

                m_hasContinuousWorkShift = UiUtils.CreateCheckBox(m_uiMainPanel, "HasContinuousWorkShift", "Has Continuous Work Shift", false);
                m_hasContinuousWorkShift.width = 110f;
                m_hasContinuousWorkShift.label.textColor = new Color32(185, 221, 254, 255);
                m_hasContinuousWorkShift.label.textScale = 0.8125f;
                m_hasContinuousWorkShift.tooltip = "choose if the building will have a continuous work shift.";
                m_hasContinuousWorkShift.AlignTo(m_zonedBuildingWorldInfoPanel.component, UIAlignAnchor.TopLeft);
                m_hasContinuousWorkShift.relativePosition = new Vector3(30f, 180f);
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
                m_InnerPanel.size = new Vector2(220f, 66f);
                m_InnerPanel.relativePosition = new Vector3(30f, 210f);

                m_workShiftsLabel = UiUtils.CreateLabel(m_uiMainPanel, "OperationHoursInnerTitle", "Select number of shifts", "");
                m_workShiftsLabel.font = UiUtils.GetUIFont("OpenSans-Regular");
                m_workShiftsLabel.textAlignment = UIHorizontalAlignment.Center;
                m_workShiftsLabel.relativePosition = new Vector3(10f, 10f);
                m_InnerPanel.AttachUIComponent(m_workShiftsLabel.gameObject);

                m_workShifts = UiUtils.CreateSlider(m_InnerPanel, "ShiftCount", 1, 3, 1, 1);
                m_workShifts.tooltip = "Select how many work shifts the building should have";
                m_workShifts.size = new Vector2(130f, 8f);
                m_workShifts.relativePosition = new Vector3(25f, 48f);
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

                SaveOperationHoursBtn = UiUtils.AddButton(m_uiMainPanel, 25f, 290f, "SaveOperationHours", "Save Operation Hours", "save building working hours");
                SaveOperationHoursBtn.eventClicked += SaveOperationHours;
            }
        }

        private static void UpdateSlider()
        {
            if (m_hasContinuousWorkShift.isChecked)
            {
                m_workShifts.maxValue = m_workAtNight.isChecked ? 2 : 1;
                m_workShifts.value = 1;
            }
            else
            {
                m_workShifts.maxValue = m_workAtNight.isChecked ? 3 : 2;
                m_workShifts.value = 1;
            }
        }

        public static void RefreshData()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            var buildingAI = building.Info.GetAI();
            if(buildingAI is CommercialBuildingAI || buildingAI is IndustrialBuildingAI)
            {
                if (BuildingWorkTimeManager.BuildingsWorkTime.TryGetValue(buildingID, out var buildingWorkTime))
                {
                    m_workAtNight.isChecked = buildingWorkTime.WorkAtNight;
                    m_workAtWeekands.isChecked = buildingWorkTime.WorkAtWeekands;
                    m_hasExtendedWorkShift.isChecked = buildingWorkTime.HasExtendedWorkShift;
                    m_hasContinuousWorkShift.isChecked = buildingWorkTime.HasContinuousWorkShift;
                    m_workShifts.value = buildingWorkTime.WorkShifts;
                }
                m_operationHoursSettingsCheckBox.Show();
                m_operationHoursSettingsCheckBox.relativePosition = new Vector3(350f, 6f);
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

        private static void SaveOperationHours(UIComponent c, UIMouseEventParameter eventParameter) => SaveSettings();

        private static void SaveSettings()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkAtNight = m_workAtNight.isChecked;
            buildingWorkTime.WorkAtWeekands = m_workAtWeekands.isChecked;
            buildingWorkTime.HasExtendedWorkShift = m_hasExtendedWorkShift.isChecked;
            buildingWorkTime.HasContinuousWorkShift = m_hasContinuousWorkShift.isChecked;
            buildingWorkTime.WorkShifts = (int)m_workShifts.value;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);

            RefreshData();
        }

    }

}

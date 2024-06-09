namespace RealTime.UI
{
    using ColossalFramework;
    using ColossalFramework.UI;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    internal class NewBuildingWorldInfoPanel
    {
        protected static InstanceID? lastInstanceID = null;
        protected static bool translationSetUp = false;
        protected static float originalNameWidth = -1;
        protected static UITextField m_NameField = null;
        protected static UserEventCreationWindow eventCreationWindow = null;

        public static void AddEventUI(CityServiceWorldInfoPanel cityServicePanel)
        {
            UIMultiStateButton locationButton = null;
            UIButton createEventButton = null;
            UIFastList eventSelection = null;
            UserEventCreationWindow eventCreationWindow = null;

            try
            {
                locationButton = cityServicePanel.Find<UIMultiStateButton>("LocationMarker");
            }
            catch { }
            try
            {
                createEventButton = cityServicePanel.Find<UIButton>("CreateEventButton");
            }
            catch { }
            try
            {
                eventSelection = cityServicePanel.Find<UIFastList>("EventSelectionList");
            }
            catch { }
            try
            {
                eventCreationWindow = cityServicePanel.Find<UserEventCreationWindow>("EventCreator");
            }
            catch { }

            var m_InstanceIDInfo = typeof(CityServiceWorldInfoPanel).GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance);
            var m_InstanceID = m_InstanceIDInfo.GetValue(cityServicePanel) as InstanceID?;

            lastInstanceID = m_InstanceID;

            BuildCreationWindow(cityServicePanel.component);

            if (eventSelection != null)
            {
                eventSelection.Hide();
            }

            if (eventCreationWindow != null)
            {
                eventCreationWindow.Hide();
            }

            if (createEventButton == null && locationButton != null)
            {
                createEventButton = cityServicePanel.component.AddUIComponent<UIButton>();
                createEventButton.name = "CreateEventButton";
                createEventButton.atlas = CimTools.CimToolsHandler.CimToolBase.SpriteUtilities.GetAtlas("Ingame");
                createEventButton.normalFgSprite = "InfoIconLevel";
                createEventButton.disabledFgSprite = "InfoIconLevelDisabled";
                createEventButton.focusedFgSprite = "InfoIconLevelFocused";
                createEventButton.hoveredFgSprite = "InfoIconLevelHovered";
                createEventButton.pressedFgSprite = "InfoIconLevelPressed";
                createEventButton.width = locationButton.width;
                createEventButton.height = locationButton.height;
                createEventButton.position = locationButton.position - new Vector3(createEventButton.width - 5f, 0);
                createEventButton.eventClicked += CreateEventButton_eventClicked;

                BuildDropdownList(createEventButton);
            }

            if (m_InstanceID != null)
            {
                var _buildingManager = Singleton<BuildingManager>.instance;
                Building _currentBuilding = _buildingManager.m_buildings.m_buffer[lastInstanceID.Value.Building];

                if (CityEventBuildings.instance.BuildingHasUserEvents(ref _currentBuilding))
                {
                    createEventButton.Show();
                    createEventButton.Enable();
                    m_NameField.width = originalNameWidth - 45f;
                }
                else
                {
                    if (CityEventBuildings.instance.BuildingHasEvents(ref _currentBuilding))
                    {
                        createEventButton.Show();
                        createEventButton.Disable();
                        m_NameField.width = originalNameWidth - 45f;
                    }
                    else
                    {
                        createEventButton.Hide();
                        m_NameField.width = originalNameWidth;
                    }
                }
            }
        }

        private static void BuildCreationWindow(UIComponent parent)
        {
            if (eventCreationWindow == null)
            {
                eventCreationWindow = parent.AddUIComponent<UserEventCreationWindow>();
                eventCreationWindow.name = "EventCreator";
                Debug.Log("Creating a new UserEventCreationWindow");
                eventCreationWindow.Hide();
            }
        }

        private static void CreateEventButton_eventClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            UIFastList eventSelection = component.parent.Find<UIFastList>("EventSelectionList");
            ushort buildingID = lastInstanceID.Value.Building;

            if (lastInstanceID != null && buildingID != 0)
            {
                var _buildingManager = Singleton<BuildingManager>.instance;
                Building _currentBuilding = _buildingManager.m_buildings.m_buffer[buildingID];

                if ((_currentBuilding.m_flags & Building.Flags.Active) != Building.Flags.None)
                {
                    List<CityEvent> userEvents = CityEventBuildings.instance.GetUserEventsForBuilding(ref _currentBuilding);

                    BuildDropdownList(component);

                    if (eventSelection.isVisible)
                    {
                        eventSelection.Hide();
                    }
                    else
                    {
                        eventSelection.selectedIndex = -1;
                        eventSelection.Show();
                        eventSelection.rowsData.Clear();

                        foreach (CityEvent userEvent in userEvents)
                        {
                            XmlEvent xmlUserEvent = userEvent as XmlEvent;

                            if (xmlUserEvent != null)
                            {
                                xmlUserEvent.SetUp(ref buildingID);
                                LabelOptionItem eventToInsert = new LabelOptionItem() { linkedEvent = xmlUserEvent, readableLabel = xmlUserEvent.GetReadableName() };
                                eventSelection.rowsData.Add(eventToInsert);

                                LoggingWrapper.Log(xmlUserEvent.GetReadableName());
                            }
                        }

                        eventSelection.DisplayAt(0);
                    }
                }
            }
        }

        private static void BuildDropdownList(UIComponent component)
        {
            UIFastList eventSelection = component.parent.Find<UIFastList>("EventSelectionList");

            if (eventSelection == null)
            {
                eventSelection = UIFastList.Create<UIFastListLabel>(component.parent);
                eventSelection.name = "EventSelectionList";
                eventSelection.backgroundSprite = "UnlockingPanel";
                eventSelection.size = new Vector2(120, 60);
                eventSelection.canSelect = true;
                eventSelection.relativePosition = component.relativePosition + new Vector3(0, component.height);
                eventSelection.rowHeight = 20f;
                eventSelection.selectedIndex = -1;
                eventSelection.eventClicked += EventSelection_eventClicked;
                eventSelection.eventSelectedIndexChanged += EventSelection_eventSelectedIndexChanged;
                eventSelection.Hide();
            }
        }

        private static void UpdateEventSelection(UIComponent component)
        {
            UIFastList list = component as UIFastList;

            if (list != null)
            {
                LabelOptionItem selectedOption = list.selectedItem as LabelOptionItem;

                if (selectedOption != null && eventCreationWindow != null)
                {
                    eventCreationWindow.Show();
                    eventCreationWindow.SetUp(selectedOption, lastInstanceID.Value.Building);
                    eventCreationWindow.relativePosition = list.relativePosition + new Vector3(-(list.width / 2f), list.height);

                    LoggingWrapper.Log("Selected " + list.selectedIndex);
                }
                else
                {
                    LoggingWrapper.LogError("Couldn't find the option that has been selected for an event!");
                }
            }
            else
            {
                LoggingWrapper.LogError("Couldn't find the list that the selection was made on!");
            }
        }

        private static void EventSelection_eventClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            LoggingWrapper.Log("Clicked");
            UpdateEventSelection(component);
        }

        private static void EventSelection_eventSelectedIndexChanged(UIComponent component, int value)
        {
            LoggingWrapper.Log("IndexChanged");
            UpdateEventSelection(component);
        }
    }
}

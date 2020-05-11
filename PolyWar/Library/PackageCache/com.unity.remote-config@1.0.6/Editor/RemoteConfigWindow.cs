using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.RemoteConfig.Editor
{
    internal class RemoteConfigWindow : EditorWindow
    {
        //Window state
        [NonSerialized] bool m_Initialized;
        RulesTreeView m_RulesTreeView;
        [SerializeField] TreeViewState m_RulesTreeViewState;
        [SerializeField] MultiColumnHeaderState m_RulesMultiColumnHeaderState;
        SettingsTreeView m_SettingsTreeView;
        [SerializeField] TreeViewState m_SettingsTreeViewState;
        [SerializeField] MultiColumnHeaderState m_SettingsMultiColumnHeaderState;

        RulesMultiColumnHeader m_RulesMultiColumnHeader;
        SettingsMultiColumnHeader m_RettingsMultiColumnHeader;

        string m_SelectedRuleId = k_DefaultRuleId;

        static string m_SettingsDropdownSelectedKey;
        
        RemoteConfigWindowController m_Controller;
        
        //GUI Content
        GUIContent m_pullRulesButtonContent = new GUIContent("Pull");
        GUIContent m_pushRulesButtonContent = new GUIContent("Push");
        GUIContent m_createRuleButtonContent = new GUIContent("Add Rule");
        GUIContent m_loadingMessage = new GUIContent("Loading, please wait.");
        GUIContent m_EnvironmentsLabelContent = new GUIContent("Environment");
        GUIContent m_CreateSettingButtonContent = new GUIContent("Add Setting");
        GUIContent m_AnalyticsNotEnabledContent = new GUIContent("To get started with Unity Remote Config, you must first link your project to a Unity Cloud Project ID.\n\nA Unity Cloud Project ID is an online identifier which is used across all Unity Services. These can be created within the Services window itself, or online on the Unity Services website.\n\nThe simplest way is to use the Services window within Unity, as follows:\nTo open the Services Window, go to Window > General > Services.\n\nNote: using Unity Remote Config does not require that you turn on any additional, individual cloud services like Analytics, Ads, Cloud Build, etc.");


        //UI Style variables
        const float k_LineHeight = 22f;
        const float k_LineHeightBuffer = k_LineHeight - 2;
        
        const string k_DefaultRuleId = "defaultRule";
        private const string utcDateFormat = "YYYY-MM-DDThh:mm:ssZ";
        const string m_NoSettingsContent = "To get started, please add a setting";
        const string m_NoSettingsForTheRuleContent = "Please add at least one setting to your rule";
        private GUIStyle guiStyleLabel = new GUIStyle();
        private GUIStyle guiStyleSubLabel = new GUIStyle();

        public static string defaultRuleId
        {
            get { return k_DefaultRuleId; }
        }

        public static string settingsDropdownSelectedKey
        {
            get { return m_SettingsDropdownSelectedKey; }
            set { m_SettingsDropdownSelectedKey = value; }
        }

        Rect ruleTableRect
        {
            get {
                return new Rect(0, m_RulesTreeView.multiColumnHeader.height, position.width * .3f, (position.height - (k_LineHeight * 2.25f))); 
            }
        }

        Rect GetRuleSettingsRect(float currentY)
        {
            return new Rect(position.width * .3f, currentY, position.width * .7f, (position.height - currentY));
        }
        
        Rect GetRuleSettingsTableRect(Rect ruleSettingsRect)
        {
            return new Rect(ruleSettingsRect.x, ruleSettingsRect.y + k_LineHeight, ruleSettingsRect.width, ruleSettingsRect.height - (k_LineHeight * 2f));
        }
        
        Rect rulesMultiColumnTreeViewRect
        {
            get { return new Rect(20, 30, 500, position.height-60); }
        }

        Rect rsTableRect
        {
            get { return new Rect(position.width * .3f, m_RulesTreeView.multiColumnHeader.height, position.width * .7f, position.height - (k_LineHeight * 2.25f)); }
        }

        [MenuItem("Window/Remote Config")]
        public static void GetWindow()
        {
            var RSWindow = GetWindow<RemoteConfigWindow>();
            RSWindow.titleContent = new GUIContent("Remote Config");
            RSWindow.minSize = new Vector2(425, 300);
            RSWindow.Focus();
            RSWindow.Repaint();
        }
        
        private void OnEnable()
        {
            if (AreServicesEnabled())
            {
                InitIfNeeded();
            }
        }

        private void OnDisable()
        {
            try
            {
                m_RulesTreeView.DeleteRule -= OnDeleteRule;
                m_RulesTreeView.RuleEnabledOrDisabled -= OnRuleEnabledOrDisabled;
                m_RulesTreeView.RuleAttributesChanged -= OnRuleAttributesChanged;

                m_SettingsTreeView.UpdateSetting -= OnUpdateSetting;
                m_SettingsTreeView.DeleteSetting -= OnDeleteSetting;
                m_SettingsTreeView.SetActiveOnSettingChanged -= OnAddSettingToRule;

                m_Controller.rulesDataStoreChanged -= OnRulesDataStoreChanged;
                m_Controller.remoteSettingsStoreChanged -= OnRemoteSettingsStoreChanged;
                m_Controller.environmentChanged -= OnEnvironmentChanged;
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (NullReferenceException e)
#pragma warning restore CS0168 // Variable is declared but never used
            { }
        }

        private void OnGUI()
        {
            if (AreServicesEnabled(true))
            {
                InitIfNeeded();
                
                EditorGUI.BeginDisabledGroup(IsLoading());
                float currentY = 2f;
                DrawEnvironmentDropdown(currentY);
                DrawPushPullButtons(currentY);
                currentY += k_LineHeight;

                Rect rulesTreeViewRect = ruleTableRect;
                m_RulesTreeView.OnGUI(rulesTreeViewRect);
                DrawPaneSeparator(rulesTreeViewRect);

                if(!IsLoading())
				{
					if (GUI.Button(new Rect(2f, ruleTableRect.height + k_LineHeight + 4f, ruleTableRect.width - 5f, k_LineHeight),
					m_createRuleButtonContent))
					{
						m_Controller.AddDefaultRule();
						m_SelectedRuleId = m_Controller.GetRulesList().Last().id;
						m_RulesTreeView.SetSelection(m_SelectedRuleId);

						//TODO: move this logic elsewhere
						m_SettingsTreeView.settingsList = m_Controller.GetSettingsList();
						m_SettingsTreeView.activeSettingsList = m_Controller.GetRuleById(m_SelectedRuleId).value;
						m_SettingsTreeView.Reload();
					}
				}

                if (m_SelectedRuleId == k_DefaultRuleId)
                {
                    DrawRemoteSettingsPane();
                }
                else
                {
                    if (m_Controller.HasRules())
                    {
                        var currentRule = m_Controller.GetRuleById(m_SelectedRuleId);
                        EditorGUI.BeginDisabledGroup(currentRule.enabled);
                        currentY = DrawConfigurationsPane(currentRule);
                        DrawRuleSettingsRect(currentY);
                    }
                }

                EditorGUI.EndDisabledGroup();
                AddFooterButtons();
            }
        }

        private void InitIfNeeded()
        {
            if (!m_Initialized)
            {
                m_Controller = new RemoteConfigWindowController();

                if (m_RulesTreeViewState == null)
                {
                    m_RulesTreeViewState = new TreeViewState();
                }

                bool firstInit = m_RulesMultiColumnHeaderState == null;
                var headerState = CreateRulesMultiColumnHeaderState(rulesMultiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_RulesMultiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_RulesMultiColumnHeaderState, headerState);
                m_RulesMultiColumnHeaderState = headerState;
                
                foreach(MultiColumnHeaderState.Column column in m_RulesMultiColumnHeaderState.columns)
                {
                    column.autoResize = true;
                }
                
                m_RulesMultiColumnHeader = new RulesMultiColumnHeader(headerState);
                if (firstInit)
                {
                    m_RulesMultiColumnHeader.ResizeToFit();
                }
                m_RulesTreeView = new RulesTreeView(m_RulesTreeViewState, m_RulesMultiColumnHeader, m_Controller.GetRulesList());

                if (m_SettingsTreeViewState == null)
                {
                    m_SettingsTreeViewState = new TreeViewState();
                }
                
                firstInit = m_SettingsMultiColumnHeaderState == null;
                //var ruleSettingsTableRect = GetRuleSettingsTableRect(
                headerState = CreateSettingsMultiColumnHeaderState(position.width * .7f);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_SettingsMultiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_SettingsMultiColumnHeaderState, headerState);
                m_SettingsMultiColumnHeaderState = headerState;
                
                foreach(MultiColumnHeaderState.Column column in m_SettingsMultiColumnHeaderState.columns)
                {
                    column.autoResize = true;
                }
                
                m_RettingsMultiColumnHeader = new SettingsMultiColumnHeader(headerState);
                if (firstInit)
                {
                    m_RettingsMultiColumnHeader.ResizeToFit();
                }
                m_SettingsTreeView = new SettingsTreeView(m_SettingsTreeViewState, m_RettingsMultiColumnHeader, m_Controller.GetSettingsList(), m_Controller.GetRulesList());

                m_RulesTreeView.SelectionChangedEvent += selectedRuleId =>
                {
                    this.m_SelectedRuleId = selectedRuleId;
                    if (this.m_SelectedRuleId == k_DefaultRuleId)
                    {
                        m_SettingsTreeView.settingsList = m_Controller.GetSettingsList();
                        m_SettingsTreeView.activeSettingsList = m_Controller.GetSettingsList();
                    }
                    else
                    {
                        m_SettingsTreeView.settingsList = m_Controller.GetSettingsList();
                        m_SettingsTreeView.activeSettingsList = m_Controller.GetRuleById(selectedRuleId).value;
                    }
                    m_SettingsTreeView.Reload();
                    m_SettingsDropdownSelectedKey = "";
                };
                m_RulesTreeView.DeleteRule += OnDeleteRule;
                m_RulesTreeView.RuleEnabledOrDisabled += OnRuleEnabledOrDisabled;
                m_RulesTreeView.RuleAttributesChanged += OnRuleAttributesChanged;

                m_SettingsTreeView.UpdateSetting += OnUpdateSetting;
                m_SettingsTreeView.DeleteSetting += OnDeleteSetting;
                m_SettingsTreeView.SetActiveOnSettingChanged += OnAddSettingToRule;
                
                m_Controller.rulesDataStoreChanged += OnRulesDataStoreChanged;
                m_Controller.remoteSettingsStoreChanged += OnRemoteSettingsStoreChanged;
                m_Controller.environmentChanged += OnEnvironmentChanged;

                m_SelectedRuleId = k_DefaultRuleId;
                m_RulesTreeView.SetSelection(k_DefaultRuleId);

                m_Initialized = true;
            }
        }

        private bool AreServicesEnabled(bool calledFromOnGui = false)
        {
            if (string.IsNullOrEmpty(CloudProjectSettings.projectId) || string.IsNullOrEmpty(CloudProjectSettings.organizationId))
            {
                if(calledFromOnGui)
                {
                    GUIStyle style = GUI.skin.label;
                    style.wordWrap = true;
                    EditorGUILayout.LabelField(m_AnalyticsNotEnabledContent, style);
                }
                return false;
            }
            return true;
        }

        private void OnEnvironmentChanged()
        {
            m_SelectedRuleId = k_DefaultRuleId;
            m_RulesTreeView.SetSelection(m_SelectedRuleId);
        }

        private void OnDeleteSetting(string settingKey)
        {
            if (m_SelectedRuleId == k_DefaultRuleId)
            {
                m_Controller.DeleteRemoteSetting(settingKey);
            }
            else
            {
                m_Controller.DeleteSettingFromRule(m_SelectedRuleId, settingKey);
            }
        }

        private void OnUpdateSetting(string oldItemKey, RemoteSettingsKeyValueType newitem)
        {
            if (m_SelectedRuleId == k_DefaultRuleId)
            {
                m_Controller.UpdateRemoteSetting(oldItemKey, newitem);
            }
            else
            {
                m_Controller.UpdateSettingForRule(m_SelectedRuleId, newitem);
            }
        }

        private void AddFooterButtons()
        {
            if (IsLoading())
            {
                GUI.Label(new Rect(0, position.height - k_LineHeight, position.width, k_LineHeight), m_loadingMessage);
            }
        }

        private bool IsLoading()
        {
            bool isLoading = m_Controller.isLoading;
            m_SettingsTreeView.isLoading = isLoading;
            return isLoading;
        }

        private void OnDeleteRule(string ruleId)
        {
            m_SelectedRuleId = k_DefaultRuleId;
            m_Controller.DeleteRule(ruleId);
            m_RulesTreeView.SetSelection(m_SelectedRuleId);
        }

        private void OnRuleAttributesChanged(string ruleId, Rule newRule)
        {
            m_Controller.UpdateRuleAttributes(ruleId, newRule);
        }

        private void OnRuleEnabledOrDisabled(string ruleId, bool enabled)
        {
            m_Controller.EnableOrDisableRule(ruleId, enabled);
        }

        private void OnRulesDataStoreChanged()
        {
            m_RulesTreeView.rulesList = m_Controller.GetRulesList();
            m_RulesTreeView.Reload();
        }
        
        private void OnRemoteSettingsStoreChanged()
        {
            if (m_SelectedRuleId == k_DefaultRuleId)
            {
                m_SettingsTreeView.settingsList = m_Controller.GetSettingsList();
                m_SettingsTreeView.activeSettingsList = m_Controller.GetSettingsList();
            }
            else
            {
                m_SettingsTreeView.settingsList = m_Controller.GetSettingsList();
                m_SettingsTreeView.activeSettingsList = m_Controller.GetRuleById(m_SelectedRuleId).value;
            }
            m_SettingsTreeView.Reload();
        }

        private void DrawEnvironmentDropdown(float currentY)
        {
            var totalWidth = position.width / 2;
            EditorGUI.BeginDisabledGroup(m_Controller.GetEnvironmentsCount() <= 1 || IsLoading());
            GUI.Label(new Rect(0, currentY, totalWidth / 2, 20), m_EnvironmentsLabelContent);
            GUIContent ddBtnContent = new GUIContent(m_Controller.GetCurrentEnvironmentName());
            Rect ddRect = new Rect(totalWidth / 2, currentY, totalWidth / 2, 20);
            if (GUI.Button(ddRect, ddBtnContent, EditorStyles.popup))
            {
                m_Controller.BuildPopupListForEnvironments().DropDown(ddRect);
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPushPullButtons(float currentY)
        {
            //var boundingRect = new Rect(, currentY, position.width / 2, 20);
            float boundingBoxPadding = 5f;
            var paddedRect = new Rect((position.width / 2) + boundingBoxPadding, currentY,(position.width / 2) - (2 * boundingBoxPadding), 20);
            var buttonWidth = (paddedRect.width / 2);
            if (GUI.Button(new Rect(paddedRect.x, paddedRect.y, buttonWidth, 20),
                m_pushRulesButtonContent))
            {
                m_Controller.Push();
                m_SelectedRuleId = k_DefaultRuleId;
                m_RulesTreeView.SetSelection(m_SelectedRuleId);
            }
            if (GUI.Button(new Rect(paddedRect.x + buttonWidth + boundingBoxPadding, paddedRect.y, buttonWidth, 20),
                    m_pullRulesButtonContent))
            {
                m_Controller.Fetch();
                m_SelectedRuleId = k_DefaultRuleId;
                m_RulesTreeView.SetSelection(m_SelectedRuleId);
            }
        }

        private void DrawRemoteSettingsPane()
        {
            var remoteSettingsPaneRect = rsTableRect;
            m_SettingsTreeView.enableEditingSettingsKeys = true;
            m_SettingsTreeView.rulesList = m_Controller.GetRulesList();
            m_SettingsTreeView.OnGUI(remoteSettingsPaneRect);

            if (!m_Controller.GetSettingsList().Any())
            {
                var messageRect = new Rect(remoteSettingsPaneRect.x + 1f,remoteSettingsPaneRect.y + k_LineHeight + 6f, 400f, k_LineHeight);
                showMessage(messageRect, m_NoSettingsContent);
            }

            AddRemoteSettingsPaneFooterButtons(remoteSettingsPaneRect);
        }

        private void AddRemoteSettingsPaneFooterButtons(Rect remoteSettingsPaneRect)
        {
            var buttonWidth = remoteSettingsPaneRect.width;

            if (!IsLoading())
            {
                if (GUI.Button(new Rect(remoteSettingsPaneRect.x + 2.5f, remoteSettingsPaneRect.height + k_LineHeight + 4f, buttonWidth - 5f, k_LineHeight),
                    m_CreateSettingButtonContent))
                {
                    m_Controller.AddSetting();
                }
            }
            else
            {
                GUI.Label(new Rect(0, position.height - k_LineHeight, position.width, k_LineHeight), m_loadingMessage);
            }
        }
        
        private void DrawPaneSeparator(Rect rulesTreeViewRect)
        {
            EditorGUI.DrawRect(new Rect(rulesTreeViewRect.width - 1, k_LineHeight, 1, position.height - k_LineHeight), Color.black);
        }
        
        private float DrawConfigurationsPane(Rule rule)
        {
            var configPaneRect = new Rect(position.width * .3f, m_RulesTreeView.multiColumnHeader.height, position.width * .7f, position.height * .25f);
            var currentY = configPaneRect.y;

            var name = CreateLabelAndTextField("Name: ", rule.name, currentY, configPaneRect);
            currentY += k_LineHeight;

            var condition = CreateLabelAndTextField("Condition: ", rule.condition, currentY, configPaneRect);
            currentY += k_LineHeight;

            var rolloutPercentage = CreateLabelAndSlider("Rollout Percentage: ", rule.rolloutPercentage, 1.0F, 100.0F, currentY, configPaneRect);
            currentY += k_LineHeight;

            var startDate = CreateLabelWithSubLabelAndTextField("Start Date and Time: ", utcDateFormat, rule.startDate, currentY, configPaneRect);
            currentY += 1.4f*k_LineHeight;

            var endDate = CreateLabelWithSubLabelAndTextField("End Date and Time: ", utcDateFormat, rule.endDate, currentY, configPaneRect);
            currentY += 1.4f * k_LineHeight;

            if (name != rule.name || condition != rule.condition || rolloutPercentage != rule.rolloutPercentage || startDate != rule.startDate || endDate != rule.endDate)
            {
                var newRule = new Rule(rule.id, name, rule.enabled, rule.priority, condition, rolloutPercentage, rule.value, startDate, endDate);
                m_Controller.UpdateRuleAttributes(rule.id, newRule);
            }

            if (!m_Controller.GetSettingsListForRule(rule.id).Any())
            {
                var messageRect = new Rect(configPaneRect.x + 6f, currentY, configPaneRect.width - 12f, k_LineHeight);
                showMessage(messageRect, m_NoSettingsForTheRuleContent);
            }
            currentY += 1.4f*k_LineHeight;
            return currentY;
        }

        private void DrawRuleSettingsRect(float currentY)
        {
            var settingsRect = GetRuleSettingsRect(currentY);
            
            GUI.Label(new Rect(settingsRect.x, settingsRect.y, settingsRect.width, k_LineHeight), "Settings");
            
            m_SettingsTreeView.enableEditingSettingsKeys = false;
            m_SettingsTreeView.rulesList = m_Controller.GetRulesList();
            m_SettingsTreeView.OnGUI(GetRuleSettingsTableRect(settingsRect));
        }

        private void OnAddSettingToRule(string keyName, bool active)
        {
            if (active)
            {
                m_Controller.AddSettingToRule(m_SelectedRuleId, keyName);
            }
            else
            {
                m_Controller.DeleteSettingFromRule(m_SelectedRuleId, keyName);
            }
        }


        private string CreateLabelAndTextField(string labelText, string textFieldText, float currentY, Rect configPaneRect)
        {
            var labelX = configPaneRect.x + 5;
            var labelWidth = 125f;
            var textFieldX = labelX + labelWidth + 5;
            var textFieldWidth = configPaneRect.width - labelWidth - 15;
            
            GUI.Label(new Rect(labelX, currentY, labelWidth, k_LineHeightBuffer), labelText);
            return GUI.TextField(new Rect(textFieldX, currentY, textFieldWidth, k_LineHeightBuffer), textFieldText);
        }
        
        private string CreateLabelWithSubLabelAndTextField(string labelText, string subLabelText, string textFieldText, float currentY, Rect configPaneRect)
        {
            var labelX = configPaneRect.x + 5;
            var labelWidth = 125f;
            var textFieldX = labelX + labelWidth + 5;
            var textFieldWidth = configPaneRect.width - labelWidth - 15;
            var labelHeight = (k_LineHeightBuffer * 0.8f);
            var subLabelHeight = (k_LineHeightBuffer * 0.8f);
            var subLabelColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);

            guiStyleLabel = GUI.skin.label;
            guiStyleSubLabel.fontSize = 8;
            guiStyleSubLabel.normal.textColor = subLabelColor;

            GUI.Label(new Rect(labelX, currentY, labelWidth, labelHeight), labelText, guiStyleLabel);
            GUI.Label(new Rect(labelX, currentY+labelHeight, labelWidth, subLabelHeight), subLabelText, guiStyleSubLabel);
            return GUI.TextField(new Rect(textFieldX, currentY, textFieldWidth, k_LineHeightBuffer), textFieldText);
        }

        private int CreateLabelAndSlider(string labelText, float hSliderValue, float leftValue, float rightValue, float currentY, Rect configPaneRect)
        {
            var labelX = configPaneRect.x + 5;
            var labelWidth = 125f;
            var sliderFieldX = labelX + labelWidth + 5;
            var sliderFieldWidth = configPaneRect.width - 70 - labelWidth;
            var sliderValuePositionX = labelX + labelWidth + sliderFieldWidth + 30;
            hSliderValue = GUI.HorizontalSlider(new Rect(sliderFieldX, currentY, sliderFieldWidth, k_LineHeightBuffer), hSliderValue, leftValue, rightValue);

            GUI.Label(new Rect(labelX, currentY, labelWidth, k_LineHeightBuffer), labelText);
            GUI.Label(new Rect(sliderValuePositionX, currentY, 25, k_LineHeightBuffer), hSliderValue.ToString());
            return (int)(hSliderValue);
        }

        private void showMessage(Rect messageRect, string messageText)
        {
            EditorGUI.HelpBox(messageRect, messageText, MessageType.Warning);
        }

        public static MultiColumnHeaderState CreateRulesMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column 
                {
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    canSort = false,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 20, 
                    minWidth = 20,
                    maxWidth = 20,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Enabled"),
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 16,
                    minWidth = 16,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 28,
                    minWidth = 28,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Priority"),
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 16,
                    minWidth = 16,
                    autoResize = false,
                    allowToggleVisibility = false
                }
            };
            var state = new MultiColumnHeaderState(columns);
            return state;
        }
        
        public static MultiColumnHeaderState CreateSettingsMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 20,
                    minWidth = 20,
                    maxWidth = 20,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Key"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Value"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                }
            };
            var state = new MultiColumnHeaderState(columns);
            return state;
        }
    }
    
    // Displays all the rules
    internal class RulesTreeView : TreeView
    {
        public List<Rule> rulesList;

        public event Action<string> SelectionChangedEvent;
        public event Action<string> DeleteRule;
        public event Action<string, bool> RuleEnabledOrDisabled;
        public event Action<string, Rule> RuleAttributesChanged;

        public RulesTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, List<Rule> rulesList) : base(state, multiColumnHeader)
        {
            this.rulesList = rulesList;
            useScrollView = true;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem<Rule>(0, -1, "Root", new Rule());
            var id = 0;
            var allItems = new List<TreeViewItem>();
            if (rulesList != null)
            {
                Rule defaultRule = new Rule();
                defaultRule.priority = RemoteConfigDataManager.defaultRulePriority;
                defaultRule.id = RemoteConfigWindow.defaultRuleId;
                defaultRule.enabled = true;
                allItems.Add(new TreeViewItem<Rule>(id++, 0, "Settings Config", defaultRule));
                allItems.AddRange(rulesList.Select(x => new TreeViewItem<Rule>(id++, 0, x.name, x))
                    .ToList<TreeViewItem>());
            }
            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (TreeViewItem<Rule>) args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, TreeViewItem<Rule> item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case 0:
                    if(item.data.id != RemoteConfigWindow.defaultRuleId)
                    {
                        EditorGUI.BeginDisabledGroup(item.data.enabled);
                        if (GUI.Button(cellRect, "-"))
                        {
                            DeleteRule?.Invoke(item.data.id);
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    break;
                case 1:
                    if (item.data.id != RemoteConfigWindow.defaultRuleId)
                    {
                        var toggle = GUI.Toggle(cellRect, item.data.enabled, "");
                        if (toggle != item.data.enabled)
                        {
                            RuleEnabledOrDisabled?.Invoke(item.data.id, !item.data.enabled);
                        }
                    }
                    break;
                case 2:
                    GUI.Label(cellRect, item.displayName);
                    break;
                case 3:
                    if (item.data.id != RemoteConfigWindow.defaultRuleId)
                    {
                        EditorGUI.BeginDisabledGroup(item.data.enabled);
                        var newPriority = EditorGUI.IntField(cellRect, item.data.priority);
                        if (newPriority != item.data.priority)
                        {
                            var rule = item.data;
                            rule.priority = newPriority;
                            RuleAttributesChanged?.Invoke(item.data.id, rule);
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    break;
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            var treeViewItems = GetRows() as List<TreeViewItem>;
            var treeViewItem = treeViewItems.Find(x => x.id == selectedIds[0]) as TreeViewItem<Rule>;
            var ruleId = treeViewItem.data.id;
            if (SelectionChangedEvent != null)
            {
                SelectionChangedEvent(ruleId);
            }
        }

        public void SetSelection(string selectRuleId)
        {
            var treeViewItems = GetRows() as List<TreeViewItem>;
            var selections = new List<int>();
            foreach (TreeViewItem<Rule> treeViewitem in treeViewItems)
            {
                if (selectRuleId == treeViewitem.data.id)
                {
                    selections.Add(treeViewitem.id);
                }
            }
            SetSelection(selections, TreeViewSelectionOptions.FireSelectionChanged);

        }
    }

    internal class RulesMultiColumnHeader : MultiColumnHeader
    {
        public RulesMultiColumnHeader(MultiColumnHeaderState state) : base(state)
        {
            canSort = false;
        }
    }

    internal class SettingsTreeView : TreeView
    {
        public List<RemoteSettingsKeyValueType> settingsList;
        public List<RemoteSettingsKeyValueType> activeSettingsList;
        public List<Rule> rulesList;

        public event Action<string, RemoteSettingsKeyValueType> UpdateSetting;
        public event Action<string> DeleteSetting;
        public event Action<string, bool> SetActiveOnSettingChanged;

        public bool isLoading = false;

        public bool enableEditingSettingsKeys;

        private struct RSTypeChangedStruct
        {
            public RemoteSettingsKeyValueType rs;
            public string newType;
        }

        public SettingsTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, List<RemoteSettingsKeyValueType> settingsList,
            List<Rule> rulesList, bool enableEditingSettingsKeys = true) : base(state, multiColumnHeader)
        {
            this.rowHeight = 18f;
            this.settingsList = settingsList;
            this.rulesList = rulesList;
            this.enableEditingSettingsKeys = enableEditingSettingsKeys;
            useScrollView = true;
            Reload();
        }

        private bool isActiveSettingInSettingsList(List<TreeViewItem<RemoteSettingsKeyValueType>> settingsList, string settingKey)
        {
            foreach (var setting in settingsList)
            {
                if (setting.data.key == settingKey)
                {
                    return true;
                }
            }

            return false;
        }

        private List<TreeViewItem<RemoteSettingsKeyValueType>> AddDeletedSettings(
            List<TreeViewItem<RemoteSettingsKeyValueType>> tempItems, List<RemoteSettingsKeyValueType> activeSettings, int id)
        {
            foreach (var setting in activeSettings)
            {
                if (!isActiveSettingInSettingsList(tempItems, setting.key))
                {
                    tempItems.Add(new TreeViewItem<RemoteSettingsKeyValueType>(id++, 0, setting.key, setting, false));
                }
            }
            return tempItems;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem<RemoteSettingsKeyValueType>(0, -1, "Root", new RemoteSettingsKeyValueType());
            var id = 0;
            var allItems = new List<TreeViewItem>();
            if (settingsList != null && settingsList.Count > 0 && activeSettingsList != null)
            {
                var tempItems = settingsList
                    .Select(x => new TreeViewItem<RemoteSettingsKeyValueType>(id++, 0, x.key, x, false))
                    .ToList<TreeViewItem<RemoteSettingsKeyValueType>>();
                tempItems = AddDeletedSettings(tempItems, activeSettingsList, id);

                foreach(var activeRS in activeSettingsList)
                {
                    var item = tempItems.FirstOrDefault(x => x.data.key == activeRS.key);
                    if (default(TreeViewItem<RemoteSettingsKeyValueType>) == item)
                    {
                        continue;
                    }
                    item.data = activeRS;
                    item.enabled = true;
                }

                allItems = tempItems.ToList<TreeViewItem>();
            }

            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (TreeViewItem<RemoteSettingsKeyValueType>) args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem<RemoteSettingsKeyValueType> item, int column, ref RowGUIArgs args)
        {

            switch (column)
            {
                case 0:
                    CenterRectUsingSingleLineHeight(ref cellRect);
                    if (enableEditingSettingsKeys)
                    {
                        var isDisabled = enableEditingSettingsKeys &&
                                     IsKeyInRules(item.data.key, rulesList);
                        EditorGUI.BeginDisabledGroup(isDisabled || isLoading);
                        if (GUI.Button(cellRect,
                            new GUIContent("-", isDisabled ? "Can't remove a setting used in a rule" : "")))
                        {
                            DeleteSetting?.Invoke(item.data.key);
                        }

                        EditorGUI.EndDisabledGroup();
                        break;
                    }
                    else
                    {
                        var toggle = GUI.Toggle(cellRect, item.enabled, "");
                        if (toggle != item.enabled)
                        {
                            SetActiveOnSettingChanged?.Invoke(item.data.key, toggle);
                        }
                        break;
                    }

                case 1:
                    EditorGUI.BeginDisabledGroup(!enableEditingSettingsKeys || isLoading);
                    var newKey = GUI.TextField(cellRect, item.data.key);
                    EditorGUI.EndDisabledGroup();
                    if (newKey != item.data.key)
                    {
                        UpdateSetting?.Invoke(item.data.key,
                            new RemoteSettingsKeyValueType(newKey, item.data.type, item.data.value));
                    }

                    break;
                case 2:
                    CenterRectUsingSingleLineHeight(ref cellRect);
                    EditorGUI.BeginDisabledGroup(!enableEditingSettingsKeys || isLoading);
                    GUIContent ddBtnContent = new GUIContent(string.IsNullOrEmpty(item.data.type) ? "Select a type" : item.data.type);
                    if (GUI.Button(cellRect, ddBtnContent, EditorStyles.popup))
                    {
                        BuildPopupListForSettingTypes(item).DropDown(cellRect);
                    }
                    EditorGUI.EndDisabledGroup();

                    break;
                case 3:
                    EditorGUI.BeginDisabledGroup(isLoading || item.enabled == false);
                    string newValue = item.data.value;
                    switch (item.data.type)
                    {
                        case "string":
                            newValue = GUI.TextField(cellRect, item.data.value);
                            break;
                        case "bool":
                            bool boolVal = false;
                            try
                            {
                                boolVal = bool.Parse(item.data.value);
                            }
                            catch (FormatException)
                            {
                               //Do nothing 
                            }
                            newValue = EditorGUI.Toggle(cellRect, boolVal).ToString();
                            break;
                        case "float":
                            float floatVal = 0.0f;
                            try
                            {
                                floatVal = float.Parse(item.data.value);
                            }
                            catch(FormatException)
                            {
                                //Do nothing
                            }
                            newValue = EditorGUI.FloatField(cellRect, floatVal).ToString();
                            break;
                        case "int":
                            int intVal = 0;
                            try
                            {
                                intVal = int.Parse(item.data.value);
                            }
                            catch(FormatException)
                            {
                                //Do nothing
                            }
                            newValue = EditorGUI.IntField(cellRect, intVal).ToString();
                            break;
                        case "long":
                            long longVal = 0L;
                            try
                            {
                                longVal = long.Parse(item.data.value);
                            }
                            catch(FormatException)
                            {
                                //Do nothing
                            }
                            newValue = EditorGUI.LongField(cellRect, longVal).ToString();
                            break;
                    }
                    
                    
                    
                    EditorGUI.EndDisabledGroup();
                    if (newValue != item.data.value)
                    {
                        UpdateSetting?.Invoke(item.data.key,
                            new RemoteSettingsKeyValueType(item.data.key, item.data.type, newValue));
                    }

                    break;
            }       
        }

        private GenericMenu BuildPopupListForSettingTypes(TreeViewItem<RemoteSettingsKeyValueType> treeViewItem)
        {
            var menu = new GenericMenu();

            for (int i = 0; i < RemoteConfigDataManager.rsTypes.Count; i++)
            {
                string name = RemoteConfigDataManager.rsTypes[i];
                menu.AddItem(new GUIContent(name), string.Equals(name, treeViewItem.data.type), RSTypeChangedCallback, new RSTypeChangedStruct() { newType = name, rs = treeViewItem.data });
            }

            return menu;
        }

        private void RSTypeChangedCallback(object obj)
        {
            var rSTypeChangedStruct = (RSTypeChangedStruct)obj;
            if (rSTypeChangedStruct.newType != rSTypeChangedStruct.rs.type)
            {
                UpdateSetting?.Invoke(rSTypeChangedStruct.rs.key,
                    new RemoteSettingsKeyValueType(rSTypeChangedStruct.rs.key, rSTypeChangedStruct.newType, rSTypeChangedStruct.rs.value));
            }
        }

        bool IsKeyInRules(string key, List<Rule> rulesList)
        {
            //TODO: Simplify into Linq query
            foreach (var rule in rulesList)
            {
                if(rule.enabled)
                {
                    foreach (var setting in rule.value)
                    {
                        if (key == setting.key)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    internal class SettingsMultiColumnHeader : MultiColumnHeader
    {
        public SettingsMultiColumnHeader(MultiColumnHeaderState state) : base(state)
        {
            canSort = false;
        }
    }

    internal class TreeViewItem<T> : TreeViewItem
    {
        public T data;
        public bool enabled;

        public TreeViewItem(int id, int depth, string displayName, T data, bool enabled = true) : base(id, depth,
            displayName)
        {
            this.data = data;
            this.enabled = enabled;
        }

    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

namespace Unity.RemoteConfig.Editor
{
    internal class RemoteConfigWindowController
    {
        public event Action rulesDataStoreChanged;
        public event Action remoteSettingsStoreChanged;
        public event Action environmentChanged;

        RemoteConfigDataManager m_DataManager;

        bool m_WebRequestReturnedEventSubscribed = false;
        bool m_PostAddRuleEventSubscribed = false;
        bool m_PostSettingsEventSubscribed = false;
        bool m_PutConfigsEventSubscribed = false;
        string m_CurrentEnvironment;
        bool m_IsLoading = false;

        public bool isLoading
        {
            get { return m_IsLoading; }
            set { m_IsLoading = value; }
        }

        public RemoteConfigWindowController()
        {
            m_DataManager = new RemoteConfigDataManager();
            m_DataManager.RulesDataStoreChanged += OnRulesDataStoreChanged;
            m_DataManager.RemoteSettingDataStoreChanged += OnRemoteSettingDataStoreChanged;
            m_DataManager.EnvironmentChanged += OnEnvironmentChanged;
            FetchEnvironments();
            WebUtility.rcRequestFailed += OnFailedRequest;
        }

        private void OnFetchEnvironmentsFinished(List<Environment> environments)
        {
            SetEnvironmentData(environments);
        }

        private bool SetEnvironmentData(List<Environment> environments)
        {
            if (environments != null && environments.Count > 0)
            {
                m_DataManager.SetEnvironmentsList(environments);
                var currentEnvironment = LoadEnvironments(environments, m_DataManager.GetCurrentEnvironmentName());
                m_DataManager.SetCurrentEnvironment(currentEnvironment);
                m_DataManager.SetLastSelectedEnvironment(currentEnvironment.name);
                return true;
            }

            return false;
        }

        public Environment LoadEnvironments(List<Environment> environments, string currentEnvName)
        {
            if(environments.Count > 0)
            {
                var currentEnvironment = environments[0];  // Default to the first existing one
                foreach (var environment in environments)
                {
                    if (environment.name == currentEnvName)
                    {
                        currentEnvironment = environment;
                        break;
                    }
                }
                return currentEnvironment;
            }
            else
            {
                Debug.LogWarning("No environments loaded. Please restart the Remote Config editor window");
                return new Environment();
            }
        }

        public List<Rule> GetRulesList()
        {
            var rulesList = m_DataManager.GetRulesList();

            if (rulesList == null)
            {
                rulesList = new List<Rule>();
            }

            return rulesList;
        }

        public List<RemoteSettingsKeyValueType> GetSettingsList()
        {
            var settingsList = m_DataManager.GetRSList();

            if (settingsList == null)
            {
                settingsList = new List<RemoteSettingsKeyValueType>();
            }

            return settingsList;
        }
        
        public List<RemoteSettingsKeyValueType> GetSettingsListForRule(string ruleId)
        {
            var settingsListForRule = m_DataManager.GetSettingsListForRule(ruleId);

            if (settingsListForRule == null)
            {
                settingsListForRule = new List<RemoteSettingsKeyValueType>();
            }

            return settingsListForRule;
        }

        public void AddDefaultRule()
        {
            Rule defaultRule = new Rule(Guid.NewGuid().ToString(), "New Rule", false, RemoteConfigDataManager.defaultRulePriority, "", 100,
                new List<RemoteSettingsKeyValueType>(), null, null);
            m_DataManager.UpdateRule(defaultRule);
        }
        
        public void DeleteRule(string ruleId)
        {
            m_DataManager.DeleteRule(ruleId);
        }
        
        public void EnableOrDisableRule(string ruleId, bool enabled)
        {
            m_DataManager.EnableOrDisableRule(ruleId, enabled);
        }
        
        public bool HasRules()
        {
            return m_DataManager.HasRules();
        }

        public void UpdateRuleAttributes(string ruleId, Rule newRule)
        {
            m_DataManager.UpdateRuleAttributes(ruleId, newRule);
        }
        
        public Rule GetRuleAtIndex(int selectedRuleIndex)
        {
            return m_DataManager.GetRuleAtIndex(selectedRuleIndex);
        }

        public Rule GetRuleById(string ruleId)
        {
            return m_DataManager.GetRuleByID(ruleId);
        }

        public int GetEnvironmentsCount()
        {
            return m_DataManager.GetEnvironments().Count;
        }
        
        public string GetCurrentEnvironmentName()
        {
            return m_DataManager.GetCurrentEnvironmentName();
        }
        
        public GenericMenu BuildPopupListForEnvironments()
        {
            var menu = new GenericMenu();

            for (int i = 0; i < GetEnvironmentsCount(); i++)
            {
                string name = m_DataManager.GetEnvironments()[i].name;
                menu.AddItem(new GUIContent(name), name == m_DataManager.GetCurrentEnvironmentName(), EnvironmentSelectionCallback, name);
            }

            return menu;
        }
        
        private void EnvironmentSelectionCallback(object obj)
        {
            var envrionmentName = (string)obj;
            var env = m_DataManager.GetEnvironments().Find(x => x.name == envrionmentName);
            m_DataManager.SetCurrentEnvironment(env);
            FetchSettings(m_DataManager.GetEnvironments());
        }

        public GenericMenu BuildPopupListForRuleSettings(string selectedRuleId)
        {
            var menu = new GenericMenu();

            foreach (RemoteSettingsKeyValueType rs in m_DataManager.GetRSList())
            {
                if (!m_DataManager.HasRules() || !m_DataManager.IsSettingInRule(selectedRuleId, rs.key))
                {
                    menu.AddItem(new GUIContent(rs.key), rs.key == RemoteConfigWindow.settingsDropdownSelectedKey, obj => RemoteConfigWindow.settingsDropdownSelectedKey = (string)obj, rs.key);
                }
            }

            return menu;
        }
        
        public void AddSettingToRule(string selectedRuleId, string settingsDropdownSelectedKey)
        {
            m_DataManager.AddSettingToRule(selectedRuleId, settingsDropdownSelectedKey);
        }
        
        public void Fetch()
        {
            m_IsLoading = true;
            FetchEnvironments();
        }

        private void FetchEnvironments()
        {
            WebUtility.fetchEnvironmentsFinished += FetchSettings;
            WebUtility.FetchEnvironments();
        }

        private void FetchSettings(List<Environment> environments)
        {
            WebUtility.fetchEnvironmentsFinished -= FetchSettings;
            if (SetEnvironmentData(environments))
            {
                WebUtility.fetchConfigsFinished += OnFetchRemoteSettingsFinished;
                WebUtility.FetchConfigs(m_DataManager.GetCurrentEnvironmentId());
            }
        }

        private void FetchRules()
        {
            WebUtility.fetchRulesFinished += OnFetchRulesFinished;
            WebUtility.FetchRules(m_DataManager.configId);
        }

        public void Push()
        {

            if (m_DataManager.dataStoreStatus == RemoteConfigDataManager.m_DataStoreStatus.Error)
            {
                Debug.LogError("There are errors in the Local Data Rules and or Settings please resolve them before pushing changes");
            }
            else
            {
              string environmentId = m_DataManager.GetCurrentEnvironmentId();
              if(string.IsNullOrEmpty(m_DataManager.configId))
              {
                  WebUtility.postConfigRequestFinished += OnConfigPostFinsihedPushHandler;
                  PushSettings(environmentId);
              }
              else
              {
                  PushSettings(environmentId);
                  PushAddedRules(environmentId);
                  PushUpdatedRules(environmentId);
                  PushDeletedRules(environmentId);
               }
            }
        }

        private void OnConfigPostFinsihedPushHandler(string configId)
        {
            string environmentId = m_DataManager.GetCurrentEnvironmentId();
            m_DataManager.configId = configId;
            PushAddedRules(environmentId);
            PushUpdatedRules(environmentId);
            PushDeletedRules(environmentId);
            WebUtility.postConfigRequestFinished -= OnConfigPostFinsihedPushHandler;
        }

        public void AddSetting()
        {
            RemoteSettingsKeyValueType setting = new RemoteSettingsKeyValueType("Setting" + m_DataManager.settingsCount, "", "");
            m_DataManager.AddSetting(setting);
        }

        private void OnRuleRequestSuccess(string requestType, string ruleId)
        {
            switch(requestType)
            {
                case UnityWebRequest.kHttpVerbPUT:
                    m_DataManager.RemoveRuleFromUpdatedRuleIDs(ruleId);
                    break;
                case UnityWebRequest.kHttpVerbDELETE:
                    m_DataManager.RemoveRuleFromDeletedRuleIDs(ruleId);
                    break;
            }
            DoCleanUp();
        }

        private void OnSettingsRequestFinished()
        {
            DoCleanUp();
        }

        private void OnPostConfigRequestFinished(string configId)
        {
            m_DataManager.configId = configId;
            DoCleanUp();
        }

        private void OnFailedRequest()
        {
            DoCleanUp();
        }

        private void PushSettings(string environmentId)
        {
            m_IsLoading = true;
            if (string.IsNullOrEmpty(m_DataManager.configId))
            {
                WebUtility.postConfigRequestFinished += OnPostConfigRequestFinished;
                WebUtility.PostConfig(environmentId, m_DataManager.GetRSList());
                m_PostSettingsEventSubscribed = true;
            }
            else
            {
                WebUtility.settingsRequestFinished += OnSettingsRequestFinished;
                WebUtility.PutConfig(environmentId, m_DataManager.configId, m_DataManager.GetRSList());
                m_PutConfigsEventSubscribed = true;
            }
        }

        private void PushAddedRules(string environmentId)
        {
            var addedRuleIDs = m_DataManager.GetAddedRulesIDs();
            if (addedRuleIDs.Count > 0)
            {
                m_IsLoading = true;
                foreach (var addedRuleID in addedRuleIDs)
                {
                    if (!m_PostAddRuleEventSubscribed)
                    {
                        WebUtility.postAddRuleFinished += OnPostAddRuleFinished;
                        m_PostAddRuleEventSubscribed = true;
                    }
                    WebUtility.PostAddRule(environmentId, m_DataManager.configId, m_DataManager.GetRuleByID(addedRuleID));
                }
            }
        }

        private void PushUpdatedRules(string environmentId)
        {
            var updatedRuleIDs = m_DataManager.GetUpdatedRulesIDs();
            if (updatedRuleIDs.Count > 0)
            {
                m_IsLoading = true;
                if (!m_WebRequestReturnedEventSubscribed)
                {
                    WebUtility.ruleRequestSuccess += OnRuleRequestSuccess;
                    m_WebRequestReturnedEventSubscribed = true;
                }
                foreach (var updatedRuleID in updatedRuleIDs)
                {
                    WebUtility.PutEditRule(environmentId, m_DataManager.configId, m_DataManager.GetRuleByID(updatedRuleID));
                }
            }
        }
        
        private void PushDeletedRules(string environmentId)
        {
            var deletedRuleIDs = m_DataManager.GetDeletedRulesIDs();
            if (deletedRuleIDs.Count > 0)
            {
                m_IsLoading = true;
                if (!m_WebRequestReturnedEventSubscribed)
                {
                    WebUtility.ruleRequestSuccess += OnRuleRequestSuccess;
                    m_WebRequestReturnedEventSubscribed = true;
                }
                foreach (var deletedRuleID in deletedRuleIDs)
                {
                    WebUtility.DeleteRule(environmentId, deletedRuleID);
                }
            }
        }

        private void OnPostAddRuleFinished(RuleResponse ruleResponse, string oldRuleID)
        {
            var rule = m_DataManager.GetRuleByID(oldRuleID);
            m_DataManager.DeleteRule(oldRuleID);
            rule.id = ruleResponse.id;
            m_DataManager.UpdateRule(rule);
            m_DataManager.RemoveRuleFromAddedRuleIDs(rule.id);
            DoCleanUp();
        }

        private void OnFetchRemoteSettingsFinished(RemoteConfigConfigData config)
        {
            WebUtility.fetchConfigsFinished -= OnFetchRemoteSettingsFinished;
            m_DataManager.SetRSDataStore(config);
            FetchRules();
        }

        private void OnFetchRulesFinished(List<Rule> rules)
        {
            WebUtility.fetchRulesFinished -= OnFetchRulesFinished;
            m_DataManager.ClearRuleIDs();
            m_DataManager.SetRulesDataStore(rules);
            m_IsLoading = false;
        }

        private void OnRulesDataStoreChanged()
        {
            rulesDataStoreChanged?.Invoke();
        }
        
        private void OnRemoteSettingDataStoreChanged()
        {
            remoteSettingsStoreChanged?.Invoke();
        }
        
        private void OnEnvironmentChanged()
        {
            m_IsLoading = true;
            environmentChanged?.Invoke();
        }
        
        private void DoCleanUp()
        {
            if (WebUtility.webRequestsAreDone)
            {
                if (m_PostAddRuleEventSubscribed)
                {
                    WebUtility.postAddRuleFinished -= OnPostAddRuleFinished;
                    m_PostAddRuleEventSubscribed = false;
                }
                if(m_WebRequestReturnedEventSubscribed)
                {
                    WebUtility.ruleRequestSuccess -= OnRuleRequestSuccess;
                    m_WebRequestReturnedEventSubscribed = false;
                }
                if (m_PostSettingsEventSubscribed)
                {
                    WebUtility.postConfigRequestFinished -= OnPostConfigRequestFinished;
                    m_PostSettingsEventSubscribed = false;
                }
                if(m_PutConfigsEventSubscribed)
                {
                    WebUtility.settingsRequestFinished -= OnSettingsRequestFinished;
                    m_PutConfigsEventSubscribed = false;
                }
                
                m_IsLoading = false;
            }
        }

        public void UpdateRemoteSetting(string oldItemKey, RemoteSettingsKeyValueType newItem)
        {
            m_DataManager.UpdateSetting(oldItemKey, newItem);
        }

        public void UpdateSettingForRule(string ruleId, RemoteSettingsKeyValueType updatedSetting)
        {
            m_DataManager.UpdateSettingForRule(ruleId, updatedSetting);
        }

        public void DeleteRemoteSetting(string settingkey)
        {
            m_DataManager.DeleteSetting(settingkey);
        }

        public void DeleteSettingFromRule(string selectedRuleId, string settingKey)
        {
            m_DataManager.DeleteSettingFromRule(selectedRuleId, settingKey);
        }
    }
}

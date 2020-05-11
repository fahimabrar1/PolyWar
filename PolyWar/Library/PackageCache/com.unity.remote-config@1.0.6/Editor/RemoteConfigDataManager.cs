using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Runtime.CompilerServices;

namespace Unity.RemoteConfig.Editor
{
    /// <summary>
    /// This class contains all methods needed to perform CRUD operations on the data objects.
    /// No other classes should ever interact with the data objects directly.
    /// </summary>
    internal class RemoteConfigDataManager
    {
        public event Action RulesDataStoreChanged;
        public event Action RemoteSettingDataStoreChanged;
        public event Action EnvironmentChanged; 
        
        const string k_DataStoreAssetFileName = "{0}.asset";
        const string k_DataStoreName = "RemoteConfigDataStoreAsset";
        const string k_PathToDataStore = "Assets/Editor/RemoteConfig/Data";
        const string k_CurrentEnvironment = "UnityRemoteConfigEditorEnvironment";
        
        RemoteConfigDataStore m_DataStore;
        
        public enum m_DataStoreStatus {
            Init = 0,
            UnSynchronized = 1,
            Synchronized = 2,
            Pending = 3,
            Error = 4
        };

        public const int defaultRulePriority = 1000;
        private const int maxRulePriority = defaultRulePriority;
        private const int minRulePriority = 0;
        
        public string configId { get { return m_DataStore.configId; } set { m_DataStore.configId = value; } }
      
        public static readonly List<string> rsTypes = new List<string> { "string", "bool", "float", "int", "long" };
        public m_DataStoreStatus dataStoreStatus { get; set; }
        public int settingsCount { get { return m_DataStore.rsKeyList.Count; } }

        /// <summary>
        /// Constructor: creates amd initalizes the Remote Config data store and restores the last selected environment.
        /// </summary>
        public RemoteConfigDataManager()
        {
            m_DataStore = CheckAndCreateDataStore();
            CreateRSDictionaryFromRSList();
            CreateRulesDictionaryFromRulesList();
            RestoreLastSelectedEnvironment(m_DataStore.currentEnvironment);
            dataStoreStatus = m_DataStoreStatus.Init;
        }
        
        /// <summary>
        /// Returns the name of the last selected environment that is stored in EditorPrefs.
        /// </summary>
        /// <param name="defaultEnvironment"> The default environment name to be returned if last selected environment is not found</param>
        /// <returns> Name of last selected environment or defaultEnvironment if last selected is not found</returns>
        public string RestoreLastSelectedEnvironment(string defaultEnvironment)
        {
            return EditorPrefs.GetString(k_CurrentEnvironment + Application.cloudProjectId, defaultEnvironment);
        }
        
        /// <summary>
        /// Sets the name of the last selected environment and stores it in EditorPrefs.
        /// </summary>
        /// <param name="environmentName"> Name of environment to be stored</param>
        public void SetLastSelectedEnvironment (string environmentName)
        {
            EditorPrefs.SetString(k_CurrentEnvironment + Application.cloudProjectId, environmentName);
        }
        
        /// <summary>
        /// Checks for the existence of the Remote Config data store. Creates a new data store if one doesn't already exist
        /// and saves it to the AssetDatabase.
        /// </summary>
        /// <returns>Remote Config data store object</returns>
        public RemoteConfigDataStore CheckAndCreateDataStore()
        {
            string formattedPath = Path.Combine(k_PathToDataStore, string.Format(k_DataStoreAssetFileName, k_DataStoreName));
            if (AssetDatabase.FindAssets(k_DataStoreName).Length > 0)
            {
                if (AssetDatabase.LoadAssetAtPath(formattedPath, typeof(RemoteConfigDataStore)) == null)
                {
                    AssetDatabase.DeleteAsset(formattedPath);
                }
            }
            if (AssetDatabase.FindAssets(k_DataStoreName).Length == 0)
            {
                RemoteConfigDataStore asset = InitDataStore();
                CheckAndCreateAssetFolder(k_PathToDataStore);
                AssetDatabase.CreateAsset(asset, formattedPath);
                AssetDatabase.SaveAssets();
            }
            return AssetDatabase.LoadAssetAtPath(formattedPath, typeof(RemoteConfigDataStore)) as RemoteConfigDataStore;
        }

        private RemoteConfigDataStore InitDataStore()
        {
            RemoteConfigDataStore asset = ScriptableObject.CreateInstance<RemoteConfigDataStore>();
            asset.rsKeyList = new List<RemoteSettingsKeyValueType>();
            asset.currentEnvironment = "Development";
            asset.currentEnvironmentId = null;
            asset.environments = new List<Environment>();
            asset.rulesList = new List<Rule>();
            asset.addedRulesIDs = new List<string>();
            asset.updatedRulesIDs = new List<string>();
            asset.deletedRulesIDs = new List<string>();

            return asset;
        }
        
        private void CheckAndCreateAssetFolder(string dataStorePath)
        {
            string[] folders = dataStorePath.Split('/');
            string assetPath = null;
            foreach (string folder in folders)
            {
                if (assetPath == null)
                {
                    assetPath = folder;
                }
                else
                {
                    string folderPath = Path.Combine(assetPath, folder);
                    if (!Directory.Exists(folderPath))
                    {
                        AssetDatabase.CreateFolder(assetPath, folder);
                    }
                    assetPath = folderPath;
                }
            }
        }

        private void CreateRSDictionaryFromRSList()
        {
            if (m_DataStore && m_DataStore.rsKeyList.Count != 0)
            {
                if (m_DataStore.rsKeys == null)
                {
                    m_DataStore.rsKeys = new Dictionary<string, RemoteSettingsKeyValueType>();
                }
                else
                {
                    m_DataStore.rsKeys.Clear();
                }

                foreach (RemoteSettingsKeyValueType rsPair in m_DataStore.rsKeyList)
                {
                    m_DataStore.rsKeys.Add(rsPair.key, rsPair);
                }
            }
        }

        private void CreateRSListFromRSDictionary()
        {
            m_DataStore.rsKeyList.Clear();
            foreach (var rs in m_DataStore.rsKeys)
            {
                m_DataStore.rsKeyList.Add(rs.Value);
            }
        }
        
        private void CreateRulesDictionaryFromRulesList() {
            if (m_DataStore && m_DataStore.rulesList.Count != 0)
            {
                if (m_DataStore.rulesDict == null)
                {
                    m_DataStore.rulesDict = new Dictionary<string, Rule>();
                }
                else
                {
                    m_DataStore.rulesDict.Clear();
                }

                foreach (Rule rule in m_DataStore.rulesList)
                {
                    m_DataStore.rulesDict.Add(rule.id, rule);
                }
            }
        }

        private void CreateRulesListFromRulesDictionary()
        {
            m_DataStore.rulesList.Clear();
            foreach (var rule in m_DataStore.rulesDict)
            {
                m_DataStore.rulesList.Add(rule.Value);
            }
        }

        /// <summary>
        /// Gets the Remote Settings dictionary.
        /// </summary>
        /// <returns> Dictionary containing all the Remote Settings where the key is the Remote Setting key and the
        /// value is the RemoteSettingsKeyValueType object</returns>
        public Dictionary<string, RemoteSettingsKeyValueType> GetRSDictionary()
        {
            return m_DataStore.rsKeys;
        }

        /// <summary>
        /// Gets the Remote Settings list.
        /// </summary>
        /// <returns> List of RemoteSettingsKeyValueType objects</returns>
        public List<RemoteSettingsKeyValueType> GetRSList()
        {
            return m_DataStore.rsKeyList;
        }

        /// <summary>
        /// Gets the current environment name.
        /// </summary>
        /// <returns> Name of the current environment</returns>
        public string GetCurrentEnvironmentName()
        {
            return m_DataStore.currentEnvironment;
        }

        /// <summary>
        /// Gets the current environment ID.
        /// </summary>
        /// <returns> ID of the current environment</returns>
        public string GetCurrentEnvironmentId()
        {
            return m_DataStore.currentEnvironmentId;
        }
        
        /// <summary>
        /// Gets a list of all the environments for the current working project.
        /// </summary>
        /// <returns> List of Environment objects containing the name and ID</returns>
        public List<Environment> GetEnvironments()
        {
            return m_DataStore.environments;
        }

        /// <summary>
        /// Gets the Rules dictionary.
        /// </summary>
        /// <returns> Dictionary containing all the Rules where the key is the Rule's ID and the value is the Rule object</returns>
        public Dictionary<string, Rule> GetRulesDictionary()
        {
            return m_DataStore.rulesDict;
        }

        /// <summary>
        /// Gets the Rules list.
        /// </summary>
        /// <returns> List of Rules objects</returns>
        public List<Rule> GetRulesList()
        {
            return m_DataStore.rulesList;
        }

        /// <summary>
        /// Gets the list of added Rule ID's.
        /// </summary>
        /// <returns> List of Rule ID's for new rules that were added since the last push</returns>
        public List<string> GetAddedRulesIDs()
        {
            return m_DataStore.addedRulesIDs;
        }
        
        /// <summary>
        /// Gets the list of updated Rule ID's.
        /// </summary>
        /// <returns> List of Rule ID's for rules that were updated since the last push</returns>
        public List<string> GetUpdatedRulesIDs()
        {
            return m_DataStore.updatedRulesIDs;
        }
        
        /// <summary>
        /// Gets the list of deleted Rule ID's.
        /// </summary>
        /// <returns> List of Rule ID's for rules that were deleted since the last push</returns>
        public List<string> GetDeletedRulesIDs()
        {
            return m_DataStore.deletedRulesIDs;
        }
        
        /// <summary>
        /// Gets the Rule at the given index in the rulesList.
        /// </summary>
        /// <param name="selectedRuleIndex">The index of the Rule we are getting from the rulesList</param>
        /// <returns>The Rule from the rulesList at the given index</returns>
        public Rule GetRuleAtIndex(int selectedRuleIndex)
        {
            return m_DataStore.rulesList[selectedRuleIndex];
        }

        /// <summary>
        /// Gets the Rule for the given Rule ID.
        /// </summary>
        /// <param name="ruleId">The ID of the Rule that that we want to get</param>
        /// <returns>The Rule from the rulesDict for the given index</returns>
        public Rule GetRuleByID(string ruleId)
        {
            return m_DataStore.rulesDict[ruleId];
        }

        /// <summary>
        /// Sets the the current environment ID name.
        /// </summary>
        /// <param name="currentEnvironment">Current Environment object containing the current environment name and ID</param>
        public void SetCurrentEnvironment(Environment currentEnvironment)
        {
            m_DataStore.currentEnvironment = currentEnvironment.name;
            m_DataStore.currentEnvironmentId = currentEnvironment.id;
            EnvironmentChanged?.Invoke();
        }

        /// <summary>
        /// Sets the list of Environment objects containing the name and ID.
        /// </summary>
        /// <param name="environments">List of Environment objects containing the name and ID</param>
        public void SetEnvironmentsList(List<Environment> environments)
        {
            m_DataStore.environments = environments;
        }

        /// <summary>
        /// Sets the config object on the Remote Config Data Store
        /// </summary>
        /// <param name="config">A config object representing the new config</param>
        public void SetRSDataStore(RemoteConfigConfigData config)
        {
            m_DataStore.rsKeyList = new List<RemoteSettingsKeyValueType>(config.value);
            m_DataStore.configId = config.id;
            CreateRSDictionaryFromRSList();
        }

        /// <summary>
        /// Sets the Rules data store using a list of Rules.
        /// </summary>
        /// <param name="newRulesDataStore">A list of Rule objects</param>
        public void SetRulesDataStore(List<Rule> newRulesDataStore)
        {
            m_DataStore.rulesList = new List<Rule>(newRulesDataStore);
            CreateRulesDictionaryFromRulesList();
            RulesDataStoreChanged?.Invoke();
            RemoteSettingDataStoreChanged?.Invoke();
        }

        /// <summary>
        /// Sets the Rules data store using a dictionary of Rules.
        /// </summary>
        /// <param name="newRulesDataStore">A dictionary of Rules where the key is the rule ID and the value is a Rule object</param>        
        public void SetRulesDataStore(Dictionary<string, Rule> newRulesDataStore)
        {
            m_DataStore.rulesDict = new Dictionary<string, Rule>(newRulesDataStore);
            CreateRulesListFromRulesDictionary();
        }
        
        /// <summary>
        /// Adds a rule to the Rules data store. This will add it to the rulesList and the rulesDictionary.
        /// </summary>
        /// <param name="newRule">The Rule to be added</param>
        public void UpdateRule(Rule newRule)
        {
            if (newRule.name.StartsWith("New Rule"))
            {
                int maxNewRuleNumber = 1;
                List<Rule> newRuleList = m_DataStore.rulesList.FindAll(rule => rule.name.Contains(newRule.name));
                if (newRuleList.Count > 0)
                {
                    if (newRuleList[0].name == "New Rule" && newRuleList.Count == 1)
                    {
                        newRule.name = newRule.name.Insert(8, " " + maxNewRuleNumber);
                        AddRule(newRule);
                    }
                    else if (newRuleList.Count == 1)
                    {
                        maxNewRuleNumber = Int32.Parse(newRuleList[0].name.Replace("New Rule ", "")) + 1;
                        newRule.name = newRule.name.Insert(8, " " + maxNewRuleNumber);
                        AddRule(newRule);
                    }
                    else
                    {
                        var maxNewRule = newRuleList.OrderByDescending(rule => rule.name).First().name;
                        maxNewRuleNumber = Int32.Parse(maxNewRule.Replace("New Rule ", "")) + 1;
                        newRule.name = newRule.name.Insert(8, " " + maxNewRuleNumber);
                        AddRule(newRule);
                    }
                }
                else
                {
                    AddRule(newRule);
                }
            }
            else
            {
                AddRule(newRule);
            }
        }



        /// <summary>
        /// Adds a rule to the Rules data store. This will add it to the rulesList and the rulesDictionary.
        /// </summary>
        /// <param name="newRule">The Rule to be added</param>
        public void AddRule(Rule newRule)
        {
            m_DataStore.rulesList.Add(newRule);
            CreateRulesDictionaryFromRulesList();
            RulesDataStoreChanged?.Invoke();
            RemoteSettingDataStoreChanged?.Invoke();
            AddRuleToAddedRuleIDs(newRule);
        }

        private void AddRuleToAddedRuleIDs(Rule newRule)
        {
            m_DataStore.addedRulesIDs.Add(newRule.id);
        }

        /// <summary>
        /// Deletes a rule from the Rules data store. This will delete it from the rulesList and rulesDictionary.
        /// </summary>
        /// <param name="deletedRuleID">ID of the Rule to be deleted</param>
        public void DeleteRule(string deletedRuleID)
        {
            AddRuleToDeletedRuleIDs(GetRuleByID(deletedRuleID));
            m_DataStore.rulesDict.Remove(deletedRuleID);
            CreateRulesListFromRulesDictionary();
            RulesDataStoreChanged?.Invoke();
            RemoteSettingDataStoreChanged?.Invoke();
        }

        private void AddRuleToDeletedRuleIDs(Rule deletedRule)
        {
            bool ruleAdded = false;
            if (m_DataStore.addedRulesIDs.Contains(deletedRule.id))
            {
                m_DataStore.addedRulesIDs.Remove(deletedRule.id);
                ruleAdded = true;
            }

            if (m_DataStore.updatedRulesIDs.Contains(deletedRule.id))
            {
                m_DataStore.updatedRulesIDs.Remove(deletedRule.id);
            }

            if (!ruleAdded)
            {
                m_DataStore.deletedRulesIDs.Add(deletedRule.id);
            }
        }

        /// <summary>
        /// Checks to see if the given Rule's attributes are within the accepted range.
        /// </summary>
        /// <param name="rule">Rule object to be validated</param>
        /// <returns>true if the rule is valid and false if the rule is not valid</returns>
        public bool ValidateRule(Rule rule)
        {
            if (ValidateRulePriority(rule) && ValidateRuleName(rule))
            {
                dataStoreStatus = m_DataStoreStatus.UnSynchronized;
                return true;
            }
            else
            {
                dataStoreStatus = m_DataStoreStatus.Error;
                return false;
            }
        }

        public bool ValidateRuleName(Rule rule)
        {
            var duplicateIndex = m_DataStore.rulesList.FindIndex(rules => rules.name == rule.name);

            if (duplicateIndex == -1)
            {
                return true;
            }
            else if ( m_DataStore.rulesList[duplicateIndex].id == rule.id)
            {
                return true;
            }
            else
            { 
                Debug.LogWarning( m_DataStore.rulesList[duplicateIndex].name + " already exists. Rule names must be unique.");
                return false;
            }
        }

        public bool ValidateRulePriority(Rule rule)
        {	        
            if (rule.priority < 0 || rule.priority > 1000)
            {
                Debug.LogWarning("Rule: " + rule.name + " has an invalid priority. The set priority is " + rule.priority + ". The values for priority must be between " + minRulePriority + " and " + maxRulePriority);
                return false;
            }
            else
            {
                return true;	            
            }
        }

        /// <summary>
        /// Updates the attributes for a given rule. This will update the rule in the rulesList and rulesDictionary.
        /// </summary>
        /// <param name="ruleId">ID of the rule being updated</param>
        /// <param name="newRule">Rule object containing the new attributes</param>
        public void UpdateRuleAttributes(string ruleId, Rule newRule)
        {
            if (ValidateRule(newRule))
            {
                m_DataStore.rulesDict.Remove(ruleId);
                m_DataStore.rulesDict.Add(newRule.id, newRule);
                CreateRulesListFromRulesDictionary();
                RulesDataStoreChanged?.Invoke();
                AddRuleToUpdatedRuleIDs(newRule.id);
            }
        }
        
        /// <summary>
        /// Enables or disables the given rule.
        /// </summary>
        /// <param name="ruleId">ID of Rule to be enabled or disabled</param>
        /// <param name="enabled">true = enabled, false = disabled</param>
        public void EnableOrDisableRule(string ruleId, bool enabled)
        {
            var rule = m_DataStore.rulesDict[ruleId];
            m_DataStore.rulesDict.Remove(ruleId);
            rule.enabled = enabled;
            m_DataStore.rulesDict.Add(ruleId, rule);
            CreateRulesListFromRulesDictionary();
            AddRuleToUpdatedRuleIDs(ruleId);
            RulesDataStoreChanged?.Invoke();
        }

        /// <summary>
        /// Adds the given setting to the given rule.
        /// </summary>
        /// <param name="selectedRuleId">ID of the rule that the setting should be added to</param>
        /// <param name="settingKey">Key of the setting to be added to the given rule</param>
        public void AddSettingToRule(string selectedRuleId, string settingKey)
        {
            var setting = m_DataStore.rsKeys[settingKey];
            m_DataStore.rulesDict[selectedRuleId].value.Add(setting);
            CreateRulesListFromRulesDictionary();
            RemoteSettingDataStoreChanged?.Invoke();
            AddRuleToUpdatedRuleIDs(m_DataStore.rulesDict[selectedRuleId].id);
        }

        /// <summary>
        /// Deletes the given setting to the given Rule.
        /// </summary>
        /// <param name="ruleId">ID of the rule that the setting should be deleted from</param>
        /// <param name="settingKey">Key of the setting to be deleted from the given rule</param>
        public void DeleteSettingFromRule(string ruleId, string settingKey)
        {
            var setting = m_DataStore.rulesDict[ruleId].value.Find(x => x.key == settingKey);
            m_DataStore.rulesDict[ruleId].value.Remove(setting);
            RemoteSettingDataStoreChanged?.Invoke();
            AddRuleToUpdatedRuleIDs(ruleId);
        }

        /// <summary>
        /// Updates the value of the given setting for the given rule.
        /// </summary>
        /// <param name="ruleId">ID of the rule that the updated setting belong to</param>
        /// <param name="updatedSetting">A RemoteSettingsKeyValueType containing the updated value</param>
        public void UpdateSettingForRule(string ruleId, RemoteSettingsKeyValueType updatedSetting)
        {
            var rule = m_DataStore.rulesDict[ruleId];
            var setting = rule.value.Find(arg => arg.key == updatedSetting.key);
            var settingIndex = rule.value.IndexOf(setting);
            rule.value[settingIndex] = updatedSetting;
            m_DataStore.rulesDict[ruleId] = rule;
            CreateRulesListFromRulesDictionary();
            RemoteSettingDataStoreChanged?.Invoke();
            AddRuleToUpdatedRuleIDs(ruleId);
        }

        private void AddRuleToUpdatedRuleIDs(string updatedRule)
        {
            //this is a new rule, do nothing - the changes will get picked up the add rule request
            if (!m_DataStore.addedRulesIDs.Contains(updatedRule) && !m_DataStore.updatedRulesIDs.Contains(updatedRule))
            {
                m_DataStore.updatedRulesIDs.Add(updatedRule);
            }
        }

        /// <summary>
        /// Removes the given rule ID from the list of added rules ID's.
        /// </summary>
        /// <param name="ruleId">ID of the rule to be removed from the list of added rule ID's</param>
        public void RemoveRuleFromAddedRuleIDs(string ruleId)
        {
            m_DataStore.addedRulesIDs.Remove(ruleId);
        }
        
        /// <summary>
        /// Removes the given rule ID from the list of updated rule ID's.
        /// </summary>
        /// <param name="ruleId">ID of the rule to be removed from the list of updated rule ID's</param>
        public void RemoveRuleFromUpdatedRuleIDs(string ruleId)
        {
            m_DataStore.updatedRulesIDs.Remove(ruleId);
        }
        
        /// <summary>
        /// Removes the given rule ID from the list of deleted rule ID's.
        /// </summary>
        /// <param name="ruleId">ID of the rule to be remove from the list of deleted rule ID's</param>
        public void RemoveRuleFromDeletedRuleIDs(string ruleId)
        {
            m_DataStore.deletedRulesIDs.Remove(ruleId);
        }
        
        /// <summary>
        /// Clears the list of added rule ID's, list of updated rule ID's, and the list of deleted rule ID's.
        /// </summary>
        public void ClearRuleIDs()
        {
            m_DataStore.addedRulesIDs.Clear();
            m_DataStore.updatedRulesIDs.Clear();
            m_DataStore.deletedRulesIDs.Clear();
        }

        /// <summary>
        /// Adds a setting to the Remote Settings data store. This will add the setting to the rsKeyList and rsKeys(dictionary).
        /// </summary>
        /// <param name="newSetting">The setting to be added</param>
        public void AddSetting(RemoteSettingsKeyValueType newSetting)
        {
            m_DataStore.rsKeyList.Add(newSetting);
            CreateRSDictionaryFromRSList();
            RemoteSettingDataStoreChanged?.Invoke();
        }

        /// <summary>
        /// Deletes a setting from the Remote Settings data store. This will delete the setting from the rsKeyList and rsKeys(dictionary).
        /// </summary>
        /// <param name="settingKey">The key of the setting to be deleted</param>
        public void DeleteSetting(string settingKey)
        {
            m_DataStore.rsKeys.Remove(settingKey);
            CreateRSListFromRSDictionary();
            RemoteSettingDataStoreChanged?.Invoke();
        }

        /// <summary>
        /// Updates a setting in the Remote Settings data store. This will update the setting in the rsKeyList and rsKeys(dictionary).
        /// </summary>
        /// <param name="oldSettingKey">The key of the setting to be updated</param>
        /// <param name="newSetting">The new setting with the updated fields</param>
        public void UpdateSetting(string oldSettingKey, RemoteSettingsKeyValueType newSetting)
        {
            //duplicate key
            if(oldSettingKey != newSetting.key && m_DataStore.rsKeys.ContainsKey(newSetting.key))
            {
                Debug.LogWarning(newSetting.key + " already exists. Setting keys must be unique.");
            }
            if(newSetting.key.Length >= 255)
            {
                Debug.LogWarning(newSetting.key + " is at the maximum length of 255 characters.");
            }
            else
            {
                m_DataStore.rsKeys.Remove(oldSettingKey);
                m_DataStore.rsKeys.Add(newSetting.key, newSetting);
                CreateRSListFromRSDictionary();
                OnRemoteSettingUpdated(oldSettingKey, newSetting);
                RemoteSettingDataStoreChanged?.Invoke();
            }
        }

        /// <summary>
        /// Checks to see if any rules exist
        /// </summary>
        /// <returns>true if there is at leave one rule and false if there are no rules</returns>
        public bool HasRules()
        {
            return m_DataStore.rulesList.Count > 0;
        }
        
        /// <summary>
        /// Checks if the given setting is being used by the given rule
        /// </summary>
        /// <param name="ruleId">ID of the rule that needs to be checked</param>
        /// <param name="rsKey">Key of the setting that needs to be checked</param>
        /// <returns>true if the given setting is being used by the given rule</returns>
        public bool IsSettingInRule(string ruleId, string rsKey)
        {
            var matchingRS = m_DataStore.rulesDict[ruleId].value.Where((arg) => arg.key == rsKey).ToList();
            if(matchingRS.Count > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns list of settings for particular rule
        /// </summary>
        /// <param name="ruleId">ID of the rule </param>
        /// <returns>list of settings used by the given rule</returns>
        public List<RemoteSettingsKeyValueType> GetSettingsListForRule(string ruleId)
        {
            var settingsInRule = new List<RemoteSettingsKeyValueType>();
            var settings = m_DataStore.rsKeyList;

            for (int i = 0; i < settings.Count; i++)
            {
                var key = settings[i].key;
                var type = settings[i].type;
                var value = settings[i].value;

                if (IsSettingInRule(ruleId, key))
                {
                    settingsInRule.Add(new RemoteSettingsKeyValueType(key, type, value));
                }

            }
            return settingsInRule;
        }

        private void OnRemoteSettingUpdated(string oldSettingKey, RemoteSettingsKeyValueType newRS)
        {
            //TODO: Simplify into Linq query
            for(int i = 0; i < m_DataStore.rulesList.Count; i++)
            {
                var rule = m_DataStore.rulesList[i];
                for(int j = 0; j < rule.value.Count; j++)
                {
                    var setting = rule.value[j];
                    if (setting.key == oldSettingKey)
                    {
                        setting.key = newRS.key;
                        setting.type = newRS.type;
                        rule.value[j] = setting;
                    }
                }

                m_DataStore.rulesList[i] = rule;
                CreateRulesDictionaryFromRulesList();
            }
        }
    }
}
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEditor;

namespace Unity.RemoteConfig.Editor.Tests
{
    internal class RemoteConfigDataManagerTests
    {
        [TearDown]
        public void TearDown()
        {
            var path = typeof(RemoteConfigDataManager)
                .GetField("k_PathToDataStore", BindingFlags.Static | BindingFlags.NonPublic )
                .GetValue(null) as string;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }
        
        [Test]
        public void CheckAndCreateDataStore_ReturnsDataStore()
        {
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            Assert.That(dataManager.CheckAndCreateDataStore().GetType() == typeof(RemoteConfigDataStore));
        }

        [Test]
        public void InitDataStore_InitsAll()
        {
            var dataStore = RCTestUtils.GetDataStore();
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();

            var initDataStoreMethod =
                typeof(RemoteConfigDataManager).GetMethod("InitDataStore", BindingFlags.NonPublic |
                    BindingFlags.Instance);
            initDataStoreMethod?.Invoke(dataManager, new object[] {});
            
            Assert.That(dataStore.rsKeyList != null);
            Assert.That(Equals(dataStore.currentEnvironment, "Release"));
            Assert.That(string.IsNullOrEmpty(dataStore.currentEnvironmentId));
            Assert.That(dataStore.environments != null);
            Assert.That(dataStore.rulesList != null);
            Assert.That(dataStore.addedRulesIDs != null);
            Assert.That(dataStore.updatedRulesIDs != null);
            Assert.That(dataStore.deletedRulesIDs != null);
        }

        [Test]
        public void CheckAndCreateAssetFolder_CreatesAssetFolder()
        {
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            var path = typeof(RemoteConfigDataManager)
                .GetField("k_PathToDataStore", BindingFlags.Static | BindingFlags.NonPublic )
                .GetValue(dataManager) as string;
            Directory.Delete(path, true);
            Assert.That(!Directory.Exists(path));

            var checkAndCreateAssetFolderMethod =
                typeof(RemoteConfigDataManager).GetMethod("CheckAndCreateAssetFolder",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            checkAndCreateAssetFolderMethod?.Invoke(dataManager, new object[] {path});
            Assert.That(Directory.Exists(path));
        }

        [Test]
        public void CreateRSDictionaryFromRSList_PopulatesRSDictionary()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rsKeyList = new List<RemoteSettingsKeyValueType>(RCTestUtils.rsList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();

            var createRSDictionaryFromRSListMethod =
                typeof(RemoteConfigDataManager).GetMethod("CreateRSDictionaryFromRSList",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            createRSDictionaryFromRSListMethod?.Invoke(dataManager, new object[] {});
            
            RSListShouldMatchRSDict(dataStore);
        }

        [Test]
        public void CreateRSListFromRSDictionary_PopulatesRSList()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rsKeys = new Dictionary<string, RemoteSettingsKeyValueType>(RCTestUtils.rsDict);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            var createRSListFromRSDictionaryMethod =
                typeof(RemoteConfigDataManager).GetMethod("CreateRSListFromRSDictionary",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            createRSListFromRSDictionaryMethod?.Invoke(dataManager, new object[] {});
            
            RSListShouldMatchRSDict(dataStore);
        }

        [Test]
        public void CreateRulesDictionaryFromRulesList_PopulatesRulesDictionary()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();

            var createRulesDictionaryFromRulesListMethod =
                typeof(RemoteConfigDataManager).GetMethod("CreateRulesDictionaryFromRulesList",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            createRulesDictionaryFromRulesListMethod?.Invoke(dataManager, new object[] {});
            
            RulesListShouldMatchRulesDict(dataStore);
        }

        [Test]
        public void CreateRulesListFromRulesDictionary_PopulatesRulesList()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesDict = new Dictionary<string, Rule>(RCTestUtils.rulesDict);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();

            var createRulesListFromRulesDictionaryMethod =
                typeof(RemoteConfigDataManager).GetMethod("CreateRulesListFromRulesDictionary",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            createRulesListFromRulesDictionaryMethod?.Invoke(dataManager, new object[] {});
            
            RulesListShouldMatchRulesDict(dataStore);
        }

        [Test]
        public void GetRSDictionary_ReturnsRSDictionary()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rsKeys = new Dictionary<string, RemoteSettingsKeyValueType>(RCTestUtils.rsDict);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetRSDictionary(), dataStore.rsKeys));
        }

        [Test]
        public void GetRSList_ReturnsRSList()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rsKeyList = new List<RemoteSettingsKeyValueType>(RCTestUtils.rsList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetRSList(), dataStore.rsKeyList));
        }
        
        [Test]
        public void GetCurrentEnvironmentName_ReturnsEnvironmentName()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.currentEnvironment = RCTestUtils.currentEnvironment;
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetCurrentEnvironmentName(), dataStore.currentEnvironment));
        }

        [Test]
        public void GetCurrentEnvironmentId_ReturnsCurrentEnvironmentId()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.currentEnvironmentId = RCTestUtils.currentEnvironmentId;
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetCurrentEnvironmentId(), dataStore.currentEnvironmentId));
        }

        [Test]
        public void GetEnvironments_ReturnsListOfEnvironments()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.environments = RCTestUtils.environments;
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetEnvironments(), dataStore.environments));
        }

        [Test]
        public void GetRulesDictionary_ReturnsRulesDictionary()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesDict = new Dictionary<string, Rule>(RCTestUtils.rulesDict);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetRulesDictionary(), dataStore.rulesDict));
        }

        [Test]
        public void GetRulesList_ReturnsRulesList()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetRulesList(), dataStore.rulesList));
        }

        [Test]
        public void GetAddedRulesIDs_ReturnsAddedRuleIDList()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.addedRulesIDs = new List<string>(RCTestUtils.addedRuleIDs);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetAddedRulesIDs(), dataStore.addedRulesIDs));
        }

        [Test]
        public void GetUpdatedRulesIDs_ReturnsUpdatedRuleIDList()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.updatedRulesIDs = new List<string>(RCTestUtils.updatedRuleIDs);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetUpdatedRulesIDs(), dataStore.updatedRulesIDs));
        }
        
        [Test]
        public void GetDeletedRulesIDs_ReturnsUpdatedRuleIDList()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.deletedRulesIDs = new List<string>(RCTestUtils.deletedRuleIDs);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Assert.That(Equals(dataManager.GetDeletedRulesIDs(), dataStore.deletedRulesIDs));
        }

        [Test]
        public void GetRuleAtIndex_ReturnsRuleAtIndex()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();

            for (int i = 0; i < dataStore.rulesList.Count; i++)
            {
                Assert.That(Equals(dataManager.GetRuleAtIndex(i), dataStore.rulesList[i]));
            }
        }

        [Test]
        public void GetRuleByID_ReturnsRuleWithGivenID()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            for (int i = 0; i < dataStore.rulesList.Count; i++)
            {
                Assert.That(Equals(dataManager.GetRuleByID(dataStore.rulesList[i].id), dataStore.rulesList[i]));
            }
        }

        [Test]
        public void SetCurrentEnvironment_SetsCurrentEnvironment()
        {
            var dataStore = RCTestUtils.GetDataStore();
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            
            Environment currentEnvironment = new Environment();
            currentEnvironment.name = RCTestUtils.currentEnvironment;
            currentEnvironment.id = RCTestUtils.currentEnvironmentId;
            
            dataManager.SetCurrentEnvironment(currentEnvironment);
            
            Assert.That(Equals(dataStore.currentEnvironment, currentEnvironment.name));
            Assert.That(Equals(dataStore.currentEnvironmentId, currentEnvironment.id));
        }

        [Test]
        public void SetEnvironmentsList_SetsEnvironmentsList()
        {
            var dataStore = RCTestUtils.GetDataStore();
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.SetEnvironmentsList(RCTestUtils.environments);
            
            Assert.That(Equals(dataStore.environments, RCTestUtils.environments));
        }

        [Test]
        public void SetRSDataStore_SetsRSDataStoreWhenAListIsPassedIn()
        {
            var dataStore = RCTestUtils.GetDataStore();
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            var config = new RemoteConfigConfigData() {
                type = "settings",
                id = "someId",
                value = RCTestUtils.rsList
            };
            dataManager.SetRSDataStore(config);

            Assert.That(RSDictsAreEqual(dataStore.rsKeys, RCTestUtils.rsDict));
            Assert.That(RSListsAreEqual(dataStore.rsKeyList, RCTestUtils.rsList));
            RSListShouldMatchRSDict(dataStore);
        }

        [Test]
        public void SetRulesDataStore_SetsRulesStoreWhenAListIsPassedIn()
        {
            var dataStore = RCTestUtils.GetDataStore();
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.SetRulesDataStore(RCTestUtils.rulesList);
            
            Assert.That(RuleDictsAreEqual(dataStore.rulesDict, RCTestUtils.rulesDict));
            Assert.That(RuleListsAreEqual(dataStore.rulesList, RCTestUtils.rulesList));
            RulesListShouldMatchRulesDict(dataStore);
        }
        
        [Test]
        public void SetRulesDataStore_SetsRulesStoreWhenADictIsPassedIn()
        {
            var dataStore = RCTestUtils.GetDataStore();
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.SetRulesDataStore(RCTestUtils.rulesDict);
            
            Assert.That(RuleDictsAreEqual(dataStore.rulesDict, RCTestUtils.rulesDict));
            Assert.That(RuleListsAreEqual(dataStore.rulesList, RCTestUtils.rulesList));
            RulesListShouldMatchRulesDict(dataStore);
        }

        [Test]
        public void AddRule_AddsRuleToRulesListAndAddedRulesIDs()
        {
            var dataStore = RCTestUtils.GetDataStore();
            var addedRule = RCTestUtils.rulesList[0];
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.UpdateRule(addedRule);
            
            Assert.That(dataStore.rulesList.Contains(addedRule));
            Assert.That(dataStore.addedRulesIDs.Contains(addedRule.id));
            Assert.That(dataStore.rulesDict.ContainsKey(addedRule.id));
            Assert.That(dataStore.rulesList.Contains(addedRule));
            RulesListShouldMatchRulesDict(dataStore);
        }

        [Test]
        public void DeleteRule_DeletesRuleFromRulesListAndAddsRuleToDeletedRuleIDs()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesList);
            var deletedRule = RCTestUtils.rulesList[0];
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.DeleteRule(deletedRule.id);
            
            Assert.That(!dataStore.rulesList.Contains(deletedRule));
            Assert.That(dataStore.deletedRulesIDs.Contains(deletedRule.id));
            Assert.That(!dataStore.rulesDict.ContainsKey(deletedRule.id));
            Assert.That(!dataStore.rulesList.Contains(deletedRule));
            RulesListShouldMatchRulesDict(dataStore);
        }

        [Test]
        public void DeleteRule_ShouldDeleteNewlyAddedRuleFromRulesListWithoutAddingItToDeletedRuleIDs()
        {
            var dataStore = RCTestUtils.GetDataStore();
            var deletedRule = RCTestUtils.rulesList[0];
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.UpdateRule(deletedRule);
            dataManager.DeleteRule(deletedRule.id);
            
            Assert.That(!dataStore.rulesList.Contains(deletedRule));
            Assert.That(!dataStore.deletedRulesIDs.Contains(deletedRule.id));
            Assert.That(!dataStore.rulesDict.ContainsKey(deletedRule.id));
            Assert.That(!dataStore.rulesList.Contains(deletedRule));
            RulesListShouldMatchRulesDict(dataStore);
        }

        [Test]
        public void UpdateRuleAttributes_ShouldUpdateRule()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.UpdateRuleAttributes(RCTestUtils.ruleOneId, RCTestUtils.updatedRuleOne);
            Assert.That(dataStore.rulesList.Contains(RCTestUtils.updatedRuleOne));
            Assert.That(!dataStore.rulesList.Contains(RCTestUtils.rulesList[0]));
            Assert.That(dataStore.rulesDict.ContainsKey(RCTestUtils.updatedRuleOne.id));
            var ruleFromDict = dataStore.rulesDict[RCTestUtils.updatedRuleOne.id];
            Assert.That(ruleFromDict.enabled == RCTestUtils.updatedRuleOne.enabled);
            Assert.That(string.Equals(ruleFromDict.condition, RCTestUtils.updatedRuleOne.condition));
            Assert.That(string.Equals(ruleFromDict.name, RCTestUtils.updatedRuleOne.name));
            Assert.That(ruleFromDict.rolloutPercentage == RCTestUtils.updatedRuleOne.rolloutPercentage);
        }

        [Test]
        public void EnableOrDisableRule_UpdatesEnabledFieldOfRule()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.EnableOrDisableRule(RCTestUtils.ruleOneId, false);
            Assert.That(dataStore.rulesDict[RCTestUtils.ruleOneId].enabled == false);
        }

        [Test]
        public void AddSettingToRule_ShouldAddRightSettingToRightRule()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesList);
            dataStore.rsKeyList = new List<RemoteSettingsKeyValueType>(RCTestUtils.rsList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.AddSettingToRule(RCTestUtils.ruleOneId, RCTestUtils.stringKeyName);
            Assert.That(dataStore.rulesDict[RCTestUtils.ruleOneId].value.Contains(RCTestUtils.rsList[0]));
            Assert.That(dataStore.updatedRulesIDs.Contains(RCTestUtils.ruleOneId));
        }

        [Test]
        public void DeleteSettingFromRule_ShouldRemoveRightSettingsFromRightRule()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesDictWithSettings.Values.ToList());
            dataStore.rsKeyList = new List<RemoteSettingsKeyValueType>(RCTestUtils.rsList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.DeleteSettingFromRule(RCTestUtils.ruleOneId, RCTestUtils.intKeyName);
            Assert.That(!dataStore.rulesDict[RCTestUtils.ruleOneId].value.Contains(RCTestUtils.rsList[1]));
            Assert.That(dataStore.updatedRulesIDs.Contains(RCTestUtils.ruleOneId));
        }

        [Test]
        public void UpdateSettingForRule_ShouldUpdateSettingOnRightRule()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesDictWithSettings.Values.ToList());
            dataStore.rsKeyList = new List<RemoteSettingsKeyValueType>(RCTestUtils.rsList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            var newVal = "new value";
            dataManager.UpdateSettingForRule(RCTestUtils.ruleOneId, new RemoteSettingsKeyValueType(RCTestUtils.stringKeyName, "string", newVal));
            var setting = dataStore.rulesDict[RCTestUtils.ruleOneId].value.Find((obj) => obj.key == RCTestUtils.stringKeyName);
            Assert.That(string.Equals(setting.value, newVal));
            Assert.That(dataStore.updatedRulesIDs.Contains(RCTestUtils.ruleOneId));
        }

        [Test]
        public void RemoveRuleFromAddedRuleIDs_RemovesRuleFromAddedRules()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.addedRulesIDs = new List<string>(RCTestUtils.updatedRuleIDs);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.RemoveRuleFromAddedRuleIDs(RCTestUtils.updatedRuleIDs[0]);
            Assert.That(dataStore.addedRulesIDs.Count == 2);
            Assert.That(!dataStore.addedRulesIDs.Contains(RCTestUtils.updatedRuleIDs[0]));
        }

        [Test]
        public void RemoveRuleFromUpdatedRuleIDs_RemovesRuleFromUpdatedRules()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.updatedRulesIDs = new List<string>(RCTestUtils.updatedRuleIDs);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.RemoveRuleFromUpdatedRuleIDs(RCTestUtils.updatedRuleIDs[0]);
            Assert.That(dataStore.updatedRulesIDs.Count == 2);
            Assert.That(!dataStore.updatedRulesIDs.Contains(RCTestUtils.updatedRuleIDs[0]));
        }

        [Test]
        public void RemoveRuleFromDeletedRuleIDs_RemovesRuleFromDeletedRules()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.deletedRulesIDs = new List<string>(RCTestUtils.updatedRuleIDs);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.RemoveRuleFromDeletedRuleIDs(RCTestUtils.updatedRuleIDs[0]);
            Assert.That(dataStore.deletedRulesIDs.Count == 2);
            Assert.That(!dataStore.deletedRulesIDs.Contains(RCTestUtils.updatedRuleIDs[0]));
        }

        [Test]
        public void ClearRuleIDs_ClearsAllLists ()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.updatedRulesIDs = new List<string>(RCTestUtils.updatedRuleIDs);
            dataStore.addedRulesIDs = new List<string>(RCTestUtils.addedRuleIDs);
            dataStore.deletedRulesIDs = new List<string>(RCTestUtils.deletedRuleIDs);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.ClearRuleIDs();
            Assert.That(dataStore.updatedRulesIDs.Count == 0);
            Assert.That(dataStore.addedRulesIDs.Count == 0);
            Assert.That(dataStore.deletedRulesIDs.Count == 0);
        }

        [Test]
        public void AddSetting_AddsSettingToListAndDict()
        {
            var dataStore = RCTestUtils.GetDataStore();
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.AddSetting(RCTestUtils.rsList[0]);
            Assert.That(dataStore.rsKeyList.Contains(RCTestUtils.rsList[0]));
            Assert.That(dataStore.rsKeys.ContainsKey(RCTestUtils.rsList[0].key));
            var rs = dataStore.rsKeys[RCTestUtils.rsList[0].key];
            Assert.That(string.Equals(rs.key, RCTestUtils.rsList[0].key));
            Assert.That(string.Equals(rs.type, RCTestUtils.rsList[0].type));
            Assert.That(string.Equals(rs.value, RCTestUtils.rsList[0].value));
        }

        [Test]
        public void DeleteSetting_DeletesSettingFromListAndDict()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rsKeyList = new List<RemoteSettingsKeyValueType>(RCTestUtils.rsList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            dataManager.DeleteSetting(RCTestUtils.rsList[0].key);
            Assert.That(!dataStore.rsKeyList.Contains(RCTestUtils.rsList[0]));
            Assert.That(!dataStore.rsKeys.ContainsKey(RCTestUtils.rsList[0].key));
        }

        [Test]
        public void UpdateSetting_UpdatesCorrectSetting()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rsKeyList = new List<RemoteSettingsKeyValueType>(RCTestUtils.rsList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            var newRs = new RemoteSettingsKeyValueType("updated-key", "updated-type", "updated-value");
            dataManager.UpdateSetting(RCTestUtils.rsList[0].key, newRs);
            Assert.That(!dataStore.rsKeyList.Contains(RCTestUtils.rsList[0]));
            Assert.That(!dataStore.rsKeys.ContainsKey(RCTestUtils.rsList[0].key));
            Assert.That(dataStore.rsKeyList.Contains(newRs));
            Assert.That(dataStore.rsKeys.ContainsKey(newRs.key));
            var rs = dataStore.rsKeys[newRs.key];
            Assert.That(string.Equals(rs.key, newRs.key));
            Assert.That(string.Equals(rs.type, newRs.type));
            Assert.That(string.Equals(rs.value, newRs.value));
        }

        [Test]
        public void HasRules_CorrectlyReturnsTrueIfThereAreRules()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesList);
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            Assert.That(dataManager.HasRules() == true);
        }

        [Test]
        public void HasRules_CorrectlyReturnsFalseIfThereAreNoRules()
        {
            var dataStore = RCTestUtils.GetDataStore();
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            Assert.That(dataManager.HasRules() == false);
        }

        [Test]
        public void IsSettingInRule_ReturnsTrueWhenSettingIsInRule()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesDictWithSettings.Values.ToList());
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            Assert.That(dataManager.IsSettingInRule(RCTestUtils.ruleOneId, RCTestUtils.stringKeyName));
        }

        [Test]
        public void IsSettingInRule_ReturnsFalseWhenSettingIsNotInRule()
        {
            var dataStore = RCTestUtils.GetDataStore();
            dataStore.rulesList = new List<Rule>(RCTestUtils.rulesDictWithSettings.Values.ToList());
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            Assert.That(!dataManager.IsSettingInRule(RCTestUtils.ruleOneId, "Fake Key"));
        }

        [Test]
        public void ValidateRule_ShouldReturnTrueForValidRule()
        {
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            Assert.That(dataManager.ValidateRule(RCTestUtils.CreateValidRule()));
        }

        [Test]
        public void ValidateRule_ShouldReturnFalseForInvalidRule()
        {
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            var rule = RCTestUtils.CreateValidRule();
            rule.priority = 1400;
            Assert.That(dataManager.ValidateRule(rule) == false);
            rule.priority = -1;
            Assert.That(dataManager.ValidateRule(rule) == false);
        }

        [Test]
        public void ConfigID_ShouldReturnConfigIDFromDataStore()
        {
            var dataStore = RCTestUtils.GetDataStore();
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            Assert.That(string.IsNullOrEmpty(dataManager.configId));
            dataStore.configId = "someId";
            Assert.That(string.Equals(dataManager.configId, "someId"));
        }

        [Test] 
        public void ValidateRule_ShouldReturnTrueForAddingDuplicateRuleName()
        {
            RemoteConfigDataManager dataManager = new RemoteConfigDataManager();
            var rule = RCTestUtils.CreateValidRule();
            dataManager.UpdateRule(rule);
            var ruleDuplicate = RCTestUtils.CreateValidRule();
            dataManager.UpdateRule(ruleDuplicate);
            var rulesList = dataManager.GetRulesList();
            Assert.That(rulesList[0].name != rulesList[1].name);
        }

        private bool RSDictsAreEqual(Dictionary<string, RemoteSettingsKeyValueType> dict1, Dictionary<string, RemoteSettingsKeyValueType> dict2)
        {
            return dict1.Keys.Count == dict2.Keys.Count &&
                dict1.Keys.All(k => dict2.ContainsKey(k) && Equals(dict2[k], dict1[k]));
        }
        
        private bool RuleDictsAreEqual(Dictionary<string, Rule> dict1, Dictionary<string, Rule> dict2)
        {
            return dict1.Keys.Count == dict2.Keys.Count &&
                   dict1.Keys.All(k => dict2.ContainsKey(k) && Equals(dict2[k], dict1[k]));
        }

        private bool RSListsAreEqual(List<RemoteSettingsKeyValueType> list1, List<RemoteSettingsKeyValueType> list2)
        {
            return list1.All(list2.Contains) && list1.Count == list2.Count;
        }
        
        private bool RuleListsAreEqual(List<Rule> list1, List<Rule> list2)
        {
            return list1.All(list2.Contains) && list1.Count == list2.Count;
        }

        private void RSListShouldMatchRSDict(RemoteConfigDataStore dataStore)
        {
            for (int i = 0; i < dataStore.rsKeyList.Count; i++)
            {
                Assert.That(dataStore.rsKeys.ContainsKey(dataStore.rsKeyList[i].key));
                Assert.That(Equals(dataStore.rsKeys[dataStore.rsKeyList[i].key], dataStore.rsKeyList[i]));
            }
            Assert.That(dataStore.rsKeyList.Count == dataStore.rsKeys.Count);
        }
        
        private void RulesListShouldMatchRulesDict(RemoteConfigDataStore dataStore)
        {
            for (int i = 0; i < dataStore.rulesList.Count; i++)
            {
                Assert.That(dataStore.rulesDict.ContainsKey(dataStore.rulesList[i].id));
                Assert.That(Equals(dataStore.rulesDict[dataStore.rulesList[i].id], dataStore.rulesList[i]));
            }
            Assert.That(dataStore.rulesList.Count == dataStore.rulesDict.Count);
        }
    }
    
    internal static class RCTestUtils
    {
        public const string stringKeyName = "test-string";
        public const string intKeyName = "test-int";
        public const string floatKeyName = "test-float";
        public const string boolKeyName = "test-bool";
        public const string longKeyName = "test-long";
        public const string stringValue = "test-value-1";
        public const int intValue = 1;
        public const float floatValue = 1.0f;
        public const bool boolValue = true;
        public const long longValue = 32L;
        
        public const string currentEnvironment = "test-environment";
        public const string currentEnvironmentId = "test-environment-id";

        public const string ruleOneId = "rule-id-1";
        public const string ruleOneUpdatedId = "updated-rule-id-1";
        public const string ruleOneUpdatedName = "updated-rule-name-1";

        public static Dictionary<string, RemoteSettingsKeyValueType> rsDict =
            new Dictionary<string, RemoteSettingsKeyValueType>
            {
                {stringKeyName, new RemoteSettingsKeyValueType(stringKeyName, "string", stringValue)},
                {intKeyName, new RemoteSettingsKeyValueType(intKeyName, "int", intValue.ToString())},
                {floatKeyName, new RemoteSettingsKeyValueType(floatKeyName, "float", floatValue.ToString())},
                {boolKeyName, new RemoteSettingsKeyValueType(boolKeyName, "bool", boolValue.ToString())},
                {longKeyName, new RemoteSettingsKeyValueType(longKeyName, "long", longValue.ToString())}
            };

        public static List<RemoteSettingsKeyValueType> rsList = rsDict.Values.ToList();

        public static List<Environment> environments = new List<Environment>
        {
            new Environment("env-id-1", "app-id-1", "env-name-1", "env-description-1", "created-at", "updated-at"),
            new Environment("env-id-2", "app-id-2", "env-name-2", "env-description-2", "created-at", "updated-at")
        };

        public static Dictionary<string, Rule> rulesDict = new Dictionary<string, Rule>()
        {
            {
                ruleOneId,
                new Rule(ruleOneId, "rule-name-1", true, 1000, "true", 100, new List<RemoteSettingsKeyValueType>(), "2019-07-10T23:15:14.000-0700", "2019-08-12T08:15:14.000+0430")
            },
            {
                "rule-id-2",
                new Rule("rule-id-2", "rule-name-2", true, 1000, "true", 100, new List<RemoteSettingsKeyValueType>(), "2019-07-10T23:15:14.000-0700", "2019-08-12T08:15:14.000+0430")
            }
        };

        public static Dictionary<string, Rule> rulesDictWithSettings = new Dictionary<string, Rule>()
        {
            {
                ruleOneId,
                new Rule(ruleOneId, "rule-name-1", true, 1000, "true", 100, new List<RemoteSettingsKeyValueType>(rsList), "2019-07-10T23:15:14.000-0700", "2019-08-12T08:15:14.000+0430")
            },
            {
                "rule-id-2",
                new Rule("rule-id-2", "rule-name-2", true, 1000, "true", 100, new List<RemoteSettingsKeyValueType>(rsList), "2019-07-10T23:15:14.000-0700", "2019-08-12T08:15:14.000+0430")
            }
        };

        public static Rule CreateValidRule()
        {
            return new Rule(System.Guid.NewGuid().ToString(), "New Rule", false, 1000, "true", 100, new List<RemoteSettingsKeyValueType>(rsList));
        }

        public static List<Rule> rulesList = rulesDict.Values.ToList();

        public static Rule updatedRuleOne = new Rule(ruleOneUpdatedId, ruleOneUpdatedName, false, 500, "false", 50, new List<RemoteSettingsKeyValueType>(), "2019-07-10T23:15:14.000-0700", "2019-08-12T08:15:14.000+0430");

        public static List<string> addedRuleIDs = new List<string>()
        {
            "added-rule-id-1",
            "added-rule-id-2",
            "added-rule-id-3"
        };
        
        public static List<string> updatedRuleIDs = new List<string>()
        {
            ruleOneUpdatedId,
            "updated-rule-id-2",
            "updated-rule-id-3"
        };

        public static List<string> deletedRuleIDs = new List<string>()
        {
            "deleted-rule-id-1",
            "deleted-rule-id-2",
            "deleted-rule-id-3"
        };

        public static RemoteConfigDataStore GetDataStore()
        {
            var pathToDataStore = typeof(RemoteConfigDataManager)
            .GetField("k_PathToDataStore", BindingFlags.Static | BindingFlags.NonPublic )
            .GetValue(null) as string;
            
            var dataStoreAssetFileName = typeof(RemoteConfigDataManager)
                .GetField("k_DataStoreAssetFileName", BindingFlags.Static | BindingFlags.NonPublic )
                .GetValue(null) as string;
            
            var dataStoreName = typeof(RemoteConfigDataManager)
                .GetField("k_DataStoreName", BindingFlags.Static | BindingFlags.NonPublic )
                .GetValue(null) as string;
            
            string formattedPath = Path.Combine(pathToDataStore, string.Format(dataStoreAssetFileName, dataStoreName));
            RemoteConfigDataStore asset = InitDataStore();
            CheckAndCreateAssetFolder(pathToDataStore);
            AssetDatabase.CreateAsset(asset, formattedPath);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath(formattedPath, typeof(RemoteConfigDataStore)) as RemoteConfigDataStore;
        }

        private static RemoteConfigDataStore InitDataStore()
        {
            RemoteConfigDataStore asset = ScriptableObject.CreateInstance<RemoteConfigDataStore>();
            asset.rsKeyList = new List<RemoteSettingsKeyValueType>();
            asset.currentEnvironment = "Release";
            asset.currentEnvironmentId = null;
            asset.environments = new List<Environment>();
            asset.rulesList = new List<Rule>();
            asset.addedRulesIDs = new List<string>();
            asset.updatedRulesIDs = new List<string>();
            asset.deletedRulesIDs = new List<string>();

            return asset;
        }
        
        private static void CheckAndCreateAssetFolder(string dataStorePath)
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
    }
}
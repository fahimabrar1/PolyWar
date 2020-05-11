using System.Collections.Generic;
using UnityEngine;

namespace Unity.RemoteConfig.Editor
{
    internal class RemoteConfigDataStore : ScriptableObject
    {
        // Data stores for Remote Setings
        public Dictionary<string, RemoteSettingsKeyValueType> rsKeys;
        public List<RemoteSettingsKeyValueType> rsKeyList;
        public string configId;

        public string currentEnvironment;
        public string currentEnvironmentId;
        public List<Environment> environments;
        
        // Data stores for Rules
        public Dictionary<string, Rule> rulesDict;
        public List<Rule> rulesList;

        public List<string> addedRulesIDs;
        public List<string> updatedRulesIDs;
        public List<string> deletedRulesIDs;
    }
}
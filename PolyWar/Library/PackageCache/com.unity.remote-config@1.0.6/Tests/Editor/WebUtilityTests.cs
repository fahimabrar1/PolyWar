using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;

namespace Unity.RemoteConfig.Editor.Tests
{
    internal class WebUtilityTests
    {
        [Test]
        public void SerializeRSList_ReturnsCorrectJson()
        {
            List<RemoteSettingsKeyValueType> remoteSettings = new List<RemoteSettingsKeyValueType>();
            remoteSettings.Add(new RemoteSettingsKeyValueType("testString", "string", "someString"));
            remoteSettings.Add(new RemoteSettingsKeyValueType("testBool", "bool", "False"));
            remoteSettings.Add(new RemoteSettingsKeyValueType("testInt", "int", "1234"));
            remoteSettings.Add(new RemoteSettingsKeyValueType("testFloat", "float", "1.2"));
            remoteSettings.Add(new RemoteSettingsKeyValueType("testLong", "long", "9223372036854775807"));

            string result = @"[{""key"":""testString"",""type"":""string"",""values"":[""someString""]},{""key"":""testBool"",""type"":""bool"",""values"":[false]},{""key"":""testInt"",""type"":""int"",""values"":[1234]},{""key"":""testFloat"",""type"":""float"",""values"":[1.2000000476837159]},{""key"":""testLong"",""type"":""long"",""values"":[9223372036854775807]}]";

            var methodInfo = typeof(WebUtility).GetMethod("SerializeRSList", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(string.Equals(methodInfo.Invoke(null, new object[] { remoteSettings }), result));
        }

        [Test]
        public void SerializeRSList_ThrowsExceptionWhenBadInput()
        {
            List<RemoteSettingsKeyValueType> remoteSettings = new List<RemoteSettingsKeyValueType>();
            remoteSettings.Add(new RemoteSettingsKeyValueType("testBad", "temp", "1234567890"));


            var methodInfo = typeof(WebUtility).GetMethod("SerializeRSList", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                methodInfo.Invoke(null, new object[] { remoteSettings });
            }
            catch(TargetInvocationException ex)
            {
                Assert.That(ex.InnerException.GetType() == typeof(System.Exception));
            }
        }

        [Test]
        public void SerializeWebRSList_ReturnsCorrectJson()
        {
            List<IRemoteSettingsWebPayload> remoteSettings = new List<IRemoteSettingsWebPayload>();
            remoteSettings.Add(new RemoteSettingsStringKeyValueWebPayload("testString", "someString"));
            remoteSettings.Add(new RemoteSettingsBoolKeyValueWebPayload("testBool", true));
            remoteSettings.Add(new RemoteSettingsIntKeyValueWebPayload("testInt", 1234));
            remoteSettings.Add(new RemoteSettingsFloatKeyValueWebPayload("testFloat", 1.2f));
            remoteSettings.Add(new RemoteSettingsLongKeyValueWebPayload("testLong", 9223372036854775807));

            string result = @"[{""key"":""testString"",""type"":""string"",""value"":""someString""},{""key"":""testBool"",""type"":""bool"",""value"":true},{""key"":""testInt"",""type"":""int"",""value"":1234},{""key"":""testFloat"",""type"":""float"",""value"":1.2000000476837159},{""key"":""testLong"",""type"":""long"",""value"":9223372036854775807}]";

            var methodInfo = typeof(WebUtility).GetMethod("SerializeWebRSList", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(string.Equals(methodInfo.Invoke(null, new object[] { remoteSettings }), result));
        }

        [Test]
        public void ParseRequestError_ReturnsCorrectStructOnValidInput()
        {
            var validDownloadHandlerResponse = @"{""message"":""`condition` must not be empty or null"",""code"":400}";
            RequestError requestErrorExpected = new RequestError{ message = "`condition` must not be empty or null", code = 400};
            var methodInfo = typeof(WebUtility).GetMethod("ParseRequestError", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(Equals(methodInfo.Invoke(null, new object[] { validDownloadHandlerResponse }), requestErrorExpected));
        }

        [Test]
        public void ParseRequestError_ThrowsExceptionOnIvalidInput()
        {
            var invalidDownloadHandlerResponse = @"{""message:}";
            var methodInfo = typeof(WebUtility).GetMethod("ParseRequestError", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                methodInfo.Invoke(null, new object[] { invalidDownloadHandlerResponse });
            }
            catch(TargetInvocationException ex)
            {
                Assert.That(ex.InnerException.GetType() == typeof(System.Exception));
            }
        }
    }
}


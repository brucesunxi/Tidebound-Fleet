using System.Collections;
using System.IO;
using NUnit.Framework;
using Tidebound.Unity.Analytics;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class AnalyticsPrivacyPlayModeTests
    {
        private GameObject root;
        private string path, prior;
        private LanguagePreference language;
        [SetUp] public void Before()
        {
            path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/Tidebound/analytics-v1.json"));
            prior=File.Exists(path)?File.ReadAllText(path):null;
            if(File.Exists(path))File.Delete(path);
            language=UILanguage.Preference;
        }
        [TearDown] public void After()
        {
            if(root)Object.DestroyImmediate(root);
            if(prior!=null)File.WriteAllText(path,prior);else if(File.Exists(path))File.Delete(path);
            UILanguage.SetPreference(language,false);
        }
        [UnityTest] public IEnumerator AdultConfirmationDoesNotOptInAndDecliningCreatesNoIdentifier()
        {
            root=new GameObject("AnalyticsPrivacy_Test",typeof(RectTransform),typeof(Canvas));
            var service=root.AddComponent<TideboundAnalytics>();service.enabled=false;service.Initialize(null);
            var rect=HarborUI.Rect("Choices",root.transform);rect.sizeDelta=new Vector2(360,640);
            var panel=rect.gameObject.AddComponent<AnalyticsPrivacyPanel>();panel.Initialize(service);panel.Open();yield return null;
            Assert.That(service.IsEnabled,Is.False);Assert.That(service.ReferenceId,Is.Empty);
            rect.Find("Content/Primary").GetComponent<Button>().onClick.Invoke();
            Assert.That(service.IsEnabled,Is.False,"Age selection is not consent");
            rect.Find("Content/Decline").GetComponent<Button>().onClick.Invoke();
            Assert.That(service.IsEnabled,Is.False);Assert.That(service.ReferenceId,Is.Empty);
            foreach(var locale in new[]{LanguagePreference.Chinese,LanguagePreference.English})
            {
                UILanguage.SetPreference(locale,false);panel.Open();yield return null;
                var explanation=rect.Find("Content/Explanation").GetComponent<Text>();
                Assert.That(explanation.text,Does.Contain("18"));Assert.That(explanation.text,Does.Contain("90"));
                Assert.That(explanation.rectTransform.rect.height,Is.GreaterThan(250));
            }
        }
        [UnityTest] public IEnumerator ExplicitOptInThenWithdrawalPurgesBeforeAnyNetworkOperation()
        {
            root=new GameObject("AnalyticsConsent_Test");var service=root.AddComponent<TideboundAnalytics>();
            service.enabled=false;service.Initialize(null);
            Assert.That(service.SetConsent(true,true),Is.True);Assert.That(service.IsEnabled,Is.True);
            var state=JsonUtility.FromJson<AnalyticsState>(File.ReadAllText(path));
            Assert.That(state.pending.Count,Is.EqualTo(1));Assert.That(state.pending[0].item.name,Is.EqualTo("session_start"));
            service.SetConsent(false,false);yield return null;
            state=JsonUtility.FromJson<AnalyticsState>(File.ReadAllText(path));
            Assert.That(state.pending,Is.Empty);Assert.That(state.consent,Is.False);Assert.That(state.pendingDeletion,Is.True);
            Assert.That(service.IsEnabled,Is.False);Assert.That(service.SetConsent(true,true),Is.False,"Deletion finishes before a new identity is enabled");
        }
    }
}

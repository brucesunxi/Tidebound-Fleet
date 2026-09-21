using System;
using NUnit.Framework;
using Tidebound.Unity.UI;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class UILanguageTests
    {
        private LanguagePreference previous;
        [SetUp] public void Before(){previous=UILanguage.Preference;UILanguage.SetPseudoForReview(false);}
        [TearDown] public void After(){UILanguage.SetPseudoForReview(false);UILanguage.SetPreference(previous,false);}
        [TestCase(LanguagePreference.Auto,DeviceRegion.China,LanguagePreference.Chinese)]
        [TestCase(LanguagePreference.Auto,DeviceRegion.Other,LanguagePreference.English)]
        [TestCase(LanguagePreference.Auto,DeviceRegion.Unknown,LanguagePreference.English)]
        [TestCase(LanguagePreference.English,DeviceRegion.China,LanguagePreference.English)]
        [TestCase(LanguagePreference.Chinese,DeviceRegion.Other,LanguagePreference.Chinese)]
        [TestCase(LanguagePreference.Chinese,DeviceRegion.Unknown,LanguagePreference.Chinese)]
        public void ManualSelectionTakesPriority(LanguagePreference choice,DeviceRegion region,LanguagePreference expected)
        {Assert.That(UILanguage.Resolve(choice,region),Is.EqualTo(expected));}
        [Test]
        public void UnavailableOrMalformedCountryDoesNotPretendToBeChina()
        {
            Assert.That(UILanguage.ReadRegion(()=>null),Is.EqualTo(DeviceRegion.Unknown));
            Assert.That(UILanguage.ReadRegion(()=>"zh-CN"),Is.EqualTo(DeviceRegion.Unknown));
            Assert.That(UILanguage.ReadRegion(()=>throw new Exception()),Is.EqualTo(DeviceRegion.Unknown));
            Assert.That(UILanguage.ReadRegion(()=>" cn "),Is.EqualTo(DeviceRegion.China));
            Assert.That(UILanguage.ReadRegion(()=>"US"),Is.EqualTo(DeviceRegion.Other));
        }
        [Test]
        public void ParameterizedCatalogAndMultilineMessagesTranslateWithoutChangingNumbers()
        {
            UILanguage.SetPreference(LanguagePreference.Chinese,false);
            Assert.That(UILanguage.Translate("Owned 3/16   Coins 1470   Tickets 5"),Is.EqualTo("已收藏3/16　金币1470　收藏券5"));
            Assert.That(UILanguage.Translate("Rates / pity  G 7/20  R 17/60"),Is.EqualTo("概率与保底　金7/20　红17/60"));
            Assert.That(UILanguage.Translate("Level 10 cleared - rewards saved"),Is.EqualTo("第10关完成，奖励已入账"));
            Assert.That(UILanguage.Translate("Start\nLevel 3"),Is.EqualTo("开始\n第 3 关"));
            Assert.That(UILanguage.Translate("Earn through level clears.\n\nThe unlock catalog is being prepared.\nYour current appearance stays unchanged."),Does.Not.Contain("Earn"));
            Assert.That(UILanguage.Translate("Rescue 2 (1)"),Is.EqualTo("两船救援（1）"));
            Assert.That(UILanguage.Translate("Coins: 767   |   Pending: 0   |   Tools: 5/5"),Is.EqualTo("金币：767　待结算：0　道具：5/5"));
            Assert.That(UILanguage.Translate("Default  /  Common  /  1 x 2 ship"),Is.EqualTo("默认／白色／1×2标准船"));
        }
        [Test]
        public void LanguageSelectionPersistsIndependentlyAndInvalidPreferenceFallsBackToAuto()
        {
            const string key="Tidebound.Language";var existed=PlayerPrefs.HasKey(key);var old=PlayerPrefs.GetInt(key);
            try
            {
                UILanguage.SetPreference(LanguagePreference.Chinese);UILanguage.ReloadPreference();Assert.That(UILanguage.Preference,Is.EqualTo(LanguagePreference.Chinese));
                UILanguage.SetPreference(LanguagePreference.English);UILanguage.ReloadPreference();Assert.That(UILanguage.Preference,Is.EqualTo(LanguagePreference.English));
                PlayerPrefs.SetInt(key,88);UILanguage.ReloadPreference();Assert.That(UILanguage.Preference,Is.EqualTo(LanguagePreference.Auto));
            }
            finally{if(existed)PlayerPrefs.SetInt(key,old);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();UILanguage.ReloadPreference();}
        }
        [Test]
        public void InactiveTextRetainsSourceAndRefreshesOnReopen()
        {
            var go=new GameObject("LocaleText",typeof(RectTransform));
            try
            {
                var label=go.AddComponent<HarborText>();UILanguage.SetPreference(LanguagePreference.English,false);label.text="Coins: 4000";
                go.SetActive(false);UILanguage.SetPreference(LanguagePreference.Chinese,false);go.SetActive(true);
                Assert.That(label.text,Is.EqualTo("金币：4000"));Assert.That(label.Source,Is.EqualTo("Coins: 4000"));
                UILanguage.SetPreference(LanguagePreference.English,false);Assert.That(label.text,Is.EqualTo("Coins: 4000"));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test]
        public void BundledFontContainsTheChineseInterfaceCharacters()
        {
            Assert.That(HarborUI.Font.name,Does.Contain("Noto"));
            foreach(var c in "舰队收藏抽奖补给金币设置已拥有通关")Assert.That(HarborUI.Font.HasCharacter(c),Is.True,"Missing glyph: "+c);
        }
    }
}

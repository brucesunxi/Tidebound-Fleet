using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class SettingsPausePlayModeTests
    {
        private sealed class Store : IPlayerSaveStore
        {
            public PlayerSaveData Data;
            public bool Fail;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData data){if(Fail)throw new IOException("test storage unavailable");Data=data.Copy();}
        }
        private GameObject root;
        private Store store;
        private LanguagePreference language;
        private int music, sound, languageKey;
        [SetUp] public void Before()
        {
            music=PlayerPrefs.GetInt(HarborAudio.MusicKey,-1);sound=PlayerPrefs.GetInt(HarborAudio.SoundKey,-1);
            languageKey=PlayerPrefs.GetInt("Tidebound.Language",-1);language=UILanguage.Preference;
        }
        [TearDown] public void After()
        {
            if(root)UnityEngine.Object.DestroyImmediate(root);
            Restore(HarborAudio.MusicKey,music);Restore(HarborAudio.SoundKey,sound);Restore("Tidebound.Language",languageKey);
            PlayerPrefs.Save();UILanguage.SetPreference(language,false);
        }
        private static void Restore(string key,int value){if(value<0)PlayerPrefs.DeleteKey(key);else PlayerPrefs.SetInt(key,value);}
        private PortraitPuzzleGraybox Create()
        {
            store=new Store();root=new GameObject("SettingsPause_Test");var game=root.AddComponent<PortraitPuzzleGraybox>();
            var catalog=new PlayableLevelCatalog(new[]{"Settings_1"},_=>true,id=>new LevelData
            {
                SchemaVersion=2,LevelId=id,BossId="TF_KRAKEN_01",Width=6,Height=6,
                Ships=new[]{new ShipPlacementData{Id="A",TypeId="TF_BASE_SHIP",Position=new GridPosition(0,0),Direction=ShipDirection.Up,Length=2},
                    new ShipPlacementData{Id="B",TypeId="TF_BASE_SHIP",Position=new GridPosition(3,0),Direction=ShipDirection.Up,Length=2}}
            });
            game.Initialize(catalog,saveService:new PlayerSaveService(store),campaign:true,useHomeNavigation:true);
            return game;
        }
        [UnityTest] public IEnumerator HomeSettingsHaveOnlyApprovedControlsAndBothOfflineDocuments()
        {
            var game=Create();game.OpenHomeSettings();yield return null;
            var panel=game.HomeSettingsView;
            Assert.That(panel.LanguageChinese,Is.Not.Null);Assert.That(panel.LanguageEnglish,Is.Not.Null);
            Assert.That(panel.transform.Find("LanguageAuto"),Is.Null);
            Assert.That(panel.GetComponentsInChildren<Button>().Length,Is.EqualTo(7));
            foreach(var locale in new[]{LanguagePreference.Chinese,LanguagePreference.English})
            {
                UILanguage.SetPreference(locale,false);
                panel.PrivacyButton.onClick.Invoke();yield return null;
                Assert.That(panel.IsLegalOpen,Is.True);Assert.That(panel.LegalText.Length,Is.GreaterThan(500));
                var scroll=panel.GetComponentInChildren<ScrollRect>();Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
                scroll.verticalNormalizedPosition=0;yield return null;Assert.That(scroll.verticalNormalizedPosition,Is.LessThan(.01f));
                panel.CloseLegal();panel.TermsButton.onClick.Invoke();yield return null;
                Assert.That(panel.LegalText,Does.Contain(locale==LanguagePreference.Chinese?"用户协议":"Terms of Use"));panel.CloseLegal();
            }
            Assert.That(game.Session,Is.Null);Assert.That(store.Data?.Attempt,Is.Null);
            panel.transform.Find("Close").GetComponent<Button>().onClick.Invoke();Assert.That(panel.gameObject.activeSelf,Is.False);
        }
        [UnityTest] public IEnumerator AudioChannelsAreIndependentAndPreferencesReloadWithoutChangingProgress()
        {
            var game=Create();game.OpenHomeSettings();var audio=game.AudioSettings;var coins=game.SaveService.Coins;
            audio.SetMusic(true);audio.SetSound(true);game.HomeSettingsView.MusicButton.onClick.Invoke();
            Assert.That(audio.MusicEnabled,Is.False);Assert.That(audio.SoundEnabled,Is.True);Assert.That(audio.MusicSource.mute,Is.True);
            game.HomeSettingsView.SoundButton.onClick.Invoke();Assert.That(audio.SoundSource.mute,Is.True);
            audio.ReloadPreferences();Assert.That(audio.MusicEnabled,Is.False);Assert.That(audio.SoundEnabled,Is.False);
            Assert.That(PlayerPrefs.GetInt(HarborAudio.MusicKey),Is.Zero);Assert.That(PlayerPrefs.GetInt(HarborAudio.SoundKey),Is.Zero);
            Assert.That(game.SaveService.Coins,Is.EqualTo(coins));Assert.That(store.Data?.Attempt,Is.Null);
            yield return null;
        }
        [UnityTest] public IEnumerator PauseContinuePreservesAttemptButHomeStartCreatesFreshAttemptAndSkipDoesNothing()
        {
            var game=Create();game.ContinueFromHome();var id=game.Session.SessionId;var coins=game.SaveService.Coins;
            game.ToggleMenu();yield return null;var panel=game.PauseSettingsView;
            Assert.That(game.IsPaused,Is.True);Assert.That(panel.LanguageChinese,Is.Null);Assert.That(panel.PrivacyButton,Is.Null);
            Assert.That(panel.SkipButton.interactable,Is.False);panel.SkipButton.onClick.Invoke();
            var elapsed=game.SaveService.Runtime.Transit.ElapsedTime;yield return new WaitForSecondsRealtime(.1f);
            Assert.That(game.SaveService.Runtime.Transit.ElapsedTime,Is.EqualTo(elapsed));
            Assert.That(game.Session.SessionId,Is.EqualTo(id));Assert.That(game.SaveService.Coins,Is.EqualTo(coins));
            panel.ContinueButton.onClick.Invoke();Assert.That(game.IsPaused,Is.False);Assert.That(store.Data.Attempt.Paused,Is.False);
            game.ToggleMenu();game.PauseSettingsView.ExitButton.onClick.Invoke();Assert.That(game.IsHomeOpen,Is.True);
            game.ContinueFromHome();Assert.That(game.Session.SessionId,Is.Not.EqualTo(id));Assert.That(game.IsPaused,Is.False);
        }
        [UnityTest] public IEnumerator RestartUsesExistingTransactionDirectlyAndFailedSaveKeepsPause()
        {
            var game=Create();game.ContinueFromHome();var id=game.Session.SessionId;game.ToggleMenu();
            store.Fail=true;game.PauseSettingsView.ContinueButton.onClick.Invoke();
            Assert.That(game.IsMenuOpen,Is.True);Assert.That(game.IsPaused,Is.True);Assert.That(game.Session.SessionId,Is.EqualTo(id));
            store.Fail=false;game.PauseSettingsView.RestartButton.onClick.Invoke();yield return null;
            Assert.That(game.IsMenuOpen,Is.False);Assert.That(game.Session.SessionId,Is.Not.EqualTo(id));
            Assert.That(game.LevelIndex,Is.Zero);Assert.That(game.IsPaused,Is.False);Assert.That(game.Session.Board.ShipCount,Is.EqualTo(2));
            Assert.That(game.transform.Find("RestartConfirmation"),Is.Null);
        }
    }
}

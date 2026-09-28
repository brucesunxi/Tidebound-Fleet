using System;
using UnityEngine;

namespace Tidebound.Unity.UI
{
    /// <summary>Two local audio channels. Preferences are separate from player/economy saves.</summary>
    public sealed class HarborAudio : MonoBehaviour
    {
        public const string MusicKey = "Tidebound.Audio.Music", SoundKey = "Tidebound.Audio.Sound";
        public bool PersistPreferences = true;
        public bool MusicEnabled { get; private set; }
        public bool SoundEnabled { get; private set; }
        public AudioSource MusicSource { get; private set; }
        public AudioSource SoundSource { get; private set; }
        public event Action Changed;
        private AudioClip musicClip, clickClip;
        private bool suspended;

        private void Awake()
        {
            MusicSource = gameObject.AddComponent<AudioSource>();
            SoundSource = gameObject.AddComponent<AudioSource>();
            MusicSource.playOnAwake = SoundSource.playOnAwake = false;
            MusicSource.spatialBlend = SoundSource.spatialBlend = 0;
            MusicSource.loop = true; MusicSource.volume = .16f; SoundSource.volume = .25f;
            if (FindObjectOfType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            // Original synthesized harbour theme and short UI chime; no vendor sound assets or SDK.
            musicClip = BuildMusic(); clickClip = BuildClick(); MusicSource.clip = musicClip;
            ReloadPreferences();
        }
        public void ReloadPreferences()
        {
            MusicEnabled = PlayerPrefs.GetInt(MusicKey, 1) != 0;
            SoundEnabled = PlayerPrefs.GetInt(SoundKey, 1) != 0;
            Apply(); Changed?.Invoke();
        }
        public void SetMusic(bool value)
        { MusicEnabled = value; Save(MusicKey, value); Apply(); Changed?.Invoke(); }
        public void SetSound(bool value)
        { SoundEnabled = value; Save(SoundKey, value); Apply(); Changed?.Invoke(); if (value) Click(); }
        private void Save(string key, bool value)
        { if (!PersistPreferences) return; PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); }
        public void Click()
        { if (SoundEnabled && !suspended && isActiveAndEnabled) SoundSource.PlayOneShot(clickClip); }
        private void Apply()
        {
            if (MusicSource == null) return;
            MusicSource.mute = !MusicEnabled; SoundSource.mute = !SoundEnabled;
            if (MusicEnabled && !suspended && isActiveAndEnabled)
            { if (!MusicSource.isPlaying) { MusicSource.UnPause(); if (!MusicSource.isPlaying) MusicSource.Play(); } }
            else MusicSource.Pause();
        }
        private void OnApplicationPause(bool paused) { suspended = paused; Apply(); }
        private void OnApplicationFocus(bool focused) { suspended = !focused; Apply(); }
        private void OnEnable() { Apply(); }
        private void OnDisable() { if (MusicSource) MusicSource.Pause(); if (SoundSource) SoundSource.Stop(); }
        private void OnDestroy() { if (musicClip) Destroy(musicClip); if (clickClip) Destroy(clickClip); }
        private static AudioClip BuildClick()
        {
            const int rate = 22050; var samples = new float[1764];
            for (var i = 0; i < samples.Length; i++)
            { var t = i / (float)rate; samples[i] = .35f * Mathf.Sin(2 * Mathf.PI * (840 - t * 900) * t) * Mathf.Exp(-t * 65) * Mathf.Min(1, t * 600); }
            var clip = AudioClip.Create("Tidebound_UI_Chime", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private static AudioClip BuildMusic()
        {
            const int rate = 22050; const float beat = .6f;
            var notes = new[] { 60, 64, 67, 72, 69, 67, 64, 62, 57, 60, 64, 69, 67, 64, 62, 64, 53, 57, 60, 65, 64, 60, 57, 60, 55, 59, 62, 67, 65, 62, 59, 55 };
            var samples = new float[Mathf.RoundToInt(rate * beat * notes.Length)];
            for (var n = 0; n < notes.Length; n++)
            {
                var frequency = 440 * Mathf.Pow(2, (notes[n] - 69) / 12f);
                for (var i = 0; i < Mathf.RoundToInt(rate * beat); i++)
                {
                    var t = i / (float)rate; var envelope = Mathf.Min(1, t / .018f) * Mathf.Exp(-t * 6) * Mathf.Clamp01((beat - t) / .04f);
                    var wave = Mathf.Sin(2 * Mathf.PI * frequency * t) + .18f * Mathf.Sin(4 * Mathf.PI * frequency * t);
                    samples[Mathf.RoundToInt(n * rate * beat) + i] = .38f * envelope * wave;
                }
            }
            var clip = AudioClip.Create("Tidebound_Harbour_Theme", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
    }
}

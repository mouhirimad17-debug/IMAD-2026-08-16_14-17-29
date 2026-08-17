using System.Collections.Generic;
using PrankMansion.UI;
using UnityEngine;

namespace PrankMansion.Systems
{
    /// <summary>
    /// Part 13.3's volume mix, applied for real: every AudioSource that plays
    /// music or SFX anywhere in the project registers here once, and every
    /// registered source's volume is kept in sync with PlayerProfile.MusicVolume/
    /// SfxVolume - including LIVE, while SettingsPanel's sliders are being
    /// dragged, the same "no restart needed" spirit Stage 17's LocalizationManager
    /// already established for text. Before this stage, MusicVolume/SfxVolume were
    /// only ever written to PlayerPrefs and read back by the slider itself - no
    /// actual AudioSource ever applied them (see Stage18 decisions log, item 1).
    ///
    /// baseVolume lets an individual source keep its own already-tuned relative
    /// level (e.g. PlayerCarry's wind loop at "منخفض نسبياً" 0.25) while still
    /// scaling with the user's slider - final volume = slider * baseVolume.
    /// </summary>
    public static class AudioService
    {
        private struct Entry { public AudioSource source; public float baseVolume; }

        private static readonly List<Entry> musicSources = new List<Entry>();
        private static readonly List<Entry> sfxSources = new List<Entry>();

        public static void RegisterMusic(AudioSource source, float baseVolume = 1f)
        {
            if (source == null) return;
            musicSources.Add(new Entry { source = source, baseVolume = baseVolume });
            source.volume = PlayerProfile.MusicVolume * baseVolume;
        }

        public static void RegisterSfx(AudioSource source, float baseVolume = 1f)
        {
            if (source == null) return;
            sfxSources.Add(new Entry { source = source, baseVolume = baseVolume });
            source.volume = PlayerProfile.SfxVolume * baseVolume;
        }

        // One-shot convenience for transient sources that don't need live-slider
        // tracking (a burst that's already over well before anyone could react to
        // a mid-playback slider drag) - applies the CURRENT sfx level at the
        // moment of playback instead.
        public static void PlayOneShotSfx(AudioSource source, AudioClip clip, float baseVolume = 1f)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip, PlayerProfile.SfxVolume * baseVolume);
        }

        public static void SetMusicVolume(float value)
        {
            PlayerProfile.MusicVolume = value;
            Apply(musicSources, PlayerProfile.MusicVolume);
        }

        public static void SetSfxVolume(float value)
        {
            PlayerProfile.SfxVolume = value;
            Apply(sfxSources, PlayerProfile.SfxVolume);
        }

        private static void Apply(List<Entry> list, float sliderValue)
        {
            list.RemoveAll(e => e.source == null); // scene/object may have been destroyed since registering
            foreach (var e in list) e.source.volume = sliderValue * e.baseVolume;
        }

        // Test-only: registered-source counts aren't otherwise observable, and
        // Play Mode tests create/destroy many throwaway AudioSources across runs.
        public static void ResetForTesting()
        {
            musicSources.Clear();
            sfxSources.Clear();
        }
    }
}

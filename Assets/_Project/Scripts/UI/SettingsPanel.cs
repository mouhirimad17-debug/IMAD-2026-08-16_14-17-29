using System;
using PrankMansion.Localization;
using PrankMansion.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// Part 11.7's settings screen: language toggle, switch-character shortcut,
    /// separate music/SFX volume sliders (0-100%), and a fixed informational line
    /// about Steam handling voice chat (not editable from here at all). The
    /// language buttons go through LocalizationManager.SetLanguage (Part 12.2)
    /// so this screen - and every other currently-visible screen - re-renders
    /// live without a restart.
    public class SettingsPanel : MonoBehaviour
    {
        public event Action OnSwitchCharacterRequested;
        public event Action OnBack;
        public event Action OnCreditsRequested;

        public Slider MusicSlider { get; private set; }
        public Slider SfxSlider { get; private set; }

        public void BuildUI()
        {
            var canvas = UIBuilder.CreateScreenCanvas("SettingsCanvas", transform);

            var english = UIBuilder.CreateLocalizedButton(canvas.transform, "EnglishButton", "lang.english", new Color(0.3f, 0.3f, 0.3f), Color.white);
            english.onClick.AddListener(() => LocalizationManager.SetLanguage(GameLanguage.English));
            var darija = UIBuilder.CreateLocalizedButton(canvas.transform, "DarijaButton", "lang.darija", new Color(0.3f, 0.3f, 0.3f), Color.white);
            darija.onClick.AddListener(() => LocalizationManager.SetLanguage(GameLanguage.Darija));

            var switchCharacter = UIBuilder.CreateLocalizedButton(canvas.transform, "SwitchCharacterButton", "menu.switchcharacter", new Color(0.5f, 0.4f, 0.8f), Color.white);
            switchCharacter.onClick.AddListener(() => OnSwitchCharacterRequested?.Invoke());

            MusicSlider = CreateSlider(canvas.transform, "MusicVolumeSlider", PlayerProfile.MusicVolume, AudioService.SetMusicVolume);
            SfxSlider = CreateSlider(canvas.transform, "SfxVolumeSlider", PlayerProfile.SfxVolume, AudioService.SetSfxVolume);

            UIBuilder.CreateLocalizedText(canvas.transform, "VoiceChatInfo", "settings.voicechatinfo", 14, new Color(0.7f, 0.7f, 0.7f));

            // Part 19.3: "يمكن الوصول إليها من القائمة الرئيسية (أو من شاشة
            // الإعدادات)" - reached from here so Part 11.3's fixed five-button
            // main-menu order stays untouched.
            var credits = UIBuilder.CreateLocalizedButton(canvas.transform, "CreditsButton", "credits.title", new Color(0.4f, 0.4f, 0.4f), Color.white);
            credits.onClick.AddListener(() => OnCreditsRequested?.Invoke());

            var back = UIBuilder.CreateLocalizedButton(canvas.transform, "BackButton", "settings.back", new Color(0.4f, 0.4f, 0.4f), Color.white);
            back.onClick.AddListener(() => OnBack?.Invoke());
        }

        private Slider CreateSlider(Transform parent, string name, float initialValue, Action<float> onChanged)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initialValue;
            slider.onValueChanged.AddListener(v => onChanged(v));
            return slider;
        }
    }
}

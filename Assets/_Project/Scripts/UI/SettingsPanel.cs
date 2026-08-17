using System;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// Part 11.7's settings screen: language toggle, switch-character shortcut,
    /// separate music/SFX volume sliders (0-100%), and a fixed informational line
    /// about Steam handling voice chat (not editable from here at all).
    public class SettingsPanel : MonoBehaviour
    {
        public event Action OnSwitchCharacterRequested;
        public event Action OnBack;

        public Slider MusicSlider { get; private set; }
        public Slider SfxSlider { get; private set; }

        public void BuildUI()
        {
            var canvas = UIBuilder.CreateScreenCanvas("SettingsCanvas", transform);

            var english = UIBuilder.CreateButton(canvas.transform, "EnglishButton", "English", new Color(0.3f, 0.3f, 0.3f), Color.white);
            english.onClick.AddListener(() => PlayerProfile.Language = GameLanguage.English);
            var darija = UIBuilder.CreateButton(canvas.transform, "DarijaButton", "الدارجة", new Color(0.3f, 0.3f, 0.3f), Color.white);
            darija.onClick.AddListener(() => PlayerProfile.Language = GameLanguage.Darija);

            var switchCharacter = UIBuilder.CreateButton(canvas.transform, "SwitchCharacterButton", "Switch Character", new Color(0.5f, 0.4f, 0.8f), Color.white);
            switchCharacter.onClick.AddListener(() => OnSwitchCharacterRequested?.Invoke());

            MusicSlider = CreateSlider(canvas.transform, "MusicVolumeSlider", PlayerProfile.MusicVolume, v => PlayerProfile.MusicVolume = v);
            SfxSlider = CreateSlider(canvas.transform, "SfxVolumeSlider", PlayerProfile.SfxVolume, v => PlayerProfile.SfxVolume = v);

            UIBuilder.CreateText(canvas.transform, "VoiceChatInfo",
                "Voice chat runs entirely through Steam. Muting or adjusting a specific friend's volume is managed from the Steam overlay, not here.",
                14, new Color(0.7f, 0.7f, 0.7f));

            var back = UIBuilder.CreateButton(canvas.transform, "BackButton", "Back", new Color(0.4f, 0.4f, 0.4f), Color.white);
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

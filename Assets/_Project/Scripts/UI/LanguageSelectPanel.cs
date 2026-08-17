using System;
using PrankMansion.Localization;
using UnityEngine;

namespace PrankMansion.UI
{
    /// <summary>
    /// Part 11.1's first sub-screen: two equal, undifferentiated buttons (English /
    /// Darija). No separate continue button - pressing either applies the language
    /// immediately and advances. Part 12 (Stage 17) now re-renders every screen's
    /// own text live the moment a language is chosen, via LocalizationManager.
    /// </summary>
    public class LanguageSelectPanel : MonoBehaviour
    {
        public event Action<GameLanguage> OnLanguageChosen;

        public void BuildUI()
        {
            var canvas = UIBuilder.CreateScreenCanvas("LanguageSelectCanvas", transform);

            UIBuilder.CreateLocalizedText(canvas.transform, "Logo", "lang.title", 64, Color.white);

            var english = UIBuilder.CreateLocalizedButton(canvas.transform, "EnglishButton", "lang.english", new Color(0.2f, 0.4f, 0.9f), Color.white);
            english.onClick.AddListener(() => Choose(GameLanguage.English));

            var darija = UIBuilder.CreateLocalizedButton(canvas.transform, "DarijaButton", "lang.darija", new Color(0.2f, 0.4f, 0.9f), Color.white);
            darija.onClick.AddListener(() => Choose(GameLanguage.Darija));
        }

        public void Choose(GameLanguage language)
        {
            LocalizationManager.SetLanguage(language);
            OnLanguageChosen?.Invoke(language);
        }
    }
}

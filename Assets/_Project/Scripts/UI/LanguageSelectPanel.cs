using System;
using UnityEngine;

namespace PrankMansion.UI
{
    /// <summary>
    /// Part 11.1's first sub-screen: two equal, undifferentiated buttons (English /
    /// Darija). No separate continue button - pressing either applies the language
    /// immediately and advances. Actual text re-rendering in the chosen language is
    /// Part 12's job (Stage 17, not built yet) - this stage only stores the choice
    /// and fires the flow event; every screen's own text stays English for now,
    /// same deferral pattern as the rest of this stage's still-missing systems.
    /// </summary>
    public class LanguageSelectPanel : MonoBehaviour
    {
        public event Action<GameLanguage> OnLanguageChosen;

        public void BuildUI()
        {
            var canvas = UIBuilder.CreateScreenCanvas("LanguageSelectCanvas", transform);

            UIBuilder.CreateText(canvas.transform, "Logo", "PRANK MANSION", 64, Color.white);

            var english = UIBuilder.CreateButton(canvas.transform, "EnglishButton", "English", new Color(0.2f, 0.4f, 0.9f), Color.white);
            english.onClick.AddListener(() => Choose(GameLanguage.English));

            var darija = UIBuilder.CreateButton(canvas.transform, "DarijaButton", "الدارجة", new Color(0.2f, 0.4f, 0.9f), Color.white);
            darija.onClick.AddListener(() => Choose(GameLanguage.Darija));
        }

        public void Choose(GameLanguage language)
        {
            PlayerProfile.Language = language;
            OnLanguageChosen?.Invoke(language);
        }
    }
}

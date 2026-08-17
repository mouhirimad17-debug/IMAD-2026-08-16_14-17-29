using System;
using UnityEngine;

namespace PrankMansion.UI
{
    /// <summary>
    /// Part 19.3's simple credits screen: developer name, plain text, no visual
    /// complexity. DECISION: no asset-store package with its own license-required
    /// attribution has been imported into this project by name anywhere in the
    /// document, so only the "developed by" line is shown - a future asset that
    /// requires credit can add its own line here without restructuring the screen.
    /// </summary>
    public class CreditsPanel : MonoBehaviour
    {
        public event Action OnBack;

        public void BuildUI()
        {
            var canvas = UIBuilder.CreateScreenCanvas("CreditsCanvas", transform);

            UIBuilder.CreateLocalizedText(canvas.transform, "Title", "credits.title", 40, Color.white);

            UIBuilder.CreateLocalizedText(canvas.transform, "DeveloperLabel", "credits.developedby", 20, new Color(0.8f, 0.8f, 0.8f));

            // Proper noun - never translated, same treatment as character display names (Part 12.3).
            UIBuilder.CreateText(canvas.transform, "DeveloperName", "Imad", 24, Color.white);

            var back = UIBuilder.CreateLocalizedButton(canvas.transform, "BackButton", "settings.back", new Color(0.4f, 0.4f, 0.4f), Color.white);
            back.onClick.AddListener(() => OnBack?.Invoke());
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.Localization
{
    /// <summary>
    /// Binds a single UnityEngine.UI.Text to a StringTable key (or a format key
    /// with args, for the room-name/score-line style composed strings). Refreshes
    /// itself on LocalizationManager.OnLanguageChanged so Part 12.2's "كل عنصر
    /// نصي مرئي حالياً على الشاشة يُحدَّث فوراً" holds for every screen without
    /// each panel hand-wiring its own subscription/unsubscription.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        private Text label;
        private string key;
        private object[] formatArgs;
        private TextAnchor baseAnchor;

        public string Key => key;

        public static LocalizedText Bind(Text label, string key, TextAnchor baseAnchor)
        {
            var lt = label.gameObject.AddComponent<LocalizedText>();
            lt.label = label;
            lt.baseAnchor = baseAnchor;
            lt.SetKey(key);
            return lt;
        }

        public void SetKey(string newKey, params object[] args)
        {
            key = newKey;
            formatArgs = args;
            Refresh();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => LocalizationManager.OnLanguageChanged -= Refresh;

        private void Refresh()
        {
            if (label == null || string.IsNullOrEmpty(key)) return;
            label.text = formatArgs != null && formatArgs.Length > 0
                ? LocalizationManager.Format(key, formatArgs)
                : LocalizationManager.Get(key);
            label.font = LocalizationManager.GetFont();
            label.alignment = LocalizationManager.MirrorAnchor(baseAnchor);
        }
    }
}

using System;
using PrankMansion.UI;
using UnityEngine;

namespace PrankMansion.Localization
{
    /// <summary>
    /// Part 12's runtime language authority. PlayerProfile.Language (Stage 16)
    /// remains the persisted value; this class is the sole place that CHANGES it,
    /// so every change reliably fires OnLanguageChanged and every currently-built
    /// LocalizedText refreshes immediately in place - Part 12.2's "كل عنصر نصي
    /// مرئي حالياً على الشاشة يُحدَّث فوراً بالكامل ... بلا أي حاجة لإعادة تشغيل
    /// اللعبة". Also the single place font and reading-direction decisions are
    /// made for Part 12.2's RTL/font-swap requirement.
    /// </summary>
    public static class LocalizationManager
    {
        public static event Action OnLanguageChanged;

        public static GameLanguage CurrentLanguage => PlayerProfile.Language;

        // Part 12.2: "اتجاه تخطيط الواجهة بالكامل: يتحول تلقائياً ... عند اختيار
        // الدارجة". Darija is the only RTL language this project supports.
        public static bool IsRTL => CurrentLanguage == GameLanguage.Darija;

        public static void SetLanguage(GameLanguage language)
        {
            bool changed = PlayerProfile.Language != language;
            PlayerProfile.Language = language;
            if (changed) OnLanguageChanged?.Invoke();
        }

        // Called once by UIManager.Initialize so anything built before the very
        // first language choice (there is none - LanguageSelectPanel is always
        // screen one on a fresh profile) still resolves correctly, and so
        // Play Mode tests can force a refresh without going through a panel.
        public static void ForceRefresh() => OnLanguageChanged?.Invoke();

        public static string Get(string key)
        {
            if (!StringTable.Map.TryGetValue(key, out var entry))
            {
                Debug.LogWarning($"[LocalizationManager] Missing string key: {key}");
                return $"!{key}!";
            }
            return CurrentLanguage == GameLanguage.Darija ? entry.darija : entry.en;
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        // Part 12.2: "الخط المستخدم ... خطاً يدعم عرض الحروف العربية بوضوح تام
        // ... وخطاً لاتينياً ... وكلا الخطين يجب أن يكونا مُدرجين ضمن أصول
        // المشروع مسبقاً". No font asset exists under Assets/_Project/UI/Fonts
        // yet (Law 0.2 - logged to Missing_Assets_Log.txt by Stage17LocalizationSetup).
        // DECISION: until a real Arabic-script font asset is dropped in, Darija
        // falls back to an OS-installed dynamic font known to carry Arabic glyphs
        // (Segoe UI / Tahoma / Arial, in that preference order - all standard on
        // Windows). This renders correct GLYPHS but Unity's legacy uGUI Text
        // (no com.unity.textmeshpro/Arabic-shaping package installed either) does
        // not perform Arabic contextual letter-joining or bidi reordering, so
        // Darija text displays as isolated letter forms rather than properly
        // joined script - a real, known limitation to resolve when a shaping-
        // capable font/plugin is added, not something this stage can fix with
        // engine built-ins alone.
        private static Font cachedArabicFont;
        private static readonly string[] ArabicFallbackFontNames = { "Segoe UI", "Tahoma", "Arial" };

        public static Font GetFont()
        {
            if (CurrentLanguage != GameLanguage.Darija) return UIBuilder.BuiltinFont;

            if (cachedArabicFont == null)
            {
                foreach (var name in ArabicFallbackFontNames)
                {
                    cachedArabicFont = Font.CreateDynamicFontFromOSFont(name, 24);
                    if (cachedArabicFont != null) break;
                }
                if (cachedArabicFont == null) cachedArabicFont = UIBuilder.BuiltinFont;
            }
            return cachedArabicFont;
        }

        // Part 12.2's RTL text alignment: mirrors a LEFT/RIGHT reading-direction-
        // relative anchor across the current language's direction. CENTER anchors
        // are unaffected (most of this project's still-placeholder layout uses
        // MiddleCenter, per UIBuilder.CreateText/CreateButton - see Stage17's
        // decisions log for why full spatial button-position mirroring is
        // deferred, same "logic now, pixel layout later" boundary Stage 16 drew).
        public static TextAnchor MirrorAnchor(TextAnchor anchor)
        {
            if (!IsRTL) return anchor;
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAnchor.UpperRight;
                case TextAnchor.UpperRight: return TextAnchor.UpperLeft;
                case TextAnchor.MiddleLeft: return TextAnchor.MiddleRight;
                case TextAnchor.MiddleRight: return TextAnchor.MiddleLeft;
                case TextAnchor.LowerLeft: return TextAnchor.LowerRight;
                case TextAnchor.LowerRight: return TextAnchor.LowerLeft;
                default: return anchor;
            }
        }
    }
}

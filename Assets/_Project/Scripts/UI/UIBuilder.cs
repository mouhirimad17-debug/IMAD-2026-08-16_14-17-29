using PrankMansion.Localization;
using PrankMansion.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// <summary>
    /// Shared procedural-UI construction helpers. No Fonts/Icons/Backgrounds
    /// assets exist yet (Law 0.2), so every Stage 16 screen is built from plain
    /// Unity UI primitives and the engine's own built-in font - same "generate a
    /// basic placeholder now" spirit as PlaceholderAudio's tones and
    /// RenoSoundIndicatorUI's procedural triangle sprite.
    /// </summary>
    public static class UIBuilder
    {
        public static Font BuiltinFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateScreenCanvas(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = BuiltinFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color bg, Color fg, int fontSize = 24)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var button = go.AddComponent<Button>();

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = BuiltinFont;
            text.text = label;
            text.fontSize = fontSize;
            text.color = fg;
            text.alignment = TextAnchor.MiddleCenter;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Part 13.1: "أصوات واجهة المستخدم العامة (نقر الأزرار ...) تُشغَّل
            // عند كل تفاعل مع أي عنصر واجهة تفاعلي في أي مكان من المشروع" - a
            // single insertion point here covers every button across the whole
            // project, localized or not (CreateLocalizedButton calls this).
            var clickAudio = go.AddComponent<AudioSource>();
            clickAudio.playOnAwake = false;
            clickAudio.spatialBlend = 0f; // UI sound - not positional
            button.onClick.AddListener(() => AudioService.PlayOneShotSfx(clickAudio, GetUiClickClip()));

            return button;
        }

        private static AudioClip uiClickClip;
        private static AudioClip GetUiClickClip() => uiClickClip ??= PlaceholderAudio.GenerateTone("Placeholder_UIClick", 700f, 0.08f, 0.25f);

        // --- Part 12: localized variants -----------------------------------
        // Same visuals as CreateText/CreateButton above, but the label is bound
        // to a StringTable key via LocalizedText instead of a literal string, so
        // it re-renders live on LocalizationManager.OnLanguageChanged (Part
        // 12.2). Use these for every player-facing label; keep the plain
        // CreateText/CreateButton above only for content that Part 12.3
        // explicitly excludes from translation (player names, room names) or
        // purely numeric display (scores, timers).
        public static Text CreateLocalizedText(Transform parent, string name, string key, int fontSize, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var text = CreateText(parent, name, "", fontSize, color);
            text.alignment = anchor;
            LocalizedText.Bind(text, key, anchor);
            return text;
        }

        public static Button CreateLocalizedButton(Transform parent, string name, string key, Color bg, Color fg, int fontSize = 24)
        {
            var button = CreateButton(parent, name, "", bg, fg, fontSize);
            var label = button.transform.Find("Label").GetComponent<Text>();
            LocalizedText.Bind(label, key, TextAnchor.MiddleCenter);
            return button;
        }

        public static InputField CreateInputField(Transform parent, string name, int charLimit)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var input = go.AddComponent<InputField>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            // Part 12.2 RTL: alignment reflects the language active at BUILD
            // time. Not live-updating like LocalizedText - this project's flow
            // never shows a language switch and a text-entry field on the same
            // built screen at once (LanguageSelect/Settings are separate screens
            // from NameEntry/CreateRoom/JoinRoom), so a rebuild-on-next-open is
            // sufficient, same "logic now, live-mid-screen edge case deferred"
            // boundary as the rest of this stage's layout work.
            text.font = LocalizationManager.GetFont();
            text.fontSize = 22;
            text.color = Color.black;
            text.alignment = LocalizationManager.MirrorAnchor(TextAnchor.MiddleLeft);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            input.textComponent = text;
            input.characterLimit = charLimit;
            return input;
        }
    }
}

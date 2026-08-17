using System.IO;
using UnityEditor;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 17 (Part 12, full two-language system). Like Stage 16, there is no
    /// asset import or scene/prefab wiring here - the StringTable/LocalizationManager
    /// live entirely in code and every screen is still built procedurally at
    /// runtime, so this setup script's job is the missing-asset log (Law 0.2) and
    /// the decisions log, with BuildAndTest running Stage17LocalizationTest in
    /// Play Mode.
    /// </summary>
    public static class Stage17LocalizationSetup
    {
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage17_Decisions_Log.txt";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";

        [MenuItem("PrankMansion/Stage 17 - Build Localization Decisions Log")]
        public static void BuildLocalizationSystem()
        {
            WriteMissingAssetsLog();
            WriteDecisionsLog();
            Debug.Log("[Stage17LocalizationSetup] Logs written. Localization is built entirely in code (StringTable/LocalizationManager) - no scene/prefab changes needed.");
        }

        [MenuItem("PrankMansion/Stage 17 - Build And Run Localization Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildLocalizationSystem();

            var testGo = new GameObject("Stage17_LocalizationTestRunner");
            testGo.AddComponent<Stage17LocalizationTest>();

            Debug.Log("[Stage17LocalizationSetup] Entering Play Mode to run localization system test...");
            EditorApplication.isPlaying = true;
        }

        // Law 0.2, step 2b: Part 12.2 requires "خطاً يدعم عرض الحروف العربية
        // بوضوح تام" and "كلا الخطين يجب أن يكونا مُدرجين ضمن أصول المشروع
        // مسبقاً" - no font asset exists under Assets/_Project/UI/Fonts at all
        // (still empty since Stage 16). Logged here rather than silently
        // substituting forever; LocalizationManager.GetFont's OS-dynamic-font
        // fallback keeps the system functional and testable in the meantime.
        private static void WriteMissingAssetsLog()
        {
            var dir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var line = $"{System.DateTime.Now:yyyy-MM-dd} | Assets/_Project/UI/Fonts/ (English + Arabic-script font assets) | " +
                       "Part 12.2 requires both a Latin font and an Arabic-script-capable font already present in project assets; " +
                       "neither exists yet. LocalizationManager.GetFont() falls back to an OS dynamic font (Segoe UI/Tahoma/Arial) " +
                       "for Darija in the meantime - correct glyphs, but no Arabic contextual letter-joining/bidi shaping without a " +
                       "real font asset or shaping plugin.";
            File.AppendAllText(MissingAssetsLogPath, line + System.Environment.NewLine);
        }

        private static void WriteDecisionsLog()
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new[]
            {
                "=== Stage 17 - Two-Language System - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. Part 12.1 asks for \"نظام الترجمة الرسمي المتكامل مع بيئة تطوير Unity\"",
                "   (Unity's official localization system). com.unity.localization is NOT in",
                "   Packages/manifest.json, and this stage has no live Editor/internet session",
                "   available to add and resolve a new package mid-stage safely. StringTable.cs",
                "   implements the same architecture - one key, two language columns (English/",
                "   Darija in real Arabic script), no literal UI text anywhere - as a static",
                "   code table instead of a String Table asset, matching how CharacterProfile",
                "   (Part 5) and MansionSpec (Part 3) already store their own central data",
                "   directly in code rather than as external assets. LocalizationManager.Get is",
                "   the sole read path, so swapping the backing store later is contained.",
                "",
                "2. No Arabic-script font asset exists (Law 0.2, logged to",
                "   Missing_Assets_Log.txt). LocalizationManager.GetFont() falls back to an OS",
                "   dynamic font (Font.CreateDynamicFontFromOSFont, trying Segoe UI / Tahoma /",
                "   Arial in order) for Darija. This renders correct Arabic GLYPHS, but neither",
                "   Unity's legacy uGUI Text nor this fallback perform Arabic contextual letter-",
                "   joining or bidi reordering (no com.unity.textmeshpro/shaping package",
                "   installed either) - Darija text displays as isolated letter forms rather",
                "   than properly joined script until a real shaping-capable font/plugin is",
                "   added. This is a genuine, currently-unresolved product-quality gap, not a",
                "   hidden one - flagged explicitly rather than silently shipped as 'done'.",
                "",
                "3. Part 12.2's RTL requirement is implemented as: LocalizationManager.IsRTL",
                "   flag, MirrorAnchor() flipping Left/Right-relative TextAnchors, and",
                "   CreateInputField's text alignment reading it at build time. Full spatial",
                "   mirroring of button/panel POSITIONS is not implemented, because Stage 16",
                "   never gave any UI element a real position in the first place - every",
                "   procedurally-built element still uses Unity's default centered/stacked",
                "   RectTransform (Stage 16's own decisions log already deferred real layout",
                "   art to a later pass, since Assets/_Project/UI/Backgrounds and Icons are",
                "   still empty). There is no button ORDER to mirror yet; the alignment/",
                "   direction plumbing is in place so a real layout pass can honor IsRTL",
                "   correctly once one exists.",
                "",
                "4. CreateInputField's RTL alignment is set once at BUILD time, not live via",
                "   LocalizedText's OnLanguageChanged subscription like every other label -",
                "   this project's flow never shows a language switch (LanguageSelect/Settings)",
                "   and a text-entry field (NameEntry/CreateRoom/JoinRoom) on the same already-",
                "   built screen at once, so a refresh on next open is sufficient.",
                "",
                "5. Part 12.3's host-disconnect error message ('انقطاع اتصال المضيف') has a",
                "   StringTable key (error.hostdisconnected) but no UI screen currently",
                "   surfaces it - Stage 15/16 never built disconnect-notification UI in the",
                "   first place (HostMigrationController is pure logic, no user-facing text).",
                "   The key exists so the string is ready the moment that screen is built.",
                "",
                "6. GameplayHUD (Part 11.5) has no text elements to translate beyond numeric",
                "   score/timer digits - it was already built with team identity conveyed by",
                "   color (Part 9.5.2), not text labels, so there is nothing for Stage 17 to",
                "   change there. PlayerNameplate's display name and every room-name/player-",
                "   name field across every screen stay literal per Part 12.3's explicit",
                "   exclusion (\"اسم اللاعب الشخصي، واسم الغرفة ... حران تماماً\").",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}

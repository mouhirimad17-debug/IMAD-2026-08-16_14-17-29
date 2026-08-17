using UnityEngine;

namespace PrankMansion.UI
{
    public enum GameLanguage { English, Darija }

    /// <summary>
    /// Part 11.1's locally-persisted first-launch data ("يُحفظ محلياً بشكل دائم").
    /// A single global profile (this device's own player), matching how only one
    /// person plays on one client at a time.
    /// </summary>
    public static class PlayerProfile
    {
        private const string KeyHasProfile = "PM_HasProfile";
        private const string KeyName = "PM_PlayerName";
        private const string KeyCharacter = "PM_SelectedCharacter";
        private const string KeyLanguage = "PM_Language";
        private const string KeyMusicVolume = "PM_MusicVolume";
        private const string KeySfxVolume = "PM_SfxVolume";
        private const string KeyLocalId = "PM_LocalPlayerId";

        public const int MaxNameLength = 15; // Part 11.1: "بحد أقصى خمسة عشر حرفاً"

        public static bool HasSavedProfile => PlayerPrefs.GetInt(KeyHasProfile, 0) == 1;

        public static string PlayerName
        {
            get => PlayerPrefs.GetString(KeyName, "");
            set => PlayerPrefs.SetString(KeyName, value);
        }

        public static int SelectedCharacterIndex
        {
            get => PlayerPrefs.GetInt(KeyCharacter, -1);
            set => PlayerPrefs.SetInt(KeyCharacter, value);
        }

        public static GameLanguage Language
        {
            get => (GameLanguage)PlayerPrefs.GetInt(KeyLanguage, (int)GameLanguage.English);
            set => PlayerPrefs.SetInt(KeyLanguage, (int)value);
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(KeyMusicVolume, 1f);
            set => PlayerPrefs.SetFloat(KeyMusicVolume, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(KeySfxVolume, 1f);
            set => PlayerPrefs.SetFloat(KeySfxVolume, Mathf.Clamp01(value));
        }

        // DECISION: no real Steam ID exists yet (Stage 15 deferred Steamworks.NET).
        // A locally-generated, persisted GUID stands in as this device's unique
        // lobby identity until then - LobbyManager.TryCreateRoom/TryJoinRoom need
        // SOME stable per-client id distinct from the (freely user-editable, not
        // guaranteed unique) display name.
        public static string LocalPlayerId
        {
            get
            {
                string id = PlayerPrefs.GetString(KeyLocalId, "");
                if (string.IsNullOrEmpty(id))
                {
                    id = System.Guid.NewGuid().ToString();
                    PlayerPrefs.SetString(KeyLocalId, id);
                }
                return id;
            }
        }

        public static void MarkFirstLaunchComplete()
        {
            PlayerPrefs.SetInt(KeyHasProfile, 1);
            PlayerPrefs.Save();
        }

        // Test-only reset hook - PlayerPrefs persists on disk across Play Mode
        // sessions, so Stage16UITest needs a clean slate to test both the
        // first-launch and repeat-launch paths deterministically.
        public static void ResetForTesting()
        {
            PlayerPrefs.DeleteKey(KeyHasProfile);
            PlayerPrefs.DeleteKey(KeyName);
            PlayerPrefs.DeleteKey(KeyCharacter);
            PlayerPrefs.DeleteKey(KeyLanguage);
            PlayerPrefs.DeleteKey(KeyMusicVolume);
            PlayerPrefs.DeleteKey(KeySfxVolume);
        }

        public static bool IsValidName(string name) =>
            !string.IsNullOrEmpty(name) && name.Length <= MaxNameLength && !ContainsDisruptiveSymbols(name);

        // Part 11.1: "أحرف عربية أو لاتينية أو أرقام مع منع الرموز الخاصة
        // التخريبية" - DECISION: no exact forbidden-character list is given;
        // blocks control characters and the symbols most likely to break UI text
        // rendering or downstream systems (nameplates, save files, future chat-
        // adjacent display), while allowing ordinary Arabic/Latin letters, digits,
        // and spaces.
        private static bool ContainsDisruptiveSymbols(string name)
        {
            foreach (char c in name)
            {
                if (char.IsControl(c)) return true;
                if ("<>{}[]\\/|`~^".IndexOf(c) >= 0) return true;
            }
            return false;
        }
    }
}

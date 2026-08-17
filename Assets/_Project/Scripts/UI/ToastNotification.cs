using PrankMansion.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// <summary>
    /// A small reusable "short message" surface (Part 16.1's "مصحوبة برسالة
    /// قصيرة تُعلمه بحدوث هذا التصحيح التلقائي") - lazily creates one persistent
    /// screen-space label the first time anything calls Show, so any system
    /// anywhere (stuck-recovery now; future exception-handling messages later)
    /// can surface a brief, localized, auto-hiding notice without needing its own
    /// UI plumbing or a live GameplayHUD/UIManager reference.
    /// </summary>
    public class ToastNotification : MonoBehaviour
    {
        public const float DefaultDurationSeconds = 3f; // DECISION: "رسالة قصيرة" has no exact duration given

        private static ToastNotification instance;
        private Text label;
        private float hideTimer;

        public static void Show(string localizationKey, float durationSeconds = DefaultDurationSeconds)
        {
            EnsureInstance();
            instance.label.text = LocalizationManager.Get(localizationKey);
            instance.label.font = LocalizationManager.GetFont();
            instance.label.gameObject.SetActive(true);
            instance.hideTimer = durationSeconds;
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("ToastNotification");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ToastNotification>();

            var canvas = UIBuilder.CreateScreenCanvas("ToastCanvas", go.transform);
            instance.label = UIBuilder.CreateText(canvas.transform, "ToastText", "", 22, Color.white);
            instance.label.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (hideTimer <= 0f) return;
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f) label.gameObject.SetActive(false);
        }

        // Test-only: batch Play Mode tests share one process across many stages'
        // tests, so a leftover DontDestroyOnLoad toast from an earlier test must
        // not leak into a later one.
        public static void ResetForTesting()
        {
            if (instance != null) Destroy(instance.gameObject);
            instance = null;
        }
    }
}

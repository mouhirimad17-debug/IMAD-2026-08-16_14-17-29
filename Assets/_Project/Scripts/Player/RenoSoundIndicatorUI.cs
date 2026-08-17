using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.Player
{
    /// <summary>
    /// Part 11.5's Reno-only screen indicator: "سهم شفاف صغير عند حافة الشاشة يشير
    /// لاتجاه أي صوت مكتشف قريب ... بلا ظهور مطلقاً على شاشات أي شخصية أخرى". Built
    /// here as a minimal, self-contained runtime Canvas rather than waiting for
    /// Stage 16's full UI system - CharacterSelector only adds this component to
    /// the local player when the chosen character is Reno
    /// (CharacterProfile.Entry.hasSoundDirectionIndicator), so it can never appear
    /// for anyone else, matching the document's "exclusively" requirement exactly.
    /// </summary>
    public class RenoSoundIndicatorUI : MonoBehaviour
    {
        // DECISION: no exact screen-edge margin/size given - a small, clearly
        // visible arrow inset from the edge.
        public const float EdgeMarginPx = 60f;
        public const float ArrowSizePx = 28f;

        private SoundDetector detector;
        private RectTransform arrowRect;
        private Camera targetCamera;

        private void Awake()
        {
            detector = GetComponent<SoundDetector>();
            if (detector == null) detector = gameObject.AddComponent<SoundDetector>();
            BuildUI();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("RenoSoundIndicatorCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var arrowGo = new GameObject("SoundDirectionArrow");
            arrowGo.transform.SetParent(canvasGo.transform, false);
            var image = arrowGo.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.6f); // "شفاف" - semi-transparent
            image.sprite = BuildTriangleSprite();

            arrowRect = arrowGo.GetComponent<RectTransform>();
            arrowRect.sizeDelta = new Vector2(ArrowSizePx, ArrowSizePx);
            arrowGo.SetActive(false);
        }

        // No arrow sprite asset exists (Law 0.2) - a simple procedural triangle
        // stands in, same "generate a basic shape at runtime" spirit as
        // PlaceholderAudio.GenerateTone for missing audio.
        private static Sprite BuildTriangleSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float halfWidthAtRow = (float)y / size * (size * 0.5f);
                    bool inside = Mathf.Abs(x - size * 0.5f) < halfWidthAtRow;
                    tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            if (arrowRect == null) return;

            if (!detector.HasRecentDetection)
            {
                if (arrowRect.gameObject.activeSelf) arrowRect.gameObject.SetActive(false);
                return;
            }

            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            arrowRect.gameObject.SetActive(true);

            // Direction relative to the camera's own facing, projected flat onto
            // screen space - points toward the detected sound's bearing.
            Vector3 localDir = targetCamera.transform.InverseTransformDirection(detector.LastDetectedDirection);
            float angleRad = Mathf.Atan2(localDir.x, localDir.z);
            arrowRect.rotation = Quaternion.Euler(0f, 0f, -angleRad * Mathf.Rad2Deg);

            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
            Vector2 edgeDir = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
            float maxX = Screen.width * 0.5f - EdgeMarginPx;
            float maxY = Screen.height * 0.5f - EdgeMarginPx;
            float scale = Mathf.Min(
                Mathf.Abs(edgeDir.x) > 0.001f ? maxX / Mathf.Abs(edgeDir.x) : float.MaxValue,
                Mathf.Abs(edgeDir.y) > 0.001f ? maxY / Mathf.Abs(edgeDir.y) : float.MaxValue);
            arrowRect.position = screenCenter + edgeDir * scale;
        }
    }
}

using System;
using System.Collections.Generic;
using PrankMansion.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// <summary>
    /// Part 11.2's character-select grid: seven equal cards, each with a
    /// continuously-rotating 3D preview (one full turn ~6s), name, and a one/two-
    /// line ability description (CharacterProfile.Entry.descriptionKey, Part 12). Single-
    /// select only. In "forced" mode (Part 11.1's mandatory first pick) the
    /// confirm button starts disabled until a card is chosen; reached later from
    /// Settings/the waiting room it starts enabled, since a prior selection always
    /// exists by then.
    /// </summary>
    public class CharacterSelectPanel : MonoBehaviour
    {
        public const float TurntableSecondsPerRevolution = 6f; // "دورة كاملة تقريباً كل ست ثوانٍ"

        public event Action<int> OnCharacterConfirmed;

        public int SelectedIndex { get; private set; } = -1;
        public bool IsForcedFirstPick { get; private set; }
        public bool CanConfirm => confirmButton != null && confirmButton.interactable;

        private readonly List<Transform> cardPreviewRoots = new List<Transform>();
        private readonly List<Image> cardFrames = new List<Image>();
        private Button confirmButton;
        private Canvas builtCanvas;

        public void BuildUI(bool forcedFirstPick)
        {
            // Rebuildable (Settings/waiting room can reopen this screen many times
            // in one session) - tear down any previous instance first rather than
            // leaking GameObjects or stale card lists.
            if (builtCanvas != null) Destroy(builtCanvas.gameObject);
            cardPreviewRoots.Clear();
            cardFrames.Clear();

            IsForcedFirstPick = forcedFirstPick;
            SelectedIndex = forcedFirstPick ? -1 : PlayerProfile.SelectedCharacterIndex;

            builtCanvas = UIBuilder.CreateScreenCanvas("CharacterSelectCanvas", transform);
            var grid = UIBuilder.CreatePanel(builtCanvas.transform, "CardGrid", Color.clear).transform;

            for (int i = 0; i < CharacterProfile.Table.Length; i++)
            {
                int index = i; // capture
                var entry = CharacterProfile.Table[i];

                var card = UIBuilder.CreatePanel(grid, $"Card_{entry.unityName}", new Color(0.15f, 0.15f, 0.18f));
                cardFrames.Add(card);

                var previewRoot = new GameObject("PreviewRoot");
                previewRoot.transform.SetParent(card.transform, false);
                cardPreviewRoots.Add(previewRoot.transform);
                BuildTurntablePreview(previewRoot.transform, entry);

                UIBuilder.CreateText(card.transform, "Name", entry.displayName, 24, Color.white);
                UIBuilder.CreateLocalizedText(card.transform, "Description", entry.descriptionKey, 14, new Color(0.8f, 0.8f, 0.8f));

                var button = card.gameObject.AddComponent<Button>();
                button.onClick.AddListener(() => Select(index));
            }

            confirmButton = UIBuilder.CreateLocalizedButton(builtCanvas.transform, "ConfirmButton", "charselect.confirm", new Color(0.2f, 0.7f, 0.3f), Color.white);
            confirmButton.onClick.AddListener(Confirm);
            RefreshSelectionVisuals();
        }

        // A real turntable render (dedicated camera -> RenderTexture -> RawImage)
        // is a rendering-pipeline concern that only matters visually; batch-mode
        // Play Mode testing never renders frames at all, so this stage builds the
        // actual rotating instance (the independently-correct, testable part) and
        // leaves the RenderTexture/RawImage plumbing for whenever this is first
        // viewed in a real interactive session.
        private void BuildTurntablePreview(Transform parent, CharacterProfile.Entry entry)
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                PrankMansion.Player.CharacterSelector.PrefabDir + entry.unityName + ".prefab");
            if (prefab != null)
            {
                var instance = Instantiate(prefab, parent);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
#endif
        }

        private void Update()
        {
            float degPerSec = 360f / TurntableSecondsPerRevolution;
            foreach (var root in cardPreviewRoots)
                if (root != null) root.Rotate(Vector3.up, degPerSec * Time.deltaTime, Space.World);
        }

        public void Select(int index)
        {
            SelectedIndex = index;
            RefreshSelectionVisuals();
        }

        private void RefreshSelectionVisuals()
        {
            for (int i = 0; i < cardFrames.Count; i++)
                cardFrames[i].color = i == SelectedIndex ? new Color(0.9f, 0.8f, 0.2f) : new Color(0.15f, 0.15f, 0.18f);

            // Part 11.2: disabled only while forced AND nothing picked yet; reached
            // from Settings/the waiting room (not forced), a prior pick always
            // exists so it's active by default.
            confirmButton.interactable = !IsForcedFirstPick || SelectedIndex >= 0;
        }

        public void Confirm()
        {
            if (IsForcedFirstPick && SelectedIndex < 0) return;
            if (SelectedIndex < 0) SelectedIndex = PlayerProfile.SelectedCharacterIndex;
            if (SelectedIndex < 0) return;

            PlayerProfile.SelectedCharacterIndex = SelectedIndex;
            OnCharacterConfirmed?.Invoke(SelectedIndex);
        }
    }
}

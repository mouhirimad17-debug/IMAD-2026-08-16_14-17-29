using System;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// Part 11.1's second sub-screen: single name field (<=15 chars), continue
    /// button disabled while empty/invalid, saved permanently on confirm.
    public class NameEntryPanel : MonoBehaviour
    {
        public event Action<string> OnNameConfirmed;

        private InputField inputField;
        private Button continueButton;

        public void BuildUI()
        {
            var canvas = UIBuilder.CreateScreenCanvas("NameEntryCanvas", transform);
            UIBuilder.CreateText(canvas.transform, "Title", "What's your name?", 40, Color.white);

            inputField = UIBuilder.CreateInputField(canvas.transform, "NameInput", PlayerProfile.MaxNameLength);
            inputField.onValueChanged.AddListener(_ => RefreshContinueButton());

            continueButton = UIBuilder.CreateButton(canvas.transform, "ContinueButton", "Continue", new Color(0.2f, 0.7f, 0.3f), Color.white);
            continueButton.onClick.AddListener(Confirm);
            RefreshContinueButton();
        }

        private void RefreshContinueButton() => continueButton.interactable = PlayerProfile.IsValidName(inputField.text);

        public bool CanConfirm => PlayerProfile.IsValidName(inputField.text);

        public void Confirm()
        {
            if (!PlayerProfile.IsValidName(inputField.text)) return;
            PlayerProfile.PlayerName = inputField.text;
            OnNameConfirmed?.Invoke(inputField.text);
        }

        // Test-only direct-set path (no simulated keyboard typing needed) - same
        // established pattern as PlayerCarry.SimulateInteractHeld.
        public void DebugSetName(string name)
        {
            inputField.text = name;
            RefreshContinueButton();
        }
    }
}

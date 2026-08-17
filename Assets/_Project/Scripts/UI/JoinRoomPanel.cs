using System;
using System.Collections.Generic;
using PrankMansion.Localization;
using PrankMansion.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// <summary>
    /// Part 11.4's join-room screen: a search field over the visible room list
    /// (Part 10.3), and the "six separate boxes, not one long field" code entry
    /// with auto-advance to the next box, matching common two-factor code UIs.
    /// </summary>
    public class JoinRoomPanel : MonoBehaviour
    {
        public event Action OnRoomJoined;

        public const int CodeBoxCount = RoomCodeGenerator.CodeLength;

        private LobbyManager lobbyManager;
        private InputField searchField;
        private List<LobbyInfo> lastResults = new List<LobbyInfo>();
        private string selectedLobbyId;
        private InputField[] codeBoxes;
        private Text errorText;
        private string currentErrorKey = "";

        public List<LobbyInfo> LastResults => lastResults;
        public JoinResult LastAttemptResult { get; private set; } = JoinResult.NotFound;

        public void BuildUI(LobbyManager manager)
        {
            lobbyManager = manager;
            var canvas = UIBuilder.CreateScreenCanvas("JoinRoomCanvas", transform);

            UIBuilder.CreateLocalizedText(canvas.transform, "SearchLabel", "joinroom.searchlabel", 18, Color.white);
            searchField = UIBuilder.CreateInputField(canvas.transform, "SearchInput", 20);
            searchField.onValueChanged.AddListener(RefreshSearch);

            var codeRoot = UIBuilder.CreatePanel(canvas.transform, "CodeBoxRow", Color.clear).transform;
            codeBoxes = new InputField[CodeBoxCount];
            for (int i = 0; i < CodeBoxCount; i++)
            {
                codeBoxes[i] = UIBuilder.CreateInputField(codeRoot, $"CodeBox_{i}", 1);
                int index = i;
                codeBoxes[i].onValueChanged.AddListener(v => HandleCodeBoxChanged(index, v));
            }
            SetCodeBoxesActive(false);

            errorText = UIBuilder.CreateText(canvas.transform, "ErrorText", "", 16, new Color(0.9f, 0.3f, 0.3f));

            RefreshSearch("");
        }

        private void OnEnable() => LocalizationManager.OnLanguageChanged += RefreshErrorText;
        private void OnDisable() => LocalizationManager.OnLanguageChanged -= RefreshErrorText;

        private void SetError(string key)
        {
            currentErrorKey = key;
            RefreshErrorText();
        }

        private void RefreshErrorText()
        {
            errorText.text = string.IsNullOrEmpty(currentErrorKey) ? "" : LocalizationManager.Get(currentErrorKey);
            errorText.font = LocalizationManager.GetFont();
        }

        private void RefreshSearch(string query) => lastResults = lobbyManager.SearchRooms(query);

        // Real result rows (a scrollable list widget with click handlers) need
        // Stage 16's still-nonexistent Icons/list-view art; SelectRoom is the
        // actual selection entry point a real row's click callback would call.
        public void SelectRoom(string lobbyId)
        {
            selectedLobbyId = lobbyId;
            SetCodeBoxesActive(true);
            foreach (var box in codeBoxes) box.text = "";
            SetError("");
        }

        private void SetCodeBoxesActive(bool active)
        {
            foreach (var box in codeBoxes) box.gameObject.SetActive(active);
        }

        private void HandleCodeBoxChanged(int index, string value)
        {
            // Part 11.4: "انتقال تلقائي للخانة التالية فور كتابة حرف أو رقم فيها"
            if (!string.IsNullOrEmpty(value) && index < CodeBoxCount - 1)
                codeBoxes[index + 1].ActivateInputField();

            if (AllCodeBoxesFilled()) TryJoin();
        }

        private bool AllCodeBoxesFilled()
        {
            foreach (var box in codeBoxes)
                if (string.IsNullOrEmpty(box.text)) return false;
            return true;
        }

        public void TryJoin()
        {
            var codeChars = new char[CodeBoxCount];
            for (int i = 0; i < CodeBoxCount; i++)
                codeChars[i] = codeBoxes[i].text.Length > 0 ? codeBoxes[i].text[0] : ' ';
            string code = new string(codeChars);

            var result = lobbyManager.TryJoinRoom(selectedLobbyId, code, PlayerProfile.LocalPlayerId, PlayerProfile.PlayerName);
            LastAttemptResult = result;

            switch (result)
            {
                case JoinResult.Success:
                    SetError("");
                    OnRoomJoined?.Invoke();
                    break;
                case JoinResult.WrongCode:
                    SetError("joinroom.err.wrongcode"); // Part 10.3: "كود غير صحيح"
                    foreach (var box in codeBoxes) box.text = "";
                    break;
                case JoinResult.Full:
                    SetError("joinroom.err.full"); // Part 10.3: "الغرفة ممتلئة"
                    break;
                default:
                    SetError("joinroom.err.notfound");
                    break;
            }
        }

        // Test-only direct-set path. Setting InputField.text already fires
        // onValueChanged (same as real typing), which auto-triggers TryJoin once
        // the last box is filled - do NOT also call TryJoin here, or a
        // already-succeeded join gets immediately re-attempted and overwrites
        // LastAttemptResult with a second, misleading outcome.
        public void DebugEnterCode(string code)
        {
            for (int i = 0; i < CodeBoxCount; i++)
                codeBoxes[i].text = i < code.Length ? code[i].ToString() : "";
        }
    }
}

using PrankMansion.Player;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// <summary>
    /// Part 11.5: "فوق رأس كل شخصية ... وليس عنصر واجهة ثابت على الشاشة، بل عنصر
    /// يتحرك مع الشخصية داخل العالم" - a world-space Canvas anchored above the
    /// player's head, showing their display name on a Part 9.5.2 team-colored
    /// background, billboarded toward whichever camera is currently rendering.
    /// </summary>
    public class PlayerNameplate : MonoBehaviour
    {
        public const float HeightAboveHead = 1.3f; // DECISION: no exact height given

        private Transform target;
        public Text NameText { get; private set; }
        public Image Background { get; private set; }
        public Canvas WorldCanvas { get; private set; }

        public void BuildUI(Transform followTarget, string displayName, Team team)
        {
            target = followTarget;

            var go = new GameObject("NameplateCanvas");
            go.transform.SetParent(transform, false);
            WorldCanvas = go.AddComponent<Canvas>();
            WorldCanvas.renderMode = RenderMode.WorldSpace;
            go.transform.localScale = Vector3.one * 0.01f;

            var teamColor = PlayerTeam.GetColor(team);
            Background = UIBuilder.CreatePanel(go.transform, "Background", new Color(teamColor.r, teamColor.g, teamColor.b, 0.6f));
            NameText = UIBuilder.CreateText(go.transform, "Name", displayName, 24, Color.white);

            if (target != null) transform.position = target.position + Vector3.up * HeightAboveHead;
        }

        public void SetDisplayName(string displayName) => NameText.text = displayName;

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = target.position + Vector3.up * HeightAboveHead;
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        }
    }
}

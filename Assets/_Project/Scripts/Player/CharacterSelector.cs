using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Applies one CharacterProfile.Entry (Part 5.2's exact per-character table) onto
    /// this player: instantiates the character's own corrected, Humanoid-rigged
    /// visual model (Stage12CharacterImporter's output) as a child, wires its
    /// Animator into PlayerAnimatorDriver, and pushes every numeric stat onto the
    /// sibling components that already expose a per-character hook
    /// (PlayerLocomotion, PlayerCarry, PlayerPushInteraction, FootstepSoundEmitter,
    /// SoundDetector). Full character-SELECT UI is Stage 16's job; this is the
    /// underlying mechanism that screen will call into.
    /// </summary>
    public class CharacterSelector : MonoBehaviour
    {
        public const string PrefabDir = "Assets/_Project/Prefabs/Characters/";

        public int SelectedIndex { get; private set; } = -1;
        public GameObject VisualInstance { get; private set; }

        private PlayerLocomotion locomotion;
        private PlayerCarry carry;
        private PlayerPushInteraction pushInteraction;
        private FootstepSoundEmitter footsteps;
        private SoundDetector soundDetector;
        private PlayerAnimatorDriver animatorDriver;

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            carry = GetComponent<PlayerCarry>();
            pushInteraction = GetComponent<PlayerPushInteraction>();
            footsteps = GetComponent<FootstepSoundEmitter>();
            soundDetector = GetComponent<SoundDetector>();
            animatorDriver = GetComponent<PlayerAnimatorDriver>();
        }

        public bool SelectByIndex(int index)
        {
            if (index < 0 || index >= CharacterProfile.Table.Length) return false;
            return Apply(CharacterProfile.Table[index], index);
        }

        public bool SelectByName(string unityName)
        {
            for (int i = 0; i < CharacterProfile.Table.Length; i++)
                if (CharacterProfile.Table[i].unityName == unityName)
                    return Apply(CharacterProfile.Table[i], i);
            return false;
        }

        private bool Apply(CharacterProfile.Entry entry, int index)
        {
            if (VisualInstance != null) Destroy(VisualInstance);

            var visualPrefab = LoadVisualPrefab(entry.unityName);
            if (visualPrefab == null) return false;

            VisualInstance = Instantiate(visualPrefab, transform);
            VisualInstance.transform.localPosition = Vector3.zero;
            VisualInstance.transform.localRotation = Quaternion.identity;
            SelectedIndex = index;

            if (locomotion != null) locomotion.CharacterSpeedMultiplier = entry.speedMultiplier;

            if (carry != null)
            {
                carry.heavySpeedFactor = entry.heavyCarrySpeedFactor;
                carry.windStartDelay = entry.windStartDelaySeconds;
                carry.launchHorizontalFraction = entry.launchHorizontalFraction;
                carry.launchVerticalFraction = entry.launchVerticalFraction;
                carry.throwPowerMultiplier = entry.throwPowerMultiplier;
            }

            if (pushInteraction != null) pushInteraction.pushForceMultiplier = entry.pushForceMultiplier;
            if (footsteps != null) footsteps.audibleRange = entry.footstepAudibleRange;
            if (soundDetector != null) soundDetector.hearingRange = entry.hearingRange;

            if (entry.hasSoundDirectionIndicator && GetComponent<RenoSoundIndicatorUI>() == null)
                gameObject.AddComponent<RenoSoundIndicatorUI>();

            if (animatorDriver != null)
                animatorDriver.animator = VisualInstance.GetComponentInChildren<Animator>();

            return true;
        }

        // Runtime asset lookup by prefab path - Resources.Load can't reach
        // Assets/_Project/Prefabs directly, so this uses the same AssetDatabase path
        // convention only at Editor/import time; at real runtime a build would
        // instead reference these via a serialized array or Addressables (out of
        // this stage's scope - Stage 12 only needs this to work inside the Editor
        // for testing, same as every other stage's Play Mode verification).
        private GameObject LoadVisualPrefab(string unityName)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + unityName + ".prefab");
#else
            return null;
#endif
        }
    }
}

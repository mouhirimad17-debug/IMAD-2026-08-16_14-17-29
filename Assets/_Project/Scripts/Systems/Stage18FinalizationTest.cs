using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Localization;
using PrankMansion.Networking;
using PrankMansion.Player;
using PrankMansion.Systems;
using PrankMansion.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode verification for Stage 18 (Parts 13, 15, 16, 18, 19): the audio
    /// mix's live volume application, every newly-wired sound event's structural
    /// hookup, Law 0.5's camera shake, the round's background music lifecycle,
    /// Part 15.2's idle-sleep optimization, all five Part 16 exception-handling
    /// protocols, and the Part 19.3 credits screen. Items that can't be
    /// meaningfully automated (the Occlusion Culling bake, and the network tick
    /// rate with no NetworkManager scene yet) are documented, deliberate
    /// exclusions - see Stage18_Decisions_Log.txt.
    /// </summary>
    public class Stage18FinalizationTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage18_DynamicTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            report.AppendLine("=== Stage 18 - Dynamic Finalization Test (Parts 13/15/16/18/19, Play Mode) ===");
            report.AppendLine();

            TestAudioServiceVolumeApplication();
            TestUiClickSoundWiring();
            TestMaterialImpactSoundLibrary();
            TestCollidablePropsHaveMaterialSoundAndOutOfBoundsWiring();
            TestCameraShake();
            yield return TestBackgroundMusicStartsStopsWithRound();
            yield return TestCarryableObjectIdleSleep();
            yield return TestStuckDetectionRecovers();
            yield return TestOutOfBoundsRecovery();
            TestInvalidActionGuardsAlreadyCompliant();
            TestDuplicateRoomNameHostDisambiguation();
            TestCreditsPanelLocalization();
            yield return TestRopeTieSound();

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 18 finalization matches Parts 13/15/16/18/19 end to end."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage18DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage18DynamicTest] DONE");

            AudioService.ResetForTesting();
            ToastNotification.ResetForTesting();
            LocalizationManager.SetLanguage(GameLanguage.English);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
                UnityEditor.EditorApplication.Exit(passed == total ? 0 : 1);
#endif
        }

        private void Check(string name, bool ok, string detail)
        {
            total++;
            if (ok) passed++;
            report.AppendLine($"[{(ok ? "PASS" : "FAIL")}] {name} -> {detail}");
        }

        // ---------------------------------------------------------------
        private void TestAudioServiceVolumeApplication()
        {
            AudioService.ResetForTesting();
            PlayerProfile.ResetForTesting();

            var musicGo = new GameObject("Stage18Test_MusicSource");
            var musicSource = musicGo.AddComponent<AudioSource>();
            AudioService.RegisterMusic(musicSource, 0.5f);
            Check("Registering a music source applies the current slider * its own baseline volume immediately (Part 13.3)",
                Mathf.Approximately(musicSource.volume, PlayerProfile.MusicVolume * 0.5f), $"volume={musicSource.volume}");

            AudioService.SetMusicVolume(0.3f);
            Check("Changing music volume LIVE updates every registered music source, no restart needed (Part 13.3)",
                Mathf.Approximately(musicSource.volume, 0.3f * 0.5f), $"volume={musicSource.volume}");

            var sfxGo = new GameObject("Stage18Test_SfxSource");
            var sfxSource = sfxGo.AddComponent<AudioSource>();
            AudioService.RegisterSfx(sfxSource);
            AudioService.SetSfxVolume(0.7f);
            Check("SFX volume tracks independently of music volume (Part 13.3's separate sliders)",
                Mathf.Approximately(sfxSource.volume, 0.7f) && Mathf.Approximately(musicSource.volume, 0.3f * 0.5f),
                $"sfx={sfxSource.volume} music={musicSource.volume}");

            var oneShotGo = new GameObject("Stage18Test_OneShotSource");
            var oneShotSource = oneShotGo.AddComponent<AudioSource>();
            var testClip = PlaceholderAudio.GenerateTone("Stage18Test_Tone", 440f, 0.3f, 0.5f);
            AudioService.PlayOneShotSfx(oneShotSource, testClip);
            Check("PlayOneShotSfx actually starts playback", oneShotSource.isPlaying, "");

            Destroy(musicGo);
            Destroy(sfxGo);
            Destroy(oneShotGo);
        }

        // ---------------------------------------------------------------
        private void TestUiClickSoundWiring()
        {
            var canvasGo = new GameObject("Stage18Test_ClickCanvas");
            var canvas = UIBuilder.CreateScreenCanvas("Canvas", canvasGo.transform);
            var button = UIBuilder.CreateButton(canvas.transform, "TestButton", "Click Me", Color.white, Color.black);

            var clickAudio = button.GetComponent<AudioSource>();
            Check("Every button built via UIBuilder carries its own click-sound AudioSource (Part 13.1)", clickAudio != null, "");

            button.onClick.Invoke();
            Check("Pressing a button plays the UI click sound", clickAudio != null && clickAudio.isPlaying, "");

            Destroy(canvasGo);
        }

        // ---------------------------------------------------------------
        private void TestMaterialImpactSoundLibrary()
        {
            var wood = ImpactSoundLibrary.GetClip(ImpactMaterial.Wood);
            var metal = ImpactSoundLibrary.GetClip(ImpactMaterial.Metal);
            var glass = ImpactSoundLibrary.GetClip(ImpactMaterial.Glass);
            var human = ImpactSoundLibrary.GetClip(ImpactMaterial.Human);

            Check("Each of the four materials resolves to a valid, distinct clip (Part 13.1)",
                wood != null && metal != null && glass != null && human != null &&
                wood != metal && metal != glass && glass != human && human != wood, "");

            Check("Repeated lookups return the SAME cached clip instance (no per-collision regeneration)",
                ImpactSoundLibrary.GetClip(ImpactMaterial.Wood) == wood, "");
        }

        private void TestCollidablePropsHaveMaterialSoundAndOutOfBoundsWiring()
        {
            var carry = new GameObject("Stage18Test_Carryable").AddComponent<CarryableObject>();
            var fallable = new GameObject("Stage18Test_Fallable").AddComponent<FallableProp>();
            var pushable = new GameObject("Stage18Test_Pushable").AddComponent<PushableProp>();

            Check("CarryableObject/FallableProp/PushableProp each carry an AudioSource for material-tagged impact sound (Part 13.1)",
                carry.GetComponent<AudioSource>() != null && fallable.GetComponent<AudioSource>() != null && pushable.GetComponent<AudioSource>() != null, "");

            Check("All three default to a reasonable material tag (Wood)",
                carry.material == ImpactMaterial.Wood && fallable.material == ImpactMaterial.Wood && pushable.material == ImpactMaterial.Wood, "");

            Check("Part 16.2's out-of-bounds recovery is auto-attached to all three physical prop classes ('any physical object')",
                carry.GetComponent<OutOfBoundsRecovery>() != null &&
                fallable.GetComponent<OutOfBoundsRecovery>() != null &&
                pushable.GetComponent<OutOfBoundsRecovery>() != null, "");

            Destroy(carry.gameObject);
            Destroy(fallable.gameObject);
            Destroy(pushable.gameObject);
        }

        // ---------------------------------------------------------------
        private void TestCameraShake()
        {
            var targetGo = new GameObject("Stage18Test_ShakeTarget");
            var rigGo = new GameObject("Stage18Test_ShakeRig");
            var rig = rigGo.AddComponent<PlayerCameraRig>();
            rig.target = targetGo.transform;

            Check("PlayerCameraRig.LocalInstance is reachable the moment a rig exists (Law 0.5 needs a hookable local camera)",
                PlayerCameraRig.LocalInstance == rig, "");

            rig.TriggerShake();
            Check("TriggerShake starts a shake (Law 0.5: ~0.15 unit amplitude, 0.2s duration)", rig.IsShaking, "");

            rig.ApplyCamera(0.25f); // steps past the full 0.2s duration in one call
            Check("The shake fully fades out once its duration elapses", !rig.IsShaking, "");

            Destroy(rigGo);
            Destroy(targetGo);
        }

        // ---------------------------------------------------------------
        private IEnumerator TestBackgroundMusicStartsStopsWithRound()
        {
            var roundGo = new GameObject("Stage18Test_RoundManager");
            var roundManager = roundGo.AddComponent<RoundManager>();

            roundManager.StartRound(new List<PlayerTeam>(), 300f);
            Check("Background music starts the moment a round starts (Part 13.1)", roundManager.IsMusicPlaying, "");

            // A fresh, near-zero-duration round to observe the natural stop
            // without waiting out a full 300s round.
            roundManager.StartRound(new List<PlayerTeam>(), 0.05f);
            Check("Music restarts cleanly on a fresh round", roundManager.IsMusicPlaying, "");

            yield return WaitUntilOrTimeout(() => roundManager.HasEnded, 2f);
            Check("Background music stops the moment the round ends (Part 13.1)",
                roundManager.HasEnded && !roundManager.IsMusicPlaying, $"hasEnded={roundManager.HasEnded} musicPlaying={roundManager.IsMusicPlaying}");

            Destroy(roundGo);
        }

        // ---------------------------------------------------------------
        private IEnumerator TestCarryableObjectIdleSleep()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Stage18Test_IdleCarryable";
            // Well clear of Scene_04_Mansion's real architecture (Stage18FinalizationSetup
            // opens it for the rope-tie test below) - a default-position cube would
            // otherwise spawn embedded in the ground floor and get an unwanted physics
            // de-penetration nudge that resets the idle timer before it can sleep.
            go.transform.position = new Vector3(1000f, 1000f, 1000f);
            var carry = go.AddComponent<CarryableObject>();
            carry.Body.useGravity = false;
            carry.Body.linearVelocity = Vector3.zero;
            carry.Body.angularVelocity = Vector3.zero;

            carry.DebugForceIdleTimerAt(CarryableObject.IdleSleepDelaySeconds);
            yield return null; // let Update() observe the forced timer and call Body.Sleep()

            Check("A stationary carryable object left untouched puts its Rigidbody to sleep (Part 15.2)",
                carry.Body.IsSleeping(), $"isSleeping={carry.Body.IsSleeping()}");

            Destroy(go);
        }

        // ---------------------------------------------------------------
        private IEnumerator TestStuckDetectionRecovers()
        {
            var playerGo = new GameObject("Stage18Test_StuckPlayer");
            var controller = playerGo.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            var locomotion = playerGo.AddComponent<PlayerLocomotion>();
            var stuck = playerGo.AddComponent<StuckDetection>();

            Vector3 spawn = playerGo.transform.position;

            locomotion.SetMoveInput(Vector2.up); // "استمرار محاولاته للتحرك عبر مدخلات التحكم النشطة"
            locomotion.IsExternallyControlled = true; // simulate physically wedged - position genuinely can't change
            stuck.DebugForceStuck();

            yield return null; // let StuckDetection.Update() observe the forced timer and recover

            Check("A player wedged despite active movement input is auto-repositioned to a known safe spot (Part 16.1)",
                stuck.RecoveryCount == 1 && playerGo.transform.position == spawn,
                $"recoveries={stuck.RecoveryCount} pos={playerGo.transform.position} spawn={spawn}");
            Check("Recovery hands normal control back to the player",
                !locomotion.IsExternallyControlled, $"isExternallyControlled={locomotion.IsExternallyControlled}");

            Destroy(playerGo);
        }

        // ---------------------------------------------------------------
        private IEnumerator TestOutOfBoundsRecovery()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Stage18Test_OOBCarryable";
            var carry = go.AddComponent<CarryableObject>();
            carry.Body.useGravity = false;
            var oob = go.GetComponent<OutOfBoundsRecovery>();
            Vector3 originalPos = go.transform.position;

            go.transform.position = new Vector3(originalPos.x, -50f, originalPos.z);

            float elapsed = 0f;
            while (oob.RecoveryCount == 0 && elapsed < 2.5f) { elapsed += Time.deltaTime; yield return null; }

            Check("A prop that falls far below the floor is auto-restored to its original designed position (Part 16.2)",
                oob.RecoveryCount == 1 && go.transform.position == originalPos,
                $"recoveries={oob.RecoveryCount} pos={go.transform.position} original={originalPos}");

            Destroy(go);
        }

        // ---------------------------------------------------------------
        private void TestInvalidActionGuardsAlreadyCompliant()
        {
            var go = new GameObject("Stage18Test_InvalidActionVictim");
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f; controller.radius = 0.3f; controller.center = new Vector3(0f, 0.5f, 0f);
            go.AddComponent<PlayerLocomotion>();
            go.AddComponent<PlayerRagdoll>();
            var capture = go.AddComponent<PlayerCapture>();

            // Nothing has happened to this player yet - State is still None, so
            // every one of Part 7.5's special actions must fail silently.
            bool grabEnd = capture.TryGrabEnd(null);
            bool balconyThrow = capture.TryThrowFromBalcony();
            bool fanMount = capture.TryMountOnFan(null);

            Check("Part 16.4: every special action silently no-ops when its precondition isn't met, with zero exceptions or side effects",
                !grabEnd && !balconyThrow && !fanMount && capture.State == CaptureState.None,
                $"grabEnd={grabEnd} balconyThrow={balconyThrow} fanMount={fanMount} state={capture.State}");

            Destroy(go);
        }

        // ---------------------------------------------------------------
        private void TestDuplicateRoomNameHostDisambiguation()
        {
            var hostAGo = new GameObject("Stage18Test_HostA");
            var hostA = hostAGo.AddComponent<LobbyManager>();
            hostA.TryCreateRoom("Party Room", 4, 300f, "hostA18", "Alice");

            var hostBGo = new GameObject("Stage18Test_HostB");
            var hostB = hostBGo.AddComponent<LobbyManager>();
            hostB.Directory = hostA.Directory; // same backend, like a second real client
            hostB.TryCreateRoom("Party Room", 4, 300f, "hostB18", "Bob");

            var results = hostA.SearchRooms("Party Room");
            bool foundAlice = results.Exists(r => r.HostDisplayName == "Alice");
            bool foundBob = results.Exists(r => r.HostDisplayName == "Bob");

            Check("Two rooms sharing an exact name are both returned, distinguishable by host name (Part 16.5)",
                results.Count == 2 && foundAlice && foundBob, $"count={results.Count} alice={foundAlice} bob={foundBob}");

            Destroy(hostAGo);
            Destroy(hostBGo);
        }

        // ---------------------------------------------------------------
        private void TestCreditsPanelLocalization()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
            var panelGo = new GameObject("Stage18Test_Credits");
            var panel = panelGo.AddComponent<CreditsPanel>();
            panel.BuildUI();

            var title = panelGo.transform.Find("CreditsCanvas/Title").GetComponent<Text>();
            var developerName = panelGo.transform.Find("CreditsCanvas/DeveloperName").GetComponent<Text>();
            Check("Credits screen exists and shows the developer's name (Part 19.3)", title.text == "Credits" && developerName.text == "Imad", $"title={title.text} name={developerName.text}");

            LocalizationManager.SetLanguage(GameLanguage.Darija);
            Check("Credits screen title re-renders live in Darija like every other screen (Part 12.2)", title.text == "شكر وتقدير", $"title={title.text}");
            Check("The developer's own proper name is never translated", developerName.text == "Imad", $"name={developerName.text}");

            LocalizationManager.SetLanguage(GameLanguage.English);
            Destroy(panelGo);
        }

        // ---------------------------------------------------------------
        private IEnumerator TestRopeTieSound()
        {
            // Same on-floor coordinate region Stage5CaptureTest already validated
            // (real Scene_04_Mansion floor geometry, opened by Stage18FinalizationSetup.BuildAndTest).
            var (loco, _, ragdoll, capture) = BuildVictim("Stage18Test_RopeVictim", new Vector3(45f, 0.05f, 15f));
            yield return null;
            ragdoll.TriggerRagdoll();
            yield return WaitUntilOrTimeout(() => capture.State == CaptureState.Unconscious, 8f);
            Check("Setup: victim reached Unconscious", capture.State == CaptureState.Unconscious, $"state={capture.State}");

            var (rescueLoco, rescueCarry) = BuildRescuer("Stage18Test_RopeRescuer", capture.transform.position + new Vector3(1f, 0f, 0f));
            var rope = SpawnRope(rescueCarry.transform.position + new Vector3(0f, 0.15f, 0.5f));
            yield return null;
            rescueCarry.TryPickUpNearest();
            yield return null;

            rescueCarry.transform.position = capture.transform.position + new Vector3(0.5f, 0f, 0f);
            yield return null;
            bool restrained = rescueCarry.TryRestrainNearestUnconscious();
            Check("Setup: restraining succeeded", restrained, $"restrained={restrained}");

            // The victim's GameObject also carries PlayerRagdoll's own impact-sound
            // AudioSource, so check ALL of its sources rather than assuming which
            // one GetComponent<AudioSource> (singular) happens to return first.
            var audioSources = capture.GetComponents<AudioSource>();
            bool anyPlaying = System.Array.Exists(audioSources, a => a.isPlaying);
            Check("Restraining an unconscious victim plays the rope-tie sound (Part 13.1)", anyPlaying, $"sourceCount={audioSources.Length}");

            Destroy(capture.gameObject);
            Destroy(rescueCarry.gameObject);
        }

        private (PlayerLocomotion loco, PlayerCarry carry, PlayerRagdoll ragdoll, PlayerCapture capture) BuildVictim(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f; controller.radius = 0.3f; controller.center = new Vector3(0f, 0.5f, 0f);
            var loco = go.AddComponent<PlayerLocomotion>();
            var carry = go.AddComponent<PlayerCarry>();
            var ragdoll = go.AddComponent<PlayerRagdoll>();
            var capture = go.AddComponent<PlayerCapture>();
            return (loco, carry, ragdoll, capture);
        }

        private (PlayerLocomotion loco, PlayerCarry carry) BuildRescuer(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f; controller.radius = 0.3f; controller.center = new Vector3(0f, 0.5f, 0f);
            var loco = go.AddComponent<PlayerLocomotion>();
            var carry = go.AddComponent<PlayerCarry>();
            return (loco, carry);
        }

        private CarryableObject SpawnRope(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Stage18Test_Rope";
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.4f, 0.08f, 0.08f);
            var carry = go.AddComponent<CarryableObject>();
            carry.weightClass = CarryableObject.WeightClass.Light;
            carry.isRope = true;
            return carry;
        }

        private IEnumerator WaitUntilOrTimeout(System.Func<bool> condition, float timeoutSeconds)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}

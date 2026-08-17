using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode half of Stage 11's verification (static half already ran in
    /// Stage11DoorSystemSetup.RunStaticVerification before Play Mode was entered).
    /// Proves Part 8's door/drawer toggle actually moves a real prop end to end:
    /// a real main door swings open/closed away from the player, the real kitchen
    /// washer's door swings on its horizontal hinge, and the placeholder filing
    /// cabinet's drawer slides open.
    /// </summary>
    public class Stage11DoorSystemTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage11_DynamicTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 11 - Dynamic Door/Drawer System Test (Play Mode) ===");
            report.AppendLine();

            yield return TestMainDoorSwing("Bathroom1_Door_01");
            yield return TestKitchenWasherDoor();
            yield return TestFilingCabinetDrawer();

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 11 door/drawer system works correctly on real props."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage11DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage11DynamicTest] DONE");

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

        private GameObject FindByName(string name)
        {
            foreach (var door in FindObjectsByType<DoorProp>(FindObjectsSortMode.None))
                if (door.name == name || door.name == name + "(Clone)") return door.gameObject;
            var direct = GameObject.Find(name);
            if (direct != null) return direct;
            var clone = GameObject.Find(name + "(Clone)");
            return clone;
        }

        private IEnumerator TestMainDoorSwing(string unityName)
        {
            var go = FindByName(unityName);
            Check($"Setup: {unityName} instance found in scene", go != null, $"found={(go != null)}");
            if (go == null) yield break;

            var door = go.GetComponent<DoorProp>();
            Check($"{unityName} has the real DoorProp component (Part 8.1)", door != null, $"found={(door != null)}");
            if (door == null) yield break;

            var player = new GameObject("Stage11Test_DoorPlayer").transform;
            player.position = door.transform.position + door.transform.right * 1f;

            bool toggled = door.TryToggle(player);
            Check($"The real {unityName} can be toggled by the interact button (Part 8.1)", toggled, $"toggled={toggled}");

            float elapsed = 0f;
            while (door.State != DoorProp.DoorState.Open && elapsed < 2f) { elapsed += Time.deltaTime; yield return null; }
            Check($"The real {unityName} finishes opening (Part 8.1: ~0.4s ease-out)", door.State == DoorProp.DoorState.Open, $"state={door.State} elapsed={elapsed:F2}s");

            bool toggledClosed = door.TryToggle(player);
            elapsed = 0f;
            while (door.State != DoorProp.DoorState.Closed && elapsed < 2f) { elapsed += Time.deltaTime; yield return null; }
            Check($"The real {unityName} closes again on a second press (Part 8.1: toggle)",
                toggledClosed && door.State == DoorProp.DoorState.Closed, $"state={door.State} elapsed={elapsed:F2}s");

            Destroy(player.gameObject);
            yield return null;
        }

        private IEnumerator TestKitchenWasherDoor()
        {
            var combo = GameObject.Find("Kitchen_FullCombo_01") ?? GameObject.Find("Kitchen_FullCombo_01(Clone)");
            Check("Setup: Kitchen_FullCombo_01 instance found in scene", combo != null, $"found={(combo != null)}");
            if (combo == null) yield break;

            DoorProp washerDoor = null;
            foreach (var d in combo.GetComponentsInChildren<DoorProp>())
                if (d.name.Contains("_doors")) washerDoor = d;
            Check("The real kitchen washer's door has a DoorProp (Part 8.2)", washerDoor != null, $"found={(washerDoor != null)}");
            if (washerDoor == null) yield break;

            Check("The washer door uses a horizontal hinge axis (Part 8.2's circular-door case)",
                washerDoor.hingeAxis == DoorProp.HingeAxis.Horizontal, $"axis={washerDoor.hingeAxis}");

            var player = new GameObject("Stage11Test_WasherPlayer").transform;
            player.position = washerDoor.transform.position + washerDoor.transform.forward * 1f;

            bool toggled = washerDoor.TryToggle(player);
            float elapsed = 0f;
            while (washerDoor.State != DoorProp.DoorState.Open && elapsed < 2f) { elapsed += Time.deltaTime; yield return null; }
            Check("The real washer door swings open on a real horizontal hinge (Part 8.2)",
                toggled && washerDoor.State == DoorProp.DoorState.Open, $"toggled={toggled} state={washerDoor.State} elapsed={elapsed:F2}s");

            Destroy(player.gameObject);
            yield return null;
        }

        private IEnumerator TestFilingCabinetDrawer()
        {
            var cabinet = GameObject.Find("Office_FilingCabinet_01") ?? GameObject.Find("Office_FilingCabinet_01(Clone)");
            Check("Setup: Office_FilingCabinet_01 placeholder found in scene", cabinet != null, $"found={(cabinet != null)}");
            if (cabinet == null) yield break;

            Check("The placeholder filing cabinet is Pushable (Part 4.4)", cabinet.GetComponent<PushableProp>() != null,
                $"found={(cabinet.GetComponent<PushableProp>() != null)}");

            var drawer = cabinet.GetComponentInChildren<DrawerSlideProp>();
            Check("The placeholder filing cabinet has a real DrawerSlideProp drawer (Part 8.3)", drawer != null, $"found={(drawer != null)}");
            if (drawer == null) yield break;

            Vector3 startPos = drawer.transform.position;
            var player = new GameObject("Stage11Test_CabinetPlayer").transform;
            player.position = drawer.transform.position + Vector3.forward * 0.5f;

            bool toggled = drawer.TryToggle(player);
            float elapsed = 0f;
            while (drawer.State != DrawerSlideProp.SlideState.Open && elapsed < 2f) { elapsed += Time.deltaTime; yield return null; }

            float slid = Vector3.Distance(startPos, drawer.transform.position);
            Check("Pressing interact slides the real drawer ~0.40m open (Part 8.3)",
                toggled && drawer.State == DrawerSlideProp.SlideState.Open && slid > 0.35f,
                $"toggled={toggled} state={drawer.State} slid={slid:F3}m elapsed={elapsed:F2}s");

            Destroy(player.gameObject);
            yield return null;
        }
    }
}

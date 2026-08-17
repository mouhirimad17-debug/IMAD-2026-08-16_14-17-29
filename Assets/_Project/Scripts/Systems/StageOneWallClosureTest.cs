using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Runtime (Play Mode) verification of Law 0.4 (closed mansion) for Stage 1.
    /// The real player controller does not exist yet (that is Stage 2), so this
    /// pushes real PhysX rigidbodies into every exterior wall at multiple points
    /// along its length and at both floor heights, and records whether each one
    /// was actually stopped rather than tunneling through to the outside.
    ///
    /// This is the closest faithful equivalent of Law 0.4's mandatory walk-and-
    /// collide protocol available before player movement exists. The literal
    /// player-walk version of the test should be repeated at the end of Stage 2.
    /// </summary>
    public class StageOneWallClosureTest : MonoBehaviour
    {
        private struct WallTest
        {
            public string wallName;
            public int axis;       // 0 = probe travels along X, 1 = probe travels along Z
            public float outerFace;
            public float sign;     // +1 or -1: direction of travel toward "outside"
            public float alongMin, alongMax; // sample range along the wall's length
            public float fixedAlong;         // the other axis coordinate (1m inside the inner face)
        }

        public string reportPath = "Assets/_ProjectLogs/Stage1_WallClosureTest_Report.txt";

        private IEnumerator Start()
        {
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 10f;

            var report = new StringBuilder();
            report.AppendLine("=== Stage 1 - Law 0.4 Exterior Wall Closure Test ===");
            report.AppendLine("Real PhysX rigidbody push test (player controller not yet built - see Stage 2).");
            report.AppendLine();

            var tests = BuildWallTests();
            int total = 0, passed = 0;

            foreach (var wt in tests)
            {
                for (int i = 0; i < 3; i++)
                {
                    float tt = (i + 1) / 4f; // 0.25, 0.5, 0.75 along the wall length
                    float along = Mathf.Lerp(wt.alongMin, wt.alongMax, tt);

                    float[] heights = { 1.6f, 5.0f }; // ground floor & upper floor mid-height
                    foreach (var y in heights)
                    {
                        total++;

                        Vector3 pos = Vector3.zero;
                        pos.y = y;
                        if (wt.axis == 0) { pos.x = wt.fixedAlong; pos.z = along; }
                        else { pos.z = wt.fixedAlong; pos.x = along; }

                        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.name = "WallTestProbe";
                        go.transform.position = pos;
                        go.transform.localScale = Vector3.one * 0.6f; // world radius ~0.3m
                        var rb = go.AddComponent<Rigidbody>();
                        rb.useGravity = false;
                        rb.linearDamping = 0f;
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;

                        Vector3 vel = Vector3.zero;
                        if (wt.axis == 0) vel.x = wt.sign * 4f; else vel.z = wt.sign * 4f;
                        rb.linearVelocity = vel;

                        yield return new WaitForSeconds(0.8f);

                        float coord = wt.axis == 0 ? go.transform.position.x : go.transform.position.z;
                        bool stayedInside = wt.sign > 0
                            ? (coord < wt.outerFace - 0.02f)
                            : (coord > wt.outerFace + 0.02f);

                        if (stayedInside) passed++;
                        report.AppendLine(
                            $"[{(stayedInside ? "PASS" : "FAIL")}] {wt.wallName} along={along:F2} y={y:F2} " +
                            $"-> finalCoord={coord:F3} outerFace={wt.outerFace:F2}");

                        Object.Destroy(go);
                    }
                }
            }

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: ALL EXTERIOR WALLS CLOSED - Law 0.4 satisfied (pre-player-controller physics test)."
                : "RESULT: FAILURE - at least one gap found. See FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage1Test] Report written to " + reportPath);
            Debug.Log(report.ToString());

            Time.timeScale = previousTimeScale;

            yield return null;
            Debug.Log("[Stage1Test] DONE");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
            {
                UnityEditor.EditorApplication.Exit(passed == total ? 0 : 1);
            }
#endif
        }

        private List<WallTest> BuildWallTests()
        {
            float t = MansionSpec.ExtWallT;
            var list = new List<WallTest>();

            list.Add(new WallTest
            {
                wallName = "Front(Z=0)",
                axis = 1,
                outerFace = 0f,
                sign = -1f,
                alongMin = 2f,
                alongMax = MansionSpec.LenX - 2f,
                fixedAlong = t + 1.0f
            });

            list.Add(new WallTest
            {
                wallName = "Back(Z=WidZ)",
                axis = 1,
                outerFace = MansionSpec.WidZ,
                sign = 1f,
                alongMin = 2f,
                alongMax = MansionSpec.LenX - 2f,
                fixedAlong = MansionSpec.WidZ - t - 1.0f
            });

            list.Add(new WallTest
            {
                wallName = "Left(X=0)",
                axis = 0,
                outerFace = 0f,
                sign = -1f,
                alongMin = 2f,
                alongMax = MansionSpec.WidZ - 2f,
                fixedAlong = t + 1.0f
            });

            list.Add(new WallTest
            {
                wallName = "Right(X=LenX)",
                axis = 0,
                outerFace = MansionSpec.LenX,
                sign = 1f,
                alongMin = 2f,
                alongMax = MansionSpec.WidZ - 2f,
                fixedAlong = MansionSpec.LenX - t - 1.0f
            });

            return list;
        }
    }
}

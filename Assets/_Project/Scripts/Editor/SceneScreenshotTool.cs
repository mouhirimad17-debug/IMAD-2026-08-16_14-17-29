using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Permanent headless visual-inspection tool. The project owner's machine can't
    /// reliably run the Unity Editor's own GUI (Edit Mode or Play Mode) at all, so
    /// every future visual check goes through this instead: batch-mode Unity (real
    /// graphics, NOT -nographics - that flag disables rendering entirely, which is
    /// exactly what CI-style Play Mode tests want but the opposite of what a
    /// screenshot needs), a temporary camera + light per shot, rendered to a
    /// RenderTexture and saved as a plain PNG outside Assets/ (so Unity never
    /// imports them as texture assets) for the owner to open in any normal image
    /// viewer. The scene is never saved after capturing - every camera/light this
    /// tool creates is destroyed again immediately, so repeated runs never
    /// accumulate junk in Scene_04_Mansion.unity.
    /// </summary>
    public static class SceneScreenshotTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const int ImageWidth = 1920;
        private const int ImageHeight = 1080;

        private struct RoomShot
        {
            public string name;
            public XZRect area;
            public float floorY;
            public RoomShot(string name, XZRect area, float floorY) { this.name = name; this.area = area; this.floorY = floorY; }
        }

        [MenuItem("PrankMansion/Tools - Capture Room Screenshots")]
        public static void CaptureAllRooms()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            string outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Screenshots", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(outDir);

            var rooms = new[]
            {
                new RoomShot("Gym", MansionSpec.Gym, MansionSpec.GroundY),
                new RoomShot("Foyer", MansionSpec.Foyer, MansionSpec.GroundY),
                new RoomShot("Kitchen", MansionSpec.Kitchen, MansionSpec.GroundY),
                new RoomShot("Office", MansionSpec.OfficeWing, MansionSpec.Floor2FloorY),
                new RoomShot("Bedroom1_Bathroom1", MansionSpec.Bedroom1Wing, MansionSpec.Floor2FloorY),
                new RoomShot("Bedroom2_Bathroom2", MansionSpec.Bedroom2Wing, MansionSpec.Floor2FloorY),
            };

            var rig = BuildCaptureRig();
            try
            {
                foreach (var room in rooms)
                    CaptureRoom(room, rig.cam, outDir);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rig.root);
            }

            Debug.Log($"[SceneScreenshotTool] {rooms.Length * 3} screenshots saved to: {outDir}");
        }

        private struct CaptureRig { public GameObject root; public Camera cam; }

        private static CaptureRig BuildCaptureRig()
        {
            var root = new GameObject("Stage_ScreenshotRig_TEMP");

            // No stage has ever added a real Light to the scene (Part 2.1's warm
            // lighting was never wired up as an actual Light component) - without
            // this, every capture would just be pure black. Bright and neutral is
            // the goal here (clear inspection), not the game's own final mood.
            var lightGo = new GameObject("TempSun");
            lightGo.transform.SetParent(root.transform);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.97f, 0.92f);
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.48f);

            var camGo = new GameObject("TempCamera");
            camGo.transform.SetParent(root.transform);
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.58f, 0.62f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 300f;
            cam.fieldOfView = 65f;

            return new CaptureRig { root = root, cam = cam };
        }

        private static void CaptureRoom(RoomShot room, Camera cam, string outDir)
        {
            float margin = 1.5f;
            float eyeY = room.floorY + 1.6f;

            // Two opposite interior corners at eye height, angled toward the room's
            // center - approximates what a player would actually see walking in.
            ShootPerspective(cam, room.name, "CornerA",
                new Vector3(room.area.x + margin, eyeY, room.area.z + margin),
                new Vector3(room.area.centerX, room.floorY + 1.2f, room.area.centerZ), outDir);

            ShootPerspective(cam, room.name, "CornerB",
                new Vector3(room.area.xMax - margin, eyeY, room.area.zMax - margin),
                new Vector3(room.area.centerX, room.floorY + 1.2f, room.area.centerZ), outDir);

            // Straight top-down floor-plan view - the fastest way to spot a
            // misplaced, overlapping, or missing prop across the whole room at once.
            ShootTopDown(cam, room.name, room.area, room.floorY, outDir);
        }

        private static void ShootPerspective(Camera cam, string roomName, string tag, Vector3 pos, Vector3 lookAt, string outDir)
        {
            cam.orthographic = false;
            cam.transform.position = pos;
            cam.transform.LookAt(lookAt, Vector3.up);
            RenderAndSave(cam, Path.Combine(outDir, $"{roomName}_{tag}.png"));
        }

        private static void ShootTopDown(Camera cam, string roomName, XZRect area, float floorY, string outDir)
        {
            // Every floor has a solid ceiling/roof slab (Law 0.4) - a camera placed
            // ABOVE it (e.g. over the roof) sees only that slab's top surface, not
            // an X-ray of the room below. Positioned just under this room's OWN
            // ceiling instead, so the view down is genuinely unobstructed.
            float halfSpan = Mathf.Max(area.sizeX, area.sizeZ) * 0.5f + 1f;
            cam.orthographic = true;
            cam.orthographicSize = halfSpan;
            cam.transform.position = new Vector3(area.centerX, floorY + MansionSpec.FloorClearH - 0.15f, area.centerZ);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            RenderAndSave(cam, Path.Combine(outDir, $"{roomName}_TopDown.png"));
        }

        private static void RenderAndSave(Camera cam, string path)
        {
            var rt = new RenderTexture(ImageWidth, ImageHeight, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(ImageWidth, ImageHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ImageWidth, ImageHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;

            File.WriteAllBytes(path, tex.EncodeToPNG());

            cam.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}

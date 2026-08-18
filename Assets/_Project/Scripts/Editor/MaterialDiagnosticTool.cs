using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Headless diagnostic: scans Scene_04_Mansion for every Renderer and reports
    /// any material that is missing, null, or using Unity's error/fallback shader -
    /// the actual "broken material" case, distinct from the intentionally-magenta
    /// PLACEHOLDER_Magenta material used for logged-missing props.
    /// </summary>
    public static class MaterialDiagnosticTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string ReportPath = "Assets/_ProjectLogs/Material_Diagnostic_Report.txt";

        [MenuItem("PrankMansion/Tools - Diagnose Materials")]
        public static void Diagnose()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var lines = new List<string> { "=== Material Diagnostic Report ===", "" };
            int total = 0, broken = 0, placeholder = 0;

            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    total++;
                    string path = GetPath(r.transform);

                    if (mat == null)
                    {
                        broken++;
                        lines.Add($"[MISSING] {path} -> null material slot");
                        continue;
                    }

                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    {
                        broken++;
                        lines.Add($"[BROKEN-SHADER] {path} -> material='{mat.name}' shader='{(mat.shader == null ? "NULL" : mat.shader.name)}'");
                        continue;
                    }

                    if (mat.name.Contains("PLACEHOLDER_Magenta") || mat.name.StartsWith("Placeholder_"))
                    {
                        placeholder++;
                        lines.Add($"[PLACEHOLDER] {path} -> material='{mat.name}' (intentional, world pos={r.transform.position})");
                    }
                }
            }

            lines.Add("");
            lines.Add($"Total material slots scanned: {total}");
            lines.Add($"Broken/missing materials: {broken}");
            lines.Add($"Intentional placeholders: {placeholder}");

            var dir = Path.GetDirectoryName(ReportPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(ReportPath, lines);

            Debug.Log($"[MaterialDiagnosticTool] Scanned {total} material slots: {broken} broken, {placeholder} placeholder. See {ReportPath}");
        }

        private static string GetPath(Transform t)
        {
            var parts = new List<string>();
            while (t != null) { parts.Add(t.name); t = t.parent; }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}

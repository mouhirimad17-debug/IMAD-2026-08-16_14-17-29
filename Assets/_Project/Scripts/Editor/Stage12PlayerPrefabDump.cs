using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>TEMPORARY - dumps Player.prefab's component list. Deleted after use.</summary>
    public static class Stage12PlayerPrefabDump
    {
        [MenuItem("PrankMansion/Stage 12 - DUMP Player Prefab (temp)")]
        public static void Dump()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab");
            var lines = new List<string>();
            if (prefab == null) { lines.Add("NOT FOUND"); }
            else
            {
                void Walk(Transform t, string indent)
                {
                    lines.Add($"{indent}{t.name}");
                    foreach (var c in t.GetComponents<Component>())
                        lines.Add($"{indent}  [{c.GetType().Name}]");
                    foreach (Transform child in t) Walk(child, indent + "  ");
                }
                Walk(prefab.transform, "");
            }
            File.WriteAllLines("Assets/_ProjectLogs/Stage12_PlayerPrefabDump_Report.txt", lines);
            Debug.Log("[Stage12PlayerPrefabDump] done");
#if UNITY_EDITOR
            if (Application.isBatchMode) EditorApplication.Exit(0);
#endif
        }
    }
}

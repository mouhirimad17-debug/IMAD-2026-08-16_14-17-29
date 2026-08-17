using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// TEMPORARY diagnostic (not part of the Stage 12 pipeline) - tries setting each
    /// of the 7 character FBX files and every Player_Common animation clip to a
    /// Humanoid rig, then reports whether Unity's auto-mapping produced a valid
    /// Humanoid avatar for each. Deleted once Stage 12's real pipeline is confirmed
    /// working from this result.
    /// </summary>
    public static class Stage12HumanoidCheck
    {
        private static readonly string[] CharacterPaths =
        {
            "Assets/_Project/Models/Characters/Player/Character_Slowpoke_01/character_slowpoke_01.fbx",
            "Assets/_Project/Models/Characters/Player/Character_Strongman_01/character_strongman_01.fbx",
            "Assets/_Project/Models/Characters/Player/Character_StrongPush_01/Character_strobgpush_01.fbx",
            "Assets/_Project/Models/Characters/Player/Character_BigEars_01/character_Bigears_01.fbx",
            "Assets/_Project/Models/Characters/Player/Character_QuietSteps_01/character_QuietSteps_01.fbx",
            "Assets/_Project/Models/Characters/Player/Character_QuickPour_01/character_QuickPourr_01.fbx",
            "Assets/_Project/Models/Characters/Player/Character_Featherweight_01/character_Featherweight_01.fbx",
        };

        [MenuItem("PrankMansion/Stage 12 - CHECK Humanoid Rigs (temp)")]
        public static void Check()
        {
            var lines = new List<string>();

            foreach (var path in CharacterPaths)
                lines.Add(TrySetHumanoid(path));

            var animDir = "Assets/_Project/Animations/Player_Common";
            foreach (var fbx in Directory.GetFiles(animDir, "*.fbx").OrderBy(f => f))
                lines.Add(TrySetHumanoid(fbx.Replace('\\', '/')));

            File.WriteAllLines("Assets/_ProjectLogs/Stage12_HumanoidCheck_Report.txt", lines);
            Debug.Log("[Stage12HumanoidCheck] Done, " + lines.Count + " files checked.");
#if UNITY_EDITOR
            if (Application.isBatchMode) EditorApplication.Exit(0);
#endif
        }

        private static string TrySetHumanoid(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return $"{path} -> NOT FOUND / NOT A MODEL";

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            try
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            catch (System.Exception e)
            {
                return $"{path} -> IMPORT EXCEPTION: {e.Message}";
            }

            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault(a => a is Avatar) as Avatar;
            bool isValid = avatar != null && avatar.isValid;
            bool isHuman = avatar != null && avatar.isHuman;
            return $"{path} -> avatarFound={avatar != null} isValid={isValid} isHuman={isHuman}";
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// One-time reorganizer: renames/moves the project's existing Assets/Project tree
    /// to match PrankMansion_MasterDocument.md Part 1 (folder structure) exactly,
    /// using AssetDatabase.MoveAsset/CreateFolder so every GUID (and therefore every
    /// existing reference) survives the move.
    ///
    /// Scope discipline (see Stage1_FolderReorg_Log.txt written at the end for the
    /// full rationale): this moves EXISTING files into the doc-mandated folders and
    /// fixes folder-level name typos/casing, but does NOT rename individual leaf
    /// asset files, does NOT touch third-party vendor packages (mocap/animation
    /// packs, sound packs, VFX packs - Part 1 does not govern where raw vendor
    /// source libraries sit), and does NOT run Law 0.3's per-asset scale correction
    /// (that belongs to the asset-import stages, 6-10 and 12).
    /// </summary>
    public static class MansionProjectReorganizer
    {
        private const string LogPath = "Assets/_ProjectLogs/Stage1_FolderReorg_Log.txt";
        private static readonly List<string> Log = new List<string>();
        private static readonly List<string> Unmatched = new List<string>();

        [MenuItem("PrankMansion/Stage 1 - Fix Character Folder Casing")]
        public static void FixCharacterFolderCasing()
        {
            // Unity's FBX importer had pre-created lowercase-named folders matching
            // some of the raw file names (e.g. "character_Bigears_01"); on Windows'
            // case-insensitive filesystem, CreateFolder("Character_BigEars_01") in
            // Reorganize() silently collided with those and kept the old casing.
            // Fix with the same two-step case-safe move used for "models" -> "Models".
            Log.Clear();
            Unmatched.Clear();

            string playerRoot = "Assets/_Project/Models/Characters/Player";
            var fixes = new (string wrong, string correct)[]
            {
                ("character_Bigears_01", "Character_BigEars_01"),
                ("character_Featherweight_01", "Character_Featherweight_01"),
                ("character_QuietSteps_01", "Character_QuietSteps_01"),
                ("character_slowpoke_01", "Character_Slowpoke_01"),
                ("character_strongman_01", "Character_Strongman_01"),
            };
            foreach (var (wrong, correct) in fixes)
                MoveFolderCaseSafe($"{playerRoot}/{wrong}", $"{playerRoot}/{correct}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            foreach (var l in Log) Debug.Log("[FixCasing] " + l);
        }

        [MenuItem("PrankMansion/Stage 1 - Reorganize Project To Doc Structure (Part 1)")]
        public static void Reorganize()
        {
            Log.Clear();
            Unmatched.Clear();

            RenameTopLevel();
            RenameRegionFolders();
            SortRoomProps();
            OrganizeCharactersAndParrot();
            OrganizeAnimations();
            OrganizeAudio();
            OrganizeVfx();
            CreateRemainingSkeleton();
            MoveScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WriteLog();
            Debug.Log("[MansionProjectReorganizer] Done. See " + LogPath);
        }

        // ---------------------------------------------------------------
        private static void RenameTopLevel()
        {
            MoveFolder("Assets/Project", "Assets/_Project");
            // Case-only rename ("models" -> "Models") needs a two-step hop on a
            // case-insensitive filesystem (Windows), otherwise Unity sees no change.
            MoveFolderCaseSafe("Assets/_Project/models", "Assets/_Project/Models");
        }

        private static void RenameRegionFolders()
        {
            MoveFolder("Assets/_Project/Models/Bedroom1-Bathroom1", "Assets/_Project/Models/Bedroom1_Bathroom1");
            MoveFolder("Assets/_Project/Models/Bedroom2-Bathroom2", "Assets/_Project/Models/Bedroom2_Bathroom2");
            MoveFolder("Assets/_Project/Models/Misc-Props", "Assets/_Project/Models/Misc_Global");
        }

        // ---------------------------------------------------------------
        // Part 4 physical-classification -> Static/Throwable split, for the six
        // room regions that have Static/Throwable children per Part 1. Misc_Global
        // and Architecture stay flat (no Static/Throwable) per Part 1's own tree.
        private static void SortRoomProps()
        {
            SortRegion("Foyer",
                staticFiles: new[]
                {
                    "Foyer_StaircaseDouble_01.glb", "Foyer_armchair_01.glb", "Foyer_bookshelftall_01.glb",
                    "Foyer_candelabraStanding_01.glb", "Foyer_chandelier_01.glb", "Foyer_clockgrand_01.glb",
                    "Foyer_coatrack_01.glb", "Foyer_coffeetable_01.glb", "Foyer_lightPanel_01.glb",
                    "Foyer_mirrorornate_01.glb", "Foyer_painting_01.glb", "Foyer_railing_01.glb",
                    "Foyer_rug_01.glb", "Foyer_sconce_01.glb", "Foyer_sidetable_01.glb", "Foyer_sofa__01.glb",
                    "Foyer_statue_01.glb", "Foyer_winerack_01.glb"
                },
                throwableFiles: new[]
                {
                    "Foyer_ashtray_01.glb", "Foyer_champagnebottles_01.glb", "Foyer_champagneglass_01.glb",
                    "Foyer_fruitbowl_01.glb", "Foyer_ottoman_01.glb", "Foyer_pianobench_01.glb",
                    "Foyer_pianogrand_01.glb", "Foyer_pillow_01.glb", "Foyer_vaseSmall_01.glb",
                    "Foyer_vaselarge_01.glb"
                });

            SortRegion("Kitchen",
                staticFiles: new[]
                {
                    "Kitchen_table_01.glb", "kitchen_Fullcombo_01.glb", "kitchen_door_01.glb", "kitchen_spicerack_01.glb"
                },
                throwableFiles: new[]
                {
                    "Kitchen_apple_01.glb", "Kitchen_banana_01.glb", "Kitchen_bananas_02.glb", "Kitchen_chair_01.glb",
                    "Kitchen_egg_01.glb", "Kitchen_eggs_02.glb", "Kitchen_flourbag_01.glb", "Kitchen_orange_01.glb",
                    "Kitchen_pan_01.glb", "Kitchen_plate_01.glb", "Kitchen_rollingpin_01.glb", "Kitchen_tomato_01.glb",
                    "Kitchen_watermelon_01.glb", "Kitchen_winebottle_01.glb", "kitchen_blender_01.glb",
                    "kitchen_bowl_01.glb", "kitchen_cookiejar_01.glb", "kitchen_cuttingboard_01.glb",
                    "kitchen_fruitbasket_01.glb", "kitchen_ketchupbottle_01.glb", "kitchen_kettle_01.glb",
                    "kitchen_knife_01.glb", "kitchen_knife_02.glb", "kitchen_milkcarton_01.glb", "kitchen_pot_01.glb",
                    "kitchen_toaster_01.glb", "kitchen_trashbin_01.glb"
                });

            SortRegion("Office",
                staticFiles: new[]
                {
                    "Office_Bookschelf_01.glb", "Office_clock_01.glb", "office_bulletinboard_01.glb",
                    "office_desk_01.glb", "office_meetingtable_01.glb", "office_plantpotted_01.glb",
                    "office_safe_01.glb", "office_shredder_01.glb", "office_sofaleather_01.glb", "office_worldmap_01.glb"
                },
                throwableFiles: new[]
                {
                    "Office_book_01.glb", "Office_book_02.glb", "Office_book_03.glb", "Office_book_04.glb",
                    "Office_book_05.glb", "Office_printer_01.glb", "Office_umbrella_01.glb", "office_book_06.glb",
                    "office_calendar_01.glb", "office_cardholder_01.glb", "office_chairwheels_01.glb",
                    "office_cigarashtray_01.glb", "office_deskfan_01.glb", "office_humidorbox_01.glb",
                    "office_lamp_01.glb", "office_meetingchair_01.glb", "office_monitor_01.glb",
                    "office_newtonscradle_01.glb", "office_pen_01.glb", "office_pen_02.glb", "office_pen_03.glb",
                    "office_penholder_01.glb", "office_pushpin_01.glb", "office_scissors_01.glb",
                    "office_stamp_01.glb", "office_stapler_01.glb", "office_tapedispenser_01.glb",
                    "office_wastebasketmetal_01.glb", "office_waterglass_01.glb", "office_waterpitcher_01.glb"
                });
            if (File.Exists("Assets/_Project/Models/Office/office_lamp_01.glb.meta") ||
                File.Exists("Assets/_Project/Models/Office/Static/office_lamp_01.glb") ||
                File.Exists("Assets/_Project/Models/Office/Throwable/office_lamp_01.glb"))
            {
                Unmatched.Add("office_lamp_01.glb: table has two distinct lamp entries " +
                    "(Office_LampDesk_01=carryable, Office_LampFloor_01=static-fall) and the file name " +
                    "doesn't say which. Filed under Throwable as a guess - please confirm in Stage 8.");
            }

            SortRegion("Bedroom1_Bathroom1",
                staticFiles: new[]
                {
                    "Bedroom1_bedking_01.glb", "bedroom1_Rug_01.glb", "bedroom1__bathtub_01.glb",
                    "bedroom1_bedsidetable_01.glb", "bedroom1_door_01.glb", "bedroom1_dresser_01.glb",
                    "bedroom1_mirrorDresser_01.glb", "bedroom1_mirror_01.glb", "bedroom1_painting_01.glb",
                    "bedroom1_showercurtain_01.glb", "bedroom1_sink_01.glb", "bedroom1_toilet_01.glb",
                    "bedroom1_wardrobe_01.glb", "wooden_door.glb"
                },
                throwableFiles: new[]
                {
                    "Bedroom1_lampside_01.glb", "bedroom1__towel_01.glb", "bedroom1_bathmat_01.glb",
                    "bedroom1_blanket_01.glb", "bedroom1_jewelrybox_01.glb", "bedroom1_laundrybasket_01.glb",
                    "bedroom1_perfume_01.glb", "bedroom1_perfume_02.glb", "bedroom1_pillow_01.glb",
                    "bedroom1_pillow_02.glb", "bedroom1_plunger_01.glb", "bedroom1_readingchair_01.glb",
                    "bedroom1_shampoo_01.glb", "bedroom1_shoerack_01.glb", "bedroom1_soapbottle_01.glb",
                    "bedroom1_toothbrushcup_01.glb"
                });
            Unmatched.Add("Bedroom1_Bathroom1/bedroom1__towel_01.glb and wooden_door.glb: not named in " +
                "Part 4.5's table (no bathroom1 towel listed there, and 'wooden_door' is generic). Placed by " +
                "physical nature (Throwable / Static respectively); confirm identity in Stage 9.");

            SortRegion("Bedroom2_Bathroom2",
                staticFiles: new[]
                {
                    "bedroom2_Showerhead_01.glb", "bedroom2_bedQueen_01.glb", "bedroom2_bookshelf_01.glb",
                    "bedroom2_ceilingfan_01.glb", "bedroom2_desk_01.glb", "bedroom2_door_01.glb",
                    "bedroom2_mirror_01.glb", "bedroom2_rug_01.glb", "bedroom2_sink_01.glb",
                    "bedroom2_toilet_01.glb", "bedroom2_towelrack_01.glb", "bedroom2_wardrobe_01.glb"
                },
                throwableFiles: new[]
                {
                    "bedroom2_bathmat_01.glb", "bedroom2_beanbag_01.glb", "bedroom2_chair_01.glb",
                    "bedroom2_clock_01.glb", "bedroom2_guiter_01.glb", "bedroom2_pillow_01.glb",
                    "bedroom2_soapdispenser_01.glb"
                });

            SortRegion("Gym",
                staticFiles: new[]
                {
                    "Gym_treadmill_01.glb", "Gym_weightsracks_01.glb", "gym_bench_01.glb", "gym_bike_01.glb",
                    "gym_punchingbag_01.glb"
                },
                throwableFiles: new[]
                {
                    "Gym_dumbbell_01.glb", "Gym_dumbbell_02.glb", "Gym_dumbbell_03.glb", "Gym_yogamat_01.glb",
                    "gym_barbell_01.glb", "gym_barbell_02.glb", "gym_barbell_03.glb", "gym_dumbbelllight_01.glb",
                    "gym_towel_01.glb", "gym_waterbottle_01.glb"
                });
        }

        private static void SortRegion(string region, string[] staticFiles, string[] throwableFiles)
        {
            string root = $"Assets/_Project/Models/{region}";
            if (!AssetDatabase.IsValidFolder(root))
            {
                Log.Add($"SKIP region '{region}': folder not found at {root}");
                return;
            }

            string staticDir = EnsureFolder(root, "Static");
            string throwDir = EnsureFolder(root, "Throwable");

            foreach (var f in staticFiles) MoveFile($"{root}/{f}", $"{staticDir}/{f}");
            foreach (var f in throwableFiles) MoveFile($"{root}/{f}", $"{throwDir}/{f}");

            // Anything left directly in the region root afterwards was not in either
            // list above - flag it rather than silently leaving it unsorted.
            foreach (var guid in AssetDatabase.FindAssets("t:Object", new[] { root }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(p)?.Replace('\\', '/') == root)
                    Unmatched.Add($"{region}: left unsorted in region root (not in Static/Throwable list): {p}");
            }
        }

        // ---------------------------------------------------------------
        private static void OrganizeCharactersAndParrot()
        {
            string playerRoot = "Assets/_Project/Models/Characters/Player";
            var characterFiles = new (string file, string folder)[]
            {
                ("character_slowpoke_01.fbx", "Character_Slowpoke_01"),
                ("character_strongman_01.fbx", "Character_Strongman_01"),
                ("Character_strobgpush_01.fbx", "Character_StrongPush_01"),
                ("character_Bigears_01.fbx", "Character_BigEars_01"),
                ("character_QuietSteps_01.fbx", "Character_QuietSteps_01"),
                ("character_QuickPourr_01.fbx", "Character_QuickPour_01"),
                ("character_Featherweight_01.fbx", "Character_Featherweight_01"),
            };
            foreach (var (file, folder) in characterFiles)
            {
                string dest = EnsureFolder(playerRoot, folder);
                MoveFile($"{playerRoot}/{file}", $"{dest}/{file}");
            }

            string parrotRoot = "Assets/_Project/Models/Characters/Parrot";
            string modelDir = EnsureFolder(parrotRoot, "Parrot_Model_01");
            MoveFile($"{parrotRoot}/parrot_modelt_01.glb", $"{modelDir}/parrot_modelt_01.glb");
            string cageDir = EnsureFolder(parrotRoot, "Parrot_Cage_01");
            MoveFile($"{parrotRoot}/paroott_cage_01.glb", $"{cageDir}/paroott_cage_01.glb");
        }

        private static void OrganizeAnimations()
        {
            string animRoot = "Assets/_Project/Animations";
            EnsureFolder("Assets/_Project", "Animations");
            string playerCommon = EnsureFolder(animRoot, "Player_Common");
            EnsureFolder(animRoot, "Parrot");

            string playerRoot = "Assets/_Project/Models/Characters/Player";
            // Only the clips whose names clearly match Part 5.3's common-animation
            // list are moved. Ambiguous/vendor-style clips (RM_Roll_*, ShadowBoxing_*)
            // are left in place - see the log.
            var commonClips = new[]
            {
                "Fall.fbx", "Idle.fbx", "idleCarrylight.fbx", "interact.fbx", "Jumpstart.fbx", "Land.fbx",
                "Push.fbx", "Run.fbx", "Run_silly.fbx", "StrafeLeft.fbx", "StrafeRight.fbx", "Throw.fbx",
                "Walk.fbx", "WalkBackwards_silly.fbx", "WalkCarryHeavy.fbx", "Walkcarrylight.fbx",
                "WalkingBackwards.fbx", "Walk_silly.fbx"
            };
            foreach (var clip in commonClips) MoveFile($"{playerRoot}/{clip}", $"{playerCommon}/{clip}");

            Unmatched.Add("Characters/Player root: RM_Roll_back/left45/right45/front/front_left45/front_right45 " +
                "and ShadowBoxing_mixamo.fbx left in place - not named in Part 5.3's common-animation list, " +
                "not renamed/moved to avoid guessing. Also left in place: ~20 vendor mocap/animation package " +
                "folders (Combat, Dancing, Destiny, Guns, Magic, Meme, Misc, MoCapCentral, Movement, Music, " +
                "Sitting, Sports, StarWars, SuperHero, VanillaLoopStudio, Zombie, Brawler Animations Sample, " +
                "Free Interaction Animation, IdlesMocapPack_noFACE, RokokoTVContest_MocapAssets, New Folder) " +
                "- Part 1 does not govern raw third-party source libraries, only the project's own curated assets.");
        }

        private static void OrganizeAudio()
        {
            string audioRoot = "Assets/_Project/Audio";
            string sfxRoot = $"{audioRoot}/SFX";
            EnsureFolder(sfxRoot, "Physics");
            EnsureFolder(sfxRoot, "Fart");
            EnsureFolder(sfxRoot, "Slip");
            EnsureFolder(sfxRoot, "Doors");
            EnsureFolder(sfxRoot, "UI");

            string voiceRoot = EnsureFolder(audioRoot, "Voice");
            string parrotVoice = EnsureFolder(voiceRoot, "Parrot");

            for (int i = 1; i <= 5; i++)
            {
                string f = $"SFX_PARROTLAUGH_{i:00}.mp3";
                MoveFile($"{sfxRoot}/{f}", $"{parrotVoice}/{f}");
            }
            for (int i = 1; i <= 7; i++)
            {
                string f = $"SFX_parrotmock_EN_{i:00}.mp3";
                MoveFile($"{sfxRoot}/{f}", $"{parrotVoice}/{f}");
            }

            Unmatched.Add("Audio/SFX: vendor packs ('Cartoon Game Sound 2.0', 'Casual Game Sounds U6', " +
                "'UI SFX Free Pack') left in place, not sorted into Physics/Fart/Slip/Doors/UI - that curation " +
                "is Stage 13's job (Part 13), not a folder rename.");
        }

        private static void OrganizeVfx()
        {
            string vfxRoot = "Assets/_Project/VFX";
            foreach (var name in new[]
                     {
                         "FartSmoke", "FartIgnite", "DustImpact", "FlourExplosion", "SplashPour", "StarsRagdoll",
                         "SparkleGeneric"
                     })
            {
                EnsureFolder(vfxRoot, name);
            }
        }

        private static void CreateRemainingSkeleton()
        {
            string proj = "Assets/_Project";
            EnsureFolder(proj, "Prefabs");
            EnsureFolder($"{proj}/Prefabs", "Environment");
            EnsureFolder($"{proj}/Prefabs", "Characters");
            EnsureFolder($"{proj}/Prefabs", "Props");
            EnsureFolder($"{proj}/Prefabs", "UI");

            EnsureFolder(proj, "Materials");

            EnsureFolder(proj, "UI");
            EnsureFolder($"{proj}/UI", "Fonts");
            EnsureFolder($"{proj}/UI", "Icons");
            EnsureFolder($"{proj}/UI", "Backgrounds");

            EnsureFolder(proj, "Localization");
            EnsureFolder($"{proj}/Localization", "StringTables");

            EnsureFolder(proj, "Plugins");

            string scripts = $"{proj}/Scripts";
            EnsureFolder(scripts, "Networking");
            EnsureFolder(scripts, "Player");
            EnsureFolder(scripts, "Entities");
            EnsureFolder(scripts, "UI");
            EnsureFolder(scripts, "Localization");
        }

        private static void MoveScenes()
        {
            string dest = EnsureFolder("Assets/_Project", "Scenes");
            MoveFile("Assets/Scenes/SampleScene.unity", $"{dest}/SampleScene.unity");
            MoveFile("Assets/Scenes/Scene_04_Mansion.unity", $"{dest}/Scene_04_Mansion.unity");

            if (AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                var remaining = AssetDatabase.FindAssets("", new[] { "Assets/Scenes" });
                if (remaining.Length == 0)
                {
                    AssetDatabase.DeleteAsset("Assets/Scenes");
                    Log.Add("Deleted now-empty Assets/Scenes.");
                }
                else
                {
                    Unmatched.Add("Assets/Scenes still has leftover content, not deleted: " +
                        string.Join(", ", remaining.Select(AssetDatabase.GUIDToAssetPath)));
                }
            }
        }

        // ---------------------------------------------------------------
        private static string EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    Log.Add($"WARN parent folder missing, cannot create '{path}': {parent}");
                    return path;
                }
                AssetDatabase.CreateFolder(parent, name);
                Log.Add($"Created folder: {path}");
            }
            return path;
        }

        private static void MoveFolder(string oldPath, string newPath)
        {
            if (!AssetDatabase.IsValidFolder(oldPath))
            {
                Log.Add($"SKIP move folder (source missing): {oldPath}");
                return;
            }
            if (AssetDatabase.IsValidFolder(newPath))
            {
                Log.Add($"SKIP move folder (destination already exists): {oldPath} -> {newPath}");
                return;
            }
            string err = AssetDatabase.MoveAsset(oldPath, newPath);
            Log.Add(string.IsNullOrEmpty(err)
                ? $"Renamed folder: {oldPath} -> {newPath}"
                : $"FAILED folder move {oldPath} -> {newPath}: {err}");
        }

        private static void MoveFolderCaseSafe(string oldPath, string newPath)
        {
            if (!AssetDatabase.IsValidFolder(oldPath))
            {
                Log.Add($"SKIP case-safe move (source missing): {oldPath}");
                return;
            }
            string tmp = oldPath + "__tmp_rename";
            string err1 = AssetDatabase.MoveAsset(oldPath, tmp);
            if (!string.IsNullOrEmpty(err1))
            {
                Log.Add($"FAILED case-safe move step 1 {oldPath} -> {tmp}: {err1}");
                return;
            }
            string err2 = AssetDatabase.MoveAsset(tmp, newPath);
            Log.Add(string.IsNullOrEmpty(err2)
                ? $"Renamed folder (case-fix): {oldPath} -> {newPath}"
                : $"FAILED case-safe move step 2 {tmp} -> {newPath}: {err2}");
        }

        private static void MoveFile(string oldPath, string newPath)
        {
            if (!File.Exists(oldPath))
            {
                Unmatched.Add($"Expected file not found (not moved): {oldPath}");
                return;
            }
            if (File.Exists(newPath))
            {
                Log.Add($"SKIP move file (destination already exists): {oldPath} -> {newPath}");
                return;
            }
            string err = AssetDatabase.MoveAsset(oldPath, newPath);
            if (!string.IsNullOrEmpty(err))
                Log.Add($"FAILED file move {oldPath} -> {newPath}: {err}");
        }

        private static void WriteLog()
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new List<string>
            {
                "=== Stage 1 - Project Reorganized To Match Part 1 (Folder Structure) ===",
                "",
                "--- Actions taken ---"
            };
            lines.AddRange(Log);
            lines.Add("");
            lines.Add("--- Left as-is / needs your confirmation later ---");
            if (Unmatched.Count == 0) lines.Add("(none)");
            else lines.AddRange(Unmatched.Distinct());

            File.WriteAllLines(LogPath, lines);
        }
    }
}

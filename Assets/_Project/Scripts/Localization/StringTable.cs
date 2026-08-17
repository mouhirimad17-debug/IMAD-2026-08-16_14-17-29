using System.Collections.Generic;

namespace PrankMansion.Localization
{
    /// <summary>
    /// Part 12.1's single central text table for the entire project ("جدول نصوص
    /// مركزي واحد فقط لكل المشروع"), two columns only (English, Darija written in
    /// real Arabic script). Every key here is a UI string somewhere in Parts 11.1
    /// through 11.7 (Part 12.3's full scope: main menu, create/join room, all
    /// seven character ability descriptions, every error message, in-round HUD,
    /// end-of-round screen, settings screen).
    ///
    /// DECISION: Unity's official com.unity.localization package (String Table
    /// assets, the literal reading of "نظام الترجمة الرسمي المتكامل") is not
    /// installed in Packages/manifest.json, and this pipeline has no live Editor/
    /// internet session available to add and resolve a new package safely mid-
    /// stage. This static table is the same architecture (one key, two language
    /// columns, no literal UI text) implemented in code instead of a ScriptableObject
    /// asset - matching how CharacterProfile (Part 5) and MansionSpec (Part 3)
    /// already store their own central tables directly in code rather than as
    /// external data assets. LocalizationManager.Get is the sole read path, so
    /// swapping the backing store for a real String Table asset later is a
    /// contained change.
    /// </summary>
    public static class StringTable
    {
        public struct Entry
        {
            public string en;
            public string darija;
            public Entry(string en, string darija) { this.en = en; this.darija = darija; }
        }

        public static readonly Dictionary<string, Entry> Map = new Dictionary<string, Entry>
        {
            // --- Part 11.1: Language select ---------------------------------
            ["lang.title"] = new Entry("PRANK MANSION", "PRANK MANSION"),
            ["lang.english"] = new Entry("English", "English"),
            ["lang.darija"] = new Entry("الدارجة", "الدارجة"),

            // --- Part 11.1: Name entry ---------------------------------------
            ["name.title"] = new Entry("What's your name?", "شنو سميتك؟"),
            ["name.continue"] = new Entry("Continue", "كمل"),

            // --- Part 11.2: Character select ----------------------------------
            ["charselect.confirm"] = new Entry("Confirm", "أكد"),

            // Character proper names (displayName) are intentionally NOT keyed
            // here - Part 12.3 excludes "أسماء علم خاصة" the same way it excludes
            // player/room names; only each character's ABILITY DESCRIPTION is
            // in-scope for translation.
            ["char.bomba.desc"] = new Entry(
                "Walks and runs 15% slower than everyone else, with no compensating bonus.",
                "كيمشي وكيجري ب15% أبطأ من الجميع، بلا أي تعويض."),

            ["char.zico.desc"] = new Entry(
                "Carries heavy objects solo with only a 30% speed penalty instead of 70%, and holds off the wind for a full 3 seconds instead of 1.",
                "كيحمل الأغراض الثقيلة بوحدو بخسارة سرعة ديال 30% غير عوض 70%، وكيصبر على الريح 3 ثواني كاملة عوض ثانية وحدة."),

            ["char.doran.desc"] = new Entry(
                "Pushes objects 40% harder than the standard push force.",
                "كيدفع الأغراض بقوة زايدة ب40% على القوة العادية."),

            ["char.reno.desc"] = new Entry(
                "Hears footsteps and doors from 15 meters away instead of 8, with a direction arrow only he can see.",
                "كيسمع خطوات الرجلين والأبواب من 15 متر عوض 8، وعندو سهم اتجاه غير هو لي كيشوفو."),

            ["char.fifi.desc"] = new Entry(
                "Footsteps only carry 4 meters to other players instead of 8 - much harder to hear coming.",
                "خطواتها ما كتنسمعش غير من 4 أمتار عوض 8 - صعيب بزاف تسمع بلي جاية."),

            ["char.nouka.desc"] = new Entry(
                "Throws objects 25% harder than the standard throw force.",
                "كترمي الأغراض بقوة زايدة ب25% على القوة العادية."),

            ["char.bouf.desc"] = new Entry(
                "Wind ignition launches him mostly forward (60% horizontal / 40% vertical) instead of mostly straight up.",
                "اشتعال الريح كيطيرو للقدام بزاف (60% أفقي / 40% عمودي) عوض للفوق بشكل مباشر."),

            // --- Part 11.3: Main menu ------------------------------------------
            ["menu.logo"] = new Entry("PRANK MANSION", "PRANK MANSION"),
            ["menu.createroom"] = new Entry("Create Room", "أنشئ غرفة"),
            ["menu.joinroom"] = new Entry("Join Room", "انضم لغرفة"),
            ["menu.switchcharacter"] = new Entry("Switch Character", "بدّل الشخصية"),
            ["menu.settings"] = new Entry("Settings", "الإعدادات"),
            ["menu.quit"] = new Entry("Quit", "خروج"),

            // --- Part 11.4: Create room -----------------------------------------
            ["createroom.namelabel"] = new Entry("Room Name", "اسم الغرفة"),
            ["createroom.maxplayerslabel"] = new Entry("Max Players", "أقصى عدد لاعبين"),
            ["createroom.durationlabel"] = new Entry("Round Duration", "مدة الجولة"),
            ["createroom.duration5min"] = new Entry("5 min", "5 دقايق"),
            ["createroom.duration10min"] = new Entry("10 min", "10 دقايق"),
            ["createroom.create"] = new Entry("Create", "أنشئ"),

            // --- Part 11.4: Join room -------------------------------------------
            ["joinroom.searchlabel"] = new Entry("Search Rooms", "قلّب على غرفة"),
            ["joinroom.err.wrongcode"] = new Entry("Incorrect code", "الكود غالط"),
            ["joinroom.err.full"] = new Entry("Room is full", "الغرفة كاملة"),
            ["joinroom.err.notfound"] = new Entry("Room not found", "الغرفة ماكايناش"),

            // --- Part 10.4/11.x: Waiting room ------------------------------------
            ["waitingroom.ready"] = new Entry("Ready", "واجد"),
            ["waitingroom.notready"] = new Entry("Not Ready", "ماواجدش"),
            ["waitingroom.host"] = new Entry("(Host)", "(المضيف)"),
            ["waitingroom.startgame"] = new Entry("Start Game", "بدا اللعب"),
            ["waitingroom.leave"] = new Entry("Leave", "خروج"),
            ["waitingroom.playerscount"] = new Entry("{0} players", "{0} لاعبين"),

            // --- Part 9/11.5: Gameplay HUD ---------------------------------------
            ["hud.team1"] = new Entry("Team 1", "الفريق 1"),
            ["hud.team2"] = new Entry("Team 2", "الفريق 2"),

            // --- Part 11.6: End round screen -------------------------------------
            ["endround.tie"] = new Entry("It's a Tie!", "تعادل!"),
            ["endround.winner"] = new Entry("{0} Wins!", "{0} ربح!"),
            ["endround.scores"] = new Entry("Team 1: {0}    Team 2: {1}", "الفريق 1: {0}    الفريق 2: {1}"),
            ["endround.playagain"] = new Entry("Play Again", "لعب من جديد"),
            ["endround.mainmenu"] = new Entry("Main Menu", "القائمة الرئيسية"),

            // --- Part 11.7: Settings ----------------------------------------------
            ["settings.voicechatinfo"] = new Entry(
                "Voice chat runs entirely through Steam. Muting or adjusting a specific friend's volume is managed from the Steam overlay, not here.",
                "الدردشة الصوتية كتمر بالكامل عبر Steam. كتم أو تعديل صوت صاحبك كيتدار من واجهة Steam، ماشي من هنا."),
            ["settings.back"] = new Entry("Back", "رجوع"),

            // --- Part 10.6: Host disconnect (no screen surfaces this yet - see
            // Stage17 decisions log; key exists so the string is ready the moment
            // Stage 15/16's disconnect-handling UI is built) ----------------------
            ["error.hostdisconnected"] = new Entry("The host disconnected", "المضيف قطع الاتصال"),

            // --- Part 16.1: stuck-detection auto-recovery toast -------------------
            ["notify.stuckrecovered"] = new Entry(
                "You seemed stuck, so we moved you to a safe spot.",
                "بان بلي عالق، خديناك لبلاصة آمنة."),

            // --- Part 19.3: Credits screen ----------------------------------------
            ["credits.title"] = new Entry("Credits", "شكر وتقدير"),
            ["credits.developedby"] = new Entry("Developed by", "من تطوير"),
        };
    }
}

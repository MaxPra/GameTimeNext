using GameTimeNext.Core.Framework.Components.Base;

namespace GameTimeNext.Core.Application.Profiles.Components
{
    public class CExecutables : GTNComponent<CExecutables>
    {

        public Dictionary<string, bool> KeyValuePairs { get; set; } = new Dictionary<string, bool>();


        public CExecutables(string rawValue) : base(rawValue) { }

        public CExecutables() { RawValue = string.Empty; }

        public void Initialize(Dictionary<string, bool> keyValuePairs)
        {
            KeyValuePairs = keyValuePairs;
        }

        public static Dictionary<string, bool> ConvertListToDictionary(List<string> list, bool sortOut)
        {
            Dictionary<string, bool> dic = new Dictionary<string, bool>();

            if (list.Count == 0)
                return dic;

            foreach (string lItem in list)
            {
                bool isActive = !(IsIgnored(lItem) && sortOut); // true, wenn kein Treffer in Ignore-Liste
                dic[lItem] = isActive;
            }

            return dic;
        }

        private static readonly string[] IgnoreSubstrings =
        {
            // Launcher/Starter
            "launcher","startapp","bootstrap","starter","client","host","kickoff",

            // Updater/Installer/Patcher
            "update","updater","upgrade","patch","patcher",
            "install","installer","setup","maintenancetool",
            "uninstall","unins","repair",

            // Crash/Report/Debug/Tools
            "crash","crashhandler","error","report","reporter","dumploader","dump",
            "diagnostic","logger","trace","debug","profiler","profiling","minidump",

            // Anti-Cheat
            "eac","easyanticheat","battleye","anticheat","vac","punkbuster","fairfight","nprotect","xac","ggxx",

            // Stores/Plattformen/Overlays
            "steam","steamservice","steamwebhelper","gameoverlayui",
            "epicgames","egs","origin","uplay","connect","gog","galaxy",
            "battlenet","blizzard","bethesda","rockstar","rslauncher",
            "riotclient","valorantclient",

            // Helper/Services/Misc Tools
            "helper","webhelper","service","daemon","tool","tools","manager",
            "config","settings","options","overlay","telemetry","watchdog",
            "metrics","monitor","observer","notifier","autoconfig",

            // Benchmark/Tests
            "benchmark","stress","loadtest","qa","prototype","unittest","staging","sandbox","testbuild","perf",

            // Editor/SDK/Dev
            "editor","sdk","devtool","development","modtool","builder","workshop","assettool","leveltool",

            // CLI/Shell
            "console","shell","command","cmd","cli","powershell",

            // Netzwerk/Voice
            "voice","chat","server","dedicated","relay","proxy","upnp","matchmaking","auth","login","connectivity","p2p",

            // Grafik/Engine-Begleitprozesse (sicher)
            "shader","shadercache","compile","compiler","worker",  // UE3ShaderCompileWorker.exe
            "physx","systemsoftware",                              // PhysX_..._SystemSoftware.exe
            "vcredist","vc_redist","redist","redistributable",     // vcredist_x86.exe
            "directx","dxsetup","xna","dotnet","netfx","cef","chromium"
           };


        private static bool IsIgnored(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var lower = name.ToLowerInvariant();
            return IgnoreSubstrings.Any(sub => lower.Contains(sub));
        }
    }
}

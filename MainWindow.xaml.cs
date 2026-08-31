using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MazliBoost
{
    public partial class MainWindow : Window
    {
        // =========================================================
        // GAME
        // =========================================================

        private int detectedGamePid = -1;
        private string detectedGameName = "No game detected";
        private string detectedProcessName = "";

        private readonly Dictionary<string, string> knownGames =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                { "GTA5", "Grand Theft Auto V" },
                { "GTA5Enhanced", "Grand Theft Auto V Enhanced" },
                { "RDR2", "Red Dead Redemption 2" },
                { "WorldOfTanks", "World of Tanks" },
                { "WorldOfTanksCE", "World of Tanks" },
                { "cs2", "Counter-Strike 2" },
                { "VALORANT-Win64-Shipping", "VALORANT" },
                { "FortniteClient-Win64-Shipping", "Fortnite" },
                { "RobloxPlayerBeta", "Roblox" },
                { "eldenring", "Elden Ring" },
                { "RocketLeague", "Rocket League" },
                { "Overwatch", "Overwatch" },
                { "OverwatchTest", "Overwatch" },
                { "Terraria", "Terraria" },
                { "HogwartsLegacy", "Hogwarts Legacy" },
                { "Cyberpunk2077", "Cyberpunk 2077" },
                { "witcher3", "The Witcher 3" },
                { "BeamNG.drive", "BeamNG.drive" },
                { "FactoryGame", "Satisfactory" }
            };


        // =========================================================
        // LANGUAGE
        // =========================================================

        private string currentLanguage = "English";

        private readonly Dictionary<string, Dictionary<string, string>>
            translations =
            new Dictionary<string, Dictionary<string, string>>(
                StringComparer.OrdinalIgnoreCase)
            {
                {
                    "English",
                    new Dictionary<string, string>
                    {
                        { "subtitle", "Windows Gaming Performance Utility" },
                        { "languages", "Languages" },
                        { "ready", "READY" },

                        { "detectedGame", "DETECTED GAME" },
                        { "noGame", "No game detected" },
                        { "detectAgain", "DETECT AGAIN" },
                        { "detectHint", "Start a game and detect it." },

                        { "gamingOptimizations", "GAMING OPTIMIZATIONS" },
                        { "choose", "Choose what MázliBoost should apply." },
                        { "independent", "Each optimization can be enabled or disabled independently." },

                        { "highPerformance", "High Performance Power Plan" },
                        { "gameMode", "Windows Game Mode" },
                        { "gameDvr", "Disable Game DVR / Background Capture" },
                        { "gamePriority", "Optimize Game Process Priority" },
                        { "memoryCleanup", "Background Memory Cleanup" },

                        { "selected", "Selected" },
                        { "apply", "APPLY SELECTED OPTIMIZATIONS" },
                        { "moreTweaks", "MORE OPTIMIZATIONS & TWEAKS" },

                        { "system", "SYSTEM" },
                        { "currentHardware", "Current hardware" },
                        { "memory", "MEMORY" },
                        { "powerPlan", "POWER PLAN" },

                        { "advanced", "ADVANCED" },
                        { "future", "More tweaks available." },
                        { "futureDescription", "Open More Optimizations & Tweaks to explore additional options." },

                        { "statusReady", "Status: Ready" },
                        { "statusDetecting", "Status: Detecting game..." },
                        { "statusDetected", "Status: Game detected" },
                        { "statusOptimizing", "Status: Applying selected optimizations..." },
                        { "statusComplete", "Status: Optimization complete" },

                        { "footer", "Performance without the snake oil." },

                        { "nothingSelected", "Select at least one optimization." },
                        { "optimizationComplete", "MázliBoost completed the selected optimizations." },
                        { "noOptimization", "No optimization could be applied." },
                        { "detected", "Detected game" },
                        { "noDetected", "No game was detected." },

                        { "tweaksTitle", "MORE OPTIMIZATIONS & TWEAKS" },
                        { "tweaksSubtitle", "Additional Windows and gaming optimizations." },
                        { "back", "←  BACK" },

                        { "windowsTweaks", "WINDOWS" },
                        { "gamingTweaks", "GAMING" },

                        { "visualEffects", "Visual Effects to best performance" },
                        { "visualEffectsDescription", "Changes Windows appearance - Revertable by unchecking." },

                        { "transparency", "Disable Transparency Effects" },
                        { "transparencyDescription", "Changes Windows appearance - Revertable by unchecking." },

                        { "menuDelay", "Reduce Menu Show Delay" },
                        { "menuDelayDescription", "Makes Windows menus appear faster - Revertable by unchecking." },

                        { "mouseHover", "Reduce Mouse Hover Delay" },
                        { "mouseHoverDescription", "Makes hover tooltips appear faster - Revertable by unchecking." },

                        { "tweaksGameMode", "Windows Game Mode" },
                        { "tweaksGameModeDescription", "Enables Windows Game Mode." },

                        { "tweaksGameDvr", "Disable Game DVR / Background Capture" },
                        { "tweaksGameDvrDescription", "Reduces background capture activity." },

                        { "tweaksHighPerformance", "High Performance Power Plan" },
                        { "tweaksHighPerformanceDescription", "Favors performance over power saving." },

                        { "tweaksInfoTitle", "ABOUT THESE TWEAKS" },
                        { "tweaksInfoText", "MázliBoost only applies the selected changes. Registry-based tweaks are used only where appropriate and are designed to be reversible." }
                    }
                },

                {
                    "Magyar",
                    new Dictionary<string, string>
                    {
                        { "subtitle", "Windows Gaming teljesítményoptimalizáló" },
                        { "languages", "Nyelvek" },
                        { "ready", "KÉSZ" },

                        { "detectedGame", "FELISMERT JÁTÉK" },
                        { "noGame", "Nincs felismert játék" },
                        { "detectAgain", "ÚJRA FELISMERÉS" },
                        { "detectHint", "Indíts el egy játékot, majd indítsd újra a felismerést." },

                        { "gamingOptimizations", "JÁTÉKOPTIMALIZÁLÁSOK" },
                        { "choose", "Válaszd ki, mit alkalmazzon a MázliBoost." },
                        { "independent", "Minden optimalizálás külön-külön be- vagy kikapcsolható." },

                        { "highPerformance", "High Performance energiaellátási séma" },
                        { "gameMode", "Windows Game Mode" },
                        { "gameDvr", "Game DVR / háttérfelvétel kikapcsolása" },
                        { "gamePriority", "Játékfolyamat prioritásának optimalizálása" },
                        { "memoryCleanup", "Háttérmemória tisztítása" },

                        { "selected", "Kiválasztva" },
                        { "apply", "KIVÁLASZTOTTAK ALKALMAZÁSA" },
                        { "moreTweaks", "TOVÁBBI OPTIMALIZÁLÁSOK ÉS TWEAKEK" },

                        { "system", "RENDSZER" },
                        { "currentHardware", "Jelenlegi hardver" },
                        { "memory", "MEMÓRIA" },
                        { "powerPlan", "ENERGIAELLÁTÁSI SÉMA" },

                        { "advanced", "HALADÓ" },
                        { "future", "További tweakek elérhetők." },
                        { "futureDescription", "Nyisd meg a További optimalizálások és tweakek menüt további lehetőségekért." },

                        { "statusReady", "Állapot: Kész" },
                        { "statusDetecting", "Állapot: Játék felismerése..." },
                        { "statusDetected", "Állapot: Játék felismerve" },
                        { "statusOptimizing", "Állapot: Kiválasztott optimalizálások alkalmazása..." },
                        { "statusComplete", "Állapot: Optimalizálás kész" },

                        { "footer", "Teljesítmény sallangok nélkül." },

                        { "nothingSelected", "Válassz ki legalább egy optimalizálást." },
                        { "optimizationComplete", "A MázliBoost alkalmazta a kiválasztott optimalizálásokat." },
                        { "noOptimization", "Egyetlen optimalizálást sem sikerült alkalmazni." },
                        { "detected", "Felismert játék" },
                        { "noDetected", "Nem sikerült játékot felismerni." },

                        { "tweaksTitle", "TOVÁBBI OPTIMALIZÁLÁSOK ÉS TWEAKEK" },
                        { "tweaksSubtitle", "További Windows- és játékoptimalizálások." },
                        { "back", "←  VISSZA" },

                        { "windowsTweaks", "WINDOWS" },
                        { "gamingTweaks", "JÁTÉK" },

                        { "visualEffects", "Vizuális effektek a legjobb teljesítményhez" },
                        { "visualEffectsDescription", "Megváltoztatja a Windows megjelenését – a kikapcsolással visszaállítható." },

                        { "transparency", "Átlátszósági effektek kikapcsolása" },
                        { "transparencyDescription", "Megváltoztatja a Windows megjelenését – a kikapcsolással visszaállítható." },

                        { "menuDelay", "Menük megjelenési idejének csökkentése" },
                        { "menuDelayDescription", "Gyorsabban jelennek meg a Windows menüi – a kikapcsolással visszaállítható." },

                        { "mouseHover", "Egér-hover késleltetés csökkentése" },
                        { "mouseHoverDescription", "Gyorsabban jelennek meg a hover tippek – a kikapcsolással visszaállítható." },

                        { "tweaksGameMode", "Windows Game Mode" },
                        { "tweaksGameModeDescription", "Bekapcsolja a Windows Game Mode-ot." },

                        { "tweaksGameDvr", "Game DVR / háttérfelvétel kikapcsolása" },
                        { "tweaksGameDvrDescription", "Csökkenti a háttérben futó rögzítési tevékenységet." },

                        { "tweaksHighPerformance", "High Performance energiaellátási séma" },
                        { "tweaksHighPerformanceDescription", "A teljesítményt részesíti előnyben az energiatakarékossággal szemben." },

                        { "tweaksInfoTitle", "A TWEAKEKRŐL" },
                        { "tweaksInfoText", "A MázliBoost csak a kiválasztott módosításokat alkalmazza. A registry-alapú tweakeket csak ott használjuk, ahol indokoltak, és visszaállíthatóra tervezzük őket." }
                    }
                }
            };


        // =========================================================
        // REGISTRY BACKUP
        // =========================================================

        private class RegistryBackup
        {
            public bool Existed { get; set; }
            public object Value { get; set; }
            public RegistryValueKind Kind { get; set; }
        }

        private readonly Dictionary<string, RegistryBackup>
            registryBackups =
                new Dictionary<string, RegistryBackup>();

        // =========================================================
        // POWER PLAN BACKUP
        // =========================================================

        private string previousPowerPlanGuid = null;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MainWindow()
        {
            InitializeComponent();

            LoadSystemInfo();
            DetectGame();
            UpdateSelectionCounter();
            ApplyLocalization();

            MainContent.Opacity = 0;

            StartSplashAnimation();
        }

        // =========================================================
        // LOCALIZATION
        // =========================================================

        private string T(string key)
        {
            Dictionary<string, string> language;

            if (translations.TryGetValue(
                    currentLanguage,
                    out language))
            {
                string value;

                if (language.TryGetValue(
                        key,
                        out value))
                {
                    return value;
                }
            }

            return key;
        }

        private void ApplyLocalization()
        {
            AppSubtitleText.Text = T("subtitle");
            LanguageLabelText.Text = T("languages");
            ReadyText.Text = T("ready");

            DetectedGameLabelText.Text = T("detectedGame");
            DetectButton.Content = T("detectAgain");

            GamingOptimizationsLabelText.Text =
                T("gamingOptimizations");

            ChooseText.Text = T("choose");
            IndependentText.Text = T("independent");

            HighPerformanceCheck.Content =
                T("highPerformance");

            GameModeCheck.Content =
                T("gameMode");

            GameDvrCheck.Content =
                T("gameDvr");

            GamePriorityCheck.Content =
                T("gamePriority");

            MemoryCleanupCheck.Content =
                T("memoryCleanup");

            BoostButton.Content = T("apply");
            MoreTweaksButton.Content = T("moreTweaks");

            SystemLabelText.Text = T("system");
            CurrentHardwareText.Text =
                T("currentHardware");

            MemoryLabelText.Text = T("memory");
            PowerPlanLabelText.Text = T("powerPlan");

            AdvancedLabelText.Text = T("advanced");
            FutureText.Text = T("future");
            FutureDescriptionText.Text =
                T("futureDescription");

            FooterText.Text = T("footer");

            TweaksTitleText.Text = T("tweaksTitle");
            TweaksSubtitleText.Text = T("tweaksSubtitle");
            BackButton.Content = T("back");

            WindowsTweaksHeader.Text =
                T("windowsTweaks");

            GamingTweaksHeader.Text =
                T("gamingTweaks");

            VisualEffectsCheck.Content =
                T("visualEffects");

            VisualEffectsDescription.Text =
                T("visualEffectsDescription");

            TransparencyCheck.Content =
                T("transparency");

            TransparencyDescription.Text =
                T("transparencyDescription");

            MenuDelayCheck.Content =
                T("menuDelay");

            MenuDelayDescription.Text =
                T("menuDelayDescription");

            MouseHoverCheck.Content =
                T("mouseHover");

            MouseHoverDescription.Text =
                T("mouseHoverDescription");

            TweaksGameModeCheck.Content =
                T("tweaksGameMode");

            TweaksGameModeDescription.Text =
                T("tweaksGameModeDescription");

            TweaksGameDvrCheck.Content =
                T("tweaksGameDvr");

            TweaksGameDvrDescription.Text =
                T("tweaksGameDvrDescription");

            TweaksHighPerformanceCheck.Content =
                T("tweaksHighPerformance");

            TweaksHighPerformanceDescription.Text =
                T("tweaksHighPerformanceDescription");

            TweaksInfoTitle.Text =
                T("tweaksInfoTitle");

            TweaksInfoText.Text =
                T("tweaksInfoText");

            UpdateGameUI();
            UpdateSelectionCounter();

            if (detectedGamePid <= 0)
            {
                StatusText.Text = T("statusReady");
            }
        }

        private void LanguageComboBox_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            // WPF can fire SelectionChanged while InitializeComponent()
            // is still constructing the window.
            if (LanguageComboBox == null ||
                ReadyText == null)
            {
                return;
            }

            if (LanguageComboBox.SelectedIndex == 0)
            {
                currentLanguage = "English";
            }
            else if (LanguageComboBox.SelectedIndex == 1)
            {
                currentLanguage = "Magyar";
            }
            else
            {
                return;
            }

            ApplyLocalization();
        }

        // =========================================================
        // GAME DETECTION
        // =========================================================

        private void DetectGame()
        {
            detectedGamePid = -1;
            detectedGameName = "No game detected";
            detectedProcessName = "";

            StatusText.Text =
                T("statusDetecting");

            try
            {
                Process[] processes =
                    Process.GetProcesses();

                // Known games
                foreach (Process process in processes)
                {
                    try
                    {
                        if (process.Id ==
                            Process.GetCurrentProcess().Id)
                            continue;

                        string name =
                            process.ProcessName;

                        string gameName;

                        if (knownGames.TryGetValue(
                                name,
                                out gameName))
                        {
                            detectedGamePid =
                                process.Id;

                            detectedProcessName =
                                name;

                            detectedGameName =
                                gameName;

                            break;
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch
                        {
                        }
                    }

                    if (detectedGamePid > 0)
                        break;
                }

                // Minecraft
                if (detectedGamePid <= 0)
                {
                    Process[] javaProcesses =
                        Process.GetProcessesByName("javaw");

                    foreach (Process process in javaProcesses)
                    {
                        try
                        {
                            if (IsMinecraftJavaProcess(
                                    process.Id))
                            {
                                detectedGamePid =
                                    process.Id;

                                detectedProcessName =
                                    "javaw";

                                detectedGameName =
                                    "Minecraft";

                                break;
                            }
                        }
                        catch
                        {
                        }
                        finally
                        {
                            try
                            {
                                process.Dispose();
                            }
                            catch
                            {
                            }
                        }

                        if (detectedGamePid > 0)
                            break;
                    }
                }

                // Window-title heuristics
                if (detectedGamePid <= 0)
                {
                    foreach (Process process in processes)
                    {
                        try
                        {
                            if (process.Id ==
                                Process.GetCurrentProcess().Id)
                                continue;

                            if (process.MainWindowHandle ==
                                IntPtr.Zero)
                                continue;

                            string title =
                                process.MainWindowTitle ?? "";

                            string name =
                                process.ProcessName ?? "";

                            if (string.IsNullOrWhiteSpace(
                                    title))
                                continue;

                            if (IsIgnoredProcess(
                                    name,
                                    title))
                                continue;

                            if (LooksLikeGameWindow(
                                    name,
                                    title))
                            {
                                detectedGamePid =
                                    process.Id;

                                detectedProcessName =
                                    name;

                                detectedGameName =
                                    title;

                                break;
                            }
                        }
                        catch
                        {
                        }
                        finally
                        {
                            try
                            {
                                process.Dispose();
                            }
                            catch
                            {
                            }
                        }

                        if (detectedGamePid > 0)
                            break;
                    }
                }
            }
            catch
            {
            }

            UpdateGameUI();
        }

        // =========================================================
        // MINECRAFT DETECTION
        // =========================================================

        private bool IsMinecraftJavaProcess(int pid)
        {
            try
            {
                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT CommandLine " +
                        "FROM Win32_Process " +
                        "WHERE ProcessId = " + pid))
                {
                    foreach (ManagementObject obj
                             in searcher.Get())
                    {
                        string commandLine =
                            obj["CommandLine"] != null
                                ? obj["CommandLine"].ToString()
                                : "";

                        string lower =
                            commandLine.ToLowerInvariant();

                        if (lower.Contains("minecraft"))
                            return true;

                        if (lower.Contains("fabric-loader"))
                            return true;

                        if (lower.Contains(
                            "net.fabricmc.loader"))
                            return true;

                        if (lower.Contains("quilt-loader"))
                            return true;

                        if (lower.Contains("lwjgl"))
                            return true;
                    }
                }
            }
            catch
            {
            }

            try
            {
                using Process process =
                    Process.GetProcessById(pid);

                string title =
                    process.MainWindowTitle ?? "";

                if (title.ToLowerInvariant()
                        .Contains("minecraft"))
                    return true;
            }
            catch
            {
            }

            return false;
        }

        // =========================================================
        // GAME HEURISTICS
        // =========================================================

        private bool LooksLikeGameWindow(
            string processName,
            string windowTitle)
        {
            string title =
                windowTitle.ToLowerInvariant();

            string[] keywords =
            {
                "minecraft",
                "grand theft auto",
                "gta v",
                "red dead redemption",
                "world of tanks",
                "counter-strike",
                "valorant",
                "fortnite",
                "elden ring",
                "rocket league",
                "overwatch",
                "roblox",
                "terraria",
                "stardew valley",
                "hogwarts legacy",
                "cyberpunk",
                "the witcher",
                "beamng",
                "satisfactory",
                "fall guys"
            };

            foreach (string keyword in keywords)
            {
                if (title.Contains(keyword))
                    return true;
            }

            return false;
        }

        // =========================================================
        // IGNORED PROCESSES
        // =========================================================

        private bool IsIgnoredProcess(
            string processName,
            string windowTitle)
        {
            string p =
                processName.ToLowerInvariant();

            string t =
                windowTitle.ToLowerInvariant();

            string[] ignored =
            {
                "explorer",
                "dwm",
                "searchhost",
                "searchapp",
                "sihost",
                "taskmgr",
                "devenv",
                "powershell",
                "pwsh",
                "cmd",
                "conhost",
                "applicationframehost",
                "textinputhost",
                "runtimebroker",
                "ctfmon",
                "startmenuexperiencehost",
                "lockapp",
                "systemsettings",
                "msedge",
                "chrome",
                "firefox",
                "brave",
                "opera",
                "discord",
                "steam",
                "steamwebhelper",
                "epicgameslauncher",
                "battle.net",
                "riotclientservices",
                "riotclientux",
                "eadesktop",
                "ubisoftconnect"
            };

            foreach (string item in ignored)
            {
                if (p == item)
                    return true;
            }

            string[] ignoredTitles =
            {
                "settings",
                "task manager",
                "file explorer",
                "visual studio",
                "microsoft edge",
                "google chrome",
                "mozilla firefox",
                "discord",
                "steam",
                "epic games launcher"
            };

            foreach (string item in ignoredTitles)
            {
                if (t.Contains(item))
                    return true;
            }

            return false;
        }

        // =========================================================
        // MAIN GAME UI
        // =========================================================

        private void UpdateGameUI()
        {
            if (detectedGamePid > 0)
            {
                GameNameText.Text =
                    detectedGameName;

                GameStatusText.Text =
                    detectedProcessName +
                    ".exe • PID " +
                    detectedGamePid;

                StatusText.Text =
                    T("statusDetected");
            }
            else
            {
                GameNameText.Text =
                    T("noGame");

                GameStatusText.Text =
                    T("detectHint");

                StatusText.Text =
                    T("statusReady");
            }
        }

        private void DetectButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DetectGame();
        }

        // =========================================================
        // MAIN CHECKBOX COUNTER
        // =========================================================

        private void OptimizationChanged(
            object sender,
            RoutedEventArgs e)
        {
            UpdateSelectionCounter();
        }

        private void UpdateSelectionCounter()
        {
            if (SelectedCountText == null ||
                HighPerformanceCheck == null ||
                GameModeCheck == null ||
                GameDvrCheck == null ||
                GamePriorityCheck == null ||
                MemoryCleanupCheck == null)
            {
                return;
            }

            int selected = 0;

            const int total = 5;

            if (HighPerformanceCheck.IsChecked == true)
                selected++;

            if (GameModeCheck.IsChecked == true)
                selected++;

            if (GameDvrCheck.IsChecked == true)
                selected++;

            if (GamePriorityCheck.IsChecked == true)
                selected++;

            if (MemoryCleanupCheck.IsChecked == true)
                selected++;

            SelectedCountText.Text =
                T("selected") +
                ": " +
                selected +
                " / " +
                total;
        }

        // =========================================================
        // MAIN BOOST
        // =========================================================

        private void BoostButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            UpdateSelectionCounter();

            bool anythingSelected =
                HighPerformanceCheck.IsChecked == true ||
                GameModeCheck.IsChecked == true ||
                GameDvrCheck.IsChecked == true ||
                GamePriorityCheck.IsChecked == true ||
                MemoryCleanupCheck.IsChecked == true;

            if (!anythingSelected)
            {
                MessageBox.Show(
                    T("nothingSelected"),
                    "MázliBoost",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            BoostButton.IsEnabled = false;

            StatusText.Text =
                T("statusOptimizing");

            List<string> completed =
                new List<string>();

            try
            {
                if (HighPerformanceCheck.IsChecked == true)
                {
                    if (SetHighPerformance())
                        completed.Add(
                            T("highPerformance"));
                }

                if (GameModeCheck.IsChecked == true)
                {
                    if (EnableGameMode())
                        completed.Add(
                            T("gameMode"));
                }

                if (GameDvrCheck.IsChecked == true)
                {
                    if (DisableGameCapture())
                        completed.Add(
                            T("gameDvr"));
                }

                if (GamePriorityCheck.IsChecked == true &&
                    detectedGamePid > 0)
                {
                    if (SetGamePriority(
                            detectedGamePid))
                    {
                        completed.Add(
                            T("gamePriority"));
                    }
                }

                // Intentionally conservative.
                // We don't touch arbitrary processes here.

                if (MemoryCleanupCheck.IsChecked == true)
                {
                    completed.Add(
                        T("memoryCleanup"));
                }

                LoadSystemInfo();

                StatusText.Text =
                    T("statusComplete");

                ShowOptimizationResult(
                    completed);
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Error";

                MessageBox.Show(
                    ex.Message,
                    "MázliBoost",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                BoostButton.IsEnabled = true;
            }
        }

        // =========================================================
        // POWER PLAN
        // =========================================================

        private bool SetHighPerformance()
        {
            try
            {
                if (string.IsNullOrEmpty(
                        previousPowerPlanGuid))
                {
                    previousPowerPlanGuid =
                        GetActivePowerPlanGuid();
                }

                ProcessStartInfo psi =
                    new ProcessStartInfo();

                psi.FileName =
                    "powercfg.exe";

                psi.Arguments =
                    "/setactive SCHEME_MIN";

                psi.UseShellExecute =
                    true;

                psi.Verb =
                    "runas";

                psi.CreateNoWindow =
                    true;

                using (Process process =
                    Process.Start(psi))
                {
                    process.WaitForExit();

                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private string GetActivePowerPlanGuid()
        {
            try
            {
                ProcessStartInfo psi =
                    new ProcessStartInfo();

                psi.FileName =
                    "powercfg.exe";

                psi.Arguments =
                    "/getactivescheme";

                psi.UseShellExecute =
                    false;

                psi.RedirectStandardOutput =
                    true;

                psi.CreateNoWindow =
                    true;

                using (Process process =
                    Process.Start(psi))
                {
                    string output =
                        process.StandardOutput.ReadToEnd();

                    process.WaitForExit();

                    Match match =
                        Regex.Match(
                            output,
                            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

                    if (match.Success)
                        return match.Value;
                }
            }
            catch
            {
            }

            return null;
        }

        private bool RestorePreviousPowerPlan()
        {
            if (string.IsNullOrEmpty(
                    previousPowerPlanGuid))
                return true;

            try
            {
                ProcessStartInfo psi =
                    new ProcessStartInfo();

                psi.FileName =
                    "powercfg.exe";

                psi.Arguments =
                    "/setactive " +
                    previousPowerPlanGuid;

                psi.UseShellExecute =
                    true;

                psi.Verb =
                    "runas";

                psi.CreateNoWindow =
                    true;

                using (Process process =
                    Process.Start(psi))
                {
                    process.WaitForExit();

                    bool success =
                        process.ExitCode == 0;

                    if (success)
                        previousPowerPlanGuid = null;

                    return success;
                }
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // GAME MODE
        // =========================================================

        private bool EnableGameMode()
        {
            try
            {
                using (RegistryKey key =
                    Registry.CurrentUser.CreateSubKey(
                        @"Software\Microsoft\GameBar"))
                {
                    if (key == null)
                        return false;

                    key.SetValue(
                        "AutoGameModeEnabled",
                        1,
                        RegistryValueKind.DWord);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // GAME DVR
        // =========================================================

        private bool DisableGameCapture()
        {
            bool success = false;

            try
            {
                using (RegistryKey key =
                    Registry.CurrentUser.CreateSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\GameDVR"))
                {
                    if (key != null)
                    {
                        key.SetValue(
                            "AppCaptureEnabled",
                            0,
                            RegistryValueKind.DWord);

                        success = true;
                    }
                }
            }
            catch
            {
            }

            try
            {
                using (RegistryKey key =
                    Registry.CurrentUser.CreateSubKey(
                        @"System\GameConfigStore"))
                {
                    if (key != null)
                    {
                        key.SetValue(
                            "GameDVR_Enabled",
                            0,
                            RegistryValueKind.DWord);

                        success = true;
                    }
                }
            }
            catch
            {
            }

            return success;
        }

        // =========================================================
        // GAME PRIORITY
        // =========================================================

        private bool SetGamePriority(int pid)
        {
            try
            {
                using (Process process =
                    Process.GetProcessById(pid))
                {
                    process.PriorityClass =
                        ProcessPriorityClass.AboveNormal;

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // SYSTEM INFO
        // =========================================================

        private void LoadSystemInfo()
        {
            try
            {
                CpuText.Text =
                    GetCpuName();
            }
            catch
            {
                CpuText.Text =
                    "Unknown CPU";
            }

            try
            {
                RamText.Text =
                    GetRamGB().ToString("0.0") +
                    " GB";
            }
            catch
            {
                RamText.Text =
                    "Unknown";
            }

            try
            {
                PowerText.Text =
                    GetPowerPlan();
            }
            catch
            {
                PowerText.Text =
                    "Unknown";
            }
        }

        private string GetCpuName()
        {
            using (ManagementObjectSearcher searcher =
                new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_Processor"))
            {
                foreach (ManagementObject obj
                         in searcher.Get())
                {
                    string name =
                        obj["Name"] != null
                            ? obj["Name"].ToString().Trim()
                            : "";

                    if (!string.IsNullOrWhiteSpace(
                            name))
                    {
                        return name;
                    }
                }
            }

            return "Unknown CPU";
        }

        private double GetRamGB()
        {
            using (ManagementObjectSearcher searcher =
                new ManagementObjectSearcher(
                    "SELECT TotalVisibleMemorySize " +
                    "FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj
                         in searcher.Get())
                {
                    double kb =
                        Convert.ToDouble(
                            obj["TotalVisibleMemorySize"]);

                    return kb /
                           1024.0 /
                           1024.0;
                }
            }

            return 0;
        }

        private string GetPowerPlan()
        {
            try
            {
                ProcessStartInfo psi =
                    new ProcessStartInfo();

                psi.FileName =
                    "powercfg.exe";

                psi.Arguments =
                    "/getactivescheme";

                psi.UseShellExecute =
                    false;

                psi.RedirectStandardOutput =
                    true;

                psi.CreateNoWindow =
                    true;

                using (Process process =
                    Process.Start(psi))
                {
                    string output =
                        process.StandardOutput.ReadToEnd();

                    process.WaitForExit();

                    int start =
                        output.IndexOf("(");

                    int end =
                        output.IndexOf(
                            ")",
                            start + 1);

                    if (start >= 0 &&
                        end > start)
                    {
                        return output.Substring(
                            start + 1,
                            end - start - 1);
                    }
                }
            }
            catch
            {
            }

            return "Unknown";
        }

        // =========================================================
        // MORE TWEAKS NAVIGATION
        // =========================================================

        private void MoreTweaksButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HomePage.Visibility =
                Visibility.Collapsed;

            TweaksPage.Visibility =
                Visibility.Visible;

            StatusText.Text =
                T("statusReady");
        }

        private void BackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            TweaksPage.Visibility =
                Visibility.Collapsed;

            HomePage.Visibility =
                Visibility.Visible;

            StatusText.Text =
                T("statusReady");
        }

        // =========================================================
        // REGISTRY BACKUP HELPERS
        // =========================================================

        private void SaveRegistryValue(
            string keyPath,
            string valueName)
        {
            string backupKey =
                keyPath + "|" + valueName;

            if (registryBackups.ContainsKey(
                    backupKey))
                return;

            using (RegistryKey key =
                Registry.CurrentUser.OpenSubKey(
                    keyPath,
                    writable: false))
            {
                if (key == null)
                {
                    registryBackups[backupKey] =
                        new RegistryBackup
                        {
                            Existed = false
                        };

                    return;
                }

                object value =
                    key.GetValue(
                        valueName,
                        null,
                        RegistryValueOptions.DoNotExpandEnvironmentNames);

                if (value == null)
                {
                    registryBackups[backupKey] =
                        new RegistryBackup
                        {
                            Existed = false
                        };

                    return;
                }

                RegistryValueKind kind =
                    key.GetValueKind(
                        valueName);

                registryBackups[backupKey] =
                    new RegistryBackup
                    {
                        Existed = true,
                        Value = value,
                        Kind = kind
                    };
            }
        }

        private void RestoreRegistryValue(
            string keyPath,
            string valueName)
        {
            string backupKey =
                keyPath + "|" + valueName;

            RegistryBackup backup;

            if (!registryBackups.TryGetValue(
                    backupKey,
                    out backup))
            {
                return;
            }

            using (RegistryKey key =
                Registry.CurrentUser.CreateSubKey(
                    keyPath))
            {
                if (key == null)
                    return;

                if (backup.Existed)
                {
                    key.SetValue(
                        valueName,
                        backup.Value,
                        backup.Kind);
                }
                else
                {
                    key.DeleteValue(
                        valueName,
                        false);
                }
            }
        }

        // =========================================================
        // VISUAL EFFECTS
        // =========================================================

        private void VisualEffectsCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (VisualEffectsCheck == null)
                return;

            const string path =
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";

            const string name =
                "VisualFXSetting";

            try
            {
                if (VisualEffectsCheck.IsChecked == true)
                {
                    SaveRegistryValue(
                        path,
                        name);

                    using (RegistryKey key =
                        Registry.CurrentUser.CreateSubKey(
                            path))
                    {
                        key?.SetValue(
                            name,
                            3,
                            RegistryValueKind.DWord);
                    }
                }
                else
                {
                    RestoreRegistryValue(
                        path,
                        name);
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // TRANSPARENCY
        // =========================================================

        private void TransparencyCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (TransparencyCheck == null)
                return;

            const string path =
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            const string name =
                "EnableTransparency";

            try
            {
                if (TransparencyCheck.IsChecked == true)
                {
                    SaveRegistryValue(
                        path,
                        name);

                    using (RegistryKey key =
                        Registry.CurrentUser.CreateSubKey(
                            path))
                    {
                        key?.SetValue(
                            name,
                            0,
                            RegistryValueKind.DWord);
                    }
                }
                else
                {
                    RestoreRegistryValue(
                        path,
                        name);
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // MENU DELAY
        // =========================================================

        private void MenuDelayCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (MenuDelayCheck == null)
                return;

            const string path =
                @"Control Panel\Desktop";

            const string name =
                "MenuShowDelay";

            try
            {
                if (MenuDelayCheck.IsChecked == true)
                {
                    SaveRegistryValue(
                        path,
                        name);

                    using (RegistryKey key =
                        Registry.CurrentUser.CreateSubKey(
                            path))
                    {
                        key?.SetValue(
                            name,
                            "100",
                            RegistryValueKind.String);
                    }
                }
                else
                {
                    RestoreRegistryValue(
                        path,
                        name);
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // MOUSE HOVER
        // =========================================================

        private void MouseHoverCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (MouseHoverCheck == null)
                return;

            const string path =
                @"Control Panel\Mouse";

            const string name =
                "MouseHoverTime";

            try
            {
                if (MouseHoverCheck.IsChecked == true)
                {
                    SaveRegistryValue(
                        path,
                        name);

                    using (RegistryKey key =
                        Registry.CurrentUser.CreateSubKey(
                            path))
                    {
                        key?.SetValue(
                            name,
                            "100",
                            RegistryValueKind.String);
                    }
                }
                else
                {
                    RestoreRegistryValue(
                        path,
                        name);
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // TWEAK GAME MODE
        // =========================================================

        private void TweaksGameModeCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (TweaksGameModeCheck == null)
                return;

            if (TweaksGameModeCheck.IsChecked == true)
            {
                EnableGameMode();
            }
            else
            {
                // We intentionally do not force-disable the setting.
                // Future versions can store and restore the exact
                // previous Windows state.
            }
        }

        // =========================================================
        // TWEAK GAME DVR
        // =========================================================

        private void TweaksGameDvrCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (TweaksGameDvrCheck == null)
                return;

            if (TweaksGameDvrCheck.IsChecked == true)
            {
                DisableGameCapture();
            }
        }

        // =========================================================
        // TWEAK HIGH PERFORMANCE
        // =========================================================

        private void TweaksHighPerformanceCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (TweaksHighPerformanceCheck == null)
                return;

            if (TweaksHighPerformanceCheck.IsChecked == true)
            {
                SetHighPerformance();
            }
            else
            {
                RestorePreviousPowerPlan();
                LoadSystemInfo();
            }
        }

        // =========================================================
        // SELECTION STATUS
        // =========================================================

        private void ShowOptimizationResult(
            List<string> completed)
        {
            StringBuilder builder =
                new StringBuilder();

            builder.AppendLine(
                T("optimizationComplete"));

            builder.AppendLine();

            if (completed.Count == 0)
            {
                builder.AppendLine(
                    T("noOptimization"));
            }
            else
            {
                foreach (string item
                         in completed)
                {
                    builder.AppendLine(
                        "✓ " + item);
                }
            }

            builder.AppendLine();

            if (detectedGamePid > 0)
            {
                builder.AppendLine(
                    T("detected") +
                    ": " +
                    detectedGameName);
            }
            else
            {
                builder.AppendLine(
                    T("noDetected"));
            }

            MessageBox.Show(
                builder.ToString(),
                "MázliBoost",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================================
        // SPLASH
        // =========================================================

        private void StartSplashAnimation()
        {
            SplashOverlay.Visibility =
                Visibility.Visible;

            EngineSplashImage.Visibility =
                Visibility.Visible;

            StudioSplashImage.Visibility =
                Visibility.Hidden;

            EngineSplashImage.Opacity = 0;
            StudioSplashImage.Opacity = 0;

            // -----------------------------------------------------
            // ENGINE FADE IN
            // -----------------------------------------------------

            DoubleAnimation engineIn =
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration =
                        new Duration(
                            TimeSpan.FromMilliseconds(700))
                };

            engineIn.Completed +=
                delegate
                {
                    HoldEngine();
                };

            EngineSplashImage.BeginAnimation(
                UIElement.OpacityProperty,
                engineIn);
        }

        private void HoldEngine()
        {
            DispatcherTimer timer =
                new DispatcherTimer();

            timer.Interval =
                TimeSpan.FromMilliseconds(700);

            timer.Tick +=
                delegate
                {
                    timer.Stop();
                    FadeOutEngine();
                };

            timer.Start();
        }

        private void FadeOutEngine()
        {
            DoubleAnimation animation =
                new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration =
                        new Duration(
                            TimeSpan.FromMilliseconds(550))
                };

            animation.Completed +=
                delegate
                {
                    EngineSplashImage.Visibility =
                        Visibility.Hidden;

                    StudioSplashImage.Visibility =
                        Visibility.Visible;

                    FadeInStudio();
                };

            EngineSplashImage.BeginAnimation(
                UIElement.OpacityProperty,
                animation);
        }

        private void FadeInStudio()
        {
            DoubleAnimation animation =
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration =
                        new Duration(
                            TimeSpan.FromMilliseconds(700))
                };

            animation.Completed +=
                delegate
                {
                    HoldStudio();
                };

            StudioSplashImage.BeginAnimation(
                UIElement.OpacityProperty,
                animation);
        }

        private void HoldStudio()
        {
            DispatcherTimer timer =
                new DispatcherTimer();

            timer.Interval =
                TimeSpan.FromMilliseconds(700);

            timer.Tick +=
                delegate
                {
                    timer.Stop();
                    FadeOutStudio();
                };

            timer.Start();
        }

        private void FadeOutStudio()
        {
            DoubleAnimation animation =
                new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration =
                        new Duration(
                            TimeSpan.FromMilliseconds(550))
                };

            animation.Completed +=
                delegate
                {
                    SplashOverlay.Visibility =
                        Visibility.Collapsed;

                    FadeInMainContent();
                };

            StudioSplashImage.BeginAnimation(
                UIElement.OpacityProperty,
                animation);
        }

        private void FadeInMainContent()
        {
            DoubleAnimation animation =
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration =
                        new Duration(
                            TimeSpan.FromMilliseconds(450))
                };

            MainContent.BeginAnimation(
                UIElement.OpacityProperty,
                animation);
        }
    }
}
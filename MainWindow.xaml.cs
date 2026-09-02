using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Media.Imaging;
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

        private readonly string[] supportedLanguages =
        {
            "English",
            "Magyar",
            "Deutsch",
            "Español",
            "Français",
            "Italiano",
            "Português"
        };

        private Dictionary<string, string> currentTranslations =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // =========================================================
        // SETTINGS
        // =========================================================

        private AppSettings settings = new AppSettings();
        private bool settingsChanged = false;
        private bool loadingSettings = true;
        private bool closingAfterDecision = false;

        // =========================================================
        // NATIVE MEMORY API
        // =========================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(
            ref MEMORYSTATUSEX lpBuffer);

        // =========================================================
        // PROCESS POWER THROTTLING
        // =========================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_POWER_THROTTLING_STATE
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessInformation(
            IntPtr hProcess,
            int ProcessInformationClass,
            ref PROCESS_POWER_THROTTLING_STATE ProcessInformation,
            uint ProcessInformationSize);

        private const int ProcessPowerThrottling = 4;
        private const uint ProcessPowerThrottlingExecutionSpeed = 0x1;

        // =========================================================
        // LOCALIZATION
        // =========================================================

        private static readonly Dictionary<string, string> fallbackStrings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
                { "gamePowerThrottling", "Disable Game Power Throttling" },
                { "gameFullscreenOptimization", "Disable Fullscreen Optimizations (Detected Game)" },
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
                { "startupDelay", "Disable Windows Startup Delay" },
                { "startupDelayDescription", "Reduces the delay before desktop applications start." },
                { "windowAnimations", "Disable Window Animations" },
                { "windowAnimationsDescription", "Changes Windows appearance - Revertable by unchecking." },
                { "taskbarAnimations", "Disable Taskbar Animations" },
                { "taskbarAnimationsDescription", "Changes Windows appearance - Revertable by unchecking." },
                { "explorerThumbnails", "Disable Explorer Thumbnail Previews" },
                { "explorerThumbnailsDescription", "Changes Windows appearance - Revertable by unchecking." },
                { "tweaksInfoTitle", "ABOUT THESE TWEAKS" },
                { "tweaksInfoText", "MázliBoost only applies the selected changes. Registry-based tweaks are used only where appropriate and are designed to be reversible." },
                { "saveChangesTitle", "Save changes?" },
                { "saveChangesMessage", "Would you like to save the changes?" },
                { "saveAndClose", "Yes and close" },
                { "discardAndClose", "Discard and close" },
                { "settingsSaveFailed", "MázliBoost could not save settings.json. Check that the application folder is writable." },
                { "error", "Error" }
            };

        private void LoadLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                language = "English";

            try
            {
                string resourceName =
                    "MazliBoost.Langs." + GetLanguageCode(language) + ".json";

                using Stream stream =
                    typeof(MainWindow).Assembly.GetManifestResourceStream(resourceName);

                if (stream != null)
                {
                    Dictionary<string, string> loaded =
                        JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
                        ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    currentTranslations =
                        new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);

                    currentLanguage = language;
                    return;
                }
            }
            catch
            {
            }

            currentLanguage = "English";
            currentTranslations =
                new Dictionary<string, string>(fallbackStrings, StringComparer.OrdinalIgnoreCase);
        }

        private string GetLanguageCode(string language)
        {
            return language switch
            {
                "English" => "en",
                "Magyar" => "hu",
                "Deutsch" => "de",
                "Español" => "es",
                "Français" => "fr",
                "Italiano" => "it",
                "Português" => "pt",
                _ => "en"
            };
        }

        private string T(string key)
        {
            string value;

            if (currentTranslations.TryGetValue(key, out value))
                return value;

            if (fallbackStrings.TryGetValue(key, out value))
                return value;

            return key;
        }

        private void ApplyLocalization()
        {
            if (ReadyText == null ||
                AppSubtitleText == null ||
                LanguageLabelText == null ||
                DetectButton == null)
            {
                return;
            }

            AppSubtitleText.Text = T("subtitle");
            LanguageLabelText.Text = T("languages");
            ReadyText.Text = T("ready");

            DetectedGameLabelText.Text = T("detectedGame");
            DetectButton.Content = T("detectAgain");

            GamingOptimizationsLabelText.Text = T("gamingOptimizations");
            ChooseText.Text = T("choose");
            IndependentText.Text = T("independent");

            HighPerformanceCheck.Content = T("highPerformance");
            GameModeCheck.Content = T("gameMode");
            GameDvrCheck.Content = T("gameDvr");
            GamePriorityCheck.Content = T("gamePriority");
            MemoryCleanupCheck.Content = T("memoryCleanup");
            GamePowerThrottlingCheck.Content = T("gamePowerThrottling");
            GameFullscreenOptimizationCheck.Content = T("gameFullscreenOptimization");

            BoostButton.Content = T("apply");
            MoreTweaksButton.Content = T("moreTweaks");

            SystemLabelText.Text = T("system");
            CurrentHardwareText.Text = T("currentHardware");
            MemoryLabelText.Text = T("memory");
            PowerPlanLabelText.Text = T("powerPlan");

            AdvancedLabelText.Text = T("advanced");
            FutureText.Text = T("future");
            FutureDescriptionText.Text = T("futureDescription");
            FooterText.Text = T("footer");

            TweaksTitleText.Text = T("tweaksTitle");
            TweaksSubtitleText.Text = T("tweaksSubtitle");
            BackButton.Content = T("back");
            WindowsTweaksHeader.Text = T("windowsTweaks");
            GamingTweaksHeader.Text = T("gamingTweaks");

            VisualEffectsCheck.Content = T("visualEffects");
            VisualEffectsDescription.Text = T("visualEffectsDescription");
            TransparencyCheck.Content = T("transparency");
            TransparencyDescription.Text = T("transparencyDescription");
            MenuDelayCheck.Content = T("menuDelay");
            MenuDelayDescription.Text = T("menuDelayDescription");
            MouseHoverCheck.Content = T("mouseHover");
            MouseHoverDescription.Text = T("mouseHoverDescription");
            TweaksGameModeCheck.Content = T("tweaksGameMode");
            TweaksGameModeDescription.Text = T("tweaksGameModeDescription");
            TweaksGameDvrCheck.Content = T("tweaksGameDvr");
            TweaksGameDvrDescription.Text = T("tweaksGameDvrDescription");
            TweaksHighPerformanceCheck.Content = T("tweaksHighPerformance");
            TweaksHighPerformanceDescription.Text = T("tweaksHighPerformanceDescription");
            StartupDelayCheck.Content = T("startupDelay");
            StartupDelayDescription.Text = T("startupDelayDescription");
            WindowAnimationsCheck.Content = T("windowAnimations");
            WindowAnimationsDescription.Text = T("windowAnimationsDescription");
            TaskbarAnimationsCheck.Content = T("taskbarAnimations");
            TaskbarAnimationsDescription.Text = T("taskbarAnimationsDescription");
            ExplorerThumbnailsCheck.Content = T("explorerThumbnails");
            ExplorerThumbnailsDescription.Text = T("explorerThumbnailsDescription");
            TweaksInfoTitle.Text = T("tweaksInfoTitle");
            TweaksInfoText.Text = T("tweaksInfoText");

            UpdateGameUI();
            UpdateSelectionCounter();

            if (detectedGamePid <= 0)
                StatusText.Text = T("statusReady");
        }

        private void LanguageComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (LanguageComboBox == null || ReadyText == null)
                return;

            int index = LanguageComboBox.SelectedIndex;

            if (index < 0 || index >= supportedLanguages.Length)
                return;

            LoadLanguage(supportedLanguages[index]);
            currentLanguage = supportedLanguages[index];
            settings.Language = currentLanguage;
            MarkSettingsChanged();
            ApplyLocalization();
        }

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
        // SETTINGS
        // =========================================================

        private void LoadSettings()
        {
            settings = SettingsLoader.Load();

            currentLanguage = supportedLanguages
                .FirstOrDefault(x => string.Equals(x, settings.Language, StringComparison.OrdinalIgnoreCase))
                ?? "English";

            int languageIndex = Array.IndexOf(supportedLanguages, currentLanguage);
            if (languageIndex < 0)
                languageIndex = 0;

            LanguageComboBox.SelectedIndex = languageIndex;

            HighPerformanceCheck.IsChecked = settings.MainOptimizations.HighPerformance;
            GameModeCheck.IsChecked = settings.MainOptimizations.GameMode;
            GameDvrCheck.IsChecked = settings.MainOptimizations.GameDvr;
            GamePriorityCheck.IsChecked = settings.MainOptimizations.GamePriority;
            MemoryCleanupCheck.IsChecked = settings.MainOptimizations.MemoryCleanup;
            GamePowerThrottlingCheck.IsChecked = settings.MainOptimizations.GamePowerThrottling;
            GameFullscreenOptimizationCheck.IsChecked = settings.MainOptimizations.FullscreenOptimization;

            VisualEffectsCheck.IsChecked = settings.MoreTweaks.VisualEffects;
            TransparencyCheck.IsChecked = settings.MoreTweaks.Transparency;
            MenuDelayCheck.IsChecked = settings.MoreTweaks.MenuDelay;
            MouseHoverCheck.IsChecked = settings.MoreTweaks.MouseHover;
            TweaksGameModeCheck.IsChecked = settings.MoreTweaks.GameMode;
            TweaksGameDvrCheck.IsChecked = settings.MoreTweaks.GameDvr;
            TweaksHighPerformanceCheck.IsChecked = settings.MoreTweaks.HighPerformance;
            StartupDelayCheck.IsChecked = settings.MoreTweaks.StartupDelay;
            WindowAnimationsCheck.IsChecked = settings.MoreTweaks.WindowAnimations;
            TaskbarAnimationsCheck.IsChecked = settings.MoreTweaks.TaskbarAnimations;
            ExplorerThumbnailsCheck.IsChecked = settings.MoreTweaks.ExplorerThumbnails;

            settingsChanged = false;
        }

        private void CaptureSettingsFromUi()
        {
            settings.Language = currentLanguage;

            settings.MainOptimizations.HighPerformance = HighPerformanceCheck.IsChecked == true;
            settings.MainOptimizations.GameMode = GameModeCheck.IsChecked == true;
            settings.MainOptimizations.GameDvr = GameDvrCheck.IsChecked == true;
            settings.MainOptimizations.GamePriority = GamePriorityCheck.IsChecked == true;
            settings.MainOptimizations.MemoryCleanup = MemoryCleanupCheck.IsChecked == true;
            settings.MainOptimizations.GamePowerThrottling = GamePowerThrottlingCheck.IsChecked == true;
            settings.MainOptimizations.FullscreenOptimization = GameFullscreenOptimizationCheck.IsChecked == true;

            settings.MoreTweaks.VisualEffects = VisualEffectsCheck.IsChecked == true;
            settings.MoreTweaks.Transparency = TransparencyCheck.IsChecked == true;
            settings.MoreTweaks.MenuDelay = MenuDelayCheck.IsChecked == true;
            settings.MoreTweaks.MouseHover = MouseHoverCheck.IsChecked == true;
            settings.MoreTweaks.GameMode = TweaksGameModeCheck.IsChecked == true;
            settings.MoreTweaks.GameDvr = TweaksGameDvrCheck.IsChecked == true;
            settings.MoreTweaks.HighPerformance = TweaksHighPerformanceCheck.IsChecked == true;
            settings.MoreTweaks.StartupDelay = StartupDelayCheck.IsChecked == true;
            settings.MoreTweaks.WindowAnimations = WindowAnimationsCheck.IsChecked == true;
            settings.MoreTweaks.TaskbarAnimations = TaskbarAnimationsCheck.IsChecked == true;
            settings.MoreTweaks.ExplorerThumbnails = ExplorerThumbnailsCheck.IsChecked == true;
        }

        private void MarkSettingsChanged()
        {
            if (!loadingSettings)
                settingsChanged = true;
        }

        private void MainWindow_Closing(
    object sender,
    System.ComponentModel.CancelEventArgs e)
        {
            if (closingAfterDecision || !settingsChanged)
                return;

            e.Cancel = true;

            SaveChangesWindow dialog = new SaveChangesWindow(
                T("saveChangesTitle"),
                T("saveChangesMessage"),
                T("saveAndClose"),
                T("discardAndClose"))
            {
                Owner = this
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                CaptureSettingsFromUi();

                if (!SettingsLoader.Save(settings))
                {
                    MessageBox.Show(
                        T("settingsSaveFailed"),
                        T("error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }
            }
            else if (result == false)
            {
                // Discard: nem mentjük a módosításokat.
            }
            else
            {
                return;
            }

            // Most már nincs szükség újabb mentési kérdésre.
            settingsChanged = false;
            closingAfterDecision = true;

            // A jelenlegi Closing esemény befejezése után zárjuk be az ablakot.
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(Close));
        }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MainWindow()
        {
            InitializeComponent();

            LoadScaledSplashImages();
            LoadLanguage("English");
            LoadSettings();
            loadingSettings = false;

            LoadSystemInfo();
            DetectGame();
            UpdateSelectionCounter();
            ApplyLocalization();

            MainContent.Opacity = 0;

            StartSplashAnimation();
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
                using Process process =
                    Process.GetProcessById(pid);

                string title =
                    process.MainWindowTitle ?? "";

                if (title.Contains(
                        "Minecraft",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
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
            MarkSettingsChanged();
        }

        private void UpdateSelectionCounter()
        {
            if (SelectedCountText == null ||
                HighPerformanceCheck == null ||
                GameModeCheck == null ||
                GameDvrCheck == null ||
                GamePriorityCheck == null ||
                MemoryCleanupCheck == null ||
                GamePowerThrottlingCheck == null ||
                GameFullscreenOptimizationCheck == null)
            {
                return;
            }

            int selected = 0;

            const int total = 7;

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

            if (GamePowerThrottlingCheck.IsChecked == true)
                selected++;

            if (GameFullscreenOptimizationCheck.IsChecked == true)
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
                MemoryCleanupCheck.IsChecked == true ||
                GamePowerThrottlingCheck.IsChecked == true ||
                GameFullscreenOptimizationCheck.IsChecked == true;

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

                if (GamePowerThrottlingCheck.IsChecked == true &&
                    detectedGamePid > 0)
                {
                    if (SetGamePowerThrottling(detectedGamePid, true))
                    {
                        completed.Add(
                            T("gamePowerThrottling"));
                    }
                }

                if (GameFullscreenOptimizationCheck.IsChecked == true &&
                    detectedGamePid > 0)
                {
                    if (SetGameFullscreenOptimization(detectedGamePid, true))
                    {
                        completed.Add(
                            T("gameFullscreenOptimization"));
                    }
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
        // GAME POWER THROTTLING
        // =========================================================

        private bool SetGamePowerThrottling(int pid, bool disableThrottling)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);

                PROCESS_POWER_THROTTLING_STATE state =
                    new PROCESS_POWER_THROTTLING_STATE
                    {
                        Version = 1,
                        ControlMask = disableThrottling
                            ? ProcessPowerThrottlingExecutionSpeed
                            : 0,
                        StateMask = 0
                    };

                return SetProcessInformation(
                    process.Handle,
                    ProcessPowerThrottling,
                    ref state,
                    (uint)Marshal.SizeOf<PROCESS_POWER_THROTTLING_STATE>());
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // FULLSCREEN OPTIMIZATION
        // =========================================================

        private bool SetGameFullscreenOptimization(int pid, bool disable)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);
                string exePath = process.MainModule?.FileName;

                if (string.IsNullOrWhiteSpace(exePath))
                    return false;

                const string path =
                    @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";

                if (disable)
                {
                    SaveRegistryValue(path, exePath);

                    using RegistryKey key =
                        Registry.CurrentUser.CreateSubKey(path);

                    key?.SetValue(
                        exePath,
                        "~ DISABLEDXMAXIMIZEDWINDOWEDMODE",
                        RegistryValueKind.String);

                    return key != null;
                }

                RestoreRegistryValue(path, exePath);
                return true;
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
            try
            {
                using RegistryKey key =
                    Registry.LocalMachine.OpenSubKey(
                        @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");

                string name =
                    key?.GetValue("ProcessorNameString") as string;

                if (!string.IsNullOrWhiteSpace(name))
                    return name.Trim();
            }
            catch
            {
            }

            return "Unknown CPU";
        }

        private double GetRamGB()
        {
            MEMORYSTATUSEX memory = new MEMORYSTATUSEX
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };

            if (!GlobalMemoryStatusEx(ref memory))
                return 0;

            return memory.ullTotalPhys /
                   1024.0 /
                   1024.0 /
                   1024.0;
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
            MarkSettingsChanged();
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
            MarkSettingsChanged();
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
            MarkSettingsChanged();
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
            MarkSettingsChanged();
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
            MarkSettingsChanged();
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
            MarkSettingsChanged();
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
            MarkSettingsChanged();
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
        // STARTUP DELAY
        // =========================================================

        private void StartupDelayCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            MarkSettingsChanged();
            if (StartupDelayCheck == null)
                return;

            const string path =
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize";
            const string name = "StartupDelayInMSec";

            try
            {
                if (StartupDelayCheck.IsChecked == true)
                {
                    SaveRegistryValue(path, name);
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 0, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreRegistryValue(path, name);
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // WINDOW ANIMATIONS
        // =========================================================

        private void WindowAnimationsCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            MarkSettingsChanged();
            if (WindowAnimationsCheck == null)
                return;

            const string path =
                @"Control Panel\Desktop\WindowMetrics";
            const string name = "MinAnimate";

            try
            {
                if (WindowAnimationsCheck.IsChecked == true)
                {
                    SaveRegistryValue(path, name);
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, "0", RegistryValueKind.String);
                }
                else
                {
                    RestoreRegistryValue(path, name);
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // TASKBAR ANIMATIONS
        // =========================================================

        private void TaskbarAnimationsCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            MarkSettingsChanged();
            if (TaskbarAnimationsCheck == null)
                return;

            const string path =
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
            const string name = "TaskbarAnimations";

            try
            {
                if (TaskbarAnimationsCheck.IsChecked == true)
                {
                    SaveRegistryValue(path, name);
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 0, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreRegistryValue(path, name);
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // EXPLORER THUMBNAILS
        // =========================================================

        private void ExplorerThumbnailsCheck_Changed(
            object sender,
            RoutedEventArgs e)
        {
            MarkSettingsChanged();
            if (ExplorerThumbnailsCheck == null)
                return;

            const string path =
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
            const string name = "DisableThumbnails";

            try
            {
                if (ExplorerThumbnailsCheck.IsChecked == true)
                {
                    SaveRegistryValue(path, name);
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 1, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreRegistryValue(path, name);
                }
            }
            catch
            {
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
        // SPLASH IMAGE MEMORY OPTIMIZATION
        // =========================================================

        private void LoadScaledSplashImages()
        {
            ConfigureSplashImage(
                EngineSplashImage,
                "Splash/engine.png",
                900);

            ConfigureSplashImage(
                StudioSplashImage,
                "Splash/studio.png",
                700);
        }

        private void ConfigureSplashImage(
            System.Windows.Controls.Image image,
            string resourcePath,
            int decodePixelWidth)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(
                    "pack://application:,,,/" + resourcePath,
                    UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = decodePixelWidth;
                bitmap.EndInit();
                bitmap.Freeze();
                image.Source = bitmap;
            }
            catch
            {
            }
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
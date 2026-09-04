using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MazliBoost
{
    public partial class MainWindow : Window
    {
        // =========================================================
        // GAME DETECTION
        // =========================================================

        private int detectedGamePid = -1;
        private string detectedGameName = "No game detected";
        private string detectedProcessName = "";

        private readonly Dictionary<string, string> knownGames =
            new(StringComparer.OrdinalIgnoreCase)
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
            new(StringComparer.OrdinalIgnoreCase);

        // =========================================================
        // SETTINGS
        // =========================================================

        private AppSettings settings = new();
        private bool loadingSettings = true;
        private bool settingsChanged;
        private bool closingAfterDecision;

        // =========================================================
        // REGISTRY BACKUPS
        // =========================================================

        private sealed class RegistryBackup
        {
            public bool Existed { get; init; }
            public object? Value { get; init; }
            public RegistryValueKind Kind { get; init; }
        }

        private readonly Dictionary<string, RegistryBackup> currentUserBackups = new();
        private readonly Dictionary<string, RegistryBackup> localMachineBackups = new();
        private string? previousPowerPlanGuid;

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
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

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
            new(StringComparer.OrdinalIgnoreCase)
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
                { "hags", "Hardware-Accelerated GPU Scheduling" },
                { "hagsDescription", "Changes GPU scheduling behavior. Requires a restart." },
                { "globalPowerThrottling", "Disable Global Power Throttling" },
                { "globalPowerThrottlingDescription", "Disables Windows power throttling system-wide. Requires administrator rights." },
                { "startupDelay", "Disable Windows Startup Delay" },
                { "startupDelayDescription", "Reduces the delay before desktop applications start." },
                { "windowAnimations", "Disable Window Animations" },
                { "windowAnimationsDescription", "Changes Windows appearance - Revertable by unchecking." },
                { "taskbarAnimations", "Disable Taskbar Animations" },
                { "taskbarAnimationsDescription", "Changes Windows appearance - Revertable by unchecking." },
                { "explorerThumbnails", "Disable Explorer Thumbnail Previews" },
                { "explorerThumbnailsDescription", "Changes Windows appearance - Revertable by unchecking." },
                { "aeroPeek", "Disable Aero Peek" },
                { "aeroPeekDescription", "Changes Windows appearance - Revertable by unchecking." },
                { "cursorShadow", "Disable Cursor Shadow" },
                { "cursorShadowDescription", "Changes Windows appearance - Revertable by unchecking." },
                { "smoothScroll", "Disable Smooth-Scroll Effects" },
                { "smoothScrollDescription", "No reliable system-wide Windows switch is changed by this option." },
                { "tweaksInfoTitle", "ABOUT THESE TWEAKS" },
                { "tweaksInfoText", "MázliBoost only applies the selected changes. Registry-based tweaks are used only where appropriate and are designed to be reversible." },
                { "saveChangesTitle", "Save changes?" },
                { "saveChangesMessage", "Would you like to save the changes?" },
                { "saveAndClose", "Yes and close" },
                { "discardAndClose", "Discard and close" },
                { "settingsSaveFailed", "MázliBoost could not save settings.json. Check that the application folder is writable." },
                { "restartRequired", "Please restart Windows for this change to take effect." },
                { "adminRequired", "Administrator rights are required for this tweak." },
                { "error", "Error" }
            };

        private string GetLanguageCode(string language) => language switch
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

        private void LoadLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                language = "English";

            try
            {
                string resourceName =
                    $"MazliBoost.Langs.{GetLanguageCode(language)}.json";

                using Stream? stream =
                    typeof(MainWindow).Assembly.GetManifestResourceStream(resourceName);

                if (stream != null)
                {
                    Dictionary<string, string>? loaded =
                        JsonSerializer.Deserialize<Dictionary<string, string>>(stream);

                    currentTranslations =
                        new Dictionary<string, string>(
                            loaded ?? new Dictionary<string, string>(),
                            StringComparer.OrdinalIgnoreCase);

                    currentLanguage = language;
                    return;
                }
            }
            catch
            {
            }

            currentLanguage = "English";
            currentTranslations =
                new Dictionary<string, string>(
                    fallbackStrings,
                    StringComparer.OrdinalIgnoreCase);
        }

        private string T(string key)
        {
            if (currentTranslations.TryGetValue(key, out string? value))
                return value;

            if (fallbackStrings.TryGetValue(key, out value))
                return value;

            return key;
        }

        private void ApplyLocalization()
        {
            if (ReadyText == null || AppSubtitleText == null)
                return;

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
            HagsCheck.Content = T("hags");
            HagsDescription.Text = T("hagsDescription");
            GlobalPowerThrottlingCheck.Content = T("globalPowerThrottling");
            GlobalPowerThrottlingDescription.Text = T("globalPowerThrottlingDescription");
            StartupDelayCheck.Content = T("startupDelay");
            StartupDelayDescription.Text = T("startupDelayDescription");
            WindowAnimationsCheck.Content = T("windowAnimations");
            WindowAnimationsDescription.Text = T("windowAnimationsDescription");
            TaskbarAnimationsCheck.Content = T("taskbarAnimations");
            TaskbarAnimationsDescription.Text = T("taskbarAnimationsDescription");
            ExplorerThumbnailsCheck.Content = T("explorerThumbnails");
            ExplorerThumbnailsDescription.Text = T("explorerThumbnailsDescription");
            AeroPeekCheck.Content = T("aeroPeek");
            AeroPeekDescription.Text = T("aeroPeekDescription");
            CursorShadowCheck.Content = T("cursorShadow");
            CursorShadowDescription.Text = T("cursorShadowDescription");
            TweaksInfoTitle.Text = T("tweaksInfoTitle");
            TweaksInfoText.Text = T("tweaksInfoText");

            UpdateGameUI();
            UpdateSelectionCounter();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox == null)
                return;

            int index = LanguageComboBox.SelectedIndex;
            if (index < 0 || index >= supportedLanguages.Length)
                return;

            string language = supportedLanguages[index];
            LoadLanguage(language);
            currentLanguage = language;
            settings.Language = language;

            if (!loadingSettings)
                settingsChanged = true;

            ApplyLocalization();
        }

        // =========================================================
        // SETTINGS
        // =========================================================

        private void LoadSettings()
        {
            settings = SettingsLoader.Load();

            currentLanguage = supportedLanguages.FirstOrDefault(
                language => string.Equals(
                    language,
                    settings.Language,
                    StringComparison.OrdinalIgnoreCase)) ?? "English";

            int languageIndex = Array.IndexOf(supportedLanguages, currentLanguage);
            LanguageComboBox.SelectedIndex = languageIndex < 0 ? 0 : languageIndex;

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
            HagsCheck.IsChecked = settings.MoreTweaks.Hags;
            GlobalPowerThrottlingCheck.IsChecked = settings.MoreTweaks.GlobalPowerThrottling;
            StartupDelayCheck.IsChecked = settings.MoreTweaks.StartupDelay;
            WindowAnimationsCheck.IsChecked = settings.MoreTweaks.WindowAnimations;
            TaskbarAnimationsCheck.IsChecked = settings.MoreTweaks.TaskbarAnimations;
            ExplorerThumbnailsCheck.IsChecked = settings.MoreTweaks.ExplorerThumbnails;
            AeroPeekCheck.IsChecked = settings.MoreTweaks.AeroPeek;
            CursorShadowCheck.IsChecked = settings.MoreTweaks.CursorShadow;

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
            settings.MoreTweaks.Hags = HagsCheck.IsChecked == true;
            settings.MoreTweaks.GlobalPowerThrottling = GlobalPowerThrottlingCheck.IsChecked == true;
            settings.MoreTweaks.StartupDelay = StartupDelayCheck.IsChecked == true;
            settings.MoreTweaks.WindowAnimations = WindowAnimationsCheck.IsChecked == true;
            settings.MoreTweaks.TaskbarAnimations = TaskbarAnimationsCheck.IsChecked == true;
            settings.MoreTweaks.ExplorerThumbnails = ExplorerThumbnailsCheck.IsChecked == true;
            settings.MoreTweaks.AeroPeek = AeroPeekCheck.IsChecked == true;
            settings.MoreTweaks.CursorShadow = CursorShadowCheck.IsChecked == true;
        }

        private void MarkSettingsChanged()
        {
            if (!loadingSettings)
                settingsChanged = true;
        }

        // =========================================================
        // CONSTRUCTOR / CLOSE
        // =========================================================

        public MainWindow()
        {
            InitializeComponent();

            LoadScaledSplashImages();
            LoadLanguage("English");
            LoadSettings();

            loadingSettings = false;
            ApplyLocalization();
            LoadSystemInfo();
            DetectGame();
            UpdateSelectionCounter();

            MainContent.Opacity = 0;
            StartSplashAnimation();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closingAfterDecision || !settingsChanged)
                return;

            e.Cancel = true;

            SaveChangesWindow dialog = new(
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
            else if (result != false)
            {
                return;
            }

            settingsChanged = false;
            closingAfterDecision = true;

            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(Close));
        }

        // =========================================================
        // GAME DETECTION
        // =========================================================

        private void DetectGame()
        {
            detectedGamePid = -1;
            detectedGameName = "No game detected";
            detectedProcessName = "";
            StatusText.Text = T("statusDetecting");

            try
            {
                Process[] processes = Process.GetProcesses();

                // 1. Exact known games first.
                foreach (Process process in processes)
                {
                    try
                    {
                        if (knownGames.TryGetValue(
                                process.ProcessName,
                                out string? gameName))
                        {
                            detectedGamePid = process.Id;
                            detectedGameName = gameName;
                            detectedProcessName = process.ProcessName;
                            process.Dispose();
                            break;
                        }
                    }
                    catch
                    {
                    }
                }

                // 2. Minecraft Java special case.
                if (detectedGamePid <= 0)
                {
                    foreach (Process process in processes)
                    {
                        try
                        {
                            if (!process.ProcessName.Equals(
                                    "javaw",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (IsMinecraftJavaProcess(process.Id))
                            {
                                detectedGamePid = process.Id;
                                detectedGameName = "Minecraft";
                                detectedProcessName = process.ProcessName;
                                process.Dispose();
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                // 3. Window-title heuristics as a fallback.
                if (detectedGamePid <= 0)
                {
                    foreach (Process process in processes)
                    {
                        try
                        {
                            string title = process.MainWindowTitle ?? "";
                            if (string.IsNullOrWhiteSpace(title) ||
                                IsIgnoredProcess(process.ProcessName, title) ||
                                !LooksLikeGameWindow(process.ProcessName, title))
                            {
                                continue;
                            }

                            detectedGamePid = process.Id;
                            detectedGameName = title.Trim();
                            detectedProcessName = process.ProcessName;
                            process.Dispose();
                            break;
                        }
                        catch
                        {
                        }
                    }
                }

                foreach (Process process in processes)
                {
                    try { process.Dispose(); } catch { }
                }
            }
            catch
            {
            }

            UpdateGameUI();
        }

        private bool IsMinecraftJavaProcess(int pid)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);
                string title = process.MainWindowTitle ?? "";
                return title.Contains("Minecraft", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool LooksLikeGameWindow(string processName, string windowTitle)
        {
            string title = windowTitle.ToLowerInvariant();
            string[] keywords =
            {
                "minecraft", "grand theft auto", "gta v", "red dead redemption",
                "world of tanks", "counter-strike", "valorant", "fortnite",
                "elden ring", "rocket league", "overwatch", "roblox", "terraria",
                "stardew valley", "hogwarts legacy", "cyberpunk", "the witcher",
                "beamng", "satisfactory", "fall guys"
            };

            return keywords.Any(title.Contains);
        }

        private bool IsIgnoredProcess(string processName, string windowTitle)
        {
            string p = processName.ToLowerInvariant();
            string t = windowTitle.ToLowerInvariant();

            string[] ignoredProcesses =
            {
                "explorer", "dwm", "searchhost", "searchapp", "sihost", "taskmgr",
                "devenv", "powershell", "pwsh", "cmd", "conhost", "applicationframehost",
                "textinputhost", "runtimebroker", "ctfmon", "startmenuexperiencehost",
                "lockapp", "systemsettings", "msedge", "chrome", "firefox", "brave",
                "opera", "discord", "steam", "steamwebhelper", "epicgameslauncher",
                "battle.net", "riotclientservices", "riotclientux", "eadesktop", "ubisoftconnect"
            };

            if (ignoredProcesses.Contains(p, StringComparer.OrdinalIgnoreCase))
                return true;

            string[] ignoredTitles =
            {
                "settings", "task manager", "file explorer", "visual studio",
                "microsoft edge", "google chrome", "mozilla firefox", "discord",
                "steam", "epic games launcher"
            };

            return ignoredTitles.Any(t.Contains);
        }

        private void UpdateGameUI()
        {
            if (detectedGamePid > 0)
            {
                GameNameText.Text = detectedGameName;
                GameStatusText.Text =
                    $"{detectedProcessName}.exe • PID {detectedGamePid}";
                StatusText.Text = T("statusDetected");
            }
            else
            {
                GameNameText.Text = T("noGame");
                GameStatusText.Text = T("detectHint");
                StatusText.Text = T("statusReady");
            }
        }

        private void DetectButton_Click(object sender, RoutedEventArgs e) => DetectGame();

        // =========================================================
        // MAIN OPTIMIZATIONS
        // =========================================================

        private void OptimizationChanged(object sender, RoutedEventArgs e)
        {
            UpdateSelectionCounter();
            MarkSettingsChanged();
        }

        private void UpdateSelectionCounter()
        {
            if (SelectedCountText == null)
                return;

            CheckBox[] checks =
            {
                HighPerformanceCheck,
                GameModeCheck,
                GameDvrCheck,
                GamePriorityCheck,
                MemoryCleanupCheck,
                GamePowerThrottlingCheck,
                GameFullscreenOptimizationCheck
            };

            int selected = checks.Count(check => check.IsChecked == true);
            SelectedCountText.Text = $"{T("selected")}: {selected} / {checks.Length}";
        }

        private void BoostButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectionCounter();

            bool anythingSelected =
                new[]
                {
                    HighPerformanceCheck,
                    GameModeCheck,
                    GameDvrCheck,
                    GamePriorityCheck,
                    MemoryCleanupCheck,
                    GamePowerThrottlingCheck,
                    GameFullscreenOptimizationCheck
                }
                .Any(check => check.IsChecked == true);

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
            StatusText.Text = T("statusOptimizing");

            List<string> completed = new();

            try
            {
                if (HighPerformanceCheck.IsChecked == true && SetHighPerformance())
                    completed.Add(T("highPerformance"));

                if (GameModeCheck.IsChecked == true && EnableGameMode())
                    completed.Add(T("gameMode"));

                if (GameDvrCheck.IsChecked == true && DisableGameCapture())
                    completed.Add(T("gameDvr"));

                if (GamePriorityCheck.IsChecked == true && detectedGamePid > 0 && SetGamePriority(detectedGamePid))
                    completed.Add(T("gamePriority"));

                if (MemoryCleanupCheck.IsChecked == true)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    completed.Add(T("memoryCleanup"));
                }

                if (GamePowerThrottlingCheck.IsChecked == true && detectedGamePid > 0 && SetGamePowerThrottling(detectedGamePid, true))
                    completed.Add(T("gamePowerThrottling"));

                if (GameFullscreenOptimizationCheck.IsChecked == true && detectedGamePid > 0 && SetGameFullscreenOptimization(detectedGamePid, true))
                    completed.Add(T("gameFullscreenOptimization"));

                LoadSystemInfo();
                StatusText.Text = T("statusComplete");
                ShowOptimizationResult(completed);
            }
            catch (Exception ex)
            {
                StatusText.Text = T("error");
                MessageBox.Show(ex.Message, "MázliBoost", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                if (string.IsNullOrWhiteSpace(previousPowerPlanGuid))
                    previousPowerPlanGuid = GetActivePowerPlanGuid();

                return RunPowerCfg("/setactive SCHEME_MIN");
            }
            catch
            {
                return false;
            }
        }

        private bool RestorePreviousPowerPlan()
        {
            if (string.IsNullOrWhiteSpace(previousPowerPlanGuid))
                return true;

            bool success = RunPowerCfg($"/setactive {previousPowerPlanGuid}");
            if (success)
                previousPowerPlanGuid = null;

            return success;
        }

        private bool RunPowerCfg(string arguments)
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "powercfg.exe",
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(psi);
                if (process == null)
                    return false;

                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private string? GetActivePowerPlanGuid()
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "powercfg.exe",
                    Arguments = "/getactivescheme",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(psi);
                if (process == null)
                    return null;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Match match = Regex.Match(
                    output,
                    @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

                return match.Success ? match.Value : null;
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // GAME MODE / GAME DVR
        // =========================================================

        private bool EnableGameMode()
        {
            try
            {
                using RegistryKey? key =
                    Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar");

                if (key == null)
                    return false;

                key.SetValue("AutoGameModeEnabled", 1, RegistryValueKind.DWord);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool DisableGameCapture()
        {
            bool success = false;

            try
            {
                using RegistryKey? key =
                    Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\GameDVR");
                if (key != null)
                {
                    key.SetValue("AppCaptureEnabled", 0, RegistryValueKind.DWord);
                    success = true;
                }
            }
            catch { }

            try
            {
                using RegistryKey? key =
                    Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore");
                if (key != null)
                {
                    key.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
                    success = true;
                }
            }
            catch { }

            return success;
        }

        private bool SetGamePriority(int pid)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);
                process.PriorityClass = ProcessPriorityClass.AboveNormal;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool SetGamePowerThrottling(int pid, bool disableThrottling)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);
                PROCESS_POWER_THROTTLING_STATE state = new()
                {
                    Version = 1,
                    ControlMask = disableThrottling ? ProcessPowerThrottlingExecutionSpeed : 0,
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

        private bool SetGameFullscreenOptimization(int pid, bool disable)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);
                string? exePath = process.MainModule?.FileName;

                if (string.IsNullOrWhiteSpace(exePath))
                    return false;

                const string path =
                    @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";

                if (disable)
                {
                    SaveCurrentUserRegistryValue(path, exePath);

                    using RegistryKey? key =
                        Registry.CurrentUser.CreateSubKey(path);

                    if (key == null)
                        return false;

                    key.SetValue(
                        exePath,
                        "~ DISABLEDXMAXIMIZEDWINDOWEDMODE",
                        RegistryValueKind.String);

                    return true;
                }

                RestoreCurrentUserRegistryValue(path, exePath);
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
            try { CpuText.Text = GetCpuName(); } catch { CpuText.Text = "Unknown CPU"; }
            try { RamText.Text = $"{GetRamGB():0.0} GB"; } catch { RamText.Text = "Unknown"; }
            try { PowerText.Text = GetPowerPlan(); } catch { PowerText.Text = "Unknown"; }
        }

        private string GetCpuName()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                string? name = key?.GetValue("ProcessorNameString") as string;
                if (!string.IsNullOrWhiteSpace(name))
                    return name.Trim();
            }
            catch { }

            return "Unknown CPU";
        }

        private double GetRamGB()
        {
            MEMORYSTATUSEX memory = new()
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };

            if (!GlobalMemoryStatusEx(ref memory))
                return 0;

            return memory.ullTotalPhys / 1024d / 1024d / 1024d;
        }

        private string GetPowerPlan()
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "powercfg.exe",
                    Arguments = "/getactivescheme",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(psi);
                if (process == null)
                    return "Unknown";

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                int start = output.IndexOf('(');
                int end = output.IndexOf(')', start + 1);

                if (start >= 0 && end > start)
                    return output.Substring(start + 1, end - start - 1);
            }
            catch { }

            return "Unknown";
        }

        // =========================================================
        // MORE TWEAKS NAVIGATION
        // =========================================================

        private void MoreTweaksButton_Click(object sender, RoutedEventArgs e)
        {
            HomePage.Visibility = Visibility.Collapsed;
            TweaksPage.Visibility = Visibility.Visible;
            StatusText.Text = T("statusReady");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            TweaksPage.Visibility = Visibility.Collapsed;
            HomePage.Visibility = Visibility.Visible;
            StatusText.Text = T("statusReady");
        }

        // =========================================================
        // REGISTRY BACKUP HELPERS
        // =========================================================

        private static string RegistryBackupKey(string keyPath, string valueName) =>
            keyPath + "|" + valueName;

        private void SaveCurrentUserRegistryValue(string keyPath, string valueName)
        {
            string backupKey = RegistryBackupKey(keyPath, valueName);
            if (currentUserBackups.ContainsKey(backupKey))
                return;

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath, false);
            if (key == null)
            {
                currentUserBackups[backupKey] = new RegistryBackup { Existed = false };
                return;
            }

            object? value = key.GetValue(
                valueName,
                null,
                RegistryValueOptions.DoNotExpandEnvironmentNames);

            if (value == null)
            {
                currentUserBackups[backupKey] = new RegistryBackup { Existed = false };
                return;
            }

            currentUserBackups[backupKey] = new RegistryBackup
            {
                Existed = true,
                Value = value,
                Kind = key.GetValueKind(valueName)
            };
        }

        private void RestoreCurrentUserRegistryValue(string keyPath, string valueName)
        {
            string backupKey = RegistryBackupKey(keyPath, valueName);
            if (!currentUserBackups.TryGetValue(backupKey, out RegistryBackup? backup))
                return;

            using RegistryKey? key = Registry.CurrentUser.CreateSubKey(keyPath);
            if (key == null)
                return;

            if (backup.Existed)
            {
                key.SetValue(valueName, backup.Value!, backup.Kind);
            }
            else
            {
                key.DeleteValue(valueName, false);
            }
        }

        private void SaveLocalMachineRegistryValue(string keyPath, string valueName)
        {
            string backupKey = RegistryBackupKey(keyPath, valueName);
            if (localMachineBackups.ContainsKey(backupKey))
                return;

            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, false);
            if (key == null)
            {
                localMachineBackups[backupKey] = new RegistryBackup { Existed = false };
                return;
            }

            object? value = key.GetValue(
                valueName,
                null,
                RegistryValueOptions.DoNotExpandEnvironmentNames);

            if (value == null)
            {
                localMachineBackups[backupKey] = new RegistryBackup { Existed = false };
                return;
            }

            localMachineBackups[backupKey] = new RegistryBackup
            {
                Existed = true,
                Value = value,
                Kind = key.GetValueKind(valueName)
            };
        }

        private void RestoreLocalMachineRegistryValue(string keyPath, string valueName)
        {
            string backupKey = RegistryBackupKey(keyPath, valueName);
            if (!localMachineBackups.TryGetValue(backupKey, out RegistryBackup? backup))
                return;

            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(keyPath);
            if (key == null)
                return;

            if (backup.Existed)
                key.SetValue(valueName, backup.Value!, backup.Kind);
            else
                key.DeleteValue(valueName, false);
        }

        // =========================================================
        // CURRENT USER TWEAKS
        // =========================================================

        private void VisualEffectsCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
            const string name = "VisualFXSetting";

            try
            {
                if (VisualEffectsCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 3, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void TransparencyCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string name = "EnableTransparency";

            try
            {
                if (TransparencyCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 0, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void MenuDelayCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Control Panel\Desktop";
            const string name = "MenuShowDelay";

            try
            {
                if (MenuDelayCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, "100", RegistryValueKind.String);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void MouseHoverCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Control Panel\Mouse";
            const string name = "MouseHoverTime";

            try
            {
                if (MouseHoverCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, "100", RegistryValueKind.String);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void TweaksGameModeCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            if (TweaksGameModeCheck.IsChecked == true)
                EnableGameMode();
        }

        private void TweaksGameDvrCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            if (TweaksGameDvrCheck.IsChecked == true)
                DisableGameCapture();
        }

        private void TweaksHighPerformanceCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();

            if (TweaksHighPerformanceCheck.IsChecked == true)
                SetHighPerformance();
            else
            {
                RestorePreviousPowerPlan();
                LoadSystemInfo();
            }
        }

        private void HagsCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();

            const string path = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
            const string name = "HwSchMode";

            try
            {
                if (HagsCheck.IsChecked == true)
                {
                    SaveLocalMachineRegistryValue(path, name);
                    using RegistryKey? key = Registry.LocalMachine.CreateSubKey(path);
                    if (key == null) throw new InvalidOperationException();
                    key.SetValue(name, 2, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreLocalMachineRegistryValue(path, name);
                }

                MessageBox.Show(T("restartRequired"), "MázliBoost", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show(T("adminRequired"), T("error"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GlobalPowerThrottlingCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();

            const string path = @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling";
            const string name = "PowerThrottlingOff";

            try
            {
                if (GlobalPowerThrottlingCheck.IsChecked == true)
                {
                    SaveLocalMachineRegistryValue(path, name);
                    using RegistryKey? key = Registry.LocalMachine.CreateSubKey(path);
                    if (key == null) throw new InvalidOperationException();
                    key.SetValue(name, 1, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreLocalMachineRegistryValue(path, name);
                }
            }
            catch
            {
                MessageBox.Show(T("adminRequired"), T("error"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartupDelayCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize";
            const string name = "StartupDelayInMSec";

            try
            {
                if (StartupDelayCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 0, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void WindowAnimationsCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Control Panel\Desktop\WindowMetrics";
            const string name = "MinAnimate";

            try
            {
                if (WindowAnimationsCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, "0", RegistryValueKind.String);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void TaskbarAnimationsCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
            const string name = "TaskbarAnimations";

            try
            {
                if (TaskbarAnimationsCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 0, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void ExplorerThumbnailsCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
            const string name = "DisableThumbnails";

            try
            {
                if (ExplorerThumbnailsCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 1, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void AeroPeekCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Software\Microsoft\Windows\DWM";
            const string name = "EnableAeroPeek";

            try
            {
                if (AeroPeekCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 0, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void CursorShadowCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingSettings) return;
            MarkSettingsChanged();
            const string path = @"Control Panel\Cursors";
            const string name = "CursorShadow";

            try
            {
                if (CursorShadowCheck.IsChecked == true)
                {
                    SaveCurrentUserRegistryValue(path, name);
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(path);
                    key?.SetValue(name, 0, RegistryValueKind.DWord);
                }
                else
                {
                    RestoreCurrentUserRegistryValue(path, name);
                }
            }
            catch { }
        }

        private void SmoothScrollCheck_Changed(object sender, RoutedEventArgs e)
        {
            // There is no reliable system-wide Windows registry switch for smooth scrolling.
            // We keep the setting for UI/settings consistency, but intentionally do not fake a tweak.
            if (loadingSettings) return;
            MarkSettingsChanged();
        }

        // =========================================================
        // RESULT DIALOG
        // =========================================================

        private void ShowOptimizationResult(List<string> completed)
        {
            StringBuilder builder = new();
            builder.AppendLine(T("optimizationComplete"));
            builder.AppendLine();

            if (completed.Count == 0)
            {
                builder.AppendLine(T("noOptimization"));
            }
            else
            {
                foreach (string item in completed)
                    builder.AppendLine("✓ " + item);
            }

            builder.AppendLine();

            builder.AppendLine(
                detectedGamePid > 0
                    ? T("detected") + ": " + detectedGameName
                    : T("noDetected"));

            MessageBox.Show(
                builder.ToString(),
                "MázliBoost",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================================
        // SPLASH
        // =========================================================

        private void LoadScaledSplashImages()
        {
            ConfigureSplashImage(EngineSplashImage, "Splash/engine.png", 900);
            ConfigureSplashImage(StudioSplashImage, "Splash/studio.png", 700);
        }

        private void ConfigureSplashImage(Image image, string resourcePath, int decodePixelWidth)
        {
            try
            {
                BitmapImage bitmap = new();
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

        private void StartSplashAnimation()
        {
            SplashOverlay.Visibility = Visibility.Visible;
            EngineSplashImage.Visibility = Visibility.Visible;
            StudioSplashImage.Visibility = Visibility.Hidden;
            EngineSplashImage.Opacity = 0;
            StudioSplashImage.Opacity = 0;

            DoubleAnimation engineIn = new(0, 1, TimeSpan.FromMilliseconds(700));
            engineIn.Completed += (_, _) => HoldEngine();
            EngineSplashImage.BeginAnimation(UIElement.OpacityProperty, engineIn);
        }

        private void HoldEngine()
        {
            DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(700) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                FadeOutEngine();
            };
            timer.Start();
        }

        private void FadeOutEngine()
        {
            DoubleAnimation animation = new(1, 0, TimeSpan.FromMilliseconds(550));
            animation.Completed += (_, _) =>
            {
                EngineSplashImage.Visibility = Visibility.Hidden;
                StudioSplashImage.Visibility = Visibility.Visible;
                FadeInStudio();
            };
            EngineSplashImage.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private void FadeInStudio()
        {
            DoubleAnimation animation = new(0, 1, TimeSpan.FromMilliseconds(700));
            animation.Completed += (_, _) => HoldStudio();
            StudioSplashImage.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private void HoldStudio()
        {
            DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(700) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                FadeOutStudio();
            };
            timer.Start();
        }

        private void FadeOutStudio()
        {
            DoubleAnimation animation = new(1, 0, TimeSpan.FromMilliseconds(550));
            animation.Completed += (_, _) =>
            {
                SplashOverlay.Visibility = Visibility.Collapsed;
                FadeInMainContent();
            };
            StudioSplashImage.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private void FadeInMainContent()
        {
            DoubleAnimation animation = new(0, 1, TimeSpan.FromMilliseconds(450));
            MainContent.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }
}

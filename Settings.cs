using System;
using System.IO;
using System.Text.Json;

namespace MazliBoost
{
    public sealed class AppSettings
    {
        public string Language { get; set; } = "English";
        public MainOptimizationSettings MainOptimizations { get; set; } = new();
        public MoreTweakSettings MoreTweaks { get; set; } = new();
    }

    public sealed class MainOptimizationSettings
    {
        public bool HighPerformance { get; set; } = true;
        public bool GameMode { get; set; } = true;
        public bool GameDvr { get; set; } = true;
        public bool GamePriority { get; set; } = true;
        public bool MemoryCleanup { get; set; } = true;
        public bool GamePowerThrottling { get; set; } = true;
        public bool FullscreenOptimization { get; set; } = false;
    }

    public sealed class MoreTweakSettings
    {
        public bool VisualEffects { get; set; }
        public bool Transparency { get; set; }
        public bool MenuDelay { get; set; }
        public bool MouseHover { get; set; }
        public bool GameMode { get; set; }
        public bool GameDvr { get; set; }
        public bool HighPerformance { get; set; }
        public bool Hags { get; set; }
        public bool GlobalPowerThrottling { get; set; }
        public bool StartupDelay { get; set; }
        public bool WindowAnimations { get; set; }
        public bool TaskbarAnimations { get; set; }
        public bool ExplorerThumbnails { get; set; }
        public bool AeroPeek { get; set; }
        public bool CursorShadow { get; set; }
        public bool SmoothScroll { get; set; }
    }

    public static class SettingsLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string SettingsPath =>
            Path.Combine(AppContext.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                string json = File.ReadAllText(SettingsPath);
                AppSettings? loaded =
                    JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                if (loaded == null)
                    return new AppSettings();

                loaded.MainOptimizations ??= new MainOptimizationSettings();
                loaded.MoreTweaks ??= new MoreTweakSettings();

                if (string.IsNullOrWhiteSpace(loaded.Language))
                    loaded.Language = "English";

                return loaded;
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static bool Save(AppSettings settings)
        {
            string tempPath = SettingsPath + ".tmp";

            try
            {
                string directory = AppContext.BaseDirectory;
                Directory.CreateDirectory(directory);

                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(tempPath, json);

                if (File.Exists(SettingsPath))
                {
                    File.Replace(tempPath, SettingsPath, null);
                }
                else
                {
                    File.Move(tempPath, SettingsPath);
                }

                return true;
            }
            catch
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                }

                return false;
            }
        }
    }
}

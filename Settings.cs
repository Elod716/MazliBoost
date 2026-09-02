using System;
using System.IO;
using System.Text.Json;

namespace MazliBoost
{
    public sealed class AppSettings
    {
        public string Language { get; set; } = "English";
        public MainOptimizationSettings MainOptimizations { get; set; } = new MainOptimizationSettings();
        public MoreTweakSettings MoreTweaks { get; set; } = new MoreTweakSettings();
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
        public bool VisualEffects { get; set; } = false;
        public bool Transparency { get; set; } = false;
        public bool MenuDelay { get; set; } = false;
        public bool MouseHover { get; set; } = false;
        public bool GameMode { get; set; } = false;
        public bool GameDvr { get; set; } = false;
        public bool HighPerformance { get; set; } = false;
        public bool StartupDelay { get; set; } = false;
        public bool WindowAnimations { get; set; } = false;
        public bool TaskbarAnimations { get; set; } = false;
        public bool ExplorerThumbnails { get; set; } = false;
    }

    public static class SettingsLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
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
                AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

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
            try
            {
                string directory = AppContext.BaseDirectory;
                Directory.CreateDirectory(directory);

                string tempPath = SettingsPath + ".tmp";
                string json = JsonSerializer.Serialize(settings, JsonOptions);

                File.WriteAllText(tempPath, json);

                if (File.Exists(SettingsPath))
                    File.Replace(tempPath, SettingsPath, null);
                else
                    File.Move(tempPath, SettingsPath);

                return true;
            }
            catch
            {
                try
                {
                    string tempPath = SettingsPath + ".tmp";
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

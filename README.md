# 🚀 MázliBoost

**MázliBoost** is a lightweight Windows gaming optimization utility designed to reduce unnecessary background load and prepare your PC for gaming.

The goal is simple:

> **Less background load. Better consistency. No snake oil.**

MázliBoost is currently in **V0.1** development. It is intentionally small and straightforward, with more advanced optimizations planned for future releases.

---

## ✨ Features

### 🎮 Automatic Game Detection
MázliBoost can automatically detect running games without requiring the user to know or enter an `.exe` filename.

It currently supports:

- Known game process detection
- Minecraft / Java process detection
- Fabric / LWJGL-based Minecraft detection
- Window-title based game detection
- Manual **Detect Again** refresh

### 🚀 Gaming Boost

The current Boost operation can apply several lightweight optimizations:

- ⚡ Switch to the Windows **High Performance** power plan
- 🎮 Enable Windows Game Mode
- 🎥 Disable Game DVR / background game capture
- 📈 Set the detected game's process priority to **Above Normal**
- 🧹 Perform a lightweight background working-set cleanup

> MázliBoost does **not** promise a fixed FPS increase. Results depend on the game, hardware, drivers, background applications, and current Windows configuration.

### 🖥️ System Information

The current interface displays basic system information:

- CPU
- Installed RAM
- Active Windows power plan

---

## 🛡️ Safety Philosophy

MázliBoost is designed to avoid aggressive "FPS boost" tricks.

The current version does **not** automatically:

- Delete Windows system files
- Disable critical Windows services
- Modify GPU clocks directly
- Disable Windows Defender
- Disable Memory Integrity
- Apply undocumented registry tweaks
- Kill random system processes

The idea is to make useful changes while keeping the system stable.

---

## 🧪 Current Version

**Version:** `V0.1`

### Current status

| Feature | Status |
|---|:---:|
| Automatic game detection | ✅ |
| Minecraft detection | ✅ |
| Game process optimization | ✅ |
| High Performance power plan | ✅ |
| Windows Game Mode | ✅ |
| Game DVR optimization | ✅ |
| Lightweight memory cleanup | ✅ |
| Basic system information | ✅ |
| Modern WPF interface | ✅ |
| Advanced optimization menu | 🔜 |
| GPU tuning | 🔜 |
| Detailed hardware monitoring | 🔜 |
| Before/After performance measurements | 🔜 |

---

## 💻 Requirements

### Operating System

- Windows 10
- Windows 11

### Development

The project is currently developed with:

- **Visual Studio 2022**
- **C#**
- **.NET 8**
- **WPF**

The project also uses:

- `System.Management`

Install it through NuGet if it is not already included in the project.

---

## 🔧 Building from Source

1. Clone the repository:

```bash
git clone https://github.com/YOUR-USERNAME/MazliBoost.git
```

2. Open the solution in **Visual Studio 2022**.

3. Restore NuGet packages.

4. Build the project:

```text
Ctrl + Shift + B
```

5. Run:

```text
F5
```

For a normal user build, publish the project as a Windows executable from Visual Studio.

---

## 🎮 How to Use

### 1. Start your game

Launch the game normally and enter gameplay.

### 2. Start MázliBoost

Open MázliBoost while the game is running.

### 3. Detect the game

MázliBoost will attempt to identify the game automatically.

If it does not immediately find it, click:

```text
↻ DETECT AGAIN
```

### 4. Boost

Once the game is detected:

```text
🚀 BOOST NOW
```

MázliBoost will apply the currently supported optimizations.

---

## ⚠️ Important Notes

### Administrator privileges

Some operations, such as changing the active Windows power plan, may require administrator privileges.

Windows may therefore display a UAC confirmation.

### Process priority

MázliBoost currently uses:

```text
AboveNormal
```

instead of `High`.

This is intentional. Setting games to `High` priority can unnecessarily starve Windows and other important processes.

### Memory cleanup

The memory optimization is intentionally lightweight.

Freeing or reducing working sets does **not** magically create RAM or guarantee higher FPS. Windows may simply load the data again when it is needed.

MázliBoost therefore treats this as a background-load optimization rather than a guaranteed FPS increase.

---

## 🔮 Roadmap

### V0.2

Planned improvements:

- 🎨 Improved UI and graphics
- 🖥️ Detailed System Information
- 📊 CPU/GPU/RAM usage monitoring
- 🌡️ Temperature monitoring
- 🎮 Improved game detection
- ⚙️ **More Optimizations & Tweaks** menu
- ↩️ Better restore/undo functionality

### Future

Potential features include:

- 📈 Before/After performance measurements
- ⏱️ Frametime monitoring
- 🧠 Smarter background-process optimization
- 🪟 Optional Windows debloat tools
- 🎮 Per-game optimization profiles
- ⚡ Optional GPU tuning where safe and properly supported
- 💾 Saved optimization profiles

Security-sensitive Windows settings will remain separated from normal one-click gaming optimizations.

---

## 🤝 Contributing

MázliBoost is an evolving project.

Issues, suggestions, optimization ideas, and code contributions are welcome.

If you find a bug:

1. Open an Issue.
2. Include your Windows version.
3. Describe what you were doing.
4. Include the relevant error message or log if available.

---

## 📜 License

Add your preferred open-source license here.

For example:

```text
MIT License
```

---

## 💡 Philosophy

MázliBoost is not intended to be another questionable:

> **"DOWNLOAD NOW — +500 FPS!!!"**

optimizer.

The project aims to provide **small, understandable, measurable optimizations** that users can actually see and control.

More performance where it is possible.

Less unnecessary background load.

And absolutely no deleting `System32` for +3 FPS. 😄

---

**MázliBoost — Performance without the snake oil.** 🚀

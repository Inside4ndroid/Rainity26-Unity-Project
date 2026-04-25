# Rainity26 — Advanced Documentation

> **Platform:** Windows (Standalone)  
> **Engine:** Unity (URP)  
> **DesktopApplication DLL Source:** [Inside4ndroid/Rainity26-DesktopApplication-API](https://github.com/Inside4ndroid/Rainity26-DesktopApplication-API)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Project Structure](#2-project-structure)
3. [Requirements & Setup](#3-requirements--setup)
4. [Rainity Component (Inspector)](#4-rainity-component-inspector)
5. [Data Structures](#5-data-structures)
6. [Rainity API Reference](#6-rainity-api-reference)
   - [Lifecycle](#lifecycle)
   - [Hardware Monitoring](#hardware-monitoring)
   - [System Information](#system-information)
   - [Desktop & Window Utilities](#desktop--window-utilities)
   - [User & Avatar](#user--avatar)
   - [File Utilities](#file-utilities)
   - [Input Simulation](#input-simulation)
   - [Startup Management](#startup-management)
   - [System Tray](#system-tray)
7. [SystemTray Class](#7-systemtray-class)
8. [RainityInput System](#8-rainityinput-system)
9. [WinAPI Class](#9-winapi-class)
10. [SetupDesktop Class](#10-setupdesktop-class)
11. [DesktopApplication.dll API Reference](#11-desktopapplicationdll-api-reference)
    - [Initialization](#initialization)
    - [CPU Functions](#cpu-functions)
    - [Memory Functions](#memory-functions)
    - [Disk Functions](#disk-functions)
    - [Network Functions](#network-functions)
    - [GPU Functions](#gpu-functions)
    - [Battery & Power](#battery--power)
    - [System Statistics](#system-statistics)
    - [Hardware Temperature (Placeholders)](#hardware-temperature-placeholders)
    - [Input & UI Utilities](#input--ui-utilities)
12. [Example Scripts](#12-example-scripts)
13. [Tips & Best Practices](#13-tips--best-practices)
14. [Limitations & Known Issues](#14-limitations--known-issues)

---

## 1. Overview

**Rainity26** is a Unity-based Windows desktop application framework that enables your Unity build to behave as an interactive live wallpaper or desktop widget. It provides:

- **Window layering control** — position your app behind desktop icons, behind all windows, or in a borderless overlay mode.
- **Full system hardware monitoring** — CPU, RAM, VRAM, GPU, disk I/O, network, battery, temperatures and more via the native `DesktopApplication.dll`.
- **Desktop integration** — system tray icons, startup shortcuts, wallpaper retrieval, file icon extraction.
- **Low-level input capture** — mouse hooks and keyboard simulation for when the window sits below the icon layer.

---

## 2. Project Structure

```
Assets/Rainity/
├── Editor/
│   └── RainityEditor.cs          — Custom Unity Inspector for the Rainity component
├── Example/
│   ├── DemoScript.cs             — General demo
│   ├── PerformanceMonitors.cs    — UI-based CPU/RAM/VRAM/Disk/GPU meter example
│   ├── ProgramIcon.cs            — File icon rendering example
│   └── Main.unity                — Example scene
├── Plugins/
│   └── x64/
│       └── DesktopApplication.dll  — Native C++ Windows monitoring DLL (x64)
│   └── x86/                        — (reserved for future 32-bit support)
├── Scripts/
│   ├── Rainity.cs                — Core MonoBehaviour & public API
│   ├── SetupDesktop.cs           — Window hierarchy & Win32 layering logic
│   ├── SystemTray.cs             — System tray icon wrapper
│   ├── WeatherObject.cs          — JSON model for legacy weather API
│   ├── WinAPI.cs                 — Win32 P/Invoke wrappers
│   └── Input/
│       ├── RainityInput.cs               — Low-level mouse/keyboard input
│       ├── RainityBaseInput.cs           — Base input module
│       └── RainityStandaloneInputModule.cs — Input System integration
├── Interop.IWshRuntimeLibrary.dll  — Windows Script Host COM interop (startup shortcuts)
├── System.Drawing.dll              — GDI+ for bitmap/icon operations
└── System.Windows.Forms.dll        — Windows Forms for system tray support
```

---

## 3. Requirements & Setup

### Runtime Requirements
| Requirement | Detail |
|---|---|
| OS | Windows 8.1 or later (Windows 10/11 recommended) |
| Architecture | x64 |
| Unity | 2022.x or later with URP |
| Build Target | PC, Mac & Linux Standalone → Windows x86_64 |

### Setup Steps

1. Add the **Rainity** prefab (or an empty GameObject) to your scene.
2. Attach the `Rainity` component.
3. Configure the [Inspector properties](#4-rainity-component-inspector) to set the desired window behaviour.
4. Build as a **Windows x64 Standalone** application.
5. Ensure `DesktopApplication.dll` is in the build output folder alongside the executable (Unity copies it automatically from `Plugins/x64/`).

> **Note:** Most features are **no-ops in the Unity Editor** and only take effect in a standalone build. `CreateSystemTrayIcon()` is explicitly skipped in the editor to avoid crashes.

---

## 4. Rainity Component (Inspector)

Attach the `Rainity` MonoBehaviour to a single GameObject in your scene.

| Property | Type | Default | Description |
|---|---|---|---|
| `windowOffset` | `Vector2` | `(0, -40)` | Pixel offset applied to the Unity window position on startup. Use `Y = -taskbarHeight` to avoid overlapping the taskbar. |
| `hideFromTaskbar` | `bool` | `false` | Hides the application from the Windows taskbar. |
| `neverHideWindow` | `bool` | `false` | Prevents the window from ever being hidden (kept on screen). |
| `keepBottomMost` | `bool` | `false` | Forces the window to stay below all other windows, but above the desktop. |
| `borderless` | `bool` | `false` | Removes the window title bar and borders. |
| `behindIcons` | `bool` | `false` | Reparents the Unity window to the desktop's WorkerW layer so it renders behind desktop icons. |
| `useRainityInput` | `bool` | `false` | Enables the low-level mouse hook (`RainityInput`) when the window is behind the icon layer. |

### Common Configurations

| Use Case | Settings |
|---|---|
| Live wallpaper behind icons | `Borderless = true`, `Behind Icons = true`, `Window Offset = (0, 0)` |
| Overlay wallpaper (above icons) | `Never Hide Window = true`, `Keep Bottom Most = true`, `Borderless = true` |
| Desktop widget with taskbar gap | Same as above + set `Window Offset Y` to your taskbar height (e.g. `-40`) |

---

## 5. Data Structures

All structs are defined globally (not nested) and can be used anywhere in your project.

### `MemoryInformation`
```csharp
public struct MemoryInformation {
    public float ramTotal;   // Total physical RAM, in bytes
    public float ramUsed;    // Used physical RAM, in bytes
    public float vRamTotal;  // Total VRAM (GPU memory), in bytes
    public float vRamUsed;   // Used VRAM, in bytes
}
```

### `PageFileInformation`
```csharp
public struct PageFileInformation {
    public long totalBytes;  // Total page file size, in bytes
    public long usedBytes;   // Used page file space, in bytes
}
```

### `DiskInformation`
```csharp
public struct DiskInformation {
    public string driveName;  // Drive path, e.g. "C:\"
    public long bytesTotal;   // Total disk capacity, in bytes
    public long bytesFree;    // Available free space, in bytes
}
```

### `DiskIOInformation`
```csharp
public struct DiskIOInformation {
    public long readBytesPerSec;   // Current disk read throughput (bytes/sec)
    public long writeBytesPerSec;  // Current disk write throughput (bytes/sec)
}
```

### `NetworkInformation`
```csharp
public struct NetworkInformation {
    public long bytesSentPerSec;  // Current upload throughput (bytes/sec)
    public long bytesRecvPerSec;  // Current download throughput (bytes/sec)
}
```

### `GPUInformation`
```csharp
public struct GPUInformation {
    public string name;               // GPU adapter name/description
    public uint   vendorId;           // PCI vendor ID (e.g. 0x10DE = NVIDIA, 0x1002 = AMD)
    public float  usagePercent;       // GPU utilisation (0–100)
    public float  temperatureCelsius; // GPU temperature (placeholder; returns -1)
    public int    clockSpeedMHz;      // GPU core clock (placeholder; returns -1)
    public int    memoryClockMHz;     // GPU memory clock (placeholder; returns -1)
    public int    fanSpeedRPM;        // Fan speed in RPM (placeholder; returns -1)
    public int    fanSpeedPercent;    // Fan speed as percentage (placeholder; returns -1)
    public float  powerDrawWatts;     // Power draw in watts (placeholder; returns -1)
    public float  voltageVolts;       // GPU voltage (placeholder; returns -1)
}
```

> **Note:** Fields marked as "placeholder" are not yet implemented in `DesktopApplication.dll` and return `-1`. See [§11 Hardware Temperature](#hardware-temperature-placeholders).

### `BatteryInformation`
```csharp
public struct BatteryInformation {
    public int  percentage;         // Battery level 0–100; -1 if no battery
    public bool isCharging;         // True if AC power connected
    public int  remainingMinutes;   // Estimated minutes remaining; -1 if charging/unknown
}
```

### `WeatherInformation` *(Deprecated)*
```csharp
public struct WeatherInformation {
    public string city;
    public string country;
    public string region;
}
```

### `RainityFile`
```csharp
public struct RainityFile {
    public string filePath;                  // Full path to file or directory
    public string fileName;                  // File name with extension
    public string fileNameWithoutExtension;  // File name without extension
    public string extension;                 // File extension (e.g. ".png"); empty for directories
    public bool   isDirectory;               // True if this entry is a directory
}
```

---

## 6. Rainity API Reference

All methods are `public static` on the `Rainity` class unless noted. Call them from any script — no reference to the `Rainity` instance is needed.

### Lifecycle

#### `Rainity.instance`
```csharp
public static Rainity instance { get; }
```
Singleton reference to the active `Rainity` MonoBehaviour. Set automatically in `Awake()`.

---

### Hardware Monitoring

All hardware query methods delegate to `DesktopApplication.dll`. Call `Initialize()` is handled internally in `Awake()`.

#### `GetMemoryInformation()`
```csharp
public static MemoryInformation GetMemoryInformation()
```
Returns total and used physical RAM and VRAM in **bytes**.

**Example:**
```csharp
MemoryInformation mem = Rainity.GetMemoryInformation();
float ramUsedGB  = mem.ramUsed  / 1_073_741_824f;
float ramTotalGB = mem.ramTotal / 1_073_741_824f;
Debug.Log($"RAM: {ramUsedGB:F1} / {ramTotalGB:F1} GB");
```

---

#### `GetCPUUsagePercent()`
```csharp
public static float GetCPUUsagePercent()
```
Returns overall CPU utilisation as a percentage (0–100). Updated once per second internally.

---

#### `GetCPUCoreUsageCount()`
```csharp
public static int GetCPUCoreUsageCount()
```
Returns the number of logical CPU cores/threads.

---

#### `GetCPUCoreUsagePercent(int coreIndex)`
```csharp
public static float GetCPUCoreUsagePercent(int coreIndex)
```
Returns utilisation for a specific logical core (0-indexed).

---

#### `GetCPUFrequencyMHz()`
```csharp
public static int GetCPUFrequencyMHz()
```
Returns the CPU base frequency in MHz.

---

#### `GetCPUModelName()`
```csharp
public static string GetCPUModelName()
```
Returns the CPU brand/model name string (e.g. `"Intel(R) Core(TM) i7-13700K"`).

---

#### `GetCPUTemperatureCelsius()`
```csharp
public static float GetCPUTemperatureCelsius()
```
Returns CPU temperature in Celsius. Currently a **placeholder** — returns `-1`. See [§14](#14-limitations--known-issues).

---

#### `GetPageFileInformation()`
```csharp
public static PageFileInformation GetPageFileInformation()
```
Returns page file (virtual memory) total and used space in **bytes**.

---

#### `GetDiskIOInformation()`
```csharp
public static DiskIOInformation GetDiskIOInformation()
```
Returns real-time disk read/write throughput in **bytes per second** across all drives.

---

#### `GetDiskInformation(string driveName)`
```csharp
public static DiskInformation GetDiskInformation(string driveName)
```
Returns capacity and free space for the specified drive.

| Parameter | Example |
|---|---|
| `driveName` | `"C:\\"` |

---

#### `GetDiskInformation(char driveLetter)`
```csharp
public static DiskInformation GetDiskInformation(char driveLetter)
```
Overload accepting a single drive letter character (e.g. `'C'`). Queries `DesktopApplication.dll` directly.

---

#### `GetNetworkInformation()`
```csharp
public static NetworkInformation GetNetworkInformation()
```
Returns bytes sent/received per second aggregated across all network interfaces.

---

#### `GetGPUInformation()`
```csharp
public static GPUInformation GetGPUInformation()
```
Returns a fully-populated `GPUInformation` struct. Placeholder fields return `-1`.

**Example:**
```csharp
GPUInformation gpu = Rainity.GetGPUInformation();
Debug.Log($"GPU: {gpu.name} | Usage: {gpu.usagePercent:F1}%");
```

---

#### `GetBatteryInformation()`
```csharp
public static BatteryInformation GetBatteryInformation()
```
Returns battery charge level, charging state, and estimated time remaining.

**Example:**
```csharp
BatteryInformation bat = Rainity.GetBatteryInformation();
if (bat.percentage >= 0)
    Debug.Log($"Battery: {bat.percentage}% {(bat.isCharging ? "(Charging)" : "")}");
else
    Debug.Log("No battery / desktop system.");
```

---

#### `GetUptimeSeconds()`
```csharp
public static long GetUptimeSeconds()
```
Returns system uptime since last boot in **seconds**.

---

#### `GetRunningProcessCount()`
```csharp
public static int GetRunningProcessCount()
```
Returns the total number of running processes.

---

#### `GetRunningThreadCount()`
```csharp
public static int GetRunningThreadCount()
```
Returns the total number of active threads system-wide.

---

#### `GetSystemHandleCount()`
```csharp
public static int GetSystemHandleCount()
```
Returns the total number of open handles system-wide.

---

#### `GetMotherboardTemperatureCelsius()`
```csharp
public static float GetMotherboardTemperatureCelsius()
```
Placeholder — returns `-1`.

---

#### `GetFanSpeedRPM(int fanIndex)`
```csharp
public static int GetFanSpeedRPM(int fanIndex)
```
Placeholder — returns `-1`.

---

### System Information

#### `GetWindowsVersionString()`
```csharp
public static string GetWindowsVersionString()
```
Returns the Windows version and build string (e.g. `"Windows 11 Build 22631"`).

---

### Desktop & Window Utilities

#### `GetWallpaperImage()`
```csharp
public static Texture2D GetWallpaperImage()
```
Retrieves the current desktop wallpaper as a `Texture2D`. Attempts four different registry/file-system paths as fallbacks.

Returns `null` and logs an error if the wallpaper cannot be located.

---

#### `GetFileIcon(string path)`
```csharp
public static Texture2D GetFileIcon(string path)
```
Returns a **256×256** `Texture2D` containing the shell icon for the given file or directory path. Uses `GetJumboIcon` from `DesktopApplication.dll`.

**Example:**
```csharp
Texture2D icon = Rainity.GetFileIcon(@"C:\Windows\System32\notepad.exe");
GetComponent<RawImage>().texture = icon;
```

---

#### `GetAverageColorOfTexture(Texture2D tex)`
```csharp
public static Color32 GetAverageColorOfTexture(Texture2D tex)
```
Averages all non-transparent pixels in `tex` and returns the result as a `Color32`. Useful for dynamic UI tinting based on wallpaper colours.

---

### User & Avatar

#### `GetUserName()`
```csharp
public static string GetUserName()
```
Returns the current Windows username (local name only, no domain).

---

#### `GetUserName(bool includeDomain)`
```csharp
public static string GetUserName(bool includeDomain)
```
Returns the username. When `includeDomain` is `true`, returns the fully-qualified name (e.g. `"DOMAIN\username"`).

---

#### `GetUserAvatar()`
```csharp
public static Texture2D GetUserAvatar()
```
Retrieves the current user's Windows account picture as a `Texture2D`. Converts the source image to PNG via `System.Drawing.Bitmap` before loading.

Returns `null` if no avatar file is found or loading fails.

---

### File Utilities

#### `OpenFile(string path)`
```csharp
public static void OpenFile(string path)
```
Opens the file or directory at `path` using the default associated application (equivalent to double-clicking in Explorer).

---

#### `OpenFile(string path, string arguments)`
```csharp
public static void OpenFile(string path, string arguments)
```
Same as above, with additional command-line arguments passed to the launched process.

---

#### `GetFiles(string directory)`
```csharp
public static RainityFile[] GetFiles(string directory)
```
Enumerates all files **and** subdirectories inside `directory`, returning a `RainityFile[]`.

Returns `null` and logs an error if the directory does not exist.

---

### Input Simulation

#### `SimulateKey(uint keyCode)`
```csharp
public static void SimulateKey(uint keyCode)
```
Simulates a key press using a raw [Windows Virtual-Key code](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes).

---

#### `SimulateKey(RainityInput.VirtualKeys keyCode)`
```csharp
public static void SimulateKey(RainityInput.VirtualKeys keyCode)
```
Overload accepting the typed `RainityInput.VirtualKeys` enum.

**Example:**
```csharp
// Press the Windows key
Rainity.SimulateKey(RainityInput.VirtualKeys.LWIN);
```

---

### Startup Management

#### `AddToStartup()`
```csharp
public static void AddToStartup()
```
Creates a Windows startup shortcut (`RainityApplication.lnk`) in the current user's Startup folder so the application launches on login. **No-op in the Unity Editor.**

---

#### `RemoveFromStartup()`
```csharp
public static void RemoveFromStartup()
```
Deletes the startup shortcut created by `AddToStartup()`. **No-op in the Unity Editor.**

---

### System Tray

#### `CreateSystemTrayIcon()`
```csharp
public static SystemTray CreateSystemTrayIcon()
```
Creates a new system tray (notification area) icon using the application's executable icon. Returns the `SystemTray` object to which context menu items can be added.

Returns `null` in the Unity Editor.

**Example:**
```csharp
SystemTray tray = Rainity.CreateSystemTrayIcon();
tray.AddItem("Quit", () => Application.Quit());
tray.AddSeparator();
tray.AddItem("About", () => Debug.Log("Rainity26 v1.0"));
```

All created trays are automatically disposed when the application quits.

---

## 7. SystemTray Class

`SystemTray` wraps `System.Windows.Forms.NotifyIcon`. It implements `IDisposable`.

```csharp
SystemTray tray = Rainity.CreateSystemTrayIcon();
```

### Methods

| Method | Description |
|---|---|
| `SetTitle(string title)` | Sets the tooltip text shown when hovering the tray icon. |
| `AddItem(string label, Action function)` | Adds a context menu item with the given label; `function` is invoked when clicked. |
| `AddSeparator()` | Adds a horizontal separator line in the context menu. |
| `Dispose()` | Hides and releases the tray icon. Called automatically on application quit. |

> **Note:** `SetIcon(Texture2D)` and `ShowNotification(...)` are defined but currently do not work reliably — avoid using them.

---

## 8. RainityInput System

`RainityInput` provides mouse and keyboard input when the Unity window is parented behind the desktop icon layer (i.e. the standard Unity `Input` system receives no events in that state).

### Enabling

Set `Use Rainity Input = true` on the `Rainity` component, or call:
```csharp
RainityInput.Initialize();
```

### Mouse Input

```csharp
// Analogous to Unity's Input.GetMouseButton / GetMouseButtonDown / GetMouseButtonUp
bool held = RainityInput.GetMouseButton(0);      // 0=left, 1=right, 2=middle
bool down = RainityInput.GetMouseButtonDown(0);
bool up   = RainityInput.GetMouseButtonUp(0);

// Current cursor position in screen space
Vector3 pos = RainityInput.mousePosition;
```

### Keyboard Input

```csharp
bool held = RainityInput.GetKey(RainityInput.VirtualKeys.VK_A);
bool down = RainityInput.GetKeyDown(RainityInput.VirtualKeys.VK_SPACE);
bool up   = RainityInput.GetKeyUp(RainityInput.VirtualKeys.VK_ESCAPE);
```

### Named Button Bindings

Add `RainityInputEntry` entries to the `inputEntries` array on the `RainityInput` MonoBehaviour, then query by name:
```csharp
bool fire = RainityInput.GetButton("Fire1");
```

### How It Works

`RainityInput` installs a **low-level mouse hook** via `SetMouseHook` from `DesktopApplication.dll`. The hook receives events before they are dispatched to the desktop, allowing the wallpaper to react to mouse clicks on the desktop.

---

## 9. WinAPI Class

`WinAPI` is a singleton MonoBehaviour exposing raw Win32 P/Invoke declarations. Most are used internally by `SetupDesktop`, but are public for advanced use.

### Static State (updated every frame)

```csharp
WinAPI.activeWindow;      // IntPtr — handle of the active window
WinAPI.foregroundWindow;  // IntPtr — handle of the foreground window
WinAPI.thisGame;          // IntPtr — handle of the Unity window (set in Start)
WinAPI.desktopWindow;     // IntPtr — handle of the desktop root window
WinAPI.windowRect;        // RECT   — bounding rect of the foreground window
```

### Key Structures

#### `WinAPI.RECT`
Standard Win32 `RECT` struct with `Left`, `Top`, `Right`, `Bottom` plus computed `Width`, `Height`, `X`, `Y` properties.

#### `WinAPI.MARGINS`
Used for `DwmExtendFrameIntoClientArea` to control the glass frame extent.

### Selected P/Invoke Exports

| Method | Library | Description |
|---|---|---|
| `GetWindowRect(IntPtr, out RECT)` | user32 | Gets window bounding rectangle |
| `SetWindowLong(IntPtr, int, long)` | user32 | Modifies window style flags |
| `SetWindowPos(IntPtr, ...)` | user32 | Repositions/resizes a window |
| `FindWindow(string, string)` | user32 | Finds a top-level window by class/title |
| `GetDesktopWindow()` | user32 | Returns the desktop root HWND |
| `SetForegroundWindow(IntPtr)` | user32 | Brings a window to the foreground |
| `SystemParametersInfo(...)` | user32 | Queries/sets system parameters |
| `DwmExtendFrameIntoClientArea(...)` | Dwmapi | Extends glass frame into client area |
| `GetCursorPos(out POINTFX)` | user32 | Gets cursor screen coordinates |
| `SHGetUserPicturePath(...)` | shell32 | Gets the current user's avatar image path |

---

## 10. SetupDesktop Class

`SetupDesktop` handles the low-level window reparenting and style manipulation that makes the Unity window behave as a desktop layer. It is driven automatically by `Rainity.Start()` — you do not normally call it directly.

### Key Static Properties

| Property | Type | Description |
|---|---|---|
| `windowOffset` | `Vector2` | Pixel offset from `(0,0)` screen position |
| `hideFromTaskbar` | `bool` | Hide from taskbar via `WS_EX_TOOLWINDOW` |
| `neverHide` | `bool` | Prevent the window from being minimised/hidden |
| `keepBottomMost` | `bool` | Maintain `HWND_BOTTOM` z-order |
| `borderless` | `bool` | Strip `WS_CAPTION`, `WS_THICKFRAME`, etc. |
| `behindIcons` | `bool` | Parent the window into the WorkerW desktop layer |
| `forceInBack` | `bool` | Continuously re-apply bottom-most z-order |
| `fWidth`, `fHeight` | `int` | Desktop/screen dimensions used for sizing |
| `appQuitting` | `bool` | Set to `true` just before `OnApplicationQuit` |

### Behind Icons — Technical Detail

When `behindIcons = true`, `SetupDesktop.Initialize()`:
1. Sends `0x052C` to `Progman` to force Windows to spawn a `WorkerW` layer.
2. Enumerates top-level windows to find the `WorkerW` that contains `SHELLDLL_DefView`.
3. Calls `SetParent(unityHwnd, workerW)` to reparent the Unity window into that layer.
4. Falls back to `Progman` directly if `WorkerW` is not found (logs a warning).

---

## 11. DesktopApplication.dll API Reference

**Source:** [https://github.com/Inside4ndroid/Rainity26-DesktopApplication-API](https://github.com/Inside4ndroid/Rainity26-DesktopApplication-API)  
**Language:** C (95.6%) / C++ (2.7%)  
**Build:** Release x64 — Visual Studio 2017+, C++17, Windows SDK 10.0+

The DLL is located at `Assets/Rainity/Plugins/x64/DesktopApplication.dll`. Unity includes it automatically in the build output.

All exports use the `extern "C"` linkage convention. The C# P/Invoke signatures used inside `Rainity.cs` are reproduced below for reference.

---

### Initialization

#### `Initialize()`
```csharp
[DllImport("DesktopApplication")]
private static extern bool Initialize();
```
**Must be called once** before using any monitoring functions. Initialises PDH (Performance Data Helper) query handles. Returns `true` on success.

Called automatically in `Rainity.Awake()`.

> **Sampling Note:** PDH counters require at least two samples to compute rates. There may be a ~1 second warm-up period before CPU/disk/network values are non-zero.

---

### CPU Functions

#### `GetCPUPercentPDH()`
```csharp
[DllImport("DesktopApplication")]
private static extern float GetCPUPercentPDH();
```
Returns overall CPU utilisation as a float (0–100). Uses Windows PDH — matches Task Manager accuracy. Updated internally by `Rainity.Update()` once per second.

---

#### `GetCPUPercent()`
```csharp
[DllImport("DesktopApplication")]
private static extern int GetCPUPercent();
```
Alternative integer CPU usage reading. Less accurate than PDH version.

---

#### `GetCPUCoreCount()`
```csharp
[DllImport("DesktopApplication")]
private static extern int GetCPUCoreCount();
```
Returns the number of logical processors.

---

#### `GetCPUCorePercent(int coreIndex)`
```csharp
[DllImport("DesktopApplication")]
private static extern float GetCPUCorePercent(int coreIndex);
```
Returns utilisation for logical core `coreIndex` (0-based) as a float (0–100).

---

#### `GetCPUFrequency()`
```csharp
[DllImport("DesktopApplication")]
private static extern int GetCPUFrequency();
```
Returns the CPU base clock frequency in MHz.

---

#### `GetCPUName(StringBuilder buffer, int bufferSize)`
```csharp
[DllImport("DesktopApplication", CharSet = CharSet.Unicode)]
private static extern bool GetCPUName(StringBuilder buffer, int bufferSize);
```
Writes the CPU brand name into `buffer` (Unicode). Returns `true` on success.

**Usage pattern:**
```csharp
var sb = new StringBuilder(256);
if (GetCPUName(sb, sb.Capacity))
    Debug.Log(sb.ToString());
```

---

### Memory Functions

#### `GetMemoryInfo(out long, out long, out long, out long)`
```csharp
[DllImport("DesktopApplication")]
private static extern bool GetMemoryInfo(
    out long memTotal, out long memUsed,
    out long vMemTotal, out long vMemUsed);
```
Outputs physical RAM and VRAM totals/usage in **bytes**. VRAM is read via DXGI 1.4 (requires Windows 8.1+).

Returns `true` on success.

---

#### `GetPageFileUsage(out long, out long)`
```csharp
[DllImport("DesktopApplication")]
private static extern bool GetPageFileUsage(out long totalBytes, out long usedBytes);
```
Returns page file (virtual memory) total and used in **bytes**.

---

### Disk Functions

#### `GetDiskIOStats(out long, out long)`
```csharp
[DllImport("DesktopApplication")]
private static extern bool GetDiskIOStats(
    out long readBytesPerSec,
    out long writeBytesPerSec);
```
Returns current aggregate disk read and write throughput in **bytes per second** using PDH.

---

#### `GetDiskSpace(char driveLetter, out long, out long)`
```csharp
[DllImport("DesktopApplication", CharSet = CharSet.Unicode)]
private static extern bool GetDiskSpace(
    char driveLetter,
    out long totalBytes,
    out long freeBytes);
```
Returns total capacity and free space for the specified drive letter (e.g. `'C'`) in **bytes**.

---

### Network Functions

#### `GetNetworkStats(out long, out long)`
```csharp
[DllImport("DesktopApplication")]
private static extern bool GetNetworkStats(
    out long bytesSentPerSec,
    out long bytesRecvPerSec);
```
Returns upload and download throughput in **bytes per second** aggregated across all network interfaces. Uses PDH with wildcard interface matching.

---

### GPU Functions

#### `GetGPUUsagePercent()`
```csharp
[DllImport("DesktopApplication")]
private static extern float GetGPUUsagePercent();
```
Returns GPU engine utilisation as a float (0–100). Availability depends on the GPU driver exposing the PDH counter `\GPU Engine(*engtype_3D)\Utilization Percentage`.

---

#### `GetGPUName(StringBuilder buffer, int bufferSize)`
```csharp
[DllImport("DesktopApplication", CharSet = CharSet.Unicode)]
private static extern bool GetGPUName(StringBuilder buffer, int bufferSize);
```
Writes the GPU adapter description into `buffer` (Unicode).

---

#### `GetGPUVendorID()`
```csharp
[DllImport("DesktopApplication")]
private static extern uint GetGPUVendorID();
```
Returns the PCI vendor ID of the primary GPU.

| Vendor | ID |
|---|---|
| NVIDIA | `0x10DE` |
| AMD | `0x1002` |
| Intel | `0x8086` |

---

#### `GetGPUTemperature()` *(Placeholder)*
```csharp
[DllImport("DesktopApplication")]
private static extern float GetGPUTemperature();
```
Returns `-1`. Vendor-specific SDK required. See [§14](#14-limitations--known-issues).

---

#### `GetGPUClockSpeed()` *(Placeholder)*
```csharp
[DllImport("DesktopApplication")]
private static extern int GetGPUClockSpeed();
```
Returns `-1`.

---

#### `GetGPUMemoryClock()` *(Placeholder)*
```csharp
[DllImport("DesktopApplication")]
private static extern int GetGPUMemoryClock();
```
Returns `-1`.

---

#### `GetGPUFanSpeed()` *(Placeholder)*
```csharp
[DllImport("DesktopApplication")]
private static extern int GetGPUFanSpeed();
```
Returns `-1` (RPM).

---

#### `GetGPUFanSpeedPercent()` *(Placeholder)*
```csharp
[DllImport("DesktopApplication")]
private static extern int GetGPUFanSpeedPercent();
```
Returns `-1`.

---

#### `GetGPUPowerDraw()` *(Placeholder)*
```csharp
[DllImport("DesktopApplication")]
private static extern float GetGPUPowerDraw();
```
Returns `-1` (watts).

---

#### `GetGPUVoltage()` *(Placeholder)*
```csharp
[DllImport("DesktopApplication")]
private static extern float GetGPUVoltage();
```
Returns `-1` (volts).

---

### Battery & Power

#### `GetBatteryStatus(out int, out bool, out int)`
```csharp
[DllImport("DesktopApplication")]
private static extern bool GetBatteryStatus(
    out int  percentage,
    out bool isCharging,
    out int  remainingMinutes);
```
Returns battery percentage (0–100; `-1` = no battery), charging state, and estimated time remaining (`-1` = unknown/charging).

---

### System Statistics

#### `GetSystemUptime()`
```csharp
[DllImport("DesktopApplication")]
private static extern long GetSystemUptime();
```
Returns seconds elapsed since last Windows boot.

---

#### `GetProcessCount()`
```csharp
[DllImport("DesktopApplication")]
private static extern int GetProcessCount();
```
Returns the total number of running processes.

---

#### `GetThreadCount()`
```csharp
[DllImport("DesktopApplication")]
private static extern int GetThreadCount();
```
Returns the total number of active threads.

---

#### `GetHandleCount()`
```csharp
[DllImport("DesktopApplication")]
private static extern int GetHandleCount();
```
Returns total open handle count.

---

#### `GetSystemInfoString(StringBuilder buffer, int bufferSize)`
```csharp
[DllImport("DesktopApplication", CharSet = CharSet.Unicode)]
private static extern bool GetSystemInfoString(StringBuilder buffer, int bufferSize);
```
Writes the Windows version and build info string into `buffer`.

---

### Hardware Temperature (Placeholders)

The following functions are defined in the DLL but **return `-1`** in the current release. Full implementation requires hardware-specific SDKs:

| Function | Notes |
|---|---|
| `GetCPUTemperature()` | Requires WMI / LibreHardwareMonitor / MSR reads |
| `GetMotherboardTemperature()` | Requires WMI / LibreHardwareMonitor |
| `GetFanSpeed(int fanIndex)` | Requires WMI / LibreHardwareMonitor |
| `GetGPUTemperature()` | Requires NVIDIA NVAPI / AMD ADL SDK |
| `GetGPUClockSpeed()` | Requires vendor SDK |
| `GetGPUMemoryClock()` | Requires vendor SDK |
| `GetGPUFanSpeed()` | Requires vendor SDK |
| `GetGPUPowerDraw()` | Requires NVAPI (NVIDIA only) |
| `GetGPUVoltage()` | Requires vendor SDK |

**Recommended libraries for implementation:**
- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) — CPU/motherboard temperatures
- [NVIDIA NVAPI](https://developer.nvidia.com/nvapi) — NVIDIA GPU metrics
- [AMD Display Library (ADL)](https://github.com/GPUOpen-LibrariesAndSDKs/display-library) — AMD GPU metrics

---

### Input & UI Utilities

#### `GetJumboIcon(string path, out int bmpLength)`
```csharp
[DllImport("DesktopApplication")]
private static extern IntPtr GetJumboIcon(
    [MarshalAs(UnmanagedType.LPTStr)] String path,
    out int bmpLength);
```
Returns a pointer to a raw 256×256 BGRA bitmap of the shell icon for `path`. `bmpLength` is the byte count. The caller copies the data via `Marshal.Copy`.

---

#### `SetMouseHook(IntPtr procModule)` / `UnhookMouseHook()`
```csharp
[DllImport("DesktopApplication")]
private static extern bool SetMouseHook(IntPtr procModule);
[DllImport("DesktopApplication")]
private static extern bool UnhookMouseHook();
```
Installs/removes a low-level WH_MOUSE_LL Windows hook. Used by `RainityInput`.

---

#### `GetMouseDown()` / `GetMouseUp()`
```csharp
[DllImport("DesktopApplication")]
private static extern bool GetMouseDown();
[DllImport("DesktopApplication")]
private static extern bool GetMouseUp();
```
Returns whether a mouse button event was detected since the last call.

---

#### `SimulateKeypress(uint keyCode)`
```csharp
[DllImport("DesktopApplication")]
private static extern bool SimulateKeypress(uint keyCode);
```
Injects a virtual key press using `SendInput`. Uses [Windows Virtual-Key codes](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes).

---

#### `GetAvatarPath(string username, byte[] buffer)`
```csharp
[DllImport("DesktopApplication")]
private static extern bool GetAvatarPath(
    [MarshalAs(UnmanagedType.LPWStr)] String username,
    byte[] buffer);
```
Writes the file path of the Windows account picture for `username` into `buffer` (ASCII-encoded). Returns `true` if found.

---

## 12. Example Scripts

### Performance Monitor UI

`Assets/Rainity/Example/PerformanceMonitors.cs` demonstrates querying CPU, RAM, VRAM, Disk, and GPU each frame and displaying percentages in UI `Text` and radial `Image` fill components.

```csharp
void Update() {
    // RAM
    MemoryInformation mem = Rainity.GetMemoryInformation();
    ramText.text       = Mathf.Round(mem.ramUsed / mem.ramTotal * 100) + "%";
    ramImage.fillAmount = mem.ramUsed / mem.ramTotal;

    // VRAM
    vramText.text       = Mathf.Round(mem.vRamUsed / mem.vRamTotal * 100) + "%";
    vramImage.fillAmount = mem.vRamUsed / mem.vRamTotal;

    // Disk C:
    DiskInformation disk = Rainity.GetDiskInformation("C:\\");
    diskText.text        = Mathf.Round((float)disk.bytesFree / disk.bytesTotal * 100) + "%";
    diskImage.fillAmount = (float)disk.bytesFree / disk.bytesTotal;

    // CPU
    cpuText.text        = Mathf.Round(Rainity.GetCPUUsagePercent()) + "%";
    cpuImage.fillAmount = Rainity.GetCPUUsagePercent() / 100f;

    // GPU
    GPUInformation gpu  = Rainity.GetGPUInformation();
    gpuText.text        = Mathf.Round(gpu.usagePercent) + "%";
    gpuImage.fillAmount = gpu.usagePercent / 100f;
}
```

---

### System Tray with Quit Option

```csharp
using UnityEngine;

public class TraySetup : MonoBehaviour {
    void Start() {
        SystemTray tray = Rainity.CreateSystemTrayIcon();
        if (tray != null) {
            tray.AddItem("Open", () => Debug.Log("opened"));
            tray.AddSeparator();
            tray.AddItem("Quit", Application.Quit);
        }
    }
}
```

---

### Display User Wallpaper as Background

```csharp
using UnityEngine;
using UnityEngine.UI;

public class WallpaperBackground : MonoBehaviour {
    public RawImage backgroundImage;

    void Start() {
        Texture2D wallpaper = Rainity.GetWallpaperImage();
        if (wallpaper != null)
            backgroundImage.texture = wallpaper;
    }
}
```

---

### Show All Per-Core CPU Usage

```csharp
void Update() {
    int cores = Rainity.GetCPUCoreUsageCount();
    for (int i = 0; i < cores; i++) {
        float usage = Rainity.GetCPUCoreUsagePercent(i);
        Debug.Log($"Core {i}: {usage:F1}%");
    }
}
```

---

### Network Throughput Display

```csharp
void Update() {
    NetworkInformation net = Rainity.GetNetworkInformation();
    float uploadMBps   = net.bytesSentPerSec / 1_048_576f;
    float downloadMBps = net.bytesRecvPerSec / 1_048_576f;
    Debug.Log($"Up: {uploadMBps:F2} MB/s  Down: {downloadMBps:F2} MB/s");
}
```

---

## 13. Tips & Best Practices

- **Initialise once:** `DesktopApplication.dll`'s `Initialize()` is called automatically in `Rainity.Awake()`. Do not call it again.
- **PDH warm-up:** The first ~1 second after launch, CPU/disk/network counters may read `0`. Display a loading state or skip the first reading.
- **Hardware monitor polling rate:** Query hardware at 500 ms–2 s intervals. Polling faster provides no extra accuracy and increases CPU overhead. The built-in `Rainity.Update()` already throttles CPU queries to once per second.
- **Behind-icons + input:** Enable `Use Rainity Input` only when `Behind Icons` is also enabled. Leaving the mouse hook installed unnecessarily adds overhead and can interfere with other applications.
- **System tray in editor:** `CreateSystemTrayIcon()` returns `null` in the editor. Always null-check the return value.
- **Thread safety:** PDH query functions in `DesktopApplication.dll` are isolated per-query and are safe to call from background threads, but the `Rainity` wrapper methods should be called from the Unity main thread only.
- **Startup shortcut:** The shortcut is saved as `RainityApplication.lnk` — calling `AddToStartup()` multiple times is safe (overwrites the same file).
- **GPU vendor ID:** Use `GetGPUVendorID()` to branch on vendor-specific paths, e.g. to show an NVIDIA logo vs. AMD logo.

---

## 14. Limitations & Known Issues

| Issue | Detail |
|---|---|
| Temperature APIs return -1 | `GetCPUTemperature`, `GetGPUTemperature`, `GetMotherboardTemperature`, `GetFanSpeed` are placeholders. Implementing them requires hardware-specific libraries (NVAPI, ADL, LibreHardwareMonitor). |
| GPU counter unavailability | PDH GPU counters (`GPU Engine` namespace) may not be present on older Windows versions or with certain GPU drivers. `GetGPUUsagePercent` may return 0. |
| Network aggregation | `GetNetworkStats` uses a wildcard PDH counter and aggregates all network interfaces. Per-adapter breakdown is not supported. |
| Behind-icons on multi-monitor | WorkerW reparenting targets the primary monitor's WorkerW. Multi-monitor behind-icons support may vary. |
| Weather API deprecated | `GetWeatherInformation()` is marked `[Obsolete]`. The Yahoo Weather API it relied upon has been shut down. |
| System tray in editor | `CreateSystemTrayIcon()` is no-op in the editor and may cause instability if forced. |
| Administrative privileges | Some advanced Win32 operations (e.g. certain window style changes on system windows) may require running as Administrator. |
| `SetIcon` / `ShowNotification` | These `SystemTray` methods are defined but currently non-functional. |

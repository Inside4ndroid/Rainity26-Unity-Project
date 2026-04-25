using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Microsoft.Win32;
using IWshRuntimeLibrary;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

public struct MemoryInformation {
	public float ramTotal;
	public float ramUsed;
	public float vRamTotal;
	public float vRamUsed;
}

public struct PageFileInformation {
	public long totalBytes;
	public long usedBytes;
}

public struct DiskInformation {
	public string driveName;
	public long bytesTotal;
	public long bytesFree;
}

public struct DiskIOInformation {
	public long readBytesPerSec;
	public long writeBytesPerSec;
}

public struct NetworkInformation {
	public long bytesSentPerSec;
	public long bytesRecvPerSec;
}

public struct GPUInformation {
	public string name;
	public uint vendorId;
	public float usagePercent;
	public float temperatureCelsius;
	public int clockSpeedMHz;
	public int memoryClockMHz;
	public int fanSpeedRPM;
	public int fanSpeedPercent;
	public float powerDrawWatts;
	public float voltageVolts;
}

public struct BatteryInformation {
	public int percentage;
	public bool isCharging;
	public int remainingMinutes;
}

public struct WeatherInformation {
	public string city;
	public string country;
	public string region;
}

public struct RainityFile {
	public string filePath;
	public string fileName;
	public string fileNameWithoutExtension;
	public string extension;
	public bool isDirectory;
}

public class Rainity : MonoBehaviour {

	public static Rainity instance { get; set; }

	public Vector2 windowOffset = new Vector2(0, -40);
	public bool hideFromTaskbar = false;
	public bool neverHideWindow = false;
	public bool keepBottomMost = false;
	public bool borderless = false;
	public bool behindIcons = false;
	public bool useRainityInput = false;

	private float timer = 1f;
	private static float cpuUsage = 0;

	[DllImport("DesktopApplication")]
	private static extern IntPtr GetJumboIcon([MarshalAs(UnmanagedType.LPTStr)]String path, out int bmpLength);

	[DllImport("DesktopApplication")]
	private static extern bool GetMemoryInfo(out long memTotal, out long memUsed, out long vMemTotal, out long vMemUsed);

	//Called before getting CPU information
	[DllImport("DesktopApplication")]
	private static extern bool Initialize();

	[DllImport("DesktopApplication")]
	private static extern float GetCPUPercentPDH();

	[DllImport("DesktopApplication")]
	private static extern int GetCPUPercent();

	[DllImport("DesktopApplication")]
	private static extern int GetCPUCoreCount();

	[DllImport("DesktopApplication")]
	private static extern float GetCPUCorePercent(int coreIndex);

	[DllImport("DesktopApplication")]
	private static extern int GetCPUFrequency();

	[DllImport("DesktopApplication", CharSet = CharSet.Unicode)]
	private static extern bool GetCPUName(StringBuilder buffer, int bufferSize);

	[DllImport("DesktopApplication")]
	private static extern float GetCPUTemperature();

	[DllImport("DesktopApplication")]
	private static extern bool GetPageFileUsage(out long totalBytes, out long usedBytes);

	[DllImport("DesktopApplication")]
	private static extern bool GetDiskIOStats(out long readBytesPerSec, out long writeBytesPerSec);

	[DllImport("DesktopApplication", CharSet = CharSet.Unicode)]
	private static extern bool GetDiskSpace(char driveLetter, out long totalBytes, out long freeBytes);

	[DllImport("DesktopApplication")]
	private static extern bool GetNetworkStats(out long bytesSentPerSec, out long bytesRecvPerSec);

	[DllImport("DesktopApplication")]
	private static extern float GetGPUUsagePercent();

	[DllImport("DesktopApplication", CharSet = CharSet.Unicode)]
	private static extern bool GetGPUName(StringBuilder buffer, int bufferSize);

	[DllImport("DesktopApplication")]
	private static extern float GetGPUTemperature();

	[DllImport("DesktopApplication")]
	private static extern int GetGPUClockSpeed();

	[DllImport("DesktopApplication")]
	private static extern int GetGPUMemoryClock();

	[DllImport("DesktopApplication")]
	private static extern int GetGPUFanSpeed();

	[DllImport("DesktopApplication")]
	private static extern int GetGPUFanSpeedPercent();

	[DllImport("DesktopApplication")]
	private static extern float GetGPUPowerDraw();

	[DllImport("DesktopApplication")]
	private static extern float GetGPUVoltage();

	[DllImport("DesktopApplication")]
	private static extern uint GetGPUVendorID();

	[DllImport("DesktopApplication")]
	private static extern bool GetBatteryStatus(out int percentage, out bool isCharging, out int remainingMinutes);

	[DllImport("DesktopApplication")]
	private static extern long GetSystemUptime();

	[DllImport("DesktopApplication")]
	private static extern int GetProcessCount();

	[DllImport("DesktopApplication")]
	private static extern int GetThreadCount();

	[DllImport("DesktopApplication")]
	private static extern int GetHandleCount();

	[DllImport("DesktopApplication", CharSet = CharSet.Unicode)]
	private static extern bool GetSystemInfoString(StringBuilder buffer, int bufferSize);

	[DllImport("DesktopApplication")]
	private static extern float GetMotherboardTemperature();

	[DllImport("DesktopApplication")]
	private static extern int GetFanSpeed(int fanIndex);

	[DllImport("DesktopApplication")]
	private static extern bool SimulateKeypress(uint keyCode);

	[DllImport("DesktopApplication")]
	private static extern bool GetAvatarPath([MarshalAs(UnmanagedType.LPWStr)] String username, byte[] buffer);

	public void Awake() {
		instance = this;

		Initialize();
	}

	public void Start() {
		SetupDesktop.windowOffset = windowOffset;
		SetupDesktop.hideFromTaskbar = hideFromTaskbar;
		SetupDesktop.neverHide = neverHideWindow;
		SetupDesktop.keepBottomMost = keepBottomMost;
		SetupDesktop.borderless = borderless;
		SetupDesktop.behindIcons = behindIcons;
		SetupDesktop.Initialize();

		SetupDesktop.AddWinProc();

		if (useRainityInput) {
			RainityInput.Initialize();
		}

		Application.runInBackground = true;
	}

	/// <summary>
	/// <para>Gets a 256x256 file icon of the specified file or directory.</para>
	/// </summary>
	/// <param name="path"></param>
	/// <returns>Returns a Texutre2D of the icon.</returns>
	public static Texture2D GetFileIcon(String path) {
		int bmpLength = 0;
		path = path.Replace("\"", "");
		IntPtr iconHandle = GetJumboIcon(path, out bmpLength);

		byte[] byteArray = new byte[bmpLength];
		Marshal.Copy(iconHandle, byteArray, 0, bmpLength);

		byte[] red = new byte[byteArray.Length / 4];
		byte[] green = new byte[byteArray.Length / 4];
		byte[] blue = new byte[byteArray.Length / 4];
		byte[] alpha = new byte[byteArray.Length / 4];

		for (int i = 0; i < byteArray.Length/4; i++) {
			red[i] = byteArray[i * 4];
			green[i] = byteArray[i * 4 + 1];
			blue[i] = byteArray[i * 4 + 2];
			alpha[i] = byteArray[i * 4 + 3];
		}

		for (int i = 0; i < byteArray.Length / 4; i++) {
			byteArray[i * 4] = alpha[i];
			byteArray[i * 4 + 1] = blue[i];
			byteArray[i * 4 + 2] = green[i];
			byteArray[i * 4 + 3] = red[i];
		}

		Texture2D tex = new Texture2D(256, 256, TextureFormat.ARGB32, false);
		tex.LoadRawTextureData(byteArray);
		tex.Apply();

		return tex;
	}

	/// <summary>
	/// <para>Gets information about memory and video memory usage.</para>
	/// </summary>
	/// <returns>A Rainity.MemoryInformation structure with all the information received</returns>
	public static MemoryInformation GetMemoryInformation() {
		long memTotal = 0;
		long memUsed = 0;
		long vMemTotal = 0;
		long vMemUsed = 0;
		GetMemoryInfo(out memTotal, out memUsed, out vMemTotal, out vMemUsed);
		MemoryInformation memInfo = new MemoryInformation();
		memInfo.ramTotal = (float)memTotal;
		memInfo.ramUsed = (float)memUsed;
		memInfo.vRamTotal = (float)vMemTotal;
		memInfo.vRamUsed = (float)vMemUsed;
		return memInfo;
	}

	/// <summary>
	/// <para>Gets the CPU usage of all processes as a percent.</para>
	/// </summary>
	/// <returns>The current CPU usage</returns>
	public static float GetCPUUsagePercent() {
		return cpuUsage;
	}

	/// <summary>
	/// <para>Gets information about the specified disk.</para>
	/// </summary>
	/// <param name="driveName">Example: C:\\</param>
	/// <returns>A Rainity.DiskInformation structure with all the information about the disk</returns>
	public static DiskInformation GetDiskInformation(string driveName) {
		DiskInformation diskInfo = new DiskInformation();

		foreach (DriveInfo drive in DriveInfo.GetDrives()) {
			if (drive.IsReady && drive.Name == driveName) {
				diskInfo.bytesFree = drive.TotalFreeSpace;
				diskInfo.bytesTotal = drive.TotalSize;
				diskInfo.driveName = drive.Name;
			}
		}

		return diskInfo;
	}

	public static DiskInformation GetDiskInformation(char driveLetter) {
		DiskInformation diskInfo = new DiskInformation();
		diskInfo.driveName = driveLetter.ToString();
		GetDiskSpace(driveLetter, out diskInfo.bytesTotal, out diskInfo.bytesFree);
		return diskInfo;
	}

	public static int GetCPUCoreUsageCount() {
		return GetCPUCoreCount();
	}

	public static float GetCPUCoreUsagePercent(int coreIndex) {
		return GetCPUCorePercent(coreIndex);
	}

	public static int GetCPUFrequencyMHz() {
		return GetCPUFrequency();
	}

	public static string GetCPUModelName() {
		var buf = new StringBuilder(256);
		return GetCPUName(buf, buf.Capacity) ? buf.ToString() : string.Empty;
	}

	public static float GetCPUTemperatureCelsius() {
		return GetCPUTemperature();
	}

	public static PageFileInformation GetPageFileInformation() {
		PageFileInformation info = new PageFileInformation();
		GetPageFileUsage(out info.totalBytes, out info.usedBytes);
		return info;
	}

	public static DiskIOInformation GetDiskIOInformation() {
		DiskIOInformation info = new DiskIOInformation();
		GetDiskIOStats(out info.readBytesPerSec, out info.writeBytesPerSec);
		return info;
	}

	public static NetworkInformation GetNetworkInformation() {
		NetworkInformation info = new NetworkInformation();
		GetNetworkStats(out info.bytesSentPerSec, out info.bytesRecvPerSec);
		return info;
	}

	public static GPUInformation GetGPUInformation() {
		GPUInformation info = new GPUInformation();
		var buf = new StringBuilder(256);
		bool gotName = GetGPUName(buf, buf.Capacity);
		info.name = gotName ? buf.ToString() : string.Empty;
		info.vendorId = GetGPUVendorID();
		info.usagePercent = GetGPUUsagePercent();
		info.temperatureCelsius = GetGPUTemperature();
		info.clockSpeedMHz = GetGPUClockSpeed();
		info.memoryClockMHz = GetGPUMemoryClock();
		info.fanSpeedRPM = GetGPUFanSpeed();
		info.fanSpeedPercent = GetGPUFanSpeedPercent();
		info.powerDrawWatts = GetGPUPowerDraw();
		info.voltageVolts = GetGPUVoltage();
		return info;
	}

	public static BatteryInformation GetBatteryInformation() {
		BatteryInformation info = new BatteryInformation();
		GetBatteryStatus(out info.percentage, out info.isCharging, out info.remainingMinutes);
		return info;
	}

	public static long GetUptimeSeconds() {
		return GetSystemUptime();
	}

	public static int GetRunningProcessCount() {
		return GetProcessCount();
	}

	public static int GetRunningThreadCount() {
		return GetThreadCount();
	}

	public static int GetSystemHandleCount() {
		return GetHandleCount();
	}

	public static string GetWindowsVersionString() {
		var buf = new StringBuilder(512);
		return GetSystemInfoString(buf, buf.Capacity) ? buf.ToString() : string.Empty;
	}

	public static float GetMotherboardTemperatureCelsius() {
		return GetMotherboardTemperature();
	}

	public static int GetFanSpeedRPM(int fanIndex) {
		return GetFanSpeed(fanIndex);
	}

	/// <summary>
	/// <para>Simulates a key press by the user.</para>
	/// </summary>
	/// <param name="keyCode">The keycode in uint form; uses MSDN Virtual-Key codes</param>
	public static void SimulateKey(uint keyCode) {
		SimulateKeypress(keyCode);
	}

	/// <summary>
	/// <para>Simulates a key press by the user.</para>
	/// </summary>
	/// <param name="keyCode">The keycode in RainityInput.VirtualKeys enum form</param>
	public static void SimulateKey(RainityInput.VirtualKeys keyCode) {
		SimulateKeypress((uint)keyCode);
	}

	/// <summary>
	/// <para>Averages the pixel colors of a Texture2D and returns it.</para>
	/// </summary>
	/// <param name="tex"></param>
	/// <returns>The average Color32 of the specified texture</returns>
	public static Color32 GetAverageColorOfTexture(Texture2D tex) {
		Color32[] texColors = tex.GetPixels32();

		int total = 0;

		float r = 0;
		float g = 0;
		float b = 0;

		for (int i = 0; i < texColors.Length; i++) {
			if (texColors[i].a > 0) {
				total++;
				r += texColors[i].r;
				g += texColors[i].g;
				b += texColors[i].b;
			}
		}

		return new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), 0);
	}

	private const UInt32 SPI_GETDESKWALLPAPER = 0x73;
	private const int MAX_PATH = 260;

	/// <summary>
	/// <para>Gets the current user's desktop wallpaper as a Texture2D.</para>
	/// </summary>
	/// <returns></returns>
	public static Texture2D GetWallpaperImage() {
		RegistryKey currentMachine = Registry.CurrentUser;
		RegistryKey controlPanel = currentMachine.OpenSubKey("Control Panel");
		RegistryKey desktop = controlPanel.OpenSubKey("Desktop");

		string filePath = Convert.ToString(desktop.GetValue("WallPaper"));

		controlPanel.Close();

		Texture2D tex = null;
		byte[] fileData;
		if (!System.IO.File.Exists(filePath)) {
			//Failed to retrieve wallpaper image by wallpaper registry entry, trying another method...
			filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Themes\\CachedFiles";
			if (Directory.Exists(filePath)) {
				string[] filePaths = Directory.GetFiles(filePath);
				if (filePaths.Length > 0) {
					filePath = filePaths[0];
				}
			} else {
				//Failed to retrieve wallpaper image by cached file, trying another method...
				RegistryKey regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Internet Explorer\\Desktop\\General\\", false);
				filePath = regKey.GetValue("WallpaperSource").ToString() + "h";
				if (!System.IO.File.Exists(filePath)) {
					//Failed to retrieve wallpaper image by grabbing its original source, trying another method...
					filePath = new String('\0', MAX_PATH);
					WinAPI.SystemParametersInfo(SPI_GETDESKWALLPAPER, (UInt32)filePath.Length, filePath, 0);
					filePath = filePath.Substring(0, filePath.IndexOf('\0'));
				}
			}
		}

		if (System.IO.File.Exists(filePath)) {
			fileData = System.IO.File.ReadAllBytes(filePath);
			tex = new Texture2D(2, 2);
			tex.LoadImage(fileData);
		} else {
			UnityEngine.Debug.LogError("Failed to retrieve wallpaper image using all methods!");
		}

		return tex;
	}

	/// <summary>
	/// <para>Adds the game/application/program to the user's startup folder.</para>
	/// </summary>
	public static void AddToStartup() {
		if (!Application.isEditor) {
			string[] parts = Application.dataPath.Split('/');
			string exeName = parts[parts.Length - 1].Replace("_Data", "") + ".exe";
			WshShell shell = new WshShell();
			IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\RainityApplication.lnk");
			shortcut.Description = "Startup shortcut for a Rainity application.";
			shortcut.TargetPath = Application.dataPath + "/../" + exeName;
			shortcut.Save();
		} else {
			UnityEngine.Debug.Log("Adding the program to startup only works in standalone builds.");
		}
	}

	/// <summary>
	/// <para>Removes the game/application/program from the user's startup folder.</para>
	/// </summary>
	public static void RemoveFromStartup() {
		if (!Application.isEditor) {
			System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\RainityApplication.lnk");
		} else {
			UnityEngine.Debug.Log("Removing the program from startup only works in standalone builds.");
		}
	}

	/// <summary>
	/// <para>Gets the current user's Windows username.</para>
	/// </summary>
	/// <returns></returns>
	public static string GetUserName() {
		return GetUserName(false);
	}

	/// <summary>
	/// <para>Gets the current network user's Windows username with domain included.</para>
	/// </summary>
	/// <param name="includeDomain"></param>
	/// <returns></returns>
	public static string GetUserName(bool includeDomain) {
		return includeDomain ? System.Security.Principal.WindowsIdentity.GetCurrent().Name : Environment.UserName;
	}

	/// <summary>
	/// <para>Gets the current user's Windows avatar image as a Texture2D.</para>
	/// </summary>
	/// <returns></returns>
	public static Texture2D GetUserAvatar() {
		String username = GetUserName();
		UnityEngine.Debug.Log("[GetUserAvatar] Username: " + username);

		byte[] buf = new byte[2048];
		bool pathFound = GetAvatarPath(username, buf);
		UnityEngine.Debug.Log("[GetUserAvatar] GetAvatarPath returned: " + pathFound);

		if (!pathFound) {
			UnityEngine.Debug.LogWarning("[GetUserAvatar] GetAvatarPath returned false — no avatar file found for user.");
			return null;
		}

		// Strip null bytes that follow the path string in the buffer
		//string filePath = System.Text.Encoding.ASCII.GetString(buf);
		string filePath = Encoding.ASCII.GetString(buf).TrimEnd('\0');
		int nullIndex = filePath.IndexOf('\0');
		if (nullIndex >= 0) filePath = filePath.Substring(0, nullIndex);
		filePath = filePath.Trim();
		UnityEngine.Debug.Log("[GetUserAvatar] Avatar file path: '" + filePath + "'");

		if (!System.IO.File.Exists(filePath)) {
			UnityEngine.Debug.LogWarning("[GetUserAvatar] File does not exist at path: " + filePath);
			return null;
		}

		try {
			// Convert to PNG in temp folder so Unity can load it
			string tempPath = Path.GetTempPath() + "UserAvatar.png";
			Bitmap bmp = new Bitmap(filePath);
			bmp.Save(tempPath, ImageFormat.Png);
			bmp.Dispose();
			UnityEngine.Debug.Log("[GetUserAvatar] Saved converted PNG to: " + tempPath);

			byte[] fileData = System.IO.File.ReadAllBytes(tempPath);
			Texture2D tex = new Texture2D(2, 2);
			tex.LoadImage(fileData);
			UnityEngine.Debug.Log("[GetUserAvatar] Texture loaded successfully (" + tex.width + "x" + tex.height + ")");
			return tex;
		} catch (Exception e) {
			UnityEngine.Debug.LogError("[GetUserAvatar] Exception loading avatar: " + e.Message);
			return null;
		}
	}

	//Currently causes random editor crashes, but works in build
	private static List<SystemTray> trays = new List<SystemTray>();
	/// <summary>
	/// <para>Creates a system tray icon in the task bar.  The returned SystemTray object can be used to add context menus.</para>
	/// </summary>
	/// <returns>The newly created SystemTray object</returns>
	public static SystemTray CreateSystemTrayIcon() {
		if (!Application.isEditor) {
			trays.Add(new SystemTray());
			return trays[trays.Count - 1];
		}
		return null;
	}

	/// <summary>
	/// <para>Opens the specified file or directory using the default program associated with the file's file extension.</para>
	/// </summary>
	/// <param name="path"></param>
	public static void OpenFile(string path) {
		OpenFile(path, "");
	}

	/// <summary>
	/// <para>Opens the specified file or directory using the default program associated with the file's file extension and includes command-line arguments.</para>
	/// </summary>
	/// <param name="path"></param>
	/// <param name="arguments"></param>
	public static void OpenFile(string path, string arguments) {
		Process proc = new Process();
		proc.StartInfo.FileName = path;
		proc.StartInfo.Arguments = arguments;
		proc.Start();
	}

	/// <summary>
	/// <para>Gets information about the weather in your current location.</para>
	/// </summary>
	/// <returns>A WeatherObject class that contains information about the weather.</returns>
	[Obsolete("This method is deprecated due to Yahoo Weather API being shut down.")]
	public static WeatherObject GetWeatherInformation() {
		WebClient wc = new System.Net.WebClient();
		byte[] raw = wc.DownloadData("http://ipinfo.io/postal");
		string zipCode = Encoding.UTF8.GetString(raw);

		string urlP0 = "https://query.yahooapis.com/v1/public/yql?q=";
		string urlP1 = "select%20%2A%20from%20weather.forecast%20where%20woeid%20in%20%28select%20woeid%20from%20geo.places%281%29%20where%20text%3D%22";
		string urlP2 = "%22%29&format=json&env=store%3A%2F%2Fdatatables%2Eorg%2Falltableswithkeys&callback=";

		string urlWeather = urlP0 + urlP1 + zipCode + urlP2;

		ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
		var json = new WebClient().DownloadString(urlWeather);

		WeatherObject obj = JsonUtility.FromJson<WeatherObject>(json);

		return obj;
	}

	/// <summary>
	/// <para>Gets a list of files in the specified directory and outputs their info.</para>
	/// </summary>
	/// <param name="directory"></param>
	/// <returns>An array of RainityFile objects</returns>
	public static RainityFile[] GetFiles(string directory) {
		if (Directory.Exists(directory)) {
			List<RainityFile> files = new List<RainityFile>();
			foreach (string fileName in Directory.GetFiles(directory)) {
				RainityFile file;
				file.filePath = fileName;
				file.fileName = Path.GetFileName(fileName);
				file.fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
				file.extension = Path.GetExtension(fileName);
				file.isDirectory = false;
				files.Add(file);
			}

			foreach (string fileName in Directory.GetDirectories(directory)) {
				RainityFile file;
				file.filePath = fileName;
				file.fileName = Path.GetDirectoryName(fileName);
				file.fileNameWithoutExtension = Path.GetDirectoryName(fileName);
				file.extension = "";
				file.isDirectory = true;
				files.Add(file);
			}

			return files.ToArray();
		} else {
			UnityEngine.Debug.LogError("Error with Rainity.GetFiles: Directory doesn't exist!");
		}

		return null;
	}









	IEnumerator WaitAndApply() {
		yield return new WaitForSeconds(0.5f);
		SetupDesktop.AddWinProc();
	}

	public void Update() {
		if (timer <= 0) {
			timer = 1f;
			cpuUsage = GetCPUPercentPDH();
		}
		timer -= Time.deltaTime;
	}

	public void LateUpdate() {
		RainityInput.Update();
	}

	private void OnApplicationQuit() {
		SetupDesktop.appQuitting = true;
		SetupDesktop.UnhookHook();
		foreach (SystemTray tray in trays) {
			tray.Dispose();
		}
	}
}

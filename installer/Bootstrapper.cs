using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("UzbekKeys")]
[assembly: AssemblyCompany("Shohruh04")]
[assembly: AssemblyProduct("UzbekKeys")]
[assembly: AssemblyVersion("1.0.1.0")]
[assembly: AssemblyFileVersion("1.0.1.0")]

internal static class Bootstrapper
{
    internal const string LayoutId = "A0010443";
    internal const string Profile = "0x0443:0xA0010443";
    internal const string RegistryPath = @"SYSTEM\CurrentControlSet\Control\Keyboard Layouts";
    private const string Title = "UzbekKeys";

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemInfo
    {
        internal ushort Architecture, Reserved;
        internal uint PageSize;
        internal IntPtr MinAddress, MaxAddress, ProcessorMask;
        internal uint ProcessorCount, ProcessorType, AllocationGranularity;
        internal ushort Level, Revision;
    }
    [DllImport("kernel32.dll")] private static extern void GetNativeSystemInfo(out SystemInfo info);
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("input.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InstallLayoutOrTip(string profiles, uint flags);
    [DllImport("user32.dll")]
    private static extern int GetKeyboardLayoutList(int count, [Out] IntPtr[] layouts);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnloadKeyboardLayout(IntPtr hkl);
    [DllImport("msi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint MsiEnumRelatedProductsW(string upgradeCode, uint reserved, uint index, StringBuilder productCode);

    internal static string GetArchitecture()
    {
        // The x86 bootstrapper can be emulated on ARM64. GetNativeSystemInfo
        // and legacy .NET Is64BitOperatingSystem can then misidentify the OS.
        try
        {
            ushort processMachine, nativeMachine;
            if (!IsWow64Process2(GetCurrentProcess(), out processMachine, out nativeMachine))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return ArchitectureFromMachine(nativeMachine);
        }
        catch (EntryPointNotFoundException) { } // Early Windows 10 x86/x64 only.
        SystemInfo info; GetNativeSystemInfo(out info);
        switch (info.Architecture)
        {
            case 0: return "x86";
            case 9: return "x64";
            case 12: return "arm64";
            default: throw new NotSupportedException("Bu protsessor arxitekturasi qöllab-quvvatlanmaydi.");
        }
    }

    internal static string ArchitectureFromMachine(ushort machine)
    {
        switch (machine)
        {
            case 0x014c: return "x86";
            case 0x8664: return "x64";
            case 0xaa64: return "arm64";
            default: throw new NotSupportedException("Bu protsessor arxitekturasi qöllab-quvvatlanmaydi.");
        }
    }

    internal static string GetUpgradeCode(string architecture)
    {
        switch (architecture)
        {
            case "x86": return "{4F0FD64D-CE6D-4681-B5AA-4C28C61D8971}";
            case "x64": return "{D4E781D3-6C5A-4191-8386-B01FCCB2E0A1}";
            case "arm64": return "{5D4ACD27-6BB3-472D-85D2-F865908CE932}";
            default: throw new ArgumentException("Unknown architecture");
        }
    }

    internal static string[] GetInstalledProducts(string architecture)
    {
        var products = new List<string>();
        for (uint index = 0; ; index++)
        {
            var product = new StringBuilder(39);
            uint result = MsiEnumRelatedProductsW(GetUpgradeCode(architecture), 0, index, product);
            if (result == 259) return products.ToArray(); // ERROR_NO_MORE_ITEMS.
            if (result != 0) throw new Win32Exception((int)result);
            products.Add(product.ToString());
        }
    }

    internal static bool UnloadProductLayouts(IntPtr[] layouts, Func<IntPtr, bool> unload)
    {
        bool success = true;
        foreach (IntPtr layout in layouts)
        {
            // Layout Id 0f43 + language 0443 produces FF430443. HKLs can be
            // sign-extended on x64. Never unload other Uzbek or default layouts.
            if (unchecked((uint)layout.ToInt64()) == 0xff430443u && !unload(layout)) success = false;
        }
        return success;
    }

    // Only this product's exact profile is enabled/disabled. No default-profile flag.
    private static bool ChangeProfile(bool enable)
    {
        try
        {
            bool changed = InstallLayoutOrTip(Profile, enable ? 0u : 1u);
            if (enable) return changed;
            // Remove saved profile first; a preloaded/in-use HKL may resist unload.
            // Enumerate the existing handle: loading by KLID after MSI removed its
            // registration fails or falls back to a different keyboard layout.
            int count = GetKeyboardLayoutList(0, null);
            IntPtr[] layouts = new IntPtr[count];
            int copied = GetKeyboardLayoutList(count, layouts);
            if (copied == 0) return false;
            Array.Resize(ref layouts, copied);
            bool unloaded = UnloadProductLayouts(layouts, UnloadKeyboardLayout);
            return changed && unloaded;
        }
        catch (Exception) { return false; }
    }

    private static void CheckSystemAndCollisions()
    {
        using (RegistryKey machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
        using (RegistryKey version = machine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
        {
            int build;
            if (version == null || !Int32.TryParse(Convert.ToString(version.GetValue("CurrentBuildNumber")), out build) || build < 10240)
                throw new NotSupportedException("Windows 10 yoki Windows 11 kerak.");
        }
        using (RegistryKey layouts = Registry.LocalMachine.OpenSubKey(RegistryPath))
        {
            if (layouts == null) throw new InvalidOperationException("Windows klaviatura röyxati topilmadi.");
            foreach (string id in layouts.GetSubKeyNames())
            using (RegistryKey key = layouts.OpenSubKey(id))
            {
                string file = Convert.ToString(key.GetValue("Layout File"));
                string specialId = Convert.ToString(key.GetValue("Layout Id"));
                if ((id.Equals(LayoutId, StringComparison.OrdinalIgnoreCase) || specialId.Equals("0f43", StringComparison.OrdinalIgnoreCase))
                    && !(id.Equals(LayoutId, StringComparison.OrdinalIgnoreCase) && file.Equals("uzbkeys.dll", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Klaviatura identifikatori band. Mavjud klaviatura özgartirilmadi.");
            }
        }
    }

    private static string ExtractPackage(string architecture, string directory)
    {
        Directory.CreateDirectory(directory);
        string file = Path.Combine(directory, "UzbekKeys-" + architecture + ".msi");
        using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("UzbekKeys." + architecture + ".msi"))
        {
            if (input == null) throw new FileNotFoundException("Örnatiş paketi topilmadi.");
            using (FileStream output = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None)) input.CopyTo(output);
        }
        return file;
    }

    internal static ProcessStartInfo CreateInstallerStartInfo(string arguments)
    {
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        // Sysnative is a WOW64-only virtual alias, not a physical path usable
        // by the elevated native shell. Let Windows Installer dispatch normally.
        string system = Path.Combine(windows, "System32");
        ProcessStartInfo start = new ProcessStartInfo(Path.Combine(system, "msiexec.exe"), arguments);
        // System-wide layout files and HKLM registration require an administrator.
        // Elevate only Windows Installer, preserving this user's profile identity.
        start.UseShellExecute = true;
        start.Verb = "runas";
        return start;
    }

    private static int RunInstaller(string arguments)
    {
        try
        {
            using (Process process = Process.Start(CreateInstallerStartInfo(arguments)))
            { process.WaitForExit(); return process.ExitCode; }
        }
        catch (Win32Exception error)
        {
            if (error.NativeErrorCode == 1223) return 1602; // UAC canceled.
            throw;
        }
    }

    private static bool Success(int code) { return code == 0 || code == 3010 || code == 1641; }
    private static void Inform(string text, MessageBoxIcon icon)
    {
        MessageBox.Show(text, Title, MessageBoxButtons.OK, icon);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        try
        {
            string action = args.Length > 0 ? args[0] : "";
            if (action == "/enable-profile") return ChangeProfile(true) ? 0 : 1;
            if (action == "/disable-profile") return ChangeProfile(false) ? 0 : 1;
            if (action == "/detect") { Console.WriteLine(GetArchitecture()); return 0; }
            if (action == "/extract")
            {
                if (args.Length != 2) throw new ArgumentException("/extract DIRECTORY");
                foreach (string arch in new string[] { "x86", "x64", "arm64" }) ExtractPackage(arch, args[1]);
                return 0;
            }
            string architecture = GetArchitecture();
            bool uninstall = action == "/uninstall" || action == "/uninstall-worker"
                || Path.GetFileNameWithoutExtension(Application.ExecutablePath).StartsWith("Uninstall", StringComparison.OrdinalIgnoreCase);
            if (uninstall)
            {
                if (action != "/uninstall-worker")
                {
                    if (MessageBox.Show("UzbekKeys klaviaturasini öçirasizmi?", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return 1602;
                    // Run a temporary copy so MSI can remove the installed Uninstall.exe.
                    string worker = Path.Combine(Path.GetTempPath(), "UzbekKeys-Uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
                    File.Copy(Application.ExecutablePath, worker);
                    Process.Start(new ProcessStartInfo(worker, "/uninstall-worker") { UseShellExecute = true });
                    return 0;
                }
                // MSI performs profile cleanup after successful removal. Repeat afterward
                // for the initiating user when elevation used a different admin account.
                int code = 0;
                // Enumerate before removal so old and current versions can both be
                // removed without assuming the downloaded uninstaller's version.
                foreach (string product in GetInstalledProducts(architecture))
                {
                    code = RunInstaller("/x " + product + " /qb /norestart");
                    if (code == 1602) return code;
                    if (!Success(code) && code != 1605) break;
                }
                if (Success(code) || code == 1605)
                {
                    bool cleaned = ChangeProfile(false);
                    Inform(cleaned
                        ? "UzbekKeys öçirildi."
                        : "UzbekKeys fayllari öçirildi. Windows klaviatura profilini töliq yangilay olmadi.\n\nWindows hisobidan çiqib qayta kiring. Tartib qolsa, Language options orqali olib taşlang.",
                        cleaned ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
                else Inform("Öçiriş tugamadi. Windows Installer kodi: " + code + ". Klaviatura saqlangan bölişi mumkin.", MessageBoxIcon.Warning);
                return code;
            }

            CheckSystemAndCollisions();
            if (MessageBox.Show("UzbekKeys klaviaturasi örnatilsinmi?\n\nArxitektura: " + architecture + "\nBoşqa klaviaturalar saqlanadi.", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return 1602;
            string extraction = Path.Combine(Path.GetTempPath(), "UzbekKeys-" + Guid.NewGuid().ToString("N"));
            try
            {
                string package = ExtractPackage(architecture, extraction);
                int code = RunInstaller("/i \"" + package + "\" /qb /norestart");
                if (code == 1602) return code;
                if (Success(code))
                {
                    bool enabled = ChangeProfile(true);
                    Inform(enabled
                        ? "UzbekKeys örnatildi. Win + Space orqali tanlang.\n\nKörinmasa, Windows’dan çiqib qayta kiring."
                        : "UzbekKeys örnatildi. Settings → Time & language → Language & region orqali UzbekKeys klaviaturasini qöşing.", MessageBoxIcon.Information);
                }
                else Inform("Örnatiş tugamadi. Windows Installer kodi: " + code, MessageBoxIcon.Warning);
                return code;
            }
            finally { try { Directory.Delete(extraction, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }
        catch (Exception error) { Inform(error.Message, MessageBoxIcon.Error); return 1; }
    }
}

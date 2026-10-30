using System;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("UzbekKeys")]
[assembly: AssemblyCompany("Shohruh04")]
[assembly: AssemblyProduct("UzbekKeys")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

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
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadKeyboardLayout(string id, uint flags);
    [DllImport("user32.dll")] private static extern bool UnloadKeyboardLayout(IntPtr hkl);

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

    internal static string GetProductCode(string architecture)
    {
        switch (architecture)
        {
            case "x86": return "{0325C442-84EF-42EC-A531-BE53A1B3CD31}";
            case "x64": return "{D96A670B-71C2-4C92-827B-640E2C825C53}";
            case "arm64": return "{C65B72F6-06AA-43F1-9F8E-0D114E6EE842}";
            default: throw new ArgumentException("Unknown architecture");
        }
    }

    // Only this product's exact profile is enabled/disabled. No default-profile flag.
    private static bool ChangeProfile(bool enable)
    {
        try
        {
            if (!enable)
            {
                // Load without activating, then unload this layout from this process.
                IntPtr hkl = LoadKeyboardLayout(LayoutId, 0x80);
                if (hkl != IntPtr.Zero) UnloadKeyboardLayout(hkl);
            }
            return InstallLayoutOrTip(Profile, enable ? 0u : 1u);
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
                int code = RunInstaller("/x " + GetProductCode(architecture) + " /qb /norestart");
                if (code == 1602) return code;
                if (Success(code) || code == 1605)
                {
                    ChangeProfile(false);
                    Inform("UzbekKeys öçirildi. Zarur bölsa, Windows’dan çiqib qayta kiring.", MessageBoxIcon.Information);
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

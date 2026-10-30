using System;

internal static class InstallerTests
{
    private static int checks;
    private static void Equal(string expected, string actual)
    {
        if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual);
        checks++;
    }
    private static int Main()
    {
        Equal("x86", Bootstrapper.ArchitectureFromMachine(0x014c));
        Equal("x64", Bootstrapper.ArchitectureFromMachine(0x8664));
        Equal("arm64", Bootstrapper.ArchitectureFromMachine(0xaa64));
        try { Bootstrapper.ArchitectureFromMachine(0x01c4); throw new Exception("Unsupported ARM32 accepted"); }
        catch (NotSupportedException) { checks++; }
        Equal("{0325C442-84EF-42EC-A531-BE53A1B3CD31}", Bootstrapper.GetProductCode("x86"));
        Equal("{D96A670B-71C2-4C92-827B-640E2C825C53}", Bootstrapper.GetProductCode("x64"));
        Equal("{C65B72F6-06AA-43F1-9F8E-0D114E6EE842}", Bootstrapper.GetProductCode("arm64"));
        string host = Bootstrapper.GetArchitecture();
        if (host != "x86" && host != "x64" && host != "arm64") throw new Exception("Unknown host architecture");
        var start = Bootstrapper.CreateInstallerStartInfo("/x {D96A670B-71C2-4C92-827B-640E2C825C53} /qb /norestart");
        Equal("runas", start.Verb);
        Equal(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "msiexec.exe"), start.FileName);
        if (!System.IO.File.Exists(start.FileName)) throw new Exception("Windows Installer path missing");
        checks++;
        if (!start.UseShellExecute) throw new Exception("Per-machine MSI must request administrator elevation");
        checks++;
        Console.WriteLine("PASS installer: " + checks + " architecture/product checks; detected host " + host);
        return 0;
    }
}

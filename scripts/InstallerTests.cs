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
        Equal("{4F0FD64D-CE6D-4681-B5AA-4C28C61D8971}", Bootstrapper.GetUpgradeCode("x86"));
        Equal("{D4E781D3-6C5A-4191-8386-B01FCCB2E0A1}", Bootstrapper.GetUpgradeCode("x64"));
        Equal("{5D4ACD27-6BB3-472D-85D2-F865908CE932}", Bootstrapper.GetUpgradeCode("arm64"));
        string host = Bootstrapper.GetArchitecture();
        if (host != "x86" && host != "x64" && host != "arm64") throw new Exception("Unknown host architecture");
        string[] products = Bootstrapper.GetInstalledProducts(host);
        foreach (string product in products) { Guid.Parse(product); }
        checks++;
        var start = Bootstrapper.CreateInstallerStartInfo("/x {D96A670B-71C2-4C92-827B-640E2C825C53} /qb /norestart");
        Equal("runas", start.Verb);
        Equal(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "msiexec.exe"), start.FileName);
        if (!System.IO.File.Exists(start.FileName)) throw new Exception("Windows Installer path missing");
        checks++;
        if (!start.UseShellExecute) throw new Exception("Per-machine MSI must request administrator elevation");
        checks++;
        // Regression: cleanup must never target US, Russian, built-in Uzbek,
        // another custom Uzbek layout, null, or a different-language handle.
        IntPtr[] unrelated = { new IntPtr(0x04090409), new IntPtr(0x04190419),
            new IntPtr(0x04430443), new IntPtr(unchecked((int)0xf0010443u)),
            IntPtr.Zero, new IntPtr(unchecked((int)0xff430409u)) };
        Func<IntPtr, bool> unexpected = delegate { throw new Exception("Unrelated keyboard was unloaded"); };
        if (!Bootstrapper.UnloadProductLayouts(unrelated, unexpected)) throw new Exception("No-target cleanup failed");
        checks++;
        IntPtr target = new IntPtr(unchecked((int)0xff430443u));
        var mixed = new System.Collections.Generic.List<IntPtr>(unrelated); mixed.Add(target);
        int calls = 0;
        if (!Bootstrapper.UnloadProductLayouts(mixed.ToArray(), delegate(IntPtr layout) {
            if (layout != target) throw new Exception("Wrong cleanup target"); calls++; return true;
        }) || calls != 1) throw new Exception("Existing UzbekKeys HKL was not unloaded exactly once");
        checks++;
        if (Bootstrapper.UnloadProductLayouts(new[] { target }, delegate { return false; }))
            throw new Exception("In-use layout failure was hidden");
        checks++;
        if (IntPtr.Size == 8)
        {
            if (!Bootstrapper.UnloadProductLayouts(new[] { new IntPtr(0xff430443L) }, delegate { calls++; return true; }) || calls != 2)
                throw new Exception("Zero-extended x64 handle not recognized");
            checks++;
        }
        Console.WriteLine("PASS installer: " + checks + " architecture/product/cleanup checks; detected host " + host + "; related installed products: " + products.Length);
        return 0;
    }
}

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static class TestNativeInput
{
    [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    private static extern IntPtr LoadKeyboardLayout(string id, uint flags);
    [DllImport("user32.dll")] private static extern bool UnloadKeyboardLayout(IntPtr hkl);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)]
    private static extern int ToUnicodeEx(uint vk, uint scan, byte[] state, StringBuilder output, int capacity, uint flags, IntPtr hkl);
    private static int Main()
    {
        IntPtr layout=LoadKeyboardLayout("A0010443", 0x80);
        if (layout==IntPtr.Zero) throw new Exception("Installed UzbekKeys HKL unavailable");
        int checks=0;
        try
        {
            uint[] keys={0xdb,0xdd,0xbd,0xbb};
            uint[] scans={0x1a,0x1b,0x0c,0x0d};
            string[] characters={"çÇ[{","öÖ]}","ğĞ-_","şŞ=+"};
            for(int k=0;k<keys.Length;k++)
            for(int caps=0;caps<2;caps++)
            for(int shift=0;shift<2;shift++)
            for(int altgr=0;altgr<2;altgr++)
            {
                var state=new byte[256];
                state[0x14]=(byte)caps;
                if(shift!=0) state[0x10]=0x80;
                if(altgr!=0) state[0x11]=state[0x12]=state[0xa5]=0x80;
                var text=new StringBuilder(8);
                int count=ToUnicodeEx(keys[k],scans[k],state,text,8,4,layout);
                char expected=characters[k][altgr!=0?2+shift:(caps^shift)];
                if(count!=1 || text[0]!=expected) throw new Exception("Wrong character from installed native layout: "+keys[k]);
                checks++;
            }
            Console.WriteLine("PASS installed native input: "+(IntPtr.Size*8)+"-bit process, "+checks+" ToUnicodeEx checks");
            return 0;
        }
        finally { UnloadKeyboardLayout(layout); }
    }
}

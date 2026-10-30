"""Check native PE tables on every architecture; optionally test installed Windows HKL.

No third-party packages. Static table checks are not a substitute for real app QA.
"""
from __future__ import annotations

import argparse
import ctypes
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SPECIAL = {
    0xDB: (0x1A, "ç", "Ç", "[", "{"),
    0xDD: (0x1B, "ö", "Ö", "]", "}"),
    0xBD: (0x0C, "ğ", "Ğ", "-", "_"),
    0xBB: (0x0D, "ş", "Ş", "=", "+"),
}


class PE:
    def __init__(self, path: Path):
        self.data = path.read_bytes()
        self.pe = self.u32(0x3C)
        assert self.data[self.pe:self.pe + 4] == b"PE\0\0", path
        self.machine, count = struct.unpack_from("<HH", self.data, self.pe + 4)
        optional_size = self.u16(self.pe + 20)
        optional = self.pe + 24
        self.is64 = self.u16(optional) == 0x20B
        self.base = self.u64(optional + 24) if self.is64 else self.u32(optional + 28)
        self.directories = optional + (112 if self.is64 else 96)
        self.sections = []
        for i in range(count):
            offset = optional + optional_size + i * 40
            size, rva, raw_size, raw = struct.unpack_from("<IIII", self.data, offset + 8)
            self.sections.append((rva, max(size, raw_size), raw))

    def u16(self, offset): return struct.unpack_from("<H", self.data, offset)[0]
    def u32(self, offset): return struct.unpack_from("<I", self.data, offset)[0]
    def u64(self, offset): return struct.unpack_from("<Q", self.data, offset)[0]
    def rva(self, address):
        for start, size, raw in self.sections:
            if start <= address < start + size:
                return raw + address - start
        raise AssertionError("Unmapped RVA: " + hex(address))
    def va(self, address): return self.rva(address - self.base)
    def ptr(self, offset, width): return self.u64(offset) if width == 8 else self.u32(offset)
    def string(self, rva):
        start = self.rva(rva)
        return self.data[start:self.data.index(b"\0", start)].decode("ascii")

    def export(self, name):
        directory = self.rva(self.u32(self.directories))
        count = self.u32(directory + 24)
        functions = self.rva(self.u32(directory + 28))
        names = self.rva(self.u32(directory + 32))
        ordinals = self.rva(self.u32(directory + 36))
        for i in range(count):
            if self.string(self.u32(names + i * 4)) == name:
                return self.u32(functions + self.u16(ordinals + i * 2) * 4)
        raise AssertionError("Missing export: " + name)

    def tables(self, width):
        function_rva = self.export("KbdLayerDescriptor")
        code = self.rva(function_rva)
        if self.machine == 0x14C:
            assert self.data[code] == 0xB8, "Expected x86 MOV EAX, table"
            table_address = self.u32(code + 1)
        elif self.machine == 0x8664:
            assert self.data[code:code + 3] == b"\x48\x8d\x05", "Expected x64 LEA RAX, table"
            table_address = self.base + function_rva + 7 + struct.unpack_from("<i", self.data, code + 3)[0]
        else:
            assert self.machine == 0xAA64
            adrp, add = struct.unpack_from("<II", self.data, code)
            assert adrp & 0x9F00001F == 0x90000000, "Expected ARM64 ADRP X0, page"
            assert add & 0xFFC003FF == 0x91000000, "Expected ARM64 ADD X0, X0, offset"
            immediate = ((adrp >> 5) & 0x7FFFF) << 2 | ((adrp >> 29) & 3)
            if immediate & (1 << 20): immediate -= 1 << 21
            table_address = ((self.base + function_rva) & ~0xFFF) + (immediate << 12) + ((add >> 10) & 0xFFF)
        table = self.va(table_address)
        modifiers = self.va(self.ptr(table, width))
        max_state = self.u16(modifiers + width)
        states = list(self.data[modifiers + width + 2:modifiers + width + 3 + max_state])
        entry = self.va(self.ptr(table + width, width))
        rows = {}
        while self.ptr(entry, width):
            row = self.va(self.ptr(entry, width))
            columns, stride = self.data[entry + width:entry + width + 2]
            assert stride == 2 + 2 * columns
            while self.data[row]:
                vk, caps = self.data[row:row + 2]
                values = struct.unpack_from("<" + "H" * columns, self.data, row + 2)
                rows[vk] = (caps, values)
                row += stride
            entry += 16 if width == 8 else 8
        # Validate keyboard ABI pointers and scan-code identities as well as characters.
        assert self.ptr(table + 2 * width, width) == 0, "Unexpected dead-key table"
        assert self.ptr(table + 5 * width, width) == 0, "Unexpected dead-key names"
        scan_codes = self.va(self.ptr(table + 6 * width, width))
        for vk, (scan, *_rest) in SPECIAL.items():
            assert self.u16(scan_codes + scan * 2) & 0xFF == vk
        assert self.data[table + 7 * width] >= 0x57
        return states, rows


def translated(states, rows, vk, state=0, caps=False):
    attribute, values = rows.get(vk, (0, ()))
    # CAPLOK toggles base/Shift; CAPLOKALTGR would separately toggle AltGr.
    if caps and ((state & 6 == 0 and attribute & 1) or (state & 6 == 6 and attribute & 4)):
        state ^= 1
    column = states[state] if state < len(states) else 0xF
    value = values[column] if column < len(values) else 0xF000
    return None if value == 0xF000 else chr(value)


def check_tables(architecture, width):
    path = ROOT / "build" / architecture / "uzbkeys.dll"
    image = PE(path)
    assert image.machine == {"x86": 0x14C, "x64": 0x8664, "arm64": 0xAA64, "wow64": 0x14C}[architecture]
    # No CRT or other runtime imports are required by the layout DLL.
    assert image.u32(image.directories + 8) == 0, "Unexpected runtime imports"
    states, rows = image.tables(width)
    checks = 0
    for vk, (_scan, lower, upper, plain, shifted) in SPECIAL.items():
        assert rows[vk][0] == 1, "CapsLock must not alter AltGr punctuation"
        for caps in (False, True):
            for shift in (0, 1):
                assert translated(states, rows, vk, shift, caps) == (upper if shift ^ caps else lower)
                assert translated(states, rows, vk, 6 + shift, caps) == (shifted if shift else plain)
                checks += 2
    for state in (6, 7):
        for caps in (False, True):
            assert translated(states, rows, 0xDE, state, caps) == "\u02bc"
            checks += 1
    assert translated(states, rows, 0xDE) == "'"
    assert translated(states, rows, 0xDE, 1) == '"'
    # Compare every ordinary printable key AND Ctrl combinations to the system US layout.
    if hasattr(ctypes, "WinDLL"):
        user32 = ctypes.WinDLL("user32", use_last_error=True)
        user32.LoadKeyboardLayoutW.restype = ctypes.c_void_p
        user32.MapVirtualKeyExW.argtypes = [ctypes.c_uint, ctypes.c_uint, ctypes.c_void_p]
        user32.ToUnicodeEx.argtypes = [ctypes.c_uint, ctypes.c_uint, ctypes.POINTER(ctypes.c_ubyte), ctypes.c_wchar_p, ctypes.c_int, ctypes.c_uint, ctypes.c_void_p]
        us = user32.LoadKeyboardLayoutW("00000409", 0x80)
        assert us, "Cannot load system US layout for comparison"
        for vk in rows:
            if vk in (0x6E,): continue  # Numpad decimal depends on NumLock/locale.
            scan = user32.MapVirtualKeyExW(vk, 0, us)
            for state in (0, 1, 2, 3):
                if vk in SPECIAL and state in (0, 1): continue
                key_state = (ctypes.c_ubyte * 256)()
                if state & 1: key_state[0x10] = 0x80
                if state & 2: key_state[0x11] = 0x80
                output = ctypes.create_unicode_buffer(8)
                count = user32.ToUnicodeEx(vk, scan, key_state, output, 8, 4, us)
                expected = "".join(output[:count]) if count > 0 else None
                assert translated(states, rows, vk, state) == expected, (architecture, hex(vk), state, repr(expected), repr(translated(states, rows, vk, state)))
                checks += 1
    print(f"PASS {architecture}: PE machine {image.machine:#06x}; {checks} mapping/ABI checks")
    return states, rows


def check_installed():
    import winreg
    with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\A0010443") as key:
        assert winreg.QueryValueEx(key, "Layout File")[0].lower() == "uzbkeys.dll"
    user32 = ctypes.WinDLL("user32", use_last_error=True)
    user32.LoadKeyboardLayoutW.restype = ctypes.c_void_p
    user32.ToUnicodeEx.argtypes = [ctypes.c_uint, ctypes.c_uint, ctypes.POINTER(ctypes.c_ubyte), ctypes.c_wchar_p, ctypes.c_int, ctypes.c_uint, ctypes.c_void_p]
    user32.UnloadKeyboardLayout.argtypes = [ctypes.c_void_p]
    layout = user32.LoadKeyboardLayoutW("A0010443", 0x80)
    assert layout, f"LoadKeyboardLayout failed: {ctypes.get_last_error()}"
    checks = 0
    try:
        for vk, (scan, lower, upper, plain, shifted) in SPECIAL.items():
            for caps in (False, True):
                for shift in (False, True):
                    for altgr in (False, True):
                        state = (ctypes.c_ubyte * 256)()
                        if caps: state[0x14] = 1
                        if shift: state[0x10] = 0x80
                        if altgr: state[0x11] = state[0x12] = state[0xA5] = 0x80
                        output = ctypes.create_unicode_buffer(8)
                        count = user32.ToUnicodeEx(vk, scan, state, output, 8, 4, layout)
                        expected = (shifted if shift else plain) if altgr else (upper if shift ^ caps else lower)
                        assert count == 1 and output[0] == expected, (hex(vk), caps, shift, altgr, repr(output.value), repr(expected))
                        checks += 1
        for shift in (False, True):
            state = (ctypes.c_ubyte * 256)()
            state[0x11] = state[0x12] = state[0xA5] = 0x80
            if shift: state[0x10] = 0x80
            output = ctypes.create_unicode_buffer(8)
            assert user32.ToUnicodeEx(0xDE, 0x28, state, output, 8, 4, layout) == 1
            assert ord(output[0]) == 0x02BC
            checks += 1
        print(f"PASS installed HKL {layout:#x}: {checks} real ToUnicodeEx checks")
    finally:
        user32.UnloadKeyboardLayout(layout)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--installed", action="store_true")
    options = parser.parse_args()
    signatures = [check_tables(architecture, width) for architecture, width in (("x86", 4), ("x64", 8), ("wow64", 8), ("arm64", 8))]
    assert all(signature == signatures[0] for signature in signatures), "Architecture mapping divergence"
    if options.installed: check_installed()
    print("PASS: all architecture mappings are identical; no installed/app QA implied by static checks.")

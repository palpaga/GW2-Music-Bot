using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Gw2MusicBot
{
    public static class InputSimulator
    {
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;
        private const uint MAPVK_VK_TO_VSC = 0x00;

        public static void PressKey(ushort vkCode)
        {
            ushort scanCode = (ushort)MapVirtualKey(vkCode, MAPVK_VK_TO_VSC);

            INPUT[] inputs = new INPUT[2];

            // 1. Key pressed down
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wScan = scanCode;
            inputs[0].u.ki.dwFlags = KEYEVENTF_SCANCODE;

            // 2. Key released (Immediately after)
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wScan = scanCode;
            inputs[1].u.ki.dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP;

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        
        // Virtual Key Codes for 0-9
        public const ushort VK_0 = 0x30;
        public const ushort VK_1 = 0x31;
        public const ushort VK_2 = 0x32;
        public const ushort VK_3 = 0x33;
        public const ushort VK_4 = 0x34;
        public const ushort VK_5 = 0x35;
        public const ushort VK_6 = 0x36;
        public const ushort VK_7 = 0x37;
        public const ushort VK_8 = 0x38;
        public const ushort VK_9 = 0x39;

        // Virtual Key Codes for F1-F5
        public const ushort VK_F1 = 0x70;
        public const ushort VK_F2 = 0x71;
        public const ushort VK_F3 = 0x72;
        public const ushort VK_F4 = 0x73;
        public const ushort VK_F5 = 0x74;
    }
}

﻿// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Input;
using QuickLook.NativeMethods;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace QuickLook
{
    internal class GlobalKeyboardHook : IDisposable
    {
        private static GlobalKeyboardHook _instance;

        private User32.KeyboardHookProc _callback;
        private IntPtr _hhook = IntPtr.Zero;
        internal List<Keys> HookedKeys = new List<Keys>();

        protected GlobalKeyboardHook()
        {
            Hook();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Unhook();
        }

        internal event KeyEventHandler KeyDown;
        internal event KeyEventHandler KeyUp;

        ~GlobalKeyboardHook()
        {
            Dispose();
        }

        internal static GlobalKeyboardHook GetInstance()
        {
            return _instance ?? (_instance = new GlobalKeyboardHook());
        }

        private void Hook()
        {
            _callback = HookProc;

            var hInstance = Kernel32.LoadLibrary("user32.dll");
            _hhook = User32.SetWindowsHookEx(User32.WH_KEYBOARD_LL, _callback, hInstance, 0);
        }

        private void Unhook()
        {
            if (_callback == null) return;

            User32.UnhookWindowsHookEx(_hhook);

            _callback = null;
        }

        private int HookProc(int code, int wParam, ref User32.KeyboardHookStruct lParam)
        {
            if (code >= 0)
            {
                var key = (Keys) lParam.vkCode;
                if (HookedKeys.Contains(key))
                {
                    key = AddModifiers(key);

                    var kea = new KeyEventArgs(key);
                    if (wParam == User32.WM_KEYDOWN || wParam == User32.WM_SYSKEYDOWN)
                        KeyDown?.Invoke(this, kea);
                    if (wParam == User32.WM_KEYUP || wParam == User32.WM_SYSKEYUP)
                        KeyUp?.Invoke(this, kea);
                    if (kea.Handled)
                        return 1;
                }
            }
            return User32.CallNextHookEx(_hhook, code, wParam, ref lParam);
        }

        private Keys AddModifiers(Keys key)
        {
            //Ctrl
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) key = key | Keys.Control;

            //Shift
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) key = key | Keys.Shift;

            //Alt
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) key = key | Keys.Alt;

            return key;
        }
    }
}
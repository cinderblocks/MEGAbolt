﻿/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2021, Sjofn, LLC
 * All rights reserved.
 *  
 * Radegast is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.If not, see<https://www.gnu.org/licenses/>.
 */

using Microsoft.Win32;
using MsdnMag;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MEGAbolt.Controls.MsgBoxCheck
{
    public class MessageBox
    {
        private const int WS_VISIBLE = 268435456;
        private const int WS_CHILD = 1073741824;
        private const int WS_TABSTOP = 65536;
        private const int WM_SETFONT = 48;
        private const int WM_GETFONT = 49;
        private const int BS_AUTOCHECKBOX = 3;
        private const int BM_GETCHECK = 240;
        private const int BST_CHECKED = 1;
        protected LocalCbtHook m_cbt;
        protected IntPtr m_hwnd = IntPtr.Zero;
        protected IntPtr m_hwndBtn = IntPtr.Zero;
        protected bool m_bInit = false;
        protected bool m_bCheck = false;
        protected string m_strCheck;

        public MessageBox()
        {
            m_cbt = new LocalCbtHook();
            m_cbt.WindowCreated += WndCreated;
            m_cbt.WindowDestroyed += WndDestroyed;
            m_cbt.WindowActivated += WndActivated;
        }

        public DialogResult Show(
          string strKey,
          string strValue,
          DialogResult dr,
          string strCheck,
          string strText,
          string strTitle,
          MessageBoxButtons buttons,
          MessageBoxIcon icon)
        {
            RegistryKey subKey = Registry.CurrentUser.CreateSubKey(strKey);
            try
            {
                if (Convert.ToBoolean(subKey.GetValue(strValue, false)))
                    return dr;
            }
            catch
            {
            }
            m_strCheck = strCheck;
            m_cbt.Install();
            dr = System.Windows.Forms.MessageBox.Show(strText, strTitle, buttons, icon);
            m_cbt.Uninstall();
            subKey.SetValue(strValue, m_bCheck);
            return dr;
        }

        public DialogResult Show(
          string strKey,
          string strValue,
          DialogResult dr,
          string strCheck,
          string strText,
          string strTitle,
          MessageBoxButtons buttons)
        {
            return Show(strKey, strValue, dr, strCheck, strText, strTitle, buttons, MessageBoxIcon.None);
        }

        public DialogResult Show(
          string strKey,
          string strValue,
          DialogResult dr,
          string strCheck,
          string strText,
          string strTitle)
        {
            return Show(strKey, strValue, dr, strCheck, strText, strTitle, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public DialogResult Show(
          string strKey,
          string strValue,
          DialogResult dr,
          string strCheck,
          string strText)
        {
            return Show(strKey, strValue, dr, strCheck, strText, "", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private void WndCreated(object sender, CbtEventArgs e)
        {
            if (!e.IsDialogWindow)
                return;
            m_bInit = false;
            m_hwnd = e.Handle;
        }

        private void WndDestroyed(object sender, CbtEventArgs e)
        {
            if (!(e.Handle == m_hwnd))
                return;
            m_bInit = false;
            m_hwnd = IntPtr.Zero;
            if (1 == (int)SendMessage(m_hwndBtn, 240, IntPtr.Zero, IntPtr.Zero))
                m_bCheck = true;
        }

        private void WndActivated(object sender, CbtEventArgs e)
        {
            if (m_hwnd != e.Handle || m_bInit)
                return;
            m_bInit = true;
            IntPtr dlgItem1 = GetDlgItem(m_hwnd, ushort.MaxValue);
            IntPtr num = !(dlgItem1 != IntPtr.Zero) ? SendMessage(m_hwnd, 49, IntPtr.Zero, IntPtr.Zero) : SendMessage(dlgItem1, 49, IntPtr.Zero, IntPtr.Zero);
            Font font = Font.FromHfont(num);
            IntPtr dlgItem2 = GetDlgItem(m_hwnd, 20);
            int x;
            if (dlgItem2 != IntPtr.Zero)
            {
                RECT rc = new RECT();
                GetWindowRect(dlgItem2, rc);
                POINT pt = new POINT
                {
                    x = rc.left,
                    y = rc.top
                };
                ScreenToClient(m_hwnd, pt);
                x = pt.x;
            }
            else
                x = (int)font.GetHeight();
            RECT rc1 = new RECT();
            GetClientRect(m_hwnd, rc1);
            int y = rc1.bottom - rc1.top;
            GetWindowRect(m_hwnd, rc1);
            MoveWindow(m_hwnd, rc1.left, rc1.top, rc1.right - rc1.left, rc1.bottom - rc1.top + (int)font.GetHeight() * 2, true);
            m_hwndBtn = CreateWindowEx(0, "button", m_strCheck, 1342242819, x, y, rc1.right - rc1.left - x, (int)font.GetHeight(), m_hwnd, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            SendMessage(m_hwndBtn, 48, num, new IntPtr(1));
        }

        [DllImport("user32.dll")]
        protected static extern void DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        protected static extern IntPtr GetDlgItem(IntPtr hwnd, int id);

        [DllImport("user32.dll")]
        protected static extern int GetWindowRect(IntPtr hwnd, RECT rc);

        [DllImport("user32.dll")]
        protected static extern int GetClientRect(IntPtr hwnd, RECT rc);

        [DllImport("user32.dll")]
        protected static extern void MoveWindow(
          IntPtr hwnd,
          int x,
          int y,
          int nWidth,
          int nHeight,
          bool bRepaint);

        [DllImport("user32.dll")]
        protected static extern int ScreenToClient(IntPtr hwnd, POINT pt);

        [DllImport("user32.dll", EntryPoint = "MessageBox")]
        protected static extern int _MessageBox(IntPtr hwnd, string text, string caption, int options);

        [DllImport("user32.dll")]
        protected static extern IntPtr SendMessage(
          IntPtr hwnd,
          int msg,
          IntPtr wParam,
          IntPtr lParam);

        [DllImport("user32.dll")]
        protected static extern IntPtr CreateWindowEx(
          int dwExStyle,
          string lpClassName,
          string lpWindowName,
          int dwStyle,
          int x,
          int y,
          int nWidth,
          int nHeight,
          IntPtr hWndParent,
          IntPtr hMenu,
          IntPtr hInstance,
          IntPtr lpParam);

        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}

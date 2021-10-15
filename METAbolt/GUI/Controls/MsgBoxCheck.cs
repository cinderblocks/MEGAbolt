﻿/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2021, Sjofn, LLC
 * All rights reserved.
 *  
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
            this.m_cbt = new LocalCbtHook();
            this.m_cbt.WindowCreated += this.WndCreated;
            this.m_cbt.WindowDestroyed += this.WndDestroyed;
            this.m_cbt.WindowActivated += this.WndActivated;
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
                if (Convert.ToBoolean(subKey.GetValue(strValue, (object)false)))
                    return dr;
            }
            catch
            {
            }
            this.m_strCheck = strCheck;
            this.m_cbt.Install();
            dr = System.Windows.Forms.MessageBox.Show(strText, strTitle, buttons, icon);
            this.m_cbt.Uninstall();
            subKey.SetValue(strValue, (object)this.m_bCheck);
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
            return this.Show(strKey, strValue, dr, strCheck, strText, strTitle, buttons, MessageBoxIcon.None);
        }

        public DialogResult Show(
          string strKey,
          string strValue,
          DialogResult dr,
          string strCheck,
          string strText,
          string strTitle)
        {
            return this.Show(strKey, strValue, dr, strCheck, strText, strTitle, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public DialogResult Show(
          string strKey,
          string strValue,
          DialogResult dr,
          string strCheck,
          string strText)
        {
            return this.Show(strKey, strValue, dr, strCheck, strText, "", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private void WndCreated(object sender, CbtEventArgs e)
        {
            if (!e.IsDialogWindow)
                return;
            this.m_bInit = false;
            this.m_hwnd = e.Handle;
        }

        private void WndDestroyed(object sender, CbtEventArgs e)
        {
            if (!(e.Handle == this.m_hwnd))
                return;
            this.m_bInit = false;
            this.m_hwnd = IntPtr.Zero;
            if (1 == (int)MessageBox.SendMessage(this.m_hwndBtn, 240, IntPtr.Zero, IntPtr.Zero))
                this.m_bCheck = true;
        }

        private void WndActivated(object sender, CbtEventArgs e)
        {
            if (this.m_hwnd != e.Handle || this.m_bInit)
                return;
            this.m_bInit = true;
            IntPtr dlgItem1 = MessageBox.GetDlgItem(this.m_hwnd, (int)ushort.MaxValue);
            IntPtr num = !(dlgItem1 != IntPtr.Zero) ? MessageBox.SendMessage(this.m_hwnd, 49, IntPtr.Zero, IntPtr.Zero) : MessageBox.SendMessage(dlgItem1, 49, IntPtr.Zero, IntPtr.Zero);
            Font font = Font.FromHfont(num);
            IntPtr dlgItem2 = MessageBox.GetDlgItem(this.m_hwnd, 20);
            int x;
            if (dlgItem2 != IntPtr.Zero)
            {
                MessageBox.RECT rc = new MessageBox.RECT();
                MessageBox.GetWindowRect(dlgItem2, rc);
                MessageBox.POINT pt = new MessageBox.POINT
                {
                    x = rc.left,
                    y = rc.top
                };
                MessageBox.ScreenToClient(this.m_hwnd, pt);
                x = pt.x;
            }
            else
                x = (int)font.GetHeight();
            MessageBox.RECT rc1 = new MessageBox.RECT();
            MessageBox.GetClientRect(this.m_hwnd, rc1);
            int y = rc1.bottom - rc1.top;
            MessageBox.GetWindowRect(this.m_hwnd, rc1);
            MessageBox.MoveWindow(this.m_hwnd, rc1.left, rc1.top, rc1.right - rc1.left, rc1.bottom - rc1.top + (int)font.GetHeight() * 2, true);
            this.m_hwndBtn = MessageBox.CreateWindowEx(0, "button", this.m_strCheck, 1342242819, x, y, rc1.right - rc1.left - x, (int)font.GetHeight(), this.m_hwnd, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            MessageBox.SendMessage(this.m_hwndBtn, 48, num, new IntPtr(1));
        }

        [DllImport("user32.dll")]
        protected static extern void DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        protected static extern IntPtr GetDlgItem(IntPtr hwnd, int id);

        [DllImport("user32.dll")]
        protected static extern int GetWindowRect(IntPtr hwnd, MessageBox.RECT rc);

        [DllImport("user32.dll")]
        protected static extern int GetClientRect(IntPtr hwnd, MessageBox.RECT rc);

        [DllImport("user32.dll")]
        protected static extern void MoveWindow(
          IntPtr hwnd,
          int x,
          int y,
          int nWidth,
          int nHeight,
          bool bRepaint);

        [DllImport("user32.dll")]
        protected static extern int ScreenToClient(IntPtr hwnd, MessageBox.POINT pt);

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
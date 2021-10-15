/*
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

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;

namespace MEGAbolt.Controls
{
  internal static class NativeMethods
  {
    internal const int WM_NCHITTEST = 132;
    internal const int WM_NCACTIVATE = 134;
    internal const int WS_EX_NOACTIVATE = 134217728;
    internal const int HTTRANSPARENT = -1;
    internal const int HTLEFT = 10;
    internal const int HTRIGHT = 11;
    internal const int HTTOP = 12;
    internal const int HTTOPLEFT = 13;
    internal const int HTTOPRIGHT = 14;
    internal const int HTBOTTOM = 15;
    internal const int HTBOTTOMLEFT = 16;
    internal const int HTBOTTOMRIGHT = 17;
    internal const int WM_PRINT = 791;
    internal const int WM_USER = 1024;
    internal const int WM_REFLECT = 8192;
    internal const int WM_COMMAND = 273;
    internal const int CBN_DROPDOWN = 7;
    internal const int WM_GETMINMAXINFO = 36;

    [SuppressUnmanagedCodeSecurity]
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int AnimateWindow(
      HandleRef windowHandle,
      int time,
      NativeMethods.AnimationFlags flags);

    internal static void AnimateWindow(
      Control control,
      int time,
      NativeMethods.AnimationFlags flags)
    {
      try
      {
        new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
        NativeMethods.AnimateWindow(new HandleRef((object) control, control.Handle), time, flags);
      }
      catch (SecurityException ex)
      {
      }
    }

    internal static int HIWORD(int n) => n >> 16 & (int) ushort.MaxValue;

    internal static int HIWORD(IntPtr n) => NativeMethods.HIWORD((int) (long) n);

    internal static int LOWORD(int n) => n & (int) ushort.MaxValue;

    internal static int LOWORD(IntPtr n) => NativeMethods.LOWORD((int) (long) n);

    [Flags]
    internal enum AnimationFlags
    {
      Roll = 0,
      HorizontalPositive = 1,
      HorizontalNegative = 2,
      VerticalPositive = 4,
      VerticalNegative = 8,
      Center = 16, // 0x00000010
      Hide = 65536, // 0x00010000
      Activate = 131072, // 0x00020000
      Slide = 262144, // 0x00040000
      Blend = 524288, // 0x00080000
      Mask = 1048575, // 0x000FFFFF
    }

    internal struct MINMAXINFO
    {
      public Point reserved;
      public Size maxSize;
      public Point maxPosition;
      public Size minTrackSize;
      public Size maxTrackSize;
    }
  }
}

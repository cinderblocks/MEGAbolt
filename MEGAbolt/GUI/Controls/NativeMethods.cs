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
      AnimationFlags flags);

    internal static void AnimateWindow(
      Control control,
      int time,
      AnimationFlags flags)
    {
      try
      {
        new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
        AnimateWindow(new HandleRef(control, control.Handle), time, flags);
      }
      catch (SecurityException ex)
      {
      }
    }

    internal static int HIWORD(int n) => n >> 16 & ushort.MaxValue;

    internal static int HIWORD(IntPtr n) => HIWORD((int) (long) n);

    internal static int LOWORD(int n) => n & ushort.MaxValue;

    internal static int LOWORD(IntPtr n) => LOWORD((int) (long) n);

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

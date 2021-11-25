/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2008-2014, www.metabolt.net (METAbolt)
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
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class FormFlash
{
    [DllImport("user32.dll")]
    private static extern int FlashWindowEx(ref FLASHWINFO pfwi);

    /// <summary>
    /// Flashes the form's taskbar button.
    /// </summary>
    /// <param name="form"></param>
    public static void Flash(Form form)
    {
        FLASHWINFO fw = new FLASHWINFO()
        {
            cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO))),
            hwnd = form.Handle,
            dwFlags = (Int32)(FLASHWINFOFLAGS.FLASHW_ALL | FLASHWINFOFLAGS.FLASHW_TIMERNOFG),
            uCount = 3,
            dwTimeout = 0
        };

        FlashWindowEx(ref fw);
        
    }

    /// <summary>
    /// Stops flashing the form's taskbar button.
    /// </summary>
    /// <param name="form"></param>
    public static void Unflash(Form form)
    {
        FLASHWINFO fw = new FLASHWINFO()
        {
            cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO))),
            hwnd = form.Handle,
            dwFlags = (Int32)(FLASHWINFOFLAGS.FLASHW_STOP),
            uCount = 0,
            dwTimeout = 0
        };

        FlashWindowEx(ref fw);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FLASHWINFO
{
    public UInt32 cbSize;
    public IntPtr hwnd;
    public Int32 dwFlags;
    public UInt32 uCount;
    public Int32 dwTimeout;
}

public enum FLASHWINFOFLAGS
{
    FLASHW_STOP      = 0,
    FLASHW_CAPTION   = 0x00000001,
    FLASHW_TRAY      = 0x00000002,
    FLASHW_ALL       = (FLASHW_CAPTION | FLASHW_TRAY),
    FLASHW_TIMER     = 0x00000004,
    FLASHW_TIMERNOFG = 0x0000000C
}

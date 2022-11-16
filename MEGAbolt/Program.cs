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
using System.Windows.Forms;
using System.Globalization;

namespace MEGAbolt
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MEGAbolt.Core.NativeMethods.Init();

            if (args.Length > 0)
            {
                if (args.Length != 3)
                {
                    MessageBox.Show("Command line usage: megabolt.exe [firstname] [lastname] [password]","MEGAbolt",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                else
                {
                    MEGAboltInstance instance = new MEGAboltInstance(true, args);
                    Application.Run(instance.MainForm);
                    instance = null;
                }
            }
            else
            {
                try
                {
                    MEGAboltInstance instance = new MEGAboltInstance(true);
                    Application.Run(instance.MainForm);
                    instance = null;
                }
                catch (Exception ex)
                {
                    //messagebox of last resort
                    DialogResult res = MessageBox.Show(string.Format(CultureInfo.CurrentCulture,
                        "Message: {0}, From: {1}, Stack: {2}", ex.Message, ex.Source, ex.StackTrace),
                        "MEGAbolt has encountered an unrecovarable error", MessageBoxButtons.RetryCancel, 
                        MessageBoxIcon.Exclamation);
                    if (res != DialogResult.Retry) { throw ex; }
                }
            } 
        }
    }
}
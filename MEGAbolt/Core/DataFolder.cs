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
using System.IO;
using System.Reflection;

namespace MEGAbolt
{
    class DataFolder
    {
        static bool firstRun = true;

        //This is necessary because System.IO.Directory.CreateDirectory fails with paths longer than 256 characters
        private static void CreateDirectoryRecursively(string path)
        {
            string[] pathParts = path.Split('\\');

            for (var i = 0; i < pathParts.Length; ++i)
            {
                if (i > 0) { pathParts[i] = Path.Combine(pathParts[i - 1]+"/", pathParts[i]); }

                if (!Directory.Exists(pathParts[i]))
                {
                    Directory.CreateDirectory(pathParts[i]);
                }
            }
        }

        public static string GetDataFolder()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\MEGAbolt";
            if (firstRun)
            {
                firstRun = false;
                if (!Directory.Exists(folder))
                {
                    CreateDirectoryRecursively(folder);
                }
            }
            return folder;
        }
    }
}

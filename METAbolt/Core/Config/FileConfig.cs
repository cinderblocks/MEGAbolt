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

using System.Collections.Generic;
using METAbolt.FileINI;
using System.Globalization;

namespace METAbolt
{
    public class FileConfig
    {
        private INIFile ini;
        FileConfig config;

        private string filename = string.Empty;

        public FileConfig(string filename)
        {
            this.filename = filename;
            ini = new INIFile(filename);
        }

        public FileConfig Load()
        {
            config = new FileConfig(filename);

            try
            {
                FriendGroups = config.ini.m_Sections;
            }
            catch
            {
               ;
            }

            return config;
        }

        public void CreateGroup(string groupname)
        {
            ini.CreateSection(groupname);
            FriendGroups = ini.m_Sections;
        }

        public void Save()
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> fr in ini.m_Sections)
            {
                string header = fr.Key.ToString(CultureInfo.CurrentCulture);
                Dictionary<string, string> rec = fr.Value;

                string uuid = string.Empty;
                string name = string.Empty;

                foreach (KeyValuePair<string, string> s in rec)
                {
                    uuid = s.Key.ToString(CultureInfo.CurrentCulture);
                    name = s.Value.ToString(CultureInfo.CurrentCulture);  
                }

                ini.SetValue(header, uuid, name);
            }

            ini.Flush(); 
        }

        public void AddFriendToGroup(string groupname, string friendname, string frienduuid)
        {
            ini.SetValue(groupname, frienduuid, friendname);
            FriendGroups = ini.m_Sections;
        }

        public void removeFriendFromGroup(string groupname, string frienduuid)
        {
            ini.RemoveValue(groupname, frienduuid);
            FriendGroups = ini.m_Sections;
        }

        public Dictionary<string, Dictionary<string, string>> FriendGroups { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    }
}

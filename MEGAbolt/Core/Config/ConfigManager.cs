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

namespace MEGAbolt
{
    public class ConfigManager
    {
        //renamed to remove the word default
        private string configPath = Path.Combine(DataFolder.GetDataFolder(), "MEGAbolt.ini");

        //default constructor
        public ConfigManager()
        {
            configPath = Path.Combine(DataFolder.GetDataFolder(), "MEGAbolt.ini");
		}

        //named constructor
        public ConfigManager(string name)
        {
            string fileName = name + "_MEGAbolt.ini";

            // Check if the file exists first
            FileInfo newFileInfo = new FileInfo(Path.Combine(DataFolder.GetDataFolder(), name + "_MEGAbolt.ini"));

            configPath = Path.Combine(DataFolder.GetDataFolder(), newFileInfo.Exists ? fileName : "MEGAbolt.ini");
        }

        public void ChangeConfigFile(string name)
        {
            //SaveCurrentConfig();

            string fileName = name + "_MEGAbolt.ini";

            // Check if the file exists first
            FileInfo newFileInfo = new FileInfo(Path.Combine(DataFolder.GetDataFolder(), name + "_MEGAbolt.ini"));

            try
            {
                if (newFileInfo.Exists)
                {
                    configPath = Path.Combine(DataFolder.GetDataFolder(), fileName);
           
                    //SaveCurrentConfig();

                    Config config;
                    config = Config.LoadFrom(configPath);
                    Apply(config);

                    // Check AI etc
                }
                else
                {
                    configPath = Path.Combine(DataFolder.GetDataFolder(), "MEGAbolt.ini");
                }
            }
            catch
            {
                ;
            }
        }

        public void SetAvConfig(string name)
        {
            string fileName = name + "_MEGAbolt.ini";

            configPath = Path.Combine(DataFolder.GetDataFolder(), fileName);
        }

        public void ApplyCurrentConfig()
        {
            Apply(CurrentConfig);
        }

        public void Apply(Config config)
        {
            CurrentConfig = config;
            OnConfigApplied(new ConfigAppliedEventArgs(CurrentConfig));
        }

        public void ApplyDefault()
        {
            Config config;

            if (File.Exists(configPath))
            {
                config = Config.LoadFrom(configPath);
            }
            else
            {
                config = new Config();
                config.Save(configPath);
            }

            Apply(config);
        }

        public void Reset()
        {
            Config config = new Config();
            config.Save(configPath);

            Apply(config);
        }

        public void SaveCurrentConfig()
        {
            //Check if the file has somehow became read-only
            FileInfo newFileInfo = new FileInfo(configPath);

            if (newFileInfo.Exists)
            {
                if ((newFileInfo.Attributes & FileAttributes.ReadOnly) > 0)
                {
                    newFileInfo.Attributes ^= FileAttributes.ReadOnly;
                }
            }
            else
            {
                //Reset();
                Config config = new Config();
                config.Save(configPath);
            }

            CurrentConfig.Save(configPath);
        }

        public event EventHandler<ConfigAppliedEventArgs> ConfigApplied;
        protected virtual void OnConfigApplied(ConfigAppliedEventArgs e)
        {
            ConfigApplied?.Invoke(this, e);
        }

        public Config CurrentConfig { get; private set; }
    }
}

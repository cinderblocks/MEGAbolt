//  Copyright (c) 2008 - 2014, www.metabolt.net (METAbolt)
//  Copyright (c) 2006-2008, Paul Clement (a.k.a. Delta)
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  * Redistributions of source code must retain the above copyright notice, 
//    this list of conditions and the following disclaimer. 
//  * Redistributions in binary form must reproduce the above copyright notice, 
//    this list of conditions and the following disclaimer in the documentation 
//    and/or other materials provided with the distribution. 

//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
//  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
//  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
//  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
//  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
//  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR 
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
//  WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
//  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
//  POSSIBILITY OF SUCH DAMAGE.

using System;
using System.IO;

namespace METAbolt
{
    public class ConfigManager
    {
        //renamed to remove the word default
        private string configPath = Path.Combine(METAbolt.DataFolder.GetDataFolder(), "MEGAbolt.ini");

        //default constructor
        public ConfigManager()
        {
            configPath = Path.Combine(METAbolt.DataFolder.GetDataFolder(), "MEGAbolt.ini");
		}

        //named constructor
        public ConfigManager(string name)
        {
            string fileName = name + "_MEGAbolt.ini";

            // Check if the file exists first
            FileInfo newFileInfo = new FileInfo(Path.Combine(METAbolt.DataFolder.GetDataFolder(), name + "_MEGAbolt.ini"));

            configPath = Path.Combine(METAbolt.DataFolder.GetDataFolder(), newFileInfo.Exists ? fileName : "MEGAbolt.ini");
        }

        public void ChangeConfigFile(string name)
        {
            //SaveCurrentConfig();

            string fileName = name + "_MEGAbolt.ini";

            // Check if the file exists first
            FileInfo newFileInfo = new FileInfo(Path.Combine(METAbolt.DataFolder.GetDataFolder(), name + "_MEGAbolt.ini"));

            try
            {
                if (newFileInfo.Exists)
                {
                    configPath = Path.Combine(METAbolt.DataFolder.GetDataFolder(), fileName);
           
                    //SaveCurrentConfig();

                    Config config;
                    config = Config.LoadFrom(configPath);
                    Apply(config);

                    // Check AI etc
                }
                else
                {
                    configPath = Path.Combine(METAbolt.DataFolder.GetDataFolder(), "MEGAbolt.ini");
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

            configPath = Path.Combine(METAbolt.DataFolder.GetDataFolder(), fileName);
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
            if (ConfigApplied != null) ConfigApplied(this, e);
        }

        public Config CurrentConfig { get; private set; }
    }
}

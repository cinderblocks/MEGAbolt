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
using System.Drawing;
using System.Windows.Forms;
using MEGAxCommon;

namespace MEGAbolt
{
    public partial class PrefPlugin : UserControl, IPreferencePane
    {
        private MEGAboltInstance instance;
        private ConfigManager config;
        private bool pluginschanged = false;
        private string plugins = string.Empty;  

        public PrefPlugin(MEGAboltInstance instance)
        {
            InitializeComponent();

            this.instance = instance;
            config = this.instance.Config;

            if (this.instance.EList != null)
            {
                foreach (IExtension extOn in this.instance.EList)
                {
                    listBox1.Items.Add(extOn.Title);
                }
            }

            plugins = config.CurrentConfig.PluginsToLoad;

            if (!string.IsNullOrEmpty(plugins))
            {
                string[] lplugs = plugins.Split('|');

                foreach (string plug in lplugs)
                {
                    if (!string.IsNullOrEmpty(plug))
                    {
                        listBox2.Items.Add(plug);
                    }
                }
            }
        }

        #region IPreferencePane Members

        string IPreferencePane.Name => " Plugins";

        Image IPreferencePane.Icon => Properties.Resources.plugin_icon;

        private void PrefPlugin_Load(object sender, EventArgs e)
        {

        }

        void IPreferencePane.SetPreferences()
        {
            plugins = string.Empty;
 
            foreach (string item in listBox2.Items)
            {
                plugins += item  + "|";
            }

            if (pluginschanged)
            {
                if (plugins.EndsWith("|", StringComparison.CurrentCultureIgnoreCase))
                {
                    plugins = plugins.Substring(0, plugins.Length - 1);    
                }

                config.CurrentConfig.PluginsToLoad = plugins;
            }
        }

        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a plugin to add first");
                return;
            }

            listBox2.Items.Add(listBox1.SelectedItem);
            pluginschanged = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a plugin to remove first");
                return;
            }

            listBox2.Items.Remove(listBox2.SelectedItem);
            pluginschanged = true;
        }

        
    }
}

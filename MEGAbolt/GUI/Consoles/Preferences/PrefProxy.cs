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

namespace MEGAbolt
{
    public partial class PrefProxy : UserControl, IPreferencePane
    {
        private MEGAboltInstance instance;
        private ConfigManager config;

        public PrefProxy(MEGAboltInstance instance)
        {
            InitializeComponent();

            this.instance = instance;
            config = this.instance.Config;

            checkBox1.Checked = config.CurrentConfig.UseProxy;
            textBox1.Text = config.CurrentConfig.ProxyURL;
            textBox4.Text = config.CurrentConfig.ProxyPort;
            textBox2.Text = config.CurrentConfig.ProxyUser;
            textBox3.Text = config.CurrentConfig.ProxyPWD; 
        }

        #region IPreferencePane Members

        string IPreferencePane.Name => " Proxy";

        Image IPreferencePane.Icon => Properties.Resources.proxy;

        void IPreferencePane.SetPreferences()
        {
            config.CurrentConfig.UseProxy = checkBox1.Checked;
            config.CurrentConfig.ProxyURL = textBox1.Text;
            config.CurrentConfig.ProxyPort = textBox4.Text;
            config.CurrentConfig.ProxyUser = textBox2.Text;
            config.CurrentConfig.ProxyPWD = textBox3.Text;
        }

        #endregion

        private void PrefParcelMusic_Load(object sender, EventArgs e)
        {

        }

        private void chkParcelMusic_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            groupBox2.Enabled = checkBox1.Checked; 
        }
    }
}

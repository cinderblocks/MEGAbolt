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
using MEGAbolt.Controls;
using System.Diagnostics;
using System.Net;

namespace MEGAbolt
{
    public partial class PrefAI : UserControl, IPreferencePane
    {
        private MEGAboltInstance instance;
        private ConfigManager config;
        private Popup toolTip3;
        private CustomToolTip customToolTip;
        //private GridClient client;
        private bool isloading = true;

        public PrefAI(MEGAboltInstance instance)
        {
            InitializeComponent();

            string msg4 = "Enable this option for your avatar to enter intelligent conversations (automated) with anyone that IMs it. Note that this feature only works via IM and not chat";
            toolTip3 = new Popup(customToolTip = new CustomToolTip(instance, msg4));
            toolTip3.AutoClose = false;
            toolTip3.FocusOnOpen = false;
            toolTip3.ShowingAnimation = toolTip3.HidingAnimation = PopupAnimations.Blend;

            this.instance = instance;
            //client = this.instance.Client;
            config = this.instance.Config;

            chkAI.Checked = config.CurrentConfig.AIon;
            chkReply.Checked = config.CurrentConfig.ReplyAI;
            textBox1.Text = config.CurrentConfig.ReplyText;
            checkBox2.Checked = false;   // config.CurrentConfig.MultiLingualAI;

            panel1.Enabled = groupBox1.Enabled = checkBox2.Enabled = chkAI.Checked;

            isloading = false;
        }

        private void picAI_Click(object sender, EventArgs e)
        {

        }

        private void PrefAI_Load(object sender, EventArgs e)
        {

        }

        #region IPreferencePane Members

        string IPreferencePane.Name => " AI";

        Image IPreferencePane.Icon => Properties.Resources.AI;

        void IPreferencePane.SetPreferences()
        {
            instance.Config.CurrentConfig.AIon = chkAI.Checked;
            instance.Config.CurrentConfig.ReplyAI = chkReply.Checked;
            instance.Config.CurrentConfig.ReplyText = textBox1.Text;
            instance.Config.CurrentConfig.MultiLingualAI = false;  // checkBox2.Checked;
        }

        #endregion

        private void picAI_MouseHover(object sender, EventArgs e)
        {
            toolTip3.Show(picAI);
        }

        private void picAI_MouseLeave(object sender, EventArgs e)
        {
            toolTip3.Close();
        }

        private void chkAI_CheckedChanged(object sender, EventArgs e)
        {
            if (isloading) { return; }

            panel1.Enabled = groupBox1.Enabled = checkBox2.Enabled = chkAI.Checked;
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string dir = Application.StartupPath + "\\config\\Settings.xml";

            if (System.IO.File.Exists(dir))
            {
                Process.Start("notepad.exe", dir); 
            }
            else
            {
                MessageBox.Show("File: \n" + dir + "\n\n could not be found", "MEGAbolt");  
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string dir = Application.StartupPath + "\\config";

            Process.Start("explorer.exe", dir);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string dir = Application.StartupPath + "\\aiml"; ;

            if (System.IO.Directory.Exists(dir))
            {
                Process.Start("explorer.exe", dir);
            }
            else
            {
                MessageBox.Show("AIML libraries could not be found!\nAre you sure they are installed?","MEGAbolt");  
            }
        }
    }
}

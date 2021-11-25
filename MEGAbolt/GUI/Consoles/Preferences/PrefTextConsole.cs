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

namespace MEGAbolt
{
    public partial class PrefTextConsole : UserControl, IPreferencePane
    {
        private MEGAboltInstance instance;
        private ConfigManager config;
        private Popup toolTip;
        private CustomToolTip customToolTip;
        private bool nudchanged = false;

        public PrefTextConsole(MEGAboltInstance instance)
        {
            InitializeComponent();

            this.instance = instance;
            config = this.instance.Config;

            string msg1 = "Use this setting to limit the amount (lines) of text stored on your chat screen. Especially in busy areas we recommend using this feature so that your machine does not run out of memory. The recommended setting is 250.";
            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            toolTip.AutoClose = false;
            toolTip.FocusOnOpen = false;
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            chkChatTimestamps.Checked = config.CurrentConfig.ChatTimestamps;
            chkIMTimestamps.Checked = config.CurrentConfig.IMTimestamps;
            chkSmileys.Checked = config.CurrentConfig.ChatSmileys;
            nud.Value = config.CurrentConfig.lineMax;
            chkIMs.Checked = config.CurrentConfig.SaveIMs;
            chkChat.Checked = config.CurrentConfig.SaveChat;
            txtDir.Text = config.CurrentConfig.LogDir;
            chkGroupNotices.Checked = config.CurrentConfig.DisableGroupNotices;
            chkGIMs.Checked = config.CurrentConfig.DisableGroupIMs;    

            //if (config.CurrentConfig.BusyReply != string.Empty && config.CurrentConfig.BusyReply != null)
            //{
            textBox1.Text = config.CurrentConfig.BusyReply;
            //}

            // Initial IM feature thx to Elmo Clarity 20/12/2010
            textBox2.Text = config.CurrentConfig.InitialIMReply;
            
            chkSLT.Checked = config.CurrentConfig.UseSLT;
            chkSound.Checked = config.CurrentConfig.PlaySound; 
        }

        #region IPreferencePane Members

        string IPreferencePane.Name => "Text";

        Image IPreferencePane.Icon => Properties.Resources.documents_32;

        void IPreferencePane.SetPreferences()
        {
            config.CurrentConfig.ChatTimestamps = chkChatTimestamps.Checked;
            config.CurrentConfig.IMTimestamps = chkIMTimestamps.Checked;
            config.CurrentConfig.ChatSmileys = chkSmileys.Checked;
            config.CurrentConfig.lineMax = Convert.ToInt32(nud.Value);
            config.CurrentConfig.UseSLT = chkSLT.Checked;
            config.CurrentConfig.PlaySound = chkSound.Checked;
            config.CurrentConfig.BusyReply = textBox1.Text;
            config.CurrentConfig.InitialIMReply = textBox2.Text;
            config.CurrentConfig.SaveIMs = chkIMs.Checked;
            config.CurrentConfig.SaveChat = chkChat.Checked;
            config.CurrentConfig.LogDir = txtDir.Text;
            config.CurrentConfig.DisableGroupIMs = chkGIMs.Checked;
            config.CurrentConfig.DisableGroupNotices = chkGroupNotices.Checked;  
        }

        #endregion

        private void chkIMTimestamps_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void PrefTextConsole_Load(object sender, EventArgs e)
        {

        }

        private void chkSmileys_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void chkSmileys_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show(pictureBox1);
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Close();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void chkSLT_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowser.SelectedPath = txtDir.Text;
   
            DialogResult result = folderBrowser.ShowDialog();

            if (result == DialogResult.OK)
            {
                txtDir.Text = folderBrowser.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", txtDir.Text);
        }

        private void chkGIMs_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void nud_ValueChanged(object sender, EventArgs e)
        {
            if (nudchanged)
            {
                instance.Config.CurrentConfig.BufferApplied = true;
            }

            nudchanged = true;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

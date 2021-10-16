//  Copyright (c) 2008 - 2014, www.metabolt.net (METAbolt)
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  * Redistributions of source code must retain the above copyright notice, 
//    this list of conditions and the following disclaimer. 
//  * Redistributions in binary form must reproduce the above copyright notice, 
//    this list of conditions and the following disclaimer in the documentation 
//    and/or other materials provided with the distribution. 
//  * Neither the name METAbolt nor the names of its contributors may be used to 
//    endorse or promote products derived from this software without specific prior 
//    written permission. 

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
using System.Drawing;
using System.Windows.Forms;
using MEGAbolt.Controls;
using System.Diagnostics;
using System.Net;

namespace METAbolt
{
    public partial class PrefAI : UserControl, IPreferencePane
    {
        private METAboltInstance instance;
        private ConfigManager config;
        private Popup toolTip3;
        private CustomToolTip customToolTip;
        //private GridClient client;
        private bool isloading = true;

        public PrefAI(METAboltInstance instance)
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
            string dir = Application.StartupPath.ToString() + "\\config\\Settings.xml";

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
            string dir = Application.StartupPath.ToString() + "\\config";

            Process.Start("explorer.exe", dir);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string dir = Application.StartupPath.ToString() + "\\aiml"; ;

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

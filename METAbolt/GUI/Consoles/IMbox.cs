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
using System.Windows.Forms;
using OpenMetaverse;
using MEGAbolt.NetworkComm;
using System.Threading;
using MEGAbolt.Controls;
using System.Globalization;
using BugSplatDotNetStandard;

namespace METAbolt
{
    public partial class IMbox : UserControl
    {
        private METAboltInstance instance;
        private GridClient client;
        private MEGAboltNetcom netcom;
        private TabsConsole tabsconsole;
        private Popup toolTip;
        private CustomToolTip customToolTip;

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                BugSplat crashReporter = new BugSplat("radegast", "MEGAbolt",
                    Properties.Resources.METAboltVersion)
                {
                    User = "cinder@cinderblocks.biz",
                    ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                };
                crashReporter.Post(e.Exception);
            }
        }

        public IMbox(METAboltInstance instance)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            client = this.instance.Client;
            netcom = this.instance.Netcom;

            string msg1 = "To view IMs, double click on an IM session from the list.\nWhen the IMbox tab turns BLUE it means there is a new IM.\nThis tab can be detached from the 'PC' icon on the right.";
            toolTip = new Popup(customToolTip = new CustomToolTip(instance, msg1));
            toolTip.AutoClose = false;
            toolTip.FocusOnOpen = false;
            toolTip.ShowingAnimation = toolTip.HidingAnimation = PopupAnimations.Blend;

            tabsconsole = instance.TabConsole;
            this.instance.imBox = this;

            Disposed += new EventHandler(IMbox_Disposed);

            netcom.InstantMessageReceived += new EventHandler<InstantMessageEventArgs>(netcom_InstantMessageReceived);
            this.instance.Config.ConfigApplied += new EventHandler<ConfigAppliedEventArgs>(Config_ConfigApplied);

            label5.Text = this.instance.Config.CurrentConfig.BusyReply;

            label6.Text = this.instance.Config.CurrentConfig.InitialIMReply;
            label8.Visible = this.instance.Config.CurrentConfig.DisableGroupIMs;
            label9.Visible = this.instance.Config.CurrentConfig.ReplyAI;

            if (string.IsNullOrEmpty(label5.Text))
            {
                label5.Text = "<empty>";
            }

            if (string.IsNullOrEmpty(label6.Text))
            {
                label6.Text = "<empty>";
            }
        }

        public void IMbox_Disposed(object sender, EventArgs e)
        {
            netcom.InstantMessageReceived -= new EventHandler<InstantMessageEventArgs>(netcom_InstantMessageReceived);
            this.instance.Config.ConfigApplied -= new EventHandler<ConfigAppliedEventArgs>(Config_ConfigApplied);
        }

        private void Config_ConfigApplied(object sender, ConfigAppliedEventArgs e)
        {
            //update with new config
            label5.Text = e.AppliedConfig.BusyReply;

            label6.Text = e.AppliedConfig.InitialIMReply;
            label8.Visible = e.AppliedConfig.DisableGroupIMs;
            label9.Visible = e.AppliedConfig.ReplyAI;

            if (string.IsNullOrEmpty(label5.Text))
            {
                label5.Text = "<empty>";
            }

            if (string.IsNullOrEmpty(label6.Text))
            {
                label6.Text = "<empty>";
            }
        }

        private void netcom_InstantMessageReceived(object sender, InstantMessageEventArgs e)
        {
            if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName))
                return;

            if (tabsconsole.tabs.ContainsKey(e.IM.FromAgentName.ToLower(CultureInfo.CurrentCulture)))
            {
                if (tabsconsole.tabs[e.IM.FromAgentName.ToLower(CultureInfo.CurrentCulture)].Selected)
                {
                    return;
                }
            }

            switch (e.IM.Dialog)
            {
                case InstantMessageDialog.MessageFromAgent:
                    if (e.IM.FromAgentName.ToLower(CultureInfo.CurrentCulture) == "second life")
                    {
                        return;
                    }

                    HandleIM(e);
                    break;
                case InstantMessageDialog.SessionSend:
                    HandleIM(e);
                    break;
                case InstantMessageDialog.StartTyping:
                    return;
                case InstantMessageDialog.StopTyping:
                    return;
            }
        }

        private void HandleIM(InstantMessageEventArgs e)
        {
            //if (e.IM.Dialog == InstantMessageDialog.SessionSend)
            //{
            //    // new IM
            //}

            string TabAgentName = string.Empty;

            lock (this.instance.State.GroupStore)
            {
                if (this.instance.State.GroupStore.ContainsKey(e.IM.IMSessionID))
                {
                    //if (null != client.Self.MuteList.Find(me => me.Type == MuteType.Group && (me.ID == e.IM.IMSessionID || me.ID == e.IM.FromAgentID))) return;

                    // Check to see if group IMs are disabled
                    if (instance.Config.CurrentConfig.DisableGroupIMs) return;

                    TabAgentName = this.instance.State.GroupStore[e.IM.IMSessionID];
                }
                else
                {
                    if (instance.IsAvatarMuted(e.IM.FromAgentID, e.IM.FromAgentName)) return;
                    TabAgentName = e.IM.FromAgentName;
                }
            }

            int s = lbxIMs.FindString(TabAgentName);

            if (s == -1)
            {
                lbxIMs.BeginUpdate();
                lbxIMs.Items.Add(TabAgentName + " (1)");
                lbxIMs.EndUpdate();
            }
            else
            {
                string fullName = Convert.ToString(lbxIMs.Items[s], CultureInfo.CurrentCulture);
                string imcount = string.Empty;
                int cnt = 0;

                if (fullName.Contains("("))
                {
                    try
                    {
                        string[] splits = fullName.Split('(');

                        fullName = splits[0].ToString().Trim();
                        imcount = splits[1].ToString().Trim();
                        string[] splits1 = imcount.Split(')');

                        try
                        {
                            imcount = splits1[0].ToString().Trim();
                            cnt = Convert.ToInt32(imcount, CultureInfo.CurrentCulture) + 1;
                        }
                        catch { cnt = 1; }

                        fullName = TabAgentName + " (" + cnt.ToString(CultureInfo.CurrentCulture) + ")";

                        lbxIMs.BeginUpdate();
                        lbxIMs.Items[s] = fullName;
                        lbxIMs.EndUpdate();
                    }
                    catch { ; }
                }

            }

            SetSets();
        }

        private void IMbox_Load(object sender, EventArgs e)
        {

        }

        private void SetSets()
        {
            if (lbxIMs.Items.Count > 0)
            {
                label1.Visible = false;
            }
            else
            {
                label1.Visible = true;
                tabsconsole.tabs["imbox"].Unhighlight();
            }

            label3.Text = lbxIMs.Items.Count.ToString(CultureInfo.CurrentCulture);
            instance.State.UnReadIMs = lbxIMs.Items.Count;

            lbxIMs.SelectedIndex = -1;
        }

        private void lbxIMs_DoubleClick(object sender, EventArgs e)
        {
            if (lbxIMs.SelectedItem == null) return;

            string fullName = lbxIMs.SelectedItem.ToString();
            int selinx = lbxIMs.SelectedIndex;

            string[] splits = fullName.Split('(');

            fullName = splits[0].ToString().Trim();

            lbxIMs.Items.RemoveAt(selinx);

            SetSets();

            if (tabsconsole.TabExists(fullName))
            {
                tabsconsole.SelectTab(fullName);
                return;
            }
        }

        public void IMRead(string fullName)
        {
            int s = lbxIMs.FindString(fullName);

            if (s > -1)
            {
                lbxIMs.Items.RemoveAt(s);
            }

            SetSets();
        }

        private void picAutoSit_Click(object sender, EventArgs e)
        {

        }

        private void picAutoSit_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show(picAutoSit);
        }

        private void picAutoSit_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Close();
        }

        private void lbxIMs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbxIMs.SelectedItem == null)
            {
                btnView.Enabled = false;
                return;
            }

            btnView.Enabled = true; 
        }

        private void btnView_Click(object sender, EventArgs e)
        {
            if (lbxIMs.SelectedItem == null)
            {
                btnView.Enabled = false;
                return;
            }

            string fullName = lbxIMs.SelectedItem.ToString();
            int selinx = lbxIMs.SelectedIndex;

            string[] splits = fullName.Split('(');

            fullName = splits[0].ToString().Trim();

            lbxIMs.Items.RemoveAt(selinx);

            SetSets();

            if (tabsconsole.TabExists(fullName))
            {
                tabsconsole.SelectTab(fullName);
                return;
            }
        }
    }
}

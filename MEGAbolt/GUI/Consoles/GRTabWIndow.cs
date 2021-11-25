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
using System.Windows.Forms;
using OpenMetaverse;

namespace MEGAbolt
{
    public partial class GRTabWindow : UserControl
    {
        private MEGAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;
        private UUID targetUUID;

        public GRTabWindow(MEGAboltInstance instance, InstantMessageEventArgs e)
        {
            InitializeComponent();

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            ProcessEventArgs(e);
        }

        private void ProcessEventArgs(InstantMessageEventArgs e)
        {
            //string[] split;

            try
            {
                TargetName = e.IM.FromAgentName;
                targetUUID = e.IM.FromAgentID;
                iSession = e.IM.IMSessionID;
                //string gmsg = @e.IM.Message.ToString();

                //split = gmsg.Split(new Char[] { ':' });

                //if (split.Length > 1)
                //{
                //    textBox1.Text = @split[0].ToString().Replace("Group", "\r\n\r\nGroup: ") + @split[1].ToString().Replace("\n", " ");
                //}
                //else
                //{
                //    textBox1.Text = @split[0].ToString();
                //}

                textBox1.Text = @e.IM.Message.Replace("\n", "\r\n").Replace("Group:", "\r\n\r\nGroup: ");
            }
            catch { ; }
        }

        public void CloseTab()
        {
            try
            {
                instance.TabConsole.GetTab("chat").Select();
                instance.TabConsole.GetTab(targetUUID.ToString()).Close();
            }
            catch
            {
                ;
            }
        }

        public string TargetName { get; private set; }

        public UUID TargetUUID => targetUUID;

        public UUID iSession { get; private set; }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            // There is a bug here which needs to be looked at some stage
            try
            {
                client.Self.InstantMessage(client.Self.Name, targetUUID, string.Empty, iSession, InstantMessageDialog.GroupInvitationAccept, InstantMessageOnline.Offline, instance.SIMsittingPos(), UUID.Zero, new byte[0]); // Accept Group Invitation (Join Group)
                CloseTab();
            }
            catch
            {
                ; 
            }
        }

        private void btnDecline_Click(object sender, EventArgs e)
        {
            client.Self.InstantMessage(client.Self.Name, targetUUID, string.Empty, iSession, InstantMessageDialog.GroupInvitationDecline, InstantMessageOnline.Offline, instance.SIMsittingPos(), UUID.Zero, new byte[0]); // Decline Group Invitation
            CloseTab();
        }
    }
}
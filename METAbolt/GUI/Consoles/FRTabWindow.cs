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

namespace METAbolt
{
    public partial class FRTabWindow : UserControl
    {
        private METAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;
        private UUID targetUUID;
        //private FriendsConsole fconsole;

        public FRTabWindow(METAboltInstance instance, InstantMessageEventArgs e)
        {
            InitializeComponent();

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            ProcessEventArgs(e);
        }

        private void ProcessEventArgs(InstantMessageEventArgs e)
        {
            TargetName = e.IM.FromAgentName;
            targetUUID = e.IM.FromAgentID;
            iSession = e.IM.IMSessionID;

            lblSubheading.Text =
                "You have received a Friendship invite from " + TargetName + "";

            //rtbOfferMessage.AppendText(e.IM.Message);
        }

        public void CloseTab()
        {
            instance.TabConsole.GetTab("chat").Select();
            instance.TabConsole.GetTab(targetUUID.ToString()).Close();
        }

        public string TargetName { get; private set; }

        public UUID TargetUUID => targetUUID;

        public UUID iSession { get; private set; }

        private void btnAccept_Click_1(object sender, EventArgs e)
        {
            client.Friends.AcceptFriendship(targetUUID, iSession);

            //fconsole = new FriendsConsole(instance); 
            //fconsole.InitializeFriendsList();
            ////BeginInvoke(new MethodInvoker(fconsole.InitializeFriendsList()));
            ////BeginInvoke(new MethodInvoker(fconsole.InitializeFriendsList()));
            CloseTab();
        }

        private void btnDecline_Click_1(object sender, EventArgs e)
        {
            client.Friends.DeclineFriendship(targetUUID, iSession);
            CloseTab();
        }
    }
}

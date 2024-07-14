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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenMetaverse;
using System.Threading;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;

namespace MEGAbolt
{
    public partial class FriendsConsole : UserControl
    {
        private readonly MEGAboltInstance instance;
        private readonly GridClient client;
        private FriendInfo selectedFriend;
        private FileConfig fconfig;
        Dictionary<string, Dictionary<string, string>> fgrps;

        private bool settingFriend = false;

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                if (!String.IsNullOrEmpty(Generated.BugsplatDatabase))
                {
                    BugSplat crashReporter = new BugSplat(Generated.BugsplatDatabase, "MEGAbolt",
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                    {
                        User = Generated.BugsplatUser,
                        ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                    };
                    crashReporter.Post(e.Exception);
                }
            }
        }

        public FriendsConsole(MEGAboltInstance instance)
        {
            InitializeComponent();
            Disposed += FriendsConsole_Disposed;
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            client = this.instance.Client;
            //netcom = this.instance.Netcom;

            client.Friends.FriendshipTerminated += Friends_OnFriendTerminated;
            client.Friends.FriendshipResponse += Friends_OnFriendResponse;
            client.Friends.FriendNames += Friends_OnFriendNamesReceived;
            client.Friends.FriendOffline += Friends_OnFriendOffline;
            client.Friends.FriendOnline += Friends_OnFriendOnline;
            client.Friends.FriendRightsUpdate += Friends_OnFriendRights;
            //client.Avatars.DisplayNameUpdate += new EventHandler<DisplayNameUpdateEventArgs>(Avatar_DisplayNameUpdated);    

            //InitializeFriendsList();
        }

        public void InitializeFriendsList()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(InitializeFriendsList));
                return;
            }

            List<FriendInfo> friendslist = client.Friends.FriendList.FindAll((FriendInfo friend) => true);

            instance.State.AvatarFriends = friendslist;

            if (friendslist.Count > 0)
            {
                lbxFriends.BeginUpdate();
                lbxFriends.Items.Clear();

                foreach (FriendInfo friend in friendslist)
                {
                    lbxFriends.Items.Add(new FriendsListItem(friend));
                }

                //lbxFriends.Sort();
                lbxFriends.EndUpdate();

                lblFriendName.Text = string.Empty;  
            }
        }

        public void FriendsConsole_Disposed(object sender, EventArgs e)
        {
            client.Friends.FriendshipTerminated -= Friends_OnFriendTerminated;
            client.Friends.FriendshipResponse -= Friends_OnFriendResponse;
            client.Friends.FriendNames -= Friends_OnFriendNamesReceived;
            client.Friends.FriendOffline -= Friends_OnFriendOffline;
            client.Friends.FriendOnline -= Friends_OnFriendOnline;
            client.Friends.FriendRightsUpdate -= Friends_OnFriendRights;
            //client.Avatars.DisplayNameUpdate -= new EventHandler<DisplayNameUpdateEventArgs>(Avatar_DisplayNameUpdated);

            try
            {
                fconfig?.Save();
            }
            catch { ; }
        }

        private void RefreshFriendsList()
        {
            if (cbofgroups.SelectedIndex is 0 or -1)
            {
                InitializeFriendsList();
            }
            else
            {
                GetGroupFriends(cbofgroups.SelectedItem.ToString());
            }

            SetFriend(selectedFriend);
        }

        private void Friends_OnFriendResponse(object sender, FriendshipResponseEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Friends_OnFriendResponse(sender, e)));
                return;
            }

            if (e.Accepted)
            {
                BeginInvoke(new MethodInvoker(RefreshFriendsList));
            }
        }

        private void Friends_OnFriendTerminated(object sender, FriendshipTerminatedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Friends_OnFriendTerminated(sender, e)));
                return;
            }

            BeginInvoke(new MethodInvoker(() =>
            {
                RemoveFriendFromAllGroups(e.AgentID.ToString());
                RefreshFriendsList();
            }));
        }

        private void RemoveFriendFromAllGroups(string uuid)
        {
            fgrps = fconfig.FriendGroups;

            foreach (KeyValuePair<string, Dictionary<string, string>> fr in fgrps)
            {
                string header = fr.Key;
                //Dictionary<string, string> rec = fr.Value;

                Dictionary<string, string> grps;

                fgrps.TryGetValue(header, out grps);

                foreach (KeyValuePair<string, string> s in grps)
                {
                    if (s.Key == uuid)
                    {
                        fconfig.removeFriendFromGroup(header,uuid);
                    }
                }
            }

            fgrps = fconfig.FriendGroups;
        }

        //Separate thread
        private void Friends_OnFriendOffline(object sender, FriendInfoEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Friends_OnFriendOffline(sender, e)));
                return;
            }

            BeginInvoke(new MethodInvoker(RefreshFriendsList));
        }

        //Separate thread
        private void Friends_OnFriendOnline(object sender, FriendInfoEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Friends_OnFriendOnline(sender, e)));
                return;
            }

            BeginInvoke(new MethodInvoker(RefreshFriendsList));
        }

        private void Friends_OnFriendRights(object sender, FriendInfoEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Friends_OnFriendRights(sender, e)));
                return;
            }

            BeginInvoke(new MethodInvoker(RefreshFriendsList));
        }

        private void Friends_OnFriendNamesReceived(object sender, FriendNamesEventArgs e)
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(new MethodInvoker(() => Friends_OnFriendNamesReceived(sender, e)));
                }

                return;
            }

            BeginInvoke(new MethodInvoker(() =>
            {
                try
                {
                    if (IsHandleCreated)
                    {
                        RefreshFriendsList();
                    }
                }
                catch {; }
            }));
        }

        private void SetFriend(FriendInfo friend)
        {
            //if (InvokeRequired)
            //{
            //    BeginInvoke(new MethodInvoker(() => SetFriend(friend)));
            //    return;
            //}

            try
            {
                if (friend == null) return;

                if (cbofgroups.SelectedIndex > 0)
                {
                    button2.Visible = true;
                    button2.Enabled = true;
                }
                else
                {
                    button2.Visible = false;
                    button2.Enabled = false; 
                }

                selectedFriend = friend;

                lblFriendName.Text = friend.Name + (friend.IsOnline ? " (online)" : " (offline)");

                btnRemove.Enabled = btnIM.Enabled = btnProfile.Enabled = btnOfferTeleport.Enabled = btnPay.Enabled = true;
                chkSeeMeOnline.Enabled = chkSeeMeOnMap.Enabled = chkModifyMyObjects.Enabled = true;
                chkSeeMeOnMap.Enabled = friend.CanSeeMeOnline;

                settingFriend = true;
                chkSeeMeOnline.Checked = friend.CanSeeMeOnline;
                chkSeeMeOnMap.Checked = friend.CanSeeMeOnMap;
                chkModifyMyObjects.Checked = friend.CanModifyMyObjects;
                settingFriend = false;
            }
            catch { ; }

        }

        private void lbxFriends_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0) return;

            FriendsListItem itemToDraw = (FriendsListItem)lbxFriends.Items[e.Index];

            Brush textBrush = null;
            Font textFont = null;
            
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                textBrush = new SolidBrush(Color.FromKnownColor(KnownColor.HighlightText));
                textFont = new Font(e.Font, FontStyle.Bold);
            }
            else
            {
                textBrush = new SolidBrush(Color.FromKnownColor(KnownColor.ControlText));
                textFont = new Font(e.Font, FontStyle.Regular);
            }

            SizeF stringSize = e.Graphics.MeasureString(itemToDraw.Friend.Name, textFont);
            float stringX = e.Bounds.Left + 4 + Properties.Resources.green_orb.Width;
            float stringY = e.Bounds.Top + 2 + ((Properties.Resources.green_orb.Height / 2) - (stringSize.Height / 2));

            e.Graphics.DrawImage(
                itemToDraw.Friend.IsOnline ? Properties.Resources.green_orb : Properties.Resources.green_orb_off,
                e.Bounds.Left + 2, e.Bounds.Top + 2);

            e.Graphics.DrawString(" " + itemToDraw.Friend.Name, textFont, textBrush, stringX, stringY);

            e.DrawFocusRectangle();

            textFont.Dispose();
            textBrush.Dispose();
            textFont = null;
            textBrush = null;
        }

        private void lbxFriends_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbxFriends.SelectedItem == null) return;

            FriendsListItem item = (FriendsListItem)lbxFriends.SelectedItem;
            SetFriend(item.Friend);
        }

        private void btnIM_Click(object sender, EventArgs e)
        {
            string agentname = selectedFriend.Name;

            if (instance.TabConsole.TabExists(agentname))
            {
                instance.TabConsole.SelectTab(agentname);
                return;
            }

            instance.TabConsole.AddIMTab(selectedFriend.UUID, client.Self.AgentID ^ selectedFriend.UUID, agentname);
            instance.TabConsole.SelectTab(agentname);
        }

        private void btnProfile_Click(object sender, EventArgs e)
        {
            (new frmProfile(instance, selectedFriend.Name, selectedFriend.UUID)).Show();
        }

        private void chkSeeMeOnline_CheckedChanged(object sender, EventArgs e)
        {
            if (settingFriend) return;

            SetRights();
        }

        private void chkSeeMeOnMap_CheckedChanged(object sender, EventArgs e)
        {
            if (settingFriend) return;

            SetRights();
        }

        private void chkModifyMyObjects_CheckedChanged(object sender, EventArgs e)
        {
            if (settingFriend) return;

            SetRights();
        }

        private void SetRights()
        {
            FriendRights rgts = FriendRights.None;

            if (chkSeeMeOnline.Checked)
            {
                rgts |= FriendRights.CanSeeOnline;
            }

            if (chkSeeMeOnMap.Checked)
            {
                rgts |= FriendRights.CanSeeOnMap;
            }
            if (chkModifyMyObjects.Checked)
            {
                rgts |= FriendRights.CanModifyObjects;
            }

            client.Friends.GrantRights(selectedFriend.UUID, rgts);
        }

        private void btnOfferTeleport_Click(object sender, EventArgs e)
        {
            client.Self.SendTeleportLure(selectedFriend.UUID, "Join me in " + client.Network.CurrentSim.Name + "!");
        }

        private void btnPay_Click(object sender, EventArgs e)
        {
            (new frmPay(instance, selectedFriend.UUID, selectedFriend.Name)).Show(this);
        }

        private void FriendsConsole_Load(object sender, EventArgs e)
        {
            InitializeFriendsList();

            //lbGroups.Items.Add("All");
            //lbGroups.SelectedIndex = 0;
            cbofgroups.Items.Add("...All friends");
            cbofgroups.SelectedIndex = 0;

            string fconffile = DataFolder.GetDataFolder() + "\\" + client.Self.AgentID + "_fr_groups.ini";
// maybe use "Path.Combine(MEGAbolt.DataFolder.GetDataFolder(),client.Self.AgentID.ToString() + "_fr_groups.ini");" ?

            if (!System.IO.File.Exists(fconffile))
            {
                System.IO.StreamWriter SW;

                SW = System.IO.File.CreateText(fconffile);
                SW.Dispose();
            }

            //lbxFriends.PreSelect  += new EventHandler(lbxFriends_PreSelect); 


            fconfig = new FileConfig(fconffile);
            fconfig.Load();

            LoadFriendGroups();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show($"Are you sure you want to terminate\nyour friendship with {selectedFriend.Name}?", 
                "MEGAbolt", MessageBoxButtons.YesNo);

            if (res == DialogResult.No)
            {
                return;
            }

            client.Friends.TerminateFriendship(selectedFriend.UUID);
        }

        private void lbxFriends_DoubleClick(object sender, EventArgs e)
        {
            btnIM.PerformClick();
        }

        private void lbGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblGroupName.Text = lbGroups.SelectedItem.ToString();

            textBox2.Visible = lbGroups.SelectedIndex != -1;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                fconfig.CreateGroup(txtGroup.Text.Trim());

                txtGroup.Text = string.Empty;
                //fgrps = fconfig.FriendGroups;

                LoadFriendGroups();
            }
            catch
            {
                //string exp = ex.Message; 
            }
        }

        private void LoadFriendGroups()
        {
            fgrps = fconfig.FriendGroups;

            cbofgroups.Items.Clear();
            lbGroups.Items.Clear();

            cbofgroups.Items.Add("...All friends"); 

            foreach (KeyValuePair<string, Dictionary<string, string>> fr in fgrps)
            {
                string header = fr.Key;
                //Dictionary<string, string> rec = fr.Value;

                cbofgroups.Items.Add(header);
                lbGroups.Items.Add(header);   
            }

            cbofgroups.Sorted = true;
            lbGroups.Sorted = true;
        }

        private void cbofgroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fconfig == null) return;

            button2.Enabled = false;

            if (cbofgroups.SelectedItem.ToString() == "...All friends")
            {
                RefreshFriendsList();
            }
            else
            {
                GetGroupFriends(cbofgroups.SelectedItem.ToString());
            }
        }

        private void GetGroupFriends(string group)
        {
            lblFriendName.Text = string.Empty;

            btnRemove.Enabled = btnIM.Enabled = btnProfile.Enabled = btnOfferTeleport.Enabled = btnPay.Enabled = false;
            chkSeeMeOnline.Enabled = chkSeeMeOnMap.Enabled = chkModifyMyObjects.Enabled = false;

            fgrps = fconfig.FriendGroups;

            Dictionary<string, string> grps;

            fgrps.TryGetValue(group, out grps);

            lbxFriends.BeginUpdate();
            lbxFriends.Items.Clear();

            foreach (KeyValuePair<string, string> s in grps)
            {
                List<FriendInfo> flist = instance.State.AvatarFriends;

                if (flist.Count > 0)
                {
                    foreach (FriendInfo friend in flist)
                    {
                        if (friend.Name.ToLower(CultureInfo.CurrentCulture) == s.Value.ToLower(CultureInfo.CurrentCulture))
                        {
                            lbxFriends.Items.Add(new FriendsListItem(friend));
                        }
                    }
                }
            }

            lbxFriends.EndUpdate();
        }

        private void lbxFriends_MouseDown(object sender, MouseEventArgs e)
        {
            lbxFriends_SelectedIndexChanged(null, null);

            Point pt = new Point(e.X, e.Y);
            int index = lbxFriends.IndexFromPoint(pt);

            // Starts a drag-and-drop operation.
            if (index >= 0 && index < lbxFriends.Items.Count)
            {
                FriendsListItem dltm = (FriendsListItem)lbxFriends.Items[index];

                lbxFriends.DoDragDrop(dltm, DragDropEffects.Copy);
            }
        }

        private void textBox2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FriendsListItem)))
                e.Effect = DragDropEffects.Copy;
        }

        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            if (lbGroups.SelectedIndex == -1)
            {
                MessageBox.Show("You must select a group from the list first.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            FriendsListItem node = e.Data.GetData(typeof(FriendsListItem)) as FriendsListItem;

            if (node == null) return;

            if (e.Data.GetDataPresent(typeof(FriendsListItem)))
            {
                fconfig.AddFriendToGroup(lbGroups.SelectedItem.ToString(), node.Friend.Name, node.Friend.UUID.ToString());
                MessageBox.Show($"{node.Friend.Name} has been added to your '{lbGroups.SelectedItem}' group.", "MEGAbolt", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void cbofgroups_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            fconfig.removeFriendFromGroup(lbGroups.SelectedItem.ToString(), selectedFriend.UUID.ToString());
        }        
    }
}

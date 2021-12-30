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
using System.Windows.Forms;
using OpenMetaverse;
using System.Threading;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;


// Group List user control
// Added by Legoals Luke


namespace MEGAbolt
{
    public partial class GroupsConsole : UserControl
    {
        private readonly MEGAboltInstance instance;
        private readonly GridClient Client;
        private TabsConsole tabConsole;

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                BugSplat crashReporter = new BugSplat(Generated.BugsplatDatabase, "MEGAbolt",
                    Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                {
                    User = "cinder@cinderblocks.biz",
                    ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                };
                crashReporter.Post(e.Exception);
            }
        }

        public GroupsConsole(MEGAboltInstance instance)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            Client = this.instance.Client;

            Client.Groups.CurrentGroups += Groups_OnCurrentGroups;
            Client.Groups.RequestCurrentGroups();

            Client.Groups.GroupJoinedReply += Groups_OnGroupStateChanged;
            Client.Groups.GroupLeaveReply += Groups_OnGroupStateChanged;
            Client.Groups.GroupMemberEjected += Groups_GroupMemberEjected;

            Disposed += GroupsConsole_Disposed;
        }

        public void GroupsConsole_Disposed(object sender, EventArgs e)
        {
            Client.Groups.CurrentGroups -= Groups_OnCurrentGroups;
            Client.Groups.GroupJoinedReply -= Groups_OnGroupStateChanged;
            Client.Groups.GroupLeaveReply -= Groups_OnGroupStateChanged;
            Client.Groups.GroupMemberEjected -= Groups_GroupMemberEjected;
        }

        private void Groups_GroupMemberEjected(object sender, GroupOperationEventArgs e)
        {
            Client.Groups.RequestCurrentGroups();
        }

        private void UpdateGroups()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(UpdateGroups));

                return;
            }

            try
            {
                lstGroups.Items.Clear();
                lock (instance.State.Groups)
                {
                    foreach (Group group in instance.State.Groups.Values)
                    {
                        lstGroups.Items.Add(group);

                        if (Client.Self.ActiveGroup != UUID.Zero)
                        {
                            if (Client.Self.ActiveGroup == group.ID)
                            {
                                label1.Text = $"Current active group tag: {group.Name}";
                            }
                        }
                        else
                        {
                            label1.Text = "No active group tag set";
                        }
                    }
                }
                lstGroups.Sorted = true;
                lstGroups.Sorted = false;

                //lstGroups.Items.Add("None");
                lstGroups.Items.Insert(0, "None"); 

                if (lstGroups.Items.Count > 0)
                {
                    int cnt = lstGroups.Items.Count - 1;
                    label6.Text = "Total: " + cnt + " groups";
                }
                else
                {
                    label6.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Groups Console error", Helpers.LogLevel.Error, ex); 
            }
        }

   

        #region GUI Callbacks

        private void lstGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstGroups.SelectedIndex >= 0)
            {
                if ((string)lstGroups.Items[lstGroups.SelectedIndex].ToString() != "None")
                {
                    picBusy.Visible = true;
                    Client.Groups.GroupProfile += GroupProfileHandler;
                    Group selgrp = (Group)lstGroups.Items[lstGroups.SelectedIndex];
                    Client.Groups.RequestGroupProfile(selgrp.ID);

                    cmdActivate.Enabled = cmdInfo.Enabled = cmdIM.Enabled = button4.Enabled = cmdLeave.Enabled = true;
                    label5.Text = "Group UUID: " + ((Group)lstGroups.Items[lstGroups.SelectedIndex]).ID.ToString();
                }
                else
                {
                    gbQuickInfo.Visible = false;
                    cmdActivate.Enabled = true;
                    cmdInfo.Enabled = button4.Enabled = cmdIM.Enabled = cmdLeave.Enabled = false;
                    label5.Text = string.Empty;
                }
            }
            else
            {
                gbQuickInfo.Visible = false;
                cmdActivate.Enabled = cmdInfo.Enabled = cmdIM.Enabled = button4.Enabled = cmdLeave.Enabled = false;
                label5.Text = string.Empty;
            }
        }

        private void GroupProfileHandler(object sender, GroupProfileEventArgs e)
        {
            Client.Groups.GroupProfile -= GroupProfileHandler;

            Group selgrp = e.Group;

            BeginInvoke(new MethodInvoker(delegate()
                {
                    picBusy.Visible = false;
                    gbQuickInfo.Visible = true;
                    label7.Text = "Ttl members: " + selgrp.GroupMembershipCount.ToString(CultureInfo.CurrentCulture);
                    label8.Text = "Ttl roles: " + selgrp.GroupRolesCount.ToString(CultureInfo.CurrentCulture);
                    label9.Text = "Joining fee: L$" + selgrp.MembershipFee.ToString(CultureInfo.CurrentCulture);
                    label10.Text = "Open enrolment: " + selgrp.OpenEnrollment.ToString(CultureInfo.CurrentCulture);
                }));
        }


        #endregion GUI Callbacks

        #region Network Callbacks

        private void Groups_OnCurrentGroups(object sender, CurrentGroupsEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    Groups_OnCurrentGroups(sender, e);
                }));

                return;
            }

            lock (instance.State.Groups)
            {
                foreach (KeyValuePair<UUID, Group> g in e.Groups)
                {
                    if (!instance.State.Groups.ContainsKey(g.Key))
                    {
                        instance.State.Groups.Add(g.Key, g.Value);
                    }
                }
            }

            lock (instance.State.GroupStore)
            {
                foreach (Group group in instance.State.Groups.Values)
                {
                    if (!instance.State.GroupStore.ContainsKey(group.ID))
                    {
                        instance.State.GroupStore.Add(group.ID, group.Name);
                    }
                }
            }

            BeginInvoke(new MethodInvoker(delegate()
            {
                UpdateGroups();
            }));            
        }

        #endregion     

        private void GroupsConsole_Load(object sender, EventArgs e)
        {
            tabConsole = instance.TabConsole;
        }

        private void cmdInfo_Click_1(object sender, EventArgs e)
        {
            if (lstGroups.SelectedIndex >= 0 && lstGroups.Items[lstGroups.SelectedIndex].ToString() != "None")
            {
                if (lstGroups.Items[lstGroups.SelectedIndex].ToString()  != "None")
                {
                    Group group = (Group)lstGroups.Items[lstGroups.SelectedIndex];

                    //frmGroupInfo frm = new frmGroupInfo(group, instance);
                    //frm.ShowDialog();
                    (new frmGroupInfo(group, instance)).Show();
                }

                lstGroups.SetSelected(lstGroups.SelectedIndex, true);
            }
        }

        private void cmdActivate_Click(object sender, EventArgs e)
        {
            if (lstGroups.SelectedIndex >= 0)
            {
                if (lstGroups.Items[lstGroups.SelectedIndex].ToString() == "None")
                {
                    Client.Groups.ActivateGroup(UUID.Zero);
                }
                else
                {
                    Group group = (Group)lstGroups.Items[lstGroups.SelectedIndex];
                    Client.Groups.ActivateGroup(group.ID);
                }
            }

            lstGroups.SetSelected(lstGroups.SelectedIndex, true);
        }

        private void cmdLeave_Click(object sender, EventArgs e)
        {
            if (lstGroups.SelectedIndex >= 0 && lstGroups.Items[lstGroups.SelectedIndex].ToString() != "None")
            {
                Group group = (Group)lstGroups.Items[lstGroups.SelectedIndex];

                DialogResult res = MessageBox.Show($"Are you sure you want to LEAVE {group.Name}?", "MEGAbolt", MessageBoxButtons.YesNo);

                if (res == DialogResult.No)
                {
                    return;
                }

                //Group group = (Group)lstGroups.Items[lstGroups.SelectedIndex];
                Client.Groups.LeaveGroup(group.ID);
            }
        }

        private void Groups_OnGroupStateChanged(object sender, GroupOperationEventArgs e)
        {
            Client.Groups.RequestCurrentGroups();
        }

        //private void Groups_OnGroupLeft(object sender, GroupOperationEventArgs e)
        //{
        //    Client.Groups.RequestCurrentGroups();
        //}

        private void cmdCreate_Click(object sender, EventArgs e)
        {
            if (lstGroups.SelectedIndex >= 0 && lstGroups.Items[lstGroups.SelectedIndex].ToString() != "None")
            {
                if (lstGroups.Items[lstGroups.SelectedIndex].ToString() != "None")
                {
                    Group group = (Group)lstGroups.Items[lstGroups.SelectedIndex];

                    if (tabConsole.TabExists(group.Name))
                    {
                        tabConsole.SelectTab(group.Name);
                        return;
                    }

                    tabConsole.AddIMTabGroup(group.ID, group.ID, group.Name, group);
                    tabConsole.SelectTab(group.Name);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            cmdIM.Enabled = cmdActivate.Enabled = cmdInfo.Enabled = button4.Enabled = cmdLeave.Enabled = button1.Enabled = label5.Visible = false;
            panel1.Visible = true; 
        }

        private void button3_Click(object sender, EventArgs e)
        {
            EnableNew();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Group newgroup = new Group
            {
                Name = textBox1.Text,
                Charter = textBox2.Text,
                FounderID = Client.Self.AgentID
            };
            Client.Groups.RequestCreateGroup(newgroup);

            EnableNew();
        }

        private void EnableNew()
        {
            panel1.Visible = false;
            cmdIM.Enabled = cmdActivate.Enabled = cmdInfo.Enabled = button4.Enabled = cmdLeave.Enabled = button1.Enabled = label5.Visible = true; 
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (lstGroups.SelectedIndex >= 0 && lstGroups.Items[lstGroups.SelectedIndex].ToString() != "None")
            {
                Group group = (Group)lstGroups.Items[lstGroups.SelectedIndex];

                (new frmGive(instance, group.ID, UUID.Zero)).Show(this);

                lstGroups.SetSelected(lstGroups.SelectedIndex, true);
            }
        }
    }
}
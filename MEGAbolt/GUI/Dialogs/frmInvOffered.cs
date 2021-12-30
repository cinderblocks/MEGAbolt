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
using System.Threading;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;

namespace MEGAbolt
{
    public partial class frmInvOffered : Form
    {
        private readonly MEGAboltInstance instance;
        private readonly GridClient client;
        private readonly InstantMessage msg;
        private readonly UUID objectID;
        //private bool diainv = false;
        private readonly AssetType invtype = AssetType.Unknown;
        private bool printed = false;
        private readonly InstantMessageDialog diag;

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

        public frmInvOffered(MEGAboltInstance instance, InstantMessage e, UUID objectID, AssetType type)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            client = this.instance.Client;
            msg = e;
            this.objectID = objectID;
            Text += $" [{client.Self.Name}]";

            invtype = type;

            instance.State.FolderRcvd = invtype == AssetType.Folder;

            diag = e.Dialog;

            lblSubheading.Text = $"You have received {type} named '{e.Message}' from {e.FromAgentName}";

            if (instance.Config.CurrentConfig.PlayInventoryItemReceived)
            {
                instance.MediaManager.PlayUISound(Properties.Resources.Item_received);
            }

            timer1.Interval = instance.DialogTimeOut;
            timer1.Enabled = true;
            timer1.Start();

            DateTime dte = DateTime.Now.AddMinutes(15.0d);

            label1.Text = $"This item will be auto accepted at {dte.ToShortTimeString()}";

            Text += $"   [ {client.Self.Name} ]";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            client.Inventory.RemoveItem(objectID);

            //DataRow dr = instance.MuteList.NewRow();
            //dr["uuid"] = msg.FromAgentID;
            //dr["mute_name"] = msg.FromAgentName;
            //instance.MuteList.Rows.Add(dr);

            if (diag == InstantMessageDialog.TaskInventoryOffered)
            {
                if (instance.IsObjectMuted(msg.FromAgentID, msg.FromAgentName))
                {
                    MessageBox.Show($"{msg.FromAgentName} is already in your mute list.", "MEGAbolt", 
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                instance.Client.Self.UpdateMuteListEntry(MuteType.Object, msg.FromAgentID, msg.FromAgentName);
            }
            else
            {
                if (instance.IsAvatarMuted(msg.FromAgentID, msg.FromAgentName))
                {
                    MessageBox.Show($"{msg.FromAgentName} is already in your mute list.", "MEGAbolt", 
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                instance.Client.Self.UpdateMuteListEntry(MuteType.Resident, msg.FromAgentID, msg.FromAgentName);
            }

            MessageBox.Show($"{msg.FromAgentName} is now muted.", "MEGAbolt", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            timer1.Stop();
            timer1.Enabled = false;

            Close();
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            try
            {
                UUID invfolder = UUID.Zero;

                if (invtype == AssetType.Folder)
                {
                    instance.State.FolderRcvd = true;
                    invfolder = client.Inventory.Store.RootFolder.UUID;
                }
                else
                {
                    instance.State.FolderRcvd = false;
                    invfolder = client.Inventory.FindFolderForType(invtype);
                }

                if (diag == InstantMessageDialog.InventoryOffered)
                {
                    client.Self.InstantMessage(client.Self.Name, msg.FromAgentID, string.Empty, msg.IMSessionID, 
                        InstantMessageDialog.InventoryAccepted, InstantMessageOnline.Offline, instance.SIMsittingPos(), 
                        client.Network.CurrentSim.RegionID, invfolder.GetBytes());   //  new byte[0]); // Accept Inventory Offer
                    client.Inventory.RequestFetchInventory(objectID, client.Self.AgentID);
                }
                else if (diag == InstantMessageDialog.TaskInventoryOffered)
                {
                    client.Self.InstantMessage(client.Self.Name, msg.FromAgentID, string.Empty, msg.IMSessionID,
                        InstantMessageDialog.TaskInventoryAccepted, InstantMessageOnline.Offline, instance.SIMsittingPos(), 
                        client.Network.CurrentSim.RegionID, invfolder.GetBytes()); // Accept TaskInventory Offer
                    client.Inventory.RequestFetchInventory(objectID, client.Self.AgentID);
                }
                else
                {
                    timer1.Stop();
                    timer1.Enabled = false;
                    //this.Close();
                }

                timer1.Stop();
                timer1.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error has been encountered but the item\n" +
                                "should have been saved into your inventory:\n" + ex.Message, "MEGAbolt");  
            }
            
            Close();
        }

        private void btnDecline_Click(object sender, EventArgs e)
        {
            try
            {
                UUID invfolder = UUID.Zero;

                if (invtype == AssetType.Folder)
                {
                    instance.State.FolderRcvd = true;
                    invfolder = client.Inventory.Store.RootFolder.UUID;
                }
                else
                {
                    instance.State.FolderRcvd = false;
                    invfolder = client.Inventory.FindFolderForType(invtype);
                }

                if (diag == InstantMessageDialog.InventoryOffered)
                {
                    client.Self.InstantMessage(client.Self.Name, msg.FromAgentID, string.Empty, msg.IMSessionID, 
                        InstantMessageDialog.InventoryDeclined, InstantMessageOnline.Offline, instance.SIMsittingPos(), 
                        client.Network.CurrentSim.RegionID, invfolder.GetBytes()); // Decline Inventory Offer

                    try
                    {
                        InventoryBase item = client.Inventory.Store.Items[objectID].Data;
                        UUID content = client.Inventory.FindFolderForType(FolderType.Trash);

                        InventoryFolder folder = (InventoryFolder)client.Inventory.Store.Items[content].Data;

                        if (invtype != AssetType.Folder)
                        {
                            client.Inventory.Move(item, folder);
                        }
                        else
                        {
                            client.Inventory.MoveFolder(objectID, content);
                        }
                    }
                    catch { ; }
                }
                else if (diag == InstantMessageDialog.TaskInventoryOffered)
                {
                    client.Self.InstantMessage(client.Self.Name, msg.FromAgentID, string.Empty, msg.IMSessionID, 
                        InstantMessageDialog.TaskInventoryDeclined, InstantMessageOnline.Offline, instance.SIMsittingPos(), 
                        client.Network.CurrentSim.RegionID, invfolder.GetBytes()); // Decline Inventory Offer
                }

                timer1.Stop();
                timer1.Enabled = false;
            }
            catch
            {
                ;                
            }

            Close();
        }

        private void frmInvOffered_Load(object sender, EventArgs e)
        {
            CenterToParent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //btnDecline.PerformClick();

            timer1.Enabled = false;
            timer1.Stop();
            
            if (!printed)
            {
                instance.TabConsole.DisplayChatScreen(
                    $" 'Inventory offer' from {msg.FromAgentName} has timed out and the item named '{msg.Message}' has been saved to your {invtype} folder.");
            }

            printed = true;

            btnAccept.PerformClick();

            //timer1.Dispose();

            Close();
        }

        private void frmInvOffered_MouseEnter(object sender, EventArgs e)
        {
            Opacity = 100;
        }

        private void frmInvOffered_MouseLeave(object sender, EventArgs e)
        {
            Opacity = 75;
        }
    }
}

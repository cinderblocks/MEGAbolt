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

namespace MEGAbolt
{
    public partial class InventoryItemConsole : UserControl
    {
        private MEGAboltInstance instance;
        //private SLNetCom netcom;
        private GridClient client;
        private InventoryItem item;
        //private bool thisTP = false;
        //private Primitive rootPrim;
        private bool fLoading = true;
        //private InventoryConsole iconsole;
        //private TreeNode inode;

        public InventoryItemConsole(MEGAboltInstance instance, InventoryItem item)
        {
            InitializeComponent();

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            this.item = item;
            
            Disposed += InventoryItemConsole_Disposed;

            //iconsole = new InventoryConsole(instance); 

            AddClientEvents();
            FillItemProperties();
        }

        private void InventoryItemConsole_Disposed(object sender, EventArgs e)
        {
            CleanUp();
        }

        public void CleanUp()
        {
            client.Avatars.UUIDNameReply -= Avatars_OnAvatarNames;
            //client.Self.OnTeleport -= new AgentManager.TeleportCallback(TP_Callback); 
        }

        private void AddClientEvents()
        {
            client.Avatars.UUIDNameReply += Avatars_OnAvatarNames;
            //client.Self.OnTeleport += new AgentManager.TeleportCallback(TP_Callback); 
        }

        //comes up in a separate thread
        private void Avatars_OnAvatarNames(object sender, UUIDNameReplyEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate { Avatars_OnAvatarNames(sender, e); });
                return;
            }

            //using parent to invoke for avoiding race condition between this event and whether this control is disposed
            //BeginInvoke((MethodInvoker)delegate { CreatorOwnerReceived(e.Names); });
            CreatorOwnerReceived(e.Names);
        }

        //runs on the GUI thread
        private void CreatorOwnerReceived(Dictionary<UUID, string> names)
        {
            if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { CreatorOwnerReceived(names); });
            else
            {
                foreach (KeyValuePair<UUID, string> kvp in names)
                {
                    if (kvp.Key == item.CreatorID)
                        txtItemCreator.Text = kvp.Value;
                    else if (kvp.Key == item.OwnerID)
                        txtItemOwner.Text = kvp.Value;
                }

                if (item.CreatorID == item.OwnerID) txtItemOwner.Text = txtItemCreator.Text;
            }
        }

        private void FillItemProperties()
        {
            txtItemName.Text = item.Name;

            if (item.AssetUUID != UUID.Zero)
            {
                txtUUID.Text = item.AssetUUID.ToString();
            }
            else
            {
                txtUUID.Text = item.UUID.ToString();
                txtItemUUID.Text = item.UUID.ToString();
            }

            if (item.UUID != UUID.Zero)
            {
                txtItemUUID.Text = item.UUID.ToString();
            }

            txtItemCreator.Text = txtItemOwner.Text = "Retreiving name...";
            txtItemDescription.Text = item.Description;

            List<UUID> avIDs = new List<UUID>();
            avIDs.Add(item.CreatorID);
            avIDs.Add(item.OwnerID);
            client.Avatars.RequestAvatarNames(avIDs);

            // Get permissions
            if ((item.Permissions.NextOwnerMask & PermissionMask.Modify) == PermissionMask.Modify)
            {
                checkBox1.Checked = true;
                txtItemDescription.ReadOnly = false;
            }
            else
            {
                checkBox1.Checked = false;
                txtItemDescription.ReadOnly = true;
            }

            if ((item.Permissions.NextOwnerMask & PermissionMask.Copy) == PermissionMask.Copy)
            {
                checkBox2.Checked = true;
            }
            else
            {
                checkBox2.Checked = false;
            }

            if ((item.Permissions.NextOwnerMask & PermissionMask.Transfer) == PermissionMask.Transfer)
            {
                checkBox3.Checked = true;
            }
            else
            {
                checkBox3.Checked = false;
            }

            // Set permission checboxes
            if ((item.Permissions.OwnerMask & PermissionMask.Modify) == PermissionMask.Modify)
            {
                checkBox1.Enabled = true;
                txtItemDescription.ReadOnly = false;
            }
            else
            {
                checkBox1.Enabled = false;
                txtItemDescription.ReadOnly = true;
            }

            if ((item.Permissions.OwnerMask & PermissionMask.Copy) == PermissionMask.Copy)
            {
                checkBox2.Enabled = true;
            }
            else
            {
                checkBox2.Enabled = false;
            }

            if ((item.Permissions.OwnerMask & PermissionMask.Transfer) == PermissionMask.Transfer)
            {
                checkBox3.Enabled = true;
                btnGive.Enabled = true; 
            }
            else
            {
                checkBox3.Enabled = false;
                btnGive.Enabled = false; 
            }

            label11.Visible = true;

            switch (item.InventoryType)
            {
                case InventoryType.Object:
                    InventoryObjectConsole objectConsole = new InventoryObjectConsole(instance, item);
                    btnDetach.Text = "Detach";
                    objectConsole.Dock = DockStyle.Fill;
                    pnlItemTypeProp.Controls.Add(objectConsole);

                    //objectConsole.Dispose(); 
                    break;

                case InventoryType.Notecard:
                    InventoryNotecardConsole notecardConsole = new InventoryNotecardConsole(instance, item);
                    notecardConsole.Dock = DockStyle.Fill;
                    pnlItemTypeProp.Controls.Add(notecardConsole);
                    label11.Visible = false;

                    //notecardConsole.Dispose();
                    break;

                case InventoryType.LSL:
                    InventoryScriptConsole scriptConsole = new InventoryScriptConsole(instance, item);
                    scriptConsole.Dock = DockStyle.Fill;
                    pnlItemTypeProp.Controls.Add(scriptConsole);
                    label11.Visible = false;

                    //scriptConsole.Dispose(); 
                    break;
                case InventoryType.Snapshot:
                    InventoryImageConsole imageConsole = new InventoryImageConsole(instance, item);
                    imageConsole.Dock = DockStyle.Fill;
                    pnlItemTypeProp.Controls.Add(imageConsole);
                    label11.Visible = false;
                    break;
                case InventoryType.Wearable:
                    btnDetach.Text = "Take off";
                    break;
                case InventoryType.Attachment:
                    btnDetach.Text = "Detach";
                    break;
                case InventoryType.Landmark:
                    label11.Visible = false;
                    break;
                case InventoryType.Animation:
                    InventoryAnimationConsole animationConsole = new InventoryAnimationConsole(instance, item);
                    animationConsole.Dock = DockStyle.Fill;
                    pnlItemTypeProp.Controls.Add(animationConsole);
                    label11.Visible = false;
                    break;
                case InventoryType.Texture:
                    imageConsole = new InventoryImageConsole(instance, item);
                    imageConsole.Dock = DockStyle.Fill;
                    pnlItemTypeProp.Controls.Add(imageConsole);
                    label11.Visible = false;
                    break;
                case InventoryType.Gesture:
                    InventoryGestureConsol gestureConsole = new InventoryGestureConsol(instance, item);
                    gestureConsole.Dock = DockStyle.Fill;
                    pnlItemTypeProp.Controls.Add(gestureConsole);
                    label11.Visible = false;
                    break;
            }

            if (item.InventoryType == InventoryType.Wearable)
            {
                InventoryWearable werbl = item as InventoryWearable;

                if (item.ParentUUID == instance.CoF.UUID)
                {
                    InventoryItem wItem = AInventoryItem(item);

                    werbl = wItem as InventoryWearable;
                    label9.Text = "Wearable type: " + werbl.WearableType.ToString();
                }
                else
                {
                    label9.Text = "Wearable type: " + werbl.WearableType.ToString();
                }
            }
            else
            {
                //if (item.ParentUUID == instance.CoF.UUID)
                //{

                //}
                //else
                //{
                //    label9.Text = string.Empty;
                //}

                label9.Text = "Asset type: " + item.AssetType.ToString();
            }

            if ((item.Permissions.OwnerMask & PermissionMask.Modify) != PermissionMask.Modify)
            {
                checkBox1.Enabled = checkBox2.Enabled = checkBox3.Enabled = false;   
            }

            fLoading = false;
        }

        private InventoryItem AInventoryItem(InventoryItem item)
        {
            if (item.IsLink() && client.Inventory.Store.Contains(item.AssetUUID) && client.Inventory.Store[item.AssetUUID] is InventoryItem)
            {
                return (InventoryItem)client.Inventory.Store[item.AssetUUID];
            }

            return item;
        }

        private void InventoryItemConsole_Load(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void btnGive_Click(object sender, EventArgs e)
        {
            //bool sCopy = false;

            if ((item.Permissions.OwnerMask & PermissionMask.Transfer) != PermissionMask.Transfer)
            {
                MessageBox.Show("This is a NO TRANSFER item and cannot be given away.", "STOP", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            (new frmGive(instance, item)).Show(this);
        }

        private void btnTP_Click(object sender, EventArgs e)
        {
            if (instance.State.IsSitting)
            {
                client.Self.Stand();
                instance.State.SetStanding();
            }

            UUID landmark = new UUID();
            label7.Visible = true;

            //thisTP = true; 

            //item.InventoryType 

            if (!UUID.TryParse(txtUUID.Text, out landmark))
            {
                label7.Text = "Invalid TP LLUID";
            }
            else
            {
                progressBar1.Visible = true;
                label7.Text = "Teleporting to " + txtItemName.Text;
                label7.ForeColor = Color.Black;
            }

            //client.Self.Teleport(item.AssetUUID);

            if (client.Self.Teleport(landmark))
            //if (client.Self.Teleport(item.UUID))
            {
                label7.Text = "Teleport Succesful";
                label7.ForeColor = Color.LightGreen;
            }
            else
            {
                label7.Text = "Teleport Failed";
                label7.ForeColor = Color.Red;
            }

            //Thread.Sleep(3000);
            //label7.Visible = false;
            progressBar1.Visible = false;
        }

        private void btnDetach_Click(object sender, EventArgs e)
        {
            //if (item.AssetType == AssetType.Clothing)
            //{
            //    iconsole.managerbusy = client.Appearance.ManagerBusy;
            //    client.Appearance.RemoveFromOutfit(item);
            //    //client.Appearance.RequestSetAppearance(false);
            //    //MessageBox.Show("Use the 'Changer' function or the 'Replace Outfit' option \non popup menu");
            //}
            //else
            //{
            //    if (item.AssetType == AssetType.Object)
            //    {
            //        client.Appearance.Detach(item.UUID);
            //    }
            //}

            ////BeginInvoke(new MethodInvoker(() => iconsole.refreshFolderToolStripMenuItem_Click(this, null)));

            ////iconsole.RefreshInventoryNode(inode);
            //try
            //{
            //    //iconsole.treeView1.SelectedNode.Text = inode.Text.Replace(" (WORN)", "");
            //    BeginInvoke(new MethodInvoker(() => instance.insconsole.WearTakeoff(false, inode)));
            //    //instance.insconsole.WearTakeoff(false, inode);
            //}
            //catch (Exception ex)
            //{
            //    string exp = ex.Message; 
            //}
        }

        private void btnWear_Click(object sender, EventArgs e)
        {
            //if (item.AssetType == AssetType.Clothing || item.AssetType == AssetType.Bodypart)
            //{
            //    try
            //    {
            //        iconsole.managerbusy = client.Appearance.ManagerBusy;
            //        //List<InventoryBase> clothing = new List<InventoryBase>();
            //        //clothing.Add((InventoryBase)item);

            //        client.Appearance.AddToOutfit(item);
            //        //client.Appearance.RequestSetAppearance(false);
            //        //MessageBox.Show("Use the 'Changer' function or the 'Replace Outfit' option \non popup menu"); 
            //    }
            //    catch (Exception exp)
            //    {
            //        Logger.Log("(inventory wear): " + exp.InnerException.ToString(), Helpers.LogLevel.Error);
            //    }
            //}
            //else if (item.AssetType == AssetType.Object)
            //{
            //    client.Appearance.Attach(item, AttachmentPoint.Default, false);
            //}

            ////iconsole.RefreshInventoryNode(inode);
            ////BeginInvoke(new MethodInvoker(() => iconsole.refreshFolderToolStripMenuItem_Click(this, null)));

            //try
            //{
            //    //iconsole.treeView1.SelectedNode.Text = inode.Text.Replace(" (WORN)", "") + " (WORN)";
            //    BeginInvoke(new MethodInvoker(() => instance.insconsole.WearTakeoff(true, inode)));
            //    //instance.insconsole.WearTakeoff(true, inode);
            //}
            //catch (Exception ex)
            //{
            //    string exp = ex.Message;
            //}
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            SetPerm();    
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            SetPerm();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            SetPerm();
        }

        private void SetPerm()
        {
            if (fLoading) return;

            PermissionMask Perms = PermissionMask.None;

            if (checkBox1.Checked)
            {
                Perms |= PermissionMask.Modify;
            }

            if (checkBox2.Checked)
            {
                Perms |= PermissionMask.Copy;
            }

            if (checkBox3.Checked)
            {
                Perms |= PermissionMask.Transfer;
            }

            item.Permissions.NextOwnerMask = Perms;  
            client.Inventory.RequestUpdateItem(item);
        }

        private void pnlItemTypeProp_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txtItemDescription_Leave(object sender, EventArgs e)
        {
            if (!txtItemDescription.ReadOnly)
            {
                item.Description = txtItemDescription.Text;
                client.Inventory.RequestUpdateItem(item);
            }
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void txtItemDescription_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void txtItemCreator_TextChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void txtItemOwner_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

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
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using OpenMetaverse;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Linq;
using BugSplatDotNetStandard;
using File = System.IO.File;

// Some parts of this code has been adopted from OpenMetaverse.GUI
//
/*
 * Copyright (c) 2007-2008, openmetaverse.org
 * All rights reserved.
*/


namespace MEGAbolt
{
    public partial class InventoryConsole : UserControl
    {
        private GridClient client;
        private MEGAboltInstance instance;
        private InventoryItemConsole currentProperties;
        private InventoryClipboard clip;
        private InventoryTreeSorter treeSorter = new InventoryTreeSorter();
        private bool ShowAuto = false;
        private string SortBy = "By Name";

        // auto changer vars
        private string textfile; // = "Outfit.txt";
        private string path; // = Path.Combine(Environment.CurrentDirectory, "Outfit.txt");
        private int x = 0;
        public bool managerbusy = false;
        private bool searching = false;
        //private UUID folderproc = UUID.Zero;
        private TreeNode sellectednode = new TreeNode();
        private InventoryFolder rootFolder;
        private TreeNode rootNode;
        private TreeNode selectednode = null;
        //private bool nodecol = false;
        private UUID favfolder = UUID.Zero;
        private Dictionary<UUID, InventoryItem> inventoryitems = new Dictionary<UUID, InventoryItem>();
        //private bool AppearanceSet = false;
        //private TreeViewWalker treeViewWalker;
        //public InventoryFolder CoF;
        private bool gotCoF = false;

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                BugSplat crashReporter = new BugSplat("radegast", "MEGAbolt",
                    Properties.Resources.MEGAboltVersion)
                {
                    User = "cinder@cinderblocks.biz",
                    ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                };
                crashReporter.Post(e.Exception);
            }
        }

        public InventoryConsole(MEGAboltInstance instance)
        {
            InitializeComponent();
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            client = this.instance.Client;
            clip = new InventoryClipboard(client);

            Disposed += InventoryConsole_Disposed;

            textfile = "\\" + client.Self.FirstName + "_" + client.Self.LastName + "_" + "Outfit.mtb";
            path = Path.Combine(DataFolder.GetDataFolder(), textfile);

            ReadTextFile();

            //baseinv.RestoreFromDisk

            InitializeImageList();
            InitializeTree();
            GetRoot();

            instance.insconsole = this;

            //TreeViewWalker treeViewWalker = new TreeViewWalker(treeView1);
        }

        private void InitializeImageList()
        {
            ilsInventory.Images.Add("ArrowForward", Properties.Resources.arrow_forward_16);
            ilsInventory.Images.Add("ClosedFolder", Properties.Resources.folder_closed_16);
            ilsInventory.Images.Add("OpenFolder", Properties.Resources.folder_open_16);
            ilsInventory.Images.Add("Gear", Properties.Resources.applications_16);
            ilsInventory.Images.Add("Notecard", Properties.Resources.documents_16);
            ilsInventory.Images.Add("Script", Properties.Resources.lsl_scripts_16);
            ilsInventory.Images.Add("LM", Properties.Resources.lm);
            ilsInventory.Images.Add("CallingCard", Properties.Resources.friend);
            ilsInventory.Images.Add("Objects", Properties.Resources.objects);
            ilsInventory.Images.Add("Snapshots", Properties.Resources.debug);
            ilsInventory.Images.Add("Texture", Properties.Resources.texture);
            ilsInventory.Images.Add("Wearable", Properties.Resources.wear);
        }

        private void GetRoot()
        {
            rootFolder = client.Inventory.Store.RootFolder;
            rootNode = treeView1.Nodes.Add(rootFolder.UUID.ToString(), "My Inventory");
            rootNode.Tag = rootFolder;
            rootNode.ImageKey = "ClosedFolder";

            //treeLookup.Add(rootFolder.UUID, rootNode);

            //Triggers treInventory's AfterExpand event, thus triggering the root content request
            rootNode.Nodes.Add("Requesting folder contents...");
            rootNode.Expand();
        }

        //Seperate thread
        private void Inventory_OnItemReceived(object sender, ItemReceivedEventArgs e)
        {
            if (InvokeRequired)
            {

                BeginInvoke(new MethodInvoker(delegate()
                {
                    Inventory_OnItemReceived(sender, e);
                }));

                return;
            }

            ReceivedInventoryItem(e.Item);
        }

        //UI thread
        private void ReceivedAttachement(Primitive prim)
        {
            //UpdateFolder(item.ParentUUID);
        }

        //UI thread
        private void ReceivedInventoryItem(InventoryItem item)
        {
            if (InvokeRequired)
            {

                BeginInvoke(new MethodInvoker(delegate()
                {
                    ReceivedInventoryItem(item);
                }));

                return;
            }

            try
            {
                //nodecol = false;

                //addeditem = item.UUID;
                //UUID fldr = client.Inventory.Store.RootFolder.UUID;
                UUID fldr = item.UUID;

                InventoryBase io = (InventoryBase)item;

                if (io is InventoryFolder)
                {
                    // do nothing
                }
                else
                {
                    if (item.ParentUUID != UUID.Zero)
                    {
                        fldr = item.ParentUUID;
                    }
                }

                client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;
                UpdateFolder(fldr);
                client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;
            }
            catch { ; }
        }

        private void Store_OnInventoryObjectRemoved(object sender, InventoryObjectRemovedEventArgs e)
        {
            //nodecol = false;
            //RefreshInventory();

            UUID fldr = e.Obj.UUID;

            InventoryBase io = (InventoryBase)e.Obj;

            if (io is InventoryFolder)
            {
                // do nothing
            }
            else
            {
                if (e.Obj.ParentUUID != UUID.Zero)
                {
                    fldr = e.Obj.ParentUUID;
                }
            }


            //WorkPool.QueueUserWorkItem(new WaitCallback(UpdateFolder), fldr);

            client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;
            UpdateFolder(fldr);
            client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;

        }

        //private void Store_OnInventoryObjectAdded(object sender, InventoryObjectAddedEventArgs e)
        //{
        //    if (InvokeRequired)
        //    {

        //        BeginInvoke(new MethodInvoker(delegate()
        //        {
        //            Store_OnInventoryObjectAdded(sender, e);
        //        }));

        //        return;
        //    }

        //    //////addeditem = item.UUID;
        //    //UUID fldr = client.Inventory.Store.RootFolder.UUID;

        //    //if (e.Obj.ParentUUID != UUID.Zero)
        //    //{
        //    //    fldr = e.Obj.ParentUUID;
        //    //}

        //    //WorkPool.QueueUserWorkItem(new WaitCallback(UpdateFolder), fldr);

        //    //if (!instance.State.FolderRcvd)
        //    //{
        //    //    return;
        //    //}

        //    //instance.State.FolderRcvd = false;

        //    //if (nodecol) return;

        //    //RefreshInventory();

        //    //treeView1.Nodes.Clear();

        //    //treeSorter.CurrentSortName = SortBy;
        //    //treeView1.TreeViewNodeSorter = treeSorter;

        //    //((ToolStripMenuItem)tbtnSort.DropDown.Items[0]).PerformClick();

        //    //GetRoot();

        //    UpdateFolder(e.Obj.ParentUUID);
        //}

        private void Inventory_OnAppearanceSet(object sender, AppearanceSetEventArgs e)
        {
            if (InvokeRequired)
            {

                BeginInvoke(new MethodInvoker(delegate()
                {
                    Inventory_OnAppearanceSet(sender, e);
                }));

                return;
            }

            //AppearanceSet = true;

            //CheckAttachments();
            //client.Appearance.RequestAgentWearables();  

            try
            {
                //RefreshInventory();
                if (favfolder.CompareTo(UUID.Zero) != 0)
                {
                    List<InventoryBase> foundfolders = client.Inventory.Store.GetContents(favfolder);
                    instance.MainForm.UpdateFavourites(foundfolders);
                }
            }
            catch { ; }

            if (managerbusy)
            {
                managerbusy = false;
                client.Appearance.RequestSetAppearance(true);
            }
        }

        private void Network_OnEventQueueRunning(object sender, EventQueueRunningEventArgs e)
        {
            if (e.Simulator == client.Network.CurrentSim)
            {
                GetRoots();
            }
        }

        private void GetRoots()
        {
            List<InventoryBase> invroot = client.Inventory.Store.GetContents(client.Inventory.Store.RootFolder.UUID);

            foreach (InventoryBase o in invroot)
            {
                if (o.Name.ToLower(CultureInfo.CurrentCulture) == "current outfit")
                {
                    if (!gotCoF)
                    {
                        if (o is InventoryFolder folder)
                        {
                            client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByDate);
                            instance.CoF = folder;
                            gotCoF = true;
                        }
                    }
                }

                if (!instance.Config.CurrentConfig.DisableFavs)
                {
                    if (o.Name.ToLower(CultureInfo.CurrentCulture) == "favorites" || o.Name.ToLower(CultureInfo.CurrentCulture) == "my favorites")
                    {
                        if (o is InventoryFolder)
                        {
                            favfolder = instance.FavsFolder = o.UUID;

                            client.Inventory.RequestFolderContents(favfolder, client.Self.AgentID, true, true, InventorySortOrder.ByDate); ;
                        }
                    }
                }
                else
                {
                    if (o.Name.ToLower(CultureInfo.CurrentCulture) == "favorites" || o.Name.ToLower(CultureInfo.CurrentCulture) == "my favorites")
                    {
                        if (o is InventoryFolder)
                        {
                            favfolder = instance.FavsFolder = o.UUID;
                        }
                    }
                }
            }
        }

        //private void CheckAttachments()
        //{
        //    //inventoryitems

        //    lock (inventoryitems)
        //    {
        //        List<Primitive> attachments = client.Network.CurrentSim.ObjectsPrimitives.FindAll((Primitive p) => p.ParentID == client.Self.LocalID);

        //        //foreach (InventoryItem item in inventoryitems.Values)
        //        //{
        //        //    //if (item is InventoryObject || item is InventoryAttachment)
        //        //    //{
        //        //        if (!IsAttached(attachments, item))
        //        //        {
        //        //            client.Appearance.Attach(item, AttachmentPoint.Default, false);
        //        //        }
        //        //    //}
        //        //}

        //        foreach (Primitive prim in attachments)
        //        {
        //            //if (prim.NameValues != null)
        //            //{
        //            //    for (int i = 0; i < prim.NameValues.Length; i++)
        //            //    {
        //            //        if (prim.NameValues[i].Name == "AttachItemID")
        //            //        {
        //            //            return (UUID)prim.NameValues[i].Value.ToString(); prim.
        //            //        }
        //            //    }
        //            //}

        //            client.Appearance.Detach(prim.ID);
        //            client.Appearance.Attach(prim.ID, client.Self.AgentID, "", "", Permissions.FullPermissions, 0, AttachmentPoint.Default, false); 
        //        }
        //    }
        //}

        public static bool IsAttached(List<Primitive> attachments, InventoryItem item)
        {
            foreach (Primitive prim in attachments)
            {
                if (IsAttachment(prim) == item.UUID)
                {
                    return true;
                }
            }

            return false;
        }

        private static UUID IsAttachment(Primitive prim)
        {
            if (prim.NameValues == null) return UUID.Zero;

            for (int i = 0; i < prim.NameValues.Length; i++)
            {
                if (prim.NameValues[i].Name == "AttachItemID")
                {
                    return (UUID)prim.NameValues[i].Value.ToString();
                }
            }

            return UUID.Zero;
        }

        //Seperate thread
        private void Inventory_OnFolderUpdated(object sender, FolderUpdatedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    Inventory_OnFolderUpdated(sender, e);
                }));

                return;
            }

            try
            {
                if (!searching)
                {
                    //if (folderproc == e.FolderID)
                    //{
                        ////WorkPool.QueueUserWorkItem(new WaitCallback(UpdateFolder), e.FolderID);

                        client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;
                        UpdateFolder(e.FolderID);
                        client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;
                        //folderproc = UUID.Zero;

                        if (instance.CoF != null)
                        {
                            if (e.FolderID == instance.CoF.UUID)
                            {
                                instance.CoF = (InventoryFolder)client.Inventory.Store.Items[client.Inventory.FindFolderForType(FolderType.CurrentOutfit)].Data;
                            }
                        }

                    //}
                }
            }
            catch { ; }
        }

        //private void Store_OnInventoryObjectUpdated(object sender, InventoryObjectUpdatedEventArgs e)
        //{
        //    if (InvokeRequired)
        //    {
        //        BeginInvoke(new MethodInvoker(delegate()
        //        {
        //            Store_OnInventoryObjectUpdated(sender, e);
        //        }));

        //        return;
        //    }

        //    //UpdateFolder(e.NewObject.ParentUUID);
        //    RefreshInventory();
        //}

        private void CleanUp()
        {
            ClearCurrentProperties();
            timer1.Enabled = false;
        }

        //public void UpdateFolder(object folderID)
        //{
        //    UpdateFolder((UUID)folderID);
        //}

        public void UpdateFolder(UUID folderID)
        {
            if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { UpdateFolder(folderID); });
            else
            {
                if (searching) return;

                if (folderID == UUID.Zero)
                {
                    folderID = client.Inventory.Store.RootFolder.UUID;
                }

                try
                {
                    TreeViewWalker treeViewWalker = new TreeViewWalker(treeView1);

                    treeViewWalker.LoadInventory(instance, folderID);

                    if (selectednode != null)
                    {
                        treeView1.HideSelection = false;
                        treeView1.SelectedNode = selectednode;
                        treeView1.SelectedNode.EnsureVisible();
                    }
                }
                catch { ; }

                if (folderID == favfolder)
                {
                    List<InventoryBase> invroot = client.Inventory.Store.GetContents(favfolder);
                    instance.MainForm.UpdateFavourites(invroot);
                }
                

                treeView1.Sort();  
            }
        }

        private void InitializeTree()
        {
            try
            {
                client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;
                client.Inventory.ItemReceived += Inventory_OnItemReceived;
                //client.Inventory.InventoryObjectOffered += new EventHandler<InventoryObjectOfferedEventArgs>(Inventory_InventoryObjectOffered);
                client.Appearance.AppearanceSet += Inventory_OnAppearanceSet;
                client.Inventory.Store.InventoryObjectRemoved += Store_OnInventoryObjectRemoved;
                //client.Inventory.Store.InventoryObjectUpdated += new EventHandler<InventoryObjectUpdatedEventArgs>(Store_OnInventoryObjectUpdated);
                client.Network.EventQueueRunning += Network_OnEventQueueRunning;
                //client.Appearance.AgentWearablesReply += Appearance_AgentWearablesReply;

                //client.Appearance.RequestAgentWearables();   

                foreach (ITreeSortMethod method in treeSorter.GetSortMethods())
                {
                    ToolStripMenuItem item = (ToolStripMenuItem)tbtnSort.DropDown.Items.Add(method.Name);
                    item.ToolTipText = method.Description;
                    item.Name = method.Name;

                    if (method.Name == "By Date")
                    {
                        item.ShortcutKeys = Keys.Control | Keys.D;
                    }
                    else
                    {
                        item.ShortcutKeys = Keys.Control | Keys.N;
                    }

                    item.Click += SortMethodClick;
                }

                treeSorter.CurrentSortName = SortBy;
                treeView1.TreeViewNodeSorter = treeSorter;

                ((ToolStripMenuItem)tbtnSort.DropDown.Items[0]).PerformClick();
            }
            catch (Exception ex)
            {
                Logger.Log("Inventory error (initialise tree): " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        //void Appearance_AgentWearablesReply(object sender, AgentWearablesReplyEventArgs e)
        //{
        //    var ftgd = e.ToString();
        //}

        private void InventoryConsole_Disposed(object sender, EventArgs e)
        {
            client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;
            client.Inventory.ItemReceived -= Inventory_OnItemReceived;
            //client.Inventory.InventoryObjectOffered -= new EventHandler<InventoryObjectOfferedEventArgs>(Inventory_InventoryObjectOffered);
            client.Appearance.AppearanceSet -= Inventory_OnAppearanceSet;
            client.Inventory.Store.InventoryObjectRemoved -= Store_OnInventoryObjectRemoved;
            //client.Inventory.Store.InventoryObjectAdded -= new EventHandler<InventoryObjectAddedEventArgs>(Store_OnInventoryObjectAdded);
            //netcom.ClientLoggedOut -= new EventHandler(netcom_ClientLoggedOut);
            //netcom.ClientDisconnected -= new EventHandler<DisconnectedEventArgs>(netcom_ClientDisconnected);
            //client.Inventory.Store.InventoryObjectUpdated -= new EventHandler<InventoryObjectUpdatedEventArgs>(Store_OnInventoryObjectUpdated);
            client.Network.EventQueueRunning -= Network_OnEventQueueRunning;
        }

        private void SortMethodClick(object sender, EventArgs e)
        {
            client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;

            TreeNode node = treeView1.SelectedNode;

            if (!string.IsNullOrEmpty(treeSorter.CurrentSortName))
                ((ToolStripMenuItem)tbtnSort.DropDown.Items[treeSorter.CurrentSortName]).Checked = false;

            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            treeSorter.CurrentSortName = item.Text;

            treeView1.BeginUpdate();
            treeView1.Sort();
            treeView1.EndUpdate();

            item.Checked = true;

            treeView1.SelectedNode = node;

            client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;
        }

        private void RefreshPropertiesPane()
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (io is InventoryItem item)
            {
                panel2.Visible = false;

                //TreeNode node = treeView1.SelectedNode;

                //InventoryImageConsole.vi 
                InventoryItemConsole console = new InventoryItemConsole(instance, item);
                console.Dock = DockStyle.Fill;
                splitContainer1.Panel2.Controls.Add(console);

                ClearCurrentProperties();
                //ClearAutoProperties();
                currentProperties = console;

                //item.InventoryType 

                try
                {
                    if (item.InventoryType == InventoryType.Wearable || item.InventoryType == InventoryType.Attachment || item.InventoryType == InventoryType.Object)
                    {
                        //console.Controls["btnDetach"].Visible = true;
                        //console.Controls["btnWear"].Visible = true;
                        console.Controls["label11"].Visible = true;
                        
                        console.Controls["btnTP"].Visible = false;
                    }
                    else if (item.InventoryType == InventoryType.Landmark)
                    {
                        console.Controls["btnTP"].Visible = true;
                    }
                    else
                    {
                        //console.Controls["btnDetach"].Visible = false;
                        //console.Controls["btnWear"].Visible = false;
                        console.Controls["label11"].Visible = false;

                        console.Controls["btnTP"].Visible = false;
                    }

                    //console.Controls["btnGive"].Visible = true;
                }
                catch
                {
                    ;
                }
            }
            else
            {
                if (ShowAuto)
                {
                    panel2.Visible = true;
                    textBox2.Text = io.Name.ToString(CultureInfo.CurrentCulture);
                    textBox3.Text = io.UUID.ToString();
                    ClearCurrentProperties();
                }
                else
                {
                    ClearCurrentProperties();
                }
            }
        }

        private void ClearCurrentProperties()
        {
            if (currentProperties == null) return;

            currentProperties.CleanUp();
            currentProperties.Dispose();
            currentProperties = null;
        }

        private void tmnuDelete_Click(object sender, EventArgs e)
        {
            if (instance.State.CurrentTab != "&Inventory") return;

            if (treeView1.SelectedNode == null) return;

            //nodecol = false;

            DeleteItem(treeView1.SelectedNode);
        }

        private void DeleteItem(TreeNode node)
        {
            if (node == null) return;

            InventoryBase io = (InventoryBase)node.Tag;

            if (io is InventoryFolder inventoryFolder)
            {
                try
                {
                    InventoryFolder aitem = (InventoryFolder)treeView1.SelectedNode.Tag;   // (InventoryItem)node.Tag;

                    if (aitem.PreferredType != FolderType.None)
                    {
                        return;
                        //DialogResult result = MessageBox.Show("You are about to delete a SYSTEM FOLDER!\nAre you sure you want to continue?", "MEGAbolt", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                        //if (DialogResult.No == result)
                        //{
                        //    return;
                        //}
                    }
                }
                catch
                {
                    return;
                }
                
                client.Inventory.MoveFolder(inventoryFolder.UUID, client.Inventory.FindFolderForType(FolderType.Trash));
                inventoryFolder = null;
            }
            else if (io is InventoryItem item)
            {
                //client.Inventory.RemoveItem(item.UUID);
                //item = null;

                InventoryFolder folder = (InventoryFolder)client.Inventory.Store.Items[client.Inventory.FindFolderForType(FolderType.Trash)].Data;

                client.Inventory.MoveItem(item.UUID, folder.UUID, item.Name);
                item = null;
            }

            io = null;

            node.Remove();
            node = null;

            RefreshInventory();
        }

        private void tmnuCut_Click(object sender, EventArgs e)
        {
            if (instance.State.CurrentTab != "&Inventory") return;

            if (treeView1.SelectedNode == null) return;

            //nodecol = true;

            selectednode = treeView1.SelectedNode;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (io is InventoryFolder)
            {
                InventoryFolder aitem = (InventoryFolder)treeView1.SelectedNode.Tag;   // (InventoryItem)node.Tag;

                if (aitem.PreferredType != FolderType.None)
                {
                    return;
                }
            }

            clip.SetClipboardNode(treeView1.SelectedNode, true);
            //iscut = true;
            tmnuPaste.Enabled = true;
            pasteMenu.Enabled = true;

            RefreshInventory();
        }

        private void tmnuPaste_Click(object sender, EventArgs e)
        {
            try
            {
                if (instance.State.CurrentTab != "&Inventory") return;

                if (treeView1.SelectedNode == null) return;

                //nodecol = true;

                selectednode = treeView1.SelectedNode;

                clip.PasteTo(treeView1.SelectedNode);
                tmnuPaste.Enabled = false;

                //iscut = false;
                tmnuPaste.Enabled = false;
                pasteMenu.Enabled = false;

                RefreshInventory();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tmnuNewFolder_Click(object sender, EventArgs e)
        {
            AddNewFolder();

            RefreshInventory();
        }

        private void AddNewFolder()
        {
            string newFolderName = "New Folder";

            if (treeView1.SelectedNode == null)
            {
                InventoryFolder rtFolder = client.Inventory.Store.RootFolder;
                client.Inventory.CreateFolder(rtFolder.UUID, newFolderName);

                return;
            }

            //nodecol = false;

            if (treeView1.SelectedNode.Tag is InventoryFolder tag)
            {
                client.Inventory.CreateFolder(tag.UUID, newFolderName);

                //UpdateFolder(selfolder.UUID);
            }
            else if (treeView1.SelectedNode.Tag is InventoryItem selfolder)
            {
                client.Inventory.CreateFolder(selfolder.ParentUUID, newFolderName);

                //UpdateFolder(selfolder.ParentUUID);
            }
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            //if (e.Node.Nodes[0].Tag == null)
            //{
            //    InventoryFolder folder = (InventoryFolder)client.Inventory.Store[new UUID(e.Node.Name)];    //(InventoryFolder)e.Node.Tag;

            //    if (SortBy == "Date")
            //    {
            //        client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);
            //    }
            //    else
            //    {
            //        client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByName | InventorySortOrder.FoldersByName);
            //    }
            //}
        }

        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            TreeNode aNode = (TreeNode)e.Item;

            InventoryBase io = (InventoryBase)aNode.Tag;

            if (aNode.Tag is InventoryFolder)
            {
                InventoryFolder folder = (InventoryFolder)io;

                if (folder.PreferredType != FolderType.None)
                {
                    return;
                }
            }
            else
            {
                InventoryItem item = (InventoryItem)io;

                if ((item.Permissions.OwnerMask & PermissionMask.Transfer) != PermissionMask.Transfer)
                {
                    return;
                }
            }

            treeView1.DoDragDrop(aNode, DragDropEffects.Move | DragDropEffects.Copy);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            tbtnNew.Enabled = tbtnSort.Enabled = tbtnOrganize.Enabled = (treeView1.SelectedNode != null);

            if (e.Node.Tag.ToString() == "empty") return;

            if (instance.CoF == null)
            {
                GetRoots();
                //return;
            }

            tbtnNew.Enabled = true;
            tbtnOrganize.Enabled = true;

            if (e.Node.Tag is InventoryFolder)
            {
                //tmnuRename.Enabled = false;
                replaceOutfitToolStripMenuItem.Visible = true;
                wearToolStripMenuItem.Visible = false;
                attachToToolStripMenuItem.Visible = false;
                toolStripButton2.Enabled = true;

                InventoryFolder aitem = (InventoryFolder)treeView1.SelectedNode.Tag;   // (InventoryItem)node.Tag;

                if (aitem.PreferredType == FolderType.Trash)
                {
                    for (int i = 0; i < smM1.Items.Count; i++)
                    {
                        smM1.Items[i].Visible = false;
                    }

                    emptyMenu.Visible = true;
                    emptyTrashToolStripMenuItem.Visible = true;
                }
                else if (aitem.PreferredType == FolderType.CurrentOutfit)
                {
                    tbtnNew.Enabled = false;
                    tbtnOrganize.Enabled = false;
                }
                else
                {
                    for (int i = 0; i < smM1.Items.Count; i++)
                    {
                        smM1.Items[i].Visible = true;
                    }

                    emptyMenu.Visible = false;
                    emptyTrashToolStripMenuItem.Visible = false;
                    cutMenu.Enabled = true;
                    copyMenu.Enabled = true;
                    renameToolStripMenuItem.Enabled = true;
                    deleteToolStripMenuItem.Enabled = true;
                    tmnuCut.Enabled = true;
                    tmnuRename.Enabled = true;
                    tmnuDelete.Enabled = true;
                    tmnuCopy.Enabled = true;

                    InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

                    if (io is InventoryFolder)
                    {
                        if (aitem.PreferredType != FolderType.None)
                        {
                            cutMenu.Enabled = false;
                            copyMenu.Enabled = false;
                            renameToolStripMenuItem.Enabled = false;
                            deleteToolStripMenuItem.Enabled = false;
                            tmnuCut.Enabled = false;
                            tmnuRename.Enabled = false;
                            tmnuDelete.Enabled = false;
                            tmnuCopy.Enabled = false;
                        }
                    }
                }
            }
            else if (e.Node.Tag is InventoryItem)
            {
                //tmnuRename.Enabled = true;
                replaceOutfitToolStripMenuItem.Visible = false;

                //InventoryFolder aitem = (InventoryFolder)treeView1.SelectedNode.Tag;
                InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

                if (io is InventoryObject || io is InventoryAttachment)
                {
                    try
                    {
                        if (gotCoF)
                        {
                            if (io.ParentUUID != instance.CoF.UUID)
                            {
                                attachToToolStripMenuItem.Visible = true;
                            }
                        }
                        else
                        {
                            attachToToolStripMenuItem.Visible = true;
                        }
                    }
                    catch
                    {
                        attachToToolStripMenuItem.Visible = true;
                    }
                }

                if (io.ParentUUID == instance.CoF.UUID)
                {
                    tbtnNew.Enabled = false;
                    tbtnOrganize.Enabled = false;
                }

                if (io is InventoryWearable || io is InventoryObject || io is InventoryAttachment)
                {
                    wearToolStripMenuItem.Visible = true;
                }
                else
                {
                    wearToolStripMenuItem.Visible = false;
                }

                cutMenu.Enabled = true;
                copyMenu.Enabled = true;
                renameToolStripMenuItem.Enabled = true;
                deleteToolStripMenuItem.Enabled = true;
                tmnuCut.Enabled = true;
                tmnuRename.Enabled = true;
                tmnuDelete.Enabled = true;
                tmnuCopy.Enabled = true;
            }

            RefreshPropertiesPane();
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tbtnSort_Click(object sender, EventArgs e)
        {

        }

        private void tmnuNewNotecard_Click(object sender, EventArgs e)
        {
            string newNotecardName = "New Notecard";
            string newNotecardDescription = String.Format(CultureInfo.CurrentCulture, "{0} created with MEGAbolt {1}", newNotecardName, DateTime.Now); ;
            string newNotecardContent = string.Empty;

            //nodecol = false;

            if (treeView1.SelectedNode == null)
            {
                AddNewNotecard(
                    newNotecardName,
                    newNotecardDescription,
                    newNotecardContent,
                    treeView1.Nodes[0]);
            }
            else
            {
                AddNewNotecard(
                    newNotecardName,
                    newNotecardDescription,
                    newNotecardContent,
                    treeView1.SelectedNode);
            }
        }

        private void AddNewNotecard(string notecardName, string notecardDescription, string notecardContent, TreeNode node)
        {
            if (node == null) return;

            InventoryFolder folder = null;

            //nodecol = false;

            if (node.Text == "(empty)")
            {
                folder = (InventoryFolder)node.Parent.Tag;
            }
            else
            {
                if (node.Tag is InventoryFolder tag)
                {
                    folder = tag;
                }
                else if (node.Tag is InventoryItem)
                {

                    folder = (InventoryFolder)node.Parent.Tag;
                }
            }

            client.Inventory.RequestCreateItem(folder.UUID,
                    notecardName, notecardDescription, AssetType.Notecard, UUID.Random(), InventoryType.Notecard, PermissionMask.All,
                    delegate(bool success, InventoryItem nitem)
                    {
                        if (success) // upload the asset
                        {
                            client.Inventory.RequestUploadNotecardAsset(CreateNotecardAsset(""), nitem.UUID, OnNoteUpdate);
                        }
                    }
                );
        }

        /// <summary>
        /// </summary>
        /// <param name="body"></param>
        public static byte[] CreateNotecardAsset(string body)
        {
            body = body.Trim();

            // Format the string body into Linden text
            string lindenText = "Linden text version 1\n";
            lindenText += "{\n";
            lindenText += "LLEmbeddedItems version 1\n";
            lindenText += "{\n";
            lindenText += "count 0\n";
            lindenText += "}\n";
            lindenText += "Text length " + body.Length + "\n";
            lindenText += body;
            lindenText += "}\n";

            // Assume this is a string, add 1 for the null terminator
            byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(lindenText);
            byte[] assetData = new byte[stringBytes.Length]; //+ 1];
            Array.Copy(stringBytes, 0, assetData, 0, stringBytes.Length);

            return assetData;
        }

        void OnNoteUpdate(bool success, string status, UUID itemID, UUID assetID)
        {
            if (success)
            {
                //InventoryBase invObj = client.Inventory.Store[ifolder.UUID];
                //UpdateFolder(ifolder.UUID);
                //WorkPool.QueueUserWorkItem(new WaitCallback(UpdateFolder), ifolder.UUID);

                //InventoryItem ifitm = client.Inventory.FetchItem(itemID, client.Self.AgentID, 3000);

                //UpdateFolder(ifitm.ParentUUID);

                //if (selectednode != null)
                //{
                //    ReloadInventory();
                //}

                RefreshInventory();
            }
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (panel1.Visible)
            {
                panel1.Visible = false;
                textBox1.Text = string.Empty;
            }
            else
            {
                panel1.Visible = true;
                textBox1.Focus();
            }
        }

        private void tmnuCopy_Click(object sender, EventArgs e)
        {
            if (instance.State.CurrentTab != "&Inventory") return;

            if (treeView1.SelectedNode == null) return;

            //nodecol = false;

            clip.SetClipboardNode(treeView1.SelectedNode, false);
            tmnuPaste.Enabled = true;
            pasteMenu.Enabled = true;
            //iscut = false;
        }

        private void tmnuRename_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            if (treeView1.SelectedNode.Tag.ToString() == "empty") return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (io is InventoryFolder)
            {
                InventoryFolder aitem = (InventoryFolder)treeView1.SelectedNode.Tag;   // (InventoryItem)node.Tag;

                if (aitem.PreferredType != FolderType.None)
                {
                    return;
                }
            }

            treeView1.SelectedNode.BeginEdit();
        }

        private void FindByText()
        {
            //Boolean found = false;
            //TreeNodeCollection nodes = sellectednode.Nodes;
            //TreeNodeCollection nodes = treeView1.Nodes;

            client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;

            sellectednode.Expand();
            //found = FindRecursive(sellectednode);
            FindRecursive(sellectednode);

            //foreach (TreeNode n in nodes)
            //{
            //    found = FindRecursive(n);
            //    //found = WorkPool.QueueUserWorkItem(new WaitCallback(FindRecursive), n);

            //    if (!found)
            //    {
            //        n.Collapse();
            //        found = false;
            //    }
            //}

            //foreach (TreeNode n in nodes)
            //{
            //    BackgroundWorker bw = new BackgroundWorker();
            //    bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            //    bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            //    object[] oArgs = new object[] { n, "Loading..." };
            //    bw.RunWorkerAsync(oArgs);
            //}

            searching = false;
            client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        //private void FindRecursive(Object treeNode)
        //{
        //    FindRecursive((TreeNode)treeNode);
        //}

        private Boolean FindRecursive(TreeNode treeNode)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    FindRecursive(treeNode);
                }));

                return false;
            }

            string searchstring = textBox1.Text.Trim();
            searchstring = searchstring.ToLower(CultureInfo.CurrentCulture);
            Boolean found = false;

            foreach (TreeNode tn in treeNode.Nodes)
            {
                // if the text properties match, color the item
                if (tn.Text.ToLower(CultureInfo.CurrentCulture).Contains(searchstring))
                {
                    tn.BackColor = Color.Yellow;
                    tn.ForeColor = Color.Red;
                    found = true;
                }

                if (found)
                {
                    tn.Expand();
                    //treeNode.Collapse(); 
                    found = false;
                }

                FindRecursive(tn);
            }

            return found;
        }

        private void ClearBackColor()
        {
            TreeNodeCollection nodes = treeView1.Nodes;

            foreach (TreeNode n in nodes)
            {
                ClearRecursive(n);
            }
        }

        // called by ClearBackColor function
        private void ClearRecursive(TreeNode treeNode)
        {
            foreach (TreeNode tn in treeNode.Nodes)
            {
                tn.BackColor = Color.Lavender;
                tn.ForeColor = Color.Black;
                ClearRecursive(tn);
            }
        }

        private void unExpandInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearBackColor();
            treeView1.CollapseAll();
        }

        private void expandInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.ExpandAll();
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            textBox1.SelectAll();
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            textBox1.SelectAll();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) e.SuppressKeyPress = true;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button7.Enabled = button1.Enabled = (textBox1.Text.Trim().Length > 0);
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            e.SuppressKeyPress = true;

            if (textBox1.Text.Trim().Length < 1) return;

            treeView1.ExpandAll();
            ClearBackColor();

            FindByText();
        }

        private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.CancelEdit) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            //protect against an empty name
            if (string.IsNullOrEmpty(e.Label) || io.ParentUUID == instance.CoF.UUID)
            {
                e.CancelEdit = true;
                //Logger.Log("Attempt to give inventory item a blank name was foiled!", Helpers.LogLevel.Warning);
                return;
            }           

            if (e.Node.Tag is InventoryFolder)
            {
                InventoryFolder aitem = (InventoryFolder)io;

                if (aitem.PreferredType != FolderType.None)
                {
                    e.CancelEdit = true;
                    return;
                }

                client.Inventory.MoveFolder(aitem.UUID, aitem.ParentUUID);
            }
            else if (e.Node.Tag is InventoryItem inventoryItem)
            {
                inventoryItem.Name = e.Label;
                InventoryItem item = (InventoryItem)io;

                if ((item.Permissions.OwnerMask & PermissionMask.Modify) == PermissionMask.Modify)
                {
                    client.Inventory.RequestUpdateItem(item);
                }
                else
                {
                    e.CancelEdit = true;
                    return;
                }
            }

            e.Node.Text = e.Label;

            // Refresh the inventory to reflect the change
            refreshFolderToolStripMenuItem.PerformClick();
        }

        public void refreshFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshInventory();
        }

        public void RefreshInventory()
        {
            if (InvokeRequired) BeginInvoke((MethodInvoker)RefreshInventory);
            else
            {
                TreeNode node = treeView1.SelectedNode;
                InventoryFolder folder = null;

                if (node == null)
                {
                    folder = client.Inventory.Store.RootFolder;
                }
                else
                {
                    if (node.Tag is InventoryFolder tag)
                    {
                        folder = tag;
                    }
                    else if (node.Tag is InventoryItem)
                    {
                        folder = (InventoryFolder)node.Parent.Tag;
                        node = node.Parent;
                    }
                }

                //client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByDate);

                UpdateFolder(folder.UUID);
                treeView1.SelectedNode = node;
                treeView1.HideSelection = false;
                treeView1.SelectedNode.EnsureVisible();
            }
        }

        public void RefreshInventoryNode(TreeNode node)
        {
            if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { RefreshInventoryNode(node); });
            else
            {
                InventoryFolder folder = null;

                selectednode = node;

                if (node == null)
                {
                    folder = client.Inventory.Store.RootFolder;
                }
                else
                {
                    if (node.Tag is InventoryFolder tag)
                    {
                        folder = tag;
                    }
                    else if (node.Tag is InventoryItem)
                    {
                        folder = (InventoryFolder)node.Parent.Tag;
                    }
                }

                UpdateFolder(folder.UUID);
            }
        }

        private void ReloadInventory()
        {
            if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { ReloadInventory(); });
            else
            {
                treeView1.Nodes.Clear();

                treeSorter.CurrentSortName = SortBy;
                treeView1.TreeViewNodeSorter = treeSorter;

                ((ToolStripMenuItem)tbtnSort.DropDown.Items[0]).PerformClick();

                GetRoot();
            }
        }

        public void SortInventory()
        {
            if (InvokeRequired) BeginInvoke((MethodInvoker)delegate { SortInventory(); });
            else
            {
                TreeNode node = treeView1.SelectedNode;

                try
                {
                    treeView1.Sort();
                }
                catch { ; }

                treeView1.SelectedNode = node;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string clth = string.Empty; // String.Empty;
            clth = "Clothing/" + listBox1.Items[x].ToString();

            try
            {
                UUID cfolder = client.Inventory.FindObjectByPath(client.Inventory.Store.RootFolder.UUID, client.Self.AgentID, clth, 30 * 1000);

                if (cfolder == UUID.Zero)
                {
                    Logger.Log("Outfit changer: outfit path '" + clth + "' not found", Helpers.LogLevel.Warning);
                    return;
                }

                List<InventoryBase> contents = client.Inventory.FolderContents(cfolder, client.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);

                if (contents == null)
                {
                    Logger.Log("Outfit changer: failed to get contents of '" + clth + "'", Helpers.LogLevel.Warning);
                    return;
                }

                List<InventoryItem> items = contents.OfType<InventoryItem>().ToList();

                contents = client.Inventory.Store.GetContents(instance.CoF.UUID);
                List<UUID> remclothing = new List<UUID>();

                foreach (InventoryBase item in contents)
                {
                    if (item is InventoryItem)
                    {
                        remclothing.Add(item.UUID);
                    }
                }

                if (remclothing.Count > 0)
                {
                    client.Inventory.Remove(remclothing, null);
                }

                foreach (var item in items)
                {
                    client.Inventory.CreateLink(instance.CoF.UUID, item.UUID, item.Name, string.Empty, AssetType.Link, item.InventoryType, UUID.Random(), (success, newItem) =>
                    {
                        if (success)
                        {
                            client.Inventory.RequestFetchInventory(newItem.UUID, newItem.OwnerID);
                        }
                    });
                }

                client.Appearance.ReplaceOutfit(items);

                ThreadPool.QueueUserWorkItem(sync =>
                {
                    Thread.Sleep(2000);
                    client.Appearance.RequestSetAppearance(true);
                });
                //client.Appearance.RequestSetAppearance(true);

                Logger.Log("Outfit changer: Starting to change outfit to '" + clth + "'", Helpers.LogLevel.Info);
                label5.Text = "Currently wearing folder : " + clth;

                double ntime = Convert.ToDouble(trackBar1.Value);
                DateTime nexttime = DateTime.Now;
                nexttime = nexttime.AddMinutes(ntime);
                label6.Text = "Next clothes change @ " + nexttime.ToShortTimeString();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, Helpers.LogLevel.Error);
            }

            if (x < (listBox1.Items.Count - 1))
            {
                x += 1;
            }
            else
            {
                x = 0;
            }
        }

        //private void ClearCache()
        //{
        //    string folder = client.Settings.ASSET_CACHE_DIR;

        //    if (!Directory.Exists(folder))
        //    {
        //        return;
        //    }

        //    string[] files = Directory.GetFiles(@folder);


        //    foreach (string file in files)
        //        File.Delete(file);
        //}

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                MessageBox.Show("Select a clothes folder first", "MEGAbolt");
                return;
            }

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (io is InventoryFolder)
            {
                panel2.Visible = true;
                ClearCurrentProperties();
                ShowAuto = true;
            }
            else
            {
                MessageBox.Show("Select a clothes folder first", "MEGAbolt");
            }
        }


        // Auto changer procs start here
        private void button5_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
            ShowAuto = false;
        }

        private void AddToFile()
        {
            // Delete the file if it exists.
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (StreamWriter sr = File.CreateText(path))
            {
                foreach (object o in listBox1.Items)
                {
                    // write a line of text to the file
                    sr.WriteLine(o.ToString());
                }

                //sr.Close();
                sr.Dispose();
            }
        }

        private void ReadTextFile()
        {
            try
            {
                if (!File.Exists(path))
                {
                    return;
                }

                listBox1.Items.Clear();

                using (StreamReader sr = File.OpenText(path))
                {
                    string s = "";

                    while ((s = sr.ReadLine()) != null)
                    {
                        listBox1.Items.Add(s);
                    }

                    //sr.Close();
                    sr.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Inventory error (read text file): " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text == "Start")
            {
                if (trackBar1.Value == 0)
                {
                    MessageBox.Show("Select a frequency from the slider first.", "MEGAbolt", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                x = 0;
                timer1.Interval = ((trackBar1.Value) * 60) * 1000;
                timer1.Enabled = true;
                timer1.Start();
                button3.Enabled = false;
                button4.Text = "Stop";

                double ntime = Convert.ToDouble(trackBar1.Value);
                DateTime nexttime = DateTime.Now;
                nexttime = nexttime.AddMinutes(ntime);
                label6.Text = "Next clothes change @ " + nexttime.ToShortTimeString();

                try
                {
                    AddToFile();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Auto clothes changer is running and functional\nbut the details have failed to save into\na text file for the foloowing reason: " + ex.Message, "MEGAbolt");  
                }
            }
            else
            {
                timer1.Stop();
                timer1.Enabled = false;
                button3.Enabled = true;
                button4.Text = "Start";
                label6.Text = string.Empty;
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label4.Text = "Every " + trackBar1.Value.ToString(CultureInfo.CurrentCulture) + " minutes";
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //ListViewItem Item = lvwSelected.Items.Add(lvwFindFriends.Items[vs[i].Index].Text);
            //Item.Tag = (UUID)lvwFindFriends.Items[vs[i].Index].Tag;

            listBox1.Items.Add(textBox2.Text);
            listBox1.Sorted = true;
            textBox2.Text = "Select folder from inventory";
            textBox3.Text = string.Empty;
            textBox2.Focus();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
            {
                MessageBox.Show("Select an entry to remove first", "MEGAbolt");
                return;
            }

            listBox1.Items.Remove(listBox1.SelectedItem);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                textBox2.Text = listBox1.SelectedItem.ToString();
            }

            button2.Enabled = listBox1.SelectedIndex > -1;

            if (button4.Text == "&Start")
            {
                button3.Enabled = listBox1.SelectedIndex > -1;
            }
            else
            {
                button3.Enabled = false;
            }
        }

        private void textBox2_Enter(object sender, EventArgs e)
        {
            textBox2.SelectAll();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string clth = string.Empty;

            try
            {
                clth = "Clothing/" + listBox1.SelectedItem.ToString();
                UUID cfolder = client.Inventory.FindObjectByPath(client.Inventory.Store.RootFolder.UUID, client.Self.AgentID, clth, 20 * 1000);

                if (cfolder == UUID.Zero)
                {
                    Logger.Log("Outfit changer: outfit path '" + clth + "' not found", Helpers.LogLevel.Warning);
                    return;
                }

                List<InventoryBase> contents = client.Inventory.FolderContents(cfolder, client.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);

                if (contents == null)
                {
                    Logger.Log("Outfit changer: failed to get contents of '" + clth + "'", Helpers.LogLevel.Warning);
                    return;
                }

                List<InventoryItem> items = contents.OfType<InventoryItem>().ToList();

                contents = client.Inventory.Store.GetContents(instance.CoF.UUID);
                List<UUID> remclothing = contents.OfType<InventoryItem>().Select(item => item.UUID).ToList();

                if (remclothing.Count > 0)
                {
                    client.Inventory.Remove(remclothing, null);
                }

                foreach (var item in items)
                {
                    client.Inventory.CreateLink(instance.CoF.UUID, item.UUID, item.Name, string.Empty, AssetType.Link, item.InventoryType, UUID.Random(), (success, newItem) =>
                    {
                        if (success)
                        {
                            client.Inventory.RequestFetchInventory(newItem.UUID, newItem.OwnerID);
                        }
                    });
                }

                client.Appearance.ReplaceOutfit(items);

                ThreadPool.QueueUserWorkItem(sync =>
                {
                    Thread.Sleep(2000);
                    client.Appearance.RequestSetAppearance(true);
                });

                Logger.Log("Outfit changer: Starting to change outfit to '" + clth + "'", Helpers.LogLevel.Info);
                label5.Text = "Currently wearing folder : " + clth;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, Helpers.LogLevel.Error);
            }
        }

        private void snapshotToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void tmnuNewScript_Click(object sender, EventArgs e)
        {
            string newScriptName = "New Script";
            string newScriptDescription = String.Format(CultureInfo.CurrentCulture, "{0} created with MEGAbolt {1}", newScriptName, DateTime.Now); ;
            string newScriptContent = string.Empty;


            if (treeView1.SelectedNode == null)
            {
                AddNewScript(
                    newScriptName,
                    newScriptDescription,
                    newScriptContent,
                    treeView1.Nodes[0]);
            }
            else
            {
                AddNewScript(
                    newScriptName,
                    newScriptDescription,
                    newScriptContent,
                    treeView1.SelectedNode);
            }
        }

        private void AddNewScript(string scriptName, string scriptDescription, string scriptContent, TreeNode node)
        {
            if (node == null) return;

            InventoryFolder folder = null;

            if (node.Text == "(empty)")
            {
                folder = (InventoryFolder)node.Parent.Tag;
            }
            else
            {
                if (node.Tag is InventoryFolder tag)
                {
                    folder = tag;
                }
                else if (node.Tag is InventoryItem)
                {

                    folder = (InventoryFolder)node.Parent.Tag;
                }
            }

            client.Inventory.RequestCreateItem(folder.UUID,
                    scriptName, scriptDescription, AssetType.LSLText, UUID.Random(), InventoryType.LSL, PermissionMask.All,
                    delegate(bool success, InventoryItem nitem)
                    {
                        if (success) // upload the asset
                        {
                            string scriptbody = "default\n{\n    state_entry()\n    {\n        llSay(0,'Hello MEGAbolt user');\n    }\n}";
                            client.Inventory.RequestUploadNotecardAsset(CreateScriptAsset(scriptbody), nitem.UUID, OnNoteUpdate);
                        }
                    }
                );
        }

        public static byte[] CreateScriptAsset(string body)
        {
            body = body.Trim();

            // Format the string body into Linden text
            string lindenText = body;

            // Assume this is a string, add 1 for the null terminator
            byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(lindenText);
            byte[] assetData = new byte[stringBytes.Length]; //+ 1];
            Array.Copy(stringBytes, 0, assetData, 0, stringBytes.Length);

            return assetData;
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            tmnuRename.PerformClick();
        }

        private void expandInventoryToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            treeView1.ExpandAll();
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            if (treeView1.SelectedNode.Tag.ToString() == "empty") return;

            if (treeView1.SelectedNode.Tag is InventoryItem tag)
            {
                InventoryBase io = tag;
                InventoryItem item = (InventoryItem)io;

                if (item.InventoryType != InventoryType.Landmark)
                    return;

                UUID landmark = new UUID();

                if (!UUID.TryParse(item.AssetUUID.ToString(), out landmark))
                {
                    MessageBox.Show("Invalid TP LLUID", "Teleport", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                client.Self.Teleport(landmark);

                //if (client.Self.Teleport(landmark))
                ////if (client.Self.Teleport(item.AssetUUID))
                //{
                //    MessageBox.Show("Teleport successful", "Teleport", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //}
                //else
                //{
                //    MessageBox.Show("Teleport failed", "Teleport", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //}
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void treeView1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void tbtnOrganize_Click(object sender, EventArgs e)
        {

        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshInventory();
        }

        private void replaceOutfitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            ReplacesesOutfit(io);
            //client.Appearance.RequestSetAppearance(true);
        }

        private void ReplacesesOutfit(InventoryBase io)
        {
            //selectednode = treeView1.SelectedNode;
            List<InventoryBase> cofcontents = client.Inventory.Store.GetContents(instance.CoF.UUID);
            List<InventoryBase> contents = client.Inventory.Store.GetContents(io.UUID);
            List<InventoryItem> clothing = new List<InventoryItem>();
            List<InventoryItem> oitems = new List<InventoryItem>();

            foreach (InventoryBase item in contents)
            {
                //if (item.InventoryType == InventoryType.Wearable || item.InventoryType == InventoryType.Attachment || item.InventoryType == InventoryType.Object)
                if (item is InventoryItem inventoryItem)
                {
                    clothing.Add(inventoryItem);
                }
            }

            List<UUID> remclothing = new List<UUID>();

            foreach (InventoryBase item in cofcontents)
            {
                remclothing.Add(item.UUID);
            }

            if (remclothing.Count > 0)
            {
                client.Inventory.Remove(remclothing, null);
            }

            foreach (var item in clothing)
            {
                client.Inventory.CreateLink(instance.CoF.UUID, item.UUID, item.Name, string.Empty, AssetType.Link, item.InventoryType, UUID.Random(), (success, newItem) =>
                {
                    if (success)
                    {
                        client.Inventory.RequestFetchInventory(newItem.UUID, newItem.OwnerID);
                    }
                });
            }

            foreach (var item in oitems)
            {
                client.Appearance.Attach(item, AttachmentPoint.Default, false);
            }

            managerbusy = client.Appearance.ManagerBusy;
            client.Appearance.ReplaceOutfit(clothing);

            ThreadPool.QueueUserWorkItem(sync =>
            {
                Thread.Sleep(2000);
                client.Appearance.RequestSetAppearance(true);
            });
        }

        private void smM1_Opening(object sender, CancelEventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                e.Cancel = true;
                return;
            }

            if (treeView1.SelectedNode.Tag.ToString() == "empty")
            {
                e.Cancel = true;
                return;
            }

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;
            string sitem = treeView1.SelectedNode.Text;

            if (io is InventoryFolder)
            {
                if (gotCoF == true)
                {
                    //InventoryFolder folder = (InventoryFolder)io;

                    if (io.UUID == instance.CoF.UUID)
                    {
                        takeOffToolStripMenuItem.Visible = false;
                        wearToolStripMenuItem.Visible = false;
                        replaceOutfitToolStripMenuItem.Visible = false;
                        attachToToolStripMenuItem.Visible = false;
                        newFolderToolStripMenuItem.Enabled = false;
                        newNotecardToolStripMenuItem.Enabled = false;
                        newScriptToolStripMenuItem.Enabled = false;
                    }
                    else
                    {
                        newFolderToolStripMenuItem.Enabled = true;
                        newNotecardToolStripMenuItem.Enabled = true;
                        newScriptToolStripMenuItem.Enabled = true;
                    }
                }
            }
            else
            {
                if (gotCoF == true)
                {
                    if (io.ParentUUID == instance.CoF.UUID)
                    {
                        takeOffToolStripMenuItem.Visible = true;
                        wearToolStripMenuItem.Visible = false;
                        attachToToolStripMenuItem.Visible = false;

                        newFolderToolStripMenuItem.Enabled = false;
                        newNotecardToolStripMenuItem.Enabled = false;
                        newScriptToolStripMenuItem.Enabled = false;
                        deleteToolStripMenuItem.Enabled = false;
                        renameToolStripMenuItem.Enabled = false;
                        cutMenu.Enabled = false;
                        copyMenu.Enabled = false;
                        pasteMenu.Enabled = false;
                    }
                    else
                    {
                        newFolderToolStripMenuItem.Enabled = true;
                        newNotecardToolStripMenuItem.Enabled = true;
                        newScriptToolStripMenuItem.Enabled = true;
                        //cutMenu.Enabled = true;
                        //copyMenu.Enabled = true;

                        if (sitem.ToLower(CultureInfo.CurrentCulture).Contains("worn"))
                        {
                            takeOffToolStripMenuItem.Visible = true;
                            wearToolStripMenuItem.Visible = false;
                        }
                        else
                        {
                            takeOffToolStripMenuItem.Visible = false;

                            //InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

                            if (io is InventoryWearable || io is InventoryObject || io is InventoryAttachment)
                            {
                                wearToolStripMenuItem.Visible = true;
                            }
                        }
                    }
                }
                else
                {
                    if (sitem.ToLower(CultureInfo.CurrentCulture).Contains("worn"))
                    {
                        takeOffToolStripMenuItem.Visible = true;
                        wearToolStripMenuItem.Visible = false;
                    }
                    else
                    {
                        takeOffToolStripMenuItem.Visible = false;

                        //InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

                        if (io is InventoryWearable || io is InventoryObject || io is InventoryAttachment)
                        {
                            wearToolStripMenuItem.Visible = true;
                        }
                    }
                }
            }
        }

        private void cutMenu_Click(object sender, EventArgs e)
        {
            tmnuCut.PerformClick();
        }

        private void pasteMenu_Click(object sender, EventArgs e)
        {
            tmnuPaste.PerformClick();
        }

        private void copyMenu_Click(object sender, EventArgs e)
        {
            tmnuCopy.PerformClick();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tmnuDelete.PerformClick();
        }

        private void newFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddNewFolder();
        }

        private void newScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tmnuNewScript.PerformClick();
        }

        private void newNotecardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tmnuNewNotecard.PerformClick();
        }

        private void emptyMenu_Click(object sender, EventArgs e)
        {
            client.Inventory.EmptyTrash();
            emptyMenu.Visible = false;
        }

        private void emptyTrashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emptyMenu.PerformClick();
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tmnuRename.PerformClick();
        }

        private void treeView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeView1.SelectedNode = sellectednode = treeView1.GetNodeAt(e.X, e.Y);
                smM1.Show(treeView1, e.X, e.Y);
            }

            sellectednode = treeView1.GetNodeAt(e.X, e.Y);
        }

        private void tbtnNew_Click(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {
            //if (e.Node.Nodes[0].Tag == null)
            //{
            //    InventoryFolder folder = (InventoryFolder)e.Node.Tag;
            //    client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByName);
            //}

            //nodecol = true;

            if (e.Node.Nodes[0].Tag == null)
            {
                InventoryFolder folder = (InventoryFolder)client.Inventory.Store[new UUID(e.Node.Name)];    //(InventoryFolder)e.Node.Tag;

                //selectednode = e.Node;

                //folderproc = folder.UUID;

                if (SortBy == "By Date")
                {
                    client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);
                }
                else
                {
                    client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByName | InventorySortOrder.FoldersByName);
                }
            }

            if (e.Node.ImageKey != "OpenFolder")
            {
                e.Node.ImageKey = "OpenFolder";
            }

            selectednode = e.Node;
            
            treeView1.HideSelection = false;
            treeView1.SelectedNode = selectednode;
            treeView1.SelectedNode.EnsureVisible();
        }

        private void treeView1_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            e.Node.ImageKey = "ClosedFolder";
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                //client.Inventory.FolderUpdated -= new EventHandler<FolderUpdatedEventArgs>(Inventory_OnFolderUpdated);
                //searching = true;
                treeView1.ExpandAll();
                ClearBackColor();

                //FindByText();

                TreeViewWalker treeViewWalker = new TreeViewWalker(treeView1);

                treeViewWalker.ProcessNode += treeViewWalker_ProcessNode_HighlightMatchingNodes;

                treeViewWalker.ProcessTree();
            }
            catch
            {
                ;
            }
        }

        private void treeViewWalker_ProcessNode_HighlightMatchingNodes(object sender, ProcessNodeEventArgs e)
        {
            if (e.Node.Text.ToLower(CultureInfo.CurrentCulture).IndexOf(textBox1.Text.ToLower(CultureInfo.CurrentCulture), StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                e.Node.BackColor = Color.Yellow;
                e.Node.ForeColor = Color.Red;
                e.Node.Expand();
            }

            //treeViewWalker.ProcessNode -= new ProcessNodeEventHandler(treeViewWalker_ProcessNode_HighlightMatchingNodes);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;
            searching = true;
            textBox1.Text = string.Empty;
            ClearBackColor();
            searching = false;
            client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;
        }

        private void tstInventory_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        public InventoryItem AInventoryItem(InventoryItem item)
        {
            if (item.IsLink() && client.Inventory.Store.Contains(item.AssetUUID) && client.Inventory.Store[item.AssetUUID] is InventoryItem)
            {
                return (InventoryItem)client.Inventory.Store[item.AssetUUID];
            }

            return item;
        }

        private void wearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (treeView1.SelectedNode.Tag is InventoryFolder)
            {
                InventoryFolder folder = (InventoryFolder)io;

                List<InventoryBase> contents = client.Inventory.Store.GetContents(folder.UUID);

                foreach (InventoryItem item in contents)
                {
                    if (item is InventoryWearable)
                    {
                        ProcessWearItem(item);       
                    }
                }
            }
            else
            {
                InventoryItem item = (InventoryItem)io;

                selectednode = treeView1.SelectedNode;

                ProcessWearItem(item);

                WearTakeoff(true, selectednode);
            }  
        }

        private void ProcessWearItem(InventoryItem item)
        {
            List<InventoryBase> cofcontents = client.Inventory.Store.GetContents(instance.CoF.UUID);
            List<UUID> remclothing = new List<UUID>();

            if (item is InventoryWearable)
            {
                foreach (InventoryItem link in cofcontents)
                {
                    InventoryItem wItem = AInventoryItem(link);

                    if (link.AssetUUID == item.UUID)
                    {
                        remclothing.Add(link.UUID);
                    }
                    //else if (wItem is InventoryWearable)
                    //{
                    //    InventoryWearable ci = (InventoryWearable)wItem;

                    //    if (ci.WearableType == ((InventoryWearable)item).WearableType)
                    //    {
                    //        remclothing.Add(link.UUID);
                    //    }
                    //}
                }
            }

            if (remclothing.Count > 0)
            {
                client.Inventory.Remove(remclothing, null);

                if (item.AssetType == AssetType.Clothing || item.AssetType == AssetType.Bodypart)
                {
                    client.Appearance.RemoveFromOutfit(item);
                }
                else if (item.AssetType == AssetType.Object)
                {
                    client.Appearance.Detach(item);
                }
            }

            if (item.AssetType == AssetType.Clothing || item.AssetType == AssetType.Bodypart)
            {
                managerbusy = client.Appearance.ManagerBusy;
                client.Appearance.AddToOutfit(item, true);
            }
            else if (item.AssetType == AssetType.Object)
            {
                managerbusy = client.Appearance.ManagerBusy;
                client.Appearance.Attach(item, AttachmentPoint.Default, false);
            }

            client.Inventory.CreateLink(instance.CoF.UUID, item.UUID, item.Name, string.Empty, AssetType.Link, item.InventoryType, UUID.Random(), (success, newItem) =>
            {
                if (success)
                {
                    client.Inventory.RequestFetchInventory(newItem.UUID, newItem.OwnerID);
                }
            });

            //WorkPool.QueueUserWorkItem(sync =>
            //{
            //    Thread.Sleep(2000);
            //    client.Appearance.RequestSetAppearance(true);
            //});
        }

        private void AttachTo(InventoryItem item, AttachmentPoint pnt)
        {
            client.Appearance.Attach(item, pnt, false);

            client.Inventory.CreateLink(instance.CoF.UUID, item.UUID, item.Name, string.Empty, AssetType.Link, item.InventoryType, UUID.Random(), (success, newItem) =>
            {
                if (success)
                {
                    client.Inventory.RequestFetchInventory(newItem.UUID, newItem.OwnerID);
                }
            });

            WearTakeoff(true, selectednode);
        }

        public void WearTakeoff(bool wear, TreeNode node)
        {
            try
            {
                if (!wear)
                {
                    treeView1.SelectedNode.Text = instance.CleanReplace(" (WORN)", "", node.Text);
                    treeView1.SelectedNode.ForeColor = Color.Black;
                }
                else
                {
                    treeView1.SelectedNode.Text = instance.CleanReplace(" (WORN)", "", node.Text) + " (WORN)";
                    treeView1.SelectedNode.ForeColor = Color.RoyalBlue;
                }
            }
            catch
            {
                //string exp = ex.Message; 
            }
        }

        private void takeOffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;
            InventoryItem item = (InventoryItem)io;

            if (io.ParentUUID == instance.CoF.UUID)
            {
                if (item.AssetType == AssetType.Link)
                {
                    List<UUID> remclothing = new List<UUID>();

                    remclothing.Add(item.UUID);

                    InventoryItem wItem;

                    try
                    {
                        wItem = AInventoryItem(item);

                        try
                        {
                            client.Inventory.Remove(remclothing, null);

                            if (wItem.AssetType == AssetType.Object)
                            {
                                client.Appearance.Detach(item.AssetUUID);
                            }
                            else
                            {
                                client.Appearance.RemoveFromOutfit(wItem);
                            }
                        }
                        catch (Exception ex)
                        {
                            string exp = ex.Message;
                        }
                    }
                    catch (Exception ex)
                    {
                        string exp = ex.Message;
                    }

                    return;
                }
            }

            selectednode = treeView1.SelectedNode;

            if (item.AssetType == AssetType.Clothing || item.AssetType == AssetType.Bodypart)
            {
                List<InventoryBase> contents = client.Inventory.Store.GetContents(instance.CoF.UUID);
                List<UUID> remclothing = new List<UUID>();

                foreach (InventoryItem ritem in contents)
                {
                    if (ritem.AssetUUID == item.UUID)
                    {
                        remclothing.Add(ritem.UUID);
                    }
                }

                try
                {
                    client.Inventory.Remove(remclothing, null);
                    client.Appearance.RemoveFromOutfit(item);
                }
                catch (Exception ex)
                {
                    //string exp = ex.Message;
                }
            }
            else
            {
                if (item.AssetType == AssetType.Object)
                {
                    List<InventoryBase> contents = client.Inventory.Store.GetContents(instance.CoF.UUID);
                    List<UUID> remclothing = new List<UUID>();

                    foreach (InventoryItem ritem in contents)
                    {
                        if (ritem.AssetUUID == item.UUID)
                        {
                            remclothing.Add(ritem.UUID);
                        }
                    }

                    try
                    {
                        client.Inventory.Remove(remclothing, null);
                        client.Appearance.Detach(item);
                    }
                    catch
                    {
                        //string exp = ex.Message;
                    }
                }
            }

            WearTakeoff(false, selectednode);

            //WorkPool.QueueUserWorkItem(sync =>
            //    {
            //        Thread.Sleep(2000);
            //        client.Appearance.RequestSetAppearance(true);
            //    });
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            RefreshInventory();
        }

        private void reloadInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadInventory();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            selectednode = e.Node;
        }

        private void createFolderOnRootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.SelectedNode = null;
            AddNewFolder();
            ReloadInventory();
        }

        private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Default); 
        }

        private void chestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Chest); 
        }

        private void chinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Chin); 
        }

        private void mouthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Mouth); 
        }

        private void neckToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Neck); 
        }

        private void noseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Nose); 
        }

        private void pelvisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Pelvis); 
        }

        private void earToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftEar); 
        }

        private void eyeBallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftEyeball); 
        }

        private void footToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftFoot); 
        }

        private void foreArmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftForearm); 
        }

        private void handToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftHand); 
        }

        private void hipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftHip); 
        }

        private void lowerLegToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftLowerLeg); 
        }

        private void pecToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftPec); 
        }

        private void shoulderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftShoulder); 
        }

        private void upperArmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftUpperArm); 
        }

        private void upperLegToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftUpperLeg); 
        }

        private void earToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightEar); 
        }

        private void eyeBallToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightEyeball); 
        }

        private void footToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightFoot); 
        }

        private void foreArmToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightForearm); 
        }

        private void handToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightHand); 
        }

        private void hipToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightHip); 
        }

        private void lowerLegToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightLowerLeg); 
        }

        private void pecToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightPec); 
        }

        private void shoulderToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightShoulder); 
        }

        private void upperArmToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightUpperArm); 
        }

        private void upperLegToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightUpperLeg); 
        }

        private void skullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Skull); 
        }

        private void spineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Spine); 
        }

        private void stomachToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            InventoryBase io = (InventoryBase)treeView1.SelectedNode.Tag;

            InventoryItem item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Stomach); 
        }

        private void attachToToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void InventoryConsole_Load(object sender, EventArgs e)
        {
            SetStyle(ControlStyles.DoubleBuffer, true);

            int style = NativeWinAPI.GetWindowLong(Handle, NativeWinAPI.GWL_EXSTYLE);

            style |= NativeWinAPI.WS_EX_COMPOSITE;

            NativeWinAPI.SetWindowLong(Handle, NativeWinAPI.GWL_EXSTYLE, Convert.ToInt64(style));
        }
    }

    internal static class NativeWinAPI
    {
       internal static readonly int GWL_EXSTYLE = -20;
       internal static readonly int WS_EX_COMPOSITE = 0x02000000;

       [DllImport("user32")]
       internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32")]
       internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong);
    }
}

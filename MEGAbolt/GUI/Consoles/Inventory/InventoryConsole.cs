/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2008-2014, www.metabolt.net (METAbolt)
 * Copyright(c) 2021-2022, Sjofn, LLC
 * All rights reserved.
 *  
 * MEGAbolt is free software: you can redistribute it and/or modify
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
using System.Windows.Forms;
using OpenMetaverse;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BugSplatDotNetStandard;
using File = System.IO.File;


namespace MEGAbolt
{
    public partial class InventoryConsole : UserControl
    {
        private readonly GridClient client;
        private readonly MEGAboltInstance instance;
        private InventoryItemConsole currentProperties;
        private readonly InventoryClipboard clip;
        private readonly InventoryTreeSorter treeSorter = new();
        private bool ShowAuto = false;
        private string SortBy = "By Name";

        // auto changer vars
        private string textfile; // = "Outfit.txt";
        private string path; // = Path.Combine(Environment.CurrentDirectory, "Outfit.txt");
        private int x = 0;
        public bool managerBusy = false;
        private bool searching = false;
        private TreeNode sellectednode = new TreeNode();
        private InventoryFolder rootFolder;
        private TreeNode rootNode;
        private TreeNode selectednode = null;
        private UUID favFolder = UUID.Zero;
        private bool gotCoF = false;

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                var crashReporter = new BugSplat(Generated.BugsplatDatabase, "MEGAbolt",
                    Assembly.GetExecutingAssembly().GetName().Version?.ToString())
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

            textfile = $"\\{client.Self.FirstName}_{client.Self.LastName}_Outfit.mtb";
            path = Path.Combine(DataFolder.GetDataFolder(), textfile);

            ReadTextFile();

            InitializeImageList();
            InitializeTree();
            GetRoot();

            instance.insconsole = this;
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

            //Triggers treeInventory's AfterExpand event, thus triggering the root content request
            rootNode.Nodes.Add("Requesting folder contents...");
            rootNode.Expand();
        }

        //Seperate thread
        private void Inventory_OnItemReceived(object sender, ItemReceivedEventArgs e)
        {
            if (InvokeRequired)
            {

                BeginInvoke(new MethodInvoker(() =>
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

                BeginInvoke(new MethodInvoker(() =>
                {
                    ReceivedInventoryItem(item);
                }));

                return;
            }

            try
            {
                var fldr = item.UUID;

                InventoryBase io = item;

                if (io is not InventoryFolder)
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
            var fldr = e.Obj.UUID;

            var io = e.Obj;

            if (io is not InventoryFolder)
            {
                if (e.Obj.ParentUUID != UUID.Zero)
                {
                    fldr = e.Obj.ParentUUID;
                }
            }

            client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;
            UpdateFolder(fldr);
            client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;

        }
        private void Inventory_OnAppearanceSet(object sender, AppearanceSetEventArgs e)
        {
            if (InvokeRequired)
            {

                BeginInvoke(new MethodInvoker(() =>
                {
                    Inventory_OnAppearanceSet(sender, e);
                }));

                return;
            }

            try
            {
                if (favFolder.CompareTo(UUID.Zero) != 0)
                {
                    var foundfolders = client.Inventory.Store.GetContents(favFolder);
                    instance.MainForm.UpdateFavourites(foundfolders);
                }
            }
            catch { ; }

            if (managerBusy)
            {
                managerBusy = false;
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
            foreach (var o in client.Inventory.Store.GetContents(client.Inventory.Store.RootFolder.UUID))
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
                            favFolder = instance.FavsFolder = o.UUID;

                            client.Inventory.RequestFolderContents(favFolder, client.Self.AgentID, true, true, InventorySortOrder.ByDate); ;
                        }
                    }
                }
                else
                {
                    if (o.Name.ToLower(CultureInfo.CurrentCulture) == "favorites" || o.Name.ToLower(CultureInfo.CurrentCulture) == "my favorites")
                    {
                        if (o is InventoryFolder)
                        {
                            favFolder = instance.FavsFolder = o.UUID;
                        }
                    }
                }
            }
        }

        public static bool IsAttached(List<Primitive> attachments, InventoryItem item)
        {
            return attachments.Any(prim => IsAttachment(prim) == item.UUID);
        }

        private static UUID IsAttachment(Primitive prim)
        {
            if (prim.NameValues == null) return UUID.Zero;

            for (var i = 0; i < prim.NameValues.Length; i++)
            {
                if (prim.NameValues[i].Name == "AttachItemID")
                {
                    return (UUID)prim.NameValues[i].Value.ToString();
                }
            }

            return UUID.Zero;
        }

        //Separate thread
        private void Inventory_OnFolderUpdated(object sender, FolderUpdatedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    Inventory_OnFolderUpdated(sender, e);
                }));

                return;
            }

            try
            {
                if (searching) { return; }

                client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;
                UpdateFolder(e.FolderID);
                client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;

                if (instance.CoF != null)
                {
                    if (e.FolderID == instance.CoF.UUID)
                    {
                        instance.CoF = (InventoryFolder)client.Inventory.Store.Items[client.Inventory.FindFolderForType(FolderType.CurrentOutfit)].Data;
                    }
                }
            }
            catch { ; }
        }

        private void CleanUp()
        {
            ClearCurrentProperties();
            timer1.Enabled = false;
        }

        public void UpdateFolder(UUID folderID)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate { UpdateFolder(folderID); });
            }
            else
            {
                if (searching) { return; }

                if (folderID == UUID.Zero)
                {
                    folderID = client.Inventory.Store.RootFolder.UUID;
                }

                try
                {
                    var treeViewWalker = new TreeViewWalker(treeView1);

                    treeViewWalker.LoadInventory(instance, folderID);

                    if (selectednode != null)
                    {
                        treeView1.HideSelection = false;
                        treeView1.SelectedNode = selectednode;
                        treeView1.SelectedNode.EnsureVisible();
                    }
                }
                catch { ; }

                if (folderID == favFolder)
                {
                    var invroot = client.Inventory.Store.GetContents(favFolder);
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
                client.Appearance.AppearanceSet += Inventory_OnAppearanceSet;
                client.Inventory.Store.InventoryObjectRemoved += Store_OnInventoryObjectRemoved;
                client.Network.EventQueueRunning += Network_OnEventQueueRunning;

                foreach (var method in treeSorter.GetSortMethods())
                {
                    var item = (ToolStripMenuItem)tbtnSort.DropDown.Items.Add(method.Name);
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

        private void InventoryConsole_Disposed(object sender, EventArgs e)
        {
            client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;
            client.Inventory.ItemReceived -= Inventory_OnItemReceived;
            client.Appearance.AppearanceSet -= Inventory_OnAppearanceSet;
            client.Inventory.Store.InventoryObjectRemoved -= Store_OnInventoryObjectRemoved;
            client.Network.EventQueueRunning -= Network_OnEventQueueRunning;
        }

        private void SortMethodClick(object sender, EventArgs e)
        {
            client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;

            var node = treeView1.SelectedNode;

            if (!string.IsNullOrEmpty(treeSorter.CurrentSortName))
                ((ToolStripMenuItem)tbtnSort.DropDown.Items[treeSorter.CurrentSortName]).Checked = false;

            var item = (ToolStripMenuItem)sender;
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

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (io is InventoryItem item)
            {
                panel2.Visible = false;
                
                var console = new InventoryItemConsole(instance, item);
                console.Dock = DockStyle.Fill;
                splitContainer1.Panel2.Controls.Add(console);

                ClearCurrentProperties();
                currentProperties = console;

                try
                {
                    switch (item.InventoryType)
                    {
                        case InventoryType.Wearable or InventoryType.Attachment or InventoryType.Object:
                            console.Controls["label11"].Visible = true;
                            console.Controls["btnTP"].Visible = false;
                            break;
                        case InventoryType.Landmark:
                            console.Controls["btnTP"].Visible = true;
                            break;
                        default:
                            console.Controls["label11"].Visible = false;
                            console.Controls["btnTP"].Visible = false;
                            break;
                    }
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

            var io = (InventoryBase)node.Tag;

            switch (io)
            {
                case InventoryFolder inventoryFolder:
                    try
                    {
                        var aitem = (InventoryFolder)treeView1.SelectedNode.Tag;   // (InventoryItem)node.Tag;

                        if (aitem.PreferredType != FolderType.None)
                        {
                            return;
                        }
                    }
                    catch
                    {
                        return;
                    }
                
                    client.Inventory.MoveFolder(inventoryFolder.UUID, client.Inventory.FindFolderForType(FolderType.Trash));
                    inventoryFolder = null;
                    break;
                case InventoryItem item:
                {
                    var folder = (InventoryFolder)client.Inventory.Store.Items[client.Inventory.FindFolderForType(FolderType.Trash)].Data;

                    client.Inventory.MoveItem(item.UUID, folder.UUID, item.Name);
                    item = null;
                    break;
                }
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

            selectednode = treeView1.SelectedNode;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (io is InventoryFolder)
            {
                var aitem = (InventoryFolder)treeView1.SelectedNode.Tag;   // (InventoryItem)node.Tag;

                if (aitem.PreferredType != FolderType.None)
                {
                    return;
                }
            }

            clip.SetClipboardNode(treeView1.SelectedNode, true);
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

                selectednode = treeView1.SelectedNode;

                clip.PasteTo(treeView1.SelectedNode);
                tmnuPaste.Enabled = false;
                
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
            var newFolderName = "New Folder";

            if (treeView1.SelectedNode == null)
            {
                var rtFolder = client.Inventory.Store.RootFolder;
                client.Inventory.CreateFolder(rtFolder.UUID, newFolderName);

                return;
            }

            switch (treeView1.SelectedNode.Tag)
            {
                case InventoryFolder tag:
                    client.Inventory.CreateFolder(tag.UUID, newFolderName);
                    break;
                case InventoryItem selfolder:
                    client.Inventory.CreateFolder(selfolder.ParentUUID, newFolderName);
                    break;
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
            var aNode = (TreeNode)e.Item;

            var io = (InventoryBase)aNode.Tag;

            if (aNode.Tag is InventoryFolder)
            {
                var folder = (InventoryFolder)io;

                if (folder.PreferredType != FolderType.None)
                {
                    return;
                }
            }
            else
            {
                var item = (InventoryItem)io;

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

            if (e.Node?.Tag == null || e.Node.Tag.ToString() == "empty") { return; }

            if (instance.CoF == null)
            {
                GetRoots();
            }

            tbtnNew.Enabled = true;
            tbtnOrganize.Enabled = true;

            switch (e.Node.Tag)
            {
                case InventoryFolder:
                {
                    replaceOutfitToolStripMenuItem.Visible = true;
                    wearToolStripMenuItem.Visible = false;
                    attachToToolStripMenuItem.Visible = false;
                    toolStripButton2.Enabled = true;

                    var aitem = (InventoryFolder)treeView1.SelectedNode.Tag;   // (InventoryItem)node.Tag;

                    switch (aitem.PreferredType)
                    {
                        case FolderType.Trash:
                        {
                            for (var i = 0; i < smM1.Items.Count; i++)
                            {
                                smM1.Items[i].Visible = false;
                            }

                            emptyMenu.Visible = true;
                            emptyTrashToolStripMenuItem.Visible = true;
                            break;
                        }
                        case FolderType.CurrentOutfit:
                            tbtnNew.Enabled = false;
                            tbtnOrganize.Enabled = false;
                            break;
                        default:
                        {
                            for (var i = 0; i < smM1.Items.Count; i++)
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

                            var io = (InventoryBase)treeView1.SelectedNode.Tag;

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

                            break;
                        }
                    }

                    break;
                }
                case InventoryItem:
                {
                    replaceOutfitToolStripMenuItem.Visible = false;
                        
                    var io = (InventoryBase)treeView1.SelectedNode?.Tag;

                    if (io is InventoryObject or InventoryAttachment)
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

                    if (io != null && instance.CoF != null 
                                   && io.ParentUUID == instance.CoF.UUID)
                    {
                        tbtnNew.Enabled = false;
                        tbtnOrganize.Enabled = false;
                    }

                    wearToolStripMenuItem.Visible = io is InventoryWearable or InventoryObject or InventoryAttachment;

                    cutMenu.Enabled = true;
                    copyMenu.Enabled = true;
                    renameToolStripMenuItem.Enabled = true;
                    deleteToolStripMenuItem.Enabled = true;
                    tmnuCut.Enabled = true;
                    tmnuRename.Enabled = true;
                    tmnuDelete.Enabled = true;
                    tmnuCopy.Enabled = true;
                    break;
                }
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
            var newNotecardName = "New Notecard";
            var newNotecardDescription = $"{newNotecardName} created with MEGAbolt {DateTime.Now}";
            var newNotecardContent = string.Empty;

            AddNewNotecard(newNotecardName, newNotecardDescription, newNotecardContent, treeView1.SelectedNode ?? treeView1.Nodes[0]);
        }

        private void AddNewNotecard(string notecardName, string notecardDescription, string notecardContent, TreeNode node)
        {
            if (node == null) return;

            InventoryFolder folder = null;

            if (node.Text == "(empty)")
            {
                folder = (InventoryFolder)node.Parent.Tag;
            }
            else
            {
                folder = node.Tag switch
                {
                    InventoryFolder tag => tag,
                    InventoryItem => (InventoryFolder)node.Parent.Tag,
                    _ => folder
                };
            }

            client.Inventory.RequestCreateItem(folder.UUID,
                    notecardName, notecardDescription, AssetType.Notecard, UUID.Random(), InventoryType.Notecard, PermissionMask.All,
                    (success, nitem) =>
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
            var lindenText = "Linden text version 1\n";
            lindenText += "{\n";
            lindenText += "LLEmbeddedItems version 1\n";
            lindenText += "{\n";
            lindenText += "count 0\n";
            lindenText += "}\n";
            lindenText += "Text length " + body.Length + "\n";
            lindenText += body;
            lindenText += "}\n";

            // Assume this is a string, add 1 for the null terminator
            var stringBytes = System.Text.Encoding.UTF8.GetBytes(lindenText);
            var assetData = new byte[stringBytes.Length]; //+ 1];
            Array.Copy(stringBytes, 0, assetData, 0, stringBytes.Length);

            return assetData;
        }

        void OnNoteUpdate(bool success, string status, UUID itemID, UUID assetID)
        {
            if (success)
            {
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

            clip.SetClipboardNode(treeView1.SelectedNode, false);
            tmnuPaste.Enabled = true;
            pasteMenu.Enabled = true;
        }

        private void tmnuRename_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            if (treeView1.SelectedNode.Tag.ToString() == "empty") return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (io is InventoryFolder)
            {
                var aitem = (InventoryFolder)treeView1.SelectedNode.Tag;   // (InventoryItem)node.Tag;

                if (aitem.PreferredType != FolderType.None)
                {
                    return;
                }
            }

            treeView1.SelectedNode.BeginEdit();
        }

        private void FindByText()
        {
            client.Inventory.FolderUpdated -= Inventory_OnFolderUpdated;

            sellectednode.Expand();
            FindRecursive(sellectednode);

            searching = false;
            client.Inventory.FolderUpdated += Inventory_OnFolderUpdated;
        }

        private bool FindRecursive(TreeNode treeNode)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    FindRecursive(treeNode);
                }));

                return false;
            }

            var searchstring = textBox1.Text.Trim();
            searchstring = searchstring.ToLower(CultureInfo.CurrentCulture);
            var found = false;

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
            var nodes = treeView1.Nodes;

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

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            //protect against an empty name
            if (string.IsNullOrEmpty(e.Label) || io.ParentUUID == instance.CoF.UUID)
            {
                e.CancelEdit = true;
                return;
            }           

            switch (e.Node.Tag)
            {
                case InventoryFolder:
                {
                    var aitem = (InventoryFolder)io;

                    if (aitem.PreferredType != FolderType.None)
                    {
                        e.CancelEdit = true;
                        return;
                    }

                    client.Inventory.MoveFolder(aitem.UUID, aitem.ParentUUID);
                    break;
                }
                case InventoryItem inventoryItem:
                {
                    inventoryItem.Name = e.Label;
                    var item = (InventoryItem)io;

                    if ((item.Permissions.OwnerMask & PermissionMask.Modify) == PermissionMask.Modify)
                    {
                        client.Inventory.RequestUpdateItem(item);
                    }
                    else
                    {
                        e.CancelEdit = true;
                        return;
                    }

                    break;
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
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)RefreshInventory);
            }
            else
            {
                var node = treeView1.SelectedNode;
                InventoryFolder folder = null;

                if (node == null)
                {
                    folder = client.Inventory.Store.RootFolder;
                }
                else
                {
                    switch (node.Tag)
                    {
                        case InventoryFolder tag:
                            folder = tag;
                            break;
                        case InventoryItem:
                            folder = (InventoryFolder)node.Parent.Tag;
                            node = node.Parent;
                            break;
                    }
                }

                //client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByDate);

                if (folder != null)
                {
                    UpdateFolder(folder.UUID);
                    treeView1.SelectedNode = node;
                    treeView1.HideSelection = false;
                    treeView1.SelectedNode?.EnsureVisible();
                }
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
                    folder = node.Tag switch
                    {
                        InventoryFolder tag => tag,
                        InventoryItem => (InventoryFolder)node.Parent.Tag,
                        _ => folder
                    };
                }

                UpdateFolder(folder.UUID);
            }
        }

        private void ReloadInventory()
        {
            if (InvokeRequired) BeginInvoke((MethodInvoker)ReloadInventory);
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
            if (InvokeRequired) BeginInvoke((MethodInvoker)SortInventory);
            else
            {
                var node = treeView1.SelectedNode;

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
            var clth = string.Empty; // String.Empty;
            clth = "Clothing/" + listBox1.Items[x];

            try
            {
                var cfolder = client.Inventory.FindObjectByPath(client.Inventory.Store.RootFolder.UUID, client.Self.AgentID, clth, 30 * 1000);

                if (cfolder == UUID.Zero)
                {
                    Logger.Log($"Outfit changer: outfit path '{clth}' not found", Helpers.LogLevel.Warning);
                    return;
                }

                var contents = client.Inventory.FolderContents(cfolder, client.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);

                if (contents == null)
                {
                    Logger.Log($"Outfit changer: failed to get contents of '{clth}'", Helpers.LogLevel.Warning);
                    return;
                }

                var items = contents.OfType<InventoryItem>().ToList();

                contents = client.Inventory.Store.GetContents(instance.CoF.UUID);

                foreach (var item in contents.OfType<InventoryItem>())
                {
                    client.Inventory.RemoveItem(item.UUID);
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

                Logger.Log($"Outfit changer: Starting to change outfit to '{clth}'", Helpers.LogLevel.Info);
                label5.Text = $"Currently wearing folder : {clth}";

                var ntime = Convert.ToDouble(trackBar1.Value);
                var nexttime = DateTime.Now;
                nexttime = nexttime.AddMinutes(ntime);
                label6.Text = $"Next clothes change at {nexttime.ToShortTimeString()}";
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

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                MessageBox.Show("Select a clothing folder first", "MEGAbolt");
                return;
            }

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (io is InventoryFolder)
            {
                panel2.Visible = true;
                ClearCurrentProperties();
                ShowAuto = true;
            }
            else
            {
                MessageBox.Show("Select a clothing folder first", "MEGAbolt");
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

            using var sr = File.CreateText(path);
            foreach (var o in listBox1.Items)
            {
                // write a line of text to the file
                sr.WriteLine(o.ToString());
            }

            //sr.Close();
            sr.Dispose();
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

                using var sr = File.OpenText(path);

                while (sr.ReadLine() is { } s)
                {
                    listBox1.Items.Add(s);
                }

                //sr.Close();
                sr.Dispose();
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

                var ntime = Convert.ToDouble(trackBar1.Value);
                var nexttime = DateTime.Now;
                nexttime = nexttime.AddMinutes(ntime);
                label6.Text = $"Next clothes change at {nexttime.ToShortTimeString()}";

                try
                {
                    AddToFile();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Auto clothing changer is running and functional\n" +
                                    "but the details have failed to save into\n" +
                                    "a text file for the following reason: " + ex.Message, "MEGAbolt");  
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
            label4.Text = $"Every {trackBar1.Value.ToString(CultureInfo.CurrentCulture)} minutes";
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

        private void button2_Click(object sender, EventArgs e)
        {
            var clth = string.Empty;

            try
            {
                clth = "Clothing/" + listBox1.SelectedItem;
                var cfolder = client.Inventory.FindObjectByPath(client.Inventory.Store.RootFolder.UUID, client.Self.AgentID, clth, 20 * 1000);

                if (cfolder == UUID.Zero)
                {
                    Logger.Log($"Outfit changer: outfit path '{clth}' not found", Helpers.LogLevel.Warning);
                    return;
                }

                var contents = client.Inventory.FolderContents(cfolder, client.Self.AgentID, true, true, InventorySortOrder.ByName, 20 * 1000);

                if (contents == null)
                {
                    Logger.Log($"Outfit changer: failed to get contents of '{clth}'", Helpers.LogLevel.Warning);
                    return;
                }

                var items = contents.OfType<InventoryItem>().ToList();

                contents = client.Inventory.Store.GetContents(instance.CoF.UUID);
                foreach (var item in contents.OfType<InventoryItem>())
                {
                    client.Inventory.RemoveItem(item.UUID);
                }

                foreach (var item in items)
                {
                    client.Inventory.CreateLink(instance.CoF.UUID, item.UUID, item.Name, string.Empty, 
                        AssetType.Link, item.InventoryType, UUID.Random(), (success, newItem) =>
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

                Logger.Log($"Outfit changer: Starting to change outfit to '{clth}'", Helpers.LogLevel.Info);
                label5.Text = $"Currently wearing folder : {clth}";
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
            var newScriptName = "New Script";
            var newScriptDescription = $"{newScriptName} created with MEGAbolt {DateTime.Now}";
            var newScriptContent = string.Empty;


            AddNewScript(newScriptName, newScriptDescription, newScriptContent, treeView1.SelectedNode ?? treeView1.Nodes[0]);
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
                folder = node.Tag switch
                {
                    InventoryFolder tag => tag,
                    InventoryItem => (InventoryFolder)node.Parent.Tag,
                    _ => folder
                };
            }

            client.Inventory.RequestCreateItem(folder.UUID,
                    scriptName, scriptDescription, AssetType.LSLText, UUID.Random(), InventoryType.LSL, PermissionMask.All,
                    (success, nitem) =>
                    {
                        if (success) // upload the asset
                        {
                            var scriptbody = "default\n{\n    state_entry()\n    {\n        llSay(0,'Hello MEGAbolt user');\n    }\n}";
                            client.Inventory.RequestUploadNotecardAsset(CreateScriptAsset(scriptbody), nitem.UUID, OnNoteUpdate);
                        }
                    }
                );
        }

        public static byte[] CreateScriptAsset(string body)
        {
            body = body.Trim();

            // Format the string body into Linden text
            var lindenText = body;

            // Assume this is a string, add 1 for the null terminator
            var stringBytes = System.Text.Encoding.UTF8.GetBytes(lindenText);
            var assetData = new byte[stringBytes.Length]; //+ 1];
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
                var item = (InventoryItem)io;

                if (item.InventoryType != InventoryType.Landmark)
                    return;

                var landmark = new UUID();

                if (!UUID.TryParse(item.AssetUUID.ToString(), out landmark))
                {
                    MessageBox.Show("Invalid TP LLUID", "Teleport", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                client.Self.Teleport(landmark);
            }
        }

        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void treeView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(TreeNode)) ? DragDropEffects.Copy : DragDropEffects.None;
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

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            ReplacesesOutfit(io);
        }

        private void ReplacesesOutfit(InventoryBase io)
        {
            var cofcontents = client.Inventory.Store.GetContents(instance.CoF.UUID);
            var contents = client.Inventory.Store.GetContents(io.UUID);
            var clothing = new List<InventoryItem>();
            var oitems = new List<InventoryItem>();

            foreach (var item in contents)
            {
                if (item is InventoryItem inventoryItem)
                {
                    clothing.Add(inventoryItem);
                }
            }
            
            foreach (var item in cofcontents)
            {
                client.Inventory.RemoveItem(item.UUID);
            }

            foreach (var item in clothing)
            {
                client.Inventory.CreateLink(instance.CoF.UUID, item.UUID, item.Name, string.Empty,
                    AssetType.Link, item.InventoryType, UUID.Random(), (success, newItem) =>
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

            managerBusy = client.Appearance.ManagerBusy;
            client.Appearance.ReplaceOutfit(clothing);

            ThreadPool.QueueUserWorkItem(sync =>
            {
                Thread.Sleep(2000);
                client.Appearance.RequestSetAppearance(true);
            });
        }

        private void smM1_Opening(object sender, CancelEventArgs e)
        {
            if (treeView1.SelectedNode?.Tag == null)
            {
                e.Cancel = true;
                return;
            }

            if (treeView1.SelectedNode.Tag.ToString() == "empty")
            {
                e.Cancel = true;
                return;
            }

            var io = (InventoryBase)treeView1.SelectedNode.Tag;
            var sitem = treeView1.SelectedNode.Text;

            if (io is InventoryFolder)
            {
                if (gotCoF)
                {
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
                if (gotCoF)
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

                        if (sitem.ToLower(CultureInfo.CurrentCulture).Contains("worn"))
                        {
                            takeOffToolStripMenuItem.Visible = true;
                            wearToolStripMenuItem.Visible = false;
                        }
                        else
                        {
                            takeOffToolStripMenuItem.Visible = false;

                            if (io is InventoryWearable or InventoryObject or InventoryAttachment)
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

                        if (io is InventoryWearable or InventoryObject or InventoryAttachment)
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
            if (e.Node.Nodes[0].Tag == null)
            {
                var folder = (InventoryFolder)client.Inventory.Store[new UUID(e.Node.Name)];    //(InventoryFolder)e.Node.Tag;

                if (SortBy == "By Date")
                {
                    client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID,
                        true, true, InventorySortOrder.ByDate | InventorySortOrder.FoldersByName);
                }
                else
                {
                    client.Inventory.RequestFolderContents(folder.UUID, client.Self.AgentID,
                        true, true, InventorySortOrder.ByName | InventorySortOrder.FoldersByName);
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

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                treeView1.ExpandAll();
                ClearBackColor();
                
                var treeViewWalker = new TreeViewWalker(treeView1);

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
            if (e.Node.Text.ToLower(CultureInfo.CurrentCulture).IndexOf(
                    textBox1.Text.ToLower(CultureInfo.CurrentCulture),
                    StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                e.Node.BackColor = Color.Yellow;
                e.Node.ForeColor = Color.Red;
                e.Node.Expand();
            }
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

        public InventoryItem LinkInventoryItem(InventoryItem item)
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

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            if (treeView1.SelectedNode.Tag is InventoryFolder)
            {
                var folder = (InventoryFolder)io;

                var contents = client.Inventory.Store.GetContents(folder.UUID);

                foreach (var item in contents.OfType<InventoryWearable>())
                {
                    ProcessWearItem(item);
                }
            }
            else
            {
                var item = (InventoryItem)io;

                selectednode = treeView1.SelectedNode;

                ProcessWearItem(item);

                WearTakeoff(true, selectednode);
            }  
        }

        private void ProcessWearItem(InventoryItem item)
        {
            var cofcontents = client.Inventory.Store.GetContents(instance.CoF.UUID);
            bool worn = false;
            if (item is InventoryWearable)
            {
                foreach (var link in from InventoryItem link in cofcontents
                         let wItem = LinkInventoryItem(link) where link.AssetUUID == item.UUID select link)
                {
                    client.Inventory.RemoveItem(link.UUID);
                    worn = true;
                }
            }

            if (worn)
            {
                switch (item.AssetType)
                {
                    case AssetType.Clothing or AssetType.Bodypart:
                        client.Appearance.RemoveFromOutfit(item);
                        break;
                    case AssetType.Object:
                        client.Appearance.Detach(item);
                        break;
                }
            }

            switch (item.AssetType)
            {
                case AssetType.Clothing or AssetType.Bodypart:
                    managerBusy = client.Appearance.ManagerBusy;
                    client.Appearance.AddToOutfit(item, true);
                    break;
                case AssetType.Object:
                    managerBusy = client.Appearance.ManagerBusy;
                    client.Appearance.Attach(item, AttachmentPoint.Default, false);
                    break;
            }

            client.Inventory.CreateLink(instance.CoF.UUID, item.UUID, item.Name, string.Empty, 
                AssetType.Link, item.InventoryType, UUID.Random(), (success, newItem) =>
            {
                if (success)
                {
                    client.Inventory.RequestFetchInventory(newItem.UUID, newItem.OwnerID);
                }
            });
        }

        private void AttachTo(InventoryItem item, AttachmentPoint pnt)
        {
            client.Appearance.Attach(item, pnt, false);

            client.Inventory.CreateLink(instance.CoF.UUID, item.UUID, item.Name, string.Empty, 
                AssetType.Link, item.InventoryType, UUID.Random(), (success, newItem) =>
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
            var io = (InventoryBase)treeView1.SelectedNode.Tag;
            var item = (InventoryItem)io;

            if (io.ParentUUID == instance.CoF.UUID)
            {
                if (item.AssetType == AssetType.Link)
                {
                    try
                    {
                        var wItem = LinkInventoryItem(item);

                        try
                        {
                            client.Inventory.RemoveItem(item.UUID);

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
                            var exp = ex.Message;
                        }
                    }
                    catch (Exception ex)
                    {
                        var exp = ex.Message;
                    }

                    return;
                }
            }

            selectednode = treeView1.SelectedNode;

            switch (item.AssetType)
            {
                case AssetType.Clothing or AssetType.Bodypart:
                {
                    var contents = client.Inventory.Store.GetContents(instance.CoF.UUID);

                    foreach (var ritem in contents.Cast<InventoryItem>().Where(
                                 ritem => ritem.AssetUUID == item.UUID))
                    {
                        client.Inventory.RemoveItem(ritem.UUID);
                    }
                    client.Appearance.RemoveFromOutfit(item);

                    break;
                }
                case AssetType.Object:
                {
                    var contents = client.Inventory.Store.GetContents(instance.CoF.UUID);

                    foreach (var ritem in contents.Cast<InventoryItem>().Where(
                                 ritem => ritem.AssetUUID == item.UUID))
                    {
                        client.Inventory.RemoveItem(ritem.UUID);
                    }

                    client.Appearance.Detach(item);

                    break;
                }
            }

            WearTakeoff(false, selectednode);
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

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Default); 
        }

        private void chestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Chest); 
        }

        private void chinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Chin); 
        }

        private void mouthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Mouth); 
        }

        private void neckToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Neck); 
        }

        private void noseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Nose); 
        }

        private void pelvisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Pelvis); 
        }

        private void earToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftEar); 
        }

        private void eyeBallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftEyeball); 
        }

        private void footToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftFoot); 
        }

        private void foreArmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftForearm); 
        }

        private void handToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftHand); 
        }

        private void hipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftHip); 
        }

        private void lowerLegToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftLowerLeg); 
        }

        private void pecToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftPec); 
        }

        private void shoulderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftShoulder); 
        }

        private void upperArmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftUpperArm); 
        }

        private void upperLegToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.LeftUpperLeg); 
        }

        private void earToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightEar); 
        }

        private void eyeBallToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightEyeball); 
        }

        private void footToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightFoot); 
        }

        private void foreArmToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightForearm); 
        }

        private void handToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightHand); 
        }

        private void hipToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightHip); 
        }

        private void lowerLegToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightLowerLeg); 
        }

        private void pecToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightPec); 
        }

        private void shoulderToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightShoulder); 
        }

        private void upperArmToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightUpperArm); 
        }

        private void upperLegToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.RightUpperLeg); 
        }

        private void skullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Skull); 
        }

        private void spineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Spine); 
        }

        private void stomachToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;

            var io = (InventoryBase)treeView1.SelectedNode.Tag;

            var item = (InventoryItem)io;

            AttachTo(item, AttachmentPoint.Stomach); 
        }

        private void attachToToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void InventoryConsole_Load(object sender, EventArgs e)
        {
            SetStyle(ControlStyles.DoubleBuffer, true);

            var style = NativeWinAPI.GetWindowLong(Handle, NativeWinAPI.GWL_EXSTYLE);

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
